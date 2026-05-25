import { useState } from 'react';
import { Box, Typography, Card, CardContent, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Button, Chip, IconButton, Alert, CircularProgress } from '@mui/material';
import { Add, Send, Visibility } from '@mui/icons-material';
import { Link } from 'react-router';
import CreateCustomerDialog from '@/features/customerCreate/ui/CreateCustomerDialog';
import { Customer } from '@/entities/types';
import { type CustomerPayload } from '@/entities/customer/api/customerApi';
import { useAdminCustomersQuery } from '@/entities/customer/api/customerQueries';
import { useCreateAdminCustomerMutation } from '@/features/customerCreate/model/customerCreateMutations';
import { useSendAdminCustomerInviteMutation } from '@/features/customerInvite/model/customerInviteMutations';
import { formatAccountStatus, getAccountStatusColor } from '@/shared/status/statusFormat';
import { StatusChip } from '@/shared/ui/StatusChip';

export default function Customers() {
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const { data: customers = [], isLoading, error } = useAdminCustomersQuery();
  const createCustomerMutation = useCreateAdminCustomerMutation();
  const sendInviteMutation = useSendAdminCustomerInviteMutation();

  const handleCreateCustomer = async (customerData: CustomerPayload) => {
    await createCustomerMutation.mutateAsync(customerData);
  };

  const handleSendInvite = (customer: Customer) => {
    sendInviteMutation.mutate(customer.id);
  };

  const canSendInvite = (customer: Customer) =>
    !customer.hasPortalUser &&
    customer.accountStatus !== 'Suspended' &&
    customer.accountStatus !== 'Archived' &&
    customer.phone.trim().length > 0;

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
            Customers
          </Typography>
          <Typography variant="body1" color="text.secondary">
            Manage customer accounts
          </Typography>
        </div>
        <Button variant="contained" startIcon={<Add />} onClick={() => setCreateDialogOpen(true)}>
          Create Customer
        </Button>
      </Box>

      {error && <Alert severity="error" sx={{ mb: 3 }}>{error instanceof Error ? error.message : 'Unable to load customers'}</Alert>}

      <Card>
        <CardContent>
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Business Name</TableCell>
                  <TableCell>Contact Person</TableCell>
                  <TableCell>Email</TableCell>
                  <TableCell>Phone</TableCell>
                  <TableCell>Payment Terms</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Portal User</TableCell>
                  <TableCell align="center">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {customers.map((customer) => (
                  <TableRow key={customer.id}>
                    <TableCell>{customer.businessName}</TableCell>
                    <TableCell>{customer.contactPerson}</TableCell>
                    <TableCell>{customer.email}</TableCell>
                    <TableCell>{customer.phone}</TableCell>
                    <TableCell>{customer.paymentTerms}</TableCell>
                    <TableCell>
                      <StatusChip label={formatAccountStatus(customer.accountStatus)} color={getAccountStatusColor(customer.accountStatus)} />
                    </TableCell>
                    <TableCell>
                      <Chip label={customer.hasPortalUser ? 'Created' : 'Not invited'} color={customer.hasPortalUser ? 'success' : 'default'} size="small" />
                    </TableCell>
                    <TableCell align="center">
                      <IconButton
                        size="small"
                        onClick={() => handleSendInvite(customer)}
                        disabled={!canSendInvite(customer) || sendInviteMutation.isPending}
                        title={customer.hasPortalUser ? 'Portal user already exists' : 'Create portal user and send invite email'}
                      >
                        <Send />
                      </IconButton>
                      <IconButton component={Link} to={`/admin/customers/${customer.id}`} size="small">
                        <Visibility />
                      </IconButton>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </CardContent>
      </Card>

      <CreateCustomerDialog
        open={createDialogOpen}
        onClose={() => setCreateDialogOpen(false)}
        onCreate={handleCreateCustomer}
      />
    </Box>
  );
}
