import { Outlet } from 'react-router';

export default function RootLayout() {
  return (
    <div style={{ minHeight: '100vh', background: '#f9fafb' }}>
      <Outlet />
    </div>
  );
}
