import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { Order } from '@/entities/types';
import { queryKeys } from '@/shared/api/queryKeys';

interface OrderWorkflowMutationInput {
  action: () => Promise<Order>;
  successMessage: string;
}

export function useOrderWorkflowMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ action }: OrderWorkflowMutationInput) => action(),
    onSuccess: (updatedOrder, variables) => {
      queryClient.setQueryData<Order[]>(queryKeys.adminOrders, (currentOrders = []) =>
        currentOrders.map((order) => order.id === updatedOrder.id ? updatedOrder : order)
      );
      void queryClient.invalidateQueries({ queryKey: queryKeys.adminInvoices });
      toast.success(variables.successMessage);
    },
    onError: (err) => toast.error(err instanceof Error ? err.message : 'Order action failed'),
  });
}
