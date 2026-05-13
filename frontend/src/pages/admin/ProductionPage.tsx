import { useState } from 'react';
import { Box, Typography, Card, CardContent, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Button, Chip, Dialog, DialogTitle, DialogContent, DialogActions, TextField, Alert, CircularProgress } from '@mui/material';
import { Print, Download, PlayArrow, Edit as EditIcon, CheckCircle } from '@mui/icons-material';
import { formatProductionStatus, getProductionStatusColor } from '@/shared/status/statusFormat';
import { ProductionItem } from '@/entities/types';
import { toast } from 'sonner';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { getCurrentProduction } from '@/entities/production/api/productionApi';
import { completeProduction, startProduction, updateProducedQuantity } from '@/features/productionItemUpdate/api/productionItemUpdateApi';
import { queryKeys } from '@/shared/api/queryKeys';

export default function ProductionList() {
  const queryClient = useQueryClient();
  const { data: productionItems = [], isLoading, error } = useQuery({
    queryKey: queryKeys.production,
    queryFn: getCurrentProduction,
  });
  const [selectedItem, setSelectedItem] = useState<ProductionItem | null>(null);
  const [updateDialog, setUpdateDialog] = useState(false);
  const [updateQuantity, setUpdateQuantity] = useState('');

  const replaceProductionItem = (updatedItem: ProductionItem) => {
    queryClient.setQueryData<ProductionItem[]>(queryKeys.production, (items = []) =>
      items.map((item) => item.productId === updatedItem.productId ? updatedItem : item)
    );
  };

  const startProductionMutation = useMutation({
    mutationFn: (productId: string) => startProduction(productId),
    onSuccess: (updated) => {
      replaceProductionItem(updated);
      toast.success(`Started production for ${updated.productName}`);
    },
    onError: (err) => toast.error(err instanceof Error ? err.message : 'Unable to start production'),
  });

  const updateQuantityMutation = useMutation({
    mutationFn: ({ productId, producedQuantity }: { productId: string; producedQuantity: number }) =>
      updateProducedQuantity(productId, producedQuantity),
    onSuccess: async (updated) => {
      replaceProductionItem(updated);
      toast.success(`Updated produced quantity for ${updated.productName}`);
      setUpdateDialog(false);
      setSelectedItem(null);
      await queryClient.invalidateQueries({ queryKey: queryKeys.production });
    },
    onError: (err) => toast.error(err instanceof Error ? err.message : 'Unable to update produced quantity'),
  });

  const completeProductionMutation = useMutation({
    mutationFn: (productId: string) => completeProduction(productId),
    onSuccess: async (updated) => {
      replaceProductionItem(updated);
      toast.success(`${updated.productName} marked as completed`);
      await queryClient.invalidateQueries({ queryKey: queryKeys.production });
      await queryClient.invalidateQueries({ queryKey: queryKeys.adminOrders });
    },
    onError: (err) => toast.error(err instanceof Error ? err.message : 'Unable to complete production item'),
  });

  const handleStartProduction = async (item: ProductionItem) => {
    startProductionMutation.mutate(item.productId);
  };

  const handleUpdateQuantity = (item: ProductionItem) => {
    setSelectedItem(item);
    setUpdateQuantity(item.producedQuantity.toString());
    setUpdateDialog(true);
  };

  const handleSaveQuantity = async () => {
    if (!selectedItem) return;

    const newQuantity = parseInt(updateQuantity, 10);
    if (Number.isNaN(newQuantity) || newQuantity < 0) {
      toast.error('Please enter a valid quantity');
      return;
    }

    try {
      await updateQuantityMutation.mutateAsync({ productId: selectedItem.productId, producedQuantity: newQuantity });
    } catch {
      return;
    }
  };

  const handleMarkCompleted = async (item: ProductionItem) => {
    completeProductionMutation.mutate(item.productId);
  };

  const handlePrint = () => {
    toast.success('Printing production list');
  };

  const handleExport = () => {
    toast.success('Exporting production list to CSV');
  };

  const getActionButton = (item: ProductionItem) => {
    if (item.status === 'Pending') {
      return (
        <Button size="small" variant="contained" color="primary" startIcon={<PlayArrow />} onClick={() => handleStartProduction(item)}>
          Start
        </Button>
      );
    }

    if (item.status === 'InProgress') {
      return (
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Button size="small" variant="outlined" startIcon={<EditIcon />} onClick={() => handleUpdateQuantity(item)}>
            Update
          </Button>
          <Button size="small" variant="contained" color="success" startIcon={<CheckCircle />} onClick={() => handleMarkCompleted(item)}>
            Complete
          </Button>
        </Box>
      );
    }

    if (item.status === 'Completed') {
      return <Chip label="Completed" color="success" size="small" />;
    }

    if (item.status === 'OnHold') {
      return (
        <Button size="small" variant="outlined" color="warning" onClick={() => handleStartProduction(item)}>
          Resume
        </Button>
      );
    }

    return null;
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
        <div>
          <Typography variant="h4" gutterBottom>
            Production List
          </Typography>
          <Typography variant="body1" color="text.secondary">
            This page summarizes in-production orders into product quantities for production.
          </Typography>
        </div>
        <Box>
          <Button variant="outlined" startIcon={<Print />} onClick={handlePrint} sx={{ mr: 1 }}>
            Print
          </Button>
          <Button variant="outlined" startIcon={<Download />} onClick={handleExport}>
            Export CSV
          </Button>
        </Box>
      </Box>

      {error && <Alert severity="error" sx={{ mb: 3 }}>{error instanceof Error ? error.message : 'Unable to load production list'}</Alert>}

      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Production Period
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Current production queue - {productionItems.length} product line(s) to produce
          </Typography>
        </CardContent>
      </Card>

      <Card>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Production Summary by Product
          </Typography>
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Product</TableCell>
                  <TableCell>SKU</TableCell>
                  <TableCell align="right">Total Quantity</TableCell>
                  <TableCell align="right">Produced Quantity</TableCell>
                  <TableCell>Production Status</TableCell>
                  <TableCell>Related Orders</TableCell>
                  <TableCell>Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {productionItems.map((item) => (
                  <TableRow key={item.productId}>
                    <TableCell>{item.productName}</TableCell>
                    <TableCell>{item.sku}</TableCell>
                    <TableCell align="right">
                      <Chip label={item.totalQuantity} color="primary" sx={{ minWidth: 60 }} />
                    </TableCell>
                    <TableCell align="right">
                      <Chip
                        label={`${item.producedQuantity} / ${item.totalQuantity}`}
                        color={item.producedQuantity === item.totalQuantity ? 'success' : 'default'}
                        sx={{ minWidth: 80 }}
                      />
                    </TableCell>
                    <TableCell>
                      <Chip
                        label={formatProductionStatus(item.status)}
                        size="small"
                        sx={{ bgcolor: getProductionStatusColor(item.status), color: 'white' }}
                      />
                    </TableCell>
                    <TableCell>
                      <Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap' }}>
                        {item.orderNumbers.map((orderNum) => (
                          <Chip key={orderNum} label={orderNum} size="small" variant="outlined" />
                        ))}
                      </Box>
                    </TableCell>
                    <TableCell>{getActionButton(item)}</TableCell>
                  </TableRow>
                ))}
                {productionItems.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={7} align="center">No production items found</TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </TableContainer>
        </CardContent>
      </Card>

      <Dialog open={updateDialog} onClose={() => setUpdateDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Update Produced Quantity</DialogTitle>
        <DialogContent>
          {selectedItem && (
            <Box sx={{ pt: 2 }}>
              <Typography variant="body2" color="text.secondary" gutterBottom>
                Product: {selectedItem.productName}
              </Typography>
              <Typography variant="body2" color="text.secondary" gutterBottom sx={{ mb: 3 }}>
                Total Quantity Required: {selectedItem.totalQuantity}
              </Typography>

              <TextField
                label="Produced Quantity"
                type="number"
                fullWidth
                value={updateQuantity}
                onChange={(e) => setUpdateQuantity(e.target.value)}
                inputProps={{ min: 0, max: selectedItem.totalQuantity }}
                helperText={`Enter a value between 0 and ${selectedItem.totalQuantity}`}
              />
            </Box>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setUpdateDialog(false)}>Cancel</Button>
          <Button onClick={handleSaveQuantity} variant="contained" disabled={!updateQuantity || updateQuantityMutation.isPending}>
            Update Quantity
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
