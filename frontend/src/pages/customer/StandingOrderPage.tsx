import { useEffect, useState } from 'react';
import { Box, Typography, Card, CardContent, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Button, TextField, Select, MenuItem, FormControl, InputLabel, IconButton, Dialog, DialogTitle, DialogContent, DialogActions, Grid, Alert, CircularProgress } from '@mui/material';
import { Add, Delete, Edit, Save } from '@mui/icons-material';
import { toast } from 'sonner';
import { useAuth } from '@/app/providers/AuthProvider';
import { OrderFrequency, StandingOrder } from '@/entities/types';
import { useCustomerProductsQuery } from '@/entities/product/api/productQueries';
import { useCustomerStandingOrderQuery } from '@/entities/standingOrder/api/standingOrderQueries';
import { useSaveCustomerStandingOrderMutation } from '@/features/standingOrderEditor/model/standingOrderEditorMutations';
import { formatStandingOrderStatus, getStandingOrderStatusColor } from '@/shared/status/statusFormat';
import { StatusChip } from '@/shared/ui/StatusChip';

export default function StandingOrderPage() {
  const { user } = useAuth();
  const standingOrderQuery = useCustomerStandingOrderQuery();
  const productsQuery = useCustomerProductsQuery();
  const [standingOrder, setStandingOrder] = useState<StandingOrder | null>(null);
  const [isEditing, setIsEditing] = useState(false);
  const [openAddDialog, setOpenAddDialog] = useState(false);
  const [newItem, setNewItem] = useState({ productId: '', quantity: 1 });
  const products = productsQuery.data ?? [];
  const isLoading = standingOrderQuery.isLoading || productsQuery.isLoading;
  const error = standingOrderQuery.error ?? productsQuery.error;

  useEffect(() => {
    if (standingOrderQuery.data) {
      setStandingOrder(standingOrderQuery.data);
      return;
    }

    if (standingOrderQuery.isSuccess && standingOrderQuery.data === null) {
      setStandingOrder(createEmptyStandingOrder(user?.customerId));
      setIsEditing(true);
    }
  }, [standingOrderQuery.data, standingOrderQuery.isSuccess, user?.customerId]);

  const saveStandingOrderMutation = useSaveCustomerStandingOrderMutation((updated) => {
    setStandingOrder(updated);
    setIsEditing(false);
  });

  const handleSave = async () => {
    if (!standingOrder) {
      return;
    }

    if (standingOrder.items.length === 0) {
      toast.error('Add at least one item before saving your standing order');
      return;
    }

    try {
      await saveStandingOrderMutation.mutateAsync(standingOrder);
    } catch {
      return;
    }
  };

  const handleFrequencyChange = (newFrequency: string) => {
    if (!standingOrder) return;
    setStandingOrder({
      ...standingOrder,
      frequency: newFrequency as OrderFrequency,
    });
  };

  const handleAddItem = () => {
    if (!standingOrder) return;
    const product = products.find(p => p.id === newItem.productId);
    if (product) {
      setStandingOrder({
        ...standingOrder,
        items: [
          ...standingOrder.items,
          {
            id: `new-${Date.now()}`,
            productId: product.id,
            product,
            quantity: newItem.quantity,
            unitPrice: product.effectivePrice,
          },
        ],
      });
      setOpenAddDialog(false);
      setNewItem({ productId: '', quantity: 1 });
      toast.success(`${product.name} added to standing order`);
    }
  };

  const handleRemoveItem = (itemId: string) => {
    if (!standingOrder) return;
    setStandingOrder({
      ...standingOrder,
      items: standingOrder.items.filter(item => item.id !== itemId),
    });
    toast.success('Item removed from standing order');
  };

  const handleQuantityChange = (itemId: string, quantity: number) => {
    if (!standingOrder) return;
    setStandingOrder({
      ...standingOrder,
      items: standingOrder.items.map(item =>
        item.id === itemId ? { ...item, quantity } : item
      ),
    });
  };

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (error) {
    return <Alert severity="error">{error instanceof Error ? error.message : error}</Alert>;
  }

  if (!standingOrder) {
    return <Alert severity="info">Preparing your standing order form...</Alert>;
  }

  const estimatedTotal = standingOrder.items.reduce(
    (sum, item) => sum + item.quantity * item.unitPrice,
    0
  );
  const isNewStandingOrder = !standingOrder.id;

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4">Standing Order</Typography>
        <Box>
          {isEditing ? (
            <Button variant="contained" startIcon={<Save />} onClick={handleSave} disabled={saveStandingOrderMutation.isPending}>
              {isNewStandingOrder ? 'Create Standing Order' : 'Save Changes'}
            </Button>
          ) : (
            <Button variant="outlined" startIcon={<Edit />} onClick={() => setIsEditing(true)}>
              Edit Order
            </Button>
          )}
        </Box>
      </Box>

      <Grid container spacing={3}>
        <Grid size={{ xs: 12, md: 8 }}>
          <Card>
            <CardContent>
              {isNewStandingOrder && (
                <Alert severity="info" sx={{ mb: 2 }}>
                  No standing order exists yet. Add your coffee items, choose a frequency, then create your standing order.
                </Alert>
              )}

              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                <Typography variant="h6">Order Items</Typography>
                {isEditing && (
                  <Button variant="outlined" size="small" startIcon={<Add />} onClick={() => setOpenAddDialog(true)}>
                    Add Item
                  </Button>
                )}
              </Box>

              <TableContainer>
                <Table>
                  <TableHead>
                    <TableRow>
                      <TableCell>Product</TableCell>
                      <TableCell>SKU</TableCell>
                      <TableCell align="right">Quantity</TableCell>
                      <TableCell align="right">Unit Price</TableCell>
                      <TableCell align="right">Total</TableCell>
                      {isEditing && <TableCell align="center">Actions</TableCell>}
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {standingOrder.items.length === 0 ? (
                      <TableRow>
                        <TableCell colSpan={isEditing ? 6 : 5} align="center">
                          No items yet. Click Add Item to start your standing order.
                        </TableCell>
                      </TableRow>
                    ) : (
                      standingOrder.items.map((item) => (
                        <TableRow key={item.id}>
                          <TableCell>{item.product.name}</TableCell>
                          <TableCell>{item.product.sku}</TableCell>
                          <TableCell align="right">
                            {isEditing ? (
                              <TextField
                                type="number"
                                size="small"
                                value={item.quantity}
                                onChange={(e) => handleQuantityChange(item.id, parseInt(e.target.value, 10) || 1)}
                                sx={{ width: 80 }}
                                inputProps={{ min: 1 }}
                              />
                            ) : (
                              item.quantity
                            )}
                          </TableCell>
                          <TableCell align="right">${item.unitPrice.toFixed(2)}</TableCell>
                          <TableCell align="right">${(item.quantity * item.unitPrice).toFixed(2)}</TableCell>
                          {isEditing && (
                            <TableCell align="center">
                              <IconButton size="small" color="error" onClick={() => handleRemoveItem(item.id)}>
                                <Delete />
                              </IconButton>
                            </TableCell>
                          )}
                        </TableRow>
                      ))
                    )}
                  </TableBody>
                </Table>
              </TableContainer>

              <Box sx={{ mt: 2, pt: 2, borderTop: '2px solid #e0e0e0', display: 'flex', justifyContent: 'flex-end' }}>
                <Typography variant="h6">Estimated Total: ${estimatedTotal.toFixed(2)}</Typography>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        <Grid size={{ xs: 12, md: 4 }}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Order Details
              </Typography>

              <Box sx={{ mb: 2 }}>
                <Typography variant="body2" color="text.secondary" gutterBottom>
                  Status
                </Typography>
                <StatusChip label={formatStandingOrderStatus(standingOrder.status)} color={getStandingOrderStatusColor(standingOrder.status)} />
              </Box>

              <Box sx={{ mb: 2 }}>
                <Typography variant="body2" color="text.secondary" gutterBottom>
                  Closing Frequency
                </Typography>
                {isEditing ? (
                  <FormControl fullWidth size="small">
                    <Select value={standingOrder.frequency} onChange={(e) => handleFrequencyChange(e.target.value)}>
                      <MenuItem key="weekly" value="Weekly">Weekly</MenuItem>
                      <MenuItem key="fortnightly" value="Fortnightly">Fortnightly (Every 2 weeks)</MenuItem>
                      <MenuItem key="monthly" value="Monthly">Monthly</MenuItem>
                      <MenuItem key="manual" value="ManualOnly">Manual Only (No auto-closing)</MenuItem>
                    </Select>
                  </FormControl>
                ) : (
                  <Typography variant="body1">
                    {standingOrder.frequency === 'Fortnightly' ? 'Fortnightly (Every 2 weeks)' : standingOrder.frequency}
                  </Typography>
                )}
              </Box>

              <Box sx={{ mb: 2 }}>
                <Typography variant="body2" color="text.secondary" gutterBottom>
                  Next Closing Date
                </Typography>
                <Typography variant="body1">
                  {standingOrder.nextClosingDate.toLocaleDateString('en-NZ', {
                    weekday: 'long',
                    year: 'numeric',
                    month: 'long',
                    day: 'numeric',
                  })}
                </Typography>
              </Box>

              {isEditing ? (
                <Box sx={{ mb: 2 }}>
                  <TextField
                    label="Delivery Notes"
                    fullWidth
                    multiline
                    rows={2}
                    size="small"
                    value={standingOrder.deliveryNotes ?? ''}
                    onChange={(e) => setStandingOrder({ ...standingOrder, deliveryNotes: e.target.value })}
                    placeholder="e.g., Deliver to back entrance, call before delivery"
                  />
                </Box>
              ) : standingOrder.deliveryNotes ? (
                <Box sx={{ mb: 2 }}>
                  <Typography variant="body2" color="text.secondary" gutterBottom>
                    Delivery Notes
                  </Typography>
                  <Typography variant="body2">{standingOrder.deliveryNotes}</Typography>
                </Box>
              ) : null}

              <Box sx={{ mt: 3, p: 2, bgcolor: 'info.light', borderRadius: 1 }}>
                <Typography variant="body2">
                  <strong>Note:</strong> Your order will be automatically generated based on your closing frequency.
                  Any changes made before the next closing date will be included in the next order.
                </Typography>
              </Box>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <Dialog open={openAddDialog} onClose={() => setOpenAddDialog(false)}>
        <DialogTitle>Add Item to Standing Order</DialogTitle>
        <DialogContent sx={{ minWidth: 400, pt: 2 }}>
          <FormControl fullWidth sx={{ mb: 2 }}>
            <InputLabel>Product</InputLabel>
            <Select
              value={newItem.productId}
              label="Product"
              onChange={(e) => setNewItem({ ...newItem, productId: e.target.value })}
            >
              {products.map((product) => (
                <MenuItem key={`product-${product.id}`} value={product.id}>
                  {product.name} - ${product.effectivePrice.toFixed(2)}
                  {product.hasOverride ? ` (custom price, base $${product.basePrice.toFixed(2)})` : ''}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          <TextField
            label="Quantity"
            type="number"
            fullWidth
            value={newItem.quantity}
            onChange={(e) => setNewItem({ ...newItem, quantity: parseInt(e.target.value, 10) || 1 })}
            inputProps={{ min: 1 }}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenAddDialog(false)}>Cancel</Button>
          <Button onClick={handleAddItem} variant="contained" disabled={!newItem.productId}>
            Add Item
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}

function createEmptyStandingOrder(customerId?: string): StandingOrder {
  const nextClosingDate = new Date();
  nextClosingDate.setDate(nextClosingDate.getDate() + 7);

  return {
    id: '',
    customerId: customerId ?? '',
    frequency: 'Weekly',
    nextClosingDate,
    status: 'Active',
    items: [],
  };
}
