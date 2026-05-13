# 前端代码变更说明 v1.1

**日期：** 2026-05-08  
**版本：** 1.1  
**目的：** 前后端枚举类型对齐

---

## 📋 变更概述

本次更新完成了前端代码与后端 API 契约的枚举类型对齐，确保前后端使用一致的状态定义。

---

## ✅ 已完成的修改

### 1. 类型定义文件 (`src/entities/types.ts`)

#### 修改 OrderStatus
**移除状态：**
```typescript
// ❌ 移除
'Invoiced'      // 改用 invoice_status 字段表达
'NeedsReview'   // 第一版不实现
```

**最终定义：**
```typescript
export type OrderStatus =
  | 'Generated'
  | 'InProduction'
  | 'ReadyToShip'
  | 'Shipped'
  | 'Completed'
  | 'Cancelled';
```

#### 修改 EmailStatus
**新增状态：**
```typescript
// ✅ 新增
'Pending'       // 邮件发送中
```

**最终定义：**
```typescript
export type EmailStatus =
  | 'NotSent'
  | 'Pending'   // ← 新增
  | 'Sent'
  | 'Failed'
  | 'Bounced';
```

#### InvoiceStatus 保持不变
```typescript
export type InvoiceStatus =
  | 'NotIssued'
  | 'Draft'
  | 'Issued'          // 已包含
  | 'Unpaid'
  | 'PartiallyPaid'
  | 'Paid'
  | 'Overdue'
  | 'Cancelled';
```

---

### 2. Status formatting file (`src/shared/status/statusFormat.ts`)

#### 更新 formatOrderStatus
**移除：**
```typescript
Invoiced: 'Invoiced',
NeedsReview: 'Needs Review',
```

**最终版本：**
```typescript
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
```

#### 更新 getOrderStatusColor
**移除：**
```typescript
Invoiced: '#673AB7',
NeedsReview: '#FFC107',
```

**最终版本：**
```typescript
export const getOrderStatusColor = (status: OrderStatus): string => {
  const colors: Record<OrderStatus, string> = {
    Generated: '#9E9E9E',      // 灰色
    InProduction: '#FF9800',   // 橙色
    ReadyToShip: '#2196F3',    // 蓝色
    Shipped: '#4CAF50',        // 绿色
    Completed: '#009688',      // 青色
    Cancelled: '#F44336',      // 红色
  };
  return colors[status];
};
```

#### 新增 EmailStatus 格式化函数
```typescript
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
    NotSent: '#BDBDBD',        // 浅灰
    Pending: '#FF9800',        // 橙色
    Sent: '#4CAF50',           // 绿色
    Failed: '#F44336',         // 红色
    Bounced: '#9E9E9E',        // 灰色
  };
  return colors[status];
};
```

---

### 3. PRD 文档更新 (PRD-StoryCoffee-B2B系统.md)

#### 新增版本说明章节
- ✅ 添加"前后端对齐说明（v1.1 更新）"章节
- ✅ 说明主要变更原因和影响
- ✅ 更新版本号为 1.1

#### 更新数据模型章节
- ✅ 更新 OrderStatus 类型定义
- ✅ 更新 EmailStatus 类型定义
- ✅ 更新状态 Chip 颜色规范
- ✅ 添加 EmailStatus 颜色映射

---

## 🔍 需要注意的事项

### 1. OrderStatus 变更影响

**移除的状态处理：**
- `'Invoiced'` → 使用 `order.invoice_status` 字段判断开票状态
- `'NeedsReview'` → 第一版不实现此功能

**示例代码调整：**
```typescript
// ❌ 旧代码
if (order.orderStatus === 'Invoiced') {
  // ...
}

// ✅ 新代码
if (order.invoiceStatus === 'Issued' || order.invoiceStatus === 'Unpaid') {
  // ...
}
```

### 2. EmailStatus 新增状态

**'Pending' 状态使用场景：**
- 邮件正在发送中
- 邮件已加入发送队列但结果未返回

**示例使用：**
```typescript
// 发送邮件时显示 Pending 状态
<Chip 
  label={formatEmailStatus('Pending')}
  sx={{ bgcolor: getEmailStatusColor('Pending'), color: 'white' }}
/>
```

### 3. ProductionBatch 第一版策略

**后端实现：**
- 数据库保留 `production_batches` 表
- API: `GET /api/admin/production/current` 返回当前批次

**前端实现：**
- 第一版隐藏批次概念
- 继续使用扁平化的 `ProductionItem[]`
- API 调用改为 `/api/admin/production/current`

**无需修改：**
- ✅ `src/pages/admin/ProductionPage.tsx` 无需改动
- ✅ `ProductionItem` 接口定义保持不变

---

## 📦 影响范围

