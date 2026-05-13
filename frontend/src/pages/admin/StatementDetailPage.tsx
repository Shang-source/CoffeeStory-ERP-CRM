import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router';
import { Box, Typography, Card, CardContent, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Button, Divider, Chip, CircularProgress, Alert } from '@mui/material';
import { Send, Download, ArrowBack } from '@mui/icons-material';
import { formatInvoiceStatus, getInvoiceStatusColor } from '@/shared/status/statusFormat';
import { toast } from 'sonner';
import { Statement } from '@/entities/types';
import { getAdminStatement } from '@/entities/statement/api/statementApi';
import { downloadAdminStatementPdf, sendStatementEmail } from '@/features/statementActions/api/statementActionsApi';

export default function StatementDetail() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [statement, setStatement] = useState<Statement | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    const loadStatement = async () => {
      if (!id) {
        setError('Statement id is missing');
        setIsLoading(false);
        return;
      }

      try {
        setError('');
        setStatement(await getAdminStatement(id));
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Unable to load statement');
      } finally {
        setIsLoading(false);
      }
    };

    void loadStatement();
  }, [id]);

  const handleSendStatement = async () => {
    if (!statement) {
      return;
    }

    try {
      const updated = await sendStatementEmail(statement.id);
      setStatement(updated);
      toast.success('Statement sent to customer');
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Unable to send statement');
    }
  };

  const handleDownload = async () => {
    if (!statement) {
      return;
    }

    try {
      await downloadAdminStatementPdf(statement.id);
      toast.success('Downloading statement PDF');
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

  if (error || !statement) {
    return (
      <Box>
        <Alert severity="error" sx={{ mb: 3 }}>{error || 'Statement not found'}</Alert>
        <Button onClick={() => navigate('/admin/statements')}>Back to Statements</Button>
      </Box>
    );
  }

  return (
    <Box>
      <Button startIcon={<ArrowBack />} onClick={() => navigate('/admin/statements')} sx={{ mb: 3 }}>
        Back to Statements
      </Button>

      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 3 }}>
            <Box>
              <Typography variant="h4" gutterBottom>
                {statement.statementNumber}
              </Typography>
              <Typography variant="h6" color="text.secondary" gutterBottom>
                {statement.customer?.businessName}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Statement Date: {statement.statementDate.toLocaleDateString()}
              </Typography>
              {statement.periodStart && statement.periodEnd && (
                <Typography variant="body2" color="text.secondary">
                  Period: {statement.periodStart.toLocaleDateString()} - {statement.periodEnd.toLocaleDateString()}
                </Typography>
              )}
            </Box>
            <Box>
              <Chip
                label={statement.status}
                size="medium"
                color={statement.status === 'Sent' ? 'success' : 'default'}
                sx={{ mr: 1, mb: 1 }}
              />
              <Chip
                label={`Email: ${statement.emailStatus}`}
                size="medium"
                color={statement.emailStatus === 'Sent' ? 'success' : 'default'}
                sx={{ mb: 1 }}
              />
            </Box>
          </Box>

          <Box sx={{ display: 'flex', gap: 1, mb: 3 }}>
            <Button
              variant="contained"
              startIcon={<Send />}
              onClick={handleSendStatement}
              disabled={statement.emailStatus === 'Sent'}
            >
              Send Email
            </Button>
            <Button variant="outlined" startIcon={<Download />} onClick={handleDownload}>
              Download PDF
            </Button>
          </Box>

          <Divider sx={{ my: 3 }} />

          <Typography variant="h6" gutterBottom>
            Included Invoices
          </Typography>

          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Invoice #</TableCell>
                  <TableCell>Issue Date</TableCell>
                  <TableCell>Due Date</TableCell>
                  <TableCell align="right">Total</TableCell>
                  <TableCell align="right">Amount Due</TableCell>
                  <TableCell>Payment Status</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {statement.invoices.map((invoice) => (
                  <TableRow key={invoice.id}>
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
                  </TableRow>
                ))}
                <TableRow>
                  <TableCell colSpan={4} align="right">
                    <Typography variant="h6">Total Amount Due</Typography>
                  </TableCell>
                  <TableCell align="right">
                    <Typography variant="h6">${statement.totalOutstanding.toFixed(2)}</Typography>
                  </TableCell>
                  <TableCell />
                </TableRow>
              </TableBody>
            </Table>
          </TableContainer>

          <Box sx={{ mt: 3, p: 2, bgcolor: 'info.lighter', borderRadius: 1, border: '1px solid', borderColor: 'info.light' }}>
            <Typography variant="body2">
              This statement includes invoice snapshots as of {statement.statementDate.toLocaleDateString()}.
            </Typography>
          </Box>
        </CardContent>
      </Card>
    </Box>
  );
}
