import { useMutation } from '@tanstack/react-query';
import { toast } from 'sonner';
import { changeCustomerPassword, type ChangePasswordInput } from '@/features/passwordChange/api/passwordChangeApi';

export function useChangeCustomerPasswordMutation(onChanged: () => void) {
  return useMutation({
    mutationFn: (passwordChange: ChangePasswordInput) => changeCustomerPassword(passwordChange),
    onSuccess: () => {
      toast.success('Password changed. Please sign in again.');
      onChanged();
    },
    onError: (err) => toast.error(err instanceof Error ? err.message : 'Unable to change password'),
  });
}
