import { Box, Button, Card, CardContent, Chip, Divider, Grid, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Typography } from '@mui/material';
import { ArrowBack, Download } from '@mui/icons-material';
import { Link, useParams } from 'react-router';
import { useCustomerInvoiceQuery } from '@/entities/invoice/api/invoiceQueries';
import { useDownloadInvoicePdfMutation } from '@/features/invoiceActions/model/invoiceActionsMutations';
import { formatInvoiceStatus, getInvoiceStatusColor } from '@/shared/status/statusFormat';
import { LoadingState } from '@/shared/ui/LoadingState';
import { ErrorState } from '@/shared/ui/ErrorState';
import { MoneyText } from '@/shared/ui/MoneyText';

export default function CustomerInvoiceDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { data: invoice, isLoading, error } = useCustomerInvoiceQuery(id);
  const downloadInvoiceMutation = useDownloadInvoicePdfMutation('customer');

  const handleDownload = async () => {
    if (!invoice) {
      return;
    }

    downloadInvoiceMutation.mutate({
      invoiceId: invoice.id,
      invoiceNumber: invoice.invoiceNumber,
    });
  };

  if (!id) {
    return <ErrorState message="Invoice id is required" />;
  }

  if (isLoading) {
    return <LoadingState />;
  }

  if (error || !invoice) {
    return <ErrorState message={error instanceof Error ? error.message : 'Invoice not found'} />;
  }

  return (
    <Box>
      <Button component={Link} to="/customer/invoices" startIcon={<ArrowBack />} sx={{ mb: 2 }}>
        Back to Invoices
      </Button>

      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 3 }}>
        <Box>
          <Typography variant="h4" gutterBottom>
            Invoice {invoice.invoiceNumber}
          </Typography>
          <Typography variant="body1" color="text.secondary">
            Issued {invoice.issueDate.toLocaleDateString()} · Due {invoice.dueDate.toLocaleDateString()}
          </Typography>
        </Box>
        <Button variant="contained" startIcon={<Download />} onClick={handleDownload} disabled={downloadInvoiceMutation.isPending}>
          Download PDF
        </Button>
      </Box>

      <Grid container spacing={3}>
        <Grid size={{ xs: 12, md: 8 }}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Invoice Items
              </Typography>
              <TableContainer>
                <Table>
                  <TableHead>
                    <TableRow>
                      <TableCell>Description</TableCell>
                      <TableCell align="right">Quantity</TableCell>
                      <TableCell align="right">Unit Price</TableCell>
                      <TableCell align="right">Line Total</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {invoice.items.map((item) => (
                      <TableRow key={item.id}>
                        <TableCell>{item.description}</TableCell>
                        <TableCell align="right">{item.quantity}</TableCell>
                        <TableCell align="right"><MoneyText value={item.unitPrice} /></TableCell>
                        <TableCell align="right"><MoneyText value={item.lineTotal} /></TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            </CardContent>
          </Card>

          <Card sx={{ mt: 3 }}>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Payment History
              </Typography>
              {invoice.payments?.length ? (
                <TableContainer>
                  <Table size="small">
                    <TableHead>
                      <TableRow>
                        <TableCell>Date</TableCell>
                        <TableCell>Method</TableCell>
                        <TableCell>Reference</TableCell>
                        <TableCell align="right">Amount</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {invoice.payments.filter((payment) => !payment.isVoided).map((payment) => (
                        <TableRow key={payment.id}>
                          <TableCell>{payment.paymentDate.toLocaleDateString()}</TableCell>
                          <TableCell>{payment.paymentMethod}</TableCell>
                          <TableCell>{payment.reference || '—'}</TableCell>
                          <TableCell align="right"><MoneyText value={payment.amount} /></TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </TableContainer>
              ) : (
                <Typography color="text.secondary">No payments recorded yet.</Typography>
              )}
            </CardContent>
          </Card>
        </Grid>

        <Grid size={{ xs: 12, md: 4 }}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Summary
              </Typography>
              <Chip
                label={formatInvoiceStatus(invoice.status)}
                sx={{ bgcolor: getInvoiceStatusColor(invoice.status), color: 'white', mb: 2 }}
              />
              <Divider sx={{ my: 2 }} />
              <SummaryRow label="Subtotal" value={invoice.subtotal} />
              <SummaryRow label="GST" value={invoice.gstAmount} />
              <SummaryRow label="Total" value={invoice.totalAmount} strong />
              <SummaryRow label="Paid" value={invoice.paidAmount} />
              <SummaryRow label="Amount Due" value={invoice.outstandingAmount} strong />
              <Box sx={{ mt: 3, p: 2, bgcolor: invoice.outstandingAmount > 0 ? 'warning.light' : 'success.light', borderRadius: 1 }}>
                <Typography variant="body2">
                  {invoice.outstandingAmount > 0
                    ? `Please use your account number${invoice.customer?.accountNumber ? ` (${invoice.customer.accountNumber})` : ''} as your payment reference.`
                    : 'This invoice has no outstanding balance.'}
                </Typography>
              </Box>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
}

function SummaryRow({ label, value, strong = false }: { label: string; value: number; strong?: boolean }) {
  return (
    <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
      <Typography variant="body2" sx={{ fontWeight: strong ? 700 : 400 }}>{label}</Typography>
      <Typography variant="body2" sx={{ fontWeight: strong ? 700 : 400 }}>
        <MoneyText value={value} />
      </Typography>
    </Box>
  );
}
