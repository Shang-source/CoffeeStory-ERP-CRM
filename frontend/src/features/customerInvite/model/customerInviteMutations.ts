import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { Customer } from '@/entities/types';
import { queryKeys } from '@/shared/api/queryKeys';
import { sendAdminCustomerInvite } from '@/features/customerInvite/api/customerInviteApi';

export function useSendAdminCustomerInviteMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (customerId: string) => sendAdminCustomerInvite(customerId),
    onSuccess: (updatedCustomer) => {
      queryClient.setQueryData<Customer[]>(queryKeys.adminCustomers, (currentCustomers = []) =>
        currentCustomers.map((item) => item.id === updatedCustomer.id ? updatedCustomer : item)
      );
      queryClient.setQueryData<Customer>(queryKeys.adminCustomer(updatedCustomer.id), updatedCustomer);
      toast.success(`Invite sent to ${updatedCustomer.email}`);
    },
    onError: (err) => toast.error(err instanceof Error ? err.message : 'Unable to send invite'),
  });
}
