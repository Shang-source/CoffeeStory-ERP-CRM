import { RouterProvider } from 'react-router';
import { router } from '@/app/router/router';
import { AppProviders } from '@/app/providers/AppProviders';

export default function App() {
  return (
    <AppProviders>
      <RouterProvider router={router} />
    </AppProviders>
  );
}
