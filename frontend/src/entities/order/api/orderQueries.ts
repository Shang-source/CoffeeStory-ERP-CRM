import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/shared/api/queryKeys';
import { getAdminOrders, getCustomerOrders } from '@/entities/order/api/orderApi';

export function useAdminOrdersQuery() {
  return useQuery({
    queryKey: queryKeys.adminOrders,
    queryFn: getAdminOrders,
  });
}

export function useCustomerOrdersQuery() {
  return useQuery({
    queryKey: queryKeys.customerOrders,
    queryFn: getCustomerOrders,
  });
}
