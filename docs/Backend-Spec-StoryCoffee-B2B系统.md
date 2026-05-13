# StoryCoffee B2B 系统 Backend & Database Spec v1.1

## 0. 文档目的

本文档是 StoryCoffee B2B 订单与发票管理系统的第二份开发文档，重点说明后端、数据库、API、状态流转、权限、Quartz、Redis、PDF、Email、EmailLog 和 AuditLog 的实现规格。

第一份 PRD 主要说明产品目标、页面功能、用户流程和前端原型。本文档用于补齐真实后端系统需要实现的业务规则和技术细节，开发人员可以根据本文档拆分后端任务、数据库任务和接口任务。

---

## 1. 系统边界

## 1.0 本次前后端契约对齐结论

本版本重点补齐前后端需要统一的枚举、ProductionBatch 展示策略、Statement 快照返回方式，以及核心 API Response 示例。

### 1.0.1 OrderStatus 最终定义

后端第一版只保留以下 OrderStatus：

```text
Generated
InProduction
ReadyToShip
Shipped
Completed
Cancelled
```

前端原有的：

```text
Invoiced
NeedsReview
```

第一版不作为 OrderStatus 使用。

处理原则：

```text
Invoiced：不放在 order_status 中，由 invoice.status 以及 order.invoice_status 表达开票进度
NeedsReview：第一版不实现，如未来需要，必须补充触发条件、页面展示和状态流转规则
```

### 1.0.2 InvoiceStatus 最终定义

为兼容前端已有概念，同时让发票流程更清楚，后端第一版采用以下 InvoiceStatus：

```text
NotIssued
Draft
Issued
Unpaid
PartiallyPaid
Paid
Overdue
Cancelled
```

其中：

```text
Draft：发票草稿，尚未正式生成 PDF
Issued：PDF 已生成，发票已正式生成，但还未发送给客户
Unpaid：发票已发送给客户，等待付款
PartiallyPaid：客户已部分付款
Paid：客户已付清
Overdue：超过 due_date 且仍未付清
Cancelled：已取消，第一版仅允许 Draft / Issued 状态取消
```

Invoice 状态流转统一为：

```text
NotIssued → Draft → Issued → Unpaid → PartiallyPaid → Paid
                         ↓
                      Overdue
```

说明：

```text
Generate Invoice：创建 Draft Invoice
Generate PDF：Draft → Issued
Send Email：Issued → Unpaid
Record Payment：Unpaid / Overdue → PartiallyPaid / Paid
```

### 1.0.3 EmailStatus 最终定义

为避免前后端枚举不一致，统一使用：

```text
NotSent
Pending
Sent
Failed
Bounced
```

使用规则：

```text
NotSent：Invoice / Statement 从未发送过邮件
Pending：正在发送或已创建发送尝试，但结果未返回
Sent：发送成功
Failed：发送失败
Bounced：邮件退回，第一版可暂不实现 webhook，只预留状态
```

EmailLog 中通常不会出现 NotSent，因为只有发生发送尝试才会创建 EmailLog。

### 1.0.4 ProductionBatch 前端展示策略

后端保留 ProductionBatch 概念，用于真实生产批次管理。

第一版前端可以不展示批次概念，只展示当前默认批次的 ProductionItem 聚合列表。

后端 API 需要支持两种返回方式：

```text
GET /api/admin/production/current
返回当前批次的聚合生产清单，前端可直接使用

GET /api/admin/production/batches
返回批次列表，第二阶段前端再接入
```

第一版推荐：

```text
前端隐藏 ProductionBatch
后端自动创建或选择当前 Open batch
Production List 页面仍然显示扁平化的 product summary
```

### 1.0.5 Statement 快照返回策略

后端必须保存 statement_invoices 快照。

前端可以继续把 Statement 显示为 invoices 列表，但这些 invoices 不是实时 Invoice，而是 StatementInvoiceSnapshot DTO。

即：

```text
StatementDetailResponse.invoices = statement_invoices snapshots
```

前端需要理解：

```text
历史 Statement 不会随着后续付款而改变
```

### 1.1 本文档包含

```text
后端项目结构
数据库表设计
核心 Entity / DTO 设计原则
REST API 设计
订单 / 生产 / 发票 / 付款 / 对账单状态流转
Admin / Customer 权限控制
Quartz.NET 定时任务
Redis 缓存、限流、分布式锁
Invoice / Statement PDF 生成
Email 发送
EmailLog 邮件日志
AuditLog 操作审计
```

### 1.2 本文档不包含

```text
前端页面 UI 细节
自动化测试详细方案
Docker / Kubernetes / Terraform / CI/CD 详细部署方案
数据分析 / BI / Snowflake 方案
```

这些内容放在第三份 Engineering Spec 中说明。

---

## 2. 后端技术栈

```text
Language: C#
Framework: ASP.NET Core Web API
ORM: Entity Framework Core
Database: PostgreSQL
Cache / Lock / Rate Limit: Redis
Authentication: JWT Authentication
Authorization: Role-based Access Control
Scheduled Jobs: Quartz.NET
PDF Generation: QuestPDF
File Storage: AWS S3 or S3-compatible storage
Email Provider: AWS SES or Resend
Logging: Serilog
API Docs: OpenAPI / Swagger / Scalar
Validation: FluentValidation
```

---

## 3. 后端项目结构建议

建议使用分层结构，不要把业务逻辑写在 Controller 中。

```text
StoryCoffee.Api/
├── Controllers/
│   ├── AuthController.cs
│   ├── CustomersController.cs
│   ├── ProductsController.cs
│   ├── StandingOrdersController.cs
│   ├── OrdersController.cs
│   ├── ProductionController.cs
│   ├── InvoicesController.cs
│   ├── PaymentsController.cs
│   ├── StatementsController.cs
│   ├── EmailLogsController.cs
│   └── AuditLogsController.cs
│
├── Application/
│   ├── Auth/
│   ├── Customers/
│   ├── Products/
│   ├── StandingOrders/
│   ├── Orders/
│   ├── Production/
│   ├── Invoices/
│   ├── Payments/
│   ├── Statements/
│   ├── Pdf/
│   ├── Email/
│   ├── AuditLogs/
│   └── Common/
│
├── Domain/
│   ├── Entities/
│   ├── Enums/
│   ├── ValueObjects/
│   └── Rules/
│
├── Infrastructure/
│   ├── Persistence/
│   │   ├── AppDbContext.cs
│   │   ├── EntityConfigurations/
│   │   └── Migrations/
│   ├── Redis/
│   ├── Quartz/
│   ├── Pdf/
│   ├── Storage/
│   ├── Email/
│   └── Logging/
```

### 3.1 Controllers

Controller 只负责：

```text
接收 HTTP 请求
读取当前用户信息
调用 Application Service
返回统一 Response
不要写复杂业务逻辑
```

### 3.2 Application Services

Application Service 负责业务用例，例如：

