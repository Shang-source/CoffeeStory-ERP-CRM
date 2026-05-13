import { Box, Typography, Grid, Card, CardContent, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Chip } from '@mui/material';
import { TrendingUp, ShoppingCart, LocalShipping, Warning } from '@mui/icons-material';
import { formatOrderStatus, getOrderStatusColor } from '@/shared/status/statusFormat';
import { useNavigate } from 'react-router';
import { useAdminDashboardQuery } from '@/entities/dashboard/api/dashboardQueries';
import { LoadingState } from '@/shared/ui/LoadingState';
import { ErrorState } from '@/shared/ui/ErrorState';
import { EmptyState } from '@/shared/ui/EmptyState';

export default function AdminDashboard() {
  const navigate = useNavigate();
  const { data: dashboard, isLoading, error } = useAdminDashboardQuery();

  if (isLoading) {
    return <LoadingState />;
  }

  if (error) {
    return <ErrorState message={error instanceof Error ? error.message : 'Unable to load dashboard'} />;
  }

  if (!dashboard) {
    return <EmptyState title="No dashboard data available" />;
  }

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Admin Dashboard
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 4 }}>
        Overview of your business operations
      </Typography>

      <Grid container spacing={3}>
        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <Card
            sx={{ cursor: 'pointer', '&:hover': { boxShadow: 4 } }}
            onClick={() => navigate('/admin/orders')}
          >
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                <TrendingUp sx={{ mr: 1, color: 'primary.main' }} />
                <Typography variant="body2" color="text.secondary">
                  This Week
                </Typography>
              </Box>
              <Typography variant="h4">
                {dashboard.metrics.ordersThisWeek}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Orders Generated
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <Card
            sx={{ cursor: 'pointer', '&:hover': { boxShadow: 4 } }}
            onClick={() => navigate('/admin/orders')}
          >
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                <ShoppingCart sx={{ mr: 1, color: 'warning.main' }} />
                <Typography variant="body2" color="text.secondary">
                  In Production
                </Typography>
              </Box>
              <Typography variant="h4">
                {dashboard.metrics.inProductionOrders}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Orders
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <Card
            sx={{ cursor: 'pointer', '&:hover': { boxShadow: 4 } }}
            onClick={() => navigate('/admin/orders')}
          >
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                <LocalShipping sx={{ mr: 1, color: 'success.main' }} />
                <Typography variant="body2" color="text.secondary">
                  Shipped
                </Typography>
              </Box>
              <Typography variant="h4">
                {dashboard.metrics.shippedThisWeek}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                This Week
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <Card
            sx={{ cursor: 'pointer', '&:hover': { boxShadow: 4 } }}
            onClick={() => navigate('/admin/invoices')}
          >
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                <Warning sx={{ mr: 1, color: 'error.main' }} />
                <Typography variant="body2" color="text.secondary">
                  Amount Due
                </Typography>
              </Box>
              <Typography variant="h4">
                ${dashboard.metrics.totalOutstanding.toFixed(0)}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {dashboard.metrics.unpaidInvoiceCount} Invoices
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid size={{ xs: 12, md: 8 }}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Recent Orders
              </Typography>
              <TableContainer>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>Order #</TableCell>
                      <TableCell>Customer</TableCell>
                      <TableCell>Date</TableCell>
                      <TableCell align="right">Amount</TableCell>
                      <TableCell>Status</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {dashboard.recentOrders.map((order) => (
                      <TableRow
                        key={order.id}
                        sx={{ cursor: 'pointer', '&:hover': { bgcolor: '#f5f5f5' } }}
                        onClick={() => navigate('/admin/orders')}
                      >
                        <TableCell>
                          <Typography variant="body2" sx={{ fontWeight: 500 }}>
                            {order.orderNumber}
                          </Typography>
                        </TableCell>
                        <TableCell>
                          <Typography
                            variant="body2"
                            onClick={(e) => {
                              e.stopPropagation();
                              navigate(`/admin/customers/${order.customer?.id}`);
                            }}
                            sx={{
                              cursor: 'pointer',
                              '&:hover': { textDecoration: 'underline', color: 'primary.main' }
                            }}
                          >
                            {order.customer?.businessName}
                          </Typography>
                        </TableCell>
                        <TableCell>{order.generatedAt.toLocaleDateString()}</TableCell>
                        <TableCell align="right">${order.totalAmount.toFixed(2)}</TableCell>
                        <TableCell>
                          <Chip
                            label={formatOrderStatus(order.orderStatus)}
                            size="small"
                            sx={{ bgcolor: getOrderStatusColor(order.orderStatus), color: 'white' }}
                          />
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            </CardContent>
          </Card>
        </Grid>

        <Grid size={{ xs: 12, md: 4 }}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Overdue Invoices
              </Typography>
              {dashboard.overdueInvoices.length > 0 ? (
                <Box>
                  {dashboard.overdueInvoices.map((invoice) => (
                    <Box
                      key={invoice.id}
                      sx={{
                        py: 1.5,
                        borderBottom: '1px solid #e0e0e0',
                        cursor: 'pointer',
                        '&:hover': { bgcolor: '#f5f5f5' }
                      }}
                      onClick={() => navigate('/admin/invoices')}
                    >
                      <Typography variant="body2">
                        {invoice.customer?.businessName}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {invoice.invoiceNumber} - ${invoice.outstandingAmount.toFixed(2)}
                      </Typography>
                    </Box>
                  ))}
                </Box>
              ) : (
                <Typography variant="body2" color="text.secondary">
                  No overdue invoices
                </Typography>
              )}
            </CardContent>
          </Card>

          <Card
            sx={{ mt: 2, cursor: 'pointer', '&:hover': { boxShadow: 4 } }}
            onClick={() => navigate('/admin/customers')}
          >
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Active Customers
              </Typography>
              <Typography variant="h3" color="primary">
                {dashboard.metrics.activeCustomerCount}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Total customers: {dashboard.metrics.totalCustomerCount}
              </Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
}
