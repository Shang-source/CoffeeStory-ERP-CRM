type OrderStatus = 'Generated' | 'InProduction' | 'ReadyToShip' | 'Shipped' | 'Completed' | 'Cancelled';
type InvoiceStatus = 'NotIssued' | 'Draft' | 'Issued' | 'Unpaid' | 'PartiallyPaid' | 'Paid' | 'Overdue' | 'Cancelled';
type ShipmentStatus = 'NotShipped' | 'ReadyToShip' | 'Shipped' | 'Delivered';
type ProductionStatus = 'Pending' | 'InProgress' | 'Completed' | 'OnHold';
type EmailStatus = 'NotSent' | 'Pending' | 'Sent' | 'Failed' | 'Bounced';

export const getOrderStatusColor = (status: OrderStatus): string => {
  const colors: Record<OrderStatus, string> = {
    Generated: '#9E9E9E',
    InProduction: '#FF9800',
    ReadyToShip: '#2196F3',
    Shipped: '#4CAF50',
    Completed: '#009688',
    Cancelled: '#F44336',
  };
  return colors[status];
};

export const getInvoiceStatusColor = (status: InvoiceStatus): string => {
  const colors: Record<InvoiceStatus, string> = {
    NotIssued: '#BDBDBD',
    Draft: '#9E9E9E',
    Issued: '#2196F3',
    Unpaid: '#673AB7',
    PartiallyPaid: '#FF9800',
    Paid: '#4CAF50',
    Overdue: '#F44336',
    Cancelled: '#757575',
  };
  return colors[status];
};

export const getShipmentStatusColor = (status: ShipmentStatus): string => {
  const colors: Record<ShipmentStatus, string> = {
    NotShipped: '#BDBDBD',
    ReadyToShip: '#2196F3',
    Shipped: '#4CAF50',
    Delivered: '#009688',
  };
  return colors[status];
};

export const formatOrderStatus = (status: OrderStatus): string => {
  const labels: Record<OrderStatus, string> = {
    Generated: 'Generated',
    InProduction: 'In Production',
    ReadyToShip: 'Ready to Ship',
    Shipped: 'Shipped',
    Completed: 'Completed',
    Cancelled: 'Cancelled',
  };
  return labels[status];
};

export const formatInvoiceStatus = (status: InvoiceStatus): string => {
  const labels: Record<InvoiceStatus, string> = {
    NotIssued: 'Not Issued',
    Draft: 'Draft',
    Issued: 'Issued',
    Unpaid: 'Unpaid',
    PartiallyPaid: 'Partially Paid',
    Paid: 'Paid',
    Overdue: 'Overdue',
    Cancelled: 'Cancelled',
  };
  return labels[status];
};

export const formatShipmentStatus = (status: ShipmentStatus): string => {
  const labels: Record<ShipmentStatus, string> = {
    NotShipped: 'Not Shipped',
    ReadyToShip: 'Ready to Ship',
    Shipped: 'Shipped',
    Delivered: 'Delivered',
  };
  return labels[status];
};

export const formatProductionStatus = (status: ProductionStatus): string => {
  const labels: Record<ProductionStatus, string> = {
    Pending: 'Pending',
    InProgress: 'In Progress',
    Completed: 'Completed',
    OnHold: 'On Hold',
  };
  return labels[status];
};

export const getProductionStatusColor = (status: ProductionStatus): string => {
  const colors: Record<ProductionStatus, string> = {
    Pending: '#9E9E9E',
    InProgress: '#FF9800',
    Completed: '#4CAF50',
    OnHold: '#F44336',
  };
  return colors[status];
};

export const formatEmailStatus = (status: EmailStatus): string => {
  const labels: Record<EmailStatus, string> = {
    NotSent: 'Not Sent',
    Pending: 'Pending',
    Sent: 'Sent',
    Failed: 'Failed',
    Bounced: 'Bounced',
  };
  return labels[status];
};

export const getEmailStatusColor = (status: EmailStatus): string => {
  const colors: Record<EmailStatus, string> = {
    NotSent: '#BDBDBD',
    Pending: '#FF9800',
    Sent: '#4CAF50',
    Failed: '#F44336',
    Bounced: '#9E9E9E',
  };
  return colors[status];
};