```text
CreateCustomer
UpdateStandingOrder
GenerateOrderFromStandingOrder
SendOrderToProduction
UpdateProductionQuantity
MarkOrderAsShipped
GenerateInvoicePdf
SendInvoiceEmail
RecordPayment
GenerateWeeklyStatement
```

### 3.3 Domain

Domain 层负责核心业务规则，例如：

```text
订单状态是否允许流转
付款后发票状态如何计算
Statement 是否应该保存快照
Standing Order 是否可以自动生成订单
```

### 3.4 Infrastructure

Infrastructure 层负责外部技术实现，例如：

```text
PostgreSQL
Redis
Quartz.NET
QuestPDF
S3
Email provider
Serilog
```

---

## 4. 用户、角色与权限模型

### 4.1 角色

系统第一版只支持两类角色：

```text
Admin
Customer
```

### 4.2 User 与 Customer 的关系

不要把 Customer 和 User 混为一谈。

Customer 是客户公司，例如 Auckland Cafe。

User 是登录系统的人，例如 John Smith。

一个 Customer 可以有一个或多个 Customer User。第一版可以只支持一个 Customer User，但数据库设计建议预留多用户能力。

### 4.3 建议表

#### users

```sql
create table users (
    id uuid primary key,
    email varchar(255) not null unique,
    password_hash text,
    display_name varchar(255) not null,
    role varchar(30) not null,
    customer_id uuid null references customers(id),
    is_active boolean not null default true,
    last_login_at timestamptz,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);
```

### 4.4 权限规则

#### Admin 可以：

```text
查看所有客户
创建 / 修改客户
创建 / 修改产品
查看所有 Standing Orders
查看所有 Orders
管理 Production List
生成 / 发送 Invoice
记录 Payment
生成 / 发送 Statement
查看 EmailLog
查看 AuditLog
```

#### Customer 可以：

```text
查看自己的 Dashboard
查看和维护自己的 Standing Order
查看自己的 Orders
查看自己的 Invoices
下载自己的 Invoice PDF
查看自己的 Statements
下载自己的 Statement PDF
更新自己的 Account Settings
```

#### Customer 不可以：

```text
访问 /api/admin/*
查看其他客户数据
生成 PDF
发送 Email
记录 Payment
查看 EmailLog
查看 AuditLog
修改付款条款
修改产品价格
```

### 4.5 后端权限原则

后端必须强制校验权限，不能只依赖前端隐藏按钮。

Customer 访问数据时，后端必须从 JWT 中读取 `customerId`，不能相信前端传入的 `customerId`。

示例：

```text
GET /api/customer/invoices/{invoiceId}/download-url
```

后端必须检查：

```text
invoice.customer_id == currentUser.customerId
```

否则返回：

```http
403 Forbidden
```

---

## 5. 数据库通用规范

### 5.1 命名规范

数据库表和字段使用 snake_case。

C# Entity 使用 PascalCase。

示例：

```text
Database table: standing_orders
Database field: next_closing_date
C# Entity: StandingOrder
C# Property: NextClosingDate
```

### 5.2 主键

所有主表使用 UUID：

```sql
id uuid primary key
```

### 5.3 时间字段

所有时间字段使用：

```sql
timestamptz
```

业务日期，例如 due_date、statement_date、payment_date 可以使用：

```sql
date
```

### 5.4 金额字段

所有金额字段使用：

```sql
numeric(12,2)
```

C# 中使用：

```csharp
decimal
```

禁止使用：

```text
float
double
```

原因：订单、GST、发票、付款属于财务数据，不能有浮点误差。

### 5.5 通用审计字段

核心业务表建议包含：

```sql
created_at timestamptz not null default now(),
updated_at timestamptz not null default now(),
created_by uuid null,
updated_by uuid null,
is_deleted boolean not null default false
```

对于快照表，例如 `order_items`、`invoice_items`、`statement_invoices`，可以不加 `is_deleted`。

---

## 6. 数据库表设计

## 6.1 customers

```sql
create table customers (
    id uuid primary key,
    business_name varchar(255) not null,
    contact_person varchar(255) not null,
    email varchar(255) not null,
    phone varchar(50),
    billing_address text,
    delivery_address text,
    payment_terms_days int not null default 7,
    account_status varchar(30) not null default 'Draft',
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid,
    updated_by uuid,
    is_deleted boolean not null default false
);
```

### account_status

```text
Draft
Invited
Active
Suspended
Archived
```

规则：

```text
Draft：客户刚创建，未邀请
Invited：已发送邀请，但客户未激活
Active：正常客户
Suspended：暂停客户，不能自动生成订单
Archived：归档客户，不再出现在常规列表
```

---

## 6.2 products

```sql
create table products (
    id uuid primary key,
    sku varchar(100) not null unique,
    name varchar(255) not null,
    description text,
    unit varchar(50) not null,
    price numeric(12,2) not null,
    cost numeric(12,2) not null,
    is_active boolean not null default true,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid,
    updated_by uuid
);
```

规则：

```text
SKU 必须唯一
is_active = false 的产品不能被新 Standing Order 选择
历史 Order / Invoice 中仍保留该产品快照
```

---

## 6.3 standing_orders

```sql
create table standing_orders (
    id uuid primary key,
    customer_id uuid not null references customers(id),
    frequency varchar(30) not null,
    next_closing_date date not null,
    status varchar(30) not null default 'Active',
    delivery_notes text,
    internal_notes text,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid,
    updated_by uuid
);
```

### frequency

```text
Weekly
Fortnightly
Monthly
ManualOnly
```

### status

```text
Active
Paused
Cancelled
```

规则：

```text
Active：Quartz 可以自动生成订单
Paused：不自动生成订单，但历史订单保留
Cancelled：不自动生成订单，默认不允许恢复，除非 Admin 手动重新激活
ManualOnly：不参与自动生成，只能由 Admin 或 Customer 手动触发订单
```

---

## 6.4 standing_order_items

```sql
create table standing_order_items (
    id uuid primary key,
    standing_order_id uuid not null references standing_orders(id),
    product_id uuid not null references products(id),
    quantity int not null,
    unit_price numeric(12,2) not null,
    notes text
);
```

规则：

```text
quantity 必须大于 0
unit_price 是当前客户该 Standing Order 的订购价格
Product.price 修改不自动影响已有 StandingOrderItem.unit_price
如果需要同步价格，必须由 Admin 确认
```

---

## 6.5 orders

```sql
create table orders (
    id uuid primary key,
    order_number varchar(100) not null unique,
    customer_id uuid not null references customers(id),
    standing_order_id uuid references standing_orders(id),
    generated_at timestamptz not null,
    generated_period varchar(50),
    order_status varchar(30) not null,
    invoice_status varchar(30) not null,
    shipment_status varchar(30) not null,
    subtotal numeric(12,2) not null,
    gst_amount numeric(12,2) not null,
    total_amount numeric(12,2) not null,
    shipped_at timestamptz,
    completed_at timestamptz,
    cancelled_at timestamptz,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid,
    updated_by uuid
);
```

