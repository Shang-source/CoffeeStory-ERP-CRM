import { useState } from 'react';
import { useNavigate } from 'react-router';
import { Alert, Box, Button, Card, CardContent, TextField, Typography } from '@mui/material';
import { LocalCafe } from '@mui/icons-material';
import { useAuth } from '@/app/providers/AuthProvider';

export default function Login() {
  const navigate = useNavigate();
  const { login } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const navigateAfterAuth = (role: string) => {
    navigate(role === 'Customer' ? '/customer' : '/admin');
  };

  const handleLogin = async () => {
    setIsSubmitting(true);
    setError('');
    const profile = await login(email, password);
    setIsSubmitting(false);
    if (profile) {
      navigateAfterAuth(profile.role);
    } else {
      setError('Invalid email or password');
    }
  };

  return (
    <Box sx={{
      minHeight: '100vh',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)'
    }}>
      <Card sx={{ maxWidth: 400, width: '100%', mx: 2 }}>
        <CardContent sx={{ p: 4 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', mb: 3 }}>
            <LocalCafe sx={{ fontSize: 40, mr: 1, color: '#6b4423' }} />
            <Typography variant="h4" component="h1">
              StoryCoffee
            </Typography>
          </Box>
          <Typography variant="h6" align="center" gutterBottom>
            B2B Order & Invoice Management
          </Typography>
          <Typography variant="body2" align="center" color="text.secondary" sx={{ mb: 4 }}>
            Sign in to your account
          </Typography>

          {error && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {error}
            </Alert>
          )}

          <TextField
            label="Email"
            type="email"
            fullWidth
            margin="normal"
            value={email}
            autoComplete="email"
            onChange={(e) => setEmail(e.target.value)}
          />
          <TextField
            label="Password"
            type="password"
            fullWidth
            margin="normal"
            value={password}
            autoComplete="current-password"
            onChange={(e) => setPassword(e.target.value)}
            onKeyDown={(e) => e.key === 'Enter' && handleLogin()}
          />
          <Button
            variant="contained"
            fullWidth
            size="large"
            onClick={handleLogin}
            disabled={isSubmitting}
            sx={{ mt: 3, py: 1.5 }}
          >
            {isSubmitting ? 'Signing in...' : 'Sign In'}
          </Button>
        </CardContent>
      </Card>
    </Box>
  );
}
