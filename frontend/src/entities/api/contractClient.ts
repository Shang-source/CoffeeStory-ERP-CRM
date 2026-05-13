import { AccountStatus, AdminDashboard, AuditLog, Customer, CustomerDashboard, CustomerPriceBook, CustomerPriceBookItem, CustomerProduct, EmailLog, EmailStatus, Invoice, Order, OrderFrequency, PagedResult, PaymentRecord, Product, ProductionBatch, ProductionItem, StandingOrder, StandingOrderStatus, Statement, UserRole } from '@/entities/types';
import type { LoginResponse, UserProfile } from '@/entities/user/model/authTypes';
import type { components } from '@/shared/api/generated/schema';
import { apiDownloadBlob, apiRequest, apiRequestNoContent, downloadExternalBlob } from '@/shared/api/httpClient';
import type { ApiQuery, ApiRequestBody, ApiResponse } from '@/shared/api/openapi';
import { storeSession } from '@/shared/api/sessionStorage';

type ApiSchemas = components['schemas'];
type RequireKeys<T, K extends keyof T> = T & { [P in K]-?: NonNullable<T[P]> };
type ApiCustomer = ApiSchemas['CustomerDto'];
type ApiProduct = ApiSchemas['ProductDto'];
type ApiCustomerProduct = ApiSchemas['CustomerProductDto'];
type ApiCustomerPriceBook = ApiSchemas['CustomerPriceBookDto'];
type ApiCustomerPriceBookItem = ApiSchemas['CustomerPriceBookItemDto'];
type ApiOrder = ApiSchemas['OrderDto'];
type ApiOrderItem = ApiSchemas['OrderItemDto'];
type ApiInvoice = ApiSchemas['InvoiceDto'];
type ApiInvoiceItem = ApiSchemas['InvoiceItemDto'];
type ApiPaymentRecord = ApiSchemas['PaymentRecordDto'];
type ApiStandingOrder = ApiSchemas['StandingOrderDto'];
type ApiStandingOrderItem = ApiSchemas['StandingOrderItemDto'];
type ApiStatement = ApiSchemas['StatementDto'];
type ApiProductionItem = ApiSchemas['ProductionItemDto'];
type ApiProductionBatch = ApiSchemas['ProductionBatchDto'];
type ApiAuditLog = ApiSchemas['AuditLogDto'];
type ApiEmailLog = ApiSchemas['EmailLogDto'];
type ApiAdminDashboard = ApiResponse<'/api/admin/dashboard', 'get'>;
type ApiCustomerDashboard = ApiResponse<'/api/customer/dashboard', 'get'>;

type PdfDownloadResponse = RequireKeys<ApiResponse<'/api/admin/invoices/{id}/download-url', 'get'>, 'downloadUrl' | 'fileName' | 'fileKey' | 'generatedAt'>;
type AuditLogExportQuery = ApiQuery<'/api/admin/logs/audit/export', 'get'>;
type EmailLogExportQuery = ApiQuery<'/api/admin/logs/email/export', 'get'>;
type BatchToProductionResponse = {
  updated: number;
  orders: Order[];
  productionBatch: ProductionBatch;
};
type MarkOverdueInvoicesResponse = RequireKeys<ApiResponse<'/api/admin/invoices/mark-overdue', 'post'>, 'updatedCount'>;

export interface LogQueryParams {
  search?: string;
  action?: string;
  entityType?: string;
  status?: EmailStatus | '';
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}

export async function login(email: string, password: string) {
  const response = parseLoginResponse(await apiRequest('/api/auth/login', 'post', '/api/auth/login', {
    body: JSON.stringify({ email, password }),
  }));
  storeSession(response.accessToken, response.userProfile);
  return response.userProfile;
}

export async function getAdminOrders() {
  return parseOrders(await apiRequest('/api/admin/orders', 'get', '/api/admin/orders'));
}

export async function getCustomerOrders() {
  return parseOrders(await apiRequest('/api/customer/orders', 'get', '/api/customer/orders'));
}

export async function getCustomerProfile() {
  return parseCustomer(await apiRequest('/api/customer/profile', 'get', '/api/customer/profile'));
}

export async function updateCustomerProfile(customer: Customer) {
  return parseCustomer(await apiRequest('/api/customer/profile', 'put', '/api/customer/profile', {
    body: JSON.stringify({
      businessName: customer.businessName,
      contactPerson: customer.contactPerson,
      email: customer.email,
      phone: customer.phone,
      billingAddress: customer.billingAddress,
      deliveryAddress: customer.deliveryAddress,
    }),
  }));
}

export type ChangePasswordInput = RequireKeys<ApiRequestBody<'/api/customer/password', 'post'>, 'currentPassword' | 'newPassword' | 'confirmNewPassword'>;

export async function changeCustomerPassword(input: ChangePasswordInput) {
  await apiRequestNoContent('/api/customer/password', 'post', '/api/customer/password', {
    body: JSON.stringify(input),
  });
}

export async function getCurrentProduction() {
  return parseProductionItems(await apiRequest('/api/admin/production/current', 'get', '/api/admin/production/current'));
}

