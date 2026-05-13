import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/shared/api/queryKeys';
import { getAuditLogs, type LogQueryParams } from '@/entities/auditLog/api/auditLogApi';

export function useAuditLogsQuery(params: LogQueryParams, enabled: boolean) {
  return useQuery({
    queryKey: queryKeys.auditLogsList(params),
    queryFn: () => getAuditLogs(params),
    enabled,
  });
}
