可以。下面我按你现在这张图，重新设计一套完整系统。
这套系统**不再是咖啡商城**，而是：

# StoryCoffee B2B Order & Invoice Management System

中文可以叫：

> **StoryCoffee 商业客户订阅订单与发票管理系统**

它主要解决的是：
**老客户长期订购、周期性结单、自动生成订单、生产出货、生成 Invoice、付款查核、未付款 Statement 这些线下流程线上化。**

---

# 1. 系统定位

这个系统不是给普通消费者买咖啡用的，因为 StoryCoffee 现在已经有线上购买和支付网站。

这个系统主要服务：

```text
已有商业客户
长期订阅客户
批发客户
办公室 / 咖啡店 / 餐厅 / 公司客户
```

它的核心不是购物车和支付，而是：

```text
客户维护固定订购内容
→ 系统按周期结单
→ 自动生成正式订单
→ 后台生成生产清单
→ 出货
→ 生成 Invoice
→ 客户付款
→ 管理员人工查核付款
→ 未付款客户收到 Statement
```

所以项目名称建议用：

```text
B2B Coffee Order & Invoice Management Platform
```

或者：

```text
StoryCoffee Subscription & Billing Management System
```

---

# 2. 系统核心角色

## 2.1 Business Customer

商业客户，也就是已经合作的老客户或新批发客户。

他们可以：

```text
登录系统
查看自己的固定订购清单
修改订购内容
查看结单频率
查看历史订单
查看 invoice
查看付款状态
查看未付款 statement
```

商业客户不需要每次点击“下单”。
他们维护的是一个固定订购清单，系统按周期自动生成订单。

---

## 2.2 Admin / Staff

StoryCoffee 内部员工或老板。

他们可以：

```text
创建客户账号
维护客户信息
管理产品
查看固定订购清单
执行或查看结单
查看生成的订单
生成生产清单
标记出货
生成 invoice
发送 invoice email
人工标记已付款
查看未付款客户
发送 statement
```

---

## 2.3 Finance / Accounts Staff

这个角色可以后期加。
第一版可以先和 Admin 合并。

他们主要负责：

```text
查看 invoice
核对付款
标记 paid / unpaid
发送 statement
导出财务报表
```

---

# 3. 核心概念重新定义

你图里有一些概念要改得更清楚。

---

## 3.1 Standing Order

不要再叫“购物车”。

因为这个不是普通购物车，而是客户长期维护的固定订购模板。

建议英文叫：

```text
Standing Order
```

中文叫：

```text
固定订购清单
周期性订购模板
```

它表示：

```text
这个客户平时固定要买什么咖啡
每次要多少
什么规格
什么备注
多久结一次单
```

例如：

```text
Auckland Cafe Standing Order

House Blend 1kg x 5
Decaf 500g x 2
Delivery note: Deliver every Monday morning
Closing frequency: Weekly
```

---

## 3.2 Closing Schedule

结单规则。

客户可以选择：

```text
每周结单
每两周结单
每月结单
不自动结单
```

英文可以叫：

```text
Closing Schedule
```

或者：

```text
Order Closing Frequency
```

它决定系统什么时候把 Standing Order 转成正式订单。

---

## 3.3 Generated Order

系统结单后生成的正式订单。

Standing Order 不是订单。
结单后生成的才是正式订单。

流程是：

```text
Standing Order
→ Closing time arrives
→ Generated Order
```

---

## 3.4 Production List

生产清单。

系统可以把多个客户的订单汇总成生产清单，让 StoryCoffee 知道这次需要制作多少产品。

例如：

```text
This Week Production List

House Blend 1kg x 28
Brazil Espresso 1kg x 12
Decaf 500g x 6
Filter Blend 250g x 18
```

这对实际运营非常重要，因为老板不是只看单个订单，而是要知道：

```text
这周总共要做多少咖啡
哪些产品要优先做
哪些订单要出货
```

---

## 3.5 Invoice

Invoice 是系统生成的账单。

应该支持：

```text
Invoice preview
Invoice PDF
Invoice email
Invoice status
Customer portal view
```

