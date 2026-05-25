import { Box, Button, Card, CardContent, Chip, Grid, Stack, Typography } from '@mui/material';
import { AssignmentTurnedIn, CalendarMonth, Groups, LocalShipping, ReceiptLong, ShoppingCart, TrendingUp, WarningAmber } from '@mui/icons-material';
import { formatOrderStatus, getOrderStatusColor } from '@/shared/status/statusFormat';
import { useNavigate } from 'react-router';
import { useAdminDashboardQuery } from '@/entities/dashboard/api/dashboardQueries';
import { LoadingState } from '@/shared/ui/LoadingState';
import { ErrorState } from '@/shared/ui/ErrorState';
import { EmptyState } from '@/shared/ui/EmptyState';

const palette = {
  terracotta: '#C45A3B',
  olive: '#6B7A56',
  espresso: '#4B2E20',
  cream: '#F7F3EE',
  linen: '#FBF8F3',
  sage: '#CFE1C6',
  mint: '#D9EFE6',
  blush: '#F6B5AE',
};

function MetricCard({
  title,
  value,
  subtitle,
  icon,
  accent,
  onClick,
}: {
  title: string;
  value: string | number;
  subtitle: string;
  icon: React.ReactNode;
  accent: string;
  onClick: () => void;
}) {
  return (
    <Card
      onClick={onClick}
      sx={{
        height: '100%',
        cursor: 'pointer',
        border: `1px solid ${accent}55`,
        background: `linear-gradient(135deg, #ffffff 0%, ${accent}1A 100%)`,
        boxShadow: `0 10px 26px ${accent}24`,
        borderRadius: 3,
      }}
    >
      <CardContent>
        <Stack direction="row" spacing={2} alignItems="center">
          <Box sx={{ width: 52, height: 52, borderRadius: '50%', bgcolor: accent, color: 'white', display: 'grid', placeItems: 'center' }}>
            {icon}
          </Box>
          <Box>
            <Typography variant="body2" color="text.secondary">{title}</Typography>
            <Typography variant="h3" sx={{ fontFamily: 'Georgia, serif', color: palette.espresso }}>{value}</Typography>
            <Typography variant="body2" color="text.secondary">{subtitle}</Typography>
          </Box>
        </Stack>
      </CardContent>
    </Card>
  );
}

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
    <Box sx={{ mx: -1, p: { xs: 2, md: 4 }, borderRadius: 4, bgcolor: palette.linen, color: palette.espresso }}>
      <Stack direction={{ xs: 'column', md: 'row' }} justifyContent="space-between" alignItems={{ xs: 'flex-start', md: 'center' }} spacing={2} sx={{ mb: 4 }}>
        <Box>
          <Typography variant="h3" sx={{ fontFamily: 'Georgia, serif', color: palette.espresso }}>
            Welcome back
          </Typography>
          <Typography variant="body1" color="text.secondary">Here’s an overview of your business operations.</Typography>
        </Box>
        <Button variant="outlined" startIcon={<CalendarMonth />} sx={{ color: palette.espresso, borderColor: '#D8C9B8', bgcolor: '#fff' }}>
          May 8 – May 14, 2026
        </Button>
      </Stack>

      <Grid container spacing={3}>
        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <MetricCard title="Orders Generated" value={dashboard.metrics.ordersThisWeek} subtitle="This week" icon={<TrendingUp />} accent="#16877D" onClick={() => navigate('/admin/orders')} />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <MetricCard title="In Production" value={dashboard.metrics.inProductionOrders} subtitle="Orders" icon={<ShoppingCart />} accent="#E2760C" onClick={() => navigate('/admin/orders')} />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <MetricCard title="Shipped" value={dashboard.metrics.shippedThisWeek} subtitle="This week" icon={<LocalShipping />} accent={palette.olive} onClick={() => navigate('/admin/orders')} />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <MetricCard title="Amount Due" value={`$${dashboard.metrics.totalOutstanding.toFixed(0)}`} subtitle={`${dashboard.metrics.unpaidInvoiceCount} invoices`} icon={<WarningAmber />} accent={palette.terracotta} onClick={() => navigate('/admin/invoices')} />
        </Grid>

        <Grid size={{ xs: 12, md: 8 }}>
          <Card sx={{ borderRadius: 3, border: '1px solid #E6D8C8', boxShadow: '0 12px 30px rgba(75,46,32,0.08)' }}>
            <CardContent>
              <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
                <Stack direction="row" spacing={1.5} alignItems="center">
                  <AssignmentTurnedIn sx={{ color: palette.espresso }} />
                  <Typography variant="h5" sx={{ fontFamily: 'Georgia, serif' }}>Recent Orders</Typography>
                </Stack>
                <Button size="small" onClick={() => navigate('/admin/orders')} sx={{ color: palette.terracotta }}>View all orders</Button>
              </Stack>
              <Stack divider={<Box sx={{ borderBottom: '1px solid #EADFD2' }} />}>
                {dashboard.recentOrders.slice(0, 6).map((order) => (
                  <Stack key={order.id} direction={{ xs: 'column', md: 'row' }} spacing={2} justifyContent="space-between" sx={{ py: 1.4, cursor: 'pointer' }} onClick={() => navigate('/admin/orders')}>
                    <Typography variant="body2" sx={{ width: { md: 210 }, fontWeight: 600 }}>{order.orderNumber}</Typography>
                    <Typography variant="body2" sx={{ flex: 1 }}>{order.customer?.businessName}</Typography>
                    <Typography variant="body2">{order.generatedAt.toLocaleDateString()}</Typography>
                    <Typography variant="body2" sx={{ width: 90, textAlign: { md: 'right' } }}>${order.totalAmount.toFixed(2)}</Typography>
                    <Chip label={formatOrderStatus(order.orderStatus)} size="small" sx={{ bgcolor: getOrderStatusColor(order.orderStatus), color: 'white', minWidth: 98 }} />
                  </Stack>
                ))}
              </Stack>
            </CardContent>
          </Card>
        </Grid>

        <Grid size={{ xs: 12, md: 4 }}>
          <Stack spacing={3}>
            <Card sx={{ borderRadius: 3, border: '1px solid #F0D2C8', bgcolor: '#FFFDFC' }}>
              <CardContent>
                <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
                  <Stack direction="row" spacing={1.5} alignItems="center">
                    <ReceiptLong sx={{ color: palette.terracotta }} />
                    <Typography variant="h5" sx={{ fontFamily: 'Georgia, serif' }}>Overdue Invoices</Typography>
                  </Stack>
                  <Button size="small" onClick={() => navigate('/admin/invoices')} sx={{ color: palette.terracotta }}>View all</Button>
                </Stack>
                {dashboard.overdueInvoices.length === 0 ? (
                  <Box sx={{ p: 3, borderRadius: 2, bgcolor: palette.cream, color: palette.espresso }}>
                    <Typography>No overdue invoices</Typography>
                    <Typography variant="body2" color="text.secondary">You’re all caught up.</Typography>
                  </Box>
                ) : (
                  <Stack spacing={1}>
                    {dashboard.overdueInvoices.slice(0, 4).map((invoice) => (
                      <Box key={invoice.id} sx={{ p: 1.5, borderRadius: 2, bgcolor: '#FFF4EF', cursor: 'pointer' }} onClick={() => navigate('/admin/invoices')}>
                        <Typography variant="body2" sx={{ fontWeight: 600 }}>{invoice.customer?.businessName}</Typography>
                        <Typography variant="caption" color="text.secondary">{invoice.invoiceNumber} · ${invoice.outstandingAmount.toFixed(2)}</Typography>
                      </Box>
                    ))}
                  </Stack>
                )}
              </CardContent>
            </Card>

            <Card sx={{ borderRadius: 3, border: '1px solid #D9E7CE', background: `linear-gradient(135deg, #fff 0%, ${palette.sage}80 100%)` }}>
              <CardContent>
                <Stack direction="row" spacing={1.5} alignItems="center" sx={{ mb: 1 }}>
                  <Groups sx={{ color: palette.olive }} />
                  <Typography variant="h5" sx={{ fontFamily: 'Georgia, serif' }}>Active Customers</Typography>
                </Stack>
                <Typography variant="h2" sx={{ color: palette.olive, fontFamily: 'Georgia, serif' }}>{dashboard.metrics.activeCustomerCount}</Typography>
                <Typography variant="body2" color="text.secondary">Total customers: {dashboard.metrics.totalCustomerCount}</Typography>
              </CardContent>
            </Card>
          </Stack>
        </Grid>
      </Grid>
    </Box>
  );
}
