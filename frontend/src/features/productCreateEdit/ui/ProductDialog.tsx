import { useEffect, useState } from 'react';
import { Button, Dialog, DialogActions, DialogContent, DialogTitle, FormControlLabel, Grid, Switch, TextField } from '@mui/material';
import { toast } from 'sonner';
import { Product } from '@/entities/types';
import type { ProductPayload } from '@/features/productCreateEdit/api/productCreateEditApi';

interface ProductDialogProps {
  open: boolean;
  product?: Product | null;
  onClose: () => void;
  onSave: (product: ProductPayload) => Promise<void> | void;
}

const emptyProduct: ProductPayload = {
  sku: '',
  name: '',
  description: '',
  unit: 'kg',
  price: 0,
  cost: 0,
  isActive: true,
};

export default function ProductDialog({ open, product, onClose, onSave }: ProductDialogProps) {
  const [formData, setFormData] = useState<ProductPayload>(emptyProduct);
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    setFormData(product ? {
      sku: product.sku,
      name: product.name,
      description: product.description,
      unit: product.unit,
      price: product.price,
      cost: product.cost,
      isActive: product.isActive,
    } : emptyProduct);
  }, [product, open]);

  const handleSave = async () => {
    if (!formData.sku || !formData.name || !formData.unit) {
      toast.error('SKU, name, and unit are required');
      return;
    }

    try {
      setIsSaving(true);
      await onSave(formData);
      toast.success(product ? 'Product updated successfully' : 'Product created successfully');
      onClose();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Unable to save product');
    } finally {
      setIsSaving(false);
    }
  };

  const handleChange = (field: keyof ProductPayload, value: string | number | boolean) => {
    setFormData({ ...formData, [field]: value });
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>{product ? 'Edit Product' : 'Add Product'}</DialogTitle>
      <DialogContent>
        <Grid container spacing={2} sx={{ mt: 1 }}>
          <Grid size={{ xs: 12, md: 4 }}>
            <TextField
              label="SKU"
              fullWidth
              required
              value={formData.sku}
              onChange={(event) => handleChange('sku', event.target.value)}
            />
          </Grid>
          <Grid size={{ xs: 12, md: 8 }}>
            <TextField
              label="Name"
              fullWidth
              required
              value={formData.name}
              onChange={(event) => handleChange('name', event.target.value)}
            />
          </Grid>
          <Grid size={{ xs: 12 }}>
            <TextField
              label="Description"
              fullWidth
              multiline
              rows={2}
              value={formData.description}
              onChange={(event) => handleChange('description', event.target.value)}
            />
          </Grid>
          <Grid size={{ xs: 12, md: 4 }}>
            <TextField
              label="Unit"
              fullWidth
              required
              value={formData.unit}
              onChange={(event) => handleChange('unit', event.target.value)}
            />
          </Grid>
          <Grid size={{ xs: 12, md: 4 }}>
            <TextField
              label="Price"
              type="number"
              fullWidth
              value={formData.price}
              onChange={(event) => handleChange('price', Number(event.target.value))}
              inputProps={{ min: 0, step: 0.01 }}
            />
          </Grid>
          <Grid size={{ xs: 12, md: 4 }}>
            <TextField
              label="Cost"
              type="number"
              fullWidth
              value={formData.cost}
              onChange={(event) => handleChange('cost', Number(event.target.value))}
              inputProps={{ min: 0, step: 0.01 }}
            />
          </Grid>
          <Grid size={{ xs: 12 }}>
            <FormControlLabel
              control={<Switch checked={formData.isActive} onChange={(event) => handleChange('isActive', event.target.checked)} />}
              label="Active product"
            />
          </Grid>
        </Grid>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button onClick={handleSave} variant="contained" disabled={isSaving}>
          Save Product
        </Button>
      </DialogActions>
    </Dialog>
  );
}
