## StoryCoffee Phase 1 实施计划（Vertical Slice）

### Summary
- 在 `/Users/carashang/auckland/Project/coffee` 建立 monorepo：`frontend + backend + infra + docs`，并把 `/Users/carashang/Downloads/实现功能 (2).zip` 的现有前端迁入 `frontend` 作为基线。
- 保留现有 Figma 生成页面外观，内部改造为真实 API 驱动；所有用户可见文案统一为英文。
- 交付首个端到端流程：`JWT 登录 → Admin 订单流转 → 发票状态联动`，并提供 `Customer` 只读订单查看。
- 交付可运行的本地 Docker Compose + 可部署的 dev Helm/K8s manifests。
- 采用 `.NET 8 + PostgreSQL + OpenAPI typed client`；Quartz 仅做接口与占位，不做真实调度执行。

### Implementation Changes
- **Repo & structure**
  - 初始化根目录工作区（前后端分离、统一脚本、环境变量模板、文档归档）。
  - 前端迁入 `/Users/carashang/auckland/Project/coffee/frontend`，后端新建 `/Users/carashang/auckland/Project/coffee/backend`，部署文件放 `/Users/carashang/auckland/Project/coffee/infra`。
- **Backend (Orders Core)**
  - 建立认证与角色：`Admin`、`Customer`，JWT 登录与鉴权中间件。
  - 建立最小业务模型与迁移：`users/customers/orders/order_items/invoices`（含状态字段与审计时间戳）。
  - 实现订单动作型应用服务与状态机校验：`SendToProduction`、`MarkReadyToShip`、`MarkShipped`、`GenerateInvoice`、`SendInvoice`、`CancelOrder`。
  - 实现 Customer 数据隔离：Customer 仅可访问 `customer_id` 归属订单。
  - 邮件/PDF/Quartz/Redis 保留接口与 DI 占位（stub）。
- **Frontend**
  - 登录页改为真实 `/api/auth/login`，保存 token，按 role 跳转与路由守卫。
  - `Admin Orders` 页面替换 `mockData`：读取真实列表、触发动作接口、更新状态与 toast。
  - `Customer Orders` 页面改为只读真实数据（仅当前客户）。
  - 抽离 API 层、鉴权拦截器、错误处理与 loading/empty/error 状态；保留当前 UI 样式结构。
- **Infra & Deployment**
  - 本地：`docker-compose` 启动 `frontend + api + postgres (+redis)`，包含健康检查与依赖等待。
  - K8s dev：提供 Helm chart（deployment/service/config/secret/probes/ingress 基础项）可部署前后端与数据库依赖配置。
  - 增加初始化流程：数据库迁移 + seed demo 数据（admin/customer/demo orders）。

### Public APIs / Interfaces / Types
- **Auth**
  - `POST /api/auth/login`：返回 `accessToken`, `expiresIn`, `role`, `userProfile`。
- **Admin Orders**
  - `GET /api/admin/orders`
  - `POST /api/admin/orders/{id}/send-to-production`
  - `POST /api/admin/orders/{id}/mark-ready-to-ship`
  - `POST /api/admin/orders/{id}/mark-shipped`
  - `POST /api/admin/orders/{id}/generate-invoice`
  - `POST /api/admin/orders/{id}/send-invoice`
  - `POST /api/admin/orders/{id}/cancel`
- **Customer Orders**
  - `GET /api/customer/orders`（后端强制按登录客户过滤）
- **Status contracts（与 v1.1 文档对齐）**
  - `OrderStatus`: `Generated | InProduction | ReadyToShip | Shipped | Completed | Cancelled`
  - `InvoiceStatus`: `NotIssued | Draft | Issued | Unpaid | PartiallyPaid | Paid | Overdue | Cancelled`
  - `EmailStatus`: `NotSent | Pending | Sent | Failed | Bounced`
- **Contract management**
  - 后端导出 OpenAPI；前端基于 OpenAPI 生成/维护 typed client，替换手写 mock types 为 API contract 来源。

### Test Plan
- **Backend unit tests**
  - 状态流转合法路径与非法路径（例如 `Cancelled` 后不可再发货）。
  - 发货后发票状态联动规则（`NotIssued -> Draft`）与重复动作幂等保护。
  - 角色与数据权限（Admin 全量、Customer 仅自己）。
- **Backend integration tests**
  - 登录签发 JWT、鉴权失败场景、关键订单动作接口 2xx/4xx 行为。
  - 种子数据加载后，`GET /api/admin/orders` 与 `GET /api/customer/orders` 返回正确范围。
- **Frontend tests**
  - 登录成功跳转、未登录拦截、角色路由保护。
  - Orders 页面动作按钮触发 API 后 UI 状态更新与错误提示。
- **Deployment checks**
  - Compose 一键启动与健康检查通过。
  - Helm dev values 部署后，前后端服务可互通并访问基础健康端点。

### Assumptions & Defaults
- 系统名固定为 **StoryCoffee**，UI 全英文；内部技术文档可保留中文。
- Phase 1 不实现真实邮件发送、PDF 生成、Quartz 定时执行、S3 上传，只提供可替换接口。
- 货币与业务语境沿用当前数据（NZ 市场）与现有状态机定义。
- 目标是“可开发、可演示、可部署的第一条真实业务链路”，非一次性完成全部 PRD 页面与全部后端模块。
