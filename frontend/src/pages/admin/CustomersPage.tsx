import { useState } from 'react';
import { Box, Typography, Card, CardContent, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Button, Chip, IconButton, Alert, CircularProgress } from '@mui/material';
import { Add, Send, Visibility } from '@mui/icons-material';
import { Link } from 'react-router';
import { toast } from 'sonner';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import CreateCustomerDialog from '@/features/customerCreate/ui/CreateCustomerDialog';
import { Customer } from '@/entities/types';
import { type CustomerPayload } from '@/entities/customer/api/customerApi';
import { useAdminCustomersQuery } from '@/entities/customer/api/customerQueries';
import { createAdminCustomer } from '@/features/customerCreate/api/customerCreateApi';
import { sendAdminCustomerInvite } from '@/features/customerInvite/api/customerInviteApi';
import { queryKeys } from '@/shared/api/queryKeys';

export default function Customers() {
  const queryClient = useQueryClient();
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const { data: customers = [], isLoading, error } = useAdminCustomersQuery();

  const createCustomerMutation = useMutation({
    mutationFn: (customer: CustomerPayload) => createAdminCustomer(customer),
    onSuccess: (customer) => {
      queryClient.setQueryData<Customer[]>(queryKeys.adminCustomers, (currentCustomers = []) =>
        [...currentCustomers, customer].sort((a, b) => a.businessName.localeCompare(b.businessName))
      );
    },
  });

  const sendInviteMutation = useMutation({
    mutationFn: (customerId: string) => sendAdminCustomerInvite(customerId),
    onSuccess: (updatedCustomer) => {
      queryClient.setQueryData<Customer[]>(queryKeys.adminCustomers, (currentCustomers = []) =>
        currentCustomers.map((item) => item.id === updatedCustomer.id ? updatedCustomer : item)
      );
      queryClient.setQueryData<Customer>(queryKeys.adminCustomer(updatedCustomer.id), updatedCustomer);
      toast.success(`Invite sent to ${updatedCustomer.email}`);
    },
    onError: (err) => toast.error(err instanceof Error ? err.message : 'Unable to send invite'),
  });

  const handleCreateCustomer = async (customerData: CustomerPayload) => {
    await createCustomerMutation.mutateAsync(customerData);
  };

  const handleSendInvite = (customer: Customer) => {
    sendInviteMutation.mutate(customer.id);
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
                      <Chip label={customer.accountStatus} color={customer.accountStatus === 'Active' ? 'success' : 'default'} size="small" />
                    </TableCell>
                    <TableCell align="center">
                      <IconButton
                        size="small"
                        onClick={() => handleSendInvite(customer)}
                        disabled={
                          customer.accountStatus === 'Suspended' ||
                          customer.accountStatus === 'Archived' ||
                          sendInviteMutation.isPending
                        }
                        title="Send invite email"
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
