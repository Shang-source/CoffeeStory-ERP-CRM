interface CustomerScoped {
  customerId: string;
}

export function filterOrdersByCustomer<T extends CustomerScoped>(orders: T[], customerId: string): T[] {
  return orders.filter(order => order.customerId === customerId);
}

export function filterInvoicesByCustomer<T extends CustomerScoped>(invoices: T[], customerId: string): T[] {
  return invoices.filter(invoice => invoice.customerId === customerId);
}

export function filterStatementsByCustomer<T extends CustomerScoped>(statements: T[], customerId: string): T[] {
  return statements.filter(statement => statement.customerId === customerId);
}

export function filterStandingOrdersByCustomer<T extends CustomerScoped>(standingOrders: T[], customerId: string): T[] {
  return standingOrders.filter(so => so.customerId === customerId);
}