Invoice 状态建议：

```text
Draft
Issued
Sent
Partially Paid
Paid
Overdue
Cancelled
```

---

## 3.6 Statement

Statement 是未付款汇总。

不是单张 invoice，而是某个客户当前所有未付款 invoice 的汇总。

例如：

```text
Statement for Auckland Cafe

INV-001    $320    Unpaid
INV-002    $280    Unpaid
Total Outstanding: $600
```

系统可以每周自动给未付款客户发送 statement。

---

# 4. 总体业务流程

完整流程应该是这样：

```text
1. Admin 创建客户账号
2. 客户收到邀请邮件并激活账号
3. 客户登录 Customer Portal
4. 客户设置或修改 Standing Order
5. 客户选择结单频率
6. 系统按频率自动结单
7. 系统生成正式订单
8. 后台查看订单清单
9. 系统生成生产清单
10. 员工制作产品
11. 员工标记出货
12. 系统生成 / 发送 Invoice
13. 客户登录系统查看 Invoice
14. 客户线下付款
15. Admin 人工查核付款
16. Admin 标记 Invoice 为 Paid
17. 未付款客户每周收到 Statement
```

---

# 5. 系统模块设计

这套系统可以拆成 10 个模块。

---

## 5.1 Customer Account System

客户账号系统。

因为很多客户是线下老客户，所以不建议让他们重新申请。
更合理的是：

```text
Admin 预创建客户账号
→ 系统发送邀请邮件
→ 客户点击链接
→ 设置密码
→ 登录系统
```

功能包括：

```text
客户登录
客户激活账号
客户修改密码
客户查看自己的资料
客户查看 invoice
客户查看订单历史
```

账号状态建议：

```text
Draft
Invited
Active
Suspended
Archived
```

解释：

```text
Draft = 后台已创建，但未邀请
Invited = 已发送邀请，但客户未激活
Active = 客户已激活，可以登录
Suspended = 暂停合作或限制登录
Archived = 历史客户
```

---

## 5.2 Customer Management System

后台客户管理。

Admin 可以维护：

```text
客户名称
联系人
邮箱
电话
账单地址
配送地址
付款条款
账号状态
默认结单频率
内部备注
```

页面建议：

```text
Customer List
Customer Detail
Create Customer
Edit Customer
Send Invitation
Resend Invitation
Suspend Customer
Archive Customer
```

---

## 5.3 Product Management System

产品管理。

虽然你不做咖啡商城，但系统仍然需要产品表，因为 Standing Order 和 Invoice 都依赖产品信息。

产品字段包括：

```text
产品名称
SKU
规格
单位
单价
成本价
是否启用
产品分类
备注
```

例如：

```text
House Blend 1kg
Decaf 500g
Brazil Espresso 1kg
Filter Blend 250g
```

注意：
如果 StoryCoffee 现有网站已经有产品数据，第一版可以先手动维护一份产品表。后期再考虑和现有网站同步。

---

## 5.4 Standing Order System

固定订购清单系统。

这是客户侧最重要的模块。

客户可以：

```text
查看自己的固定订购清单
添加产品
删除产品
修改数量
修改规格
修改备注
保存更改
设置是否启用
```

Standing Order 字段：

```text
客户
订购频率
下次结单日期
是否自动结单
状态
配送备注
内部备注
```

Standing Order Item 字段：

```text
产品
SKU
数量
单价
规格
备注
```

客户页面应该显示：

```text
Your Standing Order
Closing Frequency: Weekly
Next Closing Date: 15 May 2026
Status: Active

Items:
House Blend 1kg x 5
Decaf 500g x 2

[Edit Standing Order]
[Save Changes]
```

---

## 5.5 Closing / Scheduled Order Generation System

周期性结单系统。

这是自动化核心。

系统根据每个客户的结单频率自动生成正式订单。

频率包括：

```text
Weekly
Fortnightly
Monthly
Manual Only
```

系统任务逻辑：

```text
每天定时检查需要结单的 Standing Orders
找到 next_closing_date <= today 的 active standing orders
为每个 standing order 生成 Generated Order
复制当前产品、数量、价格快照
更新 next_closing_date
记录 job execution log
```

