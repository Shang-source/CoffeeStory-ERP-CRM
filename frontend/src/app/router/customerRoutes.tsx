import type { RouteObject } from 'react-router';
import RequireRole from '@/app/guards/RequireRole';
import CustomerLayout from '@/app/layouts/CustomerLayout';
import type { ComponentType } from 'react';

const lazyPage = (loader: () => Promise<{ default: ComponentType }>) => async () => ({
  Component: (await loader()).default,
});

export const customerRoutes: RouteObject[] = [
  {
    element: <RequireRole role="Customer" />,
    children: [
      {
        path: 'customer',
        Component: CustomerLayout,
        children: [
          { index: true, lazy: lazyPage(() => import('@/pages/customer/DashboardPage')) },
          { path: 'standing-order', lazy: lazyPage(() => import('@/pages/customer/StandingOrderPage')) },
          { path: 'orders', lazy: lazyPage(() => import('@/pages/customer/OrdersPage')) },
          { path: 'invoices', lazy: lazyPage(() => import('@/pages/customer/InvoicesPage')) },
          { path: 'invoices/:id', lazy: lazyPage(() => import('@/pages/customer/InvoiceDetailPage')) },
          { path: 'statements', lazy: lazyPage(() => import('@/pages/customer/StatementsPage')) },
          { path: 'statements/:id', lazy: lazyPage(() => import('@/pages/customer/StatementDetailPage')) },
          { path: 'settings', lazy: lazyPage(() => import('@/pages/customer/AccountSettingsPage')) },
        ],
      },
    ],
  },
];