export async function getProductionBatches() {
  return parseProductionBatches(await apiRequest('/api/admin/production/batches', 'get', '/api/admin/production/batches'));
}

export async function getAdminCustomers() {
  return parseCustomers(await apiRequest('/api/admin/customers', 'get', '/api/admin/customers'));
}

export async function getAdminCustomer(customerId: string) {
  return parseCustomer(await apiRequest('/api/admin/customers/{id}', 'get', `/api/admin/customers/${customerId}`));
}

export type CustomerPayload = RequireKeys<ApiRequestBody<'/api/admin/customers', 'post'>, 'businessName' | 'contactPerson' | 'email' | 'phone' | 'billingAddress' | 'deliveryAddress' | 'paymentTerms'> & {
  accountStatus: AccountStatus;
};

export async function createAdminCustomer(customer: CustomerPayload) {
  return parseCustomer(await apiRequest('/api/admin/customers', 'post', '/api/admin/customers', {
    body: JSON.stringify(customer),
  }));
}

export async function updateAdminCustomer(customerId: string, customer: CustomerPayload) {
  return parseCustomer(await apiRequest('/api/admin/customers/{id}', 'patch', `/api/admin/customers/${customerId}`, {
    body: JSON.stringify(customer),
  }));
}

export async function sendAdminCustomerInvite(customerId: string) {
  return parseCustomer(await apiRequest('/api/admin/customers/{id}/send-invite', 'post', `/api/admin/customers/${customerId}/send-invite`));
}

export async function getAdminDashboard() {
  return parseAdminDashboard(await apiRequest('/api/admin/dashboard', 'get', '/api/admin/dashboard'));
}

export async function getCustomerDashboard() {
  return parseCustomerDashboard(await apiRequest('/api/customer/dashboard', 'get', '/api/customer/dashboard'));
}

export async function getAdminProducts() {
  return getProducts();
}

export async function getProducts() {
  return parseProducts(await apiRequest('/api/products', 'get', '/api/products'));
}

export async function getAuditLogs(params: LogQueryParams = {}) {
  return parsePagedAuditLogs(await apiRequest('/api/admin/logs/audit', 'get', `/api/admin/logs/audit${toQueryString(params)}`));
}

export async function getEmailLogs(params: LogQueryParams = {}) {
  return parsePagedEmailLogs(await apiRequest('/api/admin/logs/email', 'get', `/api/admin/logs/email${toQueryString(params)}`));
}

export async function exportAuditLogs(params: LogQueryParams = {}) {
  const query = toQueryString(toAuditLogExportQuery(params));
  await apiDownloadBlob('/api/admin/logs/audit/export', 'get', `/api/admin/logs/audit/export${query}`, 'storycoffee-audit-logs.csv');
}

export async function exportEmailLogs(params: LogQueryParams = {}) {
  const query = toQueryString(toEmailLogExportQuery(params));
  await apiDownloadBlob('/api/admin/logs/email/export', 'get', `/api/admin/logs/email/export${query}`, 'storycoffee-email-logs.csv');
}

export type ProductPayload = RequireKeys<ApiRequestBody<'/api/admin/products', 'post'>, 'sku' | 'name' | 'description' | 'unit' | 'price' | 'cost' | 'isActive'>;
export type CustomerPriceBookPayload = RequireKeys<ApiRequestBody<'/api/admin/customers/{id}/price-book', 'put'>, 'items'>;

export type StandingOrderPayload = RequireKeys<Omit<ApiRequestBody<'/api/admin/standing-orders', 'post'>, 'frequency' | 'status' | 'items'>, 'nextClosingDate'> & {
  customerId?: string;
  frequency: OrderFrequency;
  status: StandingOrderStatus;
  items: Array<RequireKeys<ApiSchemas['UpdateStandingOrderItemRequest'], 'productId' | 'quantity'>>;
};

export async function createAdminProduct(product: ProductPayload) {
  return parseProduct(await apiRequest('/api/admin/products', 'post', '/api/admin/products', {
    body: JSON.stringify(product),
  }));
}

export async function updateAdminProduct(productId: string, product: ProductPayload) {
  return parseProduct(await apiRequest('/api/admin/products/{id}', 'patch', `/api/admin/products/${productId}`, {
    body: JSON.stringify(product),
  }));
}

export async function archiveAdminProduct(productId: string) {
  return parseProduct(await apiRequest('/api/admin/products/{id}/archive', 'post', `/api/admin/products/${productId}/archive`));
}

export async function getCustomerProducts() {
  return parseCustomerProducts(await apiRequest('/api/customer/products', 'get', '/api/customer/products'));
}

export async function getAdminCustomerPriceBook(customerId: string) {
  return parseCustomerPriceBook(await apiRequest('/api/admin/customers/{id}/price-book', 'get', `/api/admin/customers/${customerId}/price-book`));
}

export async function updateAdminCustomerPriceBook(customerId: string, priceBook: CustomerPriceBookPayload) {
  return parseCustomerPriceBook(await apiRequest('/api/admin/customers/{id}/price-book', 'put', `/api/admin/customers/${customerId}/price-book`, {
    body: JSON.stringify(priceBook),
  }));
}

