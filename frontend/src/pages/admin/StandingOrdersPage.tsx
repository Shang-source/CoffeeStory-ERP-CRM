import { useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Grid,
  IconButton,
  MenuItem,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
  CircularProgress,
} from '@mui/material';
import { Add, Cancel as CancelIcon, Delete, Edit, Pause, PlayArrow, RestartAlt } from '@mui/icons-material';
import { toast } from 'sonner';
import { OrderFrequency, StandingOrder, StandingOrderStatus } from '@/entities/types';
import { useAdminCustomersQuery } from '@/entities/customer/api/customerQueries';
import { useProductsQuery } from '@/entities/product/api/productQueries';
import { useAdminStandingOrdersQuery } from '@/entities/standingOrder/api/standingOrderQueries';
import { type StandingOrderPayload } from '@/features/standingOrderEditor/api/standingOrderEditorApi';
import { useSaveAdminStandingOrderMutation } from '@/features/standingOrderEditor/model/standingOrderEditorMutations';
import { cancelStandingOrder, pauseStandingOrder, resumeStandingOrder } from '@/features/standingOrderLifecycle/api/standingOrderLifecycleApi';
import { useGenerateStandingOrderNowMutation, useStandingOrderStatusActionMutation } from '@/features/standingOrderLifecycle/model/standingOrderLifecycleMutations';
import { formatStandingOrderStatus, getStandingOrderStatusColor } from '@/shared/status/statusFormat';
import { StatusChip } from '@/shared/ui/StatusChip';

const frequencies: OrderFrequency[] = ['Weekly', 'Fortnightly', 'Monthly', 'ManualOnly'];
const statuses: StandingOrderStatus[] = ['Active', 'Paused', 'Cancelled'];

interface StandingOrderFormItem {
  productId: string;
  quantity: number;
  notes: string;
}

interface StandingOrderFormState {
  customerId: string;
  frequency: OrderFrequency;
  nextClosingDate: string;
  status: StandingOrderStatus;
  deliveryNotes: string;
  internalNotes: string;
  items: StandingOrderFormItem[];
}

const emptyForm: StandingOrderFormState = {
  customerId: '',
  frequency: 'Weekly',
  nextClosingDate: new Date().toISOString().slice(0, 10),
  status: 'Active',
  deliveryNotes: '',
  internalNotes: '',
  items: [{ productId: '', quantity: 1, notes: '' }],
};

