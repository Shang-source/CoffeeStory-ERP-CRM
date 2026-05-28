import { Box, Button, Card, CardContent, Chip, Divider, Grid, Stack, Typography } from '@mui/material';
import {
  ArrowForward,
  AssignmentTurnedIn,
  Factory,
  Inventory2,
  LocalShipping,
  Payment,
  PlayArrow,
  ReportProblem,
} from '@mui/icons-material';
import { useNavigate } from 'react-router';
import { useAdminDashboardQuery } from '@/entities/dashboard/api/dashboardQueries';
import { AdminDashboardProblemItem, Invoice, Order, ProductionItem } from '@/entities/types';
import { useBatchToProductionMutation } from '@/features/batchToProduction/model/batchToProductionMutations';
import { useBatchShipAndInvoiceMutation } from '@/features/orderWorkflow/model/orderWorkflowMutations';
import { LoadingState } from '@/shared/ui/LoadingState';
import { ErrorState } from '@/shared/ui/ErrorState';
import { EmptyState } from '@/shared/ui/EmptyState';
import { formatProductionStatus, getProductionStatusColor } from '@/shared/status/statusFormat';

const palette = {
  terracotta: '#C45A3B',
  olive: '#6B7A56',
  espresso: '#4B2E20',
  cream: '#F7F3EE',
  linen: '#FBF8F3',
  teal: '#16877D',
  amber: '#E2760C',
  blue: '#1976D2',
};

const dateFormatter = new Intl.DateTimeFormat('en-NZ', {
  month: 'short',
  day: 'numeric',
  year: 'numeric',
});

const compactDateFormatter = new Intl.DateTimeFormat('en-NZ', {
  month: 'short',
  day: 'numeric',
});

function formatMoney(value: number) {
  return `$${value.toFixed(2)}`;
}

function isValidDate(value: unknown): value is Date {
  return value instanceof Date && !Number.isNaN(value.getTime());
}

function formatBusinessWeek(from: Date, to: Date) {
  if (!isValidDate(from) || !isValidDate(to)) {
    return 'Current business week';
  }

  return `${dateFormatter.format(from)} – ${dateFormatter.format(to)}`;
}

function waitLabel(dates: Array<Date | undefined | null>, emptyLabel = 'No waiting items') {
  const validDates = dates.filter(isValidDate);

  if (validDates.length === 0) {
    return emptyLabel;
  }

  const oldest = validDates.reduce((oldestDate, date) => date < oldestDate ? date : oldestDate, validDates[0]);
  const days = Math.max(0, Math.floor((Date.now() - oldest.getTime()) / 86_400_000));
  if (days === 0) {
    return 'Oldest: today';
  }

  return `Oldest: ${days} day${days === 1 ? '' : 's'}`;
}

function customerName(orderOrInvoice: Order | Invoice) {
  return orderOrInvoice.customer?.businessName ?? 'Unknown customer';
}

function QueueCard({
  title,
  subtitle,
  icon,
  accent,
  count,
  metric,
  oldest,
  actionLabel,
  onAction,
  actionDisabled,
  children,
}: {
  title: string;
  subtitle: string;
  icon: React.ReactNode;
  accent: string;
  count: number;
  metric: string;
  oldest: string;
  actionLabel: string;
  onAction: () => void;
  actionDisabled?: boolean;
  children: React.ReactNode;
}) {
  return (
    <Card sx={{ height: '100%', borderRadius: 3, border: `1px solid ${accent}44`, boxShadow: `0 10px 24px ${accent}1f` }}>
      <CardContent>
        <Stack direction="row" spacing={2} alignItems="center" sx={{ mb: 2 }}>
          <Box sx={{ width: 48, height: 48, borderRadius: 2, bgcolor: `${accent}18`, color: accent, display: 'grid', placeItems: 'center' }}>
            {icon}
          </Box>
          <Box sx={{ minWidth: 0, flex: 1 }}>
            <Typography variant="h6">{title}</Typography>
            <Typography variant="body2" color="text.secondary">{subtitle}</Typography>
          </Box>
          <Chip label={count} sx={{ bgcolor: accent, color: 'white', fontWeight: 700 }} />
        </Stack>

        <Stack direction="row" spacing={2} sx={{ mb: 2 }}>
          <Box sx={{ flex: 1, p: 1.5, borderRadius: 2, bgcolor: palette.cream }}>
            <Typography variant="caption" color="text.secondary">Workload</Typography>
            <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>{metric}</Typography>
          </Box>
          <Box sx={{ flex: 1, p: 1.5, borderRadius: 2, bgcolor: palette.cream }}>
            <Typography variant="caption" color="text.secondary">Waiting</Typography>
            <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>{oldest}</Typography>
          </Box>
        </Stack>

        <Stack spacing={1} sx={{ minHeight: 206 }}>
          {children}
        </Stack>

        <Button
          fullWidth
          variant="contained"
          endIcon={<ArrowForward />}
          onClick={onAction}
          disabled={actionDisabled}
          sx={{ mt: 2, bgcolor: accent, '&:hover': { bgcolor: accent } }}
        >
          {actionLabel}
        </Button>
      </CardContent>
    </Card>
  );
}