export async function getAdminStandingOrders() {
  return parseStandingOrders(await apiRequest('/api/admin/standing-orders', 'get', '/api/admin/standing-orders'));
}

export async function createAdminStandingOrder(standingOrder: StandingOrderPayload) {
  return parseStandingOrder(await apiRequest('/api/admin/standing-orders', 'post', '/api/admin/standing-orders', {
    body: JSON.stringify(standingOrder),
  }));
}

export async function updateAdminStandingOrder(standingOrderId: string, standingOrder: StandingOrderPayload) {
  return parseStandingOrder(await apiRequest('/api/admin/standing-orders/{id}', 'patch', `/api/admin/standing-orders/${standingOrderId}`, {
    body: JSON.stringify(standingOrder),
  }));
}

export async function generateStandingOrderNow(standingOrderId: string) {
  return parseOrder(await apiRequest('/api/admin/standing-orders/{id}/generate-now', 'post', `/api/admin/standing-orders/${standingOrderId}/generate-now`));
}

export async function pauseStandingOrder(standingOrderId: string) {
  return parseStandingOrder(await apiRequest('/api/admin/standing-orders/{id}/pause', 'post', `/api/admin/standing-orders/${standingOrderId}/pause`));
}

export async function resumeStandingOrder(standingOrderId: string) {
  return parseStandingOrder(await apiRequest('/api/admin/standing-orders/{id}/resume', 'post', `/api/admin/standing-orders/${standingOrderId}/resume`));
}

export async function cancelStandingOrder(standingOrderId: string) {
  return parseStandingOrder(await apiRequest('/api/admin/standing-orders/{id}/cancel', 'post', `/api/admin/standing-orders/${standingOrderId}/cancel`));
}

export async function getCustomerStandingOrder() {
  return parseStandingOrder(await apiRequest('/api/customer/standing-order', 'get', '/api/customer/standing-order'));
}

export async function updateCustomerStandingOrder(standingOrder: StandingOrder) {
  return parseStandingOrder(await apiRequest('/api/customer/standing-order', 'put', '/api/customer/standing-order', {
    body: JSON.stringify({
      frequency: standingOrder.frequency,
      deliveryNotes: standingOrder.deliveryNotes,
      items: standingOrder.items.map((item) => ({
        productId: item.productId,
        quantity: item.quantity,
        notes: item.notes,
      })),
    }),
  }));
}

export async function startProduction(productId: string) {
  return parseProductionItem(await apiRequest('/api/admin/production/{productId}/start', 'post', `/api/admin/production/${productId}/start`));
}

export async function updateProducedQuantity(productId: string, producedQuantity: number) {
  return parseProductionItem(await apiRequest('/api/admin/production/{productId}/quantity', 'post', `/api/admin/production/${productId}/quantity`, {
    body: JSON.stringify({ producedQuantity }),
  }));
}

export async function completeProduction(productId: string) {
  return parseProductionItem(await apiRequest('/api/admin/production/{productId}/complete', 'post', `/api/admin/production/${productId}/complete`));
}

export async function sendOrderToProduction(orderId: string) {
  return parseOrder(await apiRequest('/api/admin/orders/{id}/send-to-production', 'post', `/api/admin/orders/${orderId}/send-to-production`));
}

export async function batchSendOrdersToProduction(orderIds: string[]) {
  const response = await apiRequest('/api/admin/orders/batch-to-production', 'post', '/api/admin/orders/batch-to-production', {
    body: JSON.stringify({ orderIds }),
  });
  return parseBatchToProductionResponse(response);
}

export async function markOrderReadyToShip(orderId: string) {
  return parseOrder(await apiRequest('/api/admin/orders/{id}/mark-ready-to-ship', 'post', `/api/admin/orders/${orderId}/mark-ready-to-ship`));
}

export async function markOrderShipped(orderId: string) {
  return parseOrder(await apiRequest('/api/admin/orders/{id}/mark-shipped', 'post', `/api/admin/orders/${orderId}/mark-shipped`));
}

export async function generateInvoice(orderId: string) {
  return parseOrder(await apiRequest('/api/admin/orders/{id}/generate-invoice', 'post', `/api/admin/orders/${orderId}/generate-invoice`));
}

export async function sendInvoice(orderId: string) {
  return parseOrder(await apiRequest('/api/admin/orders/{id}/send-invoice', 'post', `/api/admin/orders/${orderId}/send-invoice`));
}

export async function cancelOrder(orderId: string) {
  return parseOrder(await apiRequest('/api/admin/orders/{id}/cancel', 'post', `/api/admin/orders/${orderId}/cancel`));
}

export async function getAdminInvoices() {
  return parseInvoices(await apiRequest('/api/admin/invoices', 'get', '/api/admin/invoices'));
}

export async function getAdminInvoice(invoiceId: string) {
  return parseInvoice(await apiRequest('/api/admin/invoices/{id}', 'get', `/api/admin/invoices/${invoiceId}`));
}

export async function getCustomerInvoices() {
  return parseInvoices(await apiRequest('/api/customer/invoices', 'get', '/api/customer/invoices'));
}

