import { useEffect, useState } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  TextField,
  Grid,
  MenuItem,
} from '@mui/material';
import type { Customer } from '@/entities/types';
import { toast } from 'sonner';
import ConfirmDialog from '@/shared/ui/ConfirmDialog/ConfirmDialog';

interface EditCustomerDialogProps {
  open: boolean;
  customer: Customer;
  onClose: () => void;
  onSave: (customer: Customer) => Promise<void> | void;
}

export default function EditCustomerDialog({
  open,
  customer,
  onClose,
  onSave,
}: EditCustomerDialogProps) {
  const [formData, setFormData] = useState(customer);
  const [isSaving, setIsSaving] = useState(false);
  const [confirmStatus, setConfirmStatus] = useState<Customer['accountStatus'] | null>(null);

  useEffect(() => {
    setFormData(customer);
  }, [customer]);

  const saveCustomer = async () => {
    try {
      setIsSaving(true);
      await onSave(formData);
      toast.success('Customer updated successfully');
      onClose();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Unable to update customer');
    } finally {
      setIsSaving(false);
    }
  };

  const handleSave = async () => {
    if (
      formData.accountStatus !== customer.accountStatus &&
      (formData.accountStatus === 'Suspended' || formData.accountStatus === 'Archived')
    ) {
      setConfirmStatus(formData.accountStatus);
      return;
    }

    await saveCustomer();
  };

  const handleChange = (field: keyof Customer, value: any) => {
    setFormData({ ...formData, [field]: value });
  };

  return (
    <>
      <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
        <DialogTitle>Edit Customer</DialogTitle>
        <DialogContent>
        <Grid container spacing={2} sx={{ mt: 1 }}>
          <Grid size={{ xs: 12, md: 6 }}>
            <TextField
              label="Business Name"
              fullWidth
              value={formData.businessName}
              onChange={(e) => handleChange('businessName', e.target.value)}
            />
          </Grid>
          <Grid size={{ xs: 12, md: 6 }}>
            <TextField
              label="Contact Person"
              fullWidth
              value={formData.contactPerson}
              onChange={(e) => handleChange('contactPerson', e.target.value)}
            />
          </Grid>
          <Grid size={{ xs: 12, md: 6 }}>
            <TextField
              label="Email"
              type="email"
              fullWidth
              value={formData.email}
              onChange={(e) => handleChange('email', e.target.value)}
            />
          </Grid>
          <Grid size={{ xs: 12, md: 6 }}>
            <TextField
              label="Phone"
              fullWidth
              value={formData.phone}
              onChange={(e) => handleChange('phone', e.target.value)}
            />
          </Grid>
          <Grid size={{ xs: 12 }}>
            <TextField
              label="Billing Address"
              fullWidth
              multiline
              rows={2}
              value={formData.billingAddress}
              onChange={(e) => handleChange('billingAddress', e.target.value)}
            />
          </Grid>
          <Grid size={{ xs: 12 }}>
            <TextField
              label="Delivery Address"
              fullWidth
              multiline
              rows={2}
              value={formData.deliveryAddress}
              onChange={(e) => handleChange('deliveryAddress', e.target.value)}
            />
          </Grid>
          <Grid size={{ xs: 12, md: 6 }}>
            <TextField
              label="Payment Terms"
              select
              fullWidth
              value={formData.paymentTerms}
              onChange={(e) => handleChange('paymentTerms', e.target.value)}
              helperText="Set when invoices are due for this customer"
            >
              <MenuItem key="7days" value="Net 7">7 days from invoice</MenuItem>
              <MenuItem key="14days" value="Net 14">14 days from invoice</MenuItem>
              <MenuItem key="30days" value="Net 30">30 days from invoice</MenuItem>
              <MenuItem key="monthly" value="Monthly statement">Monthly statement</MenuItem>
              <MenuItem key="delivery" value="Pay on delivery">Pay on delivery</MenuItem>
              <MenuItem key="upfront" value="Upfront payment">Upfront payment required</MenuItem>
              <MenuItem key="custom" value="Custom terms">Custom terms</MenuItem>
            </TextField>
          </Grid>
          <Grid size={{ xs: 12, md: 6 }}>
            <TextField
              label="Account Status"
              select
              fullWidth
              value={formData.accountStatus}
              onChange={(e) => handleChange('accountStatus', e.target.value as any)}
            >
              <MenuItem key="draft" value="Draft">Draft</MenuItem>
              <MenuItem key="invited" value="Invited">Invited</MenuItem>
              <MenuItem key="active" value="Active">Active</MenuItem>
              <MenuItem key="suspended" value="Suspended">Suspended</MenuItem>
              <MenuItem key="archived" value="Archived">Archived</MenuItem>
            </TextField>
          </Grid>
        </Grid>
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose}>Cancel</Button>
          <Button onClick={handleSave} variant="contained" disabled={isSaving}>
            Save Changes
          </Button>
        </DialogActions>
      </Dialog>
      <ConfirmDialog
        open={Boolean(confirmStatus)}
        title={confirmStatus === 'Archived' ? 'Archive Customer' : 'Suspend Customer'}
        message={
          confirmStatus === 'Archived'
            ? 'Archive this customer? The backend will reject the change if the customer still has active standing orders, open orders, or unsettled invoices.'
            : 'Suspend this customer? They will keep historical data but cannot sign in or generate new standing-order orders.'
        }
        confirmLabel={confirmStatus === 'Archived' ? 'Archive Customer' : 'Suspend Customer'}
        confirmColor={confirmStatus === 'Archived' ? 'warning' : 'error'}
        isConfirming={isSaving}
        onCancel={() => setConfirmStatus(null)}
        onConfirm={() => {
          setConfirmStatus(null);
          void saveCustomer();
        }}
      />
    </>
  );
}
