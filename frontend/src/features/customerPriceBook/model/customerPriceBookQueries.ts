import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/shared/api/queryKeys';
import { getAdminCustomerPriceBook } from '@/features/customerPriceBook/api/customerPriceBookApi';

export function useAdminCustomerPriceBookQuery(customerId?: string) {
  return useQuery({
    queryKey: queryKeys.adminCustomerPriceBook(customerId ?? ''),
    queryFn: () => getAdminCustomerPriceBook(customerId!),
    enabled: Boolean(customerId),
  });
}
