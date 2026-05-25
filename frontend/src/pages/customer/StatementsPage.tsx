import { Box, Typography, Card, CardContent, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Button, Divider } from '@mui/material';
import { Download, Visibility } from '@mui/icons-material';
import { useCustomerStatementsQuery } from '@/entities/statement/api/statementQueries';
import { useDownloadStatementPdfMutation } from '@/features/statementActions/model/statementActionsMutations';
import { Link } from 'react-router';
import { LoadingState } from '@/shared/ui/LoadingState';
import { ErrorState } from '@/shared/ui/ErrorState';
import { EmptyState } from '@/shared/ui/EmptyState';
import { formatInvoiceStatus, getInvoiceStatusColor } from '@/shared/status/statusFormat';
import { StatusChip } from '@/shared/ui/StatusChip';

export default function CustomerStatements() {
  const { data: statements = [], isLoading, error } = useCustomerStatementsQuery();
  const downloadStatementMutation = useDownloadStatementPdfMutation('customer');

  if (isLoading) {
    return <LoadingState />;
  }

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Statements
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
        View your account statements
      </Typography>

      {error && <ErrorState message={error instanceof Error ? error.message : 'Unable to load statements'} />}

      {statements.map((statement) => (
        <Card key={statement.id} sx={{ mb: 3 }}>
          <CardContent>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
              <Box>
                <Typography variant="h6">
                  {statement.statementNumber}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Statement Date: {statement.statementDate.toLocaleDateString()}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  {statement.customer?.businessName}
                </Typography>
              </Box>
              <Box>
                <Button
                  component={Link}
                  to={`/customer/statements/${statement.id}`}
                  variant="contained"
                  startIcon={<Visibility />}
                  sx={{ mr: 1 }}
                >
                  View Details
                </Button>
                <Button
                  variant="outlined"
                  startIcon={<Download />}
                  onClick={() => downloadStatementMutation.mutate({
                    statementId: statement.id,
                    statementNumber: statement.statementNumber,
                  })}
                  disabled={downloadStatementMutation.isPending && downloadStatementMutation.variables?.statementId === statement.id}
                >
                  Download PDF
                </Button>
              </Box>
            </Box>

            <Divider sx={{ my: 2 }} />

            <TableContainer>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>Invoice #</TableCell>
                    <TableCell>Issue Date</TableCell>
                    <TableCell>Due Date</TableCell>
                    <TableCell align="right">Amount</TableCell>
                    <TableCell>Status</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {statement.invoices.map((invoice) => (
                    <TableRow key={invoice.id}>
                      <TableCell>{invoice.invoiceNumber}</TableCell>
                      <TableCell>{invoice.issueDate.toLocaleDateString()}</TableCell>
                      <TableCell>{invoice.dueDate.toLocaleDateString()}</TableCell>
                      <TableCell align="right">${invoice.outstandingAmount.toFixed(2)}</TableCell>
                      <TableCell>
                        <StatusChip label={formatInvoiceStatus(invoice.status)} color={getInvoiceStatusColor(invoice.status)} />
                      </TableCell>
                    </TableRow>
                  ))}
                  <TableRow>
                    <TableCell colSpan={3} align="right">
                      <strong>Total Amount Due</strong>
                    </TableCell>
                    <TableCell align="right">
                      <strong>${statement.totalOutstanding.toFixed(2)}</strong>
                    </TableCell>
                    <TableCell />
                  </TableRow>
                </TableBody>
              </Table>
            </TableContainer>

            <Box sx={{ mt: 3, p: 2, bgcolor: 'warning.light', borderRadius: 1 }}>
              <Typography variant="body2">
                Please arrange payment at your earliest convenience. Payment details can be found on your invoices.
              </Typography>
            </Box>
          </CardContent>
        </Card>
      ))}

      {statements.length === 0 && !error && (
        <Card>
          <CardContent>
            <EmptyState title="No statements found" description="Statements will appear here once they are generated." />
          </CardContent>
        </Card>
      )}
    </Box>
  );
}
