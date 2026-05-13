import { useState } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  TextField,
  Grid,
  MenuItem,
  Typography,
  Divider,
} from '@mui/material';
import { toast } from 'sonner';
import type { CustomerPayload } from '@/features/customerCreate/api/customerCreateApi';

interface CreateCustomerDialogProps {
  open: boolean;
  onClose: () => void;
  onCreate: (customerData: CustomerPayload) => Promise<void> | void;
}

export default function CreateCustomerDialog({
  open,
  onClose,
  onCreate,
}: CreateCustomerDialogProps) {
  const [formData, setFormData] = useState({
    businessName: '',
    contactPerson: '',
    email: '',
    phone: '',
    billingAddress: '',
    deliveryAddress: '',
    paymentTerms: 'Net 14',
    accountStatus: 'Draft' as CustomerPayload['accountStatus'],
  });
  const [isSaving, setIsSaving] = useState(false);

  const handleCreate = async () => {
    if (!formData.businessName || !formData.email) {
      toast.error('Please fill in required fields');
      return;
    }

    try {
      setIsSaving(true);
      await onCreate(formData);
      toast.success('Customer created successfully');
      onClose();
      setFormData({
        businessName: '',
        contactPerson: '',
        email: '',
        phone: '',
        billingAddress: '',
        deliveryAddress: '',
        paymentTerms: 'Net 14',
        accountStatus: 'Draft',
      });
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Unable to create customer');
    } finally {
      setIsSaving(false);
    }
  };

  const handleChange = (field: string, value: any) => {
    setFormData({ ...formData, [field]: value });
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>Create New Customer</DialogTitle>
      <DialogContent>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2, mt: 1 }}>
          Create a new business customer account. Invitation email delivery is not enabled in this phase.
        </Typography>

        <Divider sx={{ mb: 3 }} />

        <Grid container spacing={2}>
          <Grid size={{ xs: 12, md: 6 }}>
            <TextField
              label="Business Name *"
              fullWidth
              value={formData.businessName}
              onChange={(e) => handleChange('businessName', e.target.value)}
              required
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
              label="Email *"
              type="email"
              fullWidth
              value={formData.email}
              onChange={(e) => handleChange('email', e.target.value)}
              required
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

          <Grid size={{ xs: 12 }}>
            <Divider sx={{ my: 2 }} />
            <Typography variant="subtitle2" gutterBottom>
              Payment Configuration
            </Typography>
            <Typography variant="caption" color="text.secondary" display="block" sx={{ mb: 2 }}>
              Set the payment terms that will apply to all invoices for this customer
            </Typography>
          </Grid>

          <Grid size={{ xs: 12, md: 6 }}>
            <TextField
              label="Payment Terms *"
              select
              fullWidth
              value={formData.paymentTerms}
              onChange={(e) => handleChange('paymentTerms', e.target.value)}
              helperText="When invoices are due for this customer"
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
              label="Initial Status"
              select
              fullWidth
              value={formData.accountStatus}
              onChange={(e) => handleChange('accountStatus', e.target.value)}
              helperText="Set to Draft to review before sending invite"
            >
              <MenuItem key="draft" value="Draft">Draft (not yet invited)</MenuItem>
              <MenuItem key="invited" value="Invited">Invited (send invitation email)</MenuItem>
              <MenuItem key="active" value="Active">Active (already set up)</MenuItem>
            </TextField>
          </Grid>
        </Grid>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button onClick={handleCreate} variant="contained" disabled={isSaving}>
          Create Customer
        </Button>
      </DialogActions>
    </Dialog>
  );
}
