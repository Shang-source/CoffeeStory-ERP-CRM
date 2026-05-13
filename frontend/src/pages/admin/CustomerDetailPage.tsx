import { useEffect, useState } from 'react';
import { Alert, Box, CircularProgress, Typography, Card, CardContent, Grid, Button, Chip, Divider, Table, TableBody, TableCell, TableHead, TableRow, TextField, Switch } from '@mui/material';
import { useParams, Link } from 'react-router';
import { Edit, ArrowBack, Send } from '@mui/icons-material';
import { toast } from 'sonner';
import EditCustomerDialog from '@/features/customerEdit/ui/EditCustomerDialog';
import type { Customer, CustomerPriceBookItem, Invoice, Order, StandingOrder } from '@/entities/types';
import { getAdminCustomer, updateAdminCustomer } from '@/entities/customer/api/customerApi';
import { getAdminInvoices } from '@/entities/invoice/api/invoiceApi';
import { getAdminOrders } from '@/entities/order/api/orderApi';
import { getAdminStandingOrders } from '@/entities/standingOrder/api/standingOrderApi';
import { sendAdminCustomerInvite } from '@/features/customerInvite/api/customerInviteApi';
import { getAdminCustomerPriceBook, updateAdminCustomerPriceBook } from '@/features/customerPriceBook/api/customerPriceBookApi';

