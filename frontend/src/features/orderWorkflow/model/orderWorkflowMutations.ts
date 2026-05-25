import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { Order } from '@/entities/types';
import { queryKeys } from '@/shared/api/queryKeys';
import { invalidateOrderState } from '@/shared/api/invalidateBusinessState';
import { batchShipAndInvoiceOrders } from '@/features/orderWorkflow/api/orderWorkflowApi';

interface OrderWorkflowMutationInput {
  action: () => Promise<Order>;
  successMessage: string;
}

export function useBatchShipAndInvoiceMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (orderIds: string[]) => batchShipAndInvoiceOrders(orderIds),
    onSuccess: async (result) => {
      queryClient.setQueryData<Order[]>(queryKeys.adminOrders, (currentOrders = []) =>
        currentOrders.map((order) => result.orders.find((updated) => updated.id === order.id) ?? order)
      );
      await invalidateOrderState(queryClient);
      const failureText = result.emailFailures.length > 0 ? ` ${result.emailFailures.length} email issue(s) need review.` : '';
      toast.success(`${result.updated} order${result.updated === 1 ? '' : 's'} shipped and invoiced.${failureText}`, {
        description: `${result.invoiceEmailsSent} invoice email(s), ${result.statementEmailsSent} statement email(s) sent.`,
      });
    },
    onError: (err) => toast.error(err instanceof Error ? err.message : 'Ship and invoice failed'),
  });
}

export function useOrderWorkflowMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ action }: OrderWorkflowMutationInput) => action(),
    onSuccess: async (updatedOrder, variables) => {
      queryClient.setQueryData<Order[]>(queryKeys.adminOrders, (currentOrders = []) =>
        currentOrders.map((order) => order.id === updatedOrder.id ? updatedOrder : order)
      );
      await invalidateOrderState(queryClient);
      toast.success(variables.successMessage);
    },
    onError: (err) => toast.error(err instanceof Error ? err.message : 'Order action failed'),
  });
}