function EmptyQueue({ message }: { message: string }) {
  return (
    <Box sx={{ p: 2, borderRadius: 2, bgcolor: '#F5F5F5', color: 'text.secondary' }}>
      <Typography variant="body2">{message}</Typography>
    </Box>
  );
}

function MoreRows({ count }: { count: number }) {
  if (count <= 0) {
    return null;
  }

  return <Typography variant="caption" color="text.secondary">+ {count} more item{count === 1 ? '' : 's'}</Typography>;
}

function OrderRows({ orders, mode }: { orders: Order[]; mode: 'production' | 'ship' }) {
  if (orders.length === 0) {
    return <EmptyQueue message={mode === 'production' ? 'No orders waiting for production.' : 'No orders ready to ship.'} />;
  }

  return (
    <>
      {orders.slice(0, 5).map((order) => (
        <Stack key={order.id} direction="row" justifyContent="space-between" spacing={2} sx={{ py: 0.8, borderBottom: '1px solid #EEE' }}>
          <Box sx={{ minWidth: 0 }}>
            <Typography variant="body2" sx={{ fontWeight: 700 }}>{order.orderNumber}</Typography>
            <Typography variant="caption" color="text.secondary">{customerName(order)}</Typography>
          </Box>
          <Typography variant="body2" sx={{ fontWeight: 700 }}>{formatMoney(order.totalAmount)}</Typography>
        </Stack>
      ))}
      <MoreRows count={orders.length - 5} />
    </>
  );
}

function ProductionRows({ items }: { items: ProductionItem[] }) {
  if (items.length === 0) {
    return <EmptyQueue message="No product lines are currently in production." />;
  }

  return (
    <>
      {items.slice(0, 5).map((item) => (
        <Stack key={item.id} direction="row" justifyContent="space-between" spacing={2} sx={{ py: 0.8, borderBottom: '1px solid #EEE' }}>
          <Box sx={{ minWidth: 0 }}>
            <Typography variant="body2" sx={{ fontWeight: 700 }}>{item.productName}</Typography>
            <Typography variant="caption" color="text.secondary">
              {item.relatedOrders.map((order) => order.customerName).join(', ')}
            </Typography>
          </Box>
          <Stack alignItems="flex-end" spacing={0.5}>
            <Typography variant="body2" sx={{ fontWeight: 700 }}>{item.producedQuantity} / {item.totalQuantity}</Typography>
            <Chip size="small" label={formatProductionStatus(item.status)} sx={{ bgcolor: getProductionStatusColor(item.status), color: 'white' }} />
          </Stack>
        </Stack>
      ))}
      <MoreRows count={items.length - 5} />
    </>
  );
}

function InvoiceRows({ invoices }: { invoices: Invoice[] }) {
  if (invoices.length === 0) {
    return <EmptyQueue message="No invoices are awaiting payment." />;
  }

  return (
    <>
      {invoices.slice(0, 5).map((invoice) => (
        <Stack key={invoice.id} direction="row" justifyContent="space-between" spacing={2} sx={{ py: 0.8, borderBottom: '1px solid #EEE' }}>
          <Box sx={{ minWidth: 0 }}>
            <Typography variant="body2" sx={{ fontWeight: 700 }}>{invoice.invoiceNumber}</Typography>
            <Typography variant="caption" color="text.secondary">
              {customerName(invoice)} · due {compactDateFormatter.format(invoice.dueDate)}
            </Typography>
          </Box>
          <Typography variant="body2" sx={{ fontWeight: 700 }}>{formatMoney(invoice.outstandingAmount)}</Typography>
        </Stack>
      ))}
      <MoreRows count={invoices.length - 5} />
    </>
  );
}

