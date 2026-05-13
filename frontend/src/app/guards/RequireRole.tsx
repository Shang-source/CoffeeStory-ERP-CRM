import { Navigate, Outlet } from 'react-router';
import { UserRole } from '@/entities/types';
import { useAuth } from '@/app/providers/AuthProvider';

export default function RequireRole({ role }: { role: UserRole }) {
  const { user } = useAuth();

  if (!user) {
    return <Navigate to="/" replace />;
  }

  if (user.role !== role) {
    return <Navigate to={user.role === 'Admin' ? '/admin' : '/customer'} replace />;
  }

  return <Outlet />;
}
