下面这段可以直接作为你给 Figma / 开发人员的文字说明。它把目前所有问题和需要调整的交互逻辑整理到一起了。

---

# StoryCoffee Admin & Customer Portal 页面调整说明

当前系统是 **StoryCoffee B2B Order & Invoice Management System**，不是普通咖啡商城。系统核心流程是：

> 客户维护 Standing Order 固定订购清单 → 系统按周期生成正式订单 → Admin 查看订单并安排生产 → 标记出货 → 生成并发送 Invoice → 客户线下转账付款 → Admin 在 Payments 页面记录付款 → 系统同步更新 Invoice 状态 → 未付款 Invoice 汇总成 Statement。

---

# 1. Orders 页面需要调整

当前 Orders 页面字段太少，无法清楚表达订单处理进度。需要增加以下字段：

```text
Order #
Customer
Generated Date
Total
Order Status
Invoice Status
Shipment Status
Actions
```

## 字段说明

**Generated Date**
表示这张订单是什么时候由系统生成的。

**Total**
表示该订单的总金额。

**Order Status**
表示订单本身的处理状态，例如：

```text
Generated
In Production
Ready to Ship
Shipped
Cancelled
```

**Invoice Status**
表示这张订单对应的 invoice 状态，例如：

```text
Not Issued
Draft
Sent
Paid
Overdue
Cancelled
```

**Shipment Status**
表示出货状态，例如：

```text
Not Shipped
Ready to Ship
Shipped
Delivered
```

**Actions**
根据订单当前状态显示不同操作，例如：

```text
View Details
Send to Production
Mark as Shipped
Generate Invoice
View Invoice
Cancel Order
```

---

# 2. Orders 页面交互逻辑

Orders 页面不能只是静态展示，状态之间需要联动。

## 订单生成后

当系统根据客户 Standing Order 自动生成订单后：

```text
Order Status = Generated
Invoice Status = Not Issued
Shipment Status = Not Shipped
```

## 点击 Send to Production

Admin 点击 **Send to Production** 后：

```text
Order Status 从 Generated 变为 In Production
该订单会进入 Production List 统计范围
```

## 点击 Mark as Shipped

Admin 点击 **Mark as Shipped** 后：

```text
Order Status = Shipped
Shipment Status = Shipped
```

同时系统应该可以触发：

```text
生成 / 确认 Invoice
Invoice Status 变为 Draft 或 Sent
发送 invoice email 给客户
```

具体可以设计成两种方式：

```text
方式 A：Mark as Shipped 后自动生成并发送 Invoice
方式 B：Mark as Shipped 后生成 Draft Invoice，Admin 预览后手动发送
```

我建议 MVP 用 **方式 B**，因为 invoice 发送前最好让 Admin 预览。

---

# 3. Production List 页面需要调整

Production List 是生产清单，不是订单列表。
它的作用是把已经生成的订单汇总成需要生产的产品数量。

页面标题下面需要加一句说明：

```text
This page summarizes generated orders into product quantities for production.
```

中文意思是：

> 这个页面会把已生成的订单汇总成生产所需的产品数量。

## Production List 页面字段

建议表格字段为：

```text
Product
SKU
Total Quantity
Related Orders
Production Status
Actions
```

## Production List 的逻辑

例如有两个订单：

```text
ORD-001: House Blend 1kg x 5
ORD-002: House Blend 1kg x 5
```

Production List 应该汇总为：

```text
House Blend 1kg
Total Quantity: 10
Related Orders: ORD-001, ORD-002
```

这个页面帮助 StoryCoffee 知道：

```text
本周需要生产哪些产品
每个产品需要生产多少
这些产品来自哪些客户订单
```

---

# 4. Payments 页面需要调整

Payments 页面是 Admin 记录客户付款的地方。

因为当前系统没有做在线支付，所以系统不会自动知道客户是否已经付款。真实流程应该是：

```text
客户收到 invoice
→ 客户通过银行转账付款
→ Admin 查看银行账户确认到账
→ Admin 登录系统
→ 进入 Payments 页面
→ 点击 Record Payment
→ 系统把对应 invoice 标记为 Paid
```

## Payments 页面字段

建议表格字段为：

```text
Invoice #
Customer
Issue Date
Due Date
Amount Due
Payment Status
Actions
```

其中 **Amount Due** 表示该 invoice 还需要支付的金额。

## Payment Status

付款状态建议为：

```text
Unpaid
Partially Paid
Paid
Overdue
```

## Record Payment 按钮

点击 **Record Payment** 后，应该弹出一个表单：

