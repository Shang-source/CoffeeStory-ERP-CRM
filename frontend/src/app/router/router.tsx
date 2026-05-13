import { createBrowserRouter } from 'react-router';
import RootLayout from '@/app/layouts/RootLayout';
import { adminRoutes } from './adminRoutes';
import { customerRoutes } from './customerRoutes';
import type { ComponentType } from 'react';

const lazyPage = (loader: () => Promise<{ default: ComponentType }>) => async () => ({
  Component: (await loader()).default,
});

export const router = createBrowserRouter([
  {
    path: "/",
    Component: RootLayout,
    children: [
      { index: true, lazy: lazyPage(() => import('@/pages/auth/LoginPage')) },
      ...customerRoutes,
      ...adminRoutes,
      { path: "*", lazy: lazyPage(() => import('@/pages/NotFoundPage')) },
    ],
  },
]);
