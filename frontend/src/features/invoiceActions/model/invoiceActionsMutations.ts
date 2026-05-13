import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { Invoice } from '@/entities/types';
import { queryKeys } from '@/shared/api/queryKeys';
import { markOverdueInvoices, sendInvoiceEmail } from '@/features/invoiceActions/api/invoiceActionsApi';

export function useSendInvoiceEmailMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (invoiceId: string) => sendInvoiceEmail(invoiceId),
    onSuccess: (updatedInvoice) => {
      queryClient.setQueryData<Invoice[]>(queryKeys.adminInvoices, (currentInvoices = []) =>
        currentInvoices.map((invoice) => invoice.id === updatedInvoice.id ? updatedInvoice : invoice)
      );
      toast.success(`Invoice ${updatedInvoice.invoiceNumber} sent to ${updatedInvoice.customer?.email ?? 'customer'}`);
    },
    onError: (err) => toast.error(err instanceof Error ? err.message : 'Unable to send invoice'),
  });
}

export function useMarkOverdueInvoicesMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => markOverdueInvoices(),
    onSuccess: async (result) => {
      await queryClient.invalidateQueries({ queryKey: queryKeys.adminInvoices });
      toast.success(`${result.updatedCount} invoice(s) marked overdue`);
    },
    onError: (err) => toast.error(err instanceof Error ? err.message : 'Unable to mark overdue invoices'),
  });
}
