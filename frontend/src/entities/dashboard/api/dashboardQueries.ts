import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/shared/api/queryKeys';
import { getAdminDashboard, getCustomerDashboard } from '@/entities/dashboard/api/dashboardApi';

export function useAdminDashboardQuery() {
  return useQuery({
    queryKey: queryKeys.adminDashboard,
    queryFn: getAdminDashboard,
  });
}

export function useCustomerDashboardQuery(enabled: boolean) {
  return useQuery({
    queryKey: queryKeys.customerDashboard,
    queryFn: getCustomerDashboard,
    enabled,
  });
}
