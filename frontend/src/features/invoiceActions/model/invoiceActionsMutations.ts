import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { Invoice } from '@/entities/types';
import { queryKeys } from '@/shared/api/queryKeys';
import { invalidateInvoiceState } from '@/shared/api/invalidateBusinessState';
import { downloadAdminInvoicePdf, downloadCustomerInvoicePdf, markOverdueInvoices, sendInvoiceEmail } from '@/features/invoiceActions/api/invoiceActionsApi';

interface DownloadInvoicePdfInput {
  invoiceId: string;
  invoiceNumber?: string;
}

export function useSendInvoiceEmailMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (invoiceId: string) => sendInvoiceEmail(invoiceId),
    onSuccess: async (updatedInvoice) => {
      queryClient.setQueryData<Invoice[]>(queryKeys.adminInvoices, (currentInvoices = []) =>
        currentInvoices.map((invoice) => invoice.id === updatedInvoice.id ? updatedInvoice : invoice)
      );
      await invalidateInvoiceState(queryClient);
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
      await invalidateInvoiceState(queryClient);
      toast.success(`${result.updatedCount} invoice(s) marked overdue`);
    },
    onError: (err) => toast.error(err instanceof Error ? err.message : 'Unable to mark overdue invoices'),
  });
}

export function useDownloadInvoicePdfMutation(scope: 'admin' | 'customer') {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ invoiceId }: DownloadInvoicePdfInput) =>
      scope === 'admin' ? downloadAdminInvoicePdf(invoiceId) : downloadCustomerInvoicePdf(invoiceId),
    onSuccess: async (_, input) => {
      await invalidateInvoiceState(queryClient);
      toast.success(`Downloading invoice${input.invoiceNumber ? ` ${input.invoiceNumber}` : ''}`);
    },
    onError: (err) => toast.error(err instanceof Error ? err.message : 'Unable to download invoice'),
  });
}
