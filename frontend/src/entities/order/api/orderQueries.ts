import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/shared/api/queryKeys';
import { getAdminOrders, getCustomerOrders } from '@/entities/order/api/orderApi';
import { OrderQueryParams } from '@/entities/types';

export function useAdminOrdersQuery(params: OrderQueryParams = {}) {
  return useQuery({
    queryKey: queryKeys.adminOrdersList(params),
    queryFn: () => getAdminOrders(params),
  });
}

export function useCustomerOrdersQuery() {
  return useQuery({
    queryKey: queryKeys.customerOrders,
    queryFn: getCustomerOrders,
  });
}
