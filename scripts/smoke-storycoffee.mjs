#!/usr/bin/env node

const apiBaseUrl = process.env.STORYCOFFEE_API_URL ?? 'http://localhost:5080';

async function request(path, options = {}) {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...options,
    headers: {
      ...(options.body ? { 'Content-Type': 'application/json' } : {}),
      ...(options.token ? { Authorization: `Bearer ${options.token}` } : {}),
      ...options.headers,
    },
  });

  if (!response.ok) {
    const text = await response.text();
    throw new Error(`${options.method ?? 'GET'} ${path} failed with ${response.status}: ${text}`);
  }

  const contentType = response.headers.get('content-type') ?? '';
  return contentType.includes('application/json') ? response.json() : response.arrayBuffer();
}

async function login(email) {
  return request('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify({ email, password: 'password' }),
  });
}

async function main() {
  await request('/health');
  await request('/ready');

  const admin = await login('admin@storycoffee.co.nz');
  const customer = await login('john@aucklandcafe.co.nz');
  const adminToken = admin.accessToken;
  const customerToken = customer.accessToken;

  const customerOrders = await request('/api/customer/orders', { token: customerToken });
  if (!Array.isArray(customerOrders) || customerOrders.length === 0) {
    throw new Error('Customer orders smoke check returned no orders.');
  }

  const priceBook = await request(`/api/admin/customers/${customer.userProfile.customerId}/price-book`, { token: adminToken });
  const priceBookItem = priceBook.items.find((item) => item.sku === 'HB-1KG') ?? priceBook.items[0];
  const overridePrice = Number((priceBookItem.basePrice - 1).toFixed(2));
  await request(`/api/admin/customers/${customer.userProfile.customerId}/price-book`, {
    method: 'PUT',
    token: adminToken,
    body: JSON.stringify({
      items: [{
        productId: priceBookItem.productId,
        overridePrice,
        isActive: true,
        notes: 'Smoke override',
      }],
    }),
  });

  const customerProducts = await request('/api/customer/products', { token: customerToken });
  const overriddenProduct = customerProducts.find((product) => product.id === priceBookItem.productId);
  if (!overriddenProduct?.hasOverride || overriddenProduct.effectivePrice !== overridePrice) {
    throw new Error('Customer product effective price smoke check failed.');
  }

  const standingOrders = await request('/api/admin/standing-orders', { token: adminToken });
  const standingOrder = standingOrders.find((entry) => entry.customerId === customer.userProfile.customerId);
  if (!standingOrder) {
    throw new Error('No standing order available for price override smoke step.');
  }

  const generatedFromStandingOrder = await request(`/api/admin/standing-orders/${standingOrder.id}/generate-now`, {
    method: 'POST',
    token: adminToken,
  });
  const generatedOverrideItem = generatedFromStandingOrder.items.find((item) => item.productId === priceBookItem.productId);
  if (!generatedOverrideItem || generatedOverrideItem.unitPriceSnapshot !== overridePrice) {
    throw new Error('Generated order did not use customer-specific effective price.');
  }

  let adminOrders = await request('/api/admin/orders', { token: adminToken });
  const generatedOrder = adminOrders.find((order) => order.orderStatus === 'Generated');
  if (generatedOrder) {
    await request('/api/admin/orders/batch-to-production', {
      method: 'POST',
      token: adminToken,
      body: JSON.stringify({ orderIds: [generatedOrder.id] }),
    });
  }

  const productionItems = await request('/api/admin/production/current', { token: adminToken });
  for (const item of productionItems.filter((entry) => entry.status !== 'Completed')) {
    await request(`/api/admin/production/items/${item.id}`, {
      method: 'PATCH',
      token: adminToken,
      body: JSON.stringify({ producedQuantity: item.totalQuantity, status: 'Completed' }),
    });
  }

  adminOrders = await request('/api/admin/orders', { token: adminToken });
  const readyOrder = adminOrders.find((order) => order.orderStatus === 'ReadyToShip');
  if (!readyOrder) {
    throw new Error('No ready-to-ship order available after production smoke step.');
  }

  const shippedOrder = await request(`/api/admin/orders/${readyOrder.id}/mark-shipped`, {
    method: 'POST',
    token: adminToken,
  });
  await request(`/api/admin/orders/${shippedOrder.id}/generate-invoice`, {
    method: 'POST',
    token: adminToken,
  });

  const invoices = await request('/api/admin/invoices', { token: adminToken });
  const invoice = invoices.find((entry) => entry.orderId === shippedOrder.id) ?? invoices[0];
  if (!invoice) {
    throw new Error('Invoice smoke check returned no invoices.');
  }

  await request(`/api/admin/invoices/${invoice.id}/download-url`, { token: adminToken });
  await request(`/api/admin/invoices/${invoice.id}/download`, { token: adminToken });
  const invoiceForEmail = invoice.status === 'Draft' || invoice.status === 'Issued'
    ? invoice
    : invoices.find((entry) => entry.status === 'Draft' || entry.status === 'Issued');
  if (!invoiceForEmail) {
    throw new Error('No draft or issued invoice available for email smoke step.');
  }

  await request(`/api/admin/invoices/${invoiceForEmail.id}/send-email`, {
    method: 'POST',
    token: adminToken,
  });

  const emailLogs = await request('/api/admin/logs/email?page=1&pageSize=10', { token: adminToken });
  if (!emailLogs.items || emailLogs.items.length === 0) {
    throw new Error('Email log smoke check returned no email logs.');
  }

  console.log('StoryCoffee smoke check passed.');
}

main().catch((error) => {
  console.error(error instanceof Error ? error.message : error);
  process.exit(1);
});
