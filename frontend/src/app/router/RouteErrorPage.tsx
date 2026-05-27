import { useEffect, useMemo, useState } from 'react';
import { Alert, Box, Button, Paper, Typography } from '@mui/material';
import RefreshIcon from '@mui/icons-material/Refresh';
import { useRouteError } from 'react-router';

const chunkReloadKey = 'storycoffee.chunkReloadAttempted';

export default function RouteErrorPage() {
  const error = useRouteError();
  const message = getErrorMessage(error);
  const isChunkError = isDynamicImportError(message);
  const [isReloading, setIsReloading] = useState(false);

  useEffect(() => {
    if (!isChunkError) {
      return;
    }

    const currentAttempt = sessionStorage.getItem(chunkReloadKey);
    if (currentAttempt === window.location.pathname) {
      return;
    }

    sessionStorage.setItem(chunkReloadKey, window.location.pathname);
    setIsReloading(true);
    window.location.reload();
  }, [isChunkError]);

  const title = useMemo(() => {
    if (isChunkError) {
      return isReloading ? 'Updating StoryCoffee…' : 'StoryCoffee was updated';
    }

    return 'Something went wrong';
  }, [isChunkError, isReloading]);

  return (
    <Box sx={{ minHeight: '100vh', display: 'grid', placeItems: 'center', bgcolor: '#f9fafb', p: 3 }}>
      <Paper sx={{ maxWidth: 560, width: '100%', p: 4 }}>
        <Typography variant="h5" gutterBottom>
          {title}
        </Typography>
        <Typography color="text.secondary" sx={{ mb: 3 }}>
          {isChunkError
            ? 'A new version has been deployed. Reload the page to load the latest files.'
            : 'The page could not be loaded.'}
        </Typography>
        {!isChunkError && (
          <Alert severity="error" sx={{ mb: 3 }}>
            {message}
          </Alert>
        )}
        <Button variant="contained" startIcon={<RefreshIcon />} onClick={() => window.location.reload()}>
          Reload page
        </Button>
      </Paper>
    </Box>
  );
}

function getErrorMessage(error: unknown) {
  if (error instanceof Error) {
    return error.message;
  }

  if (typeof error === 'string') {
    return error;
  }

  return 'Unexpected application error.';
}

function isDynamicImportError(message: string) {
  return [
    'failed to fetch dynamically imported module',
    'error loading dynamically imported module',
    'importing a module script failed',
    'loading chunk',
    'chunkloaderror',
  ].some((pattern) => message.toLowerCase().includes(pattern));
}
