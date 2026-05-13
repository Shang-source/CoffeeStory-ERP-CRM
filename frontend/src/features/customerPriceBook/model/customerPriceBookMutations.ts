import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { CustomerPriceBookItem, StandingOrder } from '@/entities/types';
import { queryKeys } from '@/shared/api/queryKeys';
import { updateAdminCustomerPriceBook } from '@/features/customerPriceBook/api/customerPriceBookApi';

interface SaveCustomerPriceBookInput {
  customerId: string;
  items: CustomerPriceBookItem[];
}

export function useSaveCustomerPriceBookMutation(onSaved: (items: CustomerPriceBookItem[]) => void) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ customerId, items }: SaveCustomerPriceBookInput) => updateAdminCustomerPriceBook(customerId, {
      items: items.map((item) => ({
        productId: item.productId,
        overridePrice: item.overridePrice ?? null,
        isActive: item.isActive,
        notes: item.notes ?? null,
      })),
    }),
    onSuccess: (saved) => {
      onSaved(saved.items);
      queryClient.setQueryData(queryKeys.adminCustomerPriceBook(saved.customerId), saved);
      queryClient.setQueryData<StandingOrder[]>(queryKeys.adminStandingOrders, (current = []) =>
        current.map((order) => {
          if (order.customerId !== saved.customerId) {
            return order;
          }

          return {
            ...order,
            items: order.items.map((item) => {
              const priceBookItem = saved.items.find((price) => price.productId === item.productId);
              return priceBookItem ? { ...item, unitPrice: priceBookItem.effectivePrice } : item;
            }),
          };
        })
      );
      toast.success('Price book saved');
    },
    onError: (err) => toast.error(err instanceof Error ? err.message : 'Unable to save price book'),
  });
}
