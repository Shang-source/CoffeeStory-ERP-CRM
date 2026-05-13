import { Box, Typography, Card, CardContent, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Chip, IconButton, Collapse, Alert, CircularProgress } from '@mui/material';
import { KeyboardArrowDown, KeyboardArrowUp } from '@mui/icons-material';
import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { formatOrderStatus, getOrderStatusColor } from '@/shared/status/statusFormat';
import { Order } from '@/entities/types';
import { getCustomerOrders } from '@/entities/order/api/orderApi';
import { queryKeys } from '@/shared/api/queryKeys';

function OrderRow({ order }: { order: Order }) {
  const [open, setOpen] = useState(false);

  return (
    <>
      <TableRow>
        <TableCell>
          <IconButton size="small" onClick={() => setOpen(!open)}>
            {open ? <KeyboardArrowUp /> : <KeyboardArrowDown />}
          </IconButton>
        </TableCell>
        <TableCell>{order.orderNumber}</TableCell>
        <TableCell>{order.generatedAt.toLocaleDateString()}</TableCell>
        <TableCell>
          <Chip
            label={formatOrderStatus(order.orderStatus)}
            size="small"
            sx={{ bgcolor: getOrderStatusColor(order.orderStatus), color: 'white' }}
          />
        </TableCell>
        <TableCell align="right">${order.totalAmount.toFixed(2)}</TableCell>
        <TableCell>
          {order.invoiceStatus && (
            <Chip label={order.invoiceStatus} size="small" variant="outlined" />
          )}
        </TableCell>
      </TableRow>
      <TableRow>
        <TableCell style={{ paddingBottom: 0, paddingTop: 0 }} colSpan={6}>
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
                  <TableRow>
                    <TableCell colSpan={4} align="right">Subtotal:</TableCell>
                    <TableCell align="right">${order.subtotal.toFixed(2)}</TableCell>
                  </TableRow>
                  <TableRow>
                    <TableCell colSpan={4} align="right">GST (15%):</TableCell>
                    <TableCell align="right">${order.gstAmount.toFixed(2)}</TableCell>
                  </TableRow>
                  <TableRow>
                    <TableCell colSpan={4} align="right"><strong>Total:</strong></TableCell>
                    <TableCell align="right"><strong>${order.totalAmount.toFixed(2)}</strong></TableCell>
                  </TableRow>
                </TableBody>
              </Table>
            </Box>
          </Collapse>
        </TableCell>
      </TableRow>
    </>
  );
}

export default function CustomerOrders() {
  const { data: orders = [], isLoading, error } = useQuery({
    queryKey: queryKeys.customerOrders,
    queryFn: getCustomerOrders,
  });

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Orders
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
        View your order history
      </Typography>

      {error && <Alert severity="error" sx={{ mb: 3 }}>{error instanceof Error ? error.message : 'Unable to load orders'}</Alert>}

      <Card>
        <CardContent>
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell />
                  <TableCell>Order Number</TableCell>
                  <TableCell>Generated Date</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell align="right">Total</TableCell>
                  <TableCell>Invoice</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {orders.map((order) => (
                  <OrderRow key={order.id} order={order} />
                ))}
                {orders.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={6} align="center">No orders found</TableCell>
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
