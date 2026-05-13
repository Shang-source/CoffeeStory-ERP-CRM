# StoryCoffee B2B 订单与发票管理系统 PRD

**版本：** 1.1  
**日期：** 2026-05-08  
**作者：** Product Team  
**状态：** 待开发

---

## 🔄 前后端对齐说明（v1.1 更新）

本版本已完成前后端枚举类型对齐，确保前端与后端 API 契约一致。

### 主要变更

#### 1. OrderStatus 精简
**移除状态：**
- ❌ `'Invoiced'` - 改用 `invoice_status` 字段表达开票进度
- ❌ `'NeedsReview'` - 第一版不实现，未来如需要需补充业务规则

**最终定义：**
```typescript
type OrderStatus = 'Generated' | 'InProduction' | 'ReadyToShip' 
                 | 'Shipped' | 'Completed' | 'Cancelled';
```

#### 2. EmailStatus 补充
**新增状态：**
- ✅ `'Pending'` - 邮件发送中或等待发送

**最终定义：**
```typescript
type EmailStatus = 'NotSent' | 'Pending' | 'Sent' | 'Failed' | 'Bounced';
```

#### 3. InvoiceStatus 保持完整
**包含 'Issued' 状态：**
```typescript
type InvoiceStatus = 'NotIssued' | 'Draft' | 'Issued' | 'Unpaid' 
                   | 'PartiallyPaid' | 'Paid' | 'Overdue' | 'Cancelled';
```
- `Draft`: 发票创建，PDF 未生成
- `Issued`: PDF 已生成，但未发送给客户
- `Unpaid`: 已发送给客户，等待付款

#### 4. ProductionBatch 第一版策略
**后端设计：** 保留 `production_batches` 表用于批次管理  
**前端第一版：** 隐藏批次概念，调用 `GET /api/admin/production/current` 获取扁平化数据  
**第二版计划：** 前端增加批次选择器展示历史批次

### 对应修改文件
- ✅ `src/entities/types.ts` - 枚举类型定义
- ✅ `src/shared/status/statusFormat.ts` - 格式化函数和颜色映射
- ✅ `PRD-StoryCoffee-B2B系统.md` - 本文档
- 📘 参考：`Backend-Spec-StoryCoffee-B2B系统.md` 后端规格文档

---

## 目录

