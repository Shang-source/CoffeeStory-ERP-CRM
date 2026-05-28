// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { makeCustomerProduct, makeStandingOrder } from '@/entities/testing/fixtures';
import { ApiError } from '@/shared/api/apiError';
import StandingOrderPage from './StandingOrderPage';

const getCustomerStandingOrderMock = vi.fn();
const getCustomerProductsMock = vi.fn();
const updateCustomerStandingOrderMock = vi.fn();

vi.mock('@/entities/standingOrder/api/standingOrderApi', () => ({
  getCustomerStandingOrder: (...args: unknown[]) => getCustomerStandingOrderMock(...args),
}));

vi.mock('@/entities/product/api/productApi', () => ({
  getCustomerProducts: (...args: unknown[]) => getCustomerProductsMock(...args),
}));

vi.mock('@/features/standingOrderEditor/api/standingOrderEditorApi', () => ({
  updateCustomerStandingOrder: (...args: unknown[]) => updateCustomerStandingOrderMock(...args),
}));

vi.mock('@/app/providers/AuthProvider', () => ({
  useAuth: () => ({
    user: {
      id: 'user-1',
      email: 'nora@example.com',
      role: 'Customer',
      customerId: 'customer-1',
      name: 'Nora Fish',
    },
  }),
}));

vi.mock('sonner', () => ({
  toast: {
    error: vi.fn(),
    success: vi.fn(),
  },
}));

describe('Customer StandingOrderPage', () => {
  beforeEach(() => {
    getCustomerStandingOrderMock.mockReset();
    getCustomerProductsMock.mockReset();
    updateCustomerStandingOrderMock.mockReset();
  });

  afterEach(() => {
    cleanup();
  });

  it('adds new standing-order items using the customer effective price', async () => {
    getCustomerStandingOrderMock.mockResolvedValue(makeStandingOrder({ items: [] }));
    getCustomerProductsMock.mockResolvedValue([makeCustomerProduct()]);

    renderWithQuery();

    fireEvent.click(await screen.findByRole('button', { name: 'Edit Order' }));
    fireEvent.click(screen.getByRole('button', { name: 'Add Item' }));
    const addItemDialog = await screen.findByRole('dialog', { name: 'Add Item to Standing Order' });
    fireEvent.mouseDown(within(addItemDialog).getByRole('combobox'));
    fireEvent.click(await screen.findByRole('option', {
      name: 'House Blend 1kg - $35.00 (custom price, base $38.00)',
    }));
    fireEvent.click(within(addItemDialog).getByRole('button', { name: 'Add Item' }));

    await waitFor(() => {
      expect(screen.getAllByText('$35.00').length).toBeGreaterThan(0);
    });
    expect(screen.getByText('Estimated Total: $35.00')).toBeInTheDocument();
  }, 20_000);

  it('shows create mode when no standing order exists yet', async () => {
    getCustomerStandingOrderMock.mockResolvedValue(null);
    getCustomerProductsMock.mockResolvedValue([makeCustomerProduct()]);

    renderWithQuery();

    expect(await screen.findByText('No standing order exists yet. Add your coffee items, choose a frequency, then create your standing order.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Create Standing Order' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Add Item' })).toBeInTheDocument();
    expect(screen.queryByText('Standing order not found.')).not.toBeInTheDocument();
  }, 20_000);

  it('treats a missing customer standing order API response as create mode', async () => {
    getCustomerStandingOrderMock.mockRejectedValue(new ApiError('Standing order not found.', 404, 'NOT_FOUND'));
    getCustomerProductsMock.mockResolvedValue([makeCustomerProduct()]);

    renderWithQuery();

    expect(await screen.findByText('No standing order exists yet. Add your coffee items, choose a frequency, then create your standing order.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Create Standing Order' })).toBeInTheDocument();
    expect(screen.queryByText('Standing order not found.')).not.toBeInTheDocument();
  }, 20_000);
});

function renderWithQuery() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <StandingOrderPage />
    </QueryClientProvider>
  );
}
