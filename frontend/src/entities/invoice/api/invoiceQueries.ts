import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/shared/api/queryKeys';
import { getAdminInvoices, getCustomerInvoice, getCustomerInvoices } from '@/entities/invoice/api/invoiceApi';

export function useAdminInvoicesQuery(enabled = true) {
  return useQuery({
    queryKey: queryKeys.adminInvoices,
    queryFn: getAdminInvoices,
    enabled,
  });
}

export function useCustomerInvoicesQuery() {
  return useQuery({
    queryKey: queryKeys.customerInvoices,
    queryFn: getCustomerInvoices,
  });
}

export function useCustomerInvoiceQuery(invoiceId?: string) {
  return useQuery({
    queryKey: queryKeys.customerInvoice(invoiceId ?? ''),
    queryFn: () => getCustomerInvoice(invoiceId!),
    enabled: Boolean(invoiceId),
  });
}
