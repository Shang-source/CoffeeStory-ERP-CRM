import { useState } from 'react';
import { Box, Typography, Card, CardContent, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Chip, IconButton, Collapse, Alert, CircularProgress } from '@mui/material';
import { Send, Download, KeyboardArrowDown, KeyboardArrowUp } from '@mui/icons-material';
import { formatInvoiceStatus, getInvoiceStatusColor } from '@/shared/status/statusFormat';
import { Invoice } from '@/entities/types';
import { useAdminInvoicesQuery } from '@/entities/invoice/api/invoiceQueries';
import { useDownloadInvoicePdfMutation, useSendInvoiceEmailMutation } from '@/features/invoiceActions/model/invoiceActionsMutations';

function InvoiceRow({
  invoice,
  isSending,
  isDownloading,
  onSendEmail,
  onDownload,
}: {
  invoice: Invoice;
  isSending: boolean;
  isDownloading: boolean;
  onSendEmail: (invoiceId: string) => void;
  onDownload: (invoice: Invoice) => void;
}) {
  const [open, setOpen] = useState(false);
  const canSendEmail = invoice.status === 'Draft' || invoice.status === 'Issued';

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
            disabled={isSending || !canSendEmail}
            title={canSendEmail ? 'Send invoice email' : 'Only draft or issued invoices can be sent'}
          >
            <Send />
          </IconButton>
          <IconButton size="small" onClick={() => onDownload(invoice)} disabled={isDownloading}>
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
  const { data: invoices = [], isLoading, error } = useAdminInvoicesQuery();
  const sendEmailMutation = useSendInvoiceEmailMutation();
  const downloadInvoiceMutation = useDownloadInvoicePdfMutation('admin');

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
                    isDownloading={downloadInvoiceMutation.isPending && downloadInvoiceMutation.variables?.invoiceId === invoice.id}
                    onSendEmail={(invoiceId) => sendEmailMutation.mutate(invoiceId)}
                    onDownload={(selectedInvoice) => downloadInvoiceMutation.mutate({
                      invoiceId: selectedInvoice.id,
                      invoiceNumber: selectedInvoice.invoiceNumber,
                    })}
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
