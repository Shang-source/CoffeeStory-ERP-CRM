import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/shared/api/queryKeys';
import { getAdminStandingOrders, getCustomerStandingOrder } from '@/entities/standingOrder/api/standingOrderApi';
import { ApiError } from '@/shared/api/apiError';

export function useAdminStandingOrdersQuery() {
  return useQuery({
    queryKey: queryKeys.adminStandingOrders,
    queryFn: getAdminStandingOrders,
  });
}

export function useCustomerStandingOrderQuery() {
  return useQuery({
    queryKey: queryKeys.customerStandingOrder,
    queryFn: async () => {
      try {
        return await getCustomerStandingOrder();
      } catch (error) {
        if (error instanceof ApiError && error.status === 404 && error.code === 'STANDING_ORDER_NOT_FOUND') {
          return null;
        }

        throw error;
      }
    },
  });
}
