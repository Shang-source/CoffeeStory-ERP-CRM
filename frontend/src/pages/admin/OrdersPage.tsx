import { useState } from 'react';
import { Box, Typography, Card, CardContent, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Chip, Button, IconButton, Collapse, Menu, MenuItem, Alert, CircularProgress } from '@mui/material';
import { KeyboardArrowDown, KeyboardArrowUp, MoreVert, PlayArrow } from '@mui/icons-material';
import { formatOrderStatus, formatInvoiceStatus, formatShipmentStatus, getOrderStatusColor, getInvoiceStatusColor, getShipmentStatusColor } from '@/shared/status/statusFormat';
import { toast } from 'sonner';
import { Order } from '@/entities/types';
import { useNavigate } from 'react-router';
import ConfirmDialog from '@/shared/ui/ConfirmDialog/ConfirmDialog';
import { useAdminOrdersQuery } from '@/entities/order/api/orderQueries';
import { useBatchToProductionMutation } from '@/features/batchToProduction/model/batchToProductionMutations';
import { cancelOrder, generateInvoice, markOrderReadyToShip, markOrderShipped, sendInvoice, sendOrderToProduction } from '@/features/orderWorkflow/api/orderWorkflowApi';
import { useOrderWorkflowMutation } from '@/features/orderWorkflow/model/orderWorkflowMutations';

interface OrderRowProps {
  order: Order;
  onOrderAction: (action: () => Promise<Order>, successMessage: string) => Promise<void>;
}