export async function getCustomerInvoice(invoiceId: string) {
  return parseInvoice(await apiRequest('/api/customer/invoices/{id}', 'get', `/api/customer/invoices/${invoiceId}`));
}

export async function sendInvoiceEmail(invoiceId: string) {
  return parseInvoice(await apiRequest('/api/admin/invoices/{id}/send-email', 'post', `/api/admin/invoices/${invoiceId}/send-email`));
}

export async function downloadAdminInvoicePdf(invoiceId: string) {
  const metadata = await apiRequest('/api/admin/invoices/{id}/download-url', 'get', `/api/admin/invoices/${invoiceId}/download-url`);
  await downloadPdf(parsePdfDownload(metadata));
}

export async function downloadCustomerInvoicePdf(invoiceId: string) {
  const metadata = await apiRequest('/api/customer/invoices/{id}/download-url', 'get', `/api/customer/invoices/${invoiceId}/download-url`);
  await downloadPdf(parsePdfDownload(metadata));
}

export type RecordPaymentInput = RequireKeys<ApiRequestBody<'/api/admin/invoices/{id}/payments', 'post'>, 'amount' | 'paymentDate' | 'paymentMethod' | 'reference'>;

export async function recordInvoicePayment(invoiceId: string, payment: RecordPaymentInput) {
  const response = await apiRequest('/api/admin/invoices/{id}/payments', 'post', `/api/admin/invoices/${invoiceId}/payments`, {
    body: JSON.stringify(payment),
  });
  return parseInvoice(required(response.invoice, 'payment.invoice'));
}

export async function voidInvoicePayment(invoiceId: string, paymentId: string, reason: string) {
  const response = await apiRequest('/api/admin/invoices/{invoiceId}/payments/{paymentId}/void', 'post', `/api/admin/invoices/${invoiceId}/payments/${paymentId}/void`, {
    body: JSON.stringify({ reason }),
  });
  return parseInvoice(required(response.invoice, 'payment.invoice'));
}

export async function markOverdueInvoices() {
  const response = await apiRequest('/api/admin/invoices/mark-overdue', 'post', '/api/admin/invoices/mark-overdue');
  return { updatedCount: required(response.updatedCount, 'markOverdue.updatedCount') } satisfies MarkOverdueInvoicesResponse;
}

export async function getAdminStatements() {
  return parseStatements(await apiRequest('/api/admin/statements', 'get', '/api/admin/statements'));
}

export async function getAdminStatement(statementId: string) {
  return parseStatement(await apiRequest('/api/admin/statements/{id}', 'get', `/api/admin/statements/${statementId}`));
}

export async function generateWeeklyStatements() {
  return parseStatements(await apiRequest('/api/admin/statements/generate-weekly', 'post', '/api/admin/statements/generate-weekly'));
}

export async function sendStatementEmail(statementId: string) {
  return parseStatement(await apiRequest('/api/admin/statements/{id}/send-email', 'post', `/api/admin/statements/${statementId}/send-email`));
}

export async function getCustomerStatements() {
  return parseStatements(await apiRequest('/api/customer/statements', 'get', '/api/customer/statements'));
}

export async function getCustomerStatement(statementId: string) {
  return parseStatement(await apiRequest('/api/customer/statements/{id}', 'get', `/api/customer/statements/${statementId}`));
}

export async function downloadAdminStatementPdf(statementId: string) {
  const metadata = await apiRequest('/api/admin/statements/{id}/download-url', 'get', `/api/admin/statements/${statementId}/download-url`);
  await downloadPdf(parsePdfDownload(metadata));
}

export async function downloadCustomerStatementPdf(statementId: string) {
  const metadata = await apiRequest('/api/customer/statements/{id}/download-url', 'get', `/api/customer/statements/${statementId}/download-url`);
  await downloadPdf(parsePdfDownload(metadata));
}

export function parseOrderResponse(order: Order): Order {
  return parseOrder(order);
}

export function parseInvoiceResponse(invoice: Invoice): Invoice {
  return parseInvoice(invoice);
}

export function parseStatementResponse(statement: Statement): Statement {
  return parseStatement(statement);
}

export function parseStandingOrderResponse(standingOrder: StandingOrder): StandingOrder {
  return parseStandingOrder(standingOrder);
}

export function parseCustomerProductResponse(product: CustomerProduct): CustomerProduct {
  return parseCustomerProduct(product);
}

export function parseCustomerPriceBookResponse(priceBook: CustomerPriceBook): CustomerPriceBook {
  return parseCustomerPriceBook(priceBook);
}

async function downloadPdf(metadata: PdfDownloadResponse) {
  await downloadExternalBlob(metadata.downloadUrl, metadata.fileName);
}

function toAuditLogExportQuery(params: LogQueryParams): AuditLogExportQuery {
  return {
    search: params.search,
    action: params.action,
    entityType: params.entityType,
    from: params.from,
    to: params.to,
  };
}

function toEmailLogExportQuery(params: LogQueryParams): EmailLogExportQuery {
  return {
    search: params.search,
    entityType: params.entityType,
    status: params.status || undefined,
    from: params.from,
    to: params.to,
  };
}

