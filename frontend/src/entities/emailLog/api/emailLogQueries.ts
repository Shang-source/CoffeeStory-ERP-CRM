import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/shared/api/queryKeys';
import { getEmailLogs, type LogQueryParams } from '@/entities/emailLog/api/emailLogApi';

export function useEmailLogsQuery(params: LogQueryParams, enabled: boolean) {
  return useQuery({
    queryKey: queryKeys.emailLogsList(params),
    queryFn: () => getEmailLogs(params),
    enabled,
  });
}