### 已修改文件
✅ `src/entities/types.ts`  
✅ `src/shared/status/statusFormat.ts`  
✅ `PRD-StoryCoffee-B2B系统.md`

### 无需修改文件
✅ `src/pages/admin/OrdersPage.tsx` - OrderStatus 使用保持兼容  
✅ `src/pages/admin/ProductionPage.tsx` - 无需改动  
✅ 所有其他页面组件 - 格式化函数签名未变

---

## Phase 5 实施补充：Standing Order Core

**日期：** 2026-05-11

### 已完成
- 后端新增 `products`、`standing_orders`、`standing_order_items` 核心模型、seed 数据与 API DTO。
- 新增 authenticated product catalog API：`GET /api/products`，供 Admin 与 Customer 共同读取。
- 新增 Admin standing order API：`GET /api/admin/standing-orders`、`POST /api/admin/standing-orders/{id}/generate-now`。
- 新增 Customer standing order API：`GET /api/customer/standing-order`、`PUT /api/customer/standing-order`，后端按 JWT customerId 强制隔离。
- 前端 Admin Products、Admin Customers、Admin Standing Orders、Customer Standing Order 页面切换为真实 API 数据。
- 手动生成 standing order 会创建真实 `Generated` order，并按 frequency 推进 `nextClosingDate`。

### 暂不实现
- 自动 Quartz 定时生成 standing order。
- Product/customer 的完整 CRUD 管理流程。

---

## Phase 6 实施补充：Customer Account Management

**日期：** 2026-05-11

### 已完成
- 新增 Admin Customers API：`GET /api/admin/customers/{id}`、`POST /api/admin/customers`、`PATCH /api/admin/customers/{id}`。
- 新增 Customer Profile API：`GET /api/customer/profile`、`PUT /api/customer/profile`，后端从 JWT customerId 读取归属客户。
- Admin Customers 列表创建动作改为真实 API 持久化。
- Admin Customer Detail 页面改为真实 API 数据，并聚合该客户的 orders、invoices、standing order。
- Customer Account Settings 页面改为真实 profile 数据，可更新业务联系信息；payment terms 与 account status 仍由 Admin 控制。

### 暂不实现
- Customer invite email 实际发送。

---

## Phase 7 实施补充：Dashboard + Product CRUD

**日期：** 2026-05-11

### 已完成
- Admin Dashboard 改为读取真实 `orders`、`invoices`、`customers` API 数据，不再使用 mock arrays。
- Customer Dashboard 改为读取真实 standing order 与 customer invoices API 数据。
- 新增 Admin Products API：`POST /api/admin/products`、`PATCH /api/admin/products/{id}`。
- Admin Products 页面新增 Add/Edit dialog，支持 SKU、name、description、unit、price、cost、active 状态维护。
- Product 更新只改变 product catalog，不回写历史 order item snapshot。

### 暂不实现
- Product 删除；当前用 `isActive=false` 下架。
- Product price book / customer-specific pricing。
- Dashboard 专用聚合 API；当前由前端组合现有 API。

---

## Phase 8 实施补充：PDF Download Stub

**日期：** 2026-05-11

### 已完成
- Invoice 新增 PDF metadata：`pdf_file_key`、`pdf_generated_at`。
- Statement 新增 PDF metadata：`pdf_file_key`、`pdf_generated_at`。
- 新增 Admin Invoice PDF API：`GET /api/admin/invoices/{id}/download-url`、`GET /api/admin/invoices/{id}/download`。
- 新增 Customer Invoice PDF API：`GET /api/customer/invoices/{id}/download-url`、`GET /api/customer/invoices/{id}/download`，后端按 JWT customerId 隔离。
- 新增 Admin/Customer Statement PDF download-url 与 download API。
- 前端 Invoice/Statement download buttons 改为真实 authenticated API 下载。
- Draft invoice 生成 PDF 后进入 `Issued`，并同步 related order invoice status。

### 暂不实现
- QuestPDF 正式版式。
- S3/MinIO/LocalStack 上传与 presigned URL。
- PDF 生成相关 EmailLog。

---

## Phase 9 实施补充：AuditLog + EmailLog

**日期：** 2026-05-11

### 已完成
- 新增 `audit_logs`、`email_logs` 数据模型与 EF 配置。
- 新增 Admin Logs API：`GET /api/admin/logs/audit`、`GET /api/admin/logs/email`。
- 关键业务动作写入 AuditLog：customer/product create/update、standing order update/generate、order transitions、invoice PDF/email/payment、statement generate/PDF/email。
- Invoice/Statement email 发送动作写入 EmailLog。
- 前端新增 Admin Logs 页面，可查看 Audit Logs 与 Email Logs。

### 暂不实现
- 真实邮件失败回执 / bounce webhook。
- 日志保留策略。

