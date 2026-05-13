import { useMemo, useState } from 'react';
import { Alert, Box, Button, Card, CardContent, Chip, CircularProgress, MenuItem, Stack, Tab, Table, TableBody, TableCell, TableContainer, TableHead, TablePagination, TableRow, Tabs, TextField, Typography } from '@mui/material';
import { Download } from '@mui/icons-material';
import { toast } from 'sonner';
import { AuditLog, EmailLog, EmailStatus } from '@/entities/types';
import { exportAuditLogs, type LogQueryParams } from '@/entities/auditLog/api/auditLogApi';
import { useAuditLogsQuery } from '@/entities/auditLog/api/auditLogQueries';
import { exportEmailLogs } from '@/entities/emailLog/api/emailLogApi';
import { useEmailLogsQuery } from '@/entities/emailLog/api/emailLogQueries';

const emailStatuses: EmailStatus[] = ['Pending', 'Sent', 'Failed', 'Bounced'];
type LogFilters = Pick<LogQueryParams, 'search' | 'entityType' | 'action' | 'status'> & {
  from: string;
  to: string;
};

const emptyFilters: LogFilters = {
  search: '',
  entityType: '',
  action: '',
  status: '',
  from: '',
  to: '',
};

export default function Logs() {
  const [tab, setTab] = useState<'audit' | 'email'>('audit');
  const [auditPage, setAuditPage] = useState(0);
  const [emailPage, setEmailPage] = useState(0);
  const [auditRowsPerPage, setAuditRowsPerPage] = useState(25);
  const [emailRowsPerPage, setEmailRowsPerPage] = useState(25);
  const [search, setSearch] = useState('');
  const [entityType, setEntityType] = useState('');
  const [action, setAction] = useState('');
  const [status, setStatus] = useState<EmailStatus | ''>('');
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [appliedFilters, setAppliedFilters] = useState<LogFilters>(emptyFilters);

  const commonParams = useMemo<LogQueryParams>(() => ({
    search: appliedFilters.search,
    entityType: appliedFilters.entityType,
    from: appliedFilters.from ? `${appliedFilters.from}T00:00:00.000Z` : undefined,
    to: appliedFilters.to ? `${appliedFilters.to}T23:59:59.999Z` : undefined,
  }), [appliedFilters]);

  const auditParams = useMemo<LogQueryParams>(() => ({
    ...commonParams,
    action: appliedFilters.action,
    page: auditPage + 1,
    pageSize: auditRowsPerPage,
  }), [appliedFilters.action, auditPage, auditRowsPerPage, commonParams]);

  const emailParams = useMemo<LogQueryParams>(() => ({
    ...commonParams,
    status: appliedFilters.status,
    page: emailPage + 1,
    pageSize: emailRowsPerPage,
  }), [appliedFilters.status, commonParams, emailPage, emailRowsPerPage]);

  const auditQuery = useAuditLogsQuery(auditParams, tab === 'audit');
  const emailQuery = useEmailLogsQuery(emailParams, tab === 'email');

  const auditLogs = auditQuery.data?.items ?? [];
  const emailLogs = emailQuery.data?.items ?? [];
  const auditTotal = auditQuery.data?.totalCount ?? 0;
  const emailTotal = emailQuery.data?.totalCount ?? 0;
  const isLoading = tab === 'audit' ? auditQuery.isLoading : emailQuery.isLoading;
  const error = tab === 'audit' ? auditQuery.error : emailQuery.error;

  const applyFilters = () => {
    setAuditPage(0);
    setEmailPage(0);
    setAppliedFilters({ search, entityType, action, status, from, to });
  };

  const clearFilters = () => {
    setSearch('');
    setEntityType('');
    setAction('');
    setStatus('');
    setFrom('');
    setTo('');
    setAuditPage(0);
    setEmailPage(0);
    setAppliedFilters(emptyFilters);
  };

  const handleExport = async () => {
    try {
      if (tab === 'audit') {
        await exportAuditLogs({ ...commonParams, action: appliedFilters.action });
      } else {
        await exportEmailLogs({ ...commonParams, status: appliedFilters.status });
      }
      toast.success('Export started');
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Unable to export logs');
    }
  };

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Logs
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
        Review audit trail and email delivery events
      </Typography>

      {error && <Alert severity="error" sx={{ mb: 3 }}>{error instanceof Error ? error.message : 'Unable to load logs'}</Alert>}

      <Card>
        <CardContent>
          <Tabs value={tab} onChange={(_, value) => setTab(value as 'audit' | 'email')} sx={{ mb: 2 }}>
            <Tab label="Audit Logs" value="audit" />
            <Tab label="Email Logs" value="email" />
          </Tabs>

          <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} sx={{ mb: 2 }}>
            <TextField label="Search" value={search} onChange={(event) => setSearch(event.target.value)} size="small" />
            <TextField label="Entity Type" value={entityType} onChange={(event) => setEntityType(event.target.value)} size="small" />
            {tab === 'audit' ? (
              <TextField label="Action" value={action} onChange={(event) => setAction(event.target.value)} size="small" />
            ) : (
              <TextField select label="Status" value={status} onChange={(event) => setStatus(event.target.value as EmailStatus | '')} size="small" sx={{ minWidth: 140 }}>
                <MenuItem value="">All</MenuItem>
                {emailStatuses.map((emailStatus) => (
                  <MenuItem key={emailStatus} value={emailStatus}>{emailStatus}</MenuItem>
                ))}
              </TextField>
            )}
            <TextField label="From" type="date" value={from} onChange={(event) => setFrom(event.target.value)} size="small" InputLabelProps={{ shrink: true }} />
            <TextField label="To" type="date" value={to} onChange={(event) => setTo(event.target.value)} size="small" InputLabelProps={{ shrink: true }} />
            <Button variant="contained" onClick={applyFilters}>Apply</Button>
            <Button variant="outlined" onClick={clearFilters}>Clear</Button>
            <Button variant="outlined" startIcon={<Download />} onClick={handleExport}>Export CSV</Button>
          </Stack>

          {isLoading ? (
            <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
              <CircularProgress />
            </Box>
          ) : tab === 'audit' ? (
            <>
              <TableContainer>
                <Table>
                  <TableHead>
                    <TableRow>
                      <TableCell>Time</TableCell>
                      <TableCell>Action</TableCell>
                      <TableCell>Entity</TableCell>
                      <TableCell>Actor</TableCell>
                      <TableCell>Message</TableCell>
                      <TableCell>Changes</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {auditLogs.map((log) => (
                      <TableRow key={log.id}>
                        <TableCell>{log.createdAt.toLocaleString()}</TableCell>
                        <TableCell><Chip label={log.action} size="small" /></TableCell>
                        <TableCell>{log.entityType}</TableCell>
                        <TableCell>{log.actorRole ?? 'System'}</TableCell>
                        <TableCell>{log.message}</TableCell>
                        <TableCell>
                          <Typography variant="caption" sx={{ whiteSpace: 'pre-wrap' }}>
                            {formatAuditChanges(log)}
                          </Typography>
                        </TableCell>
                      </TableRow>
                    ))}
                    {auditLogs.length === 0 && (
                      <TableRow>
                        <TableCell colSpan={6} align="center">No audit logs found</TableCell>
                      </TableRow>
                    )}
                  </TableBody>
                </Table>
              </TableContainer>
              <TablePagination
                component="div"
                count={auditTotal}
                page={auditPage}
                onPageChange={(_, page) => setAuditPage(page)}
                rowsPerPage={auditRowsPerPage}
                onRowsPerPageChange={(event) => {
                  setAuditRowsPerPage(Number(event.target.value));
                  setAuditPage(0);
                }}
                rowsPerPageOptions={[10, 25, 50, 100]}
              />
            </>
          ) : (
            <>
              <TableContainer>
                <Table>
                  <TableHead>
                    <TableRow>
                      <TableCell>Time</TableCell>
                      <TableCell>Related Entity</TableCell>
                      <TableCell>Recipient</TableCell>
                      <TableCell>Subject</TableCell>
                      <TableCell>Status</TableCell>
                      <TableCell>Provider</TableCell>
                      <TableCell>Last Event</TableCell>
                      <TableCell>Sent At</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {emailLogs.map((log) => (
                      <TableRow key={log.id}>
                        <TableCell>{log.createdAt.toLocaleString()}</TableCell>
                        <TableCell>{log.relatedEntityType}</TableCell>
                        <TableCell>{log.recipientEmail}</TableCell>
                        <TableCell>{log.subject}</TableCell>
                        <TableCell>
                          <Chip label={log.status} size="small" color={log.status === 'Sent' ? 'success' : log.status === 'Failed' ? 'error' : 'default'} />
                        </TableCell>
                        <TableCell>
                          <Typography variant="body2">{log.provider ?? 'N/A'}</Typography>
                          <Typography variant="caption" color="text.secondary">{log.providerMessageId ?? ''}</Typography>
                        </TableCell>
                        <TableCell>
                          <Typography variant="body2">{log.lastProviderEventType ?? 'N/A'}</Typography>
                          <Typography variant="caption" color="text.secondary">{log.lastProviderEventAt?.toLocaleString() ?? ''}</Typography>
                        </TableCell>
                        <TableCell>{log.sentAt?.toLocaleString() ?? 'N/A'}</TableCell>
                      </TableRow>
                    ))}
                    {emailLogs.length === 0 && (
                      <TableRow>
                        <TableCell colSpan={8} align="center">No email logs found</TableCell>
                      </TableRow>
                    )}
                  </TableBody>
                </Table>
              </TableContainer>
              <TablePagination
                component="div"
                count={emailTotal}
                page={emailPage}
                onPageChange={(_, page) => setEmailPage(page)}
                rowsPerPage={emailRowsPerPage}
                onRowsPerPageChange={(event) => {
                  setEmailRowsPerPage(Number(event.target.value));
                  setEmailPage(0);
                }}
                rowsPerPageOptions={[10, 25, 50, 100]}
              />
            </>
          )}
        </CardContent>
      </Card>
    </Box>
  );
}

function formatAuditChanges(log: AuditLog) {
  if (!log.oldValues && !log.newValues) {
    return 'N/A';
  }

  const oldValues = formatJson(log.oldValues);
  const newValues = formatJson(log.newValues);
  if (!oldValues) {
    return `New: ${newValues}`;
  }

  if (!newValues) {
    return `Old: ${oldValues}`;
  }

  return `Old: ${oldValues}\nNew: ${newValues}`;
}

function formatJson(value?: string) {
  if (!value) {
    return '';
  }

  try {
    return JSON.stringify(JSON.parse(value), null, 2);
  } catch {
    return value;
  }
}
