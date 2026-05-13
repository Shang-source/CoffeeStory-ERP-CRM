import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Product } from '@/entities/types';
import { queryKeys } from '@/shared/api/queryKeys';
import { createAdminProduct, type ProductPayload, updateAdminProduct } from '@/features/productCreateEdit/api/productCreateEditApi';

interface SaveAdminProductInput {
  productId?: string;
  product: ProductPayload;
}

export function useSaveAdminProductMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ productId, product }: SaveAdminProductInput) =>
      productId ? updateAdminProduct(productId, product) : createAdminProduct(product),
    onSuccess: (savedProduct) => {
      queryClient.setQueryData<Product[]>(queryKeys.adminProducts, (currentProducts = []) => {
        const exists = currentProducts.some((product) => product.id === savedProduct.id);
        const nextProducts = exists
          ? currentProducts.map(product => product.id === savedProduct.id ? savedProduct : product)
          : [...currentProducts, savedProduct];
        return nextProducts.sort((a, b) => a.name.localeCompare(b.name));
      });
    },
  });
}
