import { Alert, Box, CircularProgress, Typography, Card, CardContent, TextField, Button, Grid, Divider } from '@mui/material';
import { toast } from 'sonner';
import { useAuth } from '@/app/providers/AuthProvider';
import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router';
import { Customer } from '@/entities/types';
import { useCustomerProfileQuery } from '@/entities/customer/api/customerQueries';
import { useUpdateCustomerProfileMutation } from '@/features/customerEdit/model/customerEditMutations';
import { useChangeCustomerPasswordMutation } from '@/features/passwordChange/model/passwordChangeMutations';

export default function AccountSettings() {
  const navigate = useNavigate();
  const { user, logout } = useAuth();
  const profileQuery = useCustomerProfileQuery(Boolean(user?.customerId));
  const [customer, setCustomer] = useState<Customer | null>(null);
  const [passwordForm, setPasswordForm] = useState({
    currentPassword: '',
    newPassword: '',
    confirmNewPassword: '',
  });

  useEffect(() => {
    if (profileQuery.data) {
      setCustomer(profileQuery.data);
    }
  }, [profileQuery.data]);

  const updateProfileMutation = useUpdateCustomerProfileMutation(setCustomer);
  const changePasswordMutation = useChangeCustomerPasswordMutation(() => {
    logout();
    navigate('/');
  });

  const handleSave = async () => {
    if (!customer) {
      return;
    }

    try {
      await updateProfileMutation.mutateAsync(customer);
    } catch {
      return;
    }
  };

  const handleChange = (field: keyof Customer, value: string) => {
    if (!customer) {
      return;
    }

    setCustomer({ ...customer, [field]: value });
  };

  const handlePasswordChange = async () => {
    if (passwordForm.newPassword.length < 8) {
      toast.error('New password must be at least 8 characters');
      return;
    }

    if (passwordForm.newPassword !== passwordForm.confirmNewPassword) {
      toast.error('New passwords do not match');
      return;
    }

    try {
      await changePasswordMutation.mutateAsync(passwordForm);
    } catch {
      return;
    }
  };

  if (!user || !user.customerId) {
    return <Typography>Access denied</Typography>;
  }

  if (profileQuery.isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (profileQuery.error || !customer) {
    return <Alert severity="error">{profileQuery.error instanceof Error ? profileQuery.error.message : 'Account settings not found'}</Alert>;
  }

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Account Settings
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
        Manage your account information
      </Typography>

      <Card>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Business Information
          </Typography>
          <Divider sx={{ mb: 3 }} />

          <Grid container spacing={3}>
            <Grid size={{ xs: 12, md: 6 }}>
              <TextField
                label="Business Name"
                fullWidth
                value={customer.businessName}
                onChange={(event) => handleChange('businessName', event.target.value)}
              />
            </Grid>
            <Grid size={{ xs: 12, md: 6 }}>
              <TextField
                label="Contact Person"
                fullWidth
                value={customer.contactPerson}
                onChange={(event) => handleChange('contactPerson', event.target.value)}
              />
            </Grid>
            <Grid size={{ xs: 12, md: 6 }}>
              <TextField
                label="Email"
                type="email"
                fullWidth
                value={customer.email}
                onChange={(event) => handleChange('email', event.target.value)}
              />
            </Grid>
            <Grid size={{ xs: 12, md: 6 }}>
              <TextField
                label="Phone"
                fullWidth
                value={customer.phone}
                onChange={(event) => handleChange('phone', event.target.value)}
              />
            </Grid>
            <Grid size={{ xs: 12 }}>
              <TextField
                label="Billing Address"
                fullWidth
                multiline
                rows={2}
                value={customer.billingAddress}
                onChange={(event) => handleChange('billingAddress', event.target.value)}
              />
            </Grid>
            <Grid size={{ xs: 12 }}>
              <TextField
                label="Delivery Address"
                fullWidth
                multiline
                rows={2}
                value={customer.deliveryAddress}
                onChange={(event) => handleChange('deliveryAddress', event.target.value)}
              />
            </Grid>
          </Grid>

          <Box sx={{ mt: 3 }}>
            <Button variant="contained" onClick={handleSave} disabled={updateProfileMutation.isPending}>
              Save Changes
            </Button>
          </Box>
        </CardContent>
      </Card>

      <Card sx={{ mt: 3 }}>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Payment Terms
          </Typography>
          <Divider sx={{ mb: 3 }} />

          <Box sx={{ p: 2, bgcolor: 'grey.100', borderRadius: 1 }}>
            <Typography variant="body2" color="text.secondary" gutterBottom>
              Your account payment terms
            </Typography>
            <Typography variant="h6" gutterBottom>
              {customer.paymentTerms}
            </Typography>
            <Typography variant="caption" color="text.secondary">
              Payment terms are set by StoryCoffee based on your business agreement.
              If you need to discuss payment arrangements, please contact our accounts team.
            </Typography>
          </Box>
        </CardContent>
      </Card>

      <Card sx={{ mt: 3 }}>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Change Password
          </Typography>
          <Divider sx={{ mb: 3 }} />

          <Grid container spacing={3}>
            <Grid size={{ xs: 12 }}>
              <TextField
                label="Current Password"
                type="password"
                fullWidth
                value={passwordForm.currentPassword}
                onChange={(event) => setPasswordForm({ ...passwordForm, currentPassword: event.target.value })}
              />
            </Grid>
            <Grid size={{ xs: 12, md: 6 }}>
              <TextField
                label="New Password"
                type="password"
                fullWidth
                value={passwordForm.newPassword}
                onChange={(event) => setPasswordForm({ ...passwordForm, newPassword: event.target.value })}
              />
            </Grid>
            <Grid size={{ xs: 12, md: 6 }}>
              <TextField
                label="Confirm New Password"
                type="password"
                fullWidth
                value={passwordForm.confirmNewPassword}
                onChange={(event) => setPasswordForm({ ...passwordForm, confirmNewPassword: event.target.value })}
              />
            </Grid>
          </Grid>

          <Box sx={{ mt: 3 }}>
            <Button
              variant="contained"
              onClick={handlePasswordChange}
              disabled={changePasswordMutation.isPending || !passwordForm.currentPassword || !passwordForm.newPassword || !passwordForm.confirmNewPassword}
            >
              Update Password
            </Button>
          </Box>
        </CardContent>
      </Card>
    </Box>
  );
}