```text
Invoice #
Customer
Amount Due
Payment Date
Amount Paid
Payment Method
Payment Reference
Notes
Confirm Payment
```

## 点击 Confirm Payment 后的系统联动

当 Admin 确认付款后，系统需要更新多个地方：

```text
1. PaymentRecord 表新增一条付款记录
2. 对应 Invoice 的 paid_amount 增加
3. outstanding_amount / amount_due 减少
4. 如果已付清，Invoice Status 变为 Paid
5. Admin 的 Invoices 页面显示 Paid
6. Customer Portal 的 Invoices 页面也显示 Paid
7. Dashboard 的 Amount Due / Outstanding Balance 自动减少
8. 如果该 invoice 原本在 Statement 里，后续新的 Statement 不应再包含它
```

也就是说，付款状态必须在 **Admin 端和 Customer 端同步更新**。

---

# 5. Statements 页面需要重新设计

现在 Statements 页面很难懂，因为它直接显示了某个客户的 statement detail，但是缺少上一层的列表页面。

正确结构应该是两层：

```text
Statements List
→ Statement Detail
```

---

## 第一层：Statements List 页面

这是 Admin 进入 Statements 时首先看到的页面。

页面字段：

```text
Statement Number
Customer
Statement Date
Total Amount Due
Status
Actions
```

可以加上：

```text
Period
Email Status
```

更完整的字段如下：

```text
Statement Number
Customer
Statement Date
Period
Total Amount Due
Status
Email Status
Actions
```

## 示例

```text
STMT-20260507-001 | Auckland Cafe | 07/05/2026 | $646.30 | Draft | View / Send Email / Download PDF
STMT-20260507-002 | Wellington Coffee House | 07/05/2026 | $262.20 | Sent | View / Download PDF
```

## Statement Status

Statement 自己的状态建议为：

```text
Draft
Ready to Send
Sent
Cancelled
```

这里的状态表示 statement 本身有没有生成、确认、发送，不是 invoice 是否付款。

---

# 6. Statement Detail 页面

点击某一条 Statement 后，进入详情页。

当前系统现在展示的页面其实应该是 **Statement Detail**。

## Statement Detail 页面需要显示

```text
Statement Number
Customer
Statement Date
Statement Period
Total Amount Due
Status
Email Status
Included Invoices
```

## Included Invoices 表格字段

```text
Invoice #
Issue Date
Due Date
Total
Amount Due
Payment Status
```

不要再用 `Sent` 作为 invoice 的付款状态。
Invoice 的付款状态应该是：

```text
Unpaid
Partially Paid
Paid
Overdue
```

Statement 自己才有：

```text
Draft / Sent
```

## 页面底部说明

可以保留类似这句话：

```text
This statement includes all unpaid and overdue invoices as of 07/05/2026.
```

这句话很重要，因为它说明这张 statement 是在某一天生成的未付款汇总快照。

---

# 7. Generate Weekly Statements 按钮逻辑

Admin 点击：

```text
Generate Weekly Statements
```

系统应该执行：

```text
1. 查找所有 Unpaid 或 Overdue invoices
2. 按 Customer 分组
3. 为每个有未付款 invoice 的客户生成一张 Statement
4. 生成 Statement Number
5. 计算 Total Amount Due
6. 状态设为 Draft
7. 出现在 Statements List 页面
```

例如 Auckland Cafe 有两张未付款 invoice：

```text
INV-001: $323.15 Unpaid
INV-002: $323.15 Overdue
```

系统生成：

```text
STMT-20260507-001
Customer: Auckland Cafe
Total Amount Due: $646.30
Status: Draft
```

Admin 可以先预览，再点击 Send Email。

---

# 8. Send Email 和 Download PDF 交互

## Send Email

Admin 在 Statement Detail 页面点击 **Send Email** 后：

```text
1. 系统生成 statement email
2. 附带 PDF 或提供 PDF 下载链接
3. 发送给客户邮箱
4. Statement Email Status 更新为 Sent
5. 记录 EmailLog
```

## Download PDF

点击 **Download PDF** 后：

```text
系统下载该 statement 的 PDF 文件
```

PDF 内容应包括：

```text
Statement Number
Customer Details
Statement Date
Included Unpaid Invoices
Total Amount Due
Payment Instructions
```

---

# 9. Invoice、Payment、Statement 的关系

这三个概念必须分清楚。

## Invoice

Invoice 是单张账单。

```text
一张订单通常对应一张 invoice
```

Invoice 记录：