export default function CustomerDetail() {
  const { id } = useParams();
  const [customer, setCustomer] = useState<Customer | null>(null);
  const [customerOrders, setCustomerOrders] = useState<Order[]>([]);
  const [customerInvoices, setCustomerInvoices] = useState<Invoice[]>([]);
  const [standingOrder, setStandingOrder] = useState<StandingOrder | null>(null);
  const [priceBookItems, setPriceBookItems] = useState<CustomerPriceBookItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSavingPriceBook, setIsSavingPriceBook] = useState(false);
  const [error, setError] = useState('');
  const [editDialogOpen, setEditDialogOpen] = useState(false);

  useEffect(() => {
    const loadCustomer = async () => {
      if (!id) {
        setError('Customer id is missing');
        setIsLoading(false);
        return;
      }

      try {
        setError('');
        const [loadedCustomer, orders, invoices, standingOrders, priceBook] = await Promise.all([
          getAdminCustomer(id),
          getAdminOrders(),
          getAdminInvoices(),
          getAdminStandingOrders(),
          getAdminCustomerPriceBook(id),
        ]);
        setCustomer(loadedCustomer);
        setCustomerOrders(orders.filter(order => order.customerId === id));
        setCustomerInvoices(invoices.filter(invoice => invoice.customerId === id));
        setStandingOrder(standingOrders.find(order => order.customerId === id) ?? null);
        setPriceBookItems(priceBook.items);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Unable to load customer');
      } finally {
        setIsLoading(false);
      }
    };

    void loadCustomer();
  }, [id]);

  const handleSaveCustomer = async (updatedCustomer: Customer) => {
    if (!id) {
      return;
    }

    const savedCustomer = await updateAdminCustomer(id, {
      businessName: updatedCustomer.businessName,
      contactPerson: updatedCustomer.contactPerson,
      email: updatedCustomer.email,
      phone: updatedCustomer.phone,
      billingAddress: updatedCustomer.billingAddress,
      deliveryAddress: updatedCustomer.deliveryAddress,
      paymentTerms: updatedCustomer.paymentTerms,
      accountStatus: updatedCustomer.accountStatus,
    });
    setCustomer(savedCustomer);
  };

  const handleSendInvite = async () => {
    if (!customer) {
      return;
    }

    try {
      const updatedCustomer = await sendAdminCustomerInvite(customer.id);
      setCustomer(updatedCustomer);
      toast.success(`Invite sent to ${updatedCustomer.email}`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Unable to send invite');
    }
  };

  const handlePriceBookChange = (productId: string, update: Partial<CustomerPriceBookItem>) => {
    setPriceBookItems((currentItems) =>
      currentItems.map((item) => {
        if (item.productId !== productId) {
          return item;
        }

        const nextItem = { ...item, ...update };
        const hasOverride = nextItem.isActive && nextItem.overridePrice !== undefined;
        return {
          ...nextItem,
          hasOverride,
          effectivePrice: hasOverride ? nextItem.overridePrice! : nextItem.basePrice,
        };
      })
    );
  };

  const handleSavePriceBook = async () => {
    if (!customer) {
      return;
    }

    try {
      setIsSavingPriceBook(true);
      const saved = await updateAdminCustomerPriceBook(customer.id, {
        items: priceBookItems.map((item) => ({
          productId: item.productId,
          overridePrice: item.overridePrice ?? null,
          isActive: item.isActive,
          notes: item.notes ?? null,
        })),
      });
      setPriceBookItems(saved.items);
      if (standingOrder) {
        const repricedItems = standingOrder.items.map((item) => {
          const priceBookItem = saved.items.find((price) => price.productId === item.productId);
          return priceBookItem ? { ...item, unitPrice: priceBookItem.effectivePrice } : item;
        });
        setStandingOrder({ ...standingOrder, items: repricedItems });
      }
      toast.success('Price book saved');
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Unable to save price book');
    } finally {
      setIsSavingPriceBook(false);
    }
  };

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

  if (!customer) {
    return <Typography>Customer not found</Typography>;
  }

  return (
    <Box>
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
        <Button
          component={Link}
          to="/admin/customers"
          startIcon={<ArrowBack />}
          sx={{ mr: 2 }}
        >
          Back
        </Button>
        <Typography variant="h4" sx={{ flexGrow: 1 }}>
          {customer.businessName}
        </Typography>
        <Button
          variant="outlined"
          startIcon={<Send />}
          onClick={handleSendInvite}
          disabled={customer.accountStatus === 'Suspended' || customer.accountStatus === 'Archived'}
          sx={{ mr: 1 }}
        >
          Send Invite
        </Button>
        <Button
          variant="outlined"
          startIcon={<Edit />}
          onClick={() => setEditDialogOpen(true)}
        >
          Edit Customer
        </Button>
      </Box>

      <Grid container spacing={3}>
        <Grid size={{ xs: 12, md: 6 }}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Customer Information
              </Typography>
              <Divider sx={{ mb: 2 }} />

              <Box sx={{ mb: 2 }}>
                <Typography variant="body2" color="text.secondary">Contact Person</Typography>
                <Typography variant="body1">{customer.contactPerson}</Typography>
              </Box>

              <Box sx={{ mb: 2 }}>
                <Typography variant="body2" color="text.secondary">Email</Typography>
                <Typography variant="body1">{customer.email}</Typography>
              </Box>

              <Box sx={{ mb: 2 }}>
                <Typography variant="body2" color="text.secondary">Phone</Typography>
                <Typography variant="body1">{customer.phone}</Typography>
              </Box>

              <Box sx={{ mb: 2 }}>
                <Typography variant="body2" color="text.secondary">Account Status</Typography>
                <Chip label={customer.accountStatus} color="success" size="small" sx={{ mt: 0.5 }} />
              </Box>

              <Box sx={{ mb: 2 }}>
                <Typography variant="body2" color="text.secondary">Payment Terms</Typography>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <Typography variant="body1">{customer.paymentTerms}</Typography>
                  <Chip label="Admin Set" size="small" color="primary" variant="outlined" />
                </Box>
                <Typography variant="caption" color="text.secondary" display="block" sx={{ mt: 0.5 }}>
                  Controls invoice due dates and billing cycle
                </Typography>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        <Grid size={{ xs: 12, md: 6 }}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Addresses
              </Typography>
              <Divider sx={{ mb: 2 }} />

              <Box sx={{ mb: 3 }}>
                <Typography variant="body2" color="text.secondary" gutterBottom>
                  Billing Address
                </Typography>
                <Typography variant="body1">
                  {customer.billingAddress}
                </Typography>
              </Box>

              <Box>
                <Typography variant="body2" color="text.secondary" gutterBottom>
                  Delivery Address
                </Typography>
                <Typography variant="body1">
                  {customer.deliveryAddress}
                </Typography>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        <Grid size={{ xs: 12 }}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Standing Order
              </Typography>
              <Divider sx={{ mb: 2 }} />

              {standingOrder ? (
                <Box>
                  <Grid container spacing={2} sx={{ mb: 2 }}>
                    <Grid size={{ xs: 12, sm: 4 }}>
                      <Typography variant="body2" color="text.secondary">Frequency</Typography>
                      <Typography variant="body1">{standingOrder.frequency}</Typography>
                    </Grid>
                    <Grid size={{ xs: 12, sm: 4 }}>
                      <Typography variant="body2" color="text.secondary">Status</Typography>
                      <Chip label={standingOrder.status} color="success" size="small" />
                    </Grid>
                    <Grid size={{ xs: 12, sm: 4 }}>
                      <Typography variant="body2" color="text.secondary">Next Closing</Typography>
                      <Typography variant="body1">{standingOrder.nextClosingDate.toLocaleDateString()}</Typography>
                    </Grid>
                  </Grid>

                  <Table size="small">
                    <TableHead>
                      <TableRow>
                        <TableCell>Product</TableCell>
                        <TableCell align="right">Quantity</TableCell>
                        <TableCell align="right">Unit Price</TableCell>
                        <TableCell align="right">Total</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {standingOrder.items.map((item) => (
                        <TableRow key={item.id}>
                          <TableCell>{item.product.name}</TableCell>
                          <TableCell align="right">{item.quantity}</TableCell>
                          <TableCell align="right">${item.unitPrice.toFixed(2)}</TableCell>
                          <TableCell align="right">${(item.quantity * item.unitPrice).toFixed(2)}</TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </Box>
              ) : (
                <Typography variant="body2" color="text.secondary">
                  No standing order configured
                </Typography>
              )}
            </CardContent>
          </Card>
        </Grid>

        <Grid size={{ xs: 12 }}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                <div>
                  <Typography variant="h6" gutterBottom>
                    Price Book
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Customer-specific prices apply to future standing-order generated orders.
                  </Typography>
                </div>
                <Button variant="contained" onClick={handleSavePriceBook} disabled={isSavingPriceBook}>
                  Save Price Book
                </Button>
              </Box>
              <Divider sx={{ mb: 2 }} />
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Product</TableCell>
                    <TableCell>SKU</TableCell>
                    <TableCell align="right">Base Price</TableCell>
                    <TableCell align="right">Override Price</TableCell>
                    <TableCell align="center">Active</TableCell>
                    <TableCell align="right">Effective Price</TableCell>
                    <TableCell>Notes</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {priceBookItems.map((item) => (
                    <TableRow key={item.productId}>
                      <TableCell>{item.name}</TableCell>
                      <TableCell>{item.sku}</TableCell>
                      <TableCell align="right">${item.basePrice.toFixed(2)}</TableCell>
                      <TableCell align="right">
                        <TextField
                          type="number"
                          size="small"
                          value={item.overridePrice ?? ''}
                          onChange={(event) => {
                            const value = event.target.value;
                            handlePriceBookChange(item.productId, {
                              overridePrice: value === '' ? undefined : Number(value),
                            });
                          }}
                          inputProps={{ min: 0, step: 0.01 }}
                          sx={{ width: 120 }}
                        />
                      </TableCell>
                      <TableCell align="center">
                        <Switch
                          checked={item.isActive}
                          onChange={(event) => handlePriceBookChange(item.productId, { isActive: event.target.checked })}
                        />
                      </TableCell>
                      <TableCell align="right">
                        <Box sx={{ display: 'flex', justifyContent: 'flex-end', alignItems: 'center', gap: 1 }}>
                          ${item.effectivePrice.toFixed(2)}
                          {item.hasOverride && <Chip label="Override" size="small" color="primary" />}
                        </Box>
                      </TableCell>
                      <TableCell>
                        <TextField
                          size="small"
                          value={item.notes ?? ''}
                          onChange={(event) => handlePriceBookChange(item.productId, { notes: event.target.value })}
                          placeholder="Optional notes"
                          fullWidth
                        />
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </CardContent>
          </Card>
        </Grid>

        <Grid size={{ xs: 12, md: 6 }}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Recent Orders ({customerOrders.length})
              </Typography>
              <Divider sx={{ mb: 2 }} />

              {customerOrders.length > 0 ? (
                customerOrders.slice(0, 5).map((order) => (
                  <Box
                    key={order.id}
                    sx={{
                      py: 1.5,
                      borderBottom: '1px solid #e0e0e0',
                      '&:last-child': { borderBottom: 'none' }
                    }}
                  >
                    <Typography variant="body2">{order.orderNumber}</Typography>
                    <Typography variant="caption" color="text.secondary">
                      {order.generatedAt.toLocaleDateString()} - ${order.totalAmount.toFixed(2)}
                    </Typography>
                  </Box>
                ))
              ) : (
                <Typography variant="body2" color="text.secondary">
                  No orders yet
                </Typography>
              )}
            </CardContent>
          </Card>
        </Grid>

        <Grid size={{ xs: 12, md: 6 }}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Invoices ({customerInvoices.length})
              </Typography>
              <Divider sx={{ mb: 2 }} />

              {customerInvoices.length > 0 ? (
                customerInvoices.slice(0, 5).map((invoice) => (
                  <Box
                    key={invoice.id}
                    sx={{
                      py: 1.5,
                      borderBottom: '1px solid #e0e0e0',
                      '&:last-child': { borderBottom: 'none' }
                    }}
                  >
                    <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                      <Typography variant="body2">{invoice.invoiceNumber}</Typography>
                      <Chip label={invoice.status} size="small" />
                    </Box>
                    <Typography variant="caption" color="text.secondary">
                      Outstanding: ${invoice.outstandingAmount.toFixed(2)}
                    </Typography>
                  </Box>
                ))
              ) : (
                <Typography variant="body2" color="text.secondary">
                  No invoices yet
                </Typography>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <EditCustomerDialog
        open={editDialogOpen}
        customer={customer}
        onClose={() => setEditDialogOpen(false)}
        onSave={handleSaveCustomer}
      />
    </Box>
  );
}