## Phase 10 实施补充：Log Query Operations

**日期：** 2026-05-11

### 已完成
- Admin Audit Logs API 支持 `search`、`action`、`entityType`、`from`、`to`、`page`、`pageSize` 查询参数。
- Admin Email Logs API 支持 `search`、`entityType`、`status`、`from`、`to`、`page`、`pageSize` 查询参数。
- 新增 CSV 导出 API：`GET /api/admin/logs/audit/export`、`GET /api/admin/logs/email/export`。
- Admin Logs 页面新增筛选表单、分页控件与 CSV 导出按钮。

### 暂不实现
- 日志归档、自动清理与长期保留策略。
- 高级审计 diff 与字段级变更对比。

## Phase 11 实施补充：Audit Change Details

**日期：** 2026-05-11

### 已完成
- `audit_logs` 新增 `old_values`、`new_values` 变更快照字段，并通过 API 返回。
- Customer create/update、Customer profile update、Product create/update 写入 old/new JSON 快照。
- Customer standing order update 写入 frequency、delivery notes、items 的 old/new JSON 快照。
- Admin Logs 页面展示 AuditLog change details，CSV 导出包含 old/new values。

### 暂不实现
- 字段级 diff 高亮与敏感字段脱敏规则。
- 订单、发票、付款等 workflow audit 的完整 old/new 快照。

## Phase 12 实施补充：Standing Order Lifecycle

**日期：** 2026-05-11

### 已完成
- 新增 Admin standing order lifecycle API：`POST /api/admin/standing-orders/{id}/pause`、`resume`、`cancel`。
- 后端状态校验：Active 可 pause/cancel，Paused 可 resume/cancel，Cancelled 不可恢复或生成订单。
- 状态动作写入 AuditLog old/new 快照。
- Admin Standing Orders 页面新增 Pause、Resume、Cancel 操作，并禁用非 Active standing order 的 Generate Now。

### 暂不实现
- 自动 Quartz 定时生成 standing order。

## Phase 13 实施补充：Admin Standing Order Create/Edit

**日期：** 2026-05-11

### 已完成
- 新增 Admin standing order 创建 API：`POST /api/admin/standing-orders`。
- 新增 Admin standing order 编辑 API：`PATCH /api/admin/standing-orders/{id}`。
- 创建与编辑支持 customer、frequency、next closing date、status、delivery notes、internal notes、items。
- 创建与编辑动作写入 AuditLog old/new 快照。
- Admin Standing Orders 页面新增 Add/Edit dialog，可维护 standing order 明细。

### 暂不实现
- 自动 Quartz 定时生成 standing order。
- 多 standing order per customer 的正式业务策略；当前阻止同一客户存在多个未取消 standing order。

## Phase 14 实施补充：Standing Order Scheduled Generation

**日期：** 2026-05-11

### 已完成
- `IStandingOrderJob` 从 stub 改为真实 scheduled generation job。
- 自动生成规则：只处理 `Active`、`NextClosingDate <= now`、`Frequency != ManualOnly` 的 standing order。
- job 成功生成订单后沿用现有 `GenerateOrderNow` 逻辑推进 `nextClosingDate`。
- 新增 `job_execution_logs` 模型，记录 job name、status、processed/succeeded/failed 数量、错误信息。
- 新增 Admin job API：`POST /api/admin/jobs/standing-orders/run`、`GET /api/admin/jobs/executions`。
- 新增 hosted worker，使用 `Quartz:Enabled` 与 `Quartz:StandingOrderIntervalMinutes` 配置控制本地调度执行。

### 暂不实现
- Quartz.NET package trigger 与 distributed lock。
- job execution log 前端管理页面。

## Phase 15 实施补充：Customer Password Change

**日期：** 2026-05-11

### 已完成
- 新增 Customer password API：`POST /api/customer/password`。
- 后端校验当前密码、新密码长度、确认密码一致，并使用现有 PBKDF2 hasher 更新密码。
- 密码修改写入 AuditLog：`ChangedPassword`。
- Customer Account Settings 页面启用 Change Password 表单；成功后清空 session 并跳回登录页。

### 暂不实现
- Admin 重置客户密码。
- 密码复杂度策略、历史密码限制、强制登出所有设备。

## Phase 16 实施补充：P0/P1 Business Operations Batch

**日期：** 2026-05-11

