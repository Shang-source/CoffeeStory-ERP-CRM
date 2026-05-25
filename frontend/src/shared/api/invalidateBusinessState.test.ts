import { QueryClient } from '@tanstack/react-query';
import { describe, expect, it, vi } from 'vitest';
import { invalidateInvoiceState, invalidateOrderState, invalidateStatementState } from './invalidateBusinessState';
import { queryKeys } from './queryKeys';

describe('business state invalidation helpers', () => {
  it('invalidates order-related pages after order workflow changes', async () => {
    const queryClient = new QueryClient();
    const invalidateQueries = vi.spyOn(queryClient, 'invalidateQueries').mockResolvedValue();

    await invalidateOrderState(queryClient);

    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: queryKeys.adminOrders });
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: queryKeys.customerOrders });
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: queryKeys.production });
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: queryKeys.adminInvoices });
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: queryKeys.adminDashboard });
  });

  it('invalidates invoice-related pages after payment or PDF changes', async () => {
    const queryClient = new QueryClient();
    const invalidateQueries = vi.spyOn(queryClient, 'invalidateQueries').mockResolvedValue();

    await invalidateInvoiceState(queryClient);

    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: queryKeys.adminInvoices });
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: queryKeys.customerInvoices });
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: queryKeys.adminOrders });
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: queryKeys.adminStatements });
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: queryKeys.customerDashboard });
  });

  it('invalidates statement pages and logs after statement actions', async () => {
    const queryClient = new QueryClient();
    const invalidateQueries = vi.spyOn(queryClient, 'invalidateQueries').mockResolvedValue();

    await invalidateStatementState(queryClient);

    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: queryKeys.adminStatements });
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: queryKeys.customerStatements });
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: queryKeys.auditLogs });
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: queryKeys.emailLogs });
  });
});
