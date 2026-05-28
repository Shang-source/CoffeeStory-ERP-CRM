// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { AuditLog, EmailLog, PagedResult } from '@/entities/types';
import LogsPage from './LogsPage';

const useAuditLogsQueryMock = vi.fn();
const useEmailLogsQueryMock = vi.fn();

vi.mock('@/entities/auditLog/api/auditLogQueries', () => ({
  useAuditLogsQuery: (...args: unknown[]) => useAuditLogsQueryMock(...args),
}));

vi.mock('@/entities/emailLog/api/emailLogQueries', () => ({
  useEmailLogsQuery: (...args: unknown[]) => useEmailLogsQueryMock(...args),
}));

vi.mock('@/entities/auditLog/api/auditLogApi', () => ({
  exportAuditLogs: vi.fn(),
}));

vi.mock('@/entities/emailLog/api/emailLogApi', () => ({
  exportEmailLogs: vi.fn(),
}));

vi.mock('sonner', () => ({
  toast: {
    error: vi.fn(),
    success: vi.fn(),
  },
}));

describe('LogsPage', () => {
  afterEach(() => {
    cleanup();
  });

  beforeEach(() => {
    useAuditLogsQueryMock.mockReset();
    useEmailLogsQueryMock.mockReset();
  });

  it('renders audit logs without crashing when change values are objects or dates are missing', async () => {
    useAuditLogsQueryMock.mockReturnValue({
      data: pagedResult<AuditLog>([{
        id: 'audit-1',
        action: 'CreatedCustomer',
        entityType: 'Customer',
        message: 'Created customer Nora Fish',
        newValues: { businessName: 'Nora Fish' } as unknown as string,
      }]),
      isLoading: false,
      error: null,
    });
    useEmailLogsQueryMock.mockReturnValue({
      data: pagedResult<EmailLog>([]),
      isLoading: false,
      error: null,
    });

    render(<LogsPage />);

    expect(await screen.findByText('CreatedCustomer')).toBeInTheDocument();
    expect(screen.getByText('Created customer Nora Fish')).toBeInTheDocument();
    expect(screen.getByText(/"businessName": "Nora Fish"/)).toBeInTheDocument();
    expect(screen.getByText('N/A')).toBeInTheDocument();
  });

  it('renders email logs with optional provider dates safely', async () => {
    useAuditLogsQueryMock.mockReturnValue({
      data: pagedResult<AuditLog>([]),
      isLoading: false,
      error: null,
    });
    useEmailLogsQueryMock.mockReturnValue({
      data: pagedResult<EmailLog>([{
        id: 'email-1',
        relatedEntityType: 'Customer',
        relatedEntityId: 'customer-1',
        recipientEmail: 'nora@example.com',
        subject: 'Your StoryCoffee invite',
        status: 'Failed',
        provider: 'Resend',
      }]),
      isLoading: false,
      error: null,
    });

    render(<LogsPage />);
    fireEvent.click(screen.getByRole('tab', { name: 'Email Logs' }));

    expect(await screen.findByText('nora@example.com')).toBeInTheDocument();
    expect(screen.getByText('Your StoryCoffee invite')).toBeInTheDocument();
    expect(screen.getByText('Failed')).toBeInTheDocument();
    expect(screen.getByText('Resend')).toBeInTheDocument();
  });
});

function pagedResult<T>(items: T[]): PagedResult<T> {
  return {
    items,
    page: 1,
    pageSize: 25,
    totalCount: items.length,
    totalPages: 1,
  };
}
