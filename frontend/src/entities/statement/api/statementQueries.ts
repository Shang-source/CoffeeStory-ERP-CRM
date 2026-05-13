import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/shared/api/queryKeys';
import { getAdminStatement } from '@/entities/statement/api/statementApi';

export function useAdminStatementQuery(statementId?: string) {
  return useQuery({
    queryKey: queryKeys.adminStatement(statementId ?? ''),
    queryFn: () => getAdminStatement(statementId!),
    enabled: Boolean(statementId),
  });
}
