import { useEffect, useState } from 'react';
import { Box, Typography, Card, CardContent, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Chip, Button, Dialog, DialogTitle, DialogContent, DialogActions, TextField, MenuItem, Alert, CircularProgress } from '@mui/material';
import { CheckCircle, Undo, Warning } from '@mui/icons-material';
import { formatInvoiceStatus, getInvoiceStatusColor } from '@/shared/status/statusFormat';
import { toast } from 'sonner';
import { Invoice } from '@/entities/types';
import { getAdminInvoices } from '@/entities/invoice/api/invoiceApi';
import { markOverdueInvoices } from '@/features/invoiceActions/api/invoiceActionsApi';
import { recordInvoicePayment, voidInvoicePayment } from '@/features/paymentRecord/api/paymentRecordApi';

export default function Payments() {
  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [openDialog, setOpenDialog] = useState(false);
  const [selectedInvoice, setSelectedInvoice] = useState<Invoice | null>(null);
  const [paymentData, setPaymentData] = useState({
    amount: '',
    paymentDate: new Date().toISOString().split('T')[0],
    paymentMethod: 'BankTransfer',
    reference: '',
    note: '',
  });

  useEffect(() => {
    const loadInvoices = async () => {
      try {
        setError('');
        setInvoices(await getAdminInvoices());
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Unable to load invoices');
      } finally {
        setIsLoading(false);
      }
    };

    void loadInvoices();
  }, []);

  const unpaidInvoices = invoices.filter(inv => inv.status !== 'Paid' && inv.status !== 'Cancelled');
  const recordedPayments = invoices.flatMap((invoice) =>
    (invoice.payments ?? []).map((payment) => ({ invoice, payment }))
  ).sort((a, b) => b.payment.paymentDate.getTime() - a.payment.paymentDate.getTime());

  const handleMarkPaid = (invoice: Invoice) => {
    setSelectedInvoice(invoice);
    setPaymentData({
      amount: invoice.outstandingAmount.toFixed(2),
      paymentDate: new Date().toISOString().split('T')[0],
      paymentMethod: 'BankTransfer',
      reference: '',
      note: '',
    });
    setOpenDialog(true);
  };

  const handleSavePayment = async () => {
    if (!selectedInvoice) {
      return;
    }

    try {
      const updatedInvoice = await recordInvoicePayment(selectedInvoice.id, {
        amount: Number(paymentData.amount),
        paymentDate: new Date(paymentData.paymentDate).toISOString(),
        paymentMethod: paymentData.paymentMethod,
        reference: paymentData.reference,
        note: paymentData.note || undefined,
      });
      setInvoices((currentInvoices) =>
        currentInvoices.map((invoice) => invoice.id === updatedInvoice.id ? updatedInvoice : invoice)
      );
      toast.success(`Payment of $${paymentData.amount} recorded for invoice ${selectedInvoice.invoiceNumber}`);
      setOpenDialog(false);
      setSelectedInvoice(null);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Unable to record payment');
    }
  };

  const handleMarkOverdue = async () => {
    try {
      const result = await markOverdueInvoices();
      setInvoices(await getAdminInvoices());
      toast.success(`${result.updatedCount} invoice(s) marked overdue`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Unable to mark overdue invoices');
    }
  };

  const handleVoidPayment = async (invoiceId: string, paymentId: string) => {
    const reason = window.prompt('Reason for voiding this payment');
    if (!reason?.trim()) {
      return;
    }

    try {
      const updatedInvoice = await voidInvoicePayment(invoiceId, paymentId, reason);
      setInvoices((currentInvoices) =>
        currentInvoices.map((invoice) => invoice.id === updatedInvoice.id ? updatedInvoice : invoice)
      );
      toast.success('Payment voided');
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Unable to void payment');
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
      <Typography variant="h4" gutterBottom>
        Payments
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
        Record and manage customer payments
      </Typography>

      <Box sx={{ mb: 3 }}>
        <Button variant="outlined" startIcon={<Warning />} onClick={handleMarkOverdue}>
          Mark Overdue Invoices
        </Button>
      </Box>

      {error && <Alert severity="error" sx={{ mb: 3 }}>{error}</Alert>}

      <Card>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Unpaid Invoices
          </Typography>
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Invoice #</TableCell>
                  <TableCell>Customer</TableCell>
                  <TableCell>Issue Date</TableCell>
                  <TableCell>Due Date</TableCell>
                  <TableCell align="right">Amount Due</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell align="center">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {unpaidInvoices.map((invoice) => (
                  <TableRow key={invoice.id}>
                    <TableCell>{invoice.invoiceNumber}</TableCell>
                    <TableCell>{invoice.customer?.businessName}</TableCell>
                    <TableCell>{invoice.issueDate.toLocaleDateString()}</TableCell>
                    <TableCell>{invoice.dueDate.toLocaleDateString()}</TableCell>
                    <TableCell align="right">${invoice.outstandingAmount.toFixed(2)}</TableCell>
                    <TableCell>
                      <Chip
                        label={formatInvoiceStatus(invoice.status)}
                        size="small"
                        sx={{ bgcolor: getInvoiceStatusColor(invoice.status), color: 'white' }}
                      />
                    </TableCell>
                    <TableCell align="center">
                      <Button
                        variant="contained"
                        size="small"
                        startIcon={<CheckCircle />}
                        onClick={() => handleMarkPaid(invoice)}
                      >
                        Record Payment
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
                {unpaidInvoices.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={7} align="center">No unpaid invoices</TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </TableContainer>
        </CardContent>
      </Card>

      <Card sx={{ mt: 3 }}>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Payment Records
          </Typography>
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Invoice #</TableCell>
                  <TableCell>Customer</TableCell>
                  <TableCell>Date</TableCell>
                  <TableCell>Reference</TableCell>
                  <TableCell align="right">Amount</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell align="center">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {recordedPayments.map(({ invoice, payment }) => (
                  <TableRow key={payment.id}>
                    <TableCell>{invoice.invoiceNumber}</TableCell>
                    <TableCell>{invoice.customer?.businessName}</TableCell>
                    <TableCell>{payment.paymentDate.toLocaleDateString()}</TableCell>
                    <TableCell>{payment.reference}</TableCell>
                    <TableCell align="right">${payment.amount.toFixed(2)}</TableCell>
                    <TableCell>
                      <Chip label={payment.isVoided ? 'Voided' : 'Active'} color={payment.isVoided ? 'default' : 'success'} size="small" />
                    </TableCell>
                    <TableCell align="center">
                      <Button
                        size="small"
                        startIcon={<Undo />}
                        onClick={() => handleVoidPayment(invoice.id, payment.id)}
                        disabled={payment.isVoided}
                      >
                        Void
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
                {recordedPayments.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={7} align="center">No payment records</TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </TableContainer>
        </CardContent>
      </Card>

      <Dialog open={openDialog} onClose={() => setOpenDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Record Payment</DialogTitle>
        <DialogContent>
          {selectedInvoice && (
            <Box sx={{ pt: 2 }}>
              <Typography variant="body2" color="text.secondary" gutterBottom>
                Invoice: {selectedInvoice.invoiceNumber}
              </Typography>
              <Typography variant="body2" color="text.secondary" gutterBottom sx={{ mb: 3 }}>
                Customer: {selectedInvoice.customer?.businessName}
              </Typography>

              <Typography variant="body2" color="text.secondary" gutterBottom sx={{ mb: 2 }}>
                Amount Due: ${selectedInvoice.outstandingAmount.toFixed(2)}
              </Typography>

              <TextField
                label="Payment Date"
                type="date"
                fullWidth
                value={paymentData.paymentDate}
                onChange={(e) => setPaymentData({ ...paymentData, paymentDate: e.target.value })}
                sx={{ mb: 2 }}
                InputLabelProps={{ shrink: true }}
              />

              <TextField
                label="Amount Paid"
                type="number"
                fullWidth
                value={paymentData.amount}
                onChange={(e) => setPaymentData({ ...paymentData, amount: e.target.value })}
                sx={{ mb: 2 }}
                inputProps={{ step: '0.01', min: 0.01, max: selectedInvoice.outstandingAmount }}
              />

              <TextField
                label="Payment Method"
                select
                fullWidth
                value={paymentData.paymentMethod}
                onChange={(e) => setPaymentData({ ...paymentData, paymentMethod: e.target.value })}
                sx={{ mb: 2 }}
              >
                <MenuItem key="bank" value="BankTransfer">Bank Transfer</MenuItem>
                <MenuItem key="cash" value="Cash">Cash</MenuItem>
                <MenuItem key="cheque" value="Cheque">Cheque</MenuItem>
                <MenuItem key="other" value="Other">Other</MenuItem>
              </TextField>

              <TextField
                label="Payment Reference"
                fullWidth
                value={paymentData.reference}
                onChange={(e) => setPaymentData({ ...paymentData, reference: e.target.value })}
                sx={{ mb: 2 }}
                placeholder="e.g., Transaction ID, cheque number"
              />

              <TextField
                label="Notes"
                fullWidth
                multiline
                rows={3}
                value={paymentData.note}
                onChange={(e) => setPaymentData({ ...paymentData, note: e.target.value })}
              />
            </Box>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenDialog(false)}>Cancel</Button>
          <Button
            onClick={handleSavePayment}
            variant="contained"
            disabled={!paymentData.amount || Number(paymentData.amount) <= 0}
          >
            Record Payment
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
