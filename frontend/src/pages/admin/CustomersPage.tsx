import { useEffect, useState } from 'react';
import { Box, Typography, Card, CardContent, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Button, Chip, IconButton, Alert, CircularProgress } from '@mui/material';
import { Add, Send, Visibility } from '@mui/icons-material';
import { Link } from 'react-router';
import { toast } from 'sonner';
import CreateCustomerDialog from '@/features/customerCreate/ui/CreateCustomerDialog';
import { Customer } from '@/entities/types';
import { getAdminCustomers, type CustomerPayload } from '@/entities/customer/api/customerApi';
import { createAdminCustomer } from '@/features/customerCreate/api/customerCreateApi';
import { sendAdminCustomerInvite } from '@/features/customerInvite/api/customerInviteApi';

export default function Customers() {
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    const loadCustomers = async () => {
      try {
        setError('');
        setCustomers(await getAdminCustomers());
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Unable to load customers');
      } finally {
        setIsLoading(false);
      }
    };

    void loadCustomers();
  }, []);

  const handleCreateCustomer = async (customerData: CustomerPayload) => {
    const customer = await createAdminCustomer(customerData);
    setCustomers((currentCustomers) => [...currentCustomers, customer].sort((a, b) => a.businessName.localeCompare(b.businessName)));
  };

  const handleSendInvite = async (customer: Customer) => {
    try {
      const updatedCustomer = await sendAdminCustomerInvite(customer.id);
      setCustomers((currentCustomers) =>
        currentCustomers.map((item) => item.id === updatedCustomer.id ? updatedCustomer : item)
      );
      toast.success(`Invite sent to ${updatedCustomer.email}`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Unable to send invite');
    }
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

      {error && <Alert severity="error" sx={{ mb: 3 }}>{error}</Alert>}

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
                        disabled={customer.accountStatus === 'Suspended' || customer.accountStatus === 'Archived'}
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
