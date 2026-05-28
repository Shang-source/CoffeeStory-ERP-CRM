import { useMemo, useState } from 'react';
import { Alert, Box, Card, CardContent, Chip, Collapse, Grid, IconButton, Stack, Tab, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Tabs, TextField, Typography, CircularProgress } from '@mui/material';
import { Send, Download, KeyboardArrowDown, KeyboardArrowUp } from '@mui/icons-material';
import { formatInvoiceStatus, getInvoiceStatusColor } from '@/shared/status/statusFormat';
import { Invoice } from '@/entities/types';
import { useAdminInvoicesQuery } from '@/entities/invoice/api/invoiceQueries';
import { useDownloadInvoicePdfMutation, useSendInvoiceEmailMutation } from '@/features/invoiceActions/model/invoiceActionsMutations';

type InvoiceTab = 'needToSend' | 'awaitingPayment' | 'overdue' | 'paid' | 'failed' | 'all';

const invoiceTabs: Array<{ value: InvoiceTab; label: string; predicate: (invoice: Invoice) => boolean }> = [
  { value: 'needToSend', label: 'Need to Send', predicate: (invoice) => ['Draft', 'Issued'].includes(invoice.status) && invoice.emailStatus !== 'Sent' },
  { value: 'awaitingPayment', label: 'Awaiting Payment', predicate: (invoice) => ['Unpaid', 'PartiallyPaid'].includes(invoice.status) },
  { value: 'overdue', label: 'Overdue', predicate: (invoice) => invoice.status === 'Overdue' },
  { value: 'paid', label: 'Paid', predicate: (invoice) => invoice.status === 'Paid' },
  { value: 'failed', label: 'Failed', predicate: (invoice) => ['Failed', 'Bounced'].includes(invoice.emailStatus ?? 'NotSent') },
  { value: 'all', label: 'All', predicate: () => true },
];

function money(value: number) {
  return `$${value.toFixed(2)}`;
}

function invoiceMatchesSearch(invoice: Invoice, query: string) {
  const normalizedQuery = query.trim().toLowerCase();
  if (!normalizedQuery) {
    return true;
  }

  const searchableText = [
    invoice.invoiceNumber,
    invoice.customer?.businessName,
    invoice.customer?.contactPerson,
    invoice.customer?.email,
    invoice.totalAmount.toFixed(2),
    invoice.outstandingAmount.toFixed(2),
    invoice.status,
    invoice.emailStatus,
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
  const [tab, setTab] = useState<InvoiceTab>('needToSend');
  const [search, setSearch] = useState('');
  const { data: invoices = [], isLoading, error } = useAdminInvoicesQuery();
  const sendEmailMutation = useSendInvoiceEmailMutation();
  const downloadInvoiceMutation = useDownloadInvoicePdfMutation('admin');
  const searchedInvoices = useMemo(() => invoices.filter((invoice) => invoiceMatchesSearch(invoice, search)), [invoices, search]);
  const currentTab = invoiceTabs.find((item) => item.value === tab) ?? invoiceTabs[0];
  const visibleInvoices = useMemo(() => searchedInvoices.filter(currentTab.predicate), [searchedInvoices, currentTab]);
  const tabCounts = useMemo(() => Object.fromEntries(invoiceTabs.map((item) => [item.value, searchedInvoices.filter(item.predicate).length])), [searchedInvoices]);
  const openAmount = invoices
    .filter((invoice) => ['Unpaid', 'PartiallyPaid', 'Overdue'].includes(invoice.status))
    .reduce((sum, invoice) => sum + invoice.outstandingAmount, 0);
  const overdueAmount = invoices
    .filter((invoice) => invoice.status === 'Overdue')
    .reduce((sum, invoice) => sum + invoice.outstandingAmount, 0);
  const failedEmailCount = invoices.filter((invoice) => ['Failed', 'Bounced'].includes(invoice.emailStatus ?? 'NotSent')).length;

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
        Find invoices by send status, payment status, customer, or amount
      </Typography>

      <Grid container spacing={2} sx={{ mb: 3 }}>
        <Grid size={{ xs: 12, md: 4 }}>
          <SummaryCard label="Open amount" value={money(openAmount)} helper="Unpaid, partial, and overdue invoices" />
        </Grid>
        <Grid size={{ xs: 12, md: 4 }}>
          <SummaryCard label="Overdue amount" value={money(overdueAmount)} helper="Invoices requiring follow-up" />
        </Grid>
        <Grid size={{ xs: 12, md: 4 }}>
          <SummaryCard label="Email failures" value={`${failedEmailCount}`} helper="Failed or bounced invoice emails" />
        </Grid>
      </Grid>

      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {error instanceof Error ? error.message : 'Unable to load invoices'}
        </Alert>
      )}

      <Card>
        <Tabs value={tab} onChange={(_, value) => setTab(value)} variant="scrollable" scrollButtons="auto" sx={{ borderBottom: 1, borderColor: 'divider' }}>
          {invoiceTabs.map((item) => (
            <Tab key={item.value} value={item.value} label={`${item.label} (${tabCounts[item.value] ?? 0})`} />
          ))}
        </Tabs>
        <CardContent>
          <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} sx={{ mb: 2 }}>
            <TextField
              label="Search invoices, customers, emails, amounts"
              size="small"
              fullWidth
              value={search}
              onChange={(event) => setSearch(event.target.value)}
            />
          </Stack>
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
                {visibleInvoices.map((invoice) => (
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
                {visibleInvoices.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={9} align="center">No invoices match this queue.</TableCell>
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
