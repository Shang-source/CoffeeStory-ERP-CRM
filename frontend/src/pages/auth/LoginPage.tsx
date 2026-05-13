import { useState } from 'react';
import { useNavigate } from 'react-router';
import { Box, Card, CardContent, TextField, Button, Typography, ToggleButtonGroup, ToggleButton, Alert } from '@mui/material';
import { LocalCafe } from '@mui/icons-material';
import { useAuth } from '@/app/providers/AuthProvider';

export default function Login() {
  const navigate = useNavigate();
  const { login } = useAuth();
  const [userType, setUserType] = useState<'customer' | 'admin'>('customer');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleLogin = async () => {
    setIsSubmitting(true);
    setError('');
    const profile = await login(email, password);
    setIsSubmitting(false);
    if (profile) {
      if (profile.role === 'Customer') {
        navigate('/customer');
      } else {
        navigate('/admin');
      }
    } else {
      setError('Invalid email or password');
    }
  };

  const handleUserTypeChange = (newType: 'customer' | 'admin') => {
    setUserType(newType);
    setError('');
    if (newType === 'customer') {
      setEmail('john@aucklandcafe.co.nz');
    } else {
      setEmail('admin@storycoffee.co.nz');
    }
    setPassword('password');
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

          <ToggleButtonGroup
            value={userType}
            exclusive
            onChange={(e, value) => value && handleUserTypeChange(value)}
            fullWidth
            sx={{ mb: 3 }}
          >
            <ToggleButton value="customer">Customer Portal</ToggleButton>
            <ToggleButton value="admin">Admin Portal</ToggleButton>
          </ToggleButtonGroup>

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
            onChange={(e) => setEmail(e.target.value)}
          />
          <TextField
            label="Password"
            type="password"
            fullWidth
            margin="normal"
            value={password}
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

          <Box sx={{ mt: 3, p: 2, bgcolor: 'grey.100', borderRadius: 1 }}>
            <Typography variant="caption" display="block" gutterBottom>
              <strong>Demo Accounts:</strong>
            </Typography>
            <Typography variant="caption" display="block">
              Customer: john@aucklandcafe.co.nz / password
            </Typography>
            <Typography variant="caption" display="block">
              Admin: admin@storycoffee.co.nz / password
            </Typography>
          </Box>
        </CardContent>
      </Card>
    </Box>
  );
}
