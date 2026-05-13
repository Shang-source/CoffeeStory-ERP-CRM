// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { ReactElement } from 'react';
import { makeInvoice, makeStatement } from '@/entities/testing/fixtures';
import InvoiceDetailPage from './InvoiceDetailPage';
import StatementDetailPage from './StatementDetailPage';

const getCustomerInvoiceMock = vi.fn();
const downloadCustomerInvoicePdfMock = vi.fn();
const getCustomerStatementMock = vi.fn();
const downloadCustomerStatementPdfMock = vi.fn();

vi.mock('@/entities/invoice/api/invoiceApi', () => ({
  getCustomerInvoice: (...args: unknown[]) => getCustomerInvoiceMock(...args),
}));

vi.mock('@/features/invoiceActions/api/invoiceActionsApi', () => ({
  downloadCustomerInvoicePdf: (...args: unknown[]) => downloadCustomerInvoicePdfMock(...args),
}));

vi.mock('@/entities/statement/api/statementApi', () => ({
  getCustomerStatement: (...args: unknown[]) => getCustomerStatementMock(...args),
}));

vi.mock('@/features/statementActions/api/statementActionsApi', () => ({
  downloadCustomerStatementPdf: (...args: unknown[]) => downloadCustomerStatementPdfMock(...args),
}));

vi.mock('sonner', () => ({
  toast: {
    error: vi.fn(),
    success: vi.fn(),
  },
}));

describe('customer detail pages', () => {
  afterEach(() => {
    cleanup();
  });

  beforeEach(() => {
    getCustomerInvoiceMock.mockReset();
    downloadCustomerInvoicePdfMock.mockReset();
    getCustomerStatementMock.mockReset();
    downloadCustomerStatementPdfMock.mockReset();
  });

  it('renders an invoice detail view and downloads the invoice PDF', async () => {
    getCustomerInvoiceMock.mockResolvedValue(makeInvoice());
    downloadCustomerInvoicePdfMock.mockResolvedValue(undefined);

    renderWithQuery('/customer/invoices/invoice-1', (
      <Route path="/customer/invoices/:id" element={<InvoiceDetailPage />} />
    ));

    expect(await screen.findByRole('heading', { name: 'Invoice INV-1001' })).toBeInTheDocument();
    expect(screen.getByText('House Blend 1kg')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Download PDF' }));

    await waitFor(() => {
      expect(downloadCustomerInvoicePdfMock).toHaveBeenCalledWith('invoice-1');
    });
    expect(getCustomerInvoiceMock).toHaveBeenCalledWith('invoice-1');
  });

  it('renders a statement detail view with invoice links and downloads the statement PDF', async () => {
    getCustomerStatementMock.mockResolvedValue(makeStatement());
    downloadCustomerStatementPdfMock.mockResolvedValue(undefined);

    renderWithQuery('/customer/statements/statement-1', (
      <Route path="/customer/statements/:id" element={<StatementDetailPage />} />
    ));

    expect(await screen.findByRole('heading', { name: 'Statement STMT-1001' })).toBeInTheDocument();
    expect(screen.getByText('INV-1001')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Download PDF' }));

    await waitFor(() => {
      expect(downloadCustomerStatementPdfMock).toHaveBeenCalledWith('statement-1');
    });
    expect(getCustomerStatementMock).toHaveBeenCalledWith('statement-1');
  });
});

function renderWithQuery(initialPath: string, route: ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
    },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[initialPath]}>
        <Routes>{route}</Routes>
      </MemoryRouter>
    </QueryClientProvider>
  );
}
