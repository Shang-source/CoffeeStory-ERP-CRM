import { useState } from 'react';
import { Box, Typography, Card, CardContent, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Chip, IconButton, Collapse, Alert, CircularProgress } from '@mui/material';
import { Send, Download, KeyboardArrowDown, KeyboardArrowUp } from '@mui/icons-material';
import { formatInvoiceStatus, getInvoiceStatusColor } from '@/shared/status/statusFormat';
import { toast } from 'sonner';
import { Invoice } from '@/entities/types';
import { getAdminInvoices } from '@/entities/invoice/api/invoiceApi';
import { downloadAdminInvoicePdf, sendInvoiceEmail } from '@/features/invoiceActions/api/invoiceActionsApi';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { queryKeys } from '@/shared/api/queryKeys';

function InvoiceRow({
  invoice,
  isSending,
  onSendEmail,
}: {
  invoice: Invoice;
  isSending: boolean;
  onSendEmail: (invoiceId: string) => void;
}) {
  const [open, setOpen] = useState(false);

  const handleDownload = async () => {
    try {
      await downloadAdminInvoicePdf(invoice.id);
      toast.success(`Downloading invoice ${invoice.invoiceNumber}`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Unable to download invoice');
    }
  };

  return (
    <>
      <TableRow>
        <TableCell>
          <IconButton size="small" onClick={() => setOpen(!open)}>
            {open ? <KeyboardArrowUp /> : <KeyboardArrowDown />}
          </IconButton>
        </TableCell>
        <TableCell>{invoice.invoiceNumber}</TableCell>
        <TableCell>{invoice.customer?.businessName}</TableCell>
        <TableCell>{invoice.issueDate.toLocaleDateString()}</TableCell>
        <TableCell>{invoice.dueDate.toLocaleDateString()}</TableCell>
        <TableCell align="right">${invoice.totalAmount.toFixed(2)}</TableCell>
        <TableCell align="right">${invoice.outstandingAmount.toFixed(2)}</TableCell>
        <TableCell>
          <Chip
            label={formatInvoiceStatus(invoice.status)}
            size="small"
            sx={{ bgcolor: getInvoiceStatusColor(invoice.status), color: 'white' }}
          />
        </TableCell>
        <TableCell>
          <IconButton
            size="small"
            onClick={() => onSendEmail(invoice.id)}
            disabled={isSending || invoice.status === 'Paid' || invoice.status === 'Cancelled'}
          >
            <Send />
          </IconButton>
          <IconButton size="small" onClick={handleDownload}>
            <Download />
          </IconButton>
        </TableCell>
      </TableRow>
      <TableRow>
        <TableCell style={{ paddingBottom: 0, paddingTop: 0 }} colSpan={9}>
          <Collapse in={open} timeout="auto" unmountOnExit>
            <Box sx={{ margin: 2 }}>
              <Typography variant="subtitle2" gutterBottom>
                Invoice Items
              </Typography>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Description</TableCell>
                    <TableCell align="right">Quantity</TableCell>
                    <TableCell align="right">Unit Price</TableCell>
                    <TableCell align="right">Total</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {invoice.items.map((item) => (
                    <TableRow key={item.id}>
                      <TableCell>{item.description}</TableCell>
                      <TableCell align="right">{item.quantity}</TableCell>
                      <TableCell align="right">${item.unitPrice.toFixed(2)}</TableCell>
                      <TableCell align="right">${item.lineTotal.toFixed(2)}</TableCell>
                    </TableRow>
                  ))}
                  <TableRow>
                    <TableCell colSpan={3} align="right">Subtotal:</TableCell>
                    <TableCell align="right">${invoice.subtotal.toFixed(2)}</TableCell>
                  </TableRow>
                  <TableRow>
                    <TableCell colSpan={3} align="right">GST (15%):</TableCell>
                    <TableCell align="right">${invoice.gstAmount.toFixed(2)}</TableCell>
                  </TableRow>
                  <TableRow>
                    <TableCell colSpan={3} align="right"><strong>Total:</strong></TableCell>
                    <TableCell align="right"><strong>${invoice.totalAmount.toFixed(2)}</strong></TableCell>
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

export default function Invoices() {
  const queryClient = useQueryClient();
  const { data: invoices = [], isLoading, error } = useQuery({
    queryKey: queryKeys.adminInvoices,
    queryFn: getAdminInvoices,
  });

  const sendEmailMutation = useMutation({
    mutationFn: sendInvoiceEmail,
    onSuccess: (updatedInvoice) => {
      queryClient.setQueryData<Invoice[]>(queryKeys.adminInvoices, (currentInvoices = []) =>
        currentInvoices.map((invoice) => invoice.id === updatedInvoice.id ? updatedInvoice : invoice)
      );
      toast.success(`Invoice ${updatedInvoice.invoiceNumber} sent to ${updatedInvoice.customer?.email ?? 'customer'}`);
    },
    onError: (err) => {
      toast.error(err instanceof Error ? err.message : 'Unable to send invoice');
    },
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
        Invoices
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
        Manage customer invoices
      </Typography>

      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {error instanceof Error ? error.message : 'Unable to load invoices'}
        </Alert>
      )}

      <Card>
        <CardContent>
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell />
                  <TableCell>Invoice #</TableCell>
                  <TableCell>Customer</TableCell>
                  <TableCell>Issue Date</TableCell>
                  <TableCell>Due Date</TableCell>
                  <TableCell align="right">Total</TableCell>
                  <TableCell align="right">Amount Due</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {invoices.map((invoice) => (
                  <InvoiceRow
                    key={invoice.id}
                    invoice={invoice}
                    isSending={sendEmailMutation.isPending && sendEmailMutation.variables === invoice.id}
                    onSendEmail={(invoiceId) => sendEmailMutation.mutate(invoiceId)}
                  />
                ))}
                {invoices.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={9} align="center">No invoices found</TableCell>
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
