import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/shared/api/queryKeys';
import { getAdminCustomer, getAdminCustomers, getCustomerProfile } from '@/entities/customer/api/customerApi';

export function useAdminCustomersQuery() {
  return useQuery({
    queryKey: queryKeys.adminCustomers,
    queryFn: getAdminCustomers,
  });
}

export function useAdminCustomerQuery(customerId?: string) {
  return useQuery({
    queryKey: queryKeys.adminCustomer(customerId ?? ''),
    queryFn: () => getAdminCustomer(customerId!),
    enabled: Boolean(customerId),
  });
}

export function useCustomerProfileQuery(enabled: boolean) {
  return useQuery({
    queryKey: queryKeys.customerProfile,
    queryFn: getCustomerProfile,
    enabled,
  });
}