function toQueryString<T extends object>(params: T) {
  const searchParams = new URLSearchParams();
  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') {
      searchParams.set(key, String(value));
    }
  });
  const query = searchParams.toString();
  return query ? `?${query}` : '';
}

function parseOrders(orders: Array<ApiOrder | Order> = []) {
  return orders.map(parseOrder);
}

function parseCustomers(customers: Array<ApiCustomer | Customer> = []) {
  return customers.map(parseCustomer);
}

function parseProducts(products: Array<ApiProduct | Product> = []) {
  return products.map(parseProduct);
}

function parseCustomerProducts(products: Array<ApiCustomerProduct | CustomerProduct> = []) {
  return products.map(parseCustomerProduct);
}

function parseStandingOrders(standingOrders: Array<ApiStandingOrder | StandingOrder> = []) {
  return standingOrders.map(parseStandingOrder);
}

function parseInvoices(invoices: Array<ApiInvoice | Invoice> = []) {
  return invoices.map(parseInvoice);
}

function parseStatements(statements: Array<ApiStatement | Statement> = []) {
  return statements.map(parseStatement);
}

function parseProductionItems(items: Array<ApiProductionItem | ProductionItem> = []) {
  return items.map(parseProductionItem);
}

function parseProductionBatches(batches: Array<ApiProductionBatch | ProductionBatch> = []) {
  return batches.map(parseProductionBatch);
}

function parseAuditLogs(logs: Array<ApiAuditLog | AuditLog> = []) {
  return logs.map(parseAuditLog);
}

function parseEmailLogs(logs: Array<ApiEmailLog | EmailLog> = []) {
  return logs.map(parseEmailLog);
}

function parseLoginResponse(response: ApiResponse<'/api/auth/login', 'post'>): LoginResponse {
  const userProfile = required(response.userProfile, 'login.userProfile');
  return {
    accessToken: required(response.accessToken, 'login.accessToken'),
    expiresIn: required(response.expiresIn, 'login.expiresIn'),
    role: required(response.role, 'login.role'),
    userProfile: {
      id: required(userProfile.id, 'login.userProfile.id'),
      email: required(userProfile.email, 'login.userProfile.email'),
      role: required(userProfile.role, 'login.userProfile.role'),
      customerId: userProfile.customerId ?? undefined,
      name: required(userProfile.name, 'login.userProfile.name'),
    },
  };
}

function parsePagedAuditLogs(result: ApiSchemas['AuditLogDtoPagedResult'] | PagedResult<AuditLog>): PagedResult<AuditLog> {
  return {
    items: parseAuditLogs(result.items ?? []),
    page: required(result.page, 'auditLogs.page'),
    pageSize: required(result.pageSize, 'auditLogs.pageSize'),
    totalCount: required(result.totalCount, 'auditLogs.totalCount'),
    totalPages: required(result.totalPages, 'auditLogs.totalPages'),
  };
}

function parsePagedEmailLogs(result: ApiSchemas['EmailLogDtoPagedResult'] | PagedResult<EmailLog>): PagedResult<EmailLog> {
  return {
    items: parseEmailLogs(result.items ?? []),
    page: required(result.page, 'emailLogs.page'),
    pageSize: required(result.pageSize, 'emailLogs.pageSize'),
    totalCount: required(result.totalCount, 'emailLogs.totalCount'),
    totalPages: required(result.totalPages, 'emailLogs.totalPages'),
  };
}

function parseBatchToProductionResponse(response: ApiResponse<'/api/admin/orders/batch-to-production', 'post'>): BatchToProductionResponse {
  return {
    updated: required(response.updated, 'batchToProduction.updated'),
    orders: parseOrders(response.orders ?? []),
    productionBatch: parseProductionBatch(required(response.productionBatch, 'batchToProduction.productionBatch')),
  };
}

function parseOrder(order: ApiOrder | Order): Order {
  return {
    id: required(order.id, 'order.id'),
    orderNumber: required(order.orderNumber, 'order.orderNumber'),
    customerId: required(order.customerId, 'order.customerId'),
    customer: order.customer ? parseCustomer(order.customer) : undefined,
    standingOrderId: required(order.standingOrderId, 'order.standingOrderId'),
    generatedAt: parseDate(order.generatedAt, 'order.generatedAt'),
    orderStatus: required(order.orderStatus, 'order.orderStatus'),
    invoiceStatus: required(order.invoiceStatus, 'order.invoiceStatus'),
    shipmentStatus: required(order.shipmentStatus, 'order.shipmentStatus'),
    subtotal: required(order.subtotal, 'order.subtotal'),
    gstAmount: required(order.gstAmount, 'order.gstAmount'),
    totalAmount: required(order.totalAmount, 'order.totalAmount'),
    items: (order.items ?? []).map(parseOrderItem),
  };
}