### generated_period

用于防止 Standing Order 重复生成订单。

建议格式：

```text
2026-W19
2026-05
2026-05-08
```

不同 frequency 可以使用不同 period 格式，但必须保证同一个 Standing Order 在同一周期只能生成一次 Order。

### 唯一约束

```sql
create unique index ux_orders_standing_order_period
on orders(standing_order_id, generated_period)
where standing_order_id is not null;
```

---

## 6.6 order_items

```sql
create table order_items (
    id uuid primary key,
    order_id uuid not null references orders(id),
    product_id uuid not null references products(id),
    product_name_snapshot varchar(255) not null,
    sku_snapshot varchar(100) not null,
    quantity int not null,
    unit_price_snapshot numeric(12,2) not null,
    line_total numeric(12,2) not null,
    notes text
);
```

规则：

```text
OrderItem 必须保存产品名称、SKU、单价快照
产品未来改名或改价，不影响历史订单
line_total = quantity * unit_price_snapshot
```

---

## 6.7 production_batches

建议增加 Production Batch，而不是只用 Production Item。

```sql
create table production_batches (
    id uuid primary key,
    batch_number varchar(100) not null unique,
    production_period varchar(50) not null,
    status varchar(30) not null default 'Open',
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid,
    updated_by uuid
);
```

### status

```text
Open
InProgress
Completed
Cancelled
```

第一版可以每周生成一个 Production Batch。

---

## 6.8 production_items

```sql
create table production_items (
    id uuid primary key,
    production_batch_id uuid references production_batches(id),
    product_id uuid not null references products(id),
    product_name_snapshot varchar(255) not null,
    sku_snapshot varchar(100) not null,
    total_quantity int not null,
    produced_quantity int not null default 0,
    status varchar(30) not null default 'Pending',
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);
```

### status

```text
Pending
InProgress
Completed
OnHold
```

规则：

```text
produced_quantity 不能小于 0
produced_quantity 不能大于 total_quantity
produced_quantity == total_quantity 时 status 自动变 Completed
```

---

## 6.9 production_item_orders

用于记录生产项与订单的关系。

```sql
create table production_item_orders (
    production_item_id uuid not null references production_items(id),
    order_id uuid not null references orders(id),
    primary key (production_item_id, order_id)
);
```

第一版规则：

```text
只有某个订单的所有产品对应 ProductionItem 都 Completed，该订单才可以变 ReadyToShip
```

---

## 6.10 invoices

```sql
create table invoices (
    id uuid primary key,
    invoice_number varchar(100) not null unique,
    customer_id uuid not null references customers(id),
    order_id uuid not null references orders(id),
    issue_date date not null,
    due_date date not null,
    subtotal numeric(12,2) not null,
    gst_amount numeric(12,2) not null,
    total_amount numeric(12,2) not null,
    paid_amount numeric(12,2) not null default 0,
    outstanding_amount numeric(12,2) not null,
    status varchar(30) not null,
    pdf_file_key varchar(500),
    pdf_generated_at timestamptz,
    pdf_generated_by uuid,
    email_status varchar(30) not null default 'NotSent',
    last_emailed_at timestamptz,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid,
    updated_by uuid
);
```

### 规则

```text
每个订单第一版只允许一张 Invoice
Invoice 金额来自 Order 快照
Invoice 已发送后，不允许直接修改金额
Paid Invoice 不允许取消
如需退款 / credit note，第二阶段再实现
```

---

## 6.11 invoice_items

```sql
create table invoice_items (
    id uuid primary key,
    invoice_id uuid not null references invoices(id),
    description text not null,
    quantity int not null,
    unit_price numeric(12,2) not null,
    line_total numeric(12,2) not null
);
```

规则：

```text
InvoiceItem 从 OrderItem 复制生成
必须保存快照，不依赖当前 Product 表
```

---

## 6.12 payment_records

```sql
create table payment_records (
    id uuid primary key,
    invoice_id uuid not null references invoices(id),
    customer_id uuid not null references customers(id),
    amount numeric(12,2) not null,
    payment_date date not null,
    payment_method varchar(50) not null,
    reference varchar(255),
    marked_by uuid not null references users(id),
    note text,
    status varchar(30) not null default 'Confirmed',
    created_at timestamptz not null default now(),
    voided_at timestamptz,
    voided_by uuid,
    void_reason text
);
```

### payment_method

```text
BankTransfer
Cash
Cheque
Other
```

### status

```text
Confirmed
Voided
```

规则：

```text
amount 必须大于 0
amount 不能超过 invoice.outstanding_amount
PaymentRecord 创建后不建议物理删除
如果录错，用 Void 操作撤销
Void 后需要重新计算 Invoice paid_amount 和 outstanding_amount
```

---

## 6.13 statements

```sql
create table statements (
    id uuid primary key,
    statement_number varchar(100) not null unique,
    customer_id uuid not null references customers(id),
    statement_date date not null,
    period_start date,
    period_end date,
    total_outstanding numeric(12,2) not null,
    status varchar(30) not null,
    email_status varchar(30) not null default 'NotSent',
    pdf_file_key varchar(500),
    pdf_generated_at timestamptz,
    pdf_generated_by uuid,
    last_emailed_at timestamptz,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid,
    updated_by uuid
);
```

### status

```text
Draft
ReadyToSend
Sent
Cancelled
```

---

## 6.14 statement_invoices

Statement 必须保存快照，避免历史 Statement 因后续付款而变化。

```sql
create table statement_invoices (
    statement_id uuid not null references statements(id),
    invoice_id uuid not null references invoices(id),
    invoice_number_snapshot varchar(100) not null,
    issue_date_snapshot date not null,
    due_date_snapshot date not null,
    total_amount_snapshot numeric(12,2) not null,
    paid_amount_snapshot numeric(12,2) not null,
    outstanding_amount_snapshot numeric(12,2) not null,
    status_snapshot varchar(30) not null,
    primary key (statement_id, invoice_id)
);
```

---

## 6.15 email_logs

```sql
create table email_logs (
    id uuid primary key,
    entity_type varchar(50) not null,
    entity_id uuid not null,
    customer_id uuid,
    recipient_email varchar(255) not null,
    subject varchar(500) not null,
    body_preview text,
    attachment_file_key varchar(500),
    email_provider varchar(50),
    provider_message_id varchar(255),
    status varchar(30) not null,
    error_message text,
    sent_by uuid,
    sent_at timestamptz,
    created_at timestamptz not null default now()
);
```

### entity_type

```text
Invoice
Statement
PaymentReminder
CustomerInvite
```

### status

统一使用 EmailStatus：

```text
NotSent
Pending
Sent
Failed
Bounced
```

规则：

