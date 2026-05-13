// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthProvider } from '@/app/providers/AuthProvider';
import { adminProfile } from '@/entities/testing/fixtures';
import LoginPage from './LoginPage';

const loginMock = vi.fn();

vi.mock('@/features/auth/api/authApi', () => ({
  login: (...args: unknown[]) => loginMock(...args),
}));

describe('LoginPage', () => {
  beforeEach(() => {
    localStorage.clear();
    loginMock.mockReset();
  });

  it('navigates to the admin area after a successful admin login', async () => {
    loginMock.mockResolvedValue(adminProfile);

    render(
      <MemoryRouter initialEntries={['/']}>
        <AuthProvider>
          <Routes>
            <Route path="/" element={<LoginPage />} />
            <Route path="/admin" element={<h1>Admin Dashboard</h1>} />
          </Routes>
        </AuthProvider>
      </MemoryRouter>
    );

    fireEvent.change(screen.getByLabelText('Email'), { target: { value: 'admin@storycoffee.co.nz' } });
    fireEvent.change(screen.getByLabelText('Password'), { target: { value: 'password' } });
    fireEvent.click(screen.getByRole('button', { name: 'Sign In' }));

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Admin Dashboard' })).toBeInTheDocument();
    });
    expect(loginMock).toHaveBeenCalledWith('admin@storycoffee.co.nz', 'password');
  });

  it('shows an error when authentication fails', async () => {
    loginMock.mockRejectedValue(new Error('Invalid credentials'));

    render(
      <MemoryRouter initialEntries={['/']}>
        <AuthProvider>
          <LoginPage />
        </AuthProvider>
      </MemoryRouter>
    );

    fireEvent.change(screen.getByLabelText('Email'), { target: { value: 'admin@storycoffee.co.nz' } });
    fireEvent.change(screen.getByLabelText('Password'), { target: { value: 'bad-password' } });
    fireEvent.click(screen.getByRole('button', { name: 'Sign In' }));

    expect(await screen.findByText('Invalid email or password')).toBeInTheDocument();
  });
});
