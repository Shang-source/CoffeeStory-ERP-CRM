import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { StandingOrder } from '@/entities/types';
import { queryKeys } from '@/shared/api/queryKeys';
import { generateStandingOrderNow } from '@/features/standingOrderLifecycle/api/standingOrderLifecycleApi';

interface StandingOrderStatusActionInput {
  action: () => Promise<StandingOrder>;
  successMessage: string;
}

export function useGenerateStandingOrderNowMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (standingOrderId: string) => generateStandingOrderNow(standingOrderId),
    onSuccess: async (order) => {
      toast.success(`Order ${order.orderNumber} generated manually`);
      await queryClient.invalidateQueries({ queryKey: queryKeys.adminStandingOrders });
      await queryClient.invalidateQueries({ queryKey: queryKeys.adminOrders });
    },
    onError: (err) => toast.error(err instanceof Error ? err.message : 'Unable to generate order'),
  });
}

export function useStandingOrderStatusActionMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ action }: StandingOrderStatusActionInput) => action(),
    onSuccess: (updatedOrder, variables) => {
      queryClient.setQueryData<StandingOrder[]>(queryKeys.adminStandingOrders, (currentOrders = []) =>
        currentOrders.map((order) => order.id === updatedOrder.id ? updatedOrder : order)
      );
      toast.success(variables.successMessage);
    },
    onError: (err) => toast.error(err instanceof Error ? err.message : 'Unable to update standing order'),
  });
}