```text
Invoice.email_status / Statement.email_status 可以是 NotSent
EmailLog.status 通常从 Pending 开始，然后变 Sent / Failed / Bounced
第一版只要求实现 Pending / Sent / Failed
Bounced 第二阶段通过 Email Provider webhook 实现
```

---

## 6.16 audit_logs

```sql
create table audit_logs (
    id uuid primary key,
    user_id uuid,
    user_role varchar(50),
    entity_type varchar(50) not null,
    entity_id uuid not null,
    action varchar(100) not null,
    old_values jsonb,
    new_values jsonb,
    ip_address varchar(100),
    user_agent text,
    created_at timestamptz not null default now()
);
```

### entity_type

```text
Customer
Product
StandingOrder
Order
ProductionBatch
ProductionItem
Invoice
PaymentRecord
Statement
Email
```

### action

```text
Created
Updated
Deleted
StatusChanged
GeneratedOrder
SentToProduction
UpdatedProductionQuantity
MarkedProductionCompleted
MarkedOrderShipped
GeneratedInvoice
RecordedPayment
VoidedPayment
GeneratedStatement
GeneratedPdf
SentEmail
Cancelled
```

---

## 7. 核心状态机

## 7.1 Order 状态机

```text
Generated
   ↓ Send to Production
InProduction
   ↓ All related ProductionItems completed
ReadyToShip
   ↓ Mark as Shipped
Shipped
   ↓ Invoice Paid
Completed
```

任何未完成状态可以进入：

```text
Cancelled
```

### Order 状态规则

```text
Generated：订单刚生成，可以发送到生产或取消
InProduction：生产中，不能直接开票，不能删除
ReadyToShip：生产完成，可以标记出货
Shipped：已出货，可以生成 Invoice
Completed：发票已付清，订单完成
Cancelled：取消订单，不再参与生产和开票
```

### 禁止状态

```text
Cancelled 订单不能发送到生产
Cancelled 订单不能生成 Invoice
Completed 订单不能取消
Shipped 订单不能回退到 Generated
```

---

## 7.2 Shipment 状态机

```text
NotShipped
   ↓ Production completed
ReadyToShip
   ↓ Mark as Shipped
Shipped
   ↓ Optional delivery confirmation
Delivered
```

### 联动规则

```text
order_status = ReadyToShip 时，shipment_status 必须是 ReadyToShip
order_status = Shipped 时，shipment_status 必须是 Shipped 或 Delivered
```

---

## 7.3 Invoice 状态机

```text
NotIssued
   ↓ Generate Invoice
Draft
   ↓ Generate PDF
Issued
   ↓ Send Email
Unpaid
   ↓ Due date passed
Overdue
   ↓ Partial payment
PartiallyPaid
   ↓ Full payment
Paid
```

### 其他状态

```text
Cancelled
```

### Invoice 状态规则

```text
NotIssued：订单还未生成发票
Draft：发票已生成，但 PDF 尚未正式生成
Issued：PDF 已生成，发票已正式生成，但还未发送给客户
Unpaid：发票已发送给客户，未付款
Overdue：超过 due_date，仍未付清
PartiallyPaid：部分付款
Paid：已付清
Cancelled：取消发票，第一版仅允许 Draft / Issued 状态取消
```

### 状态动作定义

```text
Generate Invoice：NotIssued → Draft
Generate PDF：Draft → Issued
Send Email：Issued → Unpaid
Record Payment：Unpaid / Overdue → PartiallyPaid / Paid
Overdue Job：Unpaid / PartiallyPaid → Overdue
```

### 付款后状态计算

```text
new_paid_amount = current_paid_amount + payment_amount
new_outstanding_amount = total_amount - new_paid_amount

if new_outstanding_amount <= 0:
    status = Paid
else if new_paid_amount > 0:
    status = PartiallyPaid
else:
    status = Unpaid or Overdue
```

规则：

```text
payment_amount 必须 > 0
payment_amount 不能大于 outstanding_amount
Paid Invoice 不能继续收款
Paid Invoice 不能修改金额
Issued / Unpaid / PartiallyPaid / Paid 状态下，Invoice 金额不可直接修改
```

---

## 7.4 Production 状态机

```text
Pending
   ↓ Start Production
InProgress
   ↓ Mark Completed or Produced Quantity = Total Quantity
Completed
```

可选状态：

```text
OnHold
```

规则：

```text
Pending 可以 Start
InProgress 可以 Update Quantity 或 Complete
Completed 不能继续修改数量，除非 Admin 执行 Reopen
OnHold 可以 Resume 回 InProgress
```

第一版可以不做 Reopen。

---

## 7.5 Statement 状态机

```text
Draft
   ↓ Review / Confirm
ReadyToSend
   ↓ Send Email
Sent
```

可选：

```text
Cancelled
```

规则：

```text
Draft 可以重新生成 PDF
Sent 状态默认不允许修改包含的 Invoice 快照
Sent 后如果要重新发送，可以 Resend，但必须写 EmailLog 和 AuditLog
```

---

## 8. 核心业务规则

## 8.1 GST 规则

```text
GST rate = 15%
gst_amount = subtotal * 0.15
total_amount = subtotal + gst_amount
```

金额保留两位小数。

---

## 8.2 订单号、发票号、对账单号

### Order Number

```text
ORD-YYYYMMDD-XXX
Example: ORD-20260508-001
```

### Invoice Number

```text
INV-YYYYMMDD-XXX
Example: INV-20260508-001
```

### Statement Number

```text
STMT-YYYYMMDD-XXX
Example: STMT-20260508-001
```

编号生成必须在数据库事务中完成，避免并发重复。

---

## 8.3 Standing Order 自动生成订单规则

当 Standing Order 满足以下条件时，可以自动生成订单：

```text
standing_order.status = Active
customer.account_status = Active
standing_order.frequency != ManualOnly
standing_order.next_closing_date <= today in Pacific/Auckland timezone
standing_order has at least one item
```

生成订单时：

```text
复制 StandingOrderItem 到 OrderItem
保存 product_name_snapshot
保存 sku_snapshot
保存 unit_price_snapshot
计算 subtotal / gst_amount / total_amount
order_status = Generated
invoice_status = NotIssued
shipment_status = NotShipped
生成 generated_period
更新 next_closing_date
写 AuditLog
```

防重复：

```text
Redis lock
Database unique index: standing_order_id + generated_period
```

---

## 8.4 Production List 汇总规则

当订单进入 InProduction 后，系统需要按产品汇总生产需求。

第一版规则：

```text
选取 order_status in Generated / InProduction / ReadyToShip 的订单
按 product_id 汇总 quantity
生成或更新 ProductionItem
记录 ProductionItem 与 Order 的关系
```

更严格的规则：

```text
只有 Send to Production 后的订单才进入 Production List
Generated 订单不自动进入生产清单，除非 Admin 执行 Send to Production
```

建议采用更严格规则，避免客户订单刚生成就进入生产。

