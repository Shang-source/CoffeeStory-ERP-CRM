import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { Customer } from '@/entities/types';
import { queryKeys } from '@/shared/api/queryKeys';
import { updateAdminCustomer, updateCustomerProfile } from '@/entities/customer/api/customerApi';

export function useUpdateAdminCustomerMutation(customerId?: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (updatedCustomer: Customer) => updateAdminCustomer(customerId!, {
      businessName: updatedCustomer.businessName,
      contactPerson: updatedCustomer.contactPerson,
      email: updatedCustomer.email,
      phone: updatedCustomer.phone,
      billingAddress: updatedCustomer.billingAddress,
      deliveryAddress: updatedCustomer.deliveryAddress,
      paymentTerms: updatedCustomer.paymentTerms,
      accountStatus: updatedCustomer.accountStatus,
    }),
    onSuccess: (savedCustomer) => {
      queryClient.setQueryData<Customer>(queryKeys.adminCustomer(savedCustomer.id), savedCustomer);
      queryClient.setQueryData<Customer[]>(queryKeys.adminCustomers, (current = []) =>
        current.map((item) => item.id === savedCustomer.id ? savedCustomer : item)
      );
    },
  });
}

export function useUpdateCustomerProfileMutation(onUpdated: (customer: Customer) => void) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (customerProfile: Customer) => updateCustomerProfile(customerProfile),
    onSuccess: (updatedCustomer) => {
      queryClient.setQueryData<Customer>(queryKeys.customerProfile, updatedCustomer);
      onUpdated(updatedCustomer);
      toast.success('Account settings updated successfully');
    },
    onError: (err) => toast.error(err instanceof Error ? err.message : 'Unable to update account settings'),
  });
}
