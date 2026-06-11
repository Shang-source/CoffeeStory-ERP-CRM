import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router';
import { Alert, Box, Button, Card, CardContent, Grid, IconButton, Stack, Tab, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, TableSortLabel, Tabs, TextField, Typography, CircularProgress } from '@mui/material';
import { Add, Visibility, Send, Download } from '@mui/icons-material';
import { useAdminStatementsQuery } from '@/entities/statement/api/statementQueries';
import { useDownloadStatementPdfMutation, useGenerateWeeklyStatementsMutation, useSendStatementEmailMutation } from '@/features/statementActions/model/statementActionsMutations';
import { formatEmailStatus, formatStatementStatus, getEmailStatusColor, getStatementStatusColor } from '@/shared/status/statusFormat';
import { StatusChip } from '@/shared/ui/StatusChip';
import { Statement } from '@/entities/types';

type StatementTab = 'readyToSend' | 'sent' | 'failed' | 'all';
type StatementSortField = 'statementNumber' | 'customer' | 'statementDate' | 'period' | 'totalOutstanding';
type SortDirection = 'asc' | 'desc';

const statementTabs: Array<{ value: StatementTab; label: string; predicate: (statement: Statement) => boolean }> = [
  { value: 'readyToSend', label: 'Ready to Send', predicate: (statement) => ['Draft', 'ReadyToSend'].includes(statement.status) },
  { value: 'sent', label: 'Sent', predicate: (statement) => statement.status === 'Sent' || statement.emailStatus === 'Sent' },
  { value: 'failed', label: 'Failed', predicate: (statement) => ['Failed', 'Bounced'].includes(statement.emailStatus) },
  { value: 'all', label: 'All', predicate: () => true },
];

function money(value: number) {
  return `$${value.toFixed(2)}`;
}

function formatDate(value: Date) {
  return new Intl.DateTimeFormat('en-NZ', { day: '2-digit', month: '2-digit', year: 'numeric' }).format(value);
}

function statementPeriod(statement: Statement) {
  return statement.periodStart && statement.periodEnd
    ? `${formatDate(statement.periodStart)} - ${formatDate(statement.periodEnd)}`
    : 'N/A';
}

function statementMatchesSearch(statement: Statement, query: string) {
  const normalizedQuery = query.trim().toLowerCase();
  if (!normalizedQuery) {
    return true;
  }

  const searchableText = [
    statement.statementNumber,
    statement.customer?.accountNumber,
    statement.customer?.businessName,
    statement.customer?.contactPerson,
    statement.customer?.email,
    statementPeriod(statement),
    statement.totalOutstanding.toFixed(2),
    statement.status,
    statement.emailStatus,
  ].filter(Boolean).join(' ').toLowerCase();

  return searchableText.includes(normalizedQuery);
}

function SummaryCard({ label, value, helper }: { label: string; value: string; helper: string }) {
  return (
    <Card variant="outlined">
      <CardContent>
        <Typography variant="body2" color="text.secondary">{label}</Typography>
        <Typography variant="h5" sx={{ my: 0.5 }}>{value}</Typography>
        <Typography variant="caption" color="text.secondary">{helper}</Typography>
      </CardContent>
    </Card>
  );
}