```text
客户需要为某张订单支付多少钱
due date 是什么时候
目前是否已付款
```

---

## Payment

Payment 是付款记录。

```text
客户付款后，Admin 记录 payment
```

一个 invoice 可能有：

```text
0 个 payment
1 个 payment
多个 payment
```

因为可能会有部分付款。

---

## Statement

Statement 是未付款汇总。

```text
Statement 不是新的账单
Statement 是把客户所有未付款 invoice 汇总起来提醒客户付款
```

它通常包含多张 invoices。

---

# 10. 各页面之间的状态联动

现在系统的问题是：按钮点了以后，其他页面状态没有变化。
正确的交互应该是这样的。

---

## 订单状态联动

当 Order 被创建：

```text
Orders 页面出现新订单
Production List 可以统计该订单
Invoice Status = Not Issued
Shipment Status = Not Shipped
```

当 Order 状态改为 In Production：

```text
Orders 页面 Order Status 更新为 In Production
Production List 里该订单进入生产范围
```

当 Order 状态改为 Shipped：

```text
Orders 页面 Shipment Status 更新为 Shipped
可以生成 / 发送 Invoice
Invoices 页面出现对应 invoice
```

---

## Invoice 状态联动

当 Admin 生成 invoice：

```text
Invoices 页面出现 Draft Invoice
Customer Portal 可以看不到，或者只看到 Issued/Sent 后的 invoice
```

当 Admin 发送 invoice：

```text
Invoice Status = Sent / Unpaid
Customer Portal 的 Invoices 页面出现该 invoice
客户可以下载 PDF
```

建议客户侧显示：

```text
Unpaid
Paid
Overdue
```

不要显示 Sent，因为客户更关心是否要付款。

---

## Payment 状态联动

当 Admin 在 Payments 页面点击 Record Payment 并确认：

```text
PaymentRecord 新增
Invoice paid_amount 更新
Invoice amount_due 更新
Invoice Status 更新为 Paid 或 Partially Paid
Customer Portal 的 Invoice 状态同步更新
Dashboard 的 Amount Due 减少
Payments 页面不再显示已付清 invoice
Statement 后续不再包含已付清 invoice
```

---

## Statement 状态联动

当 Generate Weekly Statements 被点击：

```text
Statements List 新增 Draft Statement
Statement Detail 包含当前未付款 invoices
```

当 Send Email 被点击：

```text
Statement Status / Email Status 更新为 Sent
EmailLog 新增记录
Customer Portal 的 Statements 页面显示该 statement
```

当某个 invoice 被标记为 Paid：

```text
新生成的 Statement 不再包含这个 invoice
如果历史 Statement 已经生成，它可以作为历史快照保留不变
```

这里要注意：

> 历史 Statement 是当时的快照，不建议付款后自动修改历史 Statement 内容。
> 但是 Customer Dashboard 的 Amount Due 应该实时更新。

---

# 11. 客户端页面也要同步

Customer Portal 里客户看到的内容应该来自同一套数据。

## Customer Dashboard

如果 Admin 记录付款成功：

```text
Amount Due 应该减少
Unpaid invoice count 应该减少
Overdue warning 应该更新
```

## Customer Invoices

如果 Admin 把 invoice 标记为 Paid：

```text
客户侧该 invoice 状态也应该变成 Paid
Amount Due 变成 $0
```

## Customer Statements

如果 Admin 发送了 Statement：

```text
客户侧 Statements 页面应该出现这张 statement
客户可以下载 PDF
```

---

# 12. 最终页面结构建议

## Admin Portal

```text
Dashboard
Customers
Products
Standing Orders
Orders
Production List
Invoices
Payments
Statements
Settings
```

## Customer Portal

```text
Dashboard
Standing Order
Orders
Invoices
Statements
Account Settings
```

---

# 13. 最重要的一句话

这套系统的关键不是页面本身，而是状态流转：

```text
Order 生成后进入生产；
出货后生成 Invoice；
客户付款后 Admin 记录 Payment；
Payment 会更新 Invoice；
未付款 Invoice 会进入 Statement；
Statement 发送后客户可以查看和下载。
```

所以 Figma 原型里需要体现：

```text
每个按钮点击后，相关页面的状态都要同步变化。
```

例如：

```text
Record Payment
→ Invoice Status 从 Unpaid 变成 Paid
→ Payments 页面移除该 unpaid invoice
→ Customer Portal 同步显示 Paid
→ Amount Due 下降
```

这样这个系统才是一个真正的管理系统，而不是静态页面集合。
