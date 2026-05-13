import { useNavigate } from 'react-router';
import { Box, Typography, Card, CardContent, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Button, Chip, IconButton, Alert, CircularProgress } from '@mui/material';
import { Add, Visibility, Send, Download } from '@mui/icons-material';
import { toast } from 'sonner';
import { useAdminStatementsQuery } from '@/entities/statement/api/statementQueries';
import { downloadAdminStatementPdf } from '@/features/statementActions/api/statementActionsApi';
import { useGenerateWeeklyStatementsMutation, useSendStatementEmailMutation } from '@/features/statementActions/model/statementActionsMutations';

export default function Statements() {
  const navigate = useNavigate();
  const { data: statements = [], isLoading, error } = useAdminStatementsQuery();
  const generateWeeklyMutation = useGenerateWeeklyStatementsMutation();
  const sendEmailMutation = useSendStatementEmailMutation();

  const handleGenerateWeekly = () => {
    generateWeeklyMutation.mutate();
  };

  const handleSendEmail = (statementId: string, event: React.MouseEvent) => {
    event.stopPropagation();
    sendEmailMutation.mutate(statementId);
  };

  const handleDownload = async (statementId: string, event: React.MouseEvent) => {
    event.stopPropagation();
    try {
      await downloadAdminStatementPdf(statementId);
      toast.success('Downloading statement PDF');
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Unable to download statement');
    }
  };

  const handleViewDetails = (statementId: string) => {
    navigate(`/admin/statements/${statementId}`);
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
            Statements
          </Typography>
          <Typography variant="body1" color="text.secondary">
            Manage customer account statements and send reminders
          </Typography>
        </div>
        <Button
          variant="contained"
          startIcon={<Add />}
          onClick={handleGenerateWeekly}
          disabled={generateWeeklyMutation.isPending}
        >
          Generate Weekly Statements
        </Button>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {error instanceof Error ? error.message : 'Unable to load statements'}
        </Alert>
      )}

      <Card>
        <CardContent>
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Statement Number</TableCell>
                  <TableCell>Customer</TableCell>
                  <TableCell>Statement Date</TableCell>
                  <TableCell>Period</TableCell>
                  <TableCell align="right">Total Amount Due</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Email Status</TableCell>
                  <TableCell align="center">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {statements.map((statement) => (
                  <TableRow
                    key={statement.id}
                    hover
                    sx={{ cursor: 'pointer' }}
                    onClick={() => handleViewDetails(statement.id)}
                  >
                    <TableCell>{statement.statementNumber}</TableCell>
                    <TableCell>{statement.customer?.businessName}</TableCell>
                    <TableCell>{statement.statementDate.toLocaleDateString()}</TableCell>
                    <TableCell>
                      {statement.periodStart && statement.periodEnd
                        ? `${statement.periodStart.toLocaleDateString()} - ${statement.periodEnd.toLocaleDateString()}`
                        : 'N/A'}
                    </TableCell>
                    <TableCell align="right">${statement.totalOutstanding.toFixed(2)}</TableCell>
                    <TableCell>
                      <Chip label={statement.status} size="small" color={statement.status === 'Sent' ? 'success' : 'default'} />
                    </TableCell>
                    <TableCell>
                      <Chip label={statement.emailStatus} size="small" color={statement.emailStatus === 'Sent' ? 'success' : 'default'} />
                    </TableCell>
                    <TableCell align="center">
                      <IconButton
                        size="small"
                        onClick={(e) => {
                          e.stopPropagation();
                          handleViewDetails(statement.id);
                        }}
                        title="View Details"
                      >
                        <Visibility />
                      </IconButton>
                      <IconButton
                        size="small"
                        onClick={(e) => handleSendEmail(statement.id, e)}
                        disabled={statement.emailStatus === 'Sent' || sendEmailMutation.isPending}
                        title="Send Email"
                      >
                        <Send />
                      </IconButton>
                      <IconButton size="small" onClick={(e) => handleDownload(statement.id, e)} title="Download PDF">
                        <Download />
                      </IconButton>
                    </TableCell>
                  </TableRow>
                ))}
                {statements.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={8} align="center">No statements found</TableCell>
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
