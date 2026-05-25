import { expect, test, type APIRequestContext, type Page } from '@playwright/test';

const apiBaseUrl = process.env.E2E_API_BASE_URL ?? 'http://localhost:5080';
const adminEmail = 'admin@storycoffee.co.nz';
const customerEmail = 'john@aucklandcafe.co.nz';
const password = 'password';
const statementInvoiceStatuses = new Set(['Unpaid', 'PartiallyPaid', 'Overdue']);

test.beforeAll(async ({ request }) => {
  if (process.env.E2E_RESET_SEED !== 'true') {
    return;
  }

  const response = await request.post(`${apiBaseUrl}/api/testing/reset`, {
    headers: resetHeaders(),
  });
  await expect(response, 'E2E reset endpoint should be enabled for reset runs').toBeOK();
});

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

  const generatedOrderRow = page.locator('tr').filter({ hasText: generatedOrder.orderNumber }).first();
  await generatedOrderRow.getByRole('checkbox').check();

  const batchResponse = page.waitForResponse((response) =>
    response.url().includes('/api/admin/orders/batch-to-production') &&
    response.request().method() === 'POST'
  );
  await page.getByRole('button', { name: /Send selected to production/ }).click();
  await expect((await batchResponse).ok()).toBeTruthy();
  await expect.poll(async () => {
    const order = await getAdminOrder(request, adminToken, generatedOrder.id);
    return order?.orderStatus;
  }).toBe('InProduction');

  const readyOrder = await completeProductionForOrder(request, adminToken, generatedOrder.id, generatedOrder.orderNumber);
  const shippedOrder = await postJson(request, `/api/admin/orders/${readyOrder.id}/mark-shipped`, adminToken);
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

test('customer portal exposes financial self-service pages', async ({ page, request }) => {
  test.setTimeout(120_000);

  const admin = await apiLogin(request, adminEmail);
  const customer = await apiLogin(request, customerEmail);
  const adminToken = admin.accessToken;
  const customerToken = customer.accessToken;
  const customerId = customer.userProfile.customerId;
  expect(customerId, 'seeded customer login should include customer id').toBeTruthy();

  const invoice = await ensureCustomerOpenInvoice(request, adminToken, customerToken, customerId);
  const statement = await ensureCustomerStatement(request, adminToken, customerToken, invoice.customerId);

  await loginThroughUi(page, customerEmail);
  await page.goto('/customer');
  await expect(page.getByRole('heading', { name: /Welcome,/ })).toBeVisible();
  await expect(page.getByText('Manage your coffee orders and invoices')).toBeVisible();

  await page.goto(`/customer/invoices/${invoice.id}`);
  await expect(page.getByRole('heading', { name: `Invoice ${invoice.invoiceNumber}` })).toBeVisible();
  await expect(page.getByText('Invoice Items')).toBeVisible();

  await page.goto(`/customer/statements/${statement.id}`);
  await expect(page.getByRole('heading', { name: `Statement ${statement.statementNumber}` })).toBeVisible();
  await expect(page.getByText('Statement Invoices')).toBeVisible();

  await page.goto('/customer/settings');
  await expect(page.getByRole('heading', { name: 'Account Settings' })).toBeVisible();
  await expect(page.getByText('Business Information')).toBeVisible();
  await expect(page.getByText('Change Password')).toBeVisible();
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

async function ensureCustomerOpenInvoice(
  request: APIRequestContext,
  adminToken: string,
  customerToken: string,
  customerId: string,
) {
  const existingInvoices = await getJson(request, '/api/customer/invoices', customerToken);
  const existingOpenInvoice = existingInvoices.find((invoice: { status: string }) => statementInvoiceStatuses.has(invoice.status));
  if (existingOpenInvoice) {
    return existingOpenInvoice;
  }

  const generatedOrder = await generateOrderFromStandingOrder(request, adminToken, customerId);
  await postJson(request, '/api/admin/orders/batch-to-production', adminToken, { orderIds: [generatedOrder.id] });
  const readyOrder = await completeProductionForOrder(request, adminToken, generatedOrder.id, generatedOrder.orderNumber);
  const shippedOrder = await postJson(request, `/api/admin/orders/${readyOrder.id}/mark-shipped`, adminToken);
  await postJson(request, `/api/admin/orders/${shippedOrder.id}/generate-invoice`, adminToken);
  await postJson(request, `/api/admin/orders/${shippedOrder.id}/send-invoice`, adminToken);

  await expect.poll(async () => {
    const invoices = await getJson(request, '/api/customer/invoices', customerToken);
    const invoice = invoices.find((entry: { orderId: string; status: string }) =>
      entry.orderId === shippedOrder.id && statementInvoiceStatuses.has(entry.status)
    );
    return invoice?.id ?? '';
  }).not.toBe('');

  const invoices = await getJson(request, '/api/customer/invoices', customerToken);
  return invoices.find((entry: { orderId: string }) => entry.orderId === shippedOrder.id);
}

async function ensureCustomerStatement(
  request: APIRequestContext,
  adminToken: string,
  customerToken: string,
  customerId: string,
) {
  const existingStatements = await getJson(request, '/api/customer/statements', customerToken);
  const existingStatement = existingStatements.find((statement: { customerId: string }) => statement.customerId === customerId);
  if (existingStatement) {
    return existingStatement;
  }

  const generatedStatements = await postJson(request, '/api/admin/statements/generate-weekly', adminToken);
  const generatedStatement = generatedStatements.find((statement: { customerId: string }) => statement.customerId === customerId);
  if (generatedStatement) {
    return generatedStatement;
  }

  const statements = await getJson(request, '/api/customer/statements', customerToken);
  const statement = statements.find((entry: { customerId: string }) => entry.customerId === customerId);
  expect(statement, 'customer should have a generated statement for an open invoice').toBeTruthy();
  return statement;
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

function resetHeaders() {
  return process.env.E2E_RESET_TOKEN
    ? { 'X-StoryCoffee-Test-Token': process.env.E2E_RESET_TOKEN }
    : undefined;
}
