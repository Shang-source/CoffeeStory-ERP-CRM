import { Navigate, Outlet } from 'react-router';
import { useAuth } from '@/app/providers/AuthProvider';

export default function RequireAuth() {
  const { user } = useAuth();
  return user ? <Outlet /> : <Navigate to="/" replace />;
}