export default function StandingOrders() {
  const standingOrdersQuery = useAdminStandingOrdersQuery();
  const customersQuery = useAdminCustomersQuery();
  const productsQuery = useProductsQuery();
  const [editingOrder, setEditingOrder] = useState<StandingOrder | null>(null);
  const [formData, setFormData] = useState<StandingOrderFormState>(emptyForm);
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const standingOrders = standingOrdersQuery.data ?? [];
  const customers = customersQuery.data ?? [];
  const products = (productsQuery.data ?? []).filter((product) => product.isActive);
  const isLoading = standingOrdersQuery.isLoading || customersQuery.isLoading || productsQuery.isLoading;
  const error = standingOrdersQuery.error ?? customersQuery.error ?? productsQuery.error;
  const manualGenerateMutation = useGenerateStandingOrderNowMutation();
  const statusActionMutation = useStandingOrderStatusActionMutation();
  const saveStandingOrderMutation = useSaveAdminStandingOrderMutation(() => setIsDialogOpen(false));

  const handleManualGenerate = async (orderId: string) => {
    try {
      await manualGenerateMutation.mutateAsync(orderId);
    } catch {
      return;
    }
  };

  const runStatusAction = async (action: () => Promise<StandingOrder>, successMessage: string) => {
    try {
      await statusActionMutation.mutateAsync({ action, successMessage });
    } catch {
      return;
    }
  };

  const openCreateDialog = () => {
    setEditingOrder(null);
    setFormData({
      ...emptyForm,
      customerId: customers[0]?.id ?? '',
      items: [{ productId: products[0]?.id ?? '', quantity: 1, notes: '' }],
    });
    setIsDialogOpen(true);
  };

  const openEditDialog = (standingOrder: StandingOrder) => {
    setEditingOrder(standingOrder);
    setFormData({
      customerId: standingOrder.customerId,
      frequency: standingOrder.frequency,
      nextClosingDate: toDateInput(standingOrder.nextClosingDate),
      status: standingOrder.status,
      deliveryNotes: standingOrder.deliveryNotes ?? '',
      internalNotes: standingOrder.internalNotes ?? '',
      items: standingOrder.items.map((item) => ({
        productId: item.productId,
        quantity: item.quantity,
        notes: item.notes ?? '',
      })),
    });
    setIsDialogOpen(true);
  };

  const handleSave = async () => {
    if (!formData.customerId || formData.items.some((item) => !item.productId || item.quantity <= 0)) {
      toast.error('Customer, products, and positive quantities are required');
      return;
    }

    const payload: StandingOrderPayload = {
      customerId: formData.customerId,
      frequency: formData.frequency,
      nextClosingDate: new Date(`${formData.nextClosingDate}T00:00:00.000Z`).toISOString(),
      status: formData.status,
      deliveryNotes: formData.deliveryNotes || undefined,
      internalNotes: formData.internalNotes || undefined,
      items: formData.items.map((item) => ({
        productId: item.productId,
        quantity: item.quantity,
        notes: item.notes || undefined,
      })),
    };

    try {
      await saveStandingOrderMutation.mutateAsync({
        standingOrderId: editingOrder?.id,
        payload,
        isEditing: Boolean(editingOrder),
      });
    } catch {
      return;
    }
  };

  const updateItem = (index: number, update: Partial<StandingOrderFormItem>) => {
    setFormData((current) => ({
      ...current,
      items: current.items.map((item, itemIndex) => itemIndex === index ? { ...item, ...update } : item),
    }));
  };

  const addItem = () => {
    setFormData((current) => ({
      ...current,
      items: [...current.items, { productId: products[0]?.id ?? '', quantity: 1, notes: '' }],
    }));
  };

  const removeItem = (index: number) => {
    setFormData((current) => ({
      ...current,
      items: current.items.length === 1 ? current.items : current.items.filter((_, itemIndex) => itemIndex !== index),
    }));
  };

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Box>
          <Typography variant="h4" gutterBottom>
            Standing Orders
          </Typography>
          <Typography variant="body1" color="text.secondary">
            View and manage customer standing orders
          </Typography>
        </Box>
        <Button variant="contained" startIcon={<Add />} onClick={openCreateDialog}>
          Add Standing Order
        </Button>
      </Box>

      {error && <Alert severity="error" sx={{ mb: 3 }}>{error instanceof Error ? error.message : 'Unable to load standing orders'}</Alert>}

      <Card>
        <CardContent>
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Customer</TableCell>
                  <TableCell>Frequency</TableCell>
                  <TableCell>Next Closing</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell align="right">Est. Amount</TableCell>
                  <TableCell align="center">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {standingOrders.map((order) => {
                  const estimatedAmount = order.items.reduce(
                    (sum, item) => sum + item.quantity * item.unitPrice,
                    0
                  );

                  return (
                    <TableRow key={order.id}>
                      <TableCell>{order.customer?.businessName}</TableCell>
                      <TableCell>{order.frequency}</TableCell>
                      <TableCell>{order.nextClosingDate.toLocaleDateString()}</TableCell>
                      <TableCell>
                        <StatusChip label={formatStandingOrderStatus(order.status)} color={getStandingOrderStatusColor(order.status)} />
                      </TableCell>
                      <TableCell align="right">${estimatedAmount.toFixed(2)}</TableCell>
                      <TableCell align="center">
                        <IconButton size="small" title="Edit standing order" onClick={() => openEditDialog(order)}>
                          <Edit />
                        </IconButton>
                        <Button
                          size="small"
                          startIcon={<PlayArrow />}
                          onClick={() => handleManualGenerate(order.id)}
                          disabled={order.status !== 'Active'}
                        >
                          Generate Now
                        </Button>
                        {order.status === 'Active' && (
                          <IconButton
                            size="small"
                            title="Pause standing order"
                            onClick={() => runStatusAction(() => pauseStandingOrder(order.id), 'Standing order paused')}
                          >
                            <Pause />
                          </IconButton>
                        )}
                        {order.status === 'Paused' && (
                          <IconButton
                            size="small"
                            title="Resume standing order"
                            onClick={() => runStatusAction(() => resumeStandingOrder(order.id), 'Standing order resumed')}
                          >
                            <RestartAlt />
                          </IconButton>
                        )}
                        {order.status !== 'Cancelled' && (
                          <IconButton
                            size="small"
                            title="Cancel standing order"
                            onClick={() => runStatusAction(() => cancelStandingOrder(order.id), 'Standing order cancelled')}
                          >
                            <CancelIcon />
                          </IconButton>
                        )}
                      </TableCell>
                    </TableRow>
                  );
                })}
                {standingOrders.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={6} align="center">No standing orders found</TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </TableContainer>
        </CardContent>
      </Card>

      <Dialog open={isDialogOpen} onClose={() => setIsDialogOpen(false)} maxWidth="md" fullWidth>
        <DialogTitle>{editingOrder ? 'Edit Standing Order' : 'Add Standing Order'}</DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid size={{ xs: 12, md: 6 }}>
              <TextField
                select
                label="Customer"
                fullWidth
                required
                value={formData.customerId}
                onChange={(event) => setFormData({ ...formData, customerId: event.target.value })}
                disabled={Boolean(editingOrder)}
              >
                {customers.map((customer) => (
                  <MenuItem key={customer.id} value={customer.id}>{customer.businessName}</MenuItem>
                ))}
              </TextField>
            </Grid>
            <Grid size={{ xs: 12, md: 3 }}>
              <TextField
                select
                label="Frequency"
                fullWidth
                value={formData.frequency}
                onChange={(event) => setFormData({ ...formData, frequency: event.target.value as OrderFrequency })}
              >
                {frequencies.map((frequency) => (
                  <MenuItem key={frequency} value={frequency}>{frequency}</MenuItem>
                ))}
              </TextField>
            </Grid>
            <Grid size={{ xs: 12, md: 3 }}>
              <TextField
                select
                label="Status"
                fullWidth
                value={formData.status}
                onChange={(event) => setFormData({ ...formData, status: event.target.value as StandingOrderStatus })}
              >
                {statuses.map((status) => (
                  <MenuItem key={status} value={status}>{status}</MenuItem>
                ))}
              </TextField>
            </Grid>
            <Grid size={{ xs: 12, md: 4 }}>
              <TextField
                label="Next Closing Date"
                type="date"
                fullWidth
                value={formData.nextClosingDate}
                onChange={(event) => setFormData({ ...formData, nextClosingDate: event.target.value })}
                InputLabelProps={{ shrink: true }}
              />
            </Grid>
            <Grid size={{ xs: 12, md: 4 }}>
              <TextField
                label="Delivery Notes"
                fullWidth
                value={formData.deliveryNotes}
                onChange={(event) => setFormData({ ...formData, deliveryNotes: event.target.value })}
              />
            </Grid>
            <Grid size={{ xs: 12, md: 4 }}>
              <TextField
                label="Internal Notes"
                fullWidth
                value={formData.internalNotes}
                onChange={(event) => setFormData({ ...formData, internalNotes: event.target.value })}
              />
            </Grid>
            <Grid size={{ xs: 12 }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mt: 1 }}>
                <Typography variant="h6">Items</Typography>
                <Button size="small" startIcon={<Add />} onClick={addItem}>Add Item</Button>
              </Box>
            </Grid>
            {formData.items.map((item, index) => (
              <Grid container spacing={2} size={{ xs: 12 }} key={`${item.productId}-${index}`}>
                <Grid size={{ xs: 12, md: 5 }}>
                  <TextField
                    select
                    label="Product"
                    fullWidth
                    required
                    value={item.productId}
                    onChange={(event) => updateItem(index, { productId: event.target.value })}
                  >
                    {products.map((product) => (
                      <MenuItem key={product.id} value={product.id}>{product.name} ({product.sku})</MenuItem>
                    ))}
                  </TextField>
                </Grid>
                <Grid size={{ xs: 12, md: 2 }}>
                  <TextField
                    label="Quantity"
                    type="number"
                    fullWidth
                    required
                    value={item.quantity}
                    onChange={(event) => updateItem(index, { quantity: Number(event.target.value) })}
                    inputProps={{ min: 1, step: 1 }}
                  />
                </Grid>
                <Grid size={{ xs: 12, md: 4 }}>
                  <TextField
                    label="Notes"
                    fullWidth
                    value={item.notes}
                    onChange={(event) => updateItem(index, { notes: event.target.value })}
                  />
                </Grid>
                <Grid size={{ xs: 12, md: 1 }} sx={{ display: 'flex', alignItems: 'center' }}>
                  <IconButton size="small" color="error" onClick={() => removeItem(index)} disabled={formData.items.length === 1}>
                    <Delete />
                  </IconButton>
                </Grid>
              </Grid>
            ))}
          </Grid>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setIsDialogOpen(false)}>Cancel</Button>
          <Button onClick={handleSave} variant="contained" disabled={saveStandingOrderMutation.isPending}>
            Save Standing Order
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}

function toDateInput(value: Date) {
  return value.toISOString().slice(0, 10);
}