非常重要：生成订单时要保存快照。

因为以后产品价格可能变化，但历史订单不能被影响。

生成订单时要保存：

```text
product_name_snapshot
sku_snapshot
unit_price_snapshot
quantity
line_total
```

---

## 5.6 Order Management System

正式订单管理。

订单来源于 Standing Order 自动结单。

订单状态建议：

```text
Generated
In Production
Ready to Ship
Shipped
Invoiced
Completed
Cancelled
Needs Review
```

Admin 可以：

```text
查看订单列表
筛选订单状态
查看订单详情
修改订单状态
标记进入生产
标记准备出货
标记已出货
取消订单
```

订单列表字段：

```text
Order No.
Customer
Generated Date
Closing Frequency
Total Amount
Order Status
Invoice Status
Shipment Status
```

---

## 5.7 Production List System

生产清单系统。

这个模块用来把多个订单汇总，帮助制作产品。

Admin 点击：

```text
Generate Production List
```

系统按产品汇总：

```text
House Blend 1kg: total 28
Decaf 500g: total 6
Brazil Espresso 1kg: total 12
```

可以按这些维度筛选：

```text
日期范围
订单状态
客户
产品
是否已出货
```

生产清单状态：

```text
Draft
Confirmed
Completed
```

这个模块可以做成后台页面：

```text
Production Summary
├── Product
├── Total Quantity
├── Related Orders
├── Production Status
└── Notes
```

---

## 5.8 Fulfilment / Shipping System

出货系统。

图里提到：

```text
在系统上出货按钮
出货后自动寄送 email 帐单给客人
```

所以出货动作非常关键。

Admin 操作：

```text
Mark as Shipped
```

系统自动执行：

```text
更新订单状态为 Shipped
记录出货时间
生成或确认 Invoice
发送 Invoice Email
记录 Email Log
```

出货字段：

```text
shipping_date
delivery_method
tracking_number
delivery_note
shipped_by
shipment_status
```

第一版如果没有物流 API，可以手动填写 tracking number。
后期再接 NZ Post 或 courier API。

---

## 5.9 Invoice Management System

Invoice 管理系统。

支持：

```text
Invoice preview
Generate invoice
Issue invoice
Send invoice email
Download PDF
Customer portal view
Mark paid / unpaid
```

Invoice 生成逻辑：

```text
Order Generated
→ Admin review
→ Shipment confirmed
→ Invoice issued
→ Email sent to customer
```

Invoice 数据：

```text
Invoice No.
Customer
Order
Issue Date
Due Date
Line Items
Subtotal
GST
Total
Paid Amount
Outstanding Amount
Status
```

Invoice Preview 很重要。
在正式发送前，Admin 可以先查看：

```text
客户信息是否正确
产品数量是否正确
金额是否正确
due date 是否正确
```

---

## 5.10 Payment & Statement System

付款和 Statement 系统。

你图里是：

```text
付款
人工查核
收到款系统可以点选已付款
未收到款
未收到款的每周寄 statement
```

这说明第一版不是自动在线支付，而是人工查核。

付款流程：

```text
客户收到 invoice
客户通过银行转账或其他方式付款
Admin 查看银行到账
Admin 在系统中标记 Paid
系统记录付款信息
```

Payment Record 字段：

```text
invoice_id
amount
payment_date
payment_method
reference
marked_by
note
```

Statement 系统：

```text
每周检查 unpaid / overdue invoices
按客户汇总 outstanding amount
生成 statement
发送 statement email
记录发送日志
```

Statement 内容：

```text
客户名称
Statement Date
Unpaid Invoices
Invoice Date
Due Date
Amount
Outstanding Amount
Total Outstanding
```

---

# 6. 页面设计

## 6.1 Customer Portal 页面

客户登录后看到的系统。

页面结构：

```text
Customer Dashboard
Standing Order
Orders
Invoices
Statements
Account Settings
```

---

### Customer Dashboard

显示：

