import { useMemo, useState } from 'react';
import { Alert, Box, Button, Card, CardContent, Checkbox, Chip, Collapse, FormControlLabel, IconButton, Menu, MenuItem, Stack, Switch, Tab, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, TableSortLabel, Tabs, TextField, Toolbar, Typography } from '@mui/material';
import { KeyboardArrowDown, KeyboardArrowUp, MoreVert, PlayArrow, LocalShipping } from '@mui/icons-material';
import { formatInvoiceStatus, getInvoiceStatusColor } from '@/shared/status/statusFormat';
import { toast } from 'sonner';
import { Order, OrderQueryParams } from '@/entities/types';
import { useNavigate, useSearchParams } from 'react-router';
import ConfirmDialog from '@/shared/ui/ConfirmDialog/ConfirmDialog';
import { useAdminOrdersQuery } from '@/entities/order/api/orderQueries';
import { useBatchToProductionMutation } from '@/features/batchToProduction/model/batchToProductionMutations';
import { cancelOrder } from '@/features/orderWorkflow/api/orderWorkflowApi';
import { useBatchShipAndInvoiceMutation, useOrderWorkflowMutation } from '@/features/orderWorkflow/model/orderWorkflowMutations';
import { LoadingState } from '@/shared/ui/LoadingState';
import { ErrorState } from '@/shared/ui/ErrorState';

type OrderTab = 'all' | 'needProduction' | 'inProduction' | 'readyToShip' | 'awaitingPayment' | 'completed';
type OrderSortField = 'orderNumber' | 'customer' | 'generatedAt' | 'totalAmount';
type SortDirection = 'asc' | 'desc';

const isAwaitingPayment = (order: Order) => ['Unpaid', 'PartiallyPaid', 'Overdue'].includes(order.invoiceStatus);
const isCompleted = (order: Order) => order.orderStatus === 'Completed' || order.invoiceStatus === 'Paid';

const orderTabs: Array<{ value: OrderTab; label: string; predicate: (order: Order) => boolean }> = [
  { value: 'needProduction', label: 'Need Production', predicate: (order) => order.orderStatus === 'Generated' },
  { value: 'inProduction', label: 'In Production', predicate: (order) => order.orderStatus === 'InProduction' },
  { value: 'readyToShip', label: 'Ready to Ship', predicate: (order) => order.orderStatus === 'ReadyToShip' },
  { value: 'awaitingPayment', label: 'Awaiting Payment', predicate: isAwaitingPayment },
  { value: 'completed', label: 'Completed', predicate: isCompleted },
  { value: 'all', label: 'All', predicate: () => true },
];

const workflowStage = (order: Order): { label: string; color: string } => {
  if (order.orderStatus === 'Cancelled') {
    return { label: 'Cancelled', color: '#757575' };
  }
  if (isCompleted(order)) {
    return { label: 'Completed', color: '#009688' };
  }
  if (isAwaitingPayment(order)) {
    return { label: 'Awaiting Payment', color: '#673AB7' };
  }
  if (order.orderStatus === 'ReadyToShip') {
    return { label: 'Ready to Ship', color: '#2196F3' };
  }
  if (order.orderStatus === 'InProduction') {
    return { label: 'In Production', color: '#FF9800' };
  }
  if (order.orderStatus === 'Shipped') {
    return { label: 'Shipped', color: '#4CAF50' };
  }
  return { label: 'Need Production', color: '#9E9E9E' };
};

function formatDate(value: Date) {
  return new Intl.DateTimeFormat('en-NZ', { day: '2-digit', month: '2-digit', year: 'numeric' }).format(value);
}

interface OrderRowProps {
  order: Order;
  selected: boolean;
  onSelect: (orderId: string, checked: boolean) => void;
  onOrderAction: (action: () => Promise<Order>, successMessage: string) => Promise<void>;
}

