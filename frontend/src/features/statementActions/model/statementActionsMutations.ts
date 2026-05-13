import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { Statement } from '@/entities/types';
import { queryKeys } from '@/shared/api/queryKeys';
import { generateWeeklyStatements, sendStatementEmail } from '@/features/statementActions/api/statementActionsApi';

export function useGenerateWeeklyStatementsMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => generateWeeklyStatements(),
    onSuccess: (generated) => {
      const generatedIds = new Set(generated.map(statement => statement.id));
      queryClient.setQueryData<Statement[]>(queryKeys.adminStatements, (current = []) => [
        ...generated,
        ...current.filter(statement => !generatedIds.has(statement.id)),
      ]);
      toast.success(`${generated.length} weekly statement${generated.length === 1 ? '' : 's'} generated`);
    },
    onError: (err) => toast.error(err instanceof Error ? err.message : 'Unable to generate statements'),
  });
}

export function useSendStatementEmailMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (statementId: string) => sendStatementEmail(statementId),
    onSuccess: (updatedStatement) => {
      queryClient.setQueryData<Statement>(queryKeys.adminStatement(updatedStatement.id), updatedStatement);
      queryClient.setQueryData<Statement[]>(queryKeys.adminStatements, (current = []) =>
        current.map(statement => statement.id === updatedStatement.id ? updatedStatement : statement)
      );
      toast.success('Statement sent to customer');
    },
    onError: (err) => toast.error(err instanceof Error ? err.message : 'Unable to send statement'),
  });
}