```text
Welcome, Auckland Cafe
Standing Order Status
Next Closing Date
Last Generated Order
Outstanding Balance
Recent Invoices
```

按钮：

```text
Edit Standing Order
View Invoices
View Statements
```

---

### Standing Order Page

显示：

```text
Current Standing Order
Closing Frequency
Next Closing Date
Items
Quantity
Unit Price
Estimated Total
Delivery Notes
```

操作：

```text
Add Item
Remove Item
Change Quantity
Save Changes
Pause Standing Order
```

---

### Orders Page

显示历史正式订单：

```text
Order No.
Generated Date
Status
Total
Invoice Status
```

---

### Invoices Page

显示：

```text
Invoice No.
Issue Date
Due Date
Total
Paid Amount
Outstanding
Status
Download PDF
```

---

### Statements Page

显示：

```text
Statement Date
Total Outstanding
Download Statement
```

---

## 6.2 Admin Dashboard 页面

Admin 后台结构：

```text
Dashboard
Customers
Products
Standing Orders
Orders
Production List
Shipments
Invoices
Payments
Statements
Settings
```

---

### Admin Dashboard

显示：

```text
Orders generated this week
Orders in production
Orders shipped
Invoices unpaid
Total outstanding amount
Customers with overdue invoices
```

---

### Customers

功能：

```text
Create Customer
Edit Customer
Send Invite
View Standing Order
View Invoices
View Payments
```

---

### Standing Orders

功能：

```text
View all standing orders
Filter by frequency
Filter by active / paused
View next closing date
Manually generate order
```

---

### Orders

功能：

```text
View generated orders
Update status
Send to production
Mark ready to ship
Mark shipped
Generate invoice
```

---

### Production List

功能：

```text
Generate production summary
Group order items by product
Export production list
Mark production completed
```

---

### Invoices

功能：

```text
Preview invoice
Issue invoice
Send invoice email
Download PDF
Mark paid
Cancel invoice
```

---

### Payments

功能：

```text
View unpaid invoices
Record payment
Mark invoice paid
View payment history
```

---

### Statements

功能：

```text
Generate weekly statements
Preview statement
Send statement email
View statement history
```

---

# 7. 数据库设计

下面是核心表设计。

---

## UserAccount

```text
id
email
password_hash
role: Customer / Admin
customer_id nullable
status
last_login_at
created_at
updated_at
```

---

## Customer

```text
id
business_name
contact_person
email
phone
billing_address
delivery_address
payment_terms
account_status
created_at
updated_at
```

---

## Product

```text
id
sku
name
description
unit
price
cost
is_active
created_at
updated_at
```

---

## StandingOrder

```text
id
customer_id
frequency: Weekly / Fortnightly / Monthly / ManualOnly
next_closing_date
status: Active / Paused / Cancelled
delivery_notes
internal_notes
created_at
updated_at
```

---

## StandingOrderItem

```text
id
standing_order_id
product_id
quantity
unit_price
notes
created_at
updated_at
```

---

## GeneratedOrder

```text
id
order_number
customer_id
standing_order_id
generated_at
order_status
subtotal
gst_amount
total_amount
invoice_status
shipment_status
created_at
updated_at
```

---

## GeneratedOrderItem

```text
id
order_id
product_id
product_name_snapshot
sku_snapshot
quantity
unit_price_snapshot
line_total
notes
```

---

## Shipment

```text
id
order_id
shipment_status
shipped_at
delivery_method
tracking_number
delivery_notes
shipped_by
created_at
updated_at
```

---

## Invoice

```text
id
invoice_number
customer_id
order_id
issue_date
due_date
subtotal
gst_amount
total_amount
paid_amount
outstanding_amount
status: Draft / Issued / Sent / PartiallyPaid / Paid / Overdue / Cancelled
pdf_url
created_at
updated_at
```

---

## InvoiceItem

```text
id
invoice_id
description
quantity
unit_price
line_total
```

---

## PaymentRecord

```text
id
invoice_id
amount
payment_date
payment_method
reference
marked_by
note
created_at
```

---

## Statement