function OrderRow({ order, selected, onSelect, onOrderAction }: OrderRowProps) {
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [isCancelConfirmOpen, setIsCancelConfirmOpen] = useState(false);

  const handleMenuClose = () => setAnchorEl(null);
  const handleViewInvoice = () => {
    handleMenuClose();
    navigate('/admin/invoices');
  };
  const handleViewCustomer = () => {
    handleMenuClose();
    navigate(`/admin/customers/${order.customerId}`);
  };
  const handleViewProduction = () => {
    handleMenuClose();
    navigate('/admin/production');
  };

  const actions: Array<{ label: string; handler: () => void }> = [
    { label: 'View Customer', handler: handleViewCustomer },
  ];
  if (['Generated', 'InProduction', 'ReadyToShip'].includes(order.orderStatus)) {
    actions.push({ label: 'View Production List', handler: handleViewProduction });
  }
  if (order.invoiceStatus !== 'NotIssued') {
    actions.push({ label: 'View Invoice', handler: handleViewInvoice });
  }
  if (['Unpaid', 'PartiallyPaid', 'Overdue'].includes(order.invoiceStatus)) {
    actions.push({ label: 'Record Payment', handler: () => { handleMenuClose(); navigate('/admin/payments'); } });
  }
  if (!['Cancelled', 'Completed', 'Shipped'].includes(order.orderStatus)) {
    actions.push({ label: 'Cancel Order', handler: () => { handleMenuClose(); setIsCancelConfirmOpen(true); } });
  }

  return (
    <>
      <TableRow selected={selected}>
        <TableCell padding="checkbox">
          <Checkbox checked={selected} onChange={(event) => onSelect(order.id, event.target.checked)} />
        </TableCell>
        <TableCell>
          <IconButton size="small" onClick={() => setOpen(!open)}>
            {open ? <KeyboardArrowUp /> : <KeyboardArrowDown />}
          </IconButton>
        </TableCell>
        <TableCell>
          <Typography variant="body2" sx={{ fontWeight: 600 }}>{order.orderNumber}</Typography>
        </TableCell>
        <TableCell>
          <Button variant="text" size="small" onClick={handleViewCustomer} sx={{ textTransform: 'none', p: 0, minWidth: 'auto' }}>
            {order.customer?.businessName}
          </Button>
        </TableCell>
        <TableCell>{formatDate(order.generatedAt)}</TableCell>
        <TableCell align="right">${order.totalAmount.toFixed(2)}</TableCell>
        <TableCell><Chip label={workflowStage(order).label} size="small" sx={{ bgcolor: workflowStage(order).color, color: 'white' }} /></TableCell>
        <TableCell><Chip label={formatInvoiceStatus(order.invoiceStatus)} size="small" sx={{ bgcolor: getInvoiceStatusColor(order.invoiceStatus), color: 'white' }} /></TableCell>
        <TableCell>
          <IconButton size="small" onClick={(event) => setAnchorEl(event.currentTarget)}>
            <MoreVert />
          </IconButton>
          <Menu anchorEl={anchorEl} open={Boolean(anchorEl)} onClose={handleMenuClose}>
            {actions.map((action) => (
              <MenuItem key={action.label} onClick={action.handler}>{action.label}</MenuItem>
            ))}
          </Menu>
        </TableCell>
      </TableRow>
      <TableRow>
        <TableCell style={{ paddingBottom: 0, paddingTop: 0 }} colSpan={9}>
          <Collapse in={open} timeout="auto" unmountOnExit>
            <Box sx={{ m: 2 }}>
              <Typography variant="subtitle2" gutterBottom>Order Items</Typography>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Product</TableCell>
                    <TableCell>SKU</TableCell>
                    <TableCell align="right">Quantity</TableCell>
                    <TableCell align="right">Unit Price</TableCell>
                    <TableCell align="right">Total</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {order.items.map((item) => (
                    <TableRow key={item.id}>
                      <TableCell>{item.productNameSnapshot}</TableCell>
                      <TableCell>{item.skuSnapshot}</TableCell>
                      <TableCell align="right">{item.quantity}</TableCell>
                      <TableCell align="right">${item.unitPriceSnapshot.toFixed(2)}</TableCell>
                      <TableCell align="right">${item.lineTotal.toFixed(2)}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </Box>
          </Collapse>
        </TableCell>
      </TableRow>
      <ConfirmDialog
        open={isCancelConfirmOpen}
        title="Cancel Order"
        message={`Cancel order ${order.orderNumber}? This cannot be undone.`}
        confirmLabel="Cancel Order"
        confirmColor="error"
        onCancel={() => setIsCancelConfirmOpen(false)}
        onConfirm={() => {
          setIsCancelConfirmOpen(false);
          void onOrderAction(() => cancelOrder(order.id), 'Order cancelled');
        }}
      />
    </>
  );
}

export default function Orders() {
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const [tab, setTab] = useState<OrderTab>(() => normalizeOrderTab(searchParams.get('tab')) ?? 'needProduction');
  const [filters, setFilters] = useState<OrderQueryParams>({});
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [sortField, setSortField] = useState<OrderSortField>('generatedAt');
  const [sortDirection, setSortDirection] = useState<SortDirection>('desc');
  const [showCancelled, setShowCancelled] = useState(false);
  const [selectedOrderIds, setSelectedOrderIds] = useState<Set<string>>(new Set());
  const { data: orders = [], isLoading, error } = useAdminOrdersQuery(filters);
  const orderActionMutation = useOrderWorkflowMutation();
  const batchToProductionMutation = useBatchToProductionMutation(() => navigate('/admin/production'));
  const batchShipAndInvoiceMutation = useBatchShipAndInvoiceMutation();

  const currentTab = orderTabs.find((item) => item.value === tab) ?? orderTabs[0];
  const activeOrders = useMemo(() => orders.filter((order) => order.orderStatus !== 'Cancelled'), [orders]);
  const visibleOrders = useMemo(() => {
    const sourceOrders = tab === 'all' && showCancelled ? orders : activeOrders;
    return sourceOrders
      .filter(currentTab.predicate)
      .filter((order) => isWithinDateRange(order.generatedAt, fromDate, toDate))
      .sort((left, right) => compareOrders(left, right, sortField, sortDirection));
  }, [activeOrders, orders, currentTab, showCancelled, tab, fromDate, toDate, sortField, sortDirection]);
  const selectedOrders = orders.filter((order) => selectedOrderIds.has(order.id));
  const selectedGeneratedOrders = selectedOrders.filter((order) => order.orderStatus === 'Generated');
  const selectedReadyToShipOrders = selectedOrders.filter((order) => order.orderStatus === 'ReadyToShip');

  const tabCounts = useMemo(() => Object.fromEntries(orderTabs.map((item) => {
    const sourceOrders = item.value === 'all' && showCancelled ? orders : activeOrders;
    return [item.value, sourceOrders.filter(item.predicate).length];
  })), [activeOrders, orders, showCancelled]);
  const allVisibleSelected = visibleOrders.length > 0 && visibleOrders.every((order) => selectedOrderIds.has(order.id));
  const someVisibleSelected = visibleOrders.some((order) => selectedOrderIds.has(order.id)) && !allVisibleSelected;

  const handleSelect = (orderId: string, checked: boolean) => {
    setSelectedOrderIds((current) => {
      const next = new Set(current);
      if (checked) {
        next.add(orderId);
      } else {
        next.delete(orderId);
      }
      return next;
    });
  };

  const handleSelectVisible = (checked: boolean) => {
    setSelectedOrderIds((current) => {
      const next = new Set(current);
      visibleOrders.forEach((order) => checked ? next.add(order.id) : next.delete(order.id));
      return next;
    });
  };

  const handleSort = (field: OrderSortField) => {
    if (sortField === field) {
      setSortDirection(sortDirection === 'asc' ? 'desc' : 'asc');
      return;
    }

    setSortField(field);
    setSortDirection(field === 'totalAmount' || field === 'generatedAt' ? 'desc' : 'asc');
  };

  const handleOrderAction = async (action: () => Promise<Order>, successMessage: string) => {
    try {
      await orderActionMutation.mutateAsync({ action, successMessage });
    } catch {
      return;
    }
  };

  const handleBatchSendToProduction = async () => {
    if (selectedGeneratedOrders.length === 0) {
      toast.info('Select generated orders to send to production');
      return;
    }
    await batchToProductionMutation.mutateAsync(selectedGeneratedOrders.map((order) => order.id));
    setSelectedOrderIds(new Set());
  };

  const handleBatchShipAndInvoice = async () => {
    if (selectedReadyToShipOrders.length === 0) {
      toast.info('Select ready-to-ship orders to ship and invoice');
      return;
    }
    await batchShipAndInvoiceMutation.mutateAsync(selectedReadyToShipOrders.map((order) => order.id));
    setSelectedOrderIds(new Set());
  };

  if (isLoading) {
    return <LoadingState />;
  }

  if (error) {
    return <ErrorState message={error instanceof Error ? error.message : 'Unable to load orders'} />;
  }

  return (
    <Box>
      <Stack direction={{ xs: 'column', md: 'row' }} justifyContent="space-between" alignItems={{ xs: 'stretch', md: 'flex-start' }} spacing={2} sx={{ mb: 3 }}>
        <Box>
          <Typography variant="h4" gutterBottom>Orders</Typography>
          <Typography variant="body1" color="text.secondary">
            Simple queues for production, shipping, and payment follow-up
          </Typography>
        </Box>
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
          <Button variant="contained" startIcon={<PlayArrow />} onClick={handleBatchSendToProduction} disabled={selectedGeneratedOrders.length === 0 || batchToProductionMutation.isPending}>
            Send selected to production ({selectedGeneratedOrders.length})
          </Button>
          <Button variant="contained" color="success" startIcon={<LocalShipping />} onClick={handleBatchShipAndInvoice} disabled={selectedReadyToShipOrders.length === 0 || batchShipAndInvoiceMutation.isPending}>
            Ship + send invoices ({selectedReadyToShipOrders.length})
          </Button>
        </Stack>
      </Stack>

      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} alignItems={{ md: 'center' }}>
            <TextField label="Search orders, customers, products, SKU" size="small" fullWidth value={filters.search ?? ''} onChange={(event) => setFilters((current) => ({ ...current, search: event.target.value || undefined }))} />
            <TextField label="From" type="date" size="small" value={fromDate} onChange={(event) => setFromDate(event.target.value)} InputLabelProps={{ shrink: true }} />
            <TextField label="To" type="date" size="small" value={toDate} onChange={(event) => setToDate(event.target.value)} InputLabelProps={{ shrink: true }} />
            <FormControlLabel
              sx={{ minWidth: 170 }}
              control={<Switch checked={showCancelled} onChange={(event) => setShowCancelled(event.target.checked)} />}
              label="Show cancelled"
            />
          </Stack>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
            Work queues hide cancelled orders by default. Turn on cancelled orders when you need history in All.
          </Typography>
        </CardContent>
      </Card>

      <Card>
        <Tabs
          value={tab}
          onChange={(_, value: OrderTab) => {
            setTab(value);
            setSearchParams(value === 'needProduction' ? {} : { tab: value });
          }}
          variant="scrollable"
          scrollButtons="auto"
          sx={{ borderBottom: 1, borderColor: 'divider' }}
        >
          {orderTabs.map((item) => (
            <Tab key={item.value} value={item.value} label={`${item.label} (${tabCounts[item.value] ?? 0})`} />
          ))}
        </Tabs>
        {selectedOrderIds.size > 0 && (
          <Toolbar sx={{ gap: 2, bgcolor: '#fff8f1', borderBottom: 1, borderColor: 'divider' }}>
            <Typography variant="body2" sx={{ flexGrow: 1 }}>{selectedOrderIds.size} selected</Typography>
            <Button size="small" onClick={() => setSelectedOrderIds(new Set())}>Clear selection</Button>
          </Toolbar>
        )}
        <CardContent>
          {visibleOrders.length === 0 ? (
            <Alert severity="info">No orders match this queue.</Alert>
          ) : (
            <TableContainer>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell padding="checkbox">
                      <Checkbox checked={allVisibleSelected} indeterminate={someVisibleSelected} onChange={(event) => handleSelectVisible(event.target.checked)} />
                    </TableCell>
                    <TableCell />
                    <TableCell><TableSortLabel active={sortField === 'orderNumber'} direction={sortDirection} onClick={() => handleSort('orderNumber')}>Order #</TableSortLabel></TableCell>
                    <TableCell><TableSortLabel active={sortField === 'customer'} direction={sortDirection} onClick={() => handleSort('customer')}>Customer</TableSortLabel></TableCell>
                    <TableCell><TableSortLabel active={sortField === 'generatedAt'} direction={sortDirection} onClick={() => handleSort('generatedAt')}>Generated Date</TableSortLabel></TableCell>
                    <TableCell align="right"><TableSortLabel active={sortField === 'totalAmount'} direction={sortDirection} onClick={() => handleSort('totalAmount')}>Total</TableSortLabel></TableCell>
                    <TableCell>Workflow</TableCell>
                    <TableCell>Payment</TableCell>
                    <TableCell>Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {visibleOrders.map((order) => (
                    <OrderRow key={order.id} order={order} selected={selectedOrderIds.has(order.id)} onSelect={handleSelect} onOrderAction={handleOrderAction} />
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          )}
        </CardContent>
      </Card>
    </Box>
  );
}

function isWithinDateRange(date: Date, fromDate: string, toDate: string) {
  const time = date.getTime();
  if (fromDate && time < new Date(fromDate).getTime()) {
    return false;
  }

  if (toDate) {
    const end = new Date(toDate);
    end.setHours(23, 59, 59, 999);
    if (time > end.getTime()) {
      return false;
    }
  }

  return true;
}

function compareOrders(left: Order, right: Order, field: OrderSortField, direction: SortDirection) {
  const multiplier = direction === 'asc' ? 1 : -1;
  const leftValue = orderSortValue(left, field);
  const rightValue = orderSortValue(right, field);
  if (leftValue < rightValue) return -1 * multiplier;
  if (leftValue > rightValue) return 1 * multiplier;
  return 0;
}

function orderSortValue(order: Order, field: OrderSortField): string | number {
  switch (field) {
    case 'customer':
      return order.customer?.businessName.toLowerCase() ?? '';
    case 'generatedAt':
      return order.generatedAt.getTime();
    case 'totalAmount':
      return order.totalAmount;
    case 'orderNumber':
    default:
      return order.orderNumber.toLowerCase();
  }
}

function normalizeOrderTab(value: string | null): OrderTab | null {
  if (!value) {
    return null;
  }

  const normalized = value.replace(/-([a-z])/g, (_, letter: string) => letter.toUpperCase()) as OrderTab;
  return orderTabs.some((tab) => tab.value === normalized) ? normalized : null;
}