1. [产品概述](#1-产品概述)
2. [系统架构](#2-系统架构)
3. [数据模型](#3-数据模型)
4. [页面功能详述](#4-页面功能详述)
5. [状态流转逻辑](#5-状态流转逻辑)
6. [交互规范](#6-交互规范)
7. [技术规范](#7-技术规范)

---

## 1. 产品概述

### 1.1 产品定位

StoryCoffee B2B 订单与发票管理系统是一个面向咖啡批发商的订单管理平台，支持客户通过固定订购清单（Standing Order）自动生成订单，管理员通过后台管理生产、出货、开票和收款的完整业务流程。

### 1.2 核心业务流程

```
客户维护 Standing Order（固定订购清单）
    ↓
系统按周期自动生成订单（Order）
    ↓
Admin 在 Orders 页面查看订单并发送至生产
    ↓
Production List 页面管理产品生产进度
    ↓
生产完成后订单状态变为 Ready to Ship
    ↓
Admin 标记订单为 Shipped（已出货）
    ↓
系统生成并发送 Invoice（发票）
    ↓
客户通过银行转账付款（线下）
    ↓
Admin 在 Payments 页面记录付款
    ↓
Invoice 状态更新为 Paid
    ↓
未付款 Invoice 自动汇总成 Statement（账单汇总）
```

### 1.3 用户角色

#### 1.3.1 Admin（管理员）
- 查看所有客户订单
- 管理生产进度
- 处理出货
- 生成和发送发票
- 记录客户付款
- 生成和发送对账单

#### 1.3.2 Customer（客户）
- 维护 Standing Order（固定订购清单）
- 查看自己的订单
- 查看和下载发票
- 查看和下载对账单
- 管理账户设置

---

## 2. 系统架构

### 2.1 技术栈

- **前端框架：** React 18.3.1 + TypeScript
- **路由：** React Router 7.13.0
- **UI 组件库：** Material-UI 7.3.5
- **状态管理：** React Hooks (useState, useEffect)
- **样式：** Tailwind CSS 4.1.12
- **通知：** Sonner 2.0.3
- **表单：** React Hook Form 7.55.0

### 2.2 路由结构

```
/                           登录页面
/customer                   客户端主页（Dashboard）
/customer/standing-order    固定订购清单
/customer/orders            客户订单列表
/customer/invoices          客户发票列表
/customer/statements        客户对账单列表
/customer/settings          账户设置

/admin                      管理端主页（Dashboard）
/admin/customers            客户管理
/admin/customers/:id        客户详情
/admin/products             产品管理
/admin/standing-orders      固定订购清单管理
/admin/orders               订单管理
/admin/production           生产清单
/admin/invoices             发票管理
/admin/payments             收款记录
/admin/statements           对账单管理
/admin/statements/:id       对账单详情
```

### 2.3 项目文件结构

```
src/
├── app/
│   ├── components/
│   │   ├── ui/                      # Radix UI 组件
│   │   ├── Layout.tsx               # 根布局
│   │   ├── AdminLayout.tsx          # 管理端布局
│   │   ├── CustomerLayout.tsx       # 客户端布局
│   │   ├── CreateCustomerDialog.tsx # 创建客户对话框
│   │   └── EditCustomerDialog.tsx   # 编辑客户对话框
│   ├── pages/
│   │   ├── admin/                   # 管理端页面
│   │   │   ├── Dashboard.tsx
│   │   │   ├── Customers.tsx
│   │   │   ├── CustomerDetail.tsx
│   │   │   ├── Products.tsx
│   │   │   ├── StandingOrders.tsx
│   │   │   ├── Orders.tsx
│   │   │   ├── ProductionList.tsx
│   │   │   ├── Invoices.tsx
│   │   │   ├── Payments.tsx
│   │   │   ├── Statements.tsx
│   │   │   └── StatementDetail.tsx
│   │   ├── customer/                # 客户端页面
│   │   │   ├── Dashboard.tsx
│   │   │   ├── StandingOrder.tsx
│   │   │   ├── Orders.tsx
│   │   │   ├── Invoices.tsx
│   │   │   ├── Statements.tsx
│   │   │   └── AccountSettings.tsx
│   │   ├── Login.tsx
│   │   └── NotFound.tsx
│   ├── context/
│   │   └── AuthContext.tsx          # 认证上下文
│   ├── utils/
│   │   └── dataFilter.ts            # 数据过滤工具
│   ├── data/
│   │   └── mockData.ts              # 模拟数据
│   ├── types.ts                     # TypeScript 类型定义
│   ├── routes.tsx                   # 路由配置
│   └── App.tsx                      # 应用入口
└── styles/
    ├── theme.css                    # 主题样式
    └── fonts.css                    # 字体样式
```

---

## 3. 数据模型

### 3.1 类型定义（TypeScript）

#### 3.1.1 基础类型

```typescript
// 用户角色
type UserRole = 'Customer' | 'Admin';

// 账户状态
type AccountStatus = 'Draft' | 'Invited' | 'Active' | 'Suspended' | 'Archived';

// 订单频率
type OrderFrequency = 'Weekly' | 'Fortnightly' | 'Monthly' | 'ManualOnly';

// 固定订单状态
type StandingOrderStatus = 'Active' | 'Paused' | 'Cancelled';

// 订单状态
type OrderStatus = 
  | 'Generated'       // 已生成
  | 'InProduction'    // 生产中
  | 'ReadyToShip'     // 待出货
  | 'Shipped'         // 已出货
  | 'Completed'       // 已完成
  | 'Cancelled';      // 已取消

// 发票状态
type InvoiceStatus = 
  | 'NotIssued'       // 未开票
  | 'Draft'           // 草稿
  | 'Issued'          // 已开具
  | 'Unpaid'          // 未付款
  | 'PartiallyPaid'   // 部分付款
  | 'Paid'            // 已付款
  | 'Overdue'         // 逾期
  | 'Cancelled';      // 已取消

// 出货状态
type ShipmentStatus = 
  | 'NotShipped'      // 未出货
  | 'ReadyToShip'     // 待出货
  | 'Shipped'         // 已出货
  | 'Delivered';      // 已送达

// 对账单状态
type StatementStatus = 
  | 'Draft'           // 草稿
  | 'ReadyToSend'     // 待发送
  | 'Sent'            // 已发送
  | 'Cancelled';      // 已取消

// 邮件状态
type EmailStatus = 
  | 'NotSent'         // 未发送
  | 'Pending'         // 发送中
  | 'Sent'            // 已发送
  | 'Failed'          // 失败
  | 'Bounced';        // 退回

// 生产状态
type ProductionStatus = 
  | 'Pending'         // 待生产
  | 'InProgress'      // 生产中
  | 'Completed'       // 已完成
  | 'OnHold';         // 暂停
```

#### 3.1.2 实体模型

```typescript
// 客户
interface Customer {
  id: string;
  businessName: string;          // 公司名称
  contactPerson: string;          // 联系人
  email: string;                  // 邮箱
  phone: string;                  // 电话
  billingAddress: string;         // 账单地址
  deliveryAddress: string;        // 送货地址
  paymentTerms: string;           // 付款条款（如 "7 days", "14 days"）
  accountStatus: AccountStatus;   // 账户状态
  createdAt: Date;                // 创建时间
}

// 产品
interface Product {
  id: string;
  sku: string;                    // 产品编码
  name: string;                   // 产品名称
  description: string;            // 描述
  unit: string;                   // 单位（kg, g）
  price: number;                  // 售价
  cost: number;                   // 成本
  isActive: boolean;              // 是否启用
}

// 固定订单项
interface StandingOrderItem {
  id: string;
  productId: string;
  product: Product;
  quantity: number;               // 数量
  unitPrice: number;              // 单价
  notes?: string;                 // 备注
}

// 固定订单
interface StandingOrder {
  id: string;
  customerId: string;
  customer?: Customer;
  frequency: OrderFrequency;      // 订单频率
  nextClosingDate: Date;          // 下次截单日期
  status: StandingOrderStatus;    // 状态
  deliveryNotes?: string;         // 配送备注
  internalNotes?: string;         // 内部备注
  items: StandingOrderItem[];     // 订单项
}

// 订单项
interface OrderItem {
  id: string;
  productId: string;
  productNameSnapshot: string;    // 产品名称快照
  skuSnapshot: string;            // SKU 快照
  quantity: number;               // 数量
  unitPriceSnapshot: number;      // 单价快照
  lineTotal: number;              // 小计
  notes?: string;                 // 备注
}

// 订单
interface Order {
  id: string;
  orderNumber: string;            // 订单号
  customerId: string;
  customer?: Customer;
  standingOrderId: string;        // 关联的固定订单 ID
  generatedAt: Date;              // 生成时间
  orderStatus: OrderStatus;       // 订单状态
  invoiceStatus: InvoiceStatus;   // 发票状态
  shipmentStatus: ShipmentStatus; // 出货状态
  subtotal: number;               // 小计
  gstAmount: number;              // GST 税额（15%）
  totalAmount: number;            // 总计
  items: OrderItem[];             // 订单项
}

// 发票项
interface InvoiceItem {
  id: string;
  description: string;            // 描述
  quantity: number;               // 数量
  unitPrice: number;              // 单价
  lineTotal: number;              // 小计
}

// 发票
interface Invoice {
  id: string;
  invoiceNumber: string;          // 发票号
  customerId: string;
  customer?: Customer;
  orderId: string;                // 关联订单 ID
  issueDate: Date;                // 开具日期
  dueDate: Date;                  // 到期日期
  subtotal: number;               // 小计
  gstAmount: number;              // GST 税额
  totalAmount: number;            // 总计
  paidAmount: number;             // 已付金额
  outstandingAmount: number;      // 未付金额
  status: InvoiceStatus;          // 状态
  items: InvoiceItem[];           // 发票项
}

// 付款记录
interface PaymentRecord {
  id: string;
  invoiceId: string;              // 关联发票 ID
  amount: number;                 // 付款金额
  paymentDate: Date;              // 付款日期
  paymentMethod: string;          // 付款方式
  reference: string;              // 参考号
  markedBy: string;               // 记录人
  note?: string;                  // 备注
}

// 对账单
interface Statement {
  id: string;
  statementNumber: string;        // 对账单号
  customerId: string;
  customer?: Customer;
  statementDate: Date;            // 对账单日期
  periodStart?: Date;             // 账期开始
  periodEnd?: Date;               // 账期结束
  totalOutstanding: number;       // 总未付金额
  status: StatementStatus;        // 状态
  emailStatus: EmailStatus;       // 邮件状态
  invoices: Invoice[];            // 包含的发票
}

// 生产项
interface ProductionItem {
  productId: string;
  productName: string;            // 产品名称
  sku: string;                    // SKU
  totalQuantity: number;          // 总需求量
  producedQuantity: number;       // 已生产量
  status: ProductionStatus;       // 生产状态
  orderIds: string[];             // 关联订单 ID 列表
  orderNumbers: string[];         // 关联订单号列表
}
```

---

## 4. 页面功能详述

### 4.1 登录页面（/）

#### 4.1.1 页面布局

- 居中的登录卡片
- Logo 和标题 "StoryCoffee"
- 用户名输入框
- 密码输入框
- 登录按钮
- 演示账号快捷登录按钮

#### 4.1.2 演示账号

```typescript
// Admin 账号
{
  email: 'admin@storycoffee.co.nz',
  password: 'admin123',
  role: 'Admin'
}

// Customer 账号
{
  email: 'john@aucklandcafe.co.nz',
  password: 'customer123',
  role: 'Customer',
  customerId: 'c1'
}
```

#### 4.1.3 交互逻辑

1. 用户输入账号密码或点击演示账号按钮
2. 验证通过后：
   - 保存用户信息到 AuthContext
   - 根据角色跳转：
     - Admin → `/admin`
     - Customer → `/customer`

---

### 4.2 Admin Dashboard（/admin）

#### 4.2.1 页面结构

```
┌─────────────────────────────────────────────────────────┐
│ Admin Dashboard                                          │
│ Overview of your business operations                    │
├─────────────────────────────────────────────────────────┤
│ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐   │
│ │This Week │ │In Product│ │ Shipped  │ │Amount Due│   │
│ │    3     │ │    5     │ │    2     │ │  $1,234  │   │
│ │Orders    │ │Orders    │ │This Week │ │5 Invoices│   │
│ └──────────┘ └──────────┘ └──────────┘ └──────────┘   │
├─────────────────────────────────────────────────────────┤
│ Recent Orders                    │ Overdue Invoices    │
│ ┌──────────────────────────────┐ │ ┌─────────────────┐│
│ │Order# Customer  Date   Amount│ │ │Auckland Cafe    ││
│ │ORD-001 ...     ...    $323.15│ │ │INV-002 - $323.15││
│ └──────────────────────────────┘ │ └─────────────────┘│
│                                  │ Active Customers   │
│                                  │ ┌─────────────────┐│
│                                  │ │       3         ││
│                                  │ │Total: 3         ││
│                                  │ └─────────────────┘│
└─────────────────────────────────────────────────────────┘
```

#### 4.2.2 统计卡片

**卡片 1：This Week**
- 数据：本周生成的订单数量
- 筛选条件：`order.generatedAt >= 本周一`
- 图标：TrendingUp（蓝色）
- 点击行为：跳转到 `/admin/orders`

**卡片 2：In Production**
- 数据：生产中的订单数量
- 筛选条件：`order.orderStatus === 'InProduction'`
- 图标：ShoppingCart（橙色）
- 点击行为：跳转到 `/admin/orders`

**卡片 3：Shipped**
- 数据：本周已出货的订单数量
- 筛选条件：`order.orderStatus === 'Shipped' && order.generatedAt >= 本周一`
- 图标：LocalShipping（绿色）
- 点击行为：跳转到 `/admin/orders`

**卡片 4：Amount Due**
- 数据：所有未付款发票的总金额
- 筛选条件：`invoice.status !== 'Paid'`
- 计算：`sum(invoice.outstandingAmount)`
- 图标：Warning（红色）
- 点击行为：跳转到 `/admin/invoices`

#### 4.2.3 Recent Orders 表格

**字段：**
- Order #（订单号）- 加粗显示
- Customer（客户名称）- 可点击链接
- Date（生成日期）
- Amount（总金额）- 右对齐
- Status（订单状态）- Chip 组件

**交互：**
- 整行点击：跳转到 `/admin/orders`
- 客户名称点击：跳转到 `/admin/customers/:id`（阻止行点击事件）
- 悬停效果：背景色变为 `#f5f5f5`

**数据：**
- 显示最新 5 条订单
- 排序：按 `generatedAt` 降序

#### 4.2.4 Overdue Invoices 列表

**显示内容：**
- 客户名称
- 发票号 - 未付金额

**筛选条件：**
- `invoice.status === 'Overdue'`

**交互：**
- 点击项：跳转到 `/admin/invoices`
- 悬停效果：背景色变为 `#f5f5f5`

**空状态：**
- 当没有逾期发票时显示："No overdue invoices"

#### 4.2.5 Active Customers 卡片

**显示：**
- 大号数字：活跃客户数量
- 小字：总客户数量

**筛选条件：**
- 活跃：`customer.accountStatus === 'Active'`
- 总数：所有客户

**交互：**
- 点击卡片：跳转到 `/admin/customers`

---

### 4.3 Admin Orders 页面（/admin/orders）

#### 4.3.1 页面布局

```
┌─────────────────────────────────────────────────────────┐
│ Orders                      [Send All to Production (3)]│
│ Manage customer orders...                                │
├─────────────────────────────────────────────────────────┤
│ ℹ You have 3 orders ready to be sent to production.     │
│   Click "Send All to Production" to batch process them. │
├─────────────────────────────────────────────────────────┤
│ ┌──────────┐ ┌──────────┐ ┌──────────┐                 │
│ │Generated │ │In Product│ │Ready Ship│                 │
│ │    3     │ │    5     │ │    2     │                 │
│ └──────────┘ └──────────┘ └──────────┘                 │
├─────────────────────────────────────────────────────────┤
│ [▼] Order#  Customer  Date   Total  OrderSts InvoiceSts │
│ [ ] ORD-001 Auckland  ...    $323   ...      ...        │
│ [▶] ORD-002 ...       ...    ...    ...      ...    [⋮] │
└─────────────────────────────────────────────────────────┘
```

#### 4.3.2 顶部操作栏

**批量操作按钮：Send All to Production**
- 位置：右上角
- 样式：`variant="contained"`, `color="primary"`, `size="large"`
- 图标：`<PlayArrow />`
- 文本：显示待处理订单数量，如 "Send All to Production (3)"
- 禁用条件：当没有 Generated 状态的订单时
- 点击行为：见 [4.3.6 批量操作逻辑](#436-批量操作逻辑)

#### 4.3.3 提醒横幅

**显示条件：**
- 当存在 Generated 状态的订单时显示

**内容：**
```
ℹ You have X orders ready to be sent to production.
  Click "Send All to Production" to batch process them.
```

**样式：**
- `Alert` 组件
- `severity="info"`（蓝色）
- 底部外边距：`sx={{ mb: 3 }}`

#### 4.3.4 统计卡片

三个并排的统计卡片：

**Generated（已生成）**
- 计算：`orders.filter(o => o.orderStatus === 'Generated').length`
- 显示：大号数字

**In Production（生产中）**
- 计算：`orders.filter(o => o.orderStatus === 'InProduction').length`
- 显示：大号数字

**Ready to Ship（待出货）**
- 计算：`orders.filter(o => o.orderStatus === 'ReadyToShip').length`
- 显示：大号数字

#### 4.3.5 订单表格

**表头字段：**
1. 展开/收起图标列
2. Order #（订单号）
3. Customer（客户）
4. Generated Date（生成日期）
5. Total（总金额）
6. Order Status（订单状态）
7. Invoice Status（发票状态）
8. Shipment Status（出货状态）
9. Actions（操作）

**每行显示：**

```typescript
// Order # - 加粗显示
<Typography variant="body2" sx={{ fontWeight: 500 }}>
  {order.orderNumber}
</Typography>

// Customer - 文本按钮，可点击
<Button variant="text" size="small" onClick={跳转到客户详情}>
  {order.customer?.businessName}
</Button>

// Generated Date - 格式化日期
{order.generatedAt.toLocaleDateString()}

// Total - 右对齐，货币格式
${order.totalAmount.toFixed(2)}

// Order Status - Chip 组件
<Chip 
  label={formatOrderStatus(order.orderStatus)}
  size="small"
  sx={{ bgcolor: getOrderStatusColor(order.orderStatus), color: 'white' }}
/>

// Invoice Status - Chip 组件
<Chip 
  label={formatInvoiceStatus(order.invoiceStatus)}
  size="small"
  sx={{ bgcolor: getInvoiceStatusColor(order.invoiceStatus), color: 'white' }}
/>

// Shipment Status - Chip 组件
<Chip 
  label={formatShipmentStatus(order.shipmentStatus)}
  size="small"
  sx={{ bgcolor: getShipmentStatusColor(order.shipmentStatus), color: 'white' }}
/>

// Actions - 三点菜单
<IconButton size="small" onClick={打开菜单}>
  <MoreVert />
</IconButton>
```

**展开行（Order Items）：**

点击左侧展开图标后显示订单详情：

```
┌──────────────────────────────────────────┐
│ Order Items                               │
│ ┌────────────────────────────────────┐   │
│ │Product    SKU    Qty  Price  Total │   │
│ │House B... HB-1KG  5   $45.00 $225  │   │
│ │Decaf 500g DC-500G 2   $28.00 $56   │   │
│ └────────────────────────────────────┘   │
└──────────────────────────────────────────┘
```

**字段：**
- Product（产品名称）- productNameSnapshot
- SKU - skuSnapshot
- Quantity（数量）- 右对齐
- Unit Price（单价）- 右对齐，货币格式
- Total（小计）- 右对齐，货币格式

#### 4.3.6 Actions 菜单逻辑

**菜单项根据订单状态动态显示：**

```typescript
// 所有订单都显示
- View Customer（查看客户）→ 跳转 /admin/customers/:id

// orderStatus === 'Generated'
- Send to Production（发送至生产）→ 更新状态为 InProduction
- View Production List（查看生产清单）→ 跳转 /admin/production

// orderStatus === 'InProduction'
- View Production List（查看生产清单）→ 跳转 /admin/production
- Mark as Shipped（标记为已出货）→ 更新多个状态

// orderStatus === 'ReadyToShip'
- Mark as Shipped（标记为已出货）→ 更新多个状态

// invoiceStatus === 'NotIssued' && orderStatus === 'Shipped'
- Generate Invoice（生成发票）→ 更新 invoiceStatus 为 Draft

// invoiceStatus === 'Draft'
- Send Invoice（发送发票）→ 更新 invoiceStatus 为 Unpaid
- View Invoice（查看发票）→ 跳转 /admin/invoices

// invoiceStatus === 'Unpaid' | 'Overdue' | 'PartiallyPaid'
- View Invoice（查看发票）→ 跳转 /admin/invoices
- Record Payment（记录付款）→ 跳转 /admin/payments

// invoiceStatus === 'Paid'
- View Invoice（查看发票）→ 跳转 /admin/invoices

// orderStatus !== 'Cancelled' && orderStatus !== 'Completed'
- Cancel Order（取消订单）→ 更新 orderStatus 为 Cancelled
```

**状态更新详细逻辑：**

**Send to Production：**
```typescript
{
  orderStatus: 'InProduction'
}
// Toast: "Order sent to production"
```

**Mark as Shipped：**
```typescript
{
  orderStatus: 'Shipped',
  shipmentStatus: 'Shipped',
  invoiceStatus: invoiceStatus === 'NotIssued' ? 'Draft' : invoiceStatus
}
// Toast: "Order marked as shipped. Invoice created as draft."
```

**Generate Invoice：**
```typescript
{
  invoiceStatus: 'Draft'
}
// Toast: "Invoice generated as draft"
```

**Send Invoice：**
```typescript
{
  invoiceStatus: 'Unpaid'
}
// Toast: "Invoice sent to customer"
```

**Cancel Order：**
```typescript
{
  orderStatus: 'Cancelled'
}
// Toast: "Order cancelled"
```

#### 4.3.7 批量操作逻辑

**触发：** 点击 "Send All to Production" 按钮

**步骤：**

1. 筛选符合条件的订单
```typescript
const generatedOrders = orders.filter(
  order => order.orderStatus === 'Generated'
);
```

2. 检查是否有订单
```typescript
if (generatedOrders.length === 0) {
  toast.info('No orders available to send to production');
  return;
}
```

3. 批量更新状态
```typescript
setOrders(prevOrders =>
  prevOrders.map(order =>
    order.orderStatus === 'Generated'
      ? { ...order, orderStatus: 'InProduction' }
      : order
  )
);
```

4. 显示成功通知
```typescript
toast.success(
  `${generatedOrders.length} order${generatedOrders.length > 1 ? 's' : ''} sent to production successfully`,
  {
    description: `These orders have been added to the Production List and are now in production.`
  }
);
```

5. 1.5 秒后显示跟进通知
```typescript
setTimeout(() => {
  toast.info('View Production List to track progress', {
    action: {
      label: 'Go to Production',
      onClick: () => navigate('/admin/production')
    }
  });
}, 1500);
```

6. 页面状态自动更新
- Generated 统计卡片数字减少
- In Production 统计卡片数字增加
- 提醒横幅自动隐藏（如果没有剩余 Generated 订单）
- 批量按钮变为禁用状态

---

### 4.4 Admin Production List 页面（/admin/production）

#### 4.4.1 页面概述

Production List 是生产管理页面，用于：
- 将多个订单的产品需求汇总
- 管理每个产品的生产进度
- 跟踪已生产数量
- 当所有产品完成后自动将订单状态更新为 Ready to Ship

#### 4.4.2 页面布局

```
┌─────────────────────────────────────────────────────────┐
│ Production List                      [Print] [Export CSV]│
│ This page summarizes generated orders into product...   │
├─────────────────────────────────────────────────────────┤
│ Production Period                                        │
│ Current week - 3 product line(s) to produce             │
├─────────────────────────────────────────────────────────┤
│ Production Summary by Product                            │
│ Product    SKU    Total Produced Status  Orders  Actions│
│ House B... HB-1KG  10   6/10     InProg  ORD... [Update]│
│ Brazil E...BR-1KG  3    3/3      Complet ORD... ✓       │
│ Filter B...FB-250G 4    0/4      Pending ORD... [Start] │
└─────────────────────────────────────────────────────────┘
```

#### 4.4.3 顶部说明文字

```
Production List
This page summarizes generated orders into product quantities for production.
```

**作用：** 向用户解释此页面的功能

#### 4.4.4 顶部操作按钮

**Print 按钮：**
- 文本：Print
- 图标：`<Print />`
- 样式：`variant="outlined"`
- 点击：`toast.success('Printing production list')`

**Export CSV 按钮：**
- 文本：Export CSV
- 图标：`<Download />`
- 样式：`variant="outlined"`
- 点击：`toast.success('Exporting production list to CSV')`

#### 4.4.5 Production Period 卡片

显示当前生产周期的信息：

```
Production Period
Current week - X product line(s) to produce
```

**X 的计算：** 统计当前生产项的数量

#### 4.4.6 Production Summary 表格

**表头：**
1. Product（产品名称）
2. SKU
3. Total Quantity（总需求量）
4. Produced Quantity（已生产量）
5. Production Status（生产状态）
6. Related Orders（关联订单）
7. Actions（操作）

**数据来源逻辑：**

```typescript
// 1. 筛选需要生产的订单
const productionOrders = mockOrders.filter(
  order => 
    order.orderStatus === 'Generated' || 
    order.orderStatus === 'InProduction' ||
    order.orderStatus === 'ReadyToShip'
);

// 2. 按产品汇总
const productionItems: ProductionItem[] = [];

productionOrders.forEach(order => {
  order.items.forEach(item => {
    const existing = productionItems.find(p => p.productId === item.productId);
    if (existing) {
      // 累加数量
      existing.totalQuantity += item.quantity;
      existing.orderIds.push(order.id);
      existing.orderNumbers.push(order.orderNumber);
    } else {
      // 创建新项
      productionItems.push({
        productId: item.productId,
        productName: item.productNameSnapshot,
        sku: item.skuSnapshot,
        totalQuantity: item.quantity,
        producedQuantity: 0,
        status: 'Pending',
        orderIds: [order.id],
        orderNumbers: [order.orderNumber],
      });
    }
  });
});
```

**字段显示：**

**Product & SKU：**
```tsx
<TableCell>{item.productName}</TableCell>
<TableCell>{item.sku}</TableCell>
```

**Total Quantity：**
```tsx
<Chip
  label={item.totalQuantity}
  color="primary"
  sx={{ minWidth: 60 }}
/>
```

**Produced Quantity：**
```tsx
<Chip
  label={`${item.producedQuantity} / ${item.totalQuantity}`}
  color={item.producedQuantity === item.totalQuantity ? 'success' : 'default'}
  sx={{ minWidth: 80 }}
/>
```

**Production Status：**
```tsx
<Chip
  label={formatProductionStatus(item.status)}
  size="small"
  sx={{ 
    bgcolor: getProductionStatusColor(item.status), 
    color: 'white' 
  }}
/>
```

**状态颜色映射：**
```typescript
const colors = {
  Pending: '#9E9E9E',      // 灰色
  InProgress: '#FF9800',   // 橙色
  Completed: '#4CAF50',    // 绿色
  OnHold: '#F44336',       // 红色
};
```

**Related Orders：**
```tsx
<Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap' }}>
  {item.orderNumbers.map((orderNum, idx) => (
    <Chip
      key={idx}
      label={orderNum}
      size="small"
      variant="outlined"
    />
  ))}
</Box>
```

#### 4.4.7 Actions 按钮逻辑

**根据 Production Status 动态显示：**

**status === 'Pending'：**
```tsx
<Button
  size="small"
  variant="contained"
  color="primary"
  startIcon={<PlayArrow />}
  onClick={handleStartProduction}
>
  Start
</Button>
```

**status === 'InProgress'：**
```tsx
<Box sx={{ display: 'flex', gap: 1 }}>
  <Button
    size="small"
    variant="outlined"
    startIcon={<Edit />}
    onClick={handleUpdateQuantity}
  >
    Update
  </Button>
  <Button
    size="small"
    variant="contained"
    color="success"
    startIcon={<CheckCircle />}
    onClick={handleMarkCompleted}
  >
    Complete
  </Button>
</Box>
```

**status === 'Completed'：**
```tsx
<Chip label="Completed" color="success" size="small" />
```

**status === 'OnHold'：**
```tsx
<Button
  size="small"
  variant="outlined"
  color="warning"
  onClick={handleStartProduction}
>
  Resume
</Button>
```

#### 4.4.8 操作行为详解

**Start Production（开始生产）**

触发：点击 Start 按钮

逻辑：
```typescript
setProductionItems(prev =>
  prev.map(p =>
    p.productId === item.productId
      ? { ...p, status: 'InProgress' }
      : p
  )
);
toast.success(`Started production for ${item.productName}`);
toast.info('Related orders updated to In Production');
```

实际系统行为：
- 更新 Production Item 状态为 InProgress
- 更新所有关联订单的 orderStatus 为 InProduction

---

**Update Quantity（更新已生产数量）**

触发：点击 Update 按钮

流程：
1. 打开对话框
```tsx
<Dialog open={updateDialog} onClose={关闭} maxWidth="sm" fullWidth>
  <DialogTitle>Update Produced Quantity</DialogTitle>
  <DialogContent>
    <Typography>Product: {selectedItem.productName}</Typography>
    <Typography>Total Quantity Required: {selectedItem.totalQuantity}</Typography>
    
    <TextField
      label="Produced Quantity"
      type="number"
      fullWidth
      value={updateQuantity}
      onChange={更新输入值}
      inputProps={{ min: 0, max: selectedItem.totalQuantity }}
      helperText={`Enter a value between 0 and ${selectedItem.totalQuantity}`}
    />
  </DialogContent>
  <DialogActions>
    <Button onClick={关闭}>Cancel</Button>
    <Button onClick={保存} variant="contained">
      Update Quantity
    </Button>
  </DialogActions>
</Dialog>
```

2. 验证输入
```typescript
const newQuantity = parseInt(updateQuantity);
if (isNaN(newQuantity) || newQuantity < 0) {
  toast.error('Please enter a valid quantity');
  return;
}
if (newQuantity > selectedItem.totalQuantity) {
  toast.error('Produced quantity cannot exceed total quantity');
  return;
}
```

3. 更新状态
```typescript
setProductionItems(prev =>
  prev.map(p =>
    p.productId === selectedItem.productId
      ? {
          ...p,
          producedQuantity: newQuantity,
          status: newQuantity === p.totalQuantity ? 'Completed' : p.status
        }
      : p
  )
);
toast.success(`Updated produced quantity for ${selectedItem.productName}`);
```

**自动完成逻辑：**
- 如果 `producedQuantity === totalQuantity`，自动将 status 设为 Completed

---

**Mark as Completed（标记为完成）**

触发：点击 Complete 按钮

逻辑：
```typescript
setProductionItems(prev =>
  prev.map(p =>
    p.productId === item.productId
      ? {
          ...p,
          producedQuantity: p.totalQuantity,  // 直接设为总量
          status: 'Completed'
        }
      : p
  )
);
toast.success(`${item.productName} marked as completed`);
toast.info('Checking if related orders are ready to ship...');
```

实际系统行为：
- 更新 Production Item 为 Completed
- 检查该产品关联的所有订单
- 对于每个订单，检查其所有产品是否都已 Completed
- 如果是，将订单状态更新为 ReadyToShip

---

**Resume（恢复生产）**

触发：点击 Resume 按钮

逻辑：
```typescript
setProductionItems(prev =>
  prev.map(p =>
    p.productId === item.productId
      ? { ...p, status: 'InProgress' }
      : p
  )
);
toast.success(`${item.productName} production resumed`);
```

---

#### 4.4.9 与 Orders 页面的联动

**订单 → Production List**

当在 Orders 页面执行 "Send to Production"：
1. 订单的 orderStatus 变为 InProduction
2. Production List 自动包含该订单的产品
3. 如果产品已存在，累加数量

**Production List → 订单**

当在 Production List 标记产品为 Completed：
1. 系统检查关联的订单
2. 对于每个订单，检查该订单的所有产品是否都 Completed
3. 如果订单的所有产品都完成，自动更新订单状态：
```typescript
{
  orderStatus: 'ReadyToShip',
  shipmentStatus: 'ReadyToShip'
}
```

**示例：**

```
订单 ORD-001 包含：
- House Blend 1kg x 5
- Decaf 500g x 2

Production List 显示：
- House Blend 1kg: 10 (来自多个订单)
- Decaf 500g: 4 (来自多个订单)

当 House Blend 标记为 Completed：
→ 检查 ORD-001，发现 Decaf 还未完成
→ ORD-001 保持 InProduction 状态

当 Decaf 也标记为 Completed：
→ 再次检查 ORD-001，所有产品都完成了
→ ORD-001 自动更新为 ReadyToShip
→ Orders 页面的 Ready to Ship 统计数字增加
```

---

### 4.5 Admin Invoices 页面（/admin/invoices）

---

### 4.5 Admin Invoices 页面（/admin/invoices）

#### 4.5.1 页面布局

```
┌─────────────────────────────────────────────────────────┐
│ Invoices                                                 │
│ Manage customer invoices                                │
├─────────────────────────────────────────────────────────┤
│ [▼] Invoice# Customer   Issue   Due    Total  AmtDue Sts│
│ [ ] INV-001  Auckland   ...     ...    $323   $323   ... │
│     └─ Invoice Items                                     │
│        Product        Qty  Price  Total                  │
│        House Blend... 5    $45    $225                   │
└─────────────────────────────────────────────────────────┘
```

#### 4.5.2 表格字段

**表头：**
1. 展开/收起图标列
2. Invoice #（发票号）
3. Customer（客户）
4. Issue Date（开具日期）
5. Due Date（到期日期）
6. Total（总金额）
7. Amount Due（未付金额）
8. Status（状态）
9. Actions（操作）

#### 4.5.3 每行显示

```typescript
// Invoice #
{invoice.invoiceNumber}

// Customer
{invoice.customer?.businessName}

// Issue Date
{invoice.issueDate.toLocaleDateString()}

// Due Date
{invoice.dueDate.toLocaleDateString()}

// Total - 右对齐
${invoice.totalAmount.toFixed(2)}

// Amount Due - 右对齐
${invoice.outstandingAmount.toFixed(2)}

// Status - Chip 组件
<Chip
  label={formatInvoiceStatus(invoice.status)}
  size="small"
  sx={{ bgcolor: getInvoiceStatusColor(invoice.status), color: 'white' }}
/>

// Actions - 操作图标
<IconButton size="small" onClick={发送邮件}>
  <Send />
</IconButton>
<IconButton size="small" onClick={下载PDF}>
  <Download />
</IconButton>
```

#### 4.5.4 展开行（Invoice Items）

点击左侧展开图标显示发票明细：

```
Invoice Items
┌──────────────────────────────────────┐
│Description        Qty  Price   Total │
│House Blend 1kg x5  5   $45.00  $225  │
│Decaf 500g x2       2   $28.00  $56   │
│                    Subtotal:   $281  │
│                    GST (15%):  $42.15│
│                    Total:      $323  │
└──────────────────────────────────────┘
```

#### 4.5.5 操作行为

**Send Email（发送邮件）**
```typescript
toast.success(`Invoice ${invoice.invoiceNumber} sent to ${invoice.customer?.email}`);
```

**Download PDF（下载PDF）**
```typescript
toast.success(`Downloading invoice ${invoice.invoiceNumber}`);
```

---

### 4.6 Admin Payments 页面（/admin/payments）

#### 4.6.1 页面概述

Payments 页面用于记录客户通过银行转账等线下方式支付的款项。

#### 4.6.2 页面布局

```
┌─────────────────────────────────────────────────────────┐
│ Payments                                                 │
│ Record and manage customer payments                     │
├─────────────────────────────────────────────────────────┤
│ Unpaid Invoices                                         │
│ Invoice# Customer  Issue  Due   AmtDue  Status  Actions │
│ INV-001  Auckland  ...    ...   $323    Unpaid  [Record]│
│ INV-002  Auckland  ...    ...   $323    Overdue [Record]│
└─────────────────────────────────────────────────────────┘
```

#### 4.6.3 表格字段

**表头：**
1. Invoice #（发票号）
2. Customer（客户）
3. Issue Date（开具日期）
4. Due Date（到期日期）
5. Amount Due（未付金额）
6. Payment Status（付款状态）
7. Actions（操作）

**筛选条件：**
```typescript
const unpaidInvoices = mockInvoices.filter(
  inv => inv.status !== 'Paid'
);
```

#### 4.6.4 Record Payment 对话框

**触发：** 点击 Record Payment 按钮

**对话框内容：**

```
┌────────────────────────────────────┐
│ Record Payment                     │
├────────────────────────────────────┤
│ Invoice: INV-001                   │
│ Customer: Auckland Cafe            │
│ Amount Due: $323.15                │
│                                    │
│ Payment Date: [2026-05-07____]     │
│ Amount Paid: [____________]        │
│ Payment Method: [Bank Transfer ▼]  │
│   - Bank Transfer                  │
│   - Cash                           │
│   - Cheque                         │
│   - Other                          │
│ Payment Reference: [____________]  │
│ Notes: [_______________________]   │
│        [_______________________]   │
│        [_______________________]   │
│                                    │
│        [Cancel]  [Record Payment]  │
└────────────────────────────────────┘
```

**字段说明：**

- **Invoice** - 只读，显示发票号
- **Customer** - 只读，显示客户名称
- **Amount Due** - 只读，显示未付金额
- **Payment Date** - 日期选择器，默认今天
- **Amount Paid** - 数字输入框，必填
- **Payment Method** - 下拉选择
  - Bank Transfer（银行转账）
  - Cash（现金）
  - Cheque（支票）
  - Other（其他）
- **Payment Reference** - 文本输入框，如交易号、支票号
- **Notes** - 多行文本框

#### 4.6.5 Record Payment 逻辑

**验证：**
```typescript
if (!paymentData.amount) {
  // 按钮禁用
  disabled={!paymentData.amount}
}
```

**保存：**
```typescript
// 1. 创建 PaymentRecord
const payment = {
  id: generateId(),
  invoiceId: selectedInvoice.id,
  amount: parseFloat(paymentData.amount),
  paymentDate: new Date(paymentData.date),
  paymentMethod: paymentData.paymentMethod,
  reference: paymentData.reference,
  markedBy: currentUser.name,
  note: paymentData.note
};

// 2. 更新 Invoice
const updatedInvoice = {
  ...selectedInvoice,
  paidAmount: selectedInvoice.paidAmount + payment.amount,
  outstandingAmount: selectedInvoice.outstandingAmount - payment.amount,
  status: calculateInvoiceStatus(selectedInvoice, payment.amount)
};

// 3. 计算新状态
function calculateInvoiceStatus(invoice, paidAmount) {
  const newOutstanding = invoice.outstandingAmount - paidAmount;
  if (newOutstanding <= 0) {
    return 'Paid';
  } else if (paidAmount > 0) {
    return 'PartiallyPaid';
  } else {
    return invoice.status;
  }
}

// 4. 显示成功消息
toast.success(`Payment of $${paymentData.amount} recorded for invoice ${selectedInvoice.invoiceNumber}`);
```

#### 4.6.6 系统联动（付款后）

当 Admin 记录付款后，系统需要自动更新：

**1. Invoice 更新**
```typescript
{
  paidAmount: paidAmount + newPayment,
  outstandingAmount: outstandingAmount - newPayment,
  status: newOutstanding === 0 ? 'Paid' : 'PartiallyPaid'
}
```

**2. Payments 页面更新**
- 如果发票已付清，从 Unpaid Invoices 列表中移除
- 列表自动刷新

**3. Admin Dashboard 更新**
- Amount Due 统计数字减少
- Overdue Invoices 列表更新

**4. Admin Invoices 页面更新**
- 发票状态更新为 Paid 或 PartiallyPaid
- Amount Due 金额更新

**5. Customer Dashboard 更新**
- Amount Due 统计减少
- Unpaid invoice count 减少
- Overdue warning 更新

**6. Customer Invoices 页面更新**
- 客户侧看到发票状态变为 Paid
- Amount Due 显示 $0

**7. Statement 更新**
- 已付清的 invoice 不再出现在新生成的 Statement 中
- 历史 Statement 保持不变（快照）

---

### 4.7 Admin Statements 页面（/admin/statements）

#### 4.7.1 页面概述

Statements 页面管理客户对账单，分为两层：
1. **Statements List** - 对账单列表
2. **Statement Detail** - 对账单详情

#### 4.7.2 Statements List 布局

```
┌─────────────────────────────────────────────────────────┐
│ Statements                  [Generate Weekly Statements]│
│ Manage customer account statements and send reminders   │
├─────────────────────────────────────────────────────────┤
│ Stmt#    Customer  Date   Period      AmtDue  Sts  Email│
│ STMT-001 Auckland  05/07  04/24-05/07 $646    Sent Sent │
│ STMT-002 Welling.. 05/07  04/24-05/07 $262    Draft - │
└─────────────────────────────────────────────────────────┘
```

#### 4.7.3 表格字段

**表头：**
1. Statement Number（对账单号）
2. Customer（客户）
3. Statement Date（对账单日期）
4. Period（账期）
5. Total Amount Due（总未付金额）
6. Status（状态）
7. Email Status（邮件状态）
8. Actions（操作）

#### 4.7.4 表格交互

**整行点击：**
- 跳转到 `/admin/statements/:id` 查看详情

**Actions 图标按钮：**

```tsx
<IconButton onClick={查看详情} title="View Details">
  <Visibility />
</IconButton>
<IconButton 
  onClick={发送邮件} 
  disabled={emailStatus === 'Sent'}
  title="Send Email"
>
  <Send />
</IconButton>
<IconButton onClick={下载PDF} title="Download PDF">
  <Download />
</IconButton>
```

**点击操作时需要：**
```typescript
event.stopPropagation(); // 阻止行点击事件
```

#### 4.7.5 Generate Weekly Statements 按钮

**位置：** 页面右上角

**点击逻辑：**

```typescript
// 1. 查找所有未付款发票
const unpaidInvoices = allInvoices.filter(
  inv => inv.status === 'Unpaid' || inv.status === 'Overdue'
);

// 2. 按客户分组
const groupedByCustomer = groupBy(unpaidInvoices, 'customerId');

// 3. 为每个客户生成 Statement
const newStatements = Object.entries(groupedByCustomer).map(([customerId, invoices]) => ({
  id: generateId(),
  statementNumber: generateStatementNumber(), // STMT-YYYYMMDD-XXX
  customerId: customerId,
  customer: findCustomer(customerId),
  statementDate: new Date(),
  periodStart: earliestInvoiceDate(invoices),
  periodEnd: new Date(),
  totalOutstanding: sum(invoices.map(inv => inv.outstandingAmount)),
  status: 'Draft',
  emailStatus: 'NotSent',
  invoices: invoices
}));

// 4. 保存到系统
saveStatements(newStatements);

// 5. 显示成功消息
toast.success('Weekly statements generated for all customers with outstanding balances');
```

**生成规则：**
- 只为有未付款发票的客户生成
- 初始状态为 Draft
- 邮件状态为 NotSent
- 可以预览后再发送

---

### 4.8 Admin Statement Detail 页面（/admin/statements/:id）

#### 4.8.1 页面布局

```
┌─────────────────────────────────────────────────────────┐
│ [← Back to Statements]                                  │
├─────────────────────────────────────────────────────────┤
│ STMT-20260507-001                      [Draft] [NotSent]│
│ Auckland Cafe                                           │
│ Statement Date: 07/05/2026                              │
│ Period: 24/04/2026 - 07/05/2026                         │
│                                                         │
│ [Send Email] [Download PDF]                             │
├─────────────────────────────────────────────────────────┤
│ Included Invoices                                       │
│ Invoice# Issue   Due    Total  AmtDue  PaymentStatus   │
│ INV-001  01/05   08/05  $323   $323    Unpaid          │
│ INV-002  24/04   01/05  $323   $323    Overdue         │
│                         Total Amount Due: $646.30       │
├─────────────────────────────────────────────────────────┤
│ ℹ This statement includes all unpaid and overdue       │
│   invoices as of 07/05/2026.                           │
└─────────────────────────────────────────────────────────┘
```

#### 4.8.2 页面元素

**顶部信息：**
- Statement Number - 大号标题
- Customer - 副标题
- Statement Date - 小字
- Period - 如果有 periodStart 和 periodEnd
- Status Chips - 显示 status 和 emailStatus

**操作按钮：**

**Send Email：**
```typescript
// 禁用条件
disabled={statement.emailStatus === 'Sent'}

// 点击行为
const handleSendEmail = () => {
  // 1. 生成 email
  // 2. 附带 PDF 或链接
  // 3. 发送到客户邮箱
  // 4. 更新状态
  updateStatement({
    status: 'Sent',
    emailStatus: 'Sent'
  });
  // 5. 记录 EmailLog
  createEmailLog({
    statementId: statement.id,
    recipient: statement.customer.email,
    sentAt: new Date(),
    status: 'Sent'
  });
  
  toast.success('Statement sent to customer');
};
```

**Download PDF：**
```typescript
const handleDownload = () => {
  // 生成 PDF 包含：
  // - Statement Number
  // - Customer Details
  // - Statement Date
  // - Included Invoices
  // - Total Amount Due
  // - Payment Instructions
  
  toast.success('Downloading statement PDF');
};
```

#### 4.8.3 Included Invoices 表格

**字段：**
1. Invoice #
2. Issue Date
3. Due Date
4. Total（总金额）
5. Amount Due（未付金额）
6. Payment Status（付款状态）

**合计行：**
```tsx
<TableRow>
  <TableCell colSpan={4} align="right">
    <Typography variant="h6">Total Amount Due</Typography>
  </TableCell>
  <TableCell align="right">
    <Typography variant="h6">${statement.totalOutstanding.toFixed(2)}</Typography>
  </TableCell>
  <TableCell />
</TableRow>
```

#### 4.8.4 底部说明

```
ℹ This statement includes all unpaid and overdue invoices as of 07/05/2026.
```

**样式：**
- Info 背景色
- 圆角边框
- Padding

---

### 4.9 Admin Customers 页面（/admin/customers）

#### 4.9.1 页面布局

```
┌─────────────────────────────────────────────────────────┐
│ Customers                            [+ Create Customer]│
│ Manage customer accounts                                │
├─────────────────────────────────────────────────────────┤
│ Business Name    Contact   Email              Status    │
│ Auckland Cafe    John...   john@auckland...   Active    │
│ Wellington Co... Sarah...  sarah@wellingt...  Active    │
└─────────────────────────────────────────────────────────┘
```

#### 4.9.2 表格字段

1. Business Name（公司名称）- 可点击
2. Contact Person（联系人）
3. Email（邮箱）
4. Phone（电话）
5. Account Status（账户状态）
6. Actions（操作）

#### 4.9.3 交互

**行点击：**
```typescript
onClick={() => navigate(`/admin/customers/${customer.id}`)}
```

**Create Customer 按钮：**
- 打开创建客户对话框
- 填写表单
- 保存后刷新列表

---

### 4.10 Customer Dashboard（/customer）

#### 4.10.1 页面布局

```
┌─────────────────────────────────────────────────────────┐
│ Welcome, John                                           │
│ Manage your coffee orders and invoices                 │
├─────────────────────────────────────────────────────────┤
│ ┌────────────────┐ ┌────────────────┐ ┌──────────────┐│
│ │Standing Order  │ │Amount Due      │ │Your Standing ││
│ │Status: Active  │ │$646.30         │ │Order Items   ││
│ │Frequency:      │ │2 unpaid inv... │ │House Blend...││
│ │Fortnightly     │ │⚠ 1 overdue    │ │Decaf 500g... ││
│ │Next: 12/05/2026│ │                │ │              ││
│ │[Edit Order]    │ │[View Invoices] │ │Est: $281.00  ││
│ └────────────────┘ └────────────────┘ └──────────────┘│
├─────────────────────────────────────────────────────────┤
│ Recent Invoices                                         │
│ INV-001  Due: 08/05/2026  $323.15  [Unpaid]            │
│ INV-002  Due: 01/05/2026  $323.15  [Overdue]           │
└─────────────────────────────────────────────────────────┘
```

#### 4.10.2 Standing Order 卡片

**显示内容：**
- Status（状态）- Chip
- Closing Frequency（截单频率）
  - Weekly → "Weekly"
  - Fortnightly → "Fortnightly (Every 2 weeks)"
  - Monthly → "Monthly"
- Next Closing Date（下次截单日期）
- 说明文字："Orders auto-generated based on this schedule"
- Edit Standing Order 按钮

**数据来源：**
```typescript
const standingOrder = filterStandingOrdersByCustomer(
  mockStandingOrders, 
  user.customerId
)[0];
```

**按钮点击：**
```typescript
navigate('/customer/standing-order');
```

#### 4.10.3 Amount Due 卡片

**显示内容：**
- 大号金额：总未付款金额
- 小字：X unpaid invoice(s)
- 警告框（如果有逾期）：
  ```
  ⚠ X overdue invoice(s)
  ```
- View Invoices 按钮

**计算逻辑：**
```typescript
const customerInvoices = filterInvoicesByCustomer(mockInvoices, user.customerId);
const totalOutstanding = customerInvoices.reduce((sum, inv) => sum + inv.outstandingAmount, 0);
const overdueInvoices = customerInvoices.filter(inv => inv.status === 'Overdue');
```

**警告框样式：**
```tsx
{overdueInvoices.length > 0 && (
  <Box sx={{ mt: 2, p: 1, bgcolor: 'error.light', borderRadius: 1 }}>
    <Box sx={{ display: 'flex', alignItems: 'center' }}>
      <Warning sx={{ mr: 1, fontSize: 18 }} />
      <Typography variant="body2">
        {overdueInvoices.length} overdue invoice(s)
      </Typography>
    </Box>
  </Box>
)}
```

#### 4.10.4 Your Standing Order Items 卡片

**显示内容：**
- 标题："Your Standing Order Items"
- 产品列表：
  ```
  House Blend 1kg x 5
  $45.00 each
  
  Decaf 500g x 2
  $28.00 each
  ```
- 分隔线
- Estimated Total: $XXX.XX

**数据来源：**
```typescript
standingOrder.items.map(item => ({
  name: item.product.name,
  quantity: item.quantity,
  unitPrice: item.unitPrice
}))
```

**计算总额：**
```typescript
const estimatedTotal = standingOrder.items.reduce(
  (sum, item) => sum + (item.quantity * item.unitPrice), 
  0
);
```

#### 4.10.5 Recent Invoices 列表

**显示：**
- 最新 3 张发票
- 每行显示：
  - Invoice Number
  - Due Date
  - Amount
  - Status Chip

**交互：**
- 点击行：无操作（仅展示）
- View All Invoices 按钮：跳转到 `/customer/invoices`

---

### 4.11 Customer Standing Order 页面（/customer/standing-order）

#### 4.11.1 页面概述

客户维护自己的固定订购清单。

#### 4.11.2 页面布局

```
┌─────────────────────────────────────────────────────────┐
│ Standing Order                                          │
│ Manage your regular coffee order                       │
├─────────────────────────────────────────────────────────┤
│ Order Status: [Active ▼]  Frequency: [Fortnightly ▼]   │
│ Next Closing Date: 12/05/2026                           │
│ Delivery Notes: [Deliver every Monday morning______]   │
├─────────────────────────────────────────────────────────┤
│ Order Items                                             │
│ Product           Quantity  Unit Price  Total  [Remove] │
│ House Blend 1kg   [5____]   $45.00     $225    [×]     │
│ Decaf 500g        [2____]   $28.00     $56     [×]     │
│                                                         │
│ [+ Add Product]                                         │
├─────────────────────────────────────────────────────────┤
│ Subtotal: $281.00                                       │
│ GST (15%): $42.15                                       │
│ Estimated Total: $323.15                                │
│                                                         │
│                    [Cancel]  [Save Changes]             │
└─────────────────────────────────────────────────────────┘
```

#### 4.11.3 表单字段

**Order Status：**
- 下拉选择
- 选项：Active, Paused, Cancelled
- 默认：Active

**Frequency：**
- 下拉选择
- 选项：Weekly, Fortnightly, Monthly, ManualOnly
- 说明文字根据选择显示

**Next Closing Date：**
- 只读显示
- 系统根据 frequency 自动计算

**Delivery Notes：**
- 多行文本框
- 可选填

#### 4.11.4 Order Items 表格

**字段：**
1. Product - 下拉选择（从可用产品列表）
2. Quantity - 数字输入框
3. Unit Price - 只读（根据产品自动填充）
4. Total - 只读计算（quantity × unitPrice）
5. Remove - 删除按钮

**Add Product 按钮：**
- 在列表末尾添加新行
- 产品下拉只显示未选择的产品

#### 4.11.5 合计显示

```typescript
const subtotal = items.reduce((sum, item) => sum + (item.quantity * item.unitPrice), 0);
const gst = subtotal * 0.15;
const total = subtotal + gst;
```

#### 4.11.6 保存逻辑

```typescript
const handleSave = () => {
  // 1. 验证
  if (items.length === 0) {
    toast.error('Please add at least one product');
    return;
  }
  
  // 2. 检查数量
  const hasInvalidQuantity = items.some(item => !item.quantity || item.quantity <= 0);
  if (hasInvalidQuantity) {
    toast.error('Please enter valid quantities for all products');
    return;
  }
  
  // 3. 保存
  updateStandingOrder({
    ...standingOrder,
    status: formData.status,
    frequency: formData.frequency,
    deliveryNotes: formData.deliveryNotes,
    items: items
  });
  
  // 4. 成功提示
  toast.success('Standing order updated successfully');
};
```

---

### 4.12 Customer Invoices 页面（/customer/invoices）

#### 4.12.1 页面布局

```
┌─────────────────────────────────────────────────────────┐
│ Invoices                                                │
│ View and download your invoices                         │
├─────────────────────────────────────────────────────────┤
│ [▼] Invoice# Issue   Due    Total  AmtDue  Status  PDF │
│ [ ] INV-001  01/05   08/05  $323   $323    Unpaid  [⬇] │
│     └─ Invoice Items                                    │
│        Description      Qty  Price  Total               │
│        House Blend...   5    $45    $225                │
└─────────────────────────────────────────────────────────┘
```

#### 4.12.2 表格字段

1. 展开/收起图标
2. Invoice #
3. Issue Date
4. Due Date
5. Total
6. Amount Due
7. Status
8. PDF（下载按钮）

#### 4.12.3 数据过滤

```typescript
const customerInvoices = filterInvoicesByCustomer(mockInvoices, user.customerId);
```

**注意：**
- 客户只能看到自己的发票
- 不能看到其他客户的发票

#### 4.12.4 状态显示

客户侧发票状态更直观：
- **Unpaid** - 未付款（紫色）
- **Paid** - 已付款（绿色）
- **Overdue** - 逾期（红色）
- **PartiallyPaid** - 部分付款（橙色）

不显示 Draft, NotIssued 等内部状态。

---

### 4.13 Customer Statements 页面（/customer/statements）

#### 4.13.1 页面布局

```
┌─────────────────────────────────────────────────────────┐
│ Statements                                              │
│ View your account statements                            │
├─────────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────────┐│
│ │ STMT-20260507-001                      [Download PDF]││
│ │ Statement Date: 07/05/2026                          ││
│ │ Auckland Cafe                                       ││
│ │                                                     ││
│ │ Invoice# Issue   Due    Amount  Status             ││
│ │ INV-001  01/05   08/05  $323    Unpaid             ││
│ │ INV-002  24/04   01/05  $323    Overdue            ││
│ │                  Total Amount Due: $646.30         ││
│ │                                                     ││
│ │ ⚠ Please arrange payment at your earliest          ││
│ │   convenience. Payment details on invoices.        ││
│ └─────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────┘
```

#### 4.13.2 显示内容

每个 Statement 显示为一个卡片：

**卡片头部：**
- Statement Number
- Statement Date
- Customer Name
- Download PDF 按钮

**Included Invoices 表格：**
- Invoice #
- Issue Date
- Due Date
- Amount
- Status

**合计：**
```
Total Amount Due: $XXX.XX
```

**底部提示：**
```
⚠ Please arrange payment at your earliest convenience. 
  Payment details can be found on your invoices.
```

#### 4.13.3 数据过滤

```typescript
const customerStatements = filterStatementsByCustomer(
  mockStatements, 
  user.customerId
);
```

---

### 4.14 Customer Account Settings 页面（/customer/settings）

#### 4.14.1 页面布局

```
┌─────────────────────────────────────────────────────────┐
│ Account Settings                                        │
│ Manage your account information                         │
├─────────────────────────────────────────────────────────┤
│ Business Information                                    │
│ Business Name: [Auckland Cafe________________]          │
│ Contact Person: [John Smith_________________]           │
│ Email: [john@aucklandcafe.co.nz______]                 │
│ Phone: [09 123 4567__________]                         │
├─────────────────────────────────────────────────────────┤
│ Billing & Delivery                                      │
│ Billing Address: [123 Queen St, Auckland 1010____]     │
│                  [_____________________________]         │
│ Delivery Address: [123 Queen St, Auckland 1010___]     │
│                   [_____________________________]        │
├─────────────────────────────────────────────────────────┤
│ Payment Terms                                           │
│ Current Terms: 7 days                                   │
│                                                         │
│                    [Cancel]  [Save Changes]             │
└─────────────────────────────────────────────────────────┘
```

#### 4.14.2 字段说明

**只读字段：**
- Payment Terms - 只能由 Admin 修改

**可编辑字段：**
- Business Name
- Contact Person
- Email
- Phone
- Billing Address
- Delivery Address

#### 4.14.3 保存逻辑

```typescript
const handleSave = () => {
  // 1. 验证必填字段
  if (!formData.businessName || !formData.email) {
    toast.error('Please fill in all required fields');
    return;
  }
  
  // 2. 验证邮箱格式
  if (!isValidEmail(formData.email)) {
    toast.error('Please enter a valid email address');
    return;
  }
  
  // 3. 保存
  updateCustomer(user.customerId, formData);
  
  // 4. 成功提示
  toast.success('Account settings updated successfully');
};
```

---

## 5. 状态流转逻辑

### 5.1 订单状态流转图

```
┌──────────┐
│ Generated│ 订单生成
└────┬─────┘
     │ Send to Production
     ↓
┌────────────┐
│InProduction│ 生产中
└────┬───────┘
     │ 所有产品完成
     ↓
┌────────────┐
│ReadyToShip │ 待出货
└────┬───────┘
     │ Mark as Shipped
     ↓
┌────────┐
│Shipped │ 已出货
└────┬───┘
     │ 发票已付款
     ↓
┌─────────┐
│Completed│ 已完成
└─────────┘

     任何时候可以
         ↓
┌─────────┐
│Cancelled│ 已取消
└─────────┘
```

### 5.2 发票状态流转图

```
┌──────────┐
│NotIssued │ 订单刚生成
└────┬─────┘
     │ Generate Invoice
     ↓
┌──────┐
│Draft │ 草稿
└───┬──┘
    │ Send Invoice
    ↓
┌────────┐
│Unpaid  │ 未付款
└───┬────┘
    │ 过了 Due Date
    ↓
┌────────┐
│Overdue │ 逾期
└───┬────┘
    │
    │ Record Payment (部分)
    ↓
┌──────────────┐
│PartiallyPaid │ 部分付款
└──────┬───────┘
       │ Record Payment (全额)
       ↓
┌──────┐
│Paid  │ 已付款
└──────┘
```

### 5.3 生产状态流转图

```
┌────────┐
│Pending │ 待生产
└───┬────┘
    │ Start Production
    ↓
┌──────────┐
│InProgress│ 生产中
└────┬─────┘
     │ Mark as Completed / Update Quantity = Total
     ↓
┌─────────┐
│Completed│ 已完成
└─────────┘

InProgress 可以
     ↓
┌────────┐
│On Hold │ 暂停
└───┬────┘
    │ Resume
    ↓
返回 InProgress
```

### 5.4 对账单状态流转图

```
┌──────┐
│Draft │ 生成后
└──┬───┘
   │ 确认无误
   ↓
┌────────────┐
│ReadyToSend │ 待发送
└─────┬──────┘
      │ Send Email
      ↓
┌──────┐
│Sent  │ 已发送
└──────┘
```

### 5.5 关键联动逻辑

#### 5.5.1 Orders → Production List

```typescript
// 当订单发送至生产
Order.orderStatus = 'InProduction'
  ↓
Production List 自动包含该订单的产品
  ↓
如果产品已存在，累加 totalQuantity
```

#### 5.5.2 Production List → Orders

```typescript
// 当产品标记为完成
ProductionItem.status = 'Completed'
  ↓
检查关联的订单
  ↓
对于每个订单，检查其所有产品是否都 Completed
  ↓
如果所有产品都完成：
  Order.orderStatus = 'ReadyToShip'
  Order.shipmentStatus = 'ReadyToShip'
```

#### 5.5.3 Orders → Invoices

```typescript
// 当订单标记为已出货
Order.orderStatus = 'Shipped'
Order.shipmentStatus = 'Shipped'
  ↓
如果 Order.invoiceStatus === 'NotIssued'：
  创建 Draft Invoice
  Order.invoiceStatus = 'Draft'
```

#### 5.5.4 Payments → Invoices

```typescript
// 当记录付款
PaymentRecord 创建
  ↓
Invoice.paidAmount += payment.amount
Invoice.outstandingAmount -= payment.amount
  ↓
如果 Invoice.outstandingAmount === 0：
  Invoice.status = 'Paid'
否则：
  Invoice.status = 'PartiallyPaid'
```

#### 5.5.5 Payments → Statements

```typescript
// 当发票付清
Invoice.status = 'Paid'
  ↓
该 Invoice 不再出现在新生成的 Statement 中
  ↓
历史 Statement 保持不变（快照特性）
```

---

## 6. 交互规范

### 6.1 Toast 通知规范

#### 6.1.1 通知类型

**Success（成功）**
```typescript
toast.success('Order sent to production');
toast.success('Payment recorded successfully');
```
- 颜色：绿色
- 图标：✓
- 持续时间：3 秒

**Info（信息）**
```typescript
toast.info('Navigating to invoices page');
toast.info('No orders available to send to production');
```
- 颜色：蓝色
- 图标：ℹ
- 持续时间：3 秒

**Warning（警告）**
```typescript
toast.warning('Production put on hold');
```
- 颜色：橙色
- 图标：⚠
- 持续时间：4 秒

**Error（错误）**
```typescript
toast.error('Please enter a valid quantity');
toast.error('Failed to save changes');
```
- 颜色：红色
- 图标：✕
- 持续时间：5 秒

#### 6.1.2 带描述的通知

```typescript
toast.success('3 orders sent to production successfully', {
  description: 'These orders have been added to the Production List and are now in production.'
});
```

#### 6.1.3 带操作按钮的通知

```typescript
toast.info('View Production List to track progress', {
  action: {
    label: 'Go to Production',
    onClick: () => navigate('/admin/production')
  }
});
```

### 6.2 对话框规范

#### 6.2.1 确认对话框

用于危险操作：
- 删除客户
- 取消订单
- 取消对账单

```tsx
<Dialog open={open} onClose={handleClose}>
  <DialogTitle>Confirm Action</DialogTitle>
  <DialogContent>
    <Typography>Are you sure you want to cancel this order?</Typography>
    <Typography variant="caption" color="text.secondary">
      This action cannot be undone.
    </Typography>
  </DialogContent>
  <DialogActions>
    <Button onClick={handleClose}>Cancel</Button>
    <Button onClick={handleConfirm} color="error" variant="contained">
      Confirm
    </Button>
  </DialogActions>
</Dialog>
```

#### 6.2.2 表单对话框

用于数据输入：
- Record Payment
- Update Quantity
- Create Customer

```tsx
<Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
  <DialogTitle>Dialog Title</DialogTitle>
  <DialogContent>
    {/* 表单内容 */}
  </DialogContent>
  <DialogActions>
    <Button onClick={handleClose}>Cancel</Button>
    <Button onClick={handleSave} variant="contained" disabled={!isValid}>
      Save
    </Button>
  </DialogActions>
</Dialog>
```

### 6.3 表单验证规范

#### 6.3.1 必填字段

```typescript
// 视觉标记
<TextField
  label="Email"
  required
  error={!email && touched}
  helperText={!email && touched ? 'Email is required' : ''}
/>
```

#### 6.3.2 数字验证

```typescript
// 数量必须 > 0
if (!quantity || quantity <= 0) {
  toast.error('Quantity must be greater than 0');
  return;
}

// 不能超过最大值
if (producedQuantity > totalQuantity) {
  toast.error('Produced quantity cannot exceed total quantity');
  return;
}
```

#### 6.3.3 邮箱验证

```typescript
const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
if (!emailRegex.test(email)) {
  toast.error('Please enter a valid email address');
  return;
}
```

### 6.4 状态 Chip 颜色规范

```typescript
// 订单状态颜色
const orderStatusColors = {
  Generated: '#9E9E9E',      // 灰色
  InProduction: '#FF9800',   // 橙色
  ReadyToShip: '#2196F3',    // 蓝色
  Shipped: '#4CAF50',        // 绿色
  Completed: '#009688',      // 青色
  Cancelled: '#F44336',      // 红色
};

// 发票状态颜色
const invoiceStatusColors = {
  NotIssued: '#BDBDBD',      // 浅灰
  Draft: '#9E9E9E',          // 灰色
  Issued: '#2196F3',         // 蓝色
  Unpaid: '#673AB7',         // 紫色
  PartiallyPaid: '#FF9800',  // 橙色
  Paid: '#4CAF50',           // 绿色
  Overdue: '#F44336',        // 红色
  Cancelled: '#757575',      // 深灰
};

// 出货状态颜色
const shipmentStatusColors = {
  NotShipped: '#BDBDBD',     // 浅灰
  ReadyToShip: '#2196F3',    // 蓝色
  Shipped: '#4CAF50',        // 绿色
  Delivered: '#009688',      // 青色
};

// 生产状态颜色
const productionStatusColors = {
  Pending: '#9E9E9E',        // 灰色
  InProgress: '#FF9800',     // 橙色
  Completed: '#4CAF50',      // 绿色
  OnHold: '#F44336',         // 红色
};

// 邮件状态颜色
const emailStatusColors = {
  NotSent: '#BDBDBD',        // 浅灰
  Pending: '#FF9800',        // 橙色
  Sent: '#4CAF50',           // 绿色
  Failed: '#F44336',         // 红色
  Bounced: '#9E9E9E',        // 灰色
};
```

### 6.5 加载状态规范

#### 6.5.1 按钮加载

```tsx
<Button 
  onClick={handleSubmit}
  disabled={loading}
>
  {loading ? 'Saving...' : 'Save'}
</Button>
```

#### 6.5.2 页面加载

```tsx
{loading ? (
  <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}>
    <CircularProgress />
  </Box>
) : (
  // 页面内容
)}
```

---

## 7. 技术规范

### 7.1 API 接口设计（前后端分离架构）

#### 7.1.1 RESTful API 规范

**基础 URL：** `https://api.storycoffee.co.nz/v1`

**认证：** JWT Token
```
Authorization: Bearer <token>
```

#### 7.1.2 Orders API

**获取订单列表**
```http
GET /admin/orders
Query Parameters:
  - status: OrderStatus (可选)
  - customerId: string (可选)
  - from: date (可选)
  - to: date (可选)
  - page: number
  - limit: number

Response:
{
  "data": Order[],
  "total": number,
  "page": number,
  "limit": number
}
```

**更新订单状态**
```http
PATCH /admin/orders/:id
Body:
{
  "orderStatus": "InProduction",
  "invoiceStatus": "Draft",
  "shipmentStatus": "NotShipped"
}

Response:
{
  "data": Order
}
```

**批量发送至生产**
```http
POST /admin/orders/batch-to-production
Body:
{
  "orderIds": string[]
}

Response:
{
  "success": true,
  "updated": number,
  "orders": Order[]
}
```

#### 7.1.3 Production API

**获取生产清单**
```http
GET /admin/production
Query Parameters:
  - status: ProductionStatus (可选)

Response:
{
  "data": ProductionItem[]
}
```

**更新生产项**
```http
PATCH /admin/production/:productId
Body:
{
  "producedQuantity": number,
  "status": "InProgress" | "Completed"
}

Response:
{
  "data": ProductionItem,
  "affectedOrders": Order[]
}
```

#### 7.1.4 Invoices API

**获取发票列表**
```http
GET /admin/invoices
GET /customer/invoices

Response:
{
  "data": Invoice[]
}
```

**发送发票邮件**
```http
POST /admin/invoices/:id/send
Response:
{
  "success": true,
  "emailLog": EmailLog
}
```

#### 7.1.5 Payments API

**记录付款**
```http
POST /admin/payments
Body:
{
  "invoiceId": string,
  "amount": number,
  "paymentDate": date,
  "paymentMethod": string,
  "reference": string,
  "note": string
}

Response:
{
  "data": PaymentRecord,
  "invoice": Invoice  // 更新后的发票
}
```

#### 7.1.6 Statements API

**生成周报表**
```http
POST /admin/statements/generate-weekly
Response:
{
  "data": Statement[],
  "generated": number
}
```

**发送对账单**
```http
POST /admin/statements/:id/send
Response:
{
  "success": true,
  "statement": Statement
}
```

### 7.2 错误处理规范

#### 7.2.1 HTTP 状态码

```
200 OK - 成功
201 Created - 创建成功
400 Bad Request - 请求参数错误
401 Unauthorized - 未授权
403 Forbidden - 无权限
404 Not Found - 资源不存在
422 Unprocessable Entity - 验证失败
500 Internal Server Error - 服务器错误
```

#### 7.2.2 错误响应格式

```json
{
  "error": {
    "code": "INVALID_QUANTITY",
    "message": "Produced quantity cannot exceed total quantity",
    "details": {
      "field": "producedQuantity",
      "value": 15,
      "max": 10
    }
  }
}
```

#### 7.2.3 前端错误处理

```typescript
try {
  const response = await api.post('/orders/batch-to-production', { orderIds });
  toast.success('Orders sent to production successfully');
} catch (error) {
  if (error.response) {
    // 服务器返回错误
    toast.error(error.response.data.error.message);
  } else if (error.request) {
    // 网络错误
    toast.error('Network error. Please check your connection.');
  } else {
    // 其他错误
    toast.error('An unexpected error occurred');
  }
}
```

### 7.3 权限控制

#### 7.3.1 路由级别权限

```typescript
// Admin 路由保护
<Route 
  path="/admin" 
  element={<ProtectedRoute requiredRole="Admin" />}
>
  <Route index element={<AdminDashboard />} />
  <Route path="orders" element={<Orders />} />
  // ...
</Route>

// Customer 路由保护
<Route 
  path="/customer" 
  element={<ProtectedRoute requiredRole="Customer" />}
>
  <Route index element={<CustomerDashboard />} />
  // ...
</Route>
```

#### 7.3.2 数据级别权限

```typescript
// Customer 只能访问自己的数据
const customerInvoices = invoices.filter(
  inv => inv.customerId === user.customerId
);

// Admin 可以访问所有数据
const allInvoices = invoices;
```

### 7.4 性能优化

#### 7.4.1 分页

```typescript
// 大列表使用分页
const [page, setPage] = useState(1);
const [limit] = useState(20);

const { data, total } = await fetchOrders({ page, limit });
```

#### 7.4.2 防抖

```typescript
// 搜索输入防抖
const debouncedSearch = useMemo(
  () => debounce((value) => {
    searchOrders(value);
  }, 300),
  []
);
```

#### 7.4.3 缓存

```typescript
// 使用 React Query 缓存数据
const { data: orders } = useQuery(
  ['orders', filters],
  () => fetchOrders(filters),
  {
    staleTime: 60000, // 1 分钟
    cacheTime: 300000, // 5 分钟
  }
);
```

---

## 8. 附录

### 8.1 术语表

| 中文术语 | 英文术语 | 说明 |
|---------|---------|------|
| 固定订购清单 | Standing Order | 客户设置的定期自动订单模板 |
| 订单 | Order | 根据 Standing Order 自动生成的实际订单 |
| 发票 | Invoice | 订单出货后生成的账单 |
| 对账单 | Statement | 未付款发票的汇总 |
| 生产清单 | Production List | 按产品汇总的生产任务清单 |
| 收款记录 | Payment Record | 客户付款的记录 |
| 截单日期 | Closing Date | 订单生成的日期 |
| 未付金额 | Amount Due / Outstanding Amount | 发票中尚未支付的金额 |

### 8.2 日期格式规范

**显示格式：**
```typescript
// 短日期：07/05/2026
date.toLocaleDateString()

// 完整日期：May 7, 2026
date.toLocaleDateString('en-US', { 
  year: 'numeric', 
  month: 'long', 
  day: 'numeric' 
})

// ISO 格式（API）：2026-05-07
date.toISOString().split('T')[0]
```

### 8.3 货币格式规范

```typescript
// 显示格式
`$${amount.toFixed(2)}`  // $323.15

// 右对齐
<TableCell align="right">${amount.toFixed(2)}</TableCell>

// 大号金额
<Typography variant="h3">
  ${totalOutstanding.toFixed(2)}
</Typography>
```

### 8.4 订单号生成规则

```typescript
// 订单号：ORD-YYYYMMDD-XXX
// 示例：ORD-20260507-001

function generateOrderNumber(date: Date): string {
  const dateStr = date.toISOString().split('T')[0].replace(/-/g, '');
  const sequence = getNextSequence(date); // 当天的序号
  return `ORD-${dateStr}-${sequence.toString().padStart(3, '0')}`;
}

// 发票号：INV-XXX
// 示例：INV-001

function generateInvoiceNumber(): string {
  const sequence = getGlobalInvoiceSequence();
  return `INV-${sequence.toString().padStart(3, '0')}`;
}

// 对账单号：STMT-YYYYMMDD-XXX
// 示例：STMT-20260507-001

function generateStatementNumber(date: Date): string {
  const dateStr = date.toISOString().split('T')[0].replace(/-/g, '');
  const sequence = getNextStatementSequence(date);
  return `STMT-${dateStr}-${sequence.toString().padStart(3, '0')}`;
}
```

---

## 9. 开发检查清单

### 9.1 Admin 端功能

- [ ] Admin Dashboard 统计卡片显示正确
- [ ] Admin Dashboard 点击跳转正确
- [ ] Orders 页面显示所有字段
- [ ] Orders 批量发送至生产功能
- [ ] Orders Actions 菜单根据状态显示
- [ ] Orders 状态更新后界面刷新
- [ ] Production List 产品汇总正确
- [ ] Production List 操作按钮逻辑正确
- [ ] Production List 与 Orders 联动
- [ ] Invoices 页面显示和操作
- [ ] Payments Record Payment 功能
- [ ] Payments 记录后状态联动
- [ ] Statements List 显示
- [ ] Statements Generate Weekly 功能
- [ ] Statement Detail 页面显示
- [ ] Statement Send Email 功能

### 9.2 Customer 端功能

- [ ] Customer Dashboard 显示正确数据
- [ ] Customer Dashboard 统计正确
- [ ] Standing Order 编辑功能
- [ ] Standing Order 保存验证
- [ ] Invoices 只显示本客户数据
- [ ] Invoices 下载 PDF
- [ ] Statements 显示正确
- [ ] Account Settings 保存功能

### 9.3 状态联动

- [ ] Orders → Production List 联动
- [ ] Production List → Orders 联动
- [ ] Orders → Invoices 联动
- [ ] Payments → Invoices 联动
- [ ] Invoices → Dashboard 联动
- [ ] Admin 操作 → Customer 端同步

### 9.4 权限控制

- [ ] Admin 不能访问 Customer 路由
- [ ] Customer 不能访问 Admin 路由
- [ ] Customer 只能看到自己的数据
- [ ] 未登录自动跳转登录页

### 9.5 用户体验

- [ ] 所有操作有 Toast 反馈
- [ ] 按钮禁用状态正确
- [ ] 表单验证提示清晰
- [ ] 加载状态显示
- [ ] 错误处理友好
- [ ] 响应式布局适配

---

**文档版本：** 1.0  
**最后更新：** 2026-05-07  
**审核状态：** 待审核

---

**结束**
