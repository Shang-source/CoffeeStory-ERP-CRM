import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/shared/api/queryKeys';
import { getAdminStatement, getAdminStatements, getCustomerStatement, getCustomerStatements } from '@/entities/statement/api/statementApi';

export function useAdminStatementsQuery() {
  return useQuery({
    queryKey: queryKeys.adminStatements,
    queryFn: getAdminStatements,
  });
}

export function useAdminStatementQuery(statementId?: string) {
  return useQuery({
    queryKey: queryKeys.adminStatement(statementId ?? ''),
    queryFn: () => getAdminStatement(statementId!),
    enabled: Boolean(statementId),
  });
}

export function useCustomerStatementsQuery() {
  return useQuery({
    queryKey: queryKeys.customerStatements,
    queryFn: getCustomerStatements,
  });
}

export function useCustomerStatementQuery(statementId?: string) {
  return useQuery({
    queryKey: queryKeys.customerStatement(statementId ?? ''),
    queryFn: () => getCustomerStatement(statementId!),
    enabled: Boolean(statementId),
  });
}