---

## 8.5 订单变 ReadyToShip 规则

当某个订单关联的所有产品生产项都 Completed：

```text
order_status = ReadyToShip
shipment_status = ReadyToShip
```

如果任一产品未完成：

```text
order_status 保持 InProduction
```

---

## 8.6 出货与发票规则

当 Admin 标记订单为 Shipped：

```text
order_status = Shipped
shipment_status = Shipped
shipped_at = now()
```

如果该订单没有 Invoice：

```text
创建 Draft Invoice
invoice.status = Draft
order.invoice_status = Draft
```

第一版规则：

```text
每个订单只能生成一张 Invoice
订单必须 Shipped 后才能生成 Invoice
Generate Invoice 只创建 Draft Invoice
Generate Invoice PDF 后，Invoice 从 Draft 变 Issued
Send Invoice Email 后，Invoice 从 Issued 变 Unpaid
Invoice 进入 Issued 之后，金额默认锁定，不允许直接修改
```

---

## 8.7 付款规则

记录付款时：

```text
invoice.status 必须是 Unpaid / Overdue / PartiallyPaid
amount > 0
amount <= invoice.outstanding_amount
创建 PaymentRecord
更新 invoice.paid_amount
更新 invoice.outstanding_amount
重新计算 invoice.status
写 AuditLog
清理相关 Redis dashboard cache
```

如果 Invoice 已付清：

```text
invoice.status = Paid
invoice.outstanding_amount = 0
```

如果该 Invoice 所属 Order 已 Shipped：

```text
order_status = Completed
completed_at = now()
```

---

## 8.8 Statement 生成规则

生成 Statement 时：

```text
查询 status in Unpaid / Overdue / PartiallyPaid 的 Invoice
且 outstanding_amount > 0
按 customer_id 分组
每个 Customer 生成一个 Statement
保存 StatementInvoices 快照
total_outstanding = sum(outstanding_amount)
status = Draft
email_status = NotSent
写 AuditLog
```

历史 Statement 必须保持快照。

付款后不修改历史 Statement 内容。

---

## 9. REST API 设计

## 9.1 通用规则

### Base URL

```text
/api
```

### Auth Header

```http
Authorization: Bearer <jwt_token>
```

### 成功响应格式

```json
{
  "data": {},
  "message": "Success"
}
```

### 分页响应格式

```json
{
  "data": [],
  "total": 100,
  "page": 1,
  "limit": 20
}
```

### 错误响应格式

```json
{
  "error": {
    "code": "INVALID_PAYMENT_AMOUNT",
    "message": "Payment amount cannot exceed outstanding amount",
    "details": {
      "field": "amount",
      "value": 500,
      "max": 323.15
    }
  }
}
```

### 常用 HTTP 状态码

```text
200 OK
201 Created
400 Bad Request
401 Unauthorized
403 Forbidden
404 Not Found
409 Conflict
422 Unprocessable Entity
500 Internal Server Error
```

---

## 9.2 Auth API

### POST /api/auth/login

Request:

```json
{
  "email": "admin@storycoffee.co.nz",
  "password": "admin123"
}
```

Response:

```json
{
  "data": {
    "accessToken": "jwt-token",
    "user": {
      "id": "uuid",
      "email": "admin@storycoffee.co.nz",
      "displayName": "Admin User",
      "role": "Admin",
      "customerId": null
    }
  }
}
```

---

## 9.3 Admin Customers API

### GET /api/admin/customers

Query:

```text
status
search
page
limit
```

### POST /api/admin/customers

Request:

```json
{
  "businessName": "Auckland Cafe",
  "contactPerson": "John Smith",
  "email": "john@aucklandcafe.co.nz",
  "phone": "09 123 4567",
  "billingAddress": "123 Queen St, Auckland",
  "deliveryAddress": "123 Queen St, Auckland",
  "paymentTermsDays": 7
}
```

Side effects:

```text
Create Customer
Create Customer user if required
Write AuditLog: Created Customer
```

### PATCH /api/admin/customers/{id}

Side effects:

```text
Update Customer
Write AuditLog with old/new values
```

---

## 9.4 Products API

### GET /api/admin/products

### POST /api/admin/products

Request:

```json
{
  "sku": "HB-1KG",
  "name": "House Blend 1kg",
  "description": "House blend coffee beans",
  "unit": "kg",
  "price": 45.00,
  "cost": 25.00,
  "isActive": true
}
```

Validation:

```text
sku required and unique
price >= 0
cost >= 0
```

### PATCH /api/admin/products/{id}

Side effects:

```text
Write AuditLog
Does not change historical OrderItems / InvoiceItems
```

---

## 9.5 Standing Orders API

### GET /api/admin/standing-orders

Admin can view all.

### GET /api/customer/standing-order

Customer can only view own Standing Order.

### POST /api/admin/standing-orders

Request:

```json
{
  "customerId": "uuid",
  "frequency": "Fortnightly",
  "nextClosingDate": "2026-05-12",
  "status": "Active",
  "deliveryNotes": "Deliver Monday morning",
  "internalNotes": "VIP customer",
  "items": [
    {
      "productId": "uuid",
      "quantity": 5,
      "unitPrice": 45.00,
      "notes": ""
    }
  ]
}
```

Validation:

```text
customer must exist
customer must be Active
items length > 0
quantity > 0
unitPrice >= 0
```

### PATCH /api/customer/standing-order

Customer can update:

```text
frequency
status
items
delivery_notes
```

Customer cannot update:

```text
internal_notes
customer_id
payment_terms
```

Side effects:

```text
Write AuditLog
Do not affect already generated Orders
```

---

## 9.6 Orders API

### GET /api/admin/orders

Query:

```text
status
customerId
from
to
page
limit
```

### GET /api/customer/orders

Customer can only see own orders.

### POST /api/admin/orders/batch-to-production

Request:

```json
{
  "orderIds": ["uuid1", "uuid2"]
}
```

Validation:

```text
All orders must exist
All orders must be Generated
Cancelled orders are not allowed
```

Side effects:

```text
Update order_status = InProduction
Create or update ProductionBatch / ProductionItems
Write AuditLog for each order
Clear dashboard Redis cache
```

Response:

```json
{
  "data": {
    "updated": 3,
    "orders": []
  }
}
```

### POST /api/admin/orders/{id}/mark-shipped

Validation:

```text
order_status must be ReadyToShip
```

Side effects:

```text
order_status = Shipped
shipment_status = Shipped
shipped_at = now()
if invoice not exists, create Draft Invoice
order.invoice_status = Draft
Write AuditLog
```

### POST /api/admin/orders/{id}/cancel

Validation:

```text
order_status not in Completed / Shipped
invoice_status not Paid
```

Side effects:

```text
order_status = Cancelled
cancelled_at = now()
Write AuditLog
```

---

## 9.7 Production API

### GET /api/admin/production/current