```text
id
customer_id
statement_date
total_outstanding
pdf_url
email_sent_at
created_at
```

---

## StatementItem

```text
id
statement_id
invoice_id
invoice_number
due_date
outstanding_amount
```

---

## EmailLog

```text
id
recipient
subject
email_type
related_entity_type
related_entity_id
status
sent_at
error_message
```

---

# 8. 技术实现建议

## Backend

建议：

```text
ASP.NET Core Web API
Entity Framework Core
PostgreSQL
Background Jobs
JWT / Cookie Authentication
Role-based Access Control
```

---

## Scheduled Jobs

周期性结单和 Statement 需要后台任务。

可以用：

```text
Quartz.NET
Hangfire
ASP.NET Core BackgroundService
```

任务包括：

```text
GenerateOrdersJob
SendInvoiceEmailJob
GenerateWeeklyStatementsJob
MarkOverdueInvoicesJob
```

---

## 文件存储

不要把 PDF invoice、statement 文件存在数据库里。

建议：

```text
PostgreSQL:
订单、客户、invoice、付款记录、状态数据

S3 / object storage:
Invoice PDF
Statement PDF
导出文件
邮件附件
```

---

## Email

需要邮件服务。

可以用：

```text
AWS SES
SendGrid
Resend
Mailgun
```

邮件类型：

```text
Account invitation
Invoice email
Statement email
Password reset
Order generated notification
```

---

## Deployment

建议：

```text
Docker
AWS ECS / App Runner
AWS RDS PostgreSQL
S3
CloudWatch
CI/CD
```

部署结构：

```text
Frontend / Admin UI
→ Backend API
→ PostgreSQL RDS
→ S3 for PDF files
→ Email service
→ CloudWatch logs
```

---

# 9. 关键状态流

## Standing Order 状态

```text
Active
Paused
Cancelled
```

---

## Generated Order 状态

```text
Generated
In Production
Ready to Ship
Shipped
Invoiced
Completed
Cancelled
Needs Review
```

---

## Invoice 状态

```text
Draft
Issued
Sent
Partially Paid
Paid
Overdue
Cancelled
```

---

## Payment 状态

```text
Unpaid
Partially Paid
Paid
Overdue
```

---

# 10. 重要规则

## 规则 1：Standing Order 不是正式订单

客户修改的是 Standing Order。
系统结单后生成的才是 Generated Order。

---

## 规则 2：订单生成后要保存快照

订单中的产品名称、SKU、单价必须保存快照，防止未来产品价格变化影响历史订单。

---

## 规则 3：Invoice 发送前必须可以预览

Admin 应该可以检查 invoice 内容，再正式发送给客户。

---

## 规则 4：付款第一版可以人工标记

不用一开始就做银行自动对账。
先做：

```text
Admin 手动标记 Paid
记录付款金额、日期、方式、备注
```

---

## 规则 5：未付款客户每周生成 Statement

系统每周自动查找 unpaid / overdue invoices，然后按客户生成 statement。

---

## 规则 6：所有关键操作要有日志

尤其是：

```text
生成订单
修改订单
生成 invoice
发送 invoice
标记付款
取消 invoice
发送 statement
```

建议加 Audit Log。

---

# 11. MVP 范围

第一版建议做这些：

```text
客户账号登录
Admin 创建客户
Standing Order 管理
结单频率设置
自动生成订单
订单列表
生产清单
出货按钮
Invoice 预览
Invoice PDF 生成
Invoice Email
手动标记付款
未付款 Statement
基础后台 Dashboard
AWS 部署
```

第一版暂时不做：

```text
线上支付
银行自动对账
Xero 集成
NZ Post API
高级库存预测
复杂销售分析
手机 App
```

---

# 12. 最终一句话总结

这套系统的核心是：

```text
让 StoryCoffee 把线下商业客户的固定订购、周期性结单、生产出货、Invoice 和收款追踪流程系统化。
```

更技术一点说：

```text
这是一个基于客户账号、周期性订单生成、Invoice 管理、付款状态跟踪和 Statement 自动化的 B2B 订单运营平台。
```
