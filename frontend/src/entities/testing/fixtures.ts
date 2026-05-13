import type { Customer, CustomerPriceBook, CustomerProduct, Invoice, Order, Product, ProductionBatch, StandingOrder, Statement } from '@/entities/types';
import type { UserProfile } from '@/entities/user/model/authTypes';

export const adminProfile: UserProfile = {
  id: 'admin-1',
  email: 'admin@storycoffee.co.nz',
  role: 'Admin',
  name: 'StoryCoffee Admin',
};

export const customerProfile: UserProfile = {
  id: 'user-1',
  email: 'john@aucklandcafe.co.nz',
  role: 'Customer',
  customerId: 'customer-1',
  name: 'John Smith',
};

export function makeCustomer(overrides: Partial<Customer> = {}): Customer {
  return {
    id: 'customer-1',
    businessName: 'Auckland Cafe',
    contactPerson: 'John Smith',
    email: 'john@aucklandcafe.co.nz',
    phone: '021 000 000',
    billingAddress: '1 Queen Street, Auckland',
    deliveryAddress: '1 Queen Street, Auckland',
    paymentTerms: 'Net 14',
    accountStatus: 'Active',
    createdAt: new Date('2026-05-01T00:00:00Z'),
    ...overrides,
  };
}

export function makeProduct(overrides: Partial<Product> = {}): Product {
  return {
    id: 'product-1',
    sku: 'HB-1KG',
    name: 'House Blend 1kg',
    description: 'House blend coffee',
    unit: 'kg',
    price: 38,
    cost: 18,
    isActive: true,
    ...overrides,
  };
}

export function makeCustomerProduct(overrides: Partial<CustomerProduct> = {}): CustomerProduct {
  return {
    ...makeProduct(),
    basePrice: 38,
    effectivePrice: 35,
    hasOverride: true,
    price: 35,
    ...overrides,
  };
}

export function makeStandingOrder(overrides: Partial<StandingOrder> = {}): StandingOrder {
  const product = makeProduct();
  return {
    id: 'standing-order-1',
    customerId: 'customer-1',
    customer: makeCustomer(),
    frequency: 'Weekly',
    nextClosingDate: new Date('2026-05-18T00:00:00Z'),
    status: 'Active',
    deliveryNotes: 'Deliver Monday morning',
    items: [{
      id: 'standing-order-item-1',
      productId: product.id,
      product,
      quantity: 2,
      unitPrice: 38,
    }],
    ...overrides,
  };
}

export function makeOrder(overrides: Partial<Order> = {}): Order {
  const customer = makeCustomer();
  const product = makeProduct();
  return {
    id: 'order-1',
    orderNumber: 'ORD-1001',
    customerId: customer.id,
    customer,
    standingOrderId: 'standing-order-1',
    generatedAt: new Date('2026-05-10T00:00:00Z'),
    orderStatus: 'Generated',
    invoiceStatus: 'NotIssued',
    shipmentStatus: 'NotShipped',
    subtotal: 76,
    gstAmount: 11.4,
    totalAmount: 87.4,
    items: [{
      id: 'order-item-1',
      productId: product.id,
      productNameSnapshot: product.name,
      skuSnapshot: product.sku,
      quantity: 2,
      unitPriceSnapshot: 38,
      lineTotal: 76,
    }],
    ...overrides,
  };
}

export function makeProductionBatch(overrides: Partial<ProductionBatch> = {}): ProductionBatch {
  return {
    id: 'production-batch-1',
    batchNumber: 'PB-1001',
    productionPeriod: '2026-W20',
    status: 'Open',
    createdAt: new Date('2026-05-10T00:00:00Z'),
    updatedAt: new Date('2026-05-10T00:00:00Z'),
    ...overrides,
  };
}

export function makeInvoice(overrides: Partial<Invoice> = {}): Invoice {
  const customer = makeCustomer();
  return {
    id: 'invoice-1',
    invoiceNumber: 'INV-1001',
    customerId: customer.id,
    customer,
    orderId: 'order-1',
    issueDate: new Date('2026-05-10T00:00:00Z'),
    dueDate: new Date('2026-05-24T00:00:00Z'),
    subtotal: 76,
    gstAmount: 11.4,
    totalAmount: 87.4,
    paidAmount: 0,
    outstandingAmount: 87.4,
    status: 'Unpaid',
    emailStatus: 'Sent',
    items: [{
      id: 'invoice-item-1',
      description: 'House Blend 1kg',
      quantity: 2,
      unitPrice: 38,
      lineTotal: 76,
    }],
    payments: [],
    ...overrides,
  };
}

export function makeStatement(overrides: Partial<Statement> = {}): Statement {
  const customer = makeCustomer();
  const invoice = makeInvoice({ customer, customerId: customer.id });
  return {
    id: 'statement-1',
    statementNumber: 'STMT-1001',
    customerId: customer.id,
    customer,
    statementDate: new Date('2026-05-25T00:00:00Z'),
    periodStart: new Date('2026-05-10T00:00:00Z'),
    periodEnd: new Date('2026-05-25T00:00:00Z'),
    totalOutstanding: invoice.outstandingAmount,
    status: 'ReadyToSend',
    emailStatus: 'NotSent',
    invoices: [invoice],
    ...overrides,
  };
}

export function makeCustomerPriceBook(overrides: Partial<CustomerPriceBook> = {}): CustomerPriceBook {
  return {
    customerId: 'customer-1',
    items: [{
      productId: 'product-1',
      sku: 'HB-1KG',
      name: 'House Blend 1kg',
      unit: 'kg',
      basePrice: 38,
      effectivePrice: 38,
      hasOverride: false,
      isActive: true,
    }],
    ...overrides,
  };
}