export default function Statements() {
  const navigate = useNavigate();
  const [tab, setTab] = useState<StatementTab>('readyToSend');
  const [search, setSearch] = useState('');
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [sortField, setSortField] = useState<StatementSortField>('statementDate');
  const [sortDirection, setSortDirection] = useState<SortDirection>('desc');
  const { data: statements = [], isLoading, error } = useAdminStatementsQuery();
  const generateWeeklyMutation = useGenerateWeeklyStatementsMutation();
  const sendEmailMutation = useSendStatementEmailMutation();
  const downloadStatementMutation = useDownloadStatementPdfMutation('admin');
  const searchedStatements = useMemo(() => statements
    .filter((statement) => statementMatchesSearch(statement, search))
    .filter((statement) => isWithinDateRange(statement.statementDate, fromDate, toDate)), [statements, search, fromDate, toDate]);
  const currentTab = statementTabs.find((item) => item.value === tab) ?? statementTabs[0];
  const visibleStatements = useMemo(() => searchedStatements
    .filter(currentTab.predicate)
    .sort((left, right) => compareStatements(left, right, sortField, sortDirection)), [searchedStatements, currentTab, sortField, sortDirection]);
  const tabCounts = useMemo(() => Object.fromEntries(statementTabs.map((item) => [item.value, searchedStatements.filter(item.predicate).length])), [searchedStatements]);
  const readyToSendCount = statements.filter(statementTabs[0].predicate).length;
  const failedCount = statements.filter(statementTabs[2].predicate).length;
  const totalOutstanding = statements.reduce((sum, statement) => sum + statement.totalOutstanding, 0);

  const handleGenerateWeekly = () => {
    generateWeeklyMutation.mutate();
  };

  const handleSendEmail = (statementId: string, event: React.MouseEvent) => {
    event.stopPropagation();
    sendEmailMutation.mutate(statementId);
  };

  const handleViewDetails = (statementId: string) => {
    navigate(`/admin/statements/${statementId}`);
  };

  const handleSort = (field: StatementSortField) => {
    if (sortField === field) {
      setSortDirection(sortDirection === 'asc' ? 'desc' : 'asc');
      return;
    }

    setSortField(field);
    setSortDirection(['statementDate', 'period', 'totalOutstanding'].includes(field) ? 'desc' : 'asc');
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

      <Grid container spacing={2} sx={{ mb: 3 }}>
        <Grid size={{ xs: 12, md: 4 }}>
          <SummaryCard label="Ready to send" value={`${readyToSendCount}`} helper="Draft or ready statement emails" />
        </Grid>
        <Grid size={{ xs: 12, md: 4 }}>
          <SummaryCard label="Outstanding in statements" value={money(totalOutstanding)} helper="Total across current statements" />
        </Grid>
        <Grid size={{ xs: 12, md: 4 }}>
          <SummaryCard label="Failed emails" value={`${failedCount}`} helper="Failed or bounced statement emails" />
        </Grid>
      </Grid>

      <Card>
        <Tabs value={tab} onChange={(_, value) => setTab(value)} variant="scrollable" scrollButtons="auto" sx={{ borderBottom: 1, borderColor: 'divider' }}>
          {statementTabs.map((item) => (
            <Tab key={item.value} value={item.value} label={`${item.label} (${tabCounts[item.value] ?? 0})`} />
          ))}
        </Tabs>
        <CardContent>
          <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} sx={{ mb: 2 }}>
            <TextField
              label="Search statements, customers, account numbers, periods, amounts"
              size="small"
              fullWidth
              value={search}
              onChange={(event) => setSearch(event.target.value)}
            />
            <TextField label="From" type="date" size="small" value={fromDate} onChange={(event) => setFromDate(event.target.value)} InputLabelProps={{ shrink: true }} />
            <TextField label="To" type="date" size="small" value={toDate} onChange={(event) => setToDate(event.target.value)} InputLabelProps={{ shrink: true }} />
          </Stack>
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell><TableSortLabel active={sortField === 'statementNumber'} direction={sortDirection} onClick={() => handleSort('statementNumber')}>Statement Number</TableSortLabel></TableCell>
                  <TableCell><TableSortLabel active={sortField === 'customer'} direction={sortDirection} onClick={() => handleSort('customer')}>Customer</TableSortLabel></TableCell>
                  <TableCell><TableSortLabel active={sortField === 'statementDate'} direction={sortDirection} onClick={() => handleSort('statementDate')}>Statement Date</TableSortLabel></TableCell>
                  <TableCell><TableSortLabel active={sortField === 'period'} direction={sortDirection} onClick={() => handleSort('period')}>Period</TableSortLabel></TableCell>
                  <TableCell align="right"><TableSortLabel active={sortField === 'totalOutstanding'} direction={sortDirection} onClick={() => handleSort('totalOutstanding')}>Total Amount Due</TableSortLabel></TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Email Status</TableCell>
                  <TableCell align="center">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {visibleStatements.map((statement) => (
                  <TableRow
                    key={statement.id}
                    hover
                    sx={{ cursor: 'pointer' }}
                    onClick={() => handleViewDetails(statement.id)}
                  >
                    <TableCell>{statement.statementNumber}</TableCell>
                    <TableCell>{statement.customer?.businessName}</TableCell>
                    <TableCell>{formatDate(statement.statementDate)}</TableCell>
                    <TableCell>{statementPeriod(statement)}</TableCell>
                    <TableCell align="right">${statement.totalOutstanding.toFixed(2)}</TableCell>
                    <TableCell>
                      <StatusChip label={formatStatementStatus(statement.status)} color={getStatementStatusColor(statement.status)} />
                    </TableCell>
                    <TableCell>
                      <StatusChip label={formatEmailStatus(statement.emailStatus)} color={getEmailStatusColor(statement.emailStatus)} />
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
                      <IconButton
                        size="small"
                        onClick={(e) => {
                          e.stopPropagation();
                          downloadStatementMutation.mutate({
                            statementId: statement.id,
                            statementNumber: statement.statementNumber,
                          });
                        }}
                        disabled={downloadStatementMutation.isPending && downloadStatementMutation.variables?.statementId === statement.id}
                        title="Download PDF"
                      >
                        <Download />
                      </IconButton>
                    </TableCell>
                  </TableRow>
                ))}
                {visibleStatements.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={8} align="center">No statements match this queue.</TableCell>
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

function isWithinDateRange(date: Date, fromDate: string, toDate: string) {
  const time = date.getTime();
  if (fromDate && time < new Date(fromDate).getTime()) {
    return false;
  }

  if (toDate) {
    const end = new Date(toDate);
    end.setHours(23, 59, 59, 999);
    if (time > end.getTime()) {
      return false;
    }
  }

  return true;
}

function compareStatements(left: Statement, right: Statement, field: StatementSortField, direction: SortDirection) {
  const multiplier = direction === 'asc' ? 1 : -1;
  const leftValue = statementSortValue(left, field);
  const rightValue = statementSortValue(right, field);
  if (leftValue < rightValue) return -1 * multiplier;
  if (leftValue > rightValue) return 1 * multiplier;
  return 0;
}

function statementSortValue(statement: Statement, field: StatementSortField): string | number {
  switch (field) {
    case 'customer':
      return statement.customer?.businessName.toLowerCase() ?? '';
    case 'statementDate':
      return statement.statementDate.getTime();
    case 'period':
      return statement.periodStart?.getTime() ?? statement.statementDate.getTime();
    case 'totalOutstanding':
      return statement.totalOutstanding;
    case 'statementNumber':
    default:
      return statement.statementNumber.toLowerCase();
  }
}
