// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { makeCustomer, makeCustomerPriceBook, makeStandingOrder } from '@/entities/testing/fixtures';
import CustomerDetailPage from './CustomerDetailPage';

const getAdminCustomerMock = vi.fn();
const updateAdminCustomerMock = vi.fn();
const getAdminOrdersMock = vi.fn();
const getAdminInvoicesMock = vi.fn();
const getAdminStandingOrdersMock = vi.fn();
const sendAdminCustomerInviteMock = vi.fn();
const getAdminCustomerPriceBookMock = vi.fn();
const updateAdminCustomerPriceBookMock = vi.fn();

vi.mock('@/entities/customer/api/customerApi', () => ({
  getAdminCustomer: (...args: unknown[]) => getAdminCustomerMock(...args),
  updateAdminCustomer: (...args: unknown[]) => updateAdminCustomerMock(...args),
}));

vi.mock('@/entities/order/api/orderApi', () => ({
  getAdminOrders: (...args: unknown[]) => getAdminOrdersMock(...args),
}));

vi.mock('@/entities/invoice/api/invoiceApi', () => ({
  getAdminInvoices: (...args: unknown[]) => getAdminInvoicesMock(...args),
}));

vi.mock('@/entities/standingOrder/api/standingOrderApi', () => ({
  getAdminStandingOrders: (...args: unknown[]) => getAdminStandingOrdersMock(...args),
}));

vi.mock('@/features/customerInvite/api/customerInviteApi', () => ({
  sendAdminCustomerInvite: (...args: unknown[]) => sendAdminCustomerInviteMock(...args),
}));

vi.mock('@/features/customerPriceBook/api/customerPriceBookApi', () => ({
  getAdminCustomerPriceBook: (...args: unknown[]) => getAdminCustomerPriceBookMock(...args),
  updateAdminCustomerPriceBook: (...args: unknown[]) => updateAdminCustomerPriceBookMock(...args),
}));

vi.mock('sonner', () => ({
  toast: {
    error: vi.fn(),
    success: vi.fn(),
  },
}));

describe('CustomerDetailPage', () => {
  beforeEach(() => {
    getAdminCustomerMock.mockReset();
    updateAdminCustomerMock.mockReset();
    getAdminOrdersMock.mockReset();
    getAdminInvoicesMock.mockReset();
    getAdminStandingOrdersMock.mockReset();
    sendAdminCustomerInviteMock.mockReset();
    getAdminCustomerPriceBookMock.mockReset();
    updateAdminCustomerPriceBookMock.mockReset();
  });

  it('saves customer price book overrides and refreshes future standing-order pricing', async () => {
    getAdminCustomerMock.mockResolvedValue(makeCustomer());
    getAdminOrdersMock.mockResolvedValue([]);
    getAdminInvoicesMock.mockResolvedValue([]);
    getAdminStandingOrdersMock.mockResolvedValue([makeStandingOrder()]);
    getAdminCustomerPriceBookMock.mockResolvedValue(makeCustomerPriceBook());
    updateAdminCustomerPriceBookMock.mockResolvedValue(makeCustomerPriceBook({
      items: [{
        productId: 'product-1',
        sku: 'HB-1KG',
        name: 'House Blend 1kg',
        unit: 'kg',
        basePrice: 38,
        overridePrice: 34.5,
        effectivePrice: 34.5,
        hasOverride: true,
        isActive: true,
        notes: 'Wholesale override',
      }],
    }));

    const { container } = render(
      <MemoryRouter initialEntries={['/admin/customers/customer-1']}>
        <Routes>
          <Route path="/admin/customers/:id" element={<CustomerDetailPage />} />
        </Routes>
      </MemoryRouter>
    );

    const priceBookHeading = await screen.findByRole('heading', { name: 'Price Book' });
    const priceBookSection = priceBookHeading.closest('.MuiCardContent-root');
    expect(priceBookSection).not.toBeNull();

    const overrideInput = container.querySelector('input[type="number"]');
    expect(overrideInput).not.toBeNull();
    fireEvent.change(overrideInput!, { target: { value: '34.5' } });
    fireEvent.change(within(priceBookSection as HTMLElement).getByPlaceholderText('Optional notes'), {
      target: { value: 'Wholesale override' },
    });
    fireEvent.click(within(priceBookSection as HTMLElement).getByRole('button', { name: 'Save Price Book' }));

    await waitFor(() => {
      expect(updateAdminCustomerPriceBookMock).toHaveBeenCalledWith('customer-1', {
        items: [{
          productId: 'product-1',
          overridePrice: 34.5,
          isActive: true,
          notes: 'Wholesale override',
        }],
      });
    });
    expect((await screen.findAllByText('$34.50')).length).toBeGreaterThan(0);
  });
});