function parseOrderItem(item: ApiOrderItem | Order['items'][number]): Order['items'][number] {
  return {
    id: required(item.id, 'orderItem.id'),
    productId: required(item.productId, 'orderItem.productId'),
    productNameSnapshot: required(item.productNameSnapshot, 'orderItem.productNameSnapshot'),
    skuSnapshot: required(item.skuSnapshot, 'orderItem.skuSnapshot'),
    quantity: required(item.quantity, 'orderItem.quantity'),
    unitPriceSnapshot: required(item.unitPriceSnapshot, 'orderItem.unitPriceSnapshot'),
    lineTotal: required(item.lineTotal, 'orderItem.lineTotal'),
    notes: item.notes ?? undefined,
  };
}

function parseCustomer(customer: ApiCustomer | Customer): Customer {
  return {
    id: required(customer.id, 'customer.id'),
    businessName: required(customer.businessName, 'customer.businessName'),
    contactPerson: required(customer.contactPerson, 'customer.contactPerson'),
    email: required(customer.email, 'customer.email'),
    phone: required(customer.phone, 'customer.phone'),
    billingAddress: required(customer.billingAddress, 'customer.billingAddress'),
    deliveryAddress: required(customer.deliveryAddress, 'customer.deliveryAddress'),
    paymentTerms: required(customer.paymentTerms, 'customer.paymentTerms'),
    accountStatus: required(customer.accountStatus, 'customer.accountStatus'),
    createdAt: parseDate(customer.createdAt, 'customer.createdAt'),
  };
}

function parseProduct(product: ApiProduct | Product): Product {
  return {
    id: required(product.id, 'product.id'),
    sku: required(product.sku, 'product.sku'),
    name: required(product.name, 'product.name'),
    description: required(product.description, 'product.description'),
    unit: required(product.unit, 'product.unit'),
    price: required(product.price, 'product.price'),
    cost: required(product.cost, 'product.cost'),
    isActive: required(product.isActive, 'product.isActive'),
  };
}

function parseCustomerProduct(product: ApiCustomerProduct | CustomerProduct): CustomerProduct {
  const basePrice = required(product.basePrice, 'customerProduct.basePrice');
  const effectivePrice = required(product.effectivePrice, 'customerProduct.effectivePrice');
  return {
    id: required(product.id, 'customerProduct.id'),
    sku: required(product.sku, 'customerProduct.sku'),
    name: required(product.name, 'customerProduct.name'),
    description: required(product.description, 'customerProduct.description'),
    unit: required(product.unit, 'customerProduct.unit'),
    price: effectivePrice,
    cost: 0,
    isActive: true,
    basePrice,
    effectivePrice,
    hasOverride: required(product.hasOverride, 'customerProduct.hasOverride'),
  };
}

function parseCustomerPriceBook(priceBook: ApiCustomerPriceBook | CustomerPriceBook): CustomerPriceBook {
  return {
    customerId: required(priceBook.customerId, 'customerPriceBook.customerId'),
    items: (priceBook.items ?? []).map(parseCustomerPriceBookItem),
  };
}

function parseCustomerPriceBookItem(item: ApiCustomerPriceBookItem | CustomerPriceBookItem): CustomerPriceBookItem {
  return {
    productId: required(item.productId, 'customerPriceBookItem.productId'),
    sku: required(item.sku, 'customerPriceBookItem.sku'),
    name: required(item.name, 'customerPriceBookItem.name'),
    unit: required(item.unit, 'customerPriceBookItem.unit'),
    basePrice: required(item.basePrice, 'customerPriceBookItem.basePrice'),
    overridePrice: item.overridePrice ?? undefined,
    effectivePrice: required(item.effectivePrice, 'customerPriceBookItem.effectivePrice'),
    hasOverride: required(item.hasOverride, 'customerPriceBookItem.hasOverride'),
    isActive: required(item.isActive, 'customerPriceBookItem.isActive'),
    notes: item.notes ?? undefined,
  };
}

function parseStandingOrder(standingOrder: ApiStandingOrder | StandingOrder): StandingOrder {
  return {
    id: required(standingOrder.id, 'standingOrder.id'),
    customerId: required(standingOrder.customerId, 'standingOrder.customerId'),
    customer: standingOrder.customer ? parseCustomer(standingOrder.customer) : undefined,
    frequency: required(standingOrder.frequency, 'standingOrder.frequency'),
    nextClosingDate: parseDate(standingOrder.nextClosingDate, 'standingOrder.nextClosingDate'),
    status: required(standingOrder.status, 'standingOrder.status'),
    deliveryNotes: standingOrder.deliveryNotes ?? undefined,
    internalNotes: standingOrder.internalNotes ?? undefined,
    items: (standingOrder.items ?? []).map(parseStandingOrderItem),
  };
}

function parseStandingOrderItem(item: ApiStandingOrderItem | StandingOrder['items'][number]): StandingOrder['items'][number] {
  return {
    id: required(item.id, 'standingOrderItem.id'),
    productId: required(item.productId, 'standingOrderItem.productId'),
    product: parseProduct(required(item.product, 'standingOrderItem.product')),
    quantity: required(item.quantity, 'standingOrderItem.quantity'),
    unitPrice: required(item.unitPrice, 'standingOrderItem.unitPrice'),
    notes: item.notes ?? undefined,
  };
}

