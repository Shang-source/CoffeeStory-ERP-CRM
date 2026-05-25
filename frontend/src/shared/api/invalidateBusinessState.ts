import type { QueryClient, QueryKey } from '@tanstack/react-query';
import { queryKeys } from './queryKeys';

const orderStateKeys: QueryKey[] = [
  queryKeys.adminOrders,
  queryKeys.customerOrders,
  queryKeys.production,
  queryKeys.adminInvoices,
  queryKeys.customerInvoices,
  queryKeys.adminStatements,
  queryKeys.customerStatements,
  queryKeys.adminDashboard,
  queryKeys.customerDashboard,
  queryKeys.adminCustomers,
];

const invoiceStateKeys: QueryKey[] = [
  queryKeys.adminInvoices,
  queryKeys.customerInvoices,
  queryKeys.adminOrders,
  queryKeys.customerOrders,
  queryKeys.adminStatements,
  queryKeys.customerStatements,
  queryKeys.adminDashboard,
  queryKeys.customerDashboard,
  queryKeys.adminCustomers,
];

const statementStateKeys: QueryKey[] = [
  queryKeys.adminStatements,
  queryKeys.customerStatements,
  queryKeys.adminDashboard,
  queryKeys.customerDashboard,
  queryKeys.auditLogs,
  queryKeys.emailLogs,
];

const productionStateKeys: QueryKey[] = [
  queryKeys.production,
  queryKeys.adminOrders,
  queryKeys.customerOrders,
  queryKeys.adminDashboard,
  queryKeys.customerDashboard,
];

export function invalidateBusinessState(queryClient: QueryClient) {
  return invalidateQueryKeys(queryClient, [
    ...orderStateKeys,
    ...invoiceStateKeys,
    ...statementStateKeys,
    ...productionStateKeys,
    queryKeys.adminStandingOrders,
    queryKeys.customerStandingOrder,
    queryKeys.auditLogs,
    queryKeys.emailLogs,
  ]);
}

export function invalidateOrderState(queryClient: QueryClient) {
  return invalidateQueryKeys(queryClient, orderStateKeys);
}

export function invalidateInvoiceState(queryClient: QueryClient) {
  return invalidateQueryKeys(queryClient, invoiceStateKeys);
}

export function invalidateStatementState(queryClient: QueryClient) {
  return invalidateQueryKeys(queryClient, statementStateKeys);
}

export function invalidateProductionState(queryClient: QueryClient) {
  return invalidateQueryKeys(queryClient, productionStateKeys);
}

async function invalidateQueryKeys(queryClient: QueryClient, keys: QueryKey[]) {
  const uniqueKeys = new Map(keys.map((key) => [JSON.stringify(key), key]));
  await Promise.all(
    [...uniqueKeys.values()].map((queryKey) =>
      queryClient.invalidateQueries({ queryKey })
    )
  );
}
