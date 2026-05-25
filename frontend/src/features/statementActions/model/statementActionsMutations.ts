import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { Statement } from '@/entities/types';
import { queryKeys } from '@/shared/api/queryKeys';
import { invalidateStatementState } from '@/shared/api/invalidateBusinessState';
import { downloadAdminStatementPdf, downloadCustomerStatementPdf, generateWeeklyStatements, sendStatementEmail } from '@/features/statementActions/api/statementActionsApi';

interface DownloadStatementPdfInput {
  statementId: string;
  statementNumber?: string;
}

export function useGenerateWeeklyStatementsMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => generateWeeklyStatements(),
    onSuccess: async (generated) => {
      const generatedIds = new Set(generated.map(statement => statement.id));
      queryClient.setQueryData<Statement[]>(queryKeys.adminStatements, (current = []) => [
        ...generated,
        ...current.filter(statement => !generatedIds.has(statement.id)),
      ]);
      await invalidateStatementState(queryClient);
      toast.success(`${generated.length} weekly statement${generated.length === 1 ? '' : 's'} generated`);
    },
    onError: (err) => toast.error(err instanceof Error ? err.message : 'Unable to generate statements'),
  });
}

export function useSendStatementEmailMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (statementId: string) => sendStatementEmail(statementId),
    onSuccess: async (updatedStatement) => {
      queryClient.setQueryData<Statement>(queryKeys.adminStatement(updatedStatement.id), updatedStatement);
      queryClient.setQueryData<Statement[]>(queryKeys.adminStatements, (current = []) =>
        current.map(statement => statement.id === updatedStatement.id ? updatedStatement : statement)
      );
      await invalidateStatementState(queryClient);
      toast.success('Statement sent to customer');
    },
    onError: (err) => toast.error(err instanceof Error ? err.message : 'Unable to send statement'),
  });
}

export function useDownloadStatementPdfMutation(scope: 'admin' | 'customer') {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ statementId }: DownloadStatementPdfInput) =>
      scope === 'admin' ? downloadAdminStatementPdf(statementId) : downloadCustomerStatementPdf(statementId),
    onSuccess: async (_, input) => {
      await invalidateStatementState(queryClient);
      toast.success(`Downloading statement${input.statementNumber ? ` ${input.statementNumber}` : ''}`);
    },
    onError: (err) => toast.error(err instanceof Error ? err.message : 'Unable to download statement'),
  });
}
