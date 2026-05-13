// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router';
import { beforeEach, describe, expect, it } from 'vitest';
import { AuthProvider } from '@/app/providers/AuthProvider';
import { customerProfile } from '@/entities/testing/fixtures';
import RequireRole from './RequireRole';

describe('RequireRole', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('redirects unauthenticated users to login', async () => {
    renderGuard('/admin/orders');

    expect(await screen.findByText('Login Page')).toBeInTheDocument();
  });

  it('redirects authenticated users away from the wrong role area', async () => {
    localStorage.setItem('storycoffee.user', JSON.stringify(customerProfile));

    renderGuard('/admin/orders');

    expect(await screen.findByText('Customer Dashboard')).toBeInTheDocument();
  });
});

function renderGuard(initialPath: string) {
  render(
    <MemoryRouter initialEntries={[initialPath]}>
      <AuthProvider>
        <Routes>
          <Route path="/" element={<div>Login Page</div>} />
          <Route path="/customer" element={<div>Customer Dashboard</div>} />
          <Route element={<RequireRole role="Admin" />}>
            <Route path="/admin/orders" element={<div>Admin Orders</div>} />
          </Route>
        </Routes>
      </AuthProvider>
    </MemoryRouter>
  );
}
