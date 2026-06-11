import { useMemo, useState } from 'react';
import { Alert, Box, Button, Card, CardContent, Checkbox, Chip, Dialog, DialogActions, DialogContent, DialogTitle, Grid, MenuItem, Stack, Tab, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, TableSortLabel, Tabs, TextField, Typography, CircularProgress } from '@mui/material';
import { CheckCircle, Undo, Warning } from '@mui/icons-material';
import { useSearchParams } from 'react-router';
import { formatInvoiceStatus, getInvoiceStatusColor } from '@/shared/status/statusFormat';
import { Invoice, PaymentRecord } from '@/entities/types';
import { useAdminInvoicesQuery } from '@/entities/invoice/api/invoiceQueries';
import { useMarkOverdueInvoicesMutation } from '@/features/invoiceActions/model/invoiceActionsMutations';
import { useBatchRecordInvoicePaymentsMutation, useRecordInvoicePaymentMutation, useVoidInvoicePaymentMutation } from '@/features/paymentRecord/model/paymentRecordMutations';

const payableInvoiceStatuses = new Set(['Unpaid', 'PartiallyPaid', 'Overdue']);
type PaymentTab = 'toCollect' | 'records' | 'voided' | 'all';
type PaymentEntry = { invoice: Invoice; payment: PaymentRecord };
type InvoiceSortField = 'invoiceNumber' | 'customer' | 'issueDate' | 'dueDate' | 'amountDue';
type SortDirection = 'asc' | 'desc';

const paymentTabs: Array<{ value: PaymentTab; label: string }> = [
  { value: 'toCollect', label: 'To Collect' },
  { value: 'records', label: 'Payment Records' },
  { value: 'voided', label: 'Voided' },
  { value: 'all', label: 'All' },
];

function money(value: number) {
  return `$${value.toFixed(2)}`;
}

function formatDate(value: Date) {
  return new Intl.DateTimeFormat('en-NZ', { day: '2-digit', month: '2-digit', year: 'numeric' }).format(value);
}

function searchableText(parts: Array<string | number | undefined>) {
  return parts.filter((part) => part !== undefined && part !== '').join(' ').toLowerCase();
}

function invoiceMatchesSearch(invoice: Invoice, query: string) {
  const normalizedQuery = query.trim().toLowerCase();
  if (!normalizedQuery) {
    return true;
  }

  return searchableText([
    invoice.invoiceNumber,
    invoice.customer?.accountNumber,
    invoice.customer?.businessName,
    invoice.customer?.contactPerson,
    invoice.customer?.email,
    invoice.outstandingAmount.toFixed(2),
    invoice.totalAmount.toFixed(2),
    invoice.status,
  ]).includes(normalizedQuery);
}

