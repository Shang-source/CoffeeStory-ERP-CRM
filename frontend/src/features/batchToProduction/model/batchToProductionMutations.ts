import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { Order } from '@/entities/types';
import { queryKeys } from '@/shared/api/queryKeys';
import { invalidateOrderState } from '@/shared/api/invalidateBusinessState';
import { batchSendOrdersToProduction } from '@/features/batchToProduction/api/batchToProductionApi';

export function useBatchToProductionMutation(onViewProduction: () => void) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (orderIds: string[]) => batchSendOrdersToProduction(orderIds),
    onSuccess: async (result, orderIds) => {
      const updatedOrders = result.orders;
      queryClient.setQueryData<Order[]>(queryKeys.adminOrders, (currentOrders = []) =>
        currentOrders.map((order) => updatedOrders.find((updated) => updated.id === order.id) ?? order)
      );
      await invalidateOrderState(queryClient);
      toast.success(`${orderIds.length} order${orderIds.length > 1 ? 's' : ''} sent to production successfully`, {
        description: 'These orders have been added to the Production List and are now in production.',
      });
      setTimeout(() => {
        toast.info('View Production List to track progress', {
          action: {
            label: 'Go to Production',
            onClick: onViewProduction,
          },
        });
      }, 1500);
    },
    onError: (err) => toast.error(err instanceof Error ? err.message : 'Batch action failed'),
  });
}