function parseInvoice(invoice: ApiInvoice | Invoice): Invoice {
  return {
    id: required(invoice.id, 'invoice.id'),
    invoiceNumber: required(invoice.invoiceNumber, 'invoice.invoiceNumber'),
    customerId: required(invoice.customerId, 'invoice.customerId'),
    customer: invoice.customer ? parseCustomer(invoice.customer) : undefined,
    orderId: required(invoice.orderId, 'invoice.orderId'),
    issueDate: parseDate(invoice.issueDate, 'invoice.issueDate'),
    dueDate: parseDate(invoice.dueDate, 'invoice.dueDate'),
    subtotal: required(invoice.subtotal, 'invoice.subtotal'),
    gstAmount: required(invoice.gstAmount, 'invoice.gstAmount'),
    totalAmount: required(invoice.totalAmount, 'invoice.totalAmount'),
    paidAmount: required(invoice.paidAmount, 'invoice.paidAmount'),
    outstandingAmount: required(invoice.outstandingAmount, 'invoice.outstandingAmount'),
    status: required(invoice.status, 'invoice.status'),
    emailStatus: invoice.emailStatus,
    items: (invoice.items ?? []).map(parseInvoiceItem),
    payments: invoice.payments ? invoice.payments.map(parsePaymentRecord) : undefined,
  };
}

function parseInvoiceItem(item: ApiInvoiceItem | Invoice['items'][number]): Invoice['items'][number] {
  return {
    id: required(item.id, 'invoiceItem.id'),
    description: required(item.description, 'invoiceItem.description'),
    quantity: required(item.quantity, 'invoiceItem.quantity'),
    unitPrice: required(item.unitPrice, 'invoiceItem.unitPrice'),
    lineTotal: required(item.lineTotal, 'invoiceItem.lineTotal'),
  };
}

function parsePaymentRecord(payment: ApiPaymentRecord | PaymentRecord): PaymentRecord {
  return {
    id: required(payment.id, 'payment.id'),
    invoiceId: required(payment.invoiceId, 'payment.invoiceId'),
    amount: required(payment.amount, 'payment.amount'),
    paymentDate: parseDate(payment.paymentDate, 'payment.paymentDate'),
    paymentMethod: required(payment.paymentMethod, 'payment.paymentMethod'),
    reference: required(payment.reference, 'payment.reference'),
    markedByUserId: required(payment.markedByUserId, 'payment.markedByUserId'),
    note: payment.note ?? undefined,
    isVoided: required(payment.isVoided, 'payment.isVoided'),
    voidedAt: parseOptionalDate(payment.voidedAt),
    voidedByUserId: payment.voidedByUserId ?? undefined,
    voidReason: payment.voidReason ?? undefined,
  };
}

function parseStatement(statement: ApiStatement | Statement): Statement {
  return {
    id: required(statement.id, 'statement.id'),
    statementNumber: required(statement.statementNumber, 'statement.statementNumber'),
    customerId: required(statement.customerId, 'statement.customerId'),
    customer: statement.customer ? parseCustomer(statement.customer) : undefined,
    statementDate: parseDate(statement.statementDate, 'statement.statementDate'),
    periodStart: parseOptionalDate(statement.periodStart),
    periodEnd: parseOptionalDate(statement.periodEnd),
    totalOutstanding: required(statement.totalOutstanding, 'statement.totalOutstanding'),
    status: required(statement.status, 'statement.status'),
    emailStatus: required(statement.emailStatus, 'statement.emailStatus'),
    invoices: parseInvoices(statement.invoices ?? []),
  };
}

function parseProductionItem(item: ApiProductionItem | ProductionItem): ProductionItem {
  return {
    id: required(item.id, 'productionItem.id'),
    productionBatchId: required(item.productionBatchId, 'productionItem.productionBatchId'),
    productId: required(item.productId, 'productionItem.productId'),
    productName: required(item.productName, 'productionItem.productName'),
    sku: required(item.sku, 'productionItem.sku'),
    totalQuantity: required(item.totalQuantity, 'productionItem.totalQuantity'),
    producedQuantity: required(item.producedQuantity, 'productionItem.producedQuantity'),
    status: required(item.status, 'productionItem.status'),
    orderIds: item.orderIds ?? [],
    orderNumbers: item.orderNumbers ?? [],
  };
}

function parseProductionBatch(batch: ApiProductionBatch | ProductionBatch): ProductionBatch {
  return {
    id: required(batch.id, 'productionBatch.id'),
    batchNumber: required(batch.batchNumber, 'productionBatch.batchNumber'),
    productionPeriod: required(batch.productionPeriod, 'productionBatch.productionPeriod'),
    status: required(batch.status, 'productionBatch.status'),
    createdAt: parseDate(batch.createdAt, 'productionBatch.createdAt'),
    updatedAt: parseDate(batch.updatedAt, 'productionBatch.updatedAt'),
  };
}

