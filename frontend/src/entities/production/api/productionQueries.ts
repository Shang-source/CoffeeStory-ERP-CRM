import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/shared/api/queryKeys';
import { getCurrentProduction } from '@/entities/production/api/productionApi';

export function useCurrentProductionQuery() {
  return useQuery({
    queryKey: queryKeys.production,
    queryFn: getCurrentProduction,
  });
}
