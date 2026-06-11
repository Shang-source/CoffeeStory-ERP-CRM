// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import type { ReactElement } from 'react';
import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { makeCustomer, makeInvoice, makeStatement } from '@/entities/testing/fixtures';
import InvoicesPage from './InvoicesPage';
import PaymentsPage from './PaymentsPage';
import StatementsPage from './StatementsPage';

const getAdminInvoicesMock = vi.fn();
const getAdminStatementsMock = vi.fn();

vi.mock('@/entities/invoice/api/invoiceApi', () => ({
  getAdminInvoices: (...args: unknown[]) => getAdminInvoicesMock(...args),
}));

vi.mock('@/entities/statement/api/statementApi', () => ({
  getAdminStatements: (...args: unknown[]) => getAdminStatementsMock(...args),
}));

vi.mock('sonner', () => ({
  toast: {
    error: vi.fn(),
    info: vi.fn(),
    success: vi.fn(),
  },
}));

describe('Finance workflow pages', () => {
  beforeEach(() => {
    getAdminInvoicesMock.mockReset();
    getAdminStatementsMock.mockReset();
  });

  afterEach(() => {
    cleanup();
  });

  it('classifies invoices by action status and supports search', async () => {
    const nora = makeCustomer({ id: 'customer-nora', businessName: 'Nora Fish', email: 'nora@example.com' });
    const cafe = makeCustomer({ id: 'customer-cafe', businessName: 'Auckland Cafe', email: 'accounts@cafe.test' });
    getAdminInvoicesMock.mockResolvedValue([
      makeInvoice({ id: 'draft', invoiceNumber: 'INV-DRAFT', customerId: nora.id, customer: nora, status: 'Draft', emailStatus: 'NotSent' }),
      makeInvoice({ id: 'unpaid', invoiceNumber: 'INV-UNPAID', customerId: cafe.id, customer: cafe, status: 'Unpaid', emailStatus: 'Sent' }),
      makeInvoice({ id: 'overdue', invoiceNumber: 'INV-OVERDUE', customerId: cafe.id, customer: cafe, status: 'Overdue', emailStatus: 'Sent' }),
      makeInvoice({ id: 'paid', invoiceNumber: 'INV-PAID', customerId: cafe.id, customer: cafe, status: 'Paid', emailStatus: 'Sent', outstandingAmount: 0, paidAmount: 87.4 }),
      makeInvoice({ id: 'failed', invoiceNumber: 'INV-FAILED', customerId: cafe.id, customer: cafe, status: 'Issued', emailStatus: 'Failed' }),
    ]);

    renderWithQuery(<InvoicesPage />);

    await screen.findByText('INV-DRAFT');
    expect(screen.getByRole('tab', { name: 'Need to Send (2)' })).toHaveAttribute('aria-selected', 'true');
    expect(screen.getByRole('tab', { name: 'Awaiting Payment (1)' })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: 'Overdue (1)' })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: 'Failed (1)' })).toBeInTheDocument();

    fireEvent.click(screen.getByRole('tab', { name: 'Awaiting Payment (1)' }));
    expect(await screen.findByText('INV-UNPAID')).toBeInTheDocument();
    expect(screen.queryByText('INV-DRAFT')).not.toBeInTheDocument();

    fireEvent.change(screen.getByLabelText('Search invoices, customers, account numbers, emails, amounts'), { target: { value: 'nora' } });
    fireEvent.click(screen.getByRole('tab', { name: 'Need to Send (1)' }));
    expect(await screen.findByText('INV-DRAFT')).toBeInTheDocument();
    expect(screen.queryByText('INV-FAILED')).not.toBeInTheDocument();
  }, 15_000);

  it('separates payment collection, active records, and voided records', async () => {
    const customer = makeCustomer({ businessName: 'Nora Fish' });
    getAdminInvoicesMock.mockResolvedValue([
      makeInvoice({ id: 'collect', invoiceNumber: 'INV-COLLECT', customer, customerId: customer.id, status: 'Unpaid', outstandingAmount: 50 }),
      makeInvoice({
        id: 'paid',
        invoiceNumber: 'INV-PAID',
        customer,
        customerId: customer.id,
        status: 'Paid',
        outstandingAmount: 0,
        payments: [{
          id: 'payment-active',
          invoiceId: 'paid',
          amount: 50,
          paymentDate: new Date('2026-05-28T00:00:00Z'),
          paymentMethod: 'BankTransfer',
          reference: 'BANK-123',
          markedByUserId: 'admin',
          isVoided: false,
        }],
      }),
      makeInvoice({
        id: 'voided',
        invoiceNumber: 'INV-VOIDED',
        customer,
        customerId: customer.id,
        status: 'Paid',
        outstandingAmount: 0,
        payments: [{
          id: 'payment-voided',
          invoiceId: 'voided',
          amount: 25,
          paymentDate: new Date('2026-05-27T00:00:00Z'),
          paymentMethod: 'Cash',
          reference: 'VOID-123',
          markedByUserId: 'admin',
          isVoided: true,
        }],
      }),
    ]);

    renderWithQuery(<PaymentsPage />);

    await screen.findByText('INV-COLLECT');
    expect(screen.getByRole('tab', { name: 'To Collect (1)' })).toHaveAttribute('aria-selected', 'true');

    fireEvent.click(screen.getByRole('tab', { name: 'Payment Records (1)' }));
    expect(await screen.findByText('BANK-123')).toBeInTheDocument();
    expect(screen.queryByText('VOID-123')).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole('tab', { name: 'Voided (1)' }));
    expect(await screen.findByText('VOID-123')).toBeInTheDocument();
    expect(screen.queryByText('BANK-123')).not.toBeInTheDocument();

    fireEvent.change(screen.getByLabelText('Search invoice, customer, account number, email, amount'), { target: { value: 'bank' } });
    fireEvent.click(screen.getByRole('tab', { name: 'Payment Records (1)' }));
    expect(await screen.findByText('BANK-123')).toBeInTheDocument();
  }, 15_000);

  it('classifies statements by send status and supports search', async () => {
    const nora = makeCustomer({ id: 'customer-nora', businessName: 'Nora Fish', email: 'nora@example.com' });
    const cafe = makeCustomer({ id: 'customer-cafe', businessName: 'Auckland Cafe', email: 'accounts@cafe.test' });
    getAdminStatementsMock.mockResolvedValue([
      makeStatement({ id: 'ready', statementNumber: 'STMT-READY', customerId: nora.id, customer: nora, status: 'ReadyToSend', emailStatus: 'NotSent' }),
      makeStatement({ id: 'sent', statementNumber: 'STMT-SENT', customerId: cafe.id, customer: cafe, status: 'Sent', emailStatus: 'Sent' }),
      makeStatement({ id: 'failed', statementNumber: 'STMT-FAILED', customerId: cafe.id, customer: cafe, status: 'ReadyToSend', emailStatus: 'Failed' }),
    ]);

    renderWithQuery(<StatementsPage />);

    await screen.findByText('STMT-READY');
    expect(screen.getByRole('tab', { name: 'Ready to Send (2)' })).toHaveAttribute('aria-selected', 'true');

    fireEvent.click(screen.getByRole('tab', { name: 'Sent (1)' }));
    expect(await screen.findByText('STMT-SENT')).toBeInTheDocument();
    expect(screen.queryByText('STMT-READY')).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole('tab', { name: 'Failed (1)' }));
    expect(await screen.findByText('STMT-FAILED')).toBeInTheDocument();

    fireEvent.change(screen.getByLabelText('Search statements, customers, account numbers, periods, amounts'), { target: { value: 'nora' } });
    fireEvent.click(screen.getByRole('tab', { name: 'Ready to Send (1)' }));
    expect(await screen.findByText('STMT-READY')).toBeInTheDocument();
    expect(screen.queryByText('STMT-FAILED')).not.toBeInTheDocument();
  }, 15_000);
});

function renderWithQuery(ui: ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        {ui}
      </MemoryRouter>
    </QueryClientProvider>
  );
}