function parseAuditLog(log: ApiAuditLog | AuditLog): AuditLog {
  return {
    id: required(log.id, 'auditLog.id'),
    actorUserId: log.actorUserId ?? undefined,
    actorRole: log.actorRole ?? undefined,
    action: required(log.action, 'auditLog.action'),
    entityType: required(log.entityType, 'auditLog.entityType'),
    entityId: log.entityId ?? undefined,
    message: required(log.message, 'auditLog.message'),
    oldValues: log.oldValues ?? undefined,
    newValues: log.newValues ?? undefined,
    createdAt: parseDate(log.createdAt, 'auditLog.createdAt'),
  };
}

function parseEmailLog(log: ApiEmailLog | EmailLog): EmailLog {
  const providerFields = log as ApiEmailLog & Partial<EmailLog>;
  return {
    id: required(log.id, 'emailLog.id'),
    relatedEntityType: required(log.relatedEntityType, 'emailLog.relatedEntityType'),
    relatedEntityId: required(log.relatedEntityId, 'emailLog.relatedEntityId'),
    recipientEmail: required(log.recipientEmail, 'emailLog.recipientEmail'),
    subject: required(log.subject, 'emailLog.subject'),
    status: required(log.status, 'emailLog.status'),
    provider: providerFields.provider ?? undefined,
    providerMessageId: providerFields.providerMessageId ?? undefined,
    lastProviderEventType: providerFields.lastProviderEventType ?? undefined,
    lastProviderEventAt: parseOptionalDate(providerFields.lastProviderEventAt),
    errorMessage: log.errorMessage ?? undefined,
    createdAt: parseDate(log.createdAt, 'emailLog.createdAt'),
    sentAt: parseOptionalDate(log.sentAt),
  };
}

function parseAdminDashboard(dashboard: ApiAdminDashboard | AdminDashboard): AdminDashboard {
  const metrics = required(dashboard.metrics, 'adminDashboard.metrics');
  return {
    metrics: {
      ordersThisWeek: required(metrics.ordersThisWeek, 'adminDashboard.metrics.ordersThisWeek'),
      inProductionOrders: required(metrics.inProductionOrders, 'adminDashboard.metrics.inProductionOrders'),
      shippedThisWeek: required(metrics.shippedThisWeek, 'adminDashboard.metrics.shippedThisWeek'),
      unpaidInvoiceCount: required(metrics.unpaidInvoiceCount, 'adminDashboard.metrics.unpaidInvoiceCount'),
      overdueInvoiceCount: required(metrics.overdueInvoiceCount, 'adminDashboard.metrics.overdueInvoiceCount'),
      activeCustomerCount: required(metrics.activeCustomerCount, 'adminDashboard.metrics.activeCustomerCount'),
      totalCustomerCount: required(metrics.totalCustomerCount, 'adminDashboard.metrics.totalCustomerCount'),
      totalOutstanding: required(metrics.totalOutstanding, 'adminDashboard.metrics.totalOutstanding'),
    },
    recentOrders: parseOrders(dashboard.recentOrders ?? []),
    overdueInvoices: parseInvoices(dashboard.overdueInvoices ?? []),
  };
}

function parseCustomerDashboard(dashboard: ApiCustomerDashboard | CustomerDashboard): CustomerDashboard {
  const metrics = required(dashboard.metrics, 'customerDashboard.metrics');
  return {
    metrics: {
      openInvoiceCount: required(metrics.openInvoiceCount, 'customerDashboard.metrics.openInvoiceCount'),
      overdueInvoiceCount: required(metrics.overdueInvoiceCount, 'customerDashboard.metrics.overdueInvoiceCount'),
      totalOutstanding: required(metrics.totalOutstanding, 'customerDashboard.metrics.totalOutstanding'),
      estimatedStandingOrderTotal: required(metrics.estimatedStandingOrderTotal, 'customerDashboard.metrics.estimatedStandingOrderTotal'),
    },
    standingOrder: dashboard.standingOrder ? parseStandingOrder(dashboard.standingOrder) : undefined,
    recentInvoices: parseInvoices(dashboard.recentInvoices ?? []),
  };
}

function parsePdfDownload(metadata: ApiSchemas['PdfDownloadDto']): PdfDownloadResponse {
  return {
    downloadUrl: required(metadata.downloadUrl, 'pdfDownload.downloadUrl'),
    fileName: required(metadata.fileName, 'pdfDownload.fileName'),
    fileKey: required(metadata.fileKey, 'pdfDownload.fileKey'),
    generatedAt: required(metadata.generatedAt, 'pdfDownload.generatedAt'),
  };
}

function required<T>(value: T | null | undefined, field: string): T {
  if (value === null || value === undefined) {
    throw new Error(`Missing API field: ${field}`);
  }
  return value;
}

function parseDate(value: string | Date | null | undefined, field: string): Date {
  const parsed = required(value, field);
  const date = parsed instanceof Date ? parsed : new Date(parsed);
  if (Number.isNaN(date.getTime())) {
    throw new Error(`Invalid API date field: ${field}`);
  }
  return date;
}

function parseOptionalDate(value: string | Date | null | undefined): Date | undefined {
  if (value === null || value === undefined || value === '') {
    return undefined;
  }
  return parseDate(value, 'optionalDate');
}
