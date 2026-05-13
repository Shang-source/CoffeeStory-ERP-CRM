// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { makeCustomerProduct, makeStandingOrder } from '@/entities/testing/fixtures';
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

  it('adds new standing-order items using the customer effective price', async () => {
    getCustomerStandingOrderMock.mockResolvedValue(makeStandingOrder({ items: [] }));
    getCustomerProductsMock.mockResolvedValue([makeCustomerProduct()]);

    render(<StandingOrderPage />);

    fireEvent.click(await screen.findByRole('button', { name: 'Edit Order' }));
    fireEvent.click(screen.getByRole('button', { name: 'Add Item' }));
    fireEvent.mouseDown(screen.getByRole('combobox'));
    fireEvent.click(await screen.findByRole('option', {
      name: 'House Blend 1kg - $35.00 (custom price, base $38.00)',
    }));
    fireEvent.click(screen.getByRole('button', { name: 'Add Item' }));

    await waitFor(() => {
      expect(screen.getAllByText('$35.00').length).toBeGreaterThan(0);
    });
    expect(screen.getByText('Estimated Total: $35.00')).toBeInTheDocument();
  });
});
