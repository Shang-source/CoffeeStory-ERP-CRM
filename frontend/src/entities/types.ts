export type UserRole = 'Customer' | 'Admin';

export type AccountStatus = 'Draft' | 'Invited' | 'Active' | 'Suspended' | 'Archived';

export type OrderFrequency = 'Weekly' | 'Fortnightly' | 'Monthly' | 'ManualOnly';

export type StandingOrderStatus = 'Active' | 'Paused' | 'Cancelled';

export type OrderStatus =
  | 'Generated'
  | 'InProduction'
  | 'ReadyToShip'
  | 'Shipped'
  | 'Completed'
  | 'Cancelled';

export type InvoiceStatus =
  | 'NotIssued'
  | 'Draft'
  | 'Issued'
  | 'Unpaid'
  | 'PartiallyPaid'
  | 'Paid'
  | 'Overdue'
  | 'Cancelled';

export type ShipmentStatus =
  | 'NotShipped'
  | 'ReadyToShip'
  | 'Shipped'
  | 'Delivered';

export type StatementStatus =
  | 'Draft'
  | 'ReadyToSend'
  | 'Sent'
  | 'Cancelled';

export type EmailStatus =
  | 'NotSent'
  | 'Pending'
  | 'Sent'
  | 'Failed'
  | 'Bounced';

export type ProductionStatus =
  | 'Pending'
  | 'InProgress'
  | 'Completed'
  | 'OnHold';

export interface ProductionItem {
  id: string;
  productionBatchId: string;
  productId: string;
  productName: string;
  sku: string;
  totalQuantity: number;
  producedQuantity: number;
  status: ProductionStatus;
  orderIds: string[];
  orderNumbers: string[];
}

export type ProductionBatchStatus =
  | 'Open'
  | 'InProgress'
  | 'Completed'
  | 'Cancelled';

export interface ProductionBatch {
  id: string;
  batchNumber: string;
  productionPeriod: string;
  status: ProductionBatchStatus;
  createdAt: Date;
  updatedAt: Date;
}

export interface Customer {
  id: string;
  businessName: string;
  contactPerson: string;
  email: string;
  phone: string;
  billingAddress: string;
  deliveryAddress: string;
  paymentTerms: string;
  accountStatus: AccountStatus;
  createdAt: Date;
}

export interface Product {
  id: string;
  sku: string;
  name: string;
  description: string;
  unit: string;
  price: number;
  cost: number;
  isActive: boolean;
}

export interface CustomerProduct extends Product {
  basePrice: number;
  effectivePrice: number;
  hasOverride: boolean;
}

export interface CustomerPriceBookItem {
  productId: string;
  sku: string;
  name: string;
  unit: string;
  basePrice: number;
  overridePrice?: number;
  effectivePrice: number;
  hasOverride: boolean;
  isActive: boolean;
  notes?: string;
}

export interface CustomerPriceBook {
  customerId: string;
  items: CustomerPriceBookItem[];
}

export interface StandingOrderItem {
  id: string;
  productId: string;
  product: Product;
  quantity: number;
  unitPrice: number;
  notes?: string;
}

export interface StandingOrder {
  id: string;
  customerId: string;
  customer?: Customer;
  frequency: OrderFrequency;
  nextClosingDate: Date;
  status: StandingOrderStatus;
  deliveryNotes?: string;
  internalNotes?: string;
  items: StandingOrderItem[];
}

export interface OrderItem {
  id: string;
  productId: string;
  productNameSnapshot: string;
  skuSnapshot: string;
  quantity: number;
  unitPriceSnapshot: number;
  lineTotal: number;
  notes?: string;
}

export interface Order {
  id: string;
  orderNumber: string;
  customerId: string;
  customer?: Customer;
  standingOrderId: string;
  generatedAt: Date;
  orderStatus: OrderStatus;
  invoiceStatus: InvoiceStatus;
  shipmentStatus: ShipmentStatus;
  subtotal: number;
  gstAmount: number;
  totalAmount: number;
  items: OrderItem[];
}

export interface InvoiceItem {
  id: string;
  description: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface Invoice {
  id: string;
  invoiceNumber: string;
  customerId: string;
  customer?: Customer;
  orderId: string;
  issueDate: Date;
  dueDate: Date;
  subtotal: number;
  gstAmount: number;
  totalAmount: number;
  paidAmount: number;
  outstandingAmount: number;
  status: InvoiceStatus;
  emailStatus?: EmailStatus;
  items: InvoiceItem[];
  payments?: PaymentRecord[];
}

export interface PaymentRecord {
  id: string;
  invoiceId: string;
  amount: number;
  paymentDate: Date;
  paymentMethod: string;
  reference: string;
  markedByUserId: string;
  note?: string;
  isVoided: boolean;
  voidedAt?: Date;
  voidedByUserId?: string;
  voidReason?: string;
}

export interface Statement {
  id: string;
  statementNumber: string;
  customerId: string;
  customer?: Customer;
  statementDate: Date;
  periodStart?: Date;
  periodEnd?: Date;
  totalOutstanding: number;
  status: StatementStatus;
  emailStatus: EmailStatus;
  invoices: Invoice[];
}

export interface AuditLog {
  id: string;
  actorUserId?: string;
  actorRole?: string;
  action: string;
  entityType: string;
  entityId?: string;
  message: string;
  oldValues?: string;
  newValues?: string;
  createdAt: Date;
}

export interface EmailLog {
  id: string;
  relatedEntityType: string;
  relatedEntityId: string;
  recipientEmail: string;
  subject: string;
  status: EmailStatus;
  provider?: string;
  providerMessageId?: string;
  lastProviderEventType?: string;
  lastProviderEventAt?: Date;
  errorMessage?: string;
  createdAt: Date;
  sentAt?: Date;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface AdminDashboard {
  metrics: {
    ordersThisWeek: number;
    inProductionOrders: number;
    shippedThisWeek: number;
    unpaidInvoiceCount: number;
    overdueInvoiceCount: number;
    activeCustomerCount: number;
    totalCustomerCount: number;
    totalOutstanding: number;
  };
  recentOrders: Order[];
  overdueInvoices: Invoice[];
}

export interface CustomerDashboard {
  metrics: {
    openInvoiceCount: number;
    overdueInvoiceCount: number;
    totalOutstanding: number;
    estimatedStandingOrderTotal: number;
  };
  standingOrder?: StandingOrder;
  recentInvoices: Invoice[];
}
