import { useEffect, useState } from 'react';
import { Box, Typography, Card, CardContent, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Button, Divider, Alert, CircularProgress } from '@mui/material';
import { Download } from '@mui/icons-material';
import { toast } from 'sonner';
import { Statement } from '@/entities/types';
import { getCustomerStatements } from '@/entities/statement/api/statementApi';
import { downloadCustomerStatementPdf } from '@/features/statementActions/api/statementActionsApi';

export default function CustomerStatements() {
  const [statements, setStatements] = useState<Statement[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    const loadStatements = async () => {
      try {
        setError('');
        setStatements(await getCustomerStatements());
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Unable to load statements');
      } finally {
        setIsLoading(false);
      }
    };

    void loadStatements();
  }, []);

  const handleDownload = async (statementId: string) => {
    try {
      await downloadCustomerStatementPdf(statementId);
      toast.success('Downloading statement');
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Unable to download statement');
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
        Statements
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
        View your account statements
      </Typography>

      {error && <Alert severity="error" sx={{ mb: 3 }}>{error}</Alert>}

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
              <Button variant="outlined" startIcon={<Download />} onClick={() => handleDownload(statement.id)}>
                Download PDF
              </Button>
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
                      <TableCell>{invoice.status}</TableCell>
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
            <Typography align="center" color="text.secondary">No statements found</Typography>
          </CardContent>
        </Card>
      )}
    </Box>
  );
}
