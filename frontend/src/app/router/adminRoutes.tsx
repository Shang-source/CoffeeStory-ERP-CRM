import type { RouteObject } from 'react-router';
import RequireRole from '@/app/guards/RequireRole';
import AdminLayout from '@/app/layouts/AdminLayout';
import type { ComponentType } from 'react';

const lazyPage = (loader: () => Promise<{ default: ComponentType }>) => async () => ({
  Component: (await loader()).default,
});

export const adminRoutes: RouteObject[] = [
  {
    element: <RequireRole role="Admin" />,
    children: [
      {
        path: 'admin',
        Component: AdminLayout,
        children: [
          { index: true, lazy: lazyPage(() => import('@/pages/admin/DashboardPage')) },
          { path: 'customers', lazy: lazyPage(() => import('@/pages/admin/CustomersPage')) },
          { path: 'customers/:id', lazy: lazyPage(() => import('@/pages/admin/CustomerDetailPage')) },
          { path: 'products', lazy: lazyPage(() => import('@/pages/admin/ProductsPage')) },
          { path: 'standing-orders', lazy: lazyPage(() => import('@/pages/admin/StandingOrdersPage')) },
          { path: 'orders', lazy: lazyPage(() => import('@/pages/admin/OrdersPage')) },
          { path: 'production', lazy: lazyPage(() => import('@/pages/admin/ProductionPage')) },
          { path: 'invoices', lazy: lazyPage(() => import('@/pages/admin/InvoicesPage')) },
          { path: 'payments', lazy: lazyPage(() => import('@/pages/admin/PaymentsPage')) },
          { path: 'statements', lazy: lazyPage(() => import('@/pages/admin/StatementsPage')) },
          { path: 'statements/:id', lazy: lazyPage(() => import('@/pages/admin/StatementDetailPage')) },
          { path: 'logs', lazy: lazyPage(() => import('@/pages/admin/LogsPage')) },
        ],
      },
    ],
  },
];
