import { useEffect, useState } from 'react';
import { Alert, Box, CircularProgress, Typography, Card, CardContent, Button, Chip, Divider, Grid } from '@mui/material';
import { Link } from 'react-router';
import { ShoppingCart, Receipt, Warning } from '@mui/icons-material';
import { useAuth } from '@/app/providers/AuthProvider';
import { CustomerDashboard as CustomerDashboardData } from '@/entities/types';
import { getCustomerDashboard } from '@/entities/dashboard/api/dashboardApi';

export default function CustomerDashboard() {
  const { user } = useAuth();
  const [dashboard, setDashboard] = useState<CustomerDashboardData | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    const loadDashboard = async () => {
      if (!user?.customerId) {
        setIsLoading(false);
        return;
      }

      try {
        setError('');
        setDashboard(await getCustomerDashboard());
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Unable to load dashboard');
      } finally {
        setIsLoading(false);
      }
    };

    void loadDashboard();
  }, [user?.customerId]);

  if (!user || !user.customerId) {
    return <Typography>Access denied</Typography>;
  }

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (error) {
    return <Alert severity="error">{error}</Alert>;
  }

  if (!dashboard) {
    return <Alert severity="info">No dashboard data available</Alert>;
  }

  const { standingOrder, recentInvoices } = dashboard;

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Welcome, {user.name}
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 4 }}>
        Manage your coffee orders and invoices
      </Typography>

      <Grid container spacing={3}>
        <Grid size={{ xs: 12, md: 4 }}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                <ShoppingCart sx={{ mr: 1, color: 'primary.main' }} />
                <Typography variant="h6">Standing Order</Typography>
              </Box>
              {standingOrder ? (
                <>
                  <Typography variant="body2" color="text.secondary" gutterBottom>
                    Status
                  </Typography>
                  <Chip label={standingOrder.status} color="success" size="small" sx={{ mb: 2 }} />
                  <Typography variant="body2" color="text.secondary" gutterBottom>
                    Closing Frequency
                  </Typography>
                  <Typography variant="body1" gutterBottom>
                    {standingOrder.frequency === 'Fortnightly' ? 'Fortnightly (Every 2 weeks)' : standingOrder.frequency}
                  </Typography>
                  <Typography variant="caption" color="text.secondary" display="block">
                    Orders auto-generated based on this schedule
                  </Typography>
                  <Typography variant="body2" color="text.secondary" gutterBottom>
                    Next Closing Date
                  </Typography>
                  <Typography variant="body1" gutterBottom>
                    {standingOrder.nextClosingDate.toLocaleDateString()}
                  </Typography>
                </>
              ) : (
                <Typography variant="body2" color="text.secondary">
                  No standing order configured
                </Typography>
              )}
              <Button
                component={Link}
                to="/customer/standing-order"
                variant="outlined"
                fullWidth
                sx={{ mt: 2 }}
              >
                Edit Standing Order
              </Button>
            </CardContent>
          </Card>
        </Grid>

        <Grid size={{ xs: 12, md: 4 }}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                <Receipt sx={{ mr: 1, color: 'warning.main' }} />
                <Typography variant="h6">Amount Due</Typography>
              </Box>
              <Typography variant="h3" color="primary" gutterBottom>
                ${dashboard.metrics.totalOutstanding.toFixed(2)}
              </Typography>
              <Typography variant="body2" color="text.secondary" gutterBottom>
                {dashboard.metrics.openInvoiceCount} unpaid invoice(s)
              </Typography>
              {dashboard.metrics.overdueInvoiceCount > 0 && (
                <Box sx={{ mt: 2, p: 1, bgcolor: 'error.light', borderRadius: 1 }}>
                  <Box sx={{ display: 'flex', alignItems: 'center' }}>
                    <Warning sx={{ mr: 1, fontSize: 18 }} />
                    <Typography variant="body2">
                      {dashboard.metrics.overdueInvoiceCount} overdue invoice(s)
                    </Typography>
                  </Box>
                </Box>
              )}
              <Button
                component={Link}
                to="/customer/invoices"
                variant="outlined"
                fullWidth
                sx={{ mt: 2 }}
              >
                View Invoices
              </Button>
            </CardContent>
          </Card>
        </Grid>

        <Grid size={{ xs: 12, md: 4 }}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Your Standing Order Items
              </Typography>
              <Divider sx={{ mb: 2 }} />
              {standingOrder?.items.length ? (
                standingOrder.items.map((item) => (
                  <Box key={item.id} sx={{ mb: 1.5 }}>
                    <Typography variant="body2">
                      {item.product.name} x {item.quantity}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      ${item.unitPrice.toFixed(2)} each
                    </Typography>
                  </Box>
                ))
              ) : (
                <Typography variant="body2" color="text.secondary">
                  No standing order items
                </Typography>
              )}
              <Divider sx={{ my: 2 }} />
              <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                <Typography variant="body2">Estimated Total</Typography>
                <Typography variant="body2">
                  ${dashboard.metrics.estimatedStandingOrderTotal.toFixed(2)}
                </Typography>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        <Grid size={{ xs: 12 }}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Recent Invoices
              </Typography>
              <Box sx={{ mt: 2 }}>
                {recentInvoices.map((invoice) => (
                  <Box
                    key={invoice.id}
                    sx={{
                      display: 'flex',
                      justifyContent: 'space-between',
                      alignItems: 'center',
                      py: 2,
                      borderBottom: '1px solid #e0e0e0'
                    }}
                  >
                    <Box>
                      <Typography variant="body1">
                        {invoice.invoiceNumber}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        Due: {invoice.dueDate.toLocaleDateString()}
                      </Typography>
                    </Box>
                    <Box sx={{ textAlign: 'right' }}>
                      <Typography variant="body1">
                        ${invoice.totalAmount.toFixed(2)}
                      </Typography>
                      <Chip
                        label={invoice.status}
                        size="small"
                        color={invoice.status === 'Overdue' ? 'error' : 'default'}
                      />
                    </Box>
                  </Box>
                ))}
              </Box>
              <Button
                component={Link}
                to="/customer/invoices"
                variant="text"
                fullWidth
                sx={{ mt: 2 }}
              >
                View All Invoices
              </Button>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
}
