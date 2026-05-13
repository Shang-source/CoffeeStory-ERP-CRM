import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/shared/api/queryKeys';
import { getAdminStandingOrders, getCustomerStandingOrder } from '@/entities/standingOrder/api/standingOrderApi';

export function useAdminStandingOrdersQuery() {
  return useQuery({
    queryKey: queryKeys.adminStandingOrders,
    queryFn: getAdminStandingOrders,
  });
}

export function useCustomerStandingOrderQuery() {
  return useQuery({
    queryKey: queryKeys.customerStandingOrder,
    queryFn: getCustomerStandingOrder,
  });
}
