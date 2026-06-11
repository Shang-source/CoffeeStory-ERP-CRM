import { describe, expect, it } from 'vitest';
import { parseCustomerPriceBookResponse, parseCustomerProductResponse, parseInvoiceResponse, parseOrderResponse, parseStandingOrderResponse, parseStatementResponse } from './contractClient';
import { CustomerPriceBook, CustomerProduct, Invoice, Order, StandingOrder, Statement } from '@/entities/types';

describe('parseOrderResponse', () => {
  it('converts API date strings into Date values', () => {
    const order = parseOrderResponse({
      id: 'order-1',
      orderNumber: 'ORD-1',
      customerId: 'customer-1',
      customer: {
        id: 'customer-1',
        accountNumber: '301',
        businessName: 'Auckland Cafe',
        contactPerson: 'John Smith',
        email: 'john@aucklandcafe.co.nz',
        phone: '',
        billingAddress: '',
        deliveryAddress: '',
        paymentTerms: 'Net 14',
        accountStatus: 'Active',
        hasPortalUser: true,
        createdAt: '2026-05-01T00:00:00Z' as unknown as Date,
      },
      standingOrderId: 'standing-order-1',
      generatedAt: '2026-05-10T00:00:00Z' as unknown as Date,
      orderStatus: 'Generated',
      invoiceStatus: 'NotIssued',
      shipmentStatus: 'NotShipped',
      subtotal: 100,
      gstAmount: 15,
      totalAmount: 115,
      items: [],
    } satisfies Order);

    expect(order.generatedAt).toBeInstanceOf(Date);
    expect(order.customer?.createdAt).toBeInstanceOf(Date);
  });
});

describe('parseStatementResponse', () => {
  it('converts statement and nested invoice API dates into Date values', () => {
    const statement = parseStatementResponse({
      id: 'statement-1',
      statementNumber: 'STMT-1',
      customerId: 'customer-1',
      customer: {
        id: 'customer-1',
        accountNumber: '301',
        businessName: 'Auckland Cafe',
        contactPerson: 'John Smith',
        email: 'john@aucklandcafe.co.nz',
        phone: '',
        billingAddress: '',
        deliveryAddress: '',
        paymentTerms: 'Net 14',
        accountStatus: 'Active',
        hasPortalUser: true,
        createdAt: '2026-05-01T00:00:00Z' as unknown as Date,
      },
      statementDate: '2026-05-25T00:00:00Z' as unknown as Date,
      periodStart: '2026-05-01T00:00:00Z' as unknown as Date,
      periodEnd: '2026-05-25T00:00:00Z' as unknown as Date,
      totalOutstanding: 115,
      status: 'ReadyToSend',
      emailStatus: 'NotSent',
      invoices: [{
        id: 'invoice-1',
        invoiceNumber: 'INV-1',
        customerId: 'customer-1',
        orderId: 'order-1',
        issueDate: '2026-05-10T00:00:00Z' as unknown as Date,
        dueDate: '2026-05-24T00:00:00Z' as unknown as Date,
        subtotal: 100,
        gstAmount: 15,
        totalAmount: 115,
        paidAmount: 0,
        outstandingAmount: 115,
        status: 'Unpaid',
        items: [],
      }],
    } satisfies Statement);

    expect(statement.statementDate).toBeInstanceOf(Date);
    expect(statement.periodStart).toBeInstanceOf(Date);
    expect(statement.periodEnd).toBeInstanceOf(Date);
    expect(statement.invoices[0].issueDate).toBeInstanceOf(Date);
  });
});

describe('parseStandingOrderResponse', () => {
  it('converts standing order and customer API dates into Date values', () => {
    const standingOrder = parseStandingOrderResponse({
      id: 'standing-order-1',
      customerId: 'customer-1',
      customer: {
        id: 'customer-1',
        accountNumber: '301',
        businessName: 'Auckland Cafe',
        contactPerson: 'John Smith',
        email: 'john@aucklandcafe.co.nz',
        phone: '',
        billingAddress: '',
        deliveryAddress: '',
        paymentTerms: 'Net 14',
        accountStatus: 'Active',
        hasPortalUser: true,
        createdAt: '2026-05-01T00:00:00Z' as unknown as Date,
      },
      frequency: 'Weekly',
      nextClosingDate: '2026-05-18T00:00:00Z' as unknown as Date,
      status: 'Active',
      deliveryNotes: 'Deliver every Monday morning',
      items: [],
    } satisfies StandingOrder);

    expect(standingOrder.nextClosingDate).toBeInstanceOf(Date);
    expect(standingOrder.customer?.createdAt).toBeInstanceOf(Date);
  });
});

describe('parseInvoiceResponse', () => {
  it('converts invoice API date strings into Date values', () => {
    const invoice = parseInvoiceResponse({
      id: 'invoice-1',
      invoiceNumber: 'INV-1',
      customerId: 'customer-1',
      customer: {
        id: 'customer-1',
        accountNumber: '301',
        businessName: 'Auckland Cafe',
        contactPerson: 'John Smith',
        email: 'john@aucklandcafe.co.nz',
        phone: '',
        billingAddress: '',
        deliveryAddress: '',
        paymentTerms: 'Net 14',
        accountStatus: 'Active',
        hasPortalUser: true,
        createdAt: '2026-05-01T00:00:00Z' as unknown as Date,
      },
      orderId: 'order-1',
      issueDate: '2026-05-10T00:00:00Z' as unknown as Date,
      dueDate: '2026-05-24T00:00:00Z' as unknown as Date,
      subtotal: 100,
      gstAmount: 15,
      totalAmount: 115,
      paidAmount: 0,
      outstandingAmount: 115,
      status: 'Unpaid',
      items: [],
    } satisfies Invoice);

    expect(invoice.issueDate).toBeInstanceOf(Date);
    expect(invoice.dueDate).toBeInstanceOf(Date);
    expect(invoice.customer?.createdAt).toBeInstanceOf(Date);
  });
});

describe('customer pricing parsers', () => {
  it('maps effective customer product price into the product price used by selectors', () => {
    const product = parseCustomerProductResponse({
      id: 'product-1',
      sku: 'HB-1KG',
      name: 'House Blend 1kg',
      description: 'House blend',
      unit: 'kg',
      price: 35,
      isActive: true,
      basePrice: 38,
      effectivePrice: 35,
      hasOverride: true,
    } satisfies CustomerProduct);

    expect(product.price).toBe(35);
    expect(product.basePrice).toBe(38);
    expect(product.hasOverride).toBe(true);
  });

  it('parses price book items with optional override values', () => {
    const priceBook = parseCustomerPriceBookResponse({
      customerId: 'customer-1',
      items: [{
        productId: 'product-1',
        sku: 'HB-1KG',
        name: 'House Blend 1kg',
        unit: 'kg',
        basePrice: 38,
        overridePrice: undefined,
        effectivePrice: 38,
        hasOverride: false,
        isActive: false,
      }],
    } satisfies CustomerPriceBook);

    expect(priceBook.customerId).toBe('customer-1');
    expect(priceBook.items[0].overridePrice).toBeUndefined();
    expect(priceBook.items[0].effectivePrice).toBe(38);
  });
});
