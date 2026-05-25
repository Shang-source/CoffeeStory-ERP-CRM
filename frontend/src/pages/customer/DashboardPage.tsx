import { Alert, Box, Typography, Card, CardContent, Button, Divider, Grid } from '@mui/material';
import { Link } from 'react-router';
import { ShoppingCart, Receipt, Warning } from '@mui/icons-material';
import { useAuth } from '@/app/providers/AuthProvider';
import { useCustomerProfileQuery } from '@/entities/customer/api/customerQueries';
import { useCustomerDashboardQuery } from '@/entities/dashboard/api/dashboardQueries';
import { LoadingState } from '@/shared/ui/LoadingState';
import { ErrorState } from '@/shared/ui/ErrorState';
import { EmptyState } from '@/shared/ui/EmptyState';
import { formatAccountStatus, formatInvoiceStatus, formatStandingOrderStatus, getAccountStatusColor, getInvoiceStatusColor, getStandingOrderStatusColor } from '@/shared/status/statusFormat';
import { StatusChip } from '@/shared/ui/StatusChip';

export default function CustomerDashboard() {
  const { user } = useAuth();
  const { data: dashboard, isLoading, error } = useCustomerDashboardQuery(Boolean(user?.customerId));
  const profileQuery = useCustomerProfileQuery(Boolean(user?.customerId));

  if (!user || !user.customerId) {
    return <Typography>Access denied</Typography>;
  }

  if (isLoading) {
    return <LoadingState />;
  }

  if (error) {
    return <ErrorState message={error instanceof Error ? error.message : 'Unable to load dashboard'} />;
  }

  if (!dashboard) {
    return <EmptyState title="No dashboard data available" />;
  }

  const { standingOrder, recentInvoices } = dashboard;
  const profile = profileQuery.data;

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Welcome, {user.name}
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 4 }}>
        Manage your coffee orders and invoices
      </Typography>

      {profile && (
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 3 }}>
          <Typography variant="body2" color="text.secondary">
            Account Status
          </Typography>
          <StatusChip label={formatAccountStatus(profile.accountStatus)} color={getAccountStatusColor(profile.accountStatus)} />
        </Box>
      )}

      {profile && profile.accountStatus !== 'Active' && (
        <Alert severity="info" sx={{ mb: 3 }}>
          Your account is {formatAccountStatus(profile.accountStatus)}. Standing order generation is enabled only after StoryCoffee marks your account as Active.
        </Alert>
      )}

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
                  <Box sx={{ mb: 2 }}>
                    <StatusChip label={formatStandingOrderStatus(standingOrder.status)} color={getStandingOrderStatusColor(standingOrder.status)} />
                  </Box>
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
                    component={Link}
                    to={`/customer/invoices/${invoice.id}`}
                    sx={{
                      display: 'flex',
                      justifyContent: 'space-between',
                      alignItems: 'center',
                      py: 2,
                      borderBottom: '1px solid #e0e0e0',
                      color: 'inherit',
                      textDecoration: 'none',
                      '&:hover': { bgcolor: 'grey.50' },
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
                      <StatusChip label={formatInvoiceStatus(invoice.status)} color={getInvoiceStatusColor(invoice.status)} />
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
