import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/shared/api/queryKeys';
import { getAdminProducts, getCustomerProducts, getProducts } from '@/entities/product/api/productApi';

export function useAdminProductsQuery() {
  return useQuery({
    queryKey: queryKeys.adminProducts,
    queryFn: getAdminProducts,
  });
}

export function useProductsQuery() {
  return useQuery({
    queryKey: queryKeys.adminProducts,
    queryFn: getProducts,
  });
}

export function useCustomerProductsQuery() {
  return useQuery({
    queryKey: queryKeys.customerProducts,
    queryFn: getCustomerProducts,
  });
}
