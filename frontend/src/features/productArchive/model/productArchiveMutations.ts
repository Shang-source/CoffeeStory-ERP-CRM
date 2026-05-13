import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { Product } from '@/entities/types';
import { queryKeys } from '@/shared/api/queryKeys';
import { archiveAdminProduct } from '@/features/productArchive/api/productArchiveApi';

export function useArchiveAdminProductMutation(onArchived?: () => void) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (productId: string) => archiveAdminProduct(productId),
    onSuccess: (archivedProduct) => {
      queryClient.setQueryData<Product[]>(queryKeys.adminProducts, (currentProducts = []) =>
        currentProducts.map((item) => item.id === archivedProduct.id ? archivedProduct : item)
      );
      toast.success(`${archivedProduct.name} archived`);
      onArchived?.();
    },
    onError: (err) => toast.error(err instanceof Error ? err.message : 'Unable to archive product'),
  });
}