function paymentMatchesSearch({ invoice, payment }: PaymentEntry, query: string) {
  const normalizedQuery = query.trim().toLowerCase();
  if (!normalizedQuery) {
    return true;
  }

  return searchableText([
    invoice.invoiceNumber,
    invoice.customer?.businessName,
    invoice.customer?.contactPerson,
    invoice.customer?.email,
    payment.reference,
    payment.paymentMethod,
    payment.amount.toFixed(2),
    payment.isVoided ? 'Voided' : 'Active',
  ]).includes(normalizedQuery);
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

export default function Payments() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [tab, setTab] = useState<PaymentTab>(() => normalizePaymentTab(searchParams.get('tab')) ?? 'toCollect');
  const [search, setSearch] = useState('');
  const [openDialog, setOpenDialog] = useState(false);
  const [openBatchDialog, setOpenBatchDialog] = useState(false);
  const [selectedInvoice, setSelectedInvoice] = useState<Invoice | null>(null);
  const [selectedInvoiceIds, setSelectedInvoiceIds] = useState<string[]>([]);
  const [paymentAttempted, setPaymentAttempted] = useState(false);
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [sortField, setSortField] = useState<InvoiceSortField>('dueDate');
  const [sortDirection, setSortDirection] = useState<SortDirection>('asc');
  const [paymentData, setPaymentData] = useState({
    amount: '',
    paymentDate: new Date().toISOString().split('T')[0],
    paymentMethod: 'BankTransfer',
    reference: '',
    note: '',
  });
  const [batchPaymentData, setBatchPaymentData] = useState({
    paymentDate: new Date().toISOString().split('T')[0],
    paymentMethod: 'BankTransfer',
    reference: '',
    note: '',
  });

  const { data: invoices = [], isLoading, error } = useAdminInvoicesQuery();
  const recordPaymentMutation = useRecordInvoicePaymentMutation(() => {
    setOpenDialog(false);
    setSelectedInvoice(null);
    setPaymentAttempted(false);
  });
  const batchRecordPaymentMutation = useBatchRecordInvoicePaymentsMutation(() => {
    setOpenBatchDialog(false);
    setSelectedInvoiceIds([]);
  });
  const markOverdueMutation = useMarkOverdueInvoicesMutation();
  const voidPaymentMutation = useVoidInvoicePaymentMutation();

  const unpaidInvoices = invoices.filter(inv => payableInvoiceStatuses.has(inv.status) && inv.outstandingAmount > 0);
  const recordedPayments = invoices.flatMap((invoice) =>
    (invoice.payments ?? []).map((payment) => ({ invoice, payment }))
  ).sort((a, b) => b.payment.paymentDate.getTime() - a.payment.paymentDate.getTime());
  const visibleUnpaidInvoices = useMemo(() => unpaidInvoices
    .filter((invoice) => invoiceMatchesSearch(invoice, search))
    .filter((invoice) => isWithinDateRange(invoice, fromDate, toDate))
    .sort((left, right) => compareInvoices(left, right, sortField, sortDirection)), [unpaidInvoices, search, fromDate, toDate, sortField, sortDirection]);
  const visiblePaymentRecords = useMemo(() => recordedPayments.filter((entry) => paymentMatchesSearch(entry, search)), [recordedPayments, search]);
  const activePaymentRecords = visiblePaymentRecords.filter(({ payment }) => !payment.isVoided);
  const voidedPaymentRecords = visiblePaymentRecords.filter(({ payment }) => payment.isVoided);
  const paymentsForTab = tab === 'voided' ? voidedPaymentRecords : tab === 'records' ? activePaymentRecords : tab === 'all' ? visiblePaymentRecords : [];
  const shouldShowCollect = tab === 'toCollect' || tab === 'all';
  const shouldShowRecords = tab === 'records' || tab === 'voided' || tab === 'all';
  const tabCounts = {
    toCollect: visibleUnpaidInvoices.length,
    records: activePaymentRecords.length,
    voided: voidedPaymentRecords.length,
    all: visibleUnpaidInvoices.length + visiblePaymentRecords.length,
  };
  const openAmount = unpaidInvoices.reduce((sum, invoice) => sum + invoice.outstandingAmount, 0);
  const activePaymentTotal = recordedPayments.filter(({ payment }) => !payment.isVoided).reduce((sum, { payment }) => sum + payment.amount, 0);
  const amount = Number(paymentData.amount);
  const reference = paymentData.reference.trim();
  const isAmountValid = Boolean(selectedInvoice) && amount > 0 && amount <= selectedInvoice!.outstandingAmount;
  const isDateValid = Boolean(paymentData.paymentDate);
  const canRecordPayment = isAmountValid && isDateValid;
  const selectedInvoices = visibleUnpaidInvoices.filter((invoice) => selectedInvoiceIds.includes(invoice.id));
  const allVisibleSelected = visibleUnpaidInvoices.length > 0 && selectedInvoices.length === visibleUnpaidInvoices.length;
  const someVisibleSelected = selectedInvoices.length > 0 && !allVisibleSelected;

  const handleMarkPaid = (invoice: Invoice) => {
    setSelectedInvoice(invoice);
    setPaymentAttempted(false);
    setPaymentData({
      amount: invoice.outstandingAmount.toFixed(2),
      paymentDate: new Date().toISOString().split('T')[0],
      paymentMethod: 'BankTransfer',
      reference: '',
      note: '',
    });
    setOpenDialog(true);
  };

  const handleSort = (field: InvoiceSortField) => {
    if (sortField === field) {
      setSortDirection(sortDirection === 'asc' ? 'desc' : 'asc');
      return;
    }

    setSortField(field);
    setSortDirection(field === 'amountDue' ? 'desc' : 'asc');
  };

  const handleSelectInvoice = (invoiceId: string, checked: boolean) => {
    setSelectedInvoiceIds((current) => checked
      ? Array.from(new Set([...current, invoiceId]))
      : current.filter((id) => id !== invoiceId));
  };

  const handleSelectVisible = (checked: boolean) => {
    setSelectedInvoiceIds(checked ? visibleUnpaidInvoices.map((invoice) => invoice.id) : []);
  };

  const handleSavePayment = async () => {
    if (!selectedInvoice) {
      return;
    }

    setPaymentAttempted(true);
    if (!canRecordPayment) {
      return;
    }

    recordPaymentMutation.mutate({
      invoiceId: selectedInvoice.id,
      amountLabel: paymentData.amount,
      payload: {
        amount,
        paymentDate: new Date(paymentData.paymentDate).toISOString(),
        paymentMethod: paymentData.paymentMethod,
        reference: reference || undefined,
        note: paymentData.note.trim() || undefined,
      },
    });
  };

  const handleBatchSavePayment = () => {
    if (selectedInvoices.length === 0 || !batchPaymentData.paymentDate) {
      return;
    }

    batchRecordPaymentMutation.mutate({
      invoiceIds: selectedInvoices.map((invoice) => invoice.id),
      paymentDate: new Date(batchPaymentData.paymentDate).toISOString(),
      paymentMethod: batchPaymentData.paymentMethod,
      reference: batchPaymentData.reference.trim() || undefined,
      note: batchPaymentData.note.trim() || undefined,
    });
  };

  const handleMarkOverdue = () => {
    markOverdueMutation.mutate();
  };

  const handleVoidPayment = async (invoiceId: string, paymentId: string) => {
    const reason = window.prompt('Reason for voiding this payment');
    if (!reason?.trim()) {
      return;
    }

    voidPaymentMutation.mutate({ invoiceId, paymentId, reason });
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
        Payments
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
        Collect outstanding invoices and audit recorded payments
      </Typography>

      <Grid container spacing={2} sx={{ mb: 3 }}>
        <Grid size={{ xs: 12, md: 4 }}>
          <SummaryCard label="To collect" value={money(openAmount)} helper={`${unpaidInvoices.length} outstanding invoice(s)`} />
        </Grid>
        <Grid size={{ xs: 12, md: 4 }}>
          <SummaryCard label="Recorded payments" value={money(activePaymentTotal)} helper={`${recordedPayments.filter(({ payment }) => !payment.isVoided).length} active record(s)`} />
        </Grid>
        <Grid size={{ xs: 12, md: 4 }}>
          <SummaryCard label="Voided records" value={`${recordedPayments.filter(({ payment }) => payment.isVoided).length}`} helper="Kept for audit review" />
        </Grid>
      </Grid>

      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {error instanceof Error ? error.message : 'Unable to load invoices'}
        </Alert>
      )}

      <Card>
        <Tabs
          value={tab}
          onChange={(_, value: PaymentTab) => {
            setTab(value);
            setSearchParams(value === 'toCollect' ? {} : { tab: value });
          }}
          variant="scrollable"
          scrollButtons="auto"
          sx={{ borderBottom: 1, borderColor: 'divider' }}
        >
          {paymentTabs.map((item) => (
            <Tab key={item.value} value={item.value} label={`${item.label} (${tabCounts[item.value]})`} />
          ))}
        </Tabs>
        <CardContent>
          <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} alignItems={{ md: 'center' }} justifyContent="space-between" sx={{ mb: 2 }}>
            <TextField
              label="Search invoice, customer, account number, email, amount"
              size="small"
              fullWidth
              value={search}
              onChange={(event) => setSearch(event.target.value)}
            />
            <TextField
              label="From"
              type="date"
              size="small"
              value={fromDate}
              onChange={(event) => setFromDate(event.target.value)}
              InputLabelProps={{ shrink: true }}
            />
            <TextField
              label="To"
              type="date"
              size="small"
              value={toDate}
              onChange={(event) => setToDate(event.target.value)}
              InputLabelProps={{ shrink: true }}
            />
            {tab === 'toCollect' && (
              <>
                <Button
                  variant="contained"
                  startIcon={<CheckCircle />}
                  onClick={() => setOpenBatchDialog(true)}
                  disabled={selectedInvoices.length === 0 || batchRecordPaymentMutation.isPending}
                  sx={{ minWidth: 240 }}
                >
                  Record Selected ({selectedInvoices.length})
                </Button>
                <Button
                  variant="outlined"
                  startIcon={<Warning />}
                  onClick={handleMarkOverdue}
                  disabled={markOverdueMutation.isPending}
                  sx={{ minWidth: 220 }}
                >
                  Mark Overdue Invoices
                </Button>
              </>
            )}
          </Stack>

          {shouldShowCollect && (
            <>
              <Typography variant="h6" gutterBottom>
                To Collect
              </Typography>
              <TableContainer>
                <Table>
                  <TableHead>
                    <TableRow>
                      <TableCell padding="checkbox">
                        <Checkbox
                          checked={allVisibleSelected}
                          indeterminate={someVisibleSelected}
                          onChange={(event) => handleSelectVisible(event.target.checked)}
                        />
                      </TableCell>
                      <TableCell>
                        <TableSortLabel active={sortField === 'invoiceNumber'} direction={sortDirection} onClick={() => handleSort('invoiceNumber')}>Invoice #</TableSortLabel>
                      </TableCell>
                      <TableCell>
                        <TableSortLabel active={sortField === 'customer'} direction={sortDirection} onClick={() => handleSort('customer')}>Customer</TableSortLabel>
                      </TableCell>
                      <TableCell>
                        <TableSortLabel active={sortField === 'issueDate'} direction={sortDirection} onClick={() => handleSort('issueDate')}>Issue Date</TableSortLabel>
                      </TableCell>
                      <TableCell>
                        <TableSortLabel active={sortField === 'dueDate'} direction={sortDirection} onClick={() => handleSort('dueDate')}>Due Date</TableSortLabel>
                      </TableCell>
                      <TableCell align="right">
                        <TableSortLabel active={sortField === 'amountDue'} direction={sortDirection} onClick={() => handleSort('amountDue')}>Amount Due</TableSortLabel>
                      </TableCell>
                      <TableCell>Status</TableCell>
                      <TableCell align="center">Actions</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {visibleUnpaidInvoices.map((invoice) => (
                      <TableRow key={invoice.id}>
                        <TableCell padding="checkbox">
                          <Checkbox
                            checked={selectedInvoiceIds.includes(invoice.id)}
                            onChange={(event) => handleSelectInvoice(invoice.id, event.target.checked)}
                          />
                        </TableCell>
                        <TableCell>{invoice.invoiceNumber}</TableCell>
                        <TableCell>
                          <Typography variant="body2">{invoice.customer?.businessName}</Typography>
                          <Typography variant="caption" color="text.secondary">#{invoice.customer?.accountNumber}</Typography>
                        </TableCell>
                        <TableCell>{formatDate(invoice.issueDate)}</TableCell>
                        <TableCell>{formatDate(invoice.dueDate)}</TableCell>
                        <TableCell align="right">${invoice.outstandingAmount.toFixed(2)}</TableCell>
                        <TableCell>
                          <Chip
                            label={formatInvoiceStatus(invoice.status)}
                            size="small"
                            sx={{ bgcolor: getInvoiceStatusColor(invoice.status), color: 'white' }}
                          />
                        </TableCell>
                        <TableCell align="center">
                          <Button
                            variant="contained"
                            size="small"
                            startIcon={<CheckCircle />}
                            onClick={() => handleMarkPaid(invoice)}
                            disabled={recordPaymentMutation.isPending}
                          >
                            Record Payment
                          </Button>
                        </TableCell>
                      </TableRow>
                    ))}
                    {visibleUnpaidInvoices.length === 0 && (
                      <TableRow>
                        <TableCell colSpan={8} align="center">No invoices to collect.</TableCell>
                      </TableRow>
                    )}
                  </TableBody>
                </Table>
              </TableContainer>
            </>
          )}
        </CardContent>
      </Card>

      {shouldShowRecords && (
        <Card sx={{ mt: 3 }}>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              {tab === 'voided' ? 'Voided Payment Records' : 'Payment Records'}
            </Typography>
            <TableContainer>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>Invoice #</TableCell>
                    <TableCell>Customer</TableCell>
                    <TableCell>Date</TableCell>
                    <TableCell>Reference</TableCell>
                    <TableCell align="right">Amount</TableCell>
                    <TableCell>Status</TableCell>
                    <TableCell align="center">Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {paymentsForTab.map(({ invoice, payment }) => (
                    <TableRow key={payment.id}>
                      <TableCell>{invoice.invoiceNumber}</TableCell>
                      <TableCell>{invoice.customer?.businessName}</TableCell>
                      <TableCell>{formatDate(payment.paymentDate)}</TableCell>
                      <TableCell>{payment.reference}</TableCell>
                      <TableCell align="right">${payment.amount.toFixed(2)}</TableCell>
                      <TableCell>
                        <Chip label={payment.isVoided ? 'Voided' : 'Active'} color={payment.isVoided ? 'default' : 'success'} size="small" />
                      </TableCell>
                      <TableCell align="center">
                        <Button
                          size="small"
                          startIcon={<Undo />}
                          onClick={() => handleVoidPayment(invoice.id, payment.id)}
                          disabled={payment.isVoided || voidPaymentMutation.isPending}
                        >
                          Void
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                  {paymentsForTab.length === 0 && (
                    <TableRow>
                      <TableCell colSpan={7} align="center">No payment records match this view.</TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </TableContainer>
          </CardContent>
        </Card>
      )}

      <Dialog
        open={openDialog}
        onClose={() => {
          setOpenDialog(false);
          setPaymentAttempted(false);
        }}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Record Payment</DialogTitle>
        <DialogContent>
          {selectedInvoice && (
            <Box sx={{ pt: 2 }}>
              <Typography variant="body2" color="text.secondary" gutterBottom>
                Invoice: {selectedInvoice.invoiceNumber}
              </Typography>
              <Typography variant="body2" color="text.secondary" gutterBottom sx={{ mb: 3 }}>
                Customer: {selectedInvoice.customer?.businessName}
              </Typography>

              <Typography variant="body2" color="text.secondary" gutterBottom sx={{ mb: 2 }}>
                Amount Due: ${selectedInvoice.outstandingAmount.toFixed(2)}
              </Typography>

              <TextField
                label="Payment Date"
                type="date"
                fullWidth
                value={paymentData.paymentDate}
                onChange={(e) => setPaymentData({ ...paymentData, paymentDate: e.target.value })}
                sx={{ mb: 2 }}
                InputLabelProps={{ shrink: true }}
                error={paymentAttempted && !isDateValid}
                helperText={paymentAttempted && !isDateValid ? 'Payment date is required' : undefined}
              />

              <TextField
                label="Amount Paid"
                type="number"
                fullWidth
                value={paymentData.amount}
                onChange={(e) => setPaymentData({ ...paymentData, amount: e.target.value })}
                sx={{ mb: 2 }}
                inputProps={{ step: '0.01', min: 0.01, max: selectedInvoice.outstandingAmount }}
                error={paymentAttempted && !isAmountValid}
                helperText={paymentAttempted && !isAmountValid ? `Amount must be greater than $0.00 and no more than $${selectedInvoice.outstandingAmount.toFixed(2)}` : undefined}
              />

              <TextField
                label="Payment Method"
                select
                fullWidth
                value={paymentData.paymentMethod}
                onChange={(e) => setPaymentData({ ...paymentData, paymentMethod: e.target.value })}
                sx={{ mb: 2 }}
              >
                <MenuItem key="bank" value="BankTransfer">Bank Transfer</MenuItem>
                <MenuItem key="cash" value="Cash">Cash</MenuItem>
                <MenuItem key="cheque" value="Cheque">Cheque</MenuItem>
                <MenuItem key="other" value="Other">Other</MenuItem>
              </TextField>

              <TextField
                label="Payment Reference"
                fullWidth
                value={paymentData.reference}
                onChange={(e) => setPaymentData({ ...paymentData, reference: e.target.value })}
                sx={{ mb: 2 }}
                placeholder="e.g., Transaction ID, cheque number"
                helperText="Optional"
              />

              <TextField
                label="Notes"
                fullWidth
                multiline
                rows={3}
                value={paymentData.note}
                onChange={(e) => setPaymentData({ ...paymentData, note: e.target.value })}
              />
            </Box>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => {
            setOpenDialog(false);
            setPaymentAttempted(false);
          }}>Cancel</Button>
          <Button
            onClick={handleSavePayment}
            variant="contained"
            disabled={recordPaymentMutation.isPending}
          >
            Record Payment
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={openBatchDialog} onClose={() => setOpenBatchDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Record Selected Payments</DialogTitle>
        <DialogContent>
          <Box sx={{ pt: 2 }}>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
              This will record full outstanding payments for {selectedInvoices.length} selected invoice(s), total {money(selectedInvoices.reduce((sum, invoice) => sum + invoice.outstandingAmount, 0))}.
            </Typography>
            <TextField
              label="Payment Date"
              type="date"
              fullWidth
              value={batchPaymentData.paymentDate}
              onChange={(e) => setBatchPaymentData({ ...batchPaymentData, paymentDate: e.target.value })}
              sx={{ mb: 2 }}
              InputLabelProps={{ shrink: true }}
            />
            <TextField
              label="Payment Method"
              select
              fullWidth
              value={batchPaymentData.paymentMethod}
              onChange={(e) => setBatchPaymentData({ ...batchPaymentData, paymentMethod: e.target.value })}
              sx={{ mb: 2 }}
            >
              <MenuItem value="BankTransfer">Bank Transfer</MenuItem>
              <MenuItem value="Cash">Cash</MenuItem>
              <MenuItem value="Cheque">Cheque</MenuItem>
              <MenuItem value="Other">Other</MenuItem>
            </TextField>
            <TextField
              label="Payment Reference"
              fullWidth
              value={batchPaymentData.reference}
              onChange={(e) => setBatchPaymentData({ ...batchPaymentData, reference: e.target.value })}
              sx={{ mb: 2 }}
              helperText="Optional"
            />
            <TextField
              label="Notes"
              fullWidth
              multiline
              rows={3}
              value={batchPaymentData.note}
              onChange={(e) => setBatchPaymentData({ ...batchPaymentData, note: e.target.value })}
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenBatchDialog(false)}>Cancel</Button>
          <Button
            onClick={handleBatchSavePayment}
            variant="contained"
            disabled={selectedInvoices.length === 0 || !batchPaymentData.paymentDate || batchRecordPaymentMutation.isPending}
          >
            Record Payments
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}

function normalizePaymentTab(value: string | null): PaymentTab | null {
  if (!value) {
    return null;
  }

  return paymentTabs.some((tab) => tab.value === value) ? value as PaymentTab : null;
}

function isWithinDateRange(invoice: Invoice, fromDate: string, toDate: string) {
  const issueTime = invoice.issueDate.getTime();
  if (fromDate && issueTime < new Date(fromDate).getTime()) {
    return false;
  }

  if (toDate) {
    const end = new Date(toDate);
    end.setHours(23, 59, 59, 999);
    if (issueTime > end.getTime()) {
      return false;
    }
  }

  return true;
}

function compareInvoices(left: Invoice, right: Invoice, field: InvoiceSortField, direction: SortDirection) {
  const multiplier = direction === 'asc' ? 1 : -1;
  const leftValue = invoiceSortValue(left, field);
  const rightValue = invoiceSortValue(right, field);
  if (leftValue < rightValue) return -1 * multiplier;
  if (leftValue > rightValue) return 1 * multiplier;
  return 0;
}

function invoiceSortValue(invoice: Invoice, field: InvoiceSortField): string | number {
  switch (field) {
    case 'customer':
      return invoice.customer?.businessName.toLowerCase() ?? '';
    case 'issueDate':
      return invoice.issueDate.getTime();
    case 'dueDate':
      return invoice.dueDate.getTime();
    case 'amountDue':
      return invoice.outstandingAmount;
    case 'invoiceNumber':
    default:
      return invoice.invoiceNumber.toLowerCase();
  }
}
