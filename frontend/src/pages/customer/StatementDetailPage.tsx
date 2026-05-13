import { Box, Button, Card, CardContent, Chip, Divider, Grid, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Typography } from '@mui/material';
import { ArrowBack, Download } from '@mui/icons-material';
import { Link, useNavigate, useParams } from 'react-router';
import { toast } from 'sonner';
import { useCustomerStatementQuery } from '@/entities/statement/api/statementQueries';
import { downloadCustomerStatementPdf } from '@/features/statementActions/api/statementActionsApi';
import { formatInvoiceStatus, getInvoiceStatusColor } from '@/shared/status/statusFormat';
import { LoadingState } from '@/shared/ui/LoadingState';
import { ErrorState } from '@/shared/ui/ErrorState';
import { MoneyText } from '@/shared/ui/MoneyText';

export default function CustomerStatementDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: statement, isLoading, error } = useCustomerStatementQuery(id);

  const handleDownload = async () => {
    if (!statement) {
      return;
    }

    try {
      await downloadCustomerStatementPdf(statement.id);
      toast.success(`Downloading statement ${statement.statementNumber}`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Unable to download statement');
    }
  };

  if (!id) {
    return <ErrorState message="Statement id is required" />;
  }

  if (isLoading) {
    return <LoadingState />;
  }

  if (error || !statement) {
    return <ErrorState message={error instanceof Error ? error.message : 'Statement not found'} />;
  }

  return (
    <Box>
      <Button component={Link} to="/customer/statements" startIcon={<ArrowBack />} sx={{ mb: 2 }}>
        Back to Statements
      </Button>

      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 3 }}>
        <Box>
          <Typography variant="h4" gutterBottom>
            Statement {statement.statementNumber}
          </Typography>
          <Typography variant="body1" color="text.secondary">
            Statement date {statement.statementDate.toLocaleDateString()}
          </Typography>
          {statement.periodStart && statement.periodEnd ? (
            <Typography variant="body2" color="text.secondary">
              Period {statement.periodStart.toLocaleDateString()} – {statement.periodEnd.toLocaleDateString()}
            </Typography>
          ) : null}
        </Box>
        <Button variant="contained" startIcon={<Download />} onClick={handleDownload}>
          Download PDF
        </Button>
      </Box>

      <Grid container spacing={3}>
        <Grid size={{ xs: 12, md: 8 }}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Statement Invoices
              </Typography>
              <TableContainer>
                <Table>
                  <TableHead>
                    <TableRow>
                      <TableCell>Invoice #</TableCell>
                      <TableCell>Issue Date</TableCell>
                      <TableCell>Due Date</TableCell>
                      <TableCell>Status</TableCell>
                      <TableCell align="right">Amount Due</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {statement.invoices.map((invoice) => (
                      <TableRow
                        key={invoice.id}
                        hover
                        onClick={() => navigate(`/customer/invoices/${invoice.id}`)}
                        sx={{ cursor: 'pointer' }}
                      >
                        <TableCell>{invoice.invoiceNumber}</TableCell>
                        <TableCell>{invoice.issueDate.toLocaleDateString()}</TableCell>
                        <TableCell>{invoice.dueDate.toLocaleDateString()}</TableCell>
                        <TableCell>
                          <Chip
                            label={formatInvoiceStatus(invoice.status)}
                            size="small"
                            sx={{ bgcolor: getInvoiceStatusColor(invoice.status), color: 'white' }}
                          />
                        </TableCell>
                        <TableCell align="right"><MoneyText value={invoice.outstandingAmount} /></TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            </CardContent>
          </Card>
        </Grid>

        <Grid size={{ xs: 12, md: 4 }}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Summary
              </Typography>
              <Chip label={statement.status} sx={{ mb: 2 }} />
              <Divider sx={{ my: 2 }} />
              <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                <Typography variant="body2">Invoices</Typography>
                <Typography variant="body2">{statement.invoices.length}</Typography>
              </Box>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                <Typography variant="body2" sx={{ fontWeight: 700 }}>Total Outstanding</Typography>
                <Typography variant="body2" sx={{ fontWeight: 700 }}>
                  <MoneyText value={statement.totalOutstanding} />
                </Typography>
              </Box>
              <Box sx={{ mt: 3, p: 2, bgcolor: 'warning.light', borderRadius: 1 }}>
                <Typography variant="body2">
                  This statement summarizes unpaid invoice balances for your account.
                </Typography>
              </Box>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
}