返回当前默认 ProductionBatch 的聚合生产清单。第一版前端使用这个接口即可，不需要展示 batch 概念。

Response:

```json
{
  "data": {
    "batch": {
      "id": "batch-uuid",
      "batchNumber": "PB-20260508-001",
      "productionPeriod": "2026-W19",
      "status": "Open"
    },
    "items": [
      {
        "id": "production-item-uuid",
        "productId": "product-uuid",
        "productName": "House Blend 1kg",
        "sku": "HB-1KG",
        "totalQuantity": 10,
        "producedQuantity": 6,
        "status": "InProgress",
        "relatedOrders": [
          {
            "orderId": "order-uuid",
            "orderNumber": "ORD-20260508-001"
          }
        ]
      }
    ]
  }
}
```

### GET /api/admin/production/batches

第二阶段使用，用于展示历史批次或选择不同批次。

Response:

```json
{
  "data": [
    {
      "id": "batch-uuid",
      "batchNumber": "PB-20260508-001",
      "productionPeriod": "2026-W19",
      "status": "Open",
      "createdAt": "2026-05-08T10:30:00Z"
    }
  ]
}
```

### PATCH /api/admin/production/items/{id}

Request:

```json
{
  "producedQuantity": 10,
  "status": "Completed"
}
```

Validation:

```text
producedQuantity >= 0
producedQuantity <= totalQuantity
```

Side effects:

```text
Update ProductionItem
If producedQuantity == totalQuantity, status = Completed
Check related orders
If all products for an order completed, update order to ReadyToShip and shipment to ReadyToShip
Write AuditLog
Clear dashboard cache
```

Response:

```json
{
  "data": {
    "productionItem": {
      "id": "production-item-uuid",
      "productName": "House Blend 1kg",
      "totalQuantity": 10,
      "producedQuantity": 10,
      "status": "Completed"
    },
    "affectedOrders": [
      {
        "orderId": "order-uuid",
        "orderNumber": "ORD-20260508-001",
        "orderStatus": "ReadyToShip",
        "shipmentStatus": "ReadyToShip"
      }
    ]
  }
}
```

---

## 9.8 Invoices API

### GET /api/admin/invoices

Query:

```text
status
customerId
from
to
page
limit
```

Response:

```json
{
  "data": [
    {
      "id": "invoice-uuid",
      "invoiceNumber": "INV-20260508-001",
      "customerId": "customer-uuid",
      "customerName": "Auckland Cafe",
      "orderId": "order-uuid",
      "orderNumber": "ORD-20260508-001",
      "issueDate": "2026-05-08",
      "dueDate": "2026-05-15",
      "subtotal": 281.00,
      "gstAmount": 42.15,
      "totalAmount": 323.15,
      "paidAmount": 0,
      "outstandingAmount": 323.15,
      "status": "Issued",
      "emailStatus": "NotSent",
      "pdfFileKey": "invoices/2026/05/INV-20260508-001.pdf"
    }
  ],
  "total": 1,
  "page": 1,
  "limit": 20
}
```

### GET /api/customer/invoices

Customer only sees own invoices.

### POST /api/admin/invoices/{id}/generate-pdf

Validation:

```text
Invoice must exist
Invoice.status must be Draft or Issued
Cancelled Invoice cannot generate PDF
```

Side effects:

```text
Generate PDF with QuestPDF
Upload to S3
Update pdf_file_key / pdf_generated_at / pdf_generated_by
If status is Draft, update status to Issued
Update related order.invoice_status = Issued
Write AuditLog: GeneratedPdf
```

Response:

```json
{
  "data": {
    "invoiceId": "invoice-uuid",
    "invoiceNumber": "INV-20260508-001",
    "status": "Issued",
    "pdfFileKey": "invoices/2026/05/INV-20260508-001.pdf",
    "pdfGeneratedAt": "2026-05-08T10:30:00Z"
  }
}
```

### GET /api/admin/invoices/{id}/download-url

Admin can download any invoice PDF.

### GET /api/customer/invoices/{id}/download-url

Customer can only download own invoice PDF.

Response:

```json
{
  "data": {
    "downloadUrl": "https://presigned-url",
    "expiresInSeconds": 300
  }
}
```

### POST /api/admin/invoices/{id}/send-email

Request:

```json
{
  "recipientEmail": "john@aucklandcafe.co.nz",
  "message": "Please find your invoice attached."
}
```

Validation:

```text
Invoice must exist
Invoice.status must be Issued / Unpaid / Overdue / PartiallyPaid
Cancelled Invoice cannot be sent
Recipient email required
```

Side effects:

```text
If PDF does not exist, generate PDF first
Create EmailLog with status = Pending
Send email through provider
If success:
  EmailLog.status = Sent
  invoice.email_status = Sent
  invoice.last_emailed_at = now()
  if invoice.status = Issued, update invoice.status = Unpaid
  update order.invoice_status = Unpaid
If failed:
  EmailLog.status = Failed
  invoice.email_status = Failed
  save error_message
Write AuditLog: SentEmail
```

Response:

```json
{
  "data": {
    "invoiceId": "invoice-uuid",
    "invoiceNumber": "INV-20260508-001",
    "status": "Unpaid",
    "emailStatus": "Sent",
    "emailLogId": "email-log-uuid",
    "sentAt": "2026-05-08T10:35:00Z"
  }
}
```

---

## 9.9 Payments API

### GET /api/admin/payments

Query:

```text
customerId
invoiceId
from
to
page
limit
```

### POST /api/admin/payments

Request:

```json
{
  "invoiceId": "uuid",
  "amount": 323.15,
  "paymentDate": "2026-05-08",
  "paymentMethod": "BankTransfer",
  "reference": "BANK-REF-123",
  "note": "Paid by bank transfer"
}
```

Validation:

```text
invoice must exist
invoice.status must be Unpaid / Overdue / PartiallyPaid
amount > 0
amount <= invoice.outstanding_amount
```

Side effects:

```text
Create PaymentRecord
Update Invoice paid_amount / outstanding_amount / status
If Invoice becomes Paid, update related Order to Completed
Write AuditLog: RecordedPayment
Clear dashboard cache
```

### POST /api/admin/payments/{id}/void

Request:

```json
{
  "reason": "Payment was recorded against the wrong invoice"
}
```

Side effects:

```text
Set payment.status = Voided
Recalculate invoice paid_amount / outstanding_amount / status
Write AuditLog: VoidedPayment
```

---

## 9.10 Statements API

### POST /api/admin/statements/generate-weekly

Side effects:

```text
Find unpaid / overdue / partially paid invoices
Group by customer
Create Statement per customer
Create StatementInvoices snapshots
Write AuditLog
```

Response:

```json
{
  "data": {
    "generated": 2,
    "statements": [
      {
        "id": "statement-uuid",
        "statementNumber": "STMT-20260508-001",
        "customerId": "customer-uuid",
        "customerName": "Auckland Cafe",
        "statementDate": "2026-05-08",
        "periodStart": "2026-05-01",
        "periodEnd": "2026-05-08",
        "totalOutstanding": 646.30,
        "status": "Draft",
        "emailStatus": "NotSent"
      }
    ]
  }
}
```

### GET /api/admin/statements

### GET /api/customer/statements

Customer only sees own statements.

### GET /api/admin/statements/{id}

### GET /api/customer/statements/{id}

Customer can only access own statement.

Response uses snapshot data, not live Invoice data:

```json
{
  "data": {
    "id": "statement-uuid",
    "statementNumber": "STMT-20260508-001",
    "customerId": "customer-uuid",
    "customerName": "Auckland Cafe",
    "statementDate": "2026-05-08",
    "periodStart": "2026-05-01",
    "periodEnd": "2026-05-08",
    "totalOutstanding": 646.30,
    "status": "Draft",
    "emailStatus": "NotSent",
    "invoices": [
      {
        "invoiceId": "invoice-uuid",
        "invoiceNumber": "INV-20260501-001",
        "issueDate": "2026-05-01",
        "dueDate": "2026-05-08",
        "totalAmount": 323.15,
        "paidAmount": 0,
        "outstandingAmount": 323.15,
        "status": "Overdue"
      }
    ]
  }
}
```

### POST /api/admin/statements/{id}/generate-pdf

Side effects:

```text
Generate Statement PDF
Upload to S3
Update pdf_file_key / pdf_generated_at / pdf_generated_by
If statement.status = Draft, keep Draft or update to ReadyToSend based on business decision
Write AuditLog
```

### GET /api/admin/statements/{id}/download-url

### GET /api/customer/statements/{id}/download-url

Return pre-signed URL.

### POST /api/admin/statements/{id}/send-email

Side effects:

```text
If PDF does not exist, generate PDF first
Create EmailLog with status = Pending
Send email
If success:
  EmailLog.status = Sent
  statement.email_status = Sent
  statement.status = Sent
  statement.last_emailed_at = now()
If failed:
  EmailLog.status = Failed
  statement.email_status = Failed
Write AuditLog
```

---

## 9.11 EmailLogs API

### GET /api/admin/email-logs

Query:

```text
entityType
entityId
customerId
status
page
limit
```

Admin only.

Customer cannot access EmailLogs.

---

## 9.12 AuditLogs API

### GET /api/admin/audit-logs

Query:

```text
entityType
entityId
userId
action
from
to
page
limit
```

Admin only.

Customer cannot access AuditLogs.

---

## 10. PDF 规格

## 10.1 工具

第一版使用：

```text
QuestPDF
```

原因：

```text
适合 .NET
不依赖浏览器
部署到 Docker / Kubernetes 更简单
结构化 Invoice / Statement 容易生成
```

---

## 10.2 Invoice PDF 内容

Invoice PDF 必须包含：

```text
StoryCoffee logo / company name
Invoice Number
Issue Date
Due Date
Customer business name
Customer contact person
Billing address
Customer email
Order number
Shipped date
Invoice items
Subtotal
GST 15%
Total amount
Paid amount
Outstanding amount
Payment instructions
Footer
```

Payment instructions 包括：

```text
Bank account name
Bank account number
Payment reference: invoice number
```

---

## 10.3 Statement PDF 内容

Statement PDF 必须包含：

```text
StoryCoffee logo / company name
Statement Number
Statement Date
Period Start
Period End
Customer business name
Billing address
Customer email
Included invoices
Total outstanding
Payment instructions
Footer
```

Included invoices 字段：

```text
Invoice Number
Issue Date
Due Date
Total Amount
Paid Amount
Outstanding Amount
Status
```

---

## 10.4 S3 路径

Invoice PDF：

```text
invoices/{yyyy}/{mm}/{invoiceNumber}.pdf
```

Statement PDF：

```text
statements/{yyyy}/{mm}/{statementNumber}.pdf
```

S3 文件默认私有。

下载时由后端生成 pre-signed URL。

默认有效期：

```text
300 seconds
```

---

## 11. Email 规格

## 11.1 Email Provider

第一版可以选择：

```text
AWS SES
Resend
```

本地开发可以使用：

```text
MailHog
```

---

## 11.2 Invoice Email

Subject:

```text
Invoice {invoiceNumber} from StoryCoffee
```

Body:

```text
Hi {contactPerson},

Please find attached invoice {invoiceNumber} for your recent StoryCoffee order.

Invoice total: ${totalAmount}
Amount due: ${outstandingAmount}
Due date: {dueDate}

Please use the invoice number as the payment reference.

Thank you,
StoryCoffee
```

---

## 11.3 Statement Email

Subject:

```text
Statement {statementNumber} from StoryCoffee
```

Body:

```text
Hi {contactPerson},

Please find attached your latest StoryCoffee account statement.

Total outstanding: ${totalOutstanding}
Statement date: {statementDate}

Please arrange payment at your earliest convenience.

Thank you,
StoryCoffee
```

---

## 11.4 Email 发送规则

```text
发送前必须确认 recipientEmail 存在
如果 PDF 不存在，先自动生成 PDF
发送成功后写 EmailLog status = Sent
发送失败后写 EmailLog status = Failed，并记录 error_message
更新 Invoice / Statement email_status
写 AuditLog
```

第一版允许 Resend。

Resend 必须再次创建 EmailLog。

---

## 12. AuditLog 规格

## 12.1 必须记录的动作

```text
Create Customer
Update Customer
Create Product
Update Product
Update Standing Order
Generate Order from Standing Order
Send Order to Production
Update Production Quantity
Mark Production Completed
Mark Order as Shipped
Generate Invoice
Generate Invoice PDF
Send Invoice Email
Record Payment
Void Payment
Generate Weekly Statement
Generate Statement PDF
Send Statement Email
Cancel Order
```

## 12.2 记录内容

每条 AuditLog 必须尽量包含：

```text
user_id
user_role
entity_type
entity_id
action
old_values
new_values
ip_address
user_agent
created_at
```

## 12.3 示例

订单状态变化：

```json
{
  "entityType": "Order",
  "entityId": "order-id",
  "action": "StatusChanged",
  "oldValues": {
    "orderStatus": "Generated"
  },
  "newValues": {
    "orderStatus": "InProduction"
  }
}
```

记录付款：

```json
{
  "entityType": "Invoice",
  "entityId": "invoice-id",
  "action": "RecordedPayment",
  "oldValues": {
    "paidAmount": 0,
    "outstandingAmount": 323.15,
    "status": "Unpaid"
  },
  "newValues": {
    "paidAmount": 323.15,
    "outstandingAmount": 0,
    "status": "Paid"
  }
}
```

---

## 13. Quartz.NET 定时任务规格

## 13.1 Jobs

第一版需要实现：

