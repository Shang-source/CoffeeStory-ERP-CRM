import { Box, Typography, Card, CardContent, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Chip, IconButton, Collapse, Button } from '@mui/material';
import { Download, KeyboardArrowDown, KeyboardArrowUp, Visibility } from '@mui/icons-material';
import { useState } from 'react';
import { Link } from 'react-router';
import { formatInvoiceStatus, getInvoiceStatusColor } from '@/shared/status/statusFormat';
import { toast } from 'sonner';
import { Invoice } from '@/entities/types';
import { useCustomerInvoicesQuery } from '@/entities/invoice/api/invoiceQueries';
import { downloadCustomerInvoicePdf } from '@/features/invoiceActions/api/invoiceActionsApi';
import { LoadingState } from '@/shared/ui/LoadingState';
import { ErrorState } from '@/shared/ui/ErrorState';
import { EmptyState } from '@/shared/ui/EmptyState';

function InvoiceRow({ invoice }: { invoice: Invoice }) {
  const [open, setOpen] = useState(false);

  const handleDownload = async () => {
    try {
      await downloadCustomerInvoicePdf(invoice.id);
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
          <Button
            component={Link}
            to={`/customer/invoices/${invoice.id}`}
            size="small"
            startIcon={<Visibility />}
            sx={{ mr: 1 }}
          >
            View
          </Button>
          <IconButton size="small" onClick={handleDownload}>
            <Download />
          </IconButton>
        </TableCell>
      </TableRow>
      <TableRow>
        <TableCell style={{ paddingBottom: 0, paddingTop: 0 }} colSpan={8}>
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

export default function CustomerInvoices() {
  const { data: invoices = [], isLoading, error } = useCustomerInvoicesQuery();

  if (isLoading) {
    return <LoadingState />;
  }

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Invoices
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
        View and download your invoices
      </Typography>

      {error && <ErrorState message={error instanceof Error ? error.message : 'Unable to load invoices'} />}

      <Card>
        <CardContent>
          {invoices.length === 0 && !error ? (
            <EmptyState title="No invoices found" description="Invoices will appear here once StoryCoffee issues them." />
          ) : (
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell />
                  <TableCell>Invoice #</TableCell>
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
                  <InvoiceRow key={invoice.id} invoice={invoice} />
                ))}
              </TableBody>
            </Table>
          </TableContainer>
          )}
        </CardContent>
      </Card>
    </Box>
  );
}
