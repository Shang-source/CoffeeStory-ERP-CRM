// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { makeOrder, makeProductionBatch } from '@/entities/testing/fixtures';
import OrdersPage from './OrdersPage';

const getAdminOrdersMock = vi.fn();
const batchSendOrdersToProductionMock = vi.fn();

vi.mock('@/entities/order/api/orderApi', () => ({
  getAdminOrders: (...args: unknown[]) => getAdminOrdersMock(...args),
}));

vi.mock('@/features/batchToProduction/api/batchToProductionApi', () => ({
  batchSendOrdersToProduction: (...args: unknown[]) => batchSendOrdersToProductionMock(...args),
}));

vi.mock('@/features/orderWorkflow/api/orderWorkflowApi', () => ({
  batchShipAndInvoiceOrders: vi.fn(),
  cancelOrder: vi.fn(),
  markOrderReadyToShip: vi.fn(),
  markOrderShipped: vi.fn(),
  sendOrderToProduction: vi.fn(),
}));

vi.mock('sonner', () => ({
  toast: {
    error: vi.fn(),
    info: vi.fn(),
    success: vi.fn(),
  },
}));

describe('OrdersPage', () => {
  beforeEach(() => {
    getAdminOrdersMock.mockReset();
    batchSendOrdersToProductionMock.mockReset();
  });

  afterEach(() => {
    cleanup();
  });

  it('uses the batch-to-production API for generated orders', async () => {
    const generatedOrder = makeOrder({ id: 'order-1', orderNumber: 'ORD-1001' });
    const secondGeneratedOrder = makeOrder({ id: 'order-2', orderNumber: 'ORD-1002' });
    const updatedOrders = [
      { ...generatedOrder, orderStatus: 'InProduction' as const },
      { ...secondGeneratedOrder, orderStatus: 'InProduction' as const },
    ];
    getAdminOrdersMock
      .mockResolvedValueOnce([generatedOrder, secondGeneratedOrder])
      .mockResolvedValue(updatedOrders);
    batchSendOrdersToProductionMock.mockResolvedValue({
      updated: 2,
      orders: updatedOrders,
      productionBatch: makeProductionBatch(),
    });

    renderWithQuery();

    await screen.findByText('ORD-1001');
    fireEvent.click(screen.getAllByRole('checkbox')[0]);

    const batchButton = await screen.findByRole('button', { name: 'Send selected to production (2)' });
    fireEvent.click(batchButton);

    await waitFor(() => {
      expect(batchSendOrdersToProductionMock).toHaveBeenCalledWith(['order-1', 'order-2']);
    });
    expect(await screen.findByRole('button', { name: 'Send selected to production (0)' })).toBeDisabled();
  }, 15000);

  it('defaults to work queues and hides cancelled orders until All is explicitly expanded', async () => {
    const generatedOrder = makeOrder({ id: 'order-1', orderNumber: 'ORD-1001' });
    const cancelledOrder = makeOrder({ id: 'order-2', orderNumber: 'ORD-CANCELLED', orderStatus: 'Cancelled' });
    getAdminOrdersMock.mockResolvedValue([generatedOrder, cancelledOrder]);

    renderWithQuery();

    await screen.findByText('ORD-1001');
    expect(screen.queryByText('ORD-CANCELLED')).not.toBeInTheDocument();
    expect(screen.getByRole('tab', { name: 'Need Production (1)' })).toHaveAttribute('aria-selected', 'true');

    fireEvent.click(screen.getByRole('tab', { name: 'All (1)' }));
    expect(screen.queryByText('ORD-CANCELLED')).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole('switch', { name: 'Show cancelled' }));
    expect(await screen.findByRole('tab', { name: 'All (2)' })).toBeInTheDocument();
    expect(await screen.findByText('ORD-CANCELLED')).toBeInTheDocument();
  });
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
      <MemoryRouter>
        <OrdersPage />
      </MemoryRouter>
    </QueryClientProvider>
  );
}