```text
GenerateOrdersFromStandingOrdersJob
UpdateOverdueInvoicesJob
GenerateWeeklyStatementsJob
SendPaymentReminderJob optional
```

---

## 13.2 GenerateOrdersFromStandingOrdersJob

运行频率：

```text
每天凌晨 1 点，Pacific/Auckland timezone
```

逻辑：

```text
1. 获取 Redis lock
2. 查询 Active Standing Orders
3. next_closing_date <= today
4. customer.account_status = Active
5. frequency != ManualOnly
6. 为每个 Standing Order 生成 Order
7. 复制 StandingOrderItems 到 OrderItems
8. 保存产品快照和价格快照
9. 计算金额
10. 更新 next_closing_date
11. 写 AuditLog
12. 释放 Redis lock
```

防重复：

```text
Redis lock
orders unique index: standing_order_id + generated_period
```

---

## 13.3 UpdateOverdueInvoicesJob

运行频率：

```text
每天凌晨 2 点，Pacific/Auckland timezone
```

逻辑：

```text
1. 查询 due_date < today
2. status in Unpaid / PartiallyPaid
3. outstanding_amount > 0
4. 更新 status = Overdue
5. 写 AuditLog
6. 清理 dashboard cache
```

---

## 13.4 GenerateWeeklyStatementsJob

运行频率：

```text
每周一凌晨 3 点，Pacific/Auckland timezone
```

逻辑：

```text
1. 获取 Redis lock
2. 查询未付清 invoices
3. 按 customer_id 分组
4. 每个客户生成一张 Statement
5. 保存 statement_invoices 快照
6. 写 AuditLog
7. 释放 Redis lock
```

---

## 14. Redis 规格

## 14.1 Redis 用途

```text
Dashboard cache
Rate limiting
Quartz distributed lock
Temporary job status
```

---

## 14.2 Dashboard Cache

Key：

```text
admin:dashboard:summary
customer:{customerId}:dashboard:summary
```

TTL：

```text
60 seconds to 300 seconds
```

以下操作后需要清理缓存：

```text
Order created
Order status changed
Production updated
Invoice generated
Payment recorded
Statement generated
```

---

## 14.3 Rate Limiting

Key：

```text
rate:login:{ip}
rate:send-email:{userId}
rate:download-pdf:{userId}
```

建议规则：

```text
Login: 5 minutes max 10 attempts per IP
Send email: 1 minute max 5 attempts per user
Download PDF: 1 minute max 20 attempts per user
```

---

## 14.4 Distributed Lock

Key：

```text
lock:job:generate-orders:{date}
lock:job:weekly-statements:{week}
```

规则：

```text
获取锁成功才执行 job
锁必须有 TTL，避免死锁
Job 完成后释放锁
即使 Redis lock 失效，也必须依赖数据库 unique constraint 防重复
```

---

## 15. Error Codes

```text
UNAUTHORIZED
FORBIDDEN
NOT_FOUND
VALIDATION_ERROR
INVALID_ORDER_STATUS
INVALID_INVOICE_STATUS
INVALID_PAYMENT_AMOUNT
PAYMENT_EXCEEDS_OUTSTANDING_AMOUNT
PDF_NOT_FOUND
EMAIL_SEND_FAILED
DUPLICATE_STANDING_ORDER_GENERATION
DUPLICATE_ORDER_NUMBER
DUPLICATE_INVOICE_NUMBER
DUPLICATE_STATEMENT_NUMBER
RESOURCE_CONFLICT
```

---

## 16. MVP 不支持范围

第一版不实现以下复杂功能：

```text
Credit Note
Refund
Bank reconciliation
Xero integration
Email bounce webhook
Multi-currency
Partial shipment
Complex customer-specific price book
Inventory stock deduction
Advanced production allocation
```

如果业务中出现这些情况，第一版通过人工备注处理。

---

## 17. 开发优先级

## Phase 1: 基础后端与数据库

```text
1. PostgreSQL schema
2. EF Core entities and migrations
3. Users / Auth / JWT / RBAC
4. Customers API
5. Products API
6. StandingOrders API
```

## Phase 2: 核心订单流程

```text
1. Orders API
2. Send orders to production
3. Production batch and production items
4. Update production quantity
5. Order ReadyToShip logic
6. Mark order as shipped
```

## Phase 3: Invoice / Payment / Statement

```text
1. Generate Draft Invoice
2. Invoices API
3. Record Payment
4. Invoice status calculation
5. Generate Statement
6. Statement snapshot
```

## Phase 4: PDF / Email / Logs

```text
1. QuestPDF Invoice PDF
2. QuestPDF Statement PDF
3. S3 upload and download URL
4. Email sending
5. EmailLog
6. AuditLog
```

## Phase 5: Quartz / Redis

```text
1. GenerateOrdersFromStandingOrdersJob
2. UpdateOverdueInvoicesJob
3. GenerateWeeklyStatementsJob
4. Redis dashboard cache
5. Redis rate limiting
6. Redis distributed lock
```

---

## 18. 验收标准

开发完成后，后端必须满足：

```text
Admin 可以创建和维护 Customer
Admin 可以创建和维护 Product
Admin / Customer 可以维护 Standing Order，权限正确
系统可以根据 Standing Order 自动生成 Order
系统不会重复生成同周期 Order
Admin 可以将 Generated Order 发送到生产
Production List 可以按产品汇总订单需求
生产完成后 Order 自动变 ReadyToShip
Admin 可以标记 Order 为 Shipped
Shipped Order 可以生成 Draft Invoice
Admin 可以生成 Invoice PDF
Admin 可以发送 Invoice Email
Customer 可以下载自己的 Invoice PDF
Admin 可以记录 Payment
Payment 可以正确更新 Invoice 状态
Paid Invoice 可以让相关 Order 变 Completed
系统可以生成 Statement 并保存快照
Admin 可以生成和发送 Statement PDF
Customer 可以下载自己的 Statement PDF
所有关键操作写入 AuditLog
所有邮件发送写入 EmailLog
Redis cache / lock / rate limiting 正常工作
Customer 无法访问其他客户数据
Customer 无法访问 Admin API
```

---

## 19. 给开发人员的实现说明

```text
Please implement the backend and database for the StoryCoffee B2B system based on this Backend & Database Spec.

The current frontend PRD and prototype define the product workflow, pages, roles and mock data. This document defines the real backend behaviour.

Use ASP.NET Core Web API, EF Core, PostgreSQL, JWT authentication, RBAC, Quartz.NET, Redis, QuestPDF, S3-compatible storage, Email service, EmailLog and AuditLog.

PostgreSQL is the source of truth for business data. Redis must only be used for cache, rate limiting, job locks and temporary state.

All key financial and workflow actions must be transactional where possible and must create AuditLog entries. Email sending must create EmailLog entries whether it succeeds or fails.

Customer APIs must enforce customer-level data isolation on the backend. Do not rely on frontend route protection only.
```
