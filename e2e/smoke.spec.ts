import { expect, test, type APIRequestContext, type Page } from '@playwright/test';

const apiBaseUrl = process.env.E2E_API_BASE_URL ?? 'http://localhost:5080';
const adminEmail = 'admin@storycoffee.co.nz';
const customerEmail = 'john@aucklandcafe.co.nz';
const password = 'password';

test('StoryCoffee app loads', async ({ page }) => {
  await page.goto('/');
  await expect(page).toHaveTitle(/StoryCoffee|Vite|React/);
});

test('admin production-to-invoice workflow is visible to customer', async ({ page, request }) => {
  test.setTimeout(90_000);

  const admin = await apiLogin(request, adminEmail);
  const customer = await apiLogin(request, customerEmail);
  const adminToken = admin.accessToken;
  const customerId = customer.userProfile.customerId;
  expect(customerId, 'seeded customer login should include customer id').toBeTruthy();

  const generatedOrder = await generateOrderFromStandingOrder(request, adminToken, customerId);

  await loginThroughUi(page, adminEmail);
  await page.goto('/admin/orders');
  await expect(page.getByRole('heading', { name: 'Orders' })).toBeVisible();
  await expect(page.getByText(generatedOrder.orderNumber)).toBeVisible();

  const batchResponse = page.waitForResponse((response) =>
    response.url().includes('/api/admin/orders/batch-to-production') &&
    response.request().method() === 'POST'
  );
  await page.getByRole('button', { name: /Send All to Production/ }).click();
  await expect((await batchResponse).ok()).toBeTruthy();
  await expect.poll(async () => {
    const order = await getAdminOrder(request, adminToken, generatedOrder.id);
    return order?.orderStatus;
  }).toBe('InProduction');

  const readyOrder = await completeProductionForOrder(request, adminToken, generatedOrder.id, generatedOrder.orderNumber);
  const shippedOrder = await postJson(request, `/api/admin/orders/${readyOrder.id}/mark-shipped`, adminToken);
  await postJson(request, `/api/admin/orders/${shippedOrder.id}/generate-invoice`, adminToken);
  const invoice = await expectInvoiceForOrder(request, adminToken, shippedOrder.id);

  await page.goto('/admin/invoices');
  await expect(page.getByRole('heading', { name: 'Invoices' })).toBeVisible();
  await expect(page.getByText(invoice.invoiceNumber)).toBeVisible();

  await page.evaluate(() => localStorage.clear());
  await loginThroughUi(page, customerEmail);
  await page.goto('/customer/orders');
  await expect(page.getByRole('heading', { name: 'Orders' })).toBeVisible();
  await expect(page.getByText(shippedOrder.orderNumber)).toBeVisible();
  await expect(page.getByText('Shipped').first()).toBeVisible();
});

async function loginThroughUi(page: Page, email: string) {
  await page.goto('/');
  await page.getByLabel('Email').fill(email);
  await page.getByLabel('Password').fill(password);
  await page.getByRole('button', { name: 'Sign In' }).click();
  await expect(page.getByText(email.includes('admin') ? 'StoryCoffee - Admin Portal' : 'StoryCoffee - Customer Portal')).toBeVisible();
}

async function apiLogin(request: APIRequestContext, email: string) {
  return postJson(request, '/api/auth/login', undefined, { email, password });
}

async function generateOrderFromStandingOrder(
  request: APIRequestContext,
  adminToken: string,
  customerId: string,
) {
  const standingOrders = await getJson(request, '/api/admin/standing-orders', adminToken);
  const standingOrder = standingOrders.find((entry: { customerId: string; status: string }) =>
    entry.customerId === customerId && entry.status === 'Active'
  ) ?? standingOrders.find((entry: { status: string }) => entry.status === 'Active');

  expect(standingOrder, 'seed data should include an active standing order').toBeTruthy();
  return postJson(request, `/api/admin/standing-orders/${standingOrder.id}/generate-now`, adminToken);
}

async function completeProductionForOrder(
  request: APIRequestContext,
  adminToken: string,
  orderId: string,
  orderNumber: string,
) {
  const productionItems = await getJson(request, '/api/admin/production/current', adminToken);
  const relatedItems = productionItems.filter((item: { orderIds: string[]; orderNumbers: string[]; status: string }) =>
    item.status !== 'Completed' &&
    (item.orderIds.includes(orderId) || item.orderNumbers.includes(orderNumber))
  );

  expect(relatedItems.length, 'generated order should create production items').toBeGreaterThan(0);
  for (const item of relatedItems) {
    await patchJson(request, `/api/admin/production/items/${item.id}`, adminToken, {
      producedQuantity: item.totalQuantity,
      status: 'Completed',
    });
  }

  await expect.poll(async () => {
    const order = await getAdminOrder(request, adminToken, orderId);
    return order?.orderStatus;
  }).toBe('ReadyToShip');

  return getAdminOrder(request, adminToken, orderId);
}

async function expectInvoiceForOrder(request: APIRequestContext, adminToken: string, orderId: string) {
  const invoices = await getJson(request, '/api/admin/invoices', adminToken);
  const invoice = invoices.find((entry: { orderId: string }) => entry.orderId === orderId);
  expect(invoice, 'shipped order should have a generated invoice').toBeTruthy();
  return invoice;
}

async function getAdminOrder(request: APIRequestContext, adminToken: string, orderId: string) {
  const orders = await getJson(request, '/api/admin/orders', adminToken);
  return orders.find((entry: { id: string }) => entry.id === orderId);
}

async function getJson(request: APIRequestContext, path: string, token?: string) {
  const response = await request.get(`${apiBaseUrl}${path}`, {
    headers: authHeaders(token),
  });
  await expect(response, `${path} should return 2xx`).toBeOK();
  return response.json();
}

async function postJson(request: APIRequestContext, path: string, token?: string, data?: unknown) {
  const response = await request.post(`${apiBaseUrl}${path}`, {
    headers: authHeaders(token),
    data,
  });
  await expect(response, `${path} should return 2xx`).toBeOK();
  return response.json();
}

async function patchJson(request: APIRequestContext, path: string, token: string, data: unknown) {
  const response = await request.patch(`${apiBaseUrl}${path}`, {
    headers: authHeaders(token),
    data,
  });
  await expect(response, `${path} should return 2xx`).toBeOK();
  return response.json();
}

function authHeaders(token?: string) {
  return token ? { Authorization: `Bearer ${token}` } : undefined;
}
