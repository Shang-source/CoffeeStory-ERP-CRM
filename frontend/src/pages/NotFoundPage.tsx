import { Box, Typography, Button } from '@mui/material';
import { Link } from 'react-router';

export default function NotFound() {
  return (
    <Box sx={{
      minHeight: '100vh',
      display: 'flex',
      flexDirection: 'column',
      alignItems: 'center',
      justifyContent: 'center',
      textAlign: 'center',
      p: 3
    }}>
      <Typography variant="h1" sx={{ fontSize: '6rem', fontWeight: 'bold', mb: 2 }}>
        404
      </Typography>
      <Typography variant="h5" gutterBottom>
        Page Not Found
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 4 }}>
        The page you are looking for does not exist.
      </Typography>
      <Button component={Link} to="/" variant="contained">
        Go to Home
      </Button>
    </Box>
  );
}
