import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { Invoice } from '@/entities/types';
import { queryKeys } from '@/shared/api/queryKeys';
import { recordInvoicePayment, type RecordPaymentInput, voidInvoicePayment } from '@/features/paymentRecord/api/paymentRecordApi';

interface RecordPaymentMutationInput {
  invoiceId: string;
  payload: RecordPaymentInput;
  amountLabel: string;
}

interface VoidPaymentMutationInput {
  invoiceId: string;
  paymentId: string;
  reason: string;
}

function updateInvoiceCache(invoices: Invoice[] = [], updatedInvoice: Invoice) {
  return invoices.map((invoice) => invoice.id === updatedInvoice.id ? updatedInvoice : invoice);
}

export function useRecordInvoicePaymentMutation(onRecorded?: () => void) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ invoiceId, payload }: RecordPaymentMutationInput) => recordInvoicePayment(invoiceId, payload),
    onSuccess: (updatedInvoice, variables) => {
      queryClient.setQueryData<Invoice[]>(queryKeys.adminInvoices, (currentInvoices = []) =>
        updateInvoiceCache(currentInvoices, updatedInvoice)
      );
      toast.success(`Payment of $${variables.amountLabel} recorded for invoice ${updatedInvoice.invoiceNumber}`);
      onRecorded?.();
    },
    onError: (err) => toast.error(err instanceof Error ? err.message : 'Unable to record payment'),
  });
}

export function useVoidInvoicePaymentMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ invoiceId, paymentId, reason }: VoidPaymentMutationInput) =>
      voidInvoicePayment(invoiceId, paymentId, reason),
    onSuccess: (updatedInvoice) => {
      queryClient.setQueryData<Invoice[]>(queryKeys.adminInvoices, (currentInvoices = []) =>
        updateInvoiceCache(currentInvoices, updatedInvoice)
      );
      toast.success('Payment voided');
    },
    onError: (err) => toast.error(err instanceof Error ? err.message : 'Unable to void payment'),
  });
}