function OrderRow({ order, onOrderAction }: OrderRowProps) {
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [isCancelConfirmOpen, setIsCancelConfirmOpen] = useState(false);

  const handleMenuClick = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
  };

  const runAction = (action: () => Promise<Order>, successMessage: string) => {
    handleMenuClose();
    void onOrderAction(action, successMessage);
  };

  const confirmCancel = () => {
    handleMenuClose();
    setIsCancelConfirmOpen(true);
  };

  const handleViewInvoice = () => {
    handleMenuClose();
    navigate('/admin/invoices');
    toast.info('Navigating to invoices page');
  };

  const handleViewCustomer = () => {
    handleMenuClose();
    navigate(`/admin/customers/${order.customerId}`);
  };

  const handleViewProduction = () => {
    handleMenuClose();
    navigate('/admin/production');
    toast.info('Navigating to production list');
  };

  const getAvailableActions = () => {
    const actions: { label: string; handler: () => void }[] = [];

    actions.push({ label: 'View Customer', handler: handleViewCustomer });

    if (order.orderStatus === 'Generated') {
      actions.push({
        label: 'Send to Production',
        handler: () => runAction(() => sendOrderToProduction(order.id), 'Order sent to production'),
      });
      actions.push({ label: 'View Production List', handler: handleViewProduction });
    }

    if (order.orderStatus === 'InProduction') {
      actions.push({ label: 'View Production List', handler: handleViewProduction });
      actions.push({
        label: 'Mark Ready to Ship',
        handler: () => runAction(() => markOrderReadyToShip(order.id), 'Order marked ready to ship'),
      });
    }

    if (order.orderStatus === 'ReadyToShip') {
      actions.push({
        label: 'Mark as Shipped',
        handler: () => runAction(() => markOrderShipped(order.id), 'Order marked as shipped. Invoice created as draft.'),
      });
    }

    if (order.invoiceStatus === 'NotIssued' && order.orderStatus === 'Shipped') {
      actions.push({
        label: 'Generate Invoice',
        handler: () => runAction(() => generateInvoice(order.id), 'Invoice generated as draft'),
      });
    }

    if (order.invoiceStatus === 'Draft') {
      actions.push({
        label: 'Send Invoice',
        handler: () => runAction(() => sendInvoice(order.id), 'Invoice sent to customer'),
      });
      actions.push({ label: 'View Invoice', handler: handleViewInvoice });
    }

    if (order.invoiceStatus === 'Unpaid' || order.invoiceStatus === 'Overdue' || order.invoiceStatus === 'PartiallyPaid') {
      actions.push({ label: 'View Invoice', handler: handleViewInvoice });
      actions.push({ label: 'Record Payment', handler: () => { handleMenuClose(); navigate('/admin/payments'); } });
    }

    if (order.invoiceStatus === 'Paid') {
      actions.push({ label: 'View Invoice', handler: handleViewInvoice });
    }

    if (order.orderStatus !== 'Cancelled' && order.orderStatus !== 'Completed' && order.orderStatus !== 'Shipped') {
      actions.push({
        label: 'Cancel Order',
        handler: confirmCancel,
      });
    }

    return actions;
  };

  const actions = getAvailableActions();

  return (
    <>
      <TableRow>
        <TableCell>
          <IconButton size="small" onClick={() => setOpen(!open)}>
            {open ? <KeyboardArrowUp /> : <KeyboardArrowDown />}
          </IconButton>
        </TableCell>
        <TableCell>
          <Typography variant="body2" sx={{ fontWeight: 500 }}>
            {order.orderNumber}
          </Typography>
        </TableCell>
        <TableCell>
          <Button
            variant="text"
            size="small"
            onClick={(e) => {
              e.stopPropagation();
              navigate(`/admin/customers/${order.customerId}`);
            }}
            sx={{ textTransform: 'none', p: 0, minWidth: 'auto', '&:hover': { textDecoration: 'underline' } }}
          >
            {order.customer?.businessName}
          </Button>
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
        <TableCell>
          <Chip
            label={formatInvoiceStatus(order.invoiceStatus)}
            size="small"
            sx={{ bgcolor: getInvoiceStatusColor(order.invoiceStatus), color: 'white' }}
          />
        </TableCell>
        <TableCell>
          <Chip
            label={formatShipmentStatus(order.shipmentStatus)}
            size="small"
            sx={{ bgcolor: getShipmentStatusColor(order.shipmentStatus), color: 'white' }}
          />
        </TableCell>
        <TableCell>
          {actions.length > 0 && (
            <>
              <IconButton size="small" onClick={handleMenuClick}>
                <MoreVert />
              </IconButton>
              <Menu anchorEl={anchorEl} open={Boolean(anchorEl)} onClose={handleMenuClose}>
                {actions.map((action) => (
                  <MenuItem key={action.label} onClick={action.handler}>
                    {action.label}
                  </MenuItem>
                ))}
              </Menu>
            </>
          )}
        </TableCell>
      </TableRow>
      <TableRow>
        <TableCell style={{ paddingBottom: 0, paddingTop: 0 }} colSpan={9}>
          <Collapse in={open} timeout="auto" unmountOnExit>
            <Box sx={{ margin: 2 }}>
              <Typography variant="subtitle2" gutterBottom>
                Order Items
              </Typography>
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
  const { data: orders = [], isLoading, error } = useAdminOrdersQuery();
  const orderActionMutation = useOrderWorkflowMutation();
  const batchToProductionMutation = useBatchToProductionMutation(() => navigate('/admin/production'));

  const handleOrderAction = async (action: () => Promise<Order>, successMessage: string) => {
    try {
      await orderActionMutation.mutateAsync({ action, successMessage });
    } catch {
      return;
    }
  };

  const handleBatchSendToProduction = async () => {
    const generatedOrders = orders.filter(order => order.orderStatus === 'Generated');

    if (generatedOrders.length === 0) {
      toast.info('No orders available to send to production');
      return;
    }

    try {
      await batchToProductionMutation.mutateAsync(generatedOrders.map(order => order.id));
    } catch {
      return;
    }
  };

  const generatedOrdersCount = orders.filter(order => order.orderStatus === 'Generated').length;
  const inProductionCount = orders.filter(order => order.orderStatus === 'InProduction').length;
  const readyToShipCount = orders.filter(order => order.orderStatus === 'ReadyToShip').length;

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 3 }}>
        <div>
          <Typography variant="h4" gutterBottom>
            Orders
          </Typography>
          <Typography variant="body1" color="text.secondary">
            Manage customer orders and track their progress through production to shipment
          </Typography>
        </div>
        <Button
          variant="contained"
          color="primary"
          size="large"
          startIcon={<PlayArrow />}
          onClick={handleBatchSendToProduction}
          disabled={generatedOrdersCount === 0 || batchToProductionMutation.isPending}
        >
          Send All to Production ({generatedOrdersCount})
        </Button>
      </Box>

      {error && <Alert severity="error" sx={{ mb: 3 }}>{error instanceof Error ? error.message : 'Unable to load orders'}</Alert>}

      {generatedOrdersCount > 0 && (
        <Alert severity="info" sx={{ mb: 3 }}>
          You have {generatedOrdersCount} order{generatedOrdersCount > 1 ? 's' : ''} ready to be sent to production.
          Click "Send All to Production" to batch process them.
        </Alert>
      )}

      <Box sx={{ display: 'flex', gap: 2, mb: 3 }}>
        <Card sx={{ flex: 1 }}>
          <CardContent>
            <Typography variant="body2" color="text.secondary" gutterBottom>
              Generated
            </Typography>
            <Typography variant="h4">{generatedOrdersCount}</Typography>
          </CardContent>
        </Card>
        <Card sx={{ flex: 1 }}>
          <CardContent>
            <Typography variant="body2" color="text.secondary" gutterBottom>
              In Production
            </Typography>
            <Typography variant="h4">{inProductionCount}</Typography>
          </CardContent>
        </Card>
        <Card sx={{ flex: 1 }}>
          <CardContent>
            <Typography variant="body2" color="text.secondary" gutterBottom>
              Ready to Ship
            </Typography>
            <Typography variant="h4">{readyToShipCount}</Typography>
          </CardContent>
        </Card>
      </Box>

      <Card>
        <CardContent>
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell />
                  <TableCell>Order #</TableCell>
                  <TableCell>Customer</TableCell>
                  <TableCell>Generated Date</TableCell>
                  <TableCell align="right">Total</TableCell>
                  <TableCell>Order Status</TableCell>
                  <TableCell>Invoice Status</TableCell>
                  <TableCell>Shipment Status</TableCell>
                  <TableCell>Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {orders.map((order) => (
                  <OrderRow key={order.id} order={order} onOrderAction={handleOrderAction} />
                ))}
                {orders.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={9} align="center">No orders found</TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </TableContainer>
        </CardContent>
      </Card>
    </Box>
  );
}