function ProblemRows({ problems }: { problems: AdminDashboardProblemItem[] }) {
  if (problems.length === 0) {
    return <EmptyQueue message="No failed emails, stale work, or overdue blockers." />;
  }

  return (
    <>
      {problems.slice(0, 5).map((problem) => (
        <Stack key={problem.id} direction="row" justifyContent="space-between" spacing={2} sx={{ py: 0.8, borderBottom: '1px solid #EEE' }}>
          <Box sx={{ minWidth: 0 }}>
            <Typography variant="body2" sx={{ fontWeight: 700 }}>{problem.title}</Typography>
            <Typography variant="caption" color="text.secondary">{problem.description}</Typography>
          </Box>
          <Chip size="small" label={problem.severity} color={problem.severity === 'Critical' ? 'error' : 'warning'} />
        </Stack>
      ))}
      <MoreRows count={problems.length - 5} />
    </>
  );
}

export default function AdminDashboard() {
  const navigate = useNavigate();
  const { data: dashboard, isLoading, error } = useAdminDashboardQuery();
  const batchToProduction = useBatchToProductionMutation(() => navigate('/admin/production'));
  const batchShipAndInvoice = useBatchShipAndInvoiceMutation();

  if (isLoading) {
    return <LoadingState />;
  }

  if (error) {
    return <ErrorState message={error instanceof Error ? error.message : 'Unable to load action center'} />;
  }

  if (!dashboard) {
    return <EmptyState title="No action center data available" />;
  }

  const needProductionOrders = dashboard.needProductionOrders;
  const productionItems = dashboard.productionItems;
  const readyToShipOrders = dashboard.readyToShipOrders;
  const awaitingPaymentInvoices = dashboard.awaitingPaymentInvoices;
  const problemItems = dashboard.problemItems;
  const outstanding = awaitingPaymentInvoices.reduce((sum, invoice) => sum + invoice.outstandingAmount, 0);
  const productionQuantity = productionItems.reduce((sum, item) => sum + Math.max(0, item.totalQuantity - item.producedQuantity), 0);

  return (
    <Box sx={{ mx: -1, p: { xs: 2, md: 4 }, borderRadius: 4, bgcolor: palette.linen, color: palette.espresso }}>
      <Stack direction={{ xs: 'column', md: 'row' }} justifyContent="space-between" alignItems={{ xs: 'flex-start', md: 'center' }} spacing={2} sx={{ mb: 3 }}>
        <Box>
          <Typography variant="h3" sx={{ fontFamily: 'Georgia, serif', color: palette.espresso }}>
            Action Center
          </Typography>
          <Typography variant="body1" color="text.secondary">
            Today’s work: {needProductionOrders.length} orders need production / {productionItems.length} product lines in production / {readyToShipOrders.length} orders ready to ship / {formatMoney(outstanding)} outstanding
          </Typography>
        </Box>
        <Chip
          label={`Business week: ${formatBusinessWeek(dashboard.businessWeek.from, dashboard.businessWeek.to)}`}
          sx={{ bgcolor: '#fff', border: '1px solid #D8C9B8', px: 1.5, py: 2.5, fontWeight: 600 }}
        />
      </Stack>

      <Card sx={{ mb: 3, borderRadius: 3, border: '1px solid #E6D8C8', bgcolor: '#fff' }}>
        <CardContent>
          <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} alignItems={{ md: 'center' }} justifyContent="space-between">
            <Stack spacing={0.5}>
              <Typography variant="h6">Daily workflow</Typography>
              <Typography variant="body2" color="text.secondary">
                Work left to right: send orders to production, finish product lines, ship ready orders, then collect payment.
              </Typography>
            </Stack>
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
              <Button
                variant="outlined"
                startIcon={<PlayArrow />}
                disabled={needProductionOrders.length === 0 || batchToProduction.isPending}
                onClick={() => batchToProduction.mutate(needProductionOrders.map((order) => order.id))}
              >
                Send all to production
              </Button>
              <Button
                variant="contained"
                startIcon={<LocalShipping />}
                disabled={readyToShipOrders.length === 0 || batchShipAndInvoice.isPending}
                onClick={() => batchShipAndInvoice.mutate(readyToShipOrders.map((order) => order.id))}
              >
                Ship all ready + send invoices
              </Button>
            </Stack>
          </Stack>
        </CardContent>
      </Card>

      <Grid container spacing={3}>
        <Grid size={{ xs: 12, md: 6, xl: 4 }}>
          <QueueCard
            title="Need Production"
            subtitle="Generated orders waiting to be batched"
            icon={<AssignmentTurnedIn />}
            accent={palette.teal}
            count={needProductionOrders.length}
            metric={`${needProductionOrders.length} order${needProductionOrders.length === 1 ? '' : 's'}`}
            oldest={waitLabel(needProductionOrders.map((order) => order.generatedAt))}
            actionLabel="Send all visible to production"
            actionDisabled={needProductionOrders.length === 0 || batchToProduction.isPending}
            onAction={() => batchToProduction.mutate(needProductionOrders.map((order) => order.id))}
          >
            <OrderRows orders={needProductionOrders} mode="production" />
          </QueueCard>
        </Grid>

        <Grid size={{ xs: 12, md: 6, xl: 4 }}>
          <QueueCard
            title="Production In Progress"
            subtitle="Product lines not completed yet"
            icon={<Factory />}
            accent={palette.amber}
            count={productionItems.length}
            metric={`${productionQuantity} unit${productionQuantity === 1 ? '' : 's'} left`}
            oldest="Open queue"
            actionLabel="Open production list"
            onAction={() => navigate('/admin/production')}
          >
            <ProductionRows items={productionItems} />
          </QueueCard>
        </Grid>

        <Grid size={{ xs: 12, md: 6, xl: 4 }}>
          <QueueCard
            title="Ready to Ship"
            subtitle="Orders ready for delivery and invoice email"
            icon={<LocalShipping />}
            accent={palette.blue}
            count={readyToShipOrders.length}
            metric={formatMoney(readyToShipOrders.reduce((sum, order) => sum + order.totalAmount, 0))}
            oldest={waitLabel(readyToShipOrders.map((order) => order.generatedAt), 'Open queue')}
            actionLabel="Ship all ready + send invoices"
            actionDisabled={readyToShipOrders.length === 0 || batchShipAndInvoice.isPending}
            onAction={() => batchShipAndInvoice.mutate(readyToShipOrders.map((order) => order.id))}
          >
            <OrderRows orders={readyToShipOrders} mode="ship" />
          </QueueCard>
        </Grid>

        <Grid size={{ xs: 12, md: 6 }}>
          <QueueCard
            title="Awaiting Payment"
            subtitle="Open invoices that need follow-up"
            icon={<Payment />}
            accent={palette.olive}
            count={awaitingPaymentInvoices.length}
            metric={formatMoney(outstanding)}
            oldest={waitLabel(awaitingPaymentInvoices.map((invoice) => invoice.dueDate))}
            actionLabel="Open payments"
            onAction={() => navigate('/admin/payments')}
          >
            <InvoiceRows invoices={awaitingPaymentInvoices} />
          </QueueCard>
        </Grid>

        <Grid size={{ xs: 12, md: 6 }}>
          <QueueCard
            title="Problems"
            subtitle="Failures and stale work requiring review"
            icon={<ReportProblem />}
            accent={palette.terracotta}
            count={problemItems.length}
            metric={`${problemItems.filter((problem) => problem.severity === 'Critical').length} critical`}
            oldest={waitLabel(problemItems.map((problem) => problem.createdAt))}
            actionLabel="Review problems"
            onAction={() => navigate(problemItems[0]?.targetPath ?? '/admin/logs')}
          >
            <ProblemRows problems={problemItems} />
          </QueueCard>
        </Grid>
      </Grid>

      <Divider sx={{ my: 3 }} />
      <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
        <Button startIcon={<Inventory2 />} onClick={() => navigate('/admin/orders')}>Open Orders</Button>
        <Button startIcon={<Factory />} onClick={() => navigate('/admin/production')}>Open Production</Button>
        <Button startIcon={<Payment />} onClick={() => navigate('/admin/payments')}>Open Payments</Button>
      </Stack>
    </Box>
  );
}
