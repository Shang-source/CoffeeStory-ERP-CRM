import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Customer } from '@/entities/types';
import { queryKeys } from '@/shared/api/queryKeys';
import { createAdminCustomer, type CustomerPayload } from '@/features/customerCreate/api/customerCreateApi';

export function useCreateAdminCustomerMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (customer: CustomerPayload) => createAdminCustomer(customer),
    onSuccess: (customer) => {
      queryClient.setQueryData<Customer[]>(queryKeys.adminCustomers, (currentCustomers = []) =>
        [...currentCustomers, customer].sort((a, b) => a.businessName.localeCompare(b.businessName))
      );
    },
  });
}
