import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { StandingOrder } from '@/entities/types';
import { queryKeys } from '@/shared/api/queryKeys';
import {
  createAdminStandingOrder,
  type StandingOrderPayload,
  updateAdminStandingOrder,
  updateCustomerStandingOrder,
} from '@/features/standingOrderEditor/api/standingOrderEditorApi';

interface SaveAdminStandingOrderInput {
  standingOrderId?: string;
  payload: StandingOrderPayload;
  isEditing: boolean;
}

export function useSaveAdminStandingOrderMutation(onSaved?: () => void) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ standingOrderId, payload }: SaveAdminStandingOrderInput) =>
      standingOrderId ? updateAdminStandingOrder(standingOrderId, payload) : createAdminStandingOrder(payload),
    onSuccess: (saved, variables) => {
      queryClient.setQueryData<StandingOrder[]>(queryKeys.adminStandingOrders, (currentOrders = []) => {
        const exists = currentOrders.some((order) => order.id === saved.id);
        return exists
          ? currentOrders.map((order) => order.id === saved.id ? saved : order)
          : [...currentOrders, saved].sort((left, right) => (left.customer?.businessName ?? '').localeCompare(right.customer?.businessName ?? ''));
      });
      toast.success(variables.isEditing ? 'Standing order updated' : 'Standing order created');
      onSaved?.();
    },
    onError: (err) => toast.error(err instanceof Error ? err.message : 'Unable to save standing order'),
  });
}

export function useSaveCustomerStandingOrderMutation(onSaved: (standingOrder: StandingOrder) => void) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (standingOrder: StandingOrder) => updateCustomerStandingOrder(standingOrder),
    onSuccess: (updated) => {
      queryClient.setQueryData<StandingOrder | null>(queryKeys.customerStandingOrder, updated);
      queryClient.invalidateQueries({ queryKey: queryKeys.customerDashboard });
      onSaved(updated);
      toast.success('Standing order updated successfully');
    },
    onError: (err) => toast.error(err instanceof Error ? err.message : 'Unable to save standing order'),
  });
}
