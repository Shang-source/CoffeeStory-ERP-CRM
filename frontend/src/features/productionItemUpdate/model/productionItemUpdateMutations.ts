import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { ProductionItem } from '@/entities/types';
import { queryKeys } from '@/shared/api/queryKeys';
import { completeProduction, startProduction, updateProducedQuantity } from '@/features/productionItemUpdate/api/productionItemUpdateApi';

interface UpdateProducedQuantityInput {
  productId: string;
  producedQuantity: number;
}

function replaceProductionItem(items: ProductionItem[] = [], updatedItem: ProductionItem) {
  return items.map((item) => item.productId === updatedItem.productId ? updatedItem : item);
}

export function useStartProductionMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (productId: string) => startProduction(productId),
    onSuccess: (updated) => {
      queryClient.setQueryData<ProductionItem[]>(queryKeys.production, (items = []) => replaceProductionItem(items, updated));
      toast.success(`Started production for ${updated.productName}`);
    },
    onError: (err) => toast.error(err instanceof Error ? err.message : 'Unable to start production'),
  });
}

export function useUpdateProducedQuantityMutation(onUpdated?: () => void) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ productId, producedQuantity }: UpdateProducedQuantityInput) =>
      updateProducedQuantity(productId, producedQuantity),
    onSuccess: async (updated) => {
      queryClient.setQueryData<ProductionItem[]>(queryKeys.production, (items = []) => replaceProductionItem(items, updated));
      toast.success(`Updated produced quantity for ${updated.productName}`);
      onUpdated?.();
      await queryClient.invalidateQueries({ queryKey: queryKeys.production });
    },
    onError: (err) => toast.error(err instanceof Error ? err.message : 'Unable to update produced quantity'),
  });
}

export function useCompleteProductionMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (productId: string) => completeProduction(productId),
    onSuccess: async (updated) => {
      queryClient.setQueryData<ProductionItem[]>(queryKeys.production, (items = []) => replaceProductionItem(items, updated));
      toast.success(`${updated.productName} marked as completed`);
      await queryClient.invalidateQueries({ queryKey: queryKeys.production });
      await queryClient.invalidateQueries({ queryKey: queryKeys.adminOrders });
    },
    onError: (err) => toast.error(err instanceof Error ? err.message : 'Unable to complete production item'),
  });
}