### 已完成
- 新增 Customer invite flow：`POST /api/admin/customers/{id}/send-invite`，Draft customer 自动进入 `Invited`，并写入 `EmailLog` 与 `AuditLog`。
- 新增 Admin/Customer dashboard aggregate API：`GET /api/admin/dashboard`、`GET /api/customer/dashboard`，前端 dashboard 不再组合多个列表 API。
- 新增 Product archive API：`POST /api/admin/products/{id}/archive`，前端 Products 页面提供确认后归档。
- 新增 payment void flow：`POST /api/admin/invoices/{invoiceId}/payments/{paymentId}/void`，void 后重算 paid/outstanding/status 并写入审计日志。
- 新增 overdue invoice 标记 API：`POST /api/admin/invoices/mark-overdue`，Payments 页面可手动触发。
- Invoice PDF 内容扩展为正式业务版式基础：品牌、地址、item lines、totals、payment terms。
- Customer login 增加 suspended/archived account 阻断。

### 暂不实现
- 真实 SMTP/provider 发送、bounce webhook。
- S3/MinIO/LocalStack 对象存储与 presigned URL。
- Quartz.NET package trigger 与 distributed lock。

## Phase 17 实施补充：Backend Structure + Database Foundation

**日期：** 2026-05-12

### 已完成
- 新增 EF Core migration：`InitialCreate`，正式生成 PostgreSQL 表结构与 `AppDbContextModelSnapshot`。
- API startup 对关系型数据库改为执行 pending migrations；测试环境继续使用 InMemory `EnsureCreated`。
- 新增数据库文档：`docs/database-schema.md`，覆盖表、字段、关系、索引、状态枚举与 seed data。
- 后端目录拆分为 `Auth`、`Models`、`Data`、`Interfaces`、`Services`、`Extensions`、`Migrations`。
- 将 entity model 从单个 `Entities.cs` 拆成独立模型文件。
- 将 service interface 从 service implementation 文件中拆出到 `Interfaces`。
- 将 `CatalogService` 从 `StandingOrderService.cs` 中拆出，减少混合职责。
- DI、JSON enum serialization、CORS、Swagger、DbContext 注册移动到 `ServiceCollectionExtensions`。

### 待后续重构
- 按业务域继续拆分 DTO namespace。
- 将更多 service 内部 EF Core 访问迁移到 repository/use case 层。

## Phase 18 实施补充：Controllers + Middleware + UseCase Foundation

**日期：** 2026-05-12

### 已完成
- `Program.cs` 从 1000+ 行 route handler 收敛为 startup/bootstrap 文件。
- 全部现有 API 路径迁移到 `Controllers`，保持前端路径兼容。
- 新增 `StoryCoffeeController`，集中处理角色校验、当前用户、当前客户解析。
- 新增 `ApiExceptionMiddleware`，统一处理 `ApiException`、`KeyNotFoundException`、`InvalidOperationException` 与未处理异常。
- 新增 `JwtAuthenticationMiddleware`，替代 `Program.cs` 内联 JWT 解析。
- 新增 `DocumentRenderingService`，承接 PDF bytes 与 CSV export 生成逻辑。
- 新增 `Options/JwtOptions`，`JwtTokenService` 改为 typed options 注入。
- 新增 `Repositories/IUserRepository`、`Repositories/EfUserRepository`。
- 新增 `UseCases/AuthenticationUseCase`，登录与 customer password change 迁入 use case 层。

### 待后续重构
- 将 Billing、StandingOrder、Statement 等 service 逐步拆为 use case + repository。
- 增加 controller-level integration tests 覆盖关键错误响应 code。

### 潜在需要检查的地方
⚠️ 搜索代码中是否有硬编码 `'Invoiced'` 或 `'NeedsReview'`  
⚠️ 确认所有使用 OrderStatus 的地方已移除对这两个状态的引用

---

## 🧪 测试检查清单

- [ ] TypeScript 编译无错误
- [ ] 所有状态 Chip 显示正确
- [ ] Orders 页面状态流转正常
- [ ] Invoices 页面状态显示正确
- [ ] Statements 页面 EmailStatus 显示正确
- [ ] Production List 页面正常工作

---

## 📚 相关文档

- **前端开发 PRD**: `PRD-StoryCoffee-B2B系统.md`
- **后端规格文档**: `Backend-Spec-StoryCoffee-B2B系统.md`
- **项目总览**: `README.md`

---

## 🤝 团队协作

### 前端开发者
- ✅ 代码已更新，可以直接使用
- ⚠️ 请检查自己负责的页面是否有使用被移除的状态
- 📘 参考 PRD 文档中的状态流转图

### 后端开发者
- ✅ 按照 `Backend-Spec-StoryCoffee-B2B系统.md` 实现 API
- ✅ 确保返回的枚举值与前端定义完全一致
- 🔄 Production API 需返回当前批次的扁平化数据

### 测试人员
- 📋 参考本文档的测试检查清单
- 🔍 重点测试状态流转和显示

---

**变更完成日期：** 2026-05-08  
**审核状态：** 已完成  
**下一步：** 开始前后端集成开发
