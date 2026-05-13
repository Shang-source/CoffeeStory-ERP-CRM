# StoryCoffee B2B 系统 Engineering Spec v1.1

## 0. 文档目的

本文档是 StoryCoffee B2B 订单与发票管理系统的第三份开发文档，重点说明工程实现、自动化测试、本地开发环境、Docker、Kubernetes、Helm、Terraform、GitHub Actions CI/CD、AWS 部署、日志、监控、环境变量、Secret 管理和版本对齐规则。

三份文档的关系如下：

```text
1. Product PRD
   说明系统要做什么：业务流程、页面、角色、交互、前端原型。

2. Backend & Database Spec
   说明后端怎么做：数据库、API、状态机、权限、Quartz、Redis、PDF、Email、AuditLog。

3. Engineering Spec
   说明系统如何开发、测试、构建、部署、运行和监控。
```

本文档默认采用以下工程选择：

```text
Frontend package manager: pnpm
Backend runtime: .NET 8 LTS
Frontend API path: /api
Health endpoints: /health and /ready
PostgreSQL: 16
Redis: 7
Deployment target: Docker Compose for local, Kubernetes / AWS EKS for cloud
```

如果后端团队最终选择 .NET 9 或 .NET 10，需要全局同步修改 Dockerfile、GitHub Actions、运行时镜像、SDK 版本和部署文档。

---

# 1. 工程目标

本系统的工程目标是构建一个可本地运行、可自动测试、可容器化、可部署到 Kubernetes / AWS EKS 的 full-stack B2B 系统。

系统需要支持：

```text
本地 Docker Compose 一键启动
前端和后端独立开发
PostgreSQL 和 Redis 本地运行
PDF / S3 / Email 本地可模拟
后端自动化测试
前端自动化测试
Playwright E2E 测试
k6 性能测试
Docker 镜像构建
Kubernetes 部署
Helm 模板化部署
Terraform 管理 AWS 基础设施
GitHub Actions 自动构建、测试、部署
CloudWatch 日志和监控
健康检查 /health /ready
环境变量和 Secret 管理
版本号、构建时间、Git Commit 注入
```

---

# 2. 推荐仓库结构

建议使用 monorepo，方便统一管理前端、后端、基础设施、测试和文档。

```text
storycoffee-b2b/
├── frontend/
│   ├── src/
│   ├── public/
│   ├── package.json
│   ├── pnpm-lock.yaml
│   ├── vite.config.ts
│   ├── Dockerfile
│   └── nginx.conf
│
├── backend/
│   ├── StoryCoffee.Api/
│   ├── StoryCoffee.Application/
│   ├── StoryCoffee.Domain/
│   ├── StoryCoffee.Infrastructure/
│   ├── StoryCoffee.Tests.Unit/
│   ├── StoryCoffee.Tests.Integration/
│   ├── StoryCoffee.sln
│   └── Dockerfile
│
├── e2e/
│   ├── playwright.config.ts
│   └── tests/
│
├── performance/
│   └── k6/
│       ├── dashboard-load-test.js
│       ├── orders-load-test.js
│       └── payments-load-test.js
│
├── infra/
│   ├── docker-compose.yml
│   ├── docker-compose.test.yml
│   ├── helm/
│   │   └── storycoffee/
│   ├── k8s/
│   │   ├── dev/
│   │   └── staging/
│   └── terraform/
│       ├── environments/
│       │   ├── dev/
│       │   ├── staging/
│       │   └── prod/
│       └── modules/
│
├── docs/
│   ├── PRD-StoryCoffee-B2B系统.md
│   ├── Backend-Spec-StoryCoffee-B2B系统.md
│   └── Engineering-Spec-StoryCoffee-B2B系统.md
│
├── .github/
│   └── workflows/
│       ├── pull-request.yml
│       ├── deploy-dev.yml
│       └── deploy-staging.yml
│
└── README.md
```

---

# 3. 环境划分

系统至少支持三个环境。

```text
local：本地开发环境
staging：测试 / 预发布环境
prod：生产环境，第一版可以只设计，不实际启用
```

## 3.1 local

用途：

```text
开发人员本地开发
本地数据库调试
前后端联调
本地 E2E 测试
PDF / Email / S3 模拟
```

运行方式：

```text
Docker Compose
```

## 3.2 staging

用途：

```text
模拟真实部署
前后端验收
E2E 测试
性能测试
Kubernetes 学习和验证
```

运行方式：

```text
AWS EKS 或本地 Kubernetes 集群
```

## 3.3 prod

用途：

```text
真实生产环境
```

第一版可以只保留 Terraform 和 Helm 配置，不一定真实启用。

---

# 4. 本地开发环境

## 4.1 Docker Compose 目标

本地环境需要做到：

```text
一条命令启动前端、后端、PostgreSQL、Redis、S3 mock、Email mock
```

推荐命令：

```bash
docker compose -f infra/docker-compose.yml up --build
```

## 4.2 本地服务组成

```text
frontend：React + Vite 前端，使用 Nginx 提供静态文件和 /api 代理
api：ASP.NET Core Web API
postgres：PostgreSQL 主数据库
redis：Redis 缓存 / 锁 / 限流
localstack：模拟 AWS S3
mailhog：本地邮件测试工具
```

## 4.3 本地 API 访问原则

前端统一使用相对路径：

```text
/api
```

不要在构建后的前端中写死：

```text
http://localhost:8080/api
http://api:8080/api
```

原因：

```text
localhost 在浏览器、Docker 容器、Kubernetes Pod 中含义不同
api 是 Docker Compose 内部服务名，浏览器不能直接识别
/api + Nginx / Ingress 代理最稳定
```

## 4.4 docker-compose.yml 示例结构

```yaml
services:
  postgres:
    image: postgres:16
    container_name: storycoffee-postgres
    environment:
      POSTGRES_DB: storycoffee
      POSTGRES_USER: storycoffee
      POSTGRES_PASSWORD: storycoffee_password
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

  redis:
    image: redis:7
    container_name: storycoffee-redis
    ports:
      - "6379:6379"

  localstack:
    image: localstack/localstack
    container_name: storycoffee-localstack
    environment:
      SERVICES: s3
      AWS_DEFAULT_REGION: ap-southeast-2
    ports:
      - "4566:4566"

  mailhog:
    image: mailhog/mailhog
    container_name: storycoffee-mailhog
    ports:
      - "1025:1025"
      - "8025:8025"

  api:
    build:
      context: ../backend
      dockerfile: Dockerfile
      args:
        GIT_COMMIT: ${GIT_COMMIT:-local}
    container_name: storycoffee-api
    depends_on:
      - postgres
      - redis
      - localstack
      - mailhog
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_URLS: http://+:8080
      ConnectionStrings__Postgres: Host=postgres;Port=5432;Database=storycoffee;Username=storycoffee;Password=storycoffee_password
      Redis__ConnectionString: redis:6379
      Storage__Provider: LocalStack
      Storage__BucketName: storycoffee-documents-local
      Storage__ServiceUrl: http://localstack:4566
      Email__Provider: MailHog
      Email__SmtpHost: mailhog
      Email__SmtpPort: 1025
      Quartz__Enabled: "true"
    ports:
      - "8080:8080"

  frontend:
    build:
      context: ../frontend
      dockerfile: Dockerfile
      args:
        VITE_API_BASE_URL: /api
        VITE_APP_ENV: local
        VITE_VERSION: 1.1.0
        VITE_BUILD_TIME: ${BUILD_TIME:-local}
        VITE_GIT_COMMIT: ${GIT_COMMIT:-local}
    container_name: storycoffee-frontend
    depends_on:
      - api
    ports:
      - "5173:80"

volumes:
  postgres_data:
```

---

# 5. 环境变量规范

## 5.1 Backend 环境变量

```text
ASPNETCORE_ENVIRONMENT
ASPNETCORE_URLS
ConnectionStrings__Postgres
Redis__ConnectionString
Jwt__Issuer
Jwt__Audience
Jwt__Secret
Storage__Provider
Storage__BucketName
Storage__Region
Storage__ServiceUrl
Storage__AccessKey
Storage__SecretKey
Email__Provider
Email__FromAddress
Email__ApiKey
Email__SmtpHost
Email__SmtpPort
Quartz__Enabled
Serilog__MinimumLevel
GIT_COMMIT
BUILD_VERSION
```

## 5.2 Frontend 环境变量

```text
VITE_API_BASE_URL
VITE_APP_ENV
VITE_ENABLE_MOCKS
VITE_VERSION
VITE_BUILD_TIME
VITE_GIT_COMMIT
```

推荐：

```text
local / docker / k8s 都使用 VITE_API_BASE_URL=/api
```

## 5.3 Secret 管理原则

以下内容不得提交到 Git：

```text
数据库密码
JWT Secret
AWS Access Key
AWS Secret Key
Email API Key
Production connection string
```

本地开发可以使用 `.env.local`。

Kubernetes 中使用 `Secret`。

GitHub Actions 中使用 Repository Secrets。

生产环境推荐使用：

```text
AWS Secrets Manager
External Secrets Operator
```

---

# 6. Backend Dockerfile

## 6.1 目标

后端 Docker 镜像需要：

```text
使用 multi-stage build
编译 .NET 项目
发布 release build
暴露 8080 端口
支持 GIT_COMMIT 注入
```

## 6.2 默认 .NET 版本

本项目工程文档默认使用：

```text
.NET 8 LTS
```

如果项目决定使用 .NET 9 或 .NET 10，必须同步修改：

```text
Dockerfile SDK image
Dockerfile runtime image
GitHub Actions dotnet-version
README
Kubernetes image runtime assumptions
```

## 6.3 示例

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY StoryCoffee.sln ./
COPY StoryCoffee.Api/StoryCoffee.Api.csproj StoryCoffee.Api/
COPY StoryCoffee.Application/StoryCoffee.Application.csproj StoryCoffee.Application/
COPY StoryCoffee.Domain/StoryCoffee.Domain.csproj StoryCoffee.Domain/
COPY StoryCoffee.Infrastructure/StoryCoffee.Infrastructure.csproj StoryCoffee.Infrastructure/

RUN dotnet restore

COPY . .
RUN dotnet publish StoryCoffee.Api/StoryCoffee.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ARG GIT_COMMIT=unknown
ENV GIT_COMMIT=${GIT_COMMIT}
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "StoryCoffee.Api.dll"]
```

---

# 7. Frontend Dockerfile

## 7.1 目标

前端 Docker 镜像需要：

```text
使用 pnpm 安装依赖
构建 Vite 静态文件
使用 Nginx 提供静态资源
支持前端路由 fallback
支持 /api 反向代理到 backend
注入版本号、构建时间、Git Commit
```

## 7.2 Dockerfile

```dockerfile
FROM node:22-alpine AS build
WORKDIR /app

RUN npm install -g pnpm

COPY package.json pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile

COPY . .

ARG VITE_API_BASE_URL=/api
ARG VITE_APP_ENV=production
ARG VITE_VERSION=1.1.0
ARG VITE_BUILD_TIME=unknown
ARG VITE_GIT_COMMIT=unknown

ENV VITE_API_BASE_URL=${VITE_API_BASE_URL}
ENV VITE_APP_ENV=${VITE_APP_ENV}
ENV VITE_VERSION=${VITE_VERSION}
ENV VITE_BUILD_TIME=${VITE_BUILD_TIME}
ENV VITE_GIT_COMMIT=${VITE_GIT_COMMIT}

RUN pnpm build

FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

## 7.3 nginx.conf

本地 Docker Compose 中，Nginx 可以代理到 `api:8080`。

```nginx
server {
    listen 80;
    server_name _;

    root /usr/share/nginx/html;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }

    location /api/ {
        proxy_pass http://api:8080/api/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

Kubernetes 中可以选择：

```text
方案 A：前端 Nginx 继续代理 /api 到 backend service
方案 B：Ingress 直接把 /api 路由到 backend service
```

第一版推荐方案 B，由 Ingress 统一路由。

---

# 8. 健康检查

健康检查路径统一为：

```text
/health
/ready
```

不要使用：

```text
/api/health
/api/ready
```

## 8.1 GET /health

用途：

```text
判断应用进程是否存活
Kubernetes livenessProbe 使用
```

只要 API 进程正常运行，就返回 200。

Response:

```json
{
  "status": "Healthy",
  "version": "1.1.0",
  "commit": "abc123"
}
```

## 8.2 GET /ready

用途：

```text
判断应用是否准备好接收流量
Kubernetes readinessProbe 使用
```

需要检查：

```text
PostgreSQL 是否连接成功
Redis 是否连接成功
S3 配置是否可用，可选
```

Response:

```json
{
  "status": "Ready",
  "dependencies": {
    "postgres": "Healthy",
    "redis": "Healthy"
  },
  "version": "1.1.0",
  "commit": "abc123"
}
```

---

# 9. 前后端版本对齐 v1.1

## 9.1 枚举类型统一

后端、前端、测试数据必须使用同一套枚举。

### OrderStatus

```text
Generated
InProduction
ReadyToShip
Shipped
Completed
Cancelled
```

第一版不使用：

```text
Invoiced
NeedsReview
```

### InvoiceStatus

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

状态含义：

```text
Draft：Invoice 已创建，但 PDF 未正式生成
Issued：PDF 已生成，但尚未发送给客户
Unpaid：Invoice 已发送给客户，等待付款
```

### EmailStatus

```text
NotSent
Pending
Sent
Failed
Bounced
```

说明：

```text
Invoice / Statement 可以是 NotSent
EmailLog 通常从 Pending 开始，然后变 Sent / Failed / Bounced
```

## 9.2 ProductionBatch 对齐

第一版前端不展示 ProductionBatch。

后端保留 ProductionBatch，并提供：

```text
GET /api/admin/production/current
```

前端 Production List 页面使用该接口返回的当前批次聚合数据。

## 9.3 Statement 快照对齐

后端保存 `statement_invoices` 快照。

前端可以继续显示：

```text
statement.invoices
```

但这些 invoices 是历史快照 DTO，不是实时 Invoice。

## 9.4 测试要求

所有测试数据必须使用 v1.1 枚举。

E2E 和集成测试必须验证：

```text
API 返回的枚举值与前端 types.ts 一致
前端不再使用 Invoiced / NeedsReview
Invoice 可以从 Draft → Issued → Unpaid
EmailStatus 可以从 Pending → Sent / Failed
```

---

# 10. 自动化测试策略

自动化测试分为六层。

```text
Backend Unit Tests
Backend Integration Tests
Quartz Job Tests
Frontend Component Tests
Playwright E2E Tests
k6 Performance Tests
```

---

# 11. Backend Unit Tests

## 11.1 工具

```text
xUnit
Moq
FluentAssertions
```

## 11.2 测试对象

```text
OrderService
ProductionService
InvoiceService
PaymentService
StatementService
AuditLogService
```

## 11.3 必须测试的业务规则

### OrderService

```text
Generated order can be sent to production
Cancelled order cannot be sent to production
ReadyToShip order can be marked as shipped
Completed order cannot be cancelled
```

### ProductionService

```text
Produced quantity cannot exceed total quantity
Produced quantity cannot be negative
ProductionItem becomes Completed when produced quantity equals total quantity
Order becomes ReadyToShip when all related production items are completed
```

### InvoiceService

```text
Draft invoice becomes Issued after PDF generation
Issued invoice becomes Unpaid after email is sent
Cancelled invoice cannot generate PDF
Paid invoice cannot be modified
```

### PaymentService

```text
Payment amount must be greater than zero
Payment amount cannot exceed outstanding amount
Full payment changes invoice to Paid
Partial payment changes invoice to PartiallyPaid
Paid invoice changes related order to Completed
Voiding payment recalculates invoice status
```

### StatementService

```text
Statement includes only unpaid / overdue / partially paid invoices
Statement stores invoice snapshots
Statement total outstanding equals sum of invoice outstanding amounts
Historical statement does not change after payment
```

---

# 12. Backend Integration Tests

## 12.1 工具

```text
xUnit
WebApplicationFactory
Testcontainers
PostgreSQL Testcontainer
Redis Testcontainer
```

## 12.2 目标

Integration Test 必须测试真实 API、真实 PostgreSQL、真实 Redis 之间的交互。

不要只 mock repository。

## 12.3 必须覆盖的接口

```text
POST /api/auth/login
GET /api/admin/customers
POST /api/admin/customers
POST /api/admin/orders/batch-to-production
PATCH /api/admin/production/items/{id}
POST /api/admin/orders/{id}/mark-shipped
POST /api/admin/invoices/{id}/generate-pdf
POST /api/admin/invoices/{id}/send-email
POST /api/admin/payments
POST /api/admin/statements/generate-weekly
GET /api/customer/invoices/{id}/download-url
GET /api/customer/statements/{id}/download-url
```

## 12.4 权限测试

必须覆盖：

```text
未登录用户不能访问 protected API
Customer 不能访问 /api/admin/*
Customer A 不能访问 Customer B 的 invoice
Customer A 不能下载 Customer B 的 statement
Admin 可以访问所有客户数据
```

## 12.5 数据库副作用测试

必须验证：

```text
记录 Payment 后，payment_records 表有新记录
记录 Payment 后，invoices.paid_amount 更新
记录 Payment 后，audit_logs 有记录
发送 Email 后，email_logs 有记录
生成 PDF 后，invoice.pdf_file_key 有值
生成 Statement 后，statement_invoices 有快照记录
```

---

# 13. Quartz Job Tests

## 13.1 需要测试的 Jobs

```text
GenerateOrdersFromStandingOrdersJob
UpdateOverdueInvoicesJob
GenerateWeeklyStatementsJob
```

## 13.2 GenerateOrdersFromStandingOrdersJob 测试

必须验证：

```text
Active StandingOrder 到期后生成 Order
Paused StandingOrder 不生成 Order
ManualOnly StandingOrder 不自动生成 Order
Suspended Customer 不生成 Order
同一个 StandingOrder 同一个 generated_period 不重复生成 Order
Job 执行后 next_closing_date 正确更新
Job 执行后 AuditLog 有记录
Redis lock 生效
数据库 unique constraint 防重复生效
```

## 13.3 UpdateOverdueInvoicesJob 测试

必须验证：

```text
Due date 已过且未付清的 Invoice 变 Overdue
Paid Invoice 不会变 Overdue
Cancelled Invoice 不会变 Overdue
Job 执行后 AuditLog 有记录
```

## 13.4 GenerateWeeklyStatementsJob 测试

必须验证：

```text
只为有未付清 Invoice 的客户生成 Statement
Paid Invoice 不进入 Statement
StatementInvoices 保存快照
重复执行不会生成重复 Statement，或需要有明确 period 防重复策略
```

---

# 14. Frontend Component Tests

## 14.1 工具

```text
Vitest
React Testing Library
MSW
```

## 14.2 测试重点

```text
表单验证
按钮 disabled 状态
API 错误提示
权限菜单显示
表格数据渲染
Dialog 打开和关闭
状态 Chip 显示
```

## 14.3 必须测试的组件

```text
Login page
Admin Orders page
Production List page
Invoices page
Payments Record Payment dialog
Statements page
Customer Standing Order page
Customer Invoices page
Customer Statements page
```

## 14.4 示例测试点

```text
Record Payment 金额为空时按钮 disabled
Record Payment 金额大于 outstanding amount 时显示错误
Customer 页面不显示 Admin 菜单
Invoice status = Overdue 时显示 Overdue chip
点击 Download PDF 调用 download-url API
点击 Send Email 调用 send-email API
API 返回 INVALID_PAYMENT_AMOUNT 时显示友好错误信息
```

---

# 15. Playwright E2E Tests

## 15.1 工具

```text
Playwright
```

## 15.2 测试环境

E2E 测试建议使用：

```text
Docker Compose 启动 frontend + backend + postgres + redis
测试前 seed 数据库
测试后清理数据库
```

## 15.3 Admin 主流程

必须覆盖完整业务链路：

```text
1. Admin 登录
2. 查看 Orders 页面
3. 点击 Send All to Production
4. 进入 Production List
5. 更新 Produced Quantity
6. Mark as Completed
7. 订单变 ReadyToShip
8. Mark as Shipped
9. Invoice 创建为 Draft
10. Generate Invoice PDF
11. Invoice 状态变 Issued
12. Send Invoice Email
13. Invoice 状态变 Unpaid
14. Record Payment
15. Invoice 状态变 Paid
16. Order 状态变 Completed
17. Generate Weekly Statement
18. Generate Statement PDF
19. Send Statement Email
```

## 15.4 Customer 主流程

必须覆盖：

```text
1. Customer 登录
2. 查看 Dashboard
3. 编辑 Standing Order
4. 查看自己的 Orders
5. 查看自己的 Invoices
6. 下载 Invoice PDF
7. 查看 Statements
8. 下载 Statement PDF
9. 更新 Account Settings
```

## 15.5 权限流程

必须覆盖：

```text
未登录访问 /admin 自动跳转登录页
Customer 访问 /admin 被拒绝
Customer 不能看到其他客户 Invoice
Customer 不能访问 Admin API
Admin 可以看到所有客户数据
```

---

# 16. k6 Performance Tests

## 16.1 工具

```text
k6
```

## 16.2 测试目标

第一版学习目标：

```text
50 concurrent users
p95 response time < 300ms for normal APIs
error rate < 1%
```

PDF 生成和 Email 发送可以单独评估，不强行纳入 300ms。

## 16.3 需要测试的接口

```text
GET /api/admin/dashboard
GET /api/admin/orders
GET /api/admin/invoices
POST /api/admin/orders/batch-to-production
POST /api/admin/payments
GET /api/customer/dashboard
GET /api/customer/invoices
```

## 16.4 Redis 缓存对比测试

需要测试：

```text
Dashboard API without Redis cache
Dashboard API with Redis cache
```

记录：

```text
p50
p95
p99
error rate
requests per second
```

---

# 17. 测试数据 Seed 规范

## 17.1 Seed 数据目标

本地开发、集成测试和 E2E 测试需要稳定的测试数据。

## 17.2 Seed 用户

```text
Admin:
email: admin@storycoffee.co.nz
password: admin123
role: Admin

Customer:
email: john@aucklandcafe.co.nz
password: customer123
role: Customer
customer: Auckland Cafe
```

## 17.3 Seed 业务数据

```text
3 Customers
5 Products
2 Active StandingOrders
5 Orders with different statuses
3 Invoices with different statuses
2 PaymentRecords
2 Statements
```

## 17.4 v1.1 Seed 数据要求

```text
所有 OrderStatus 使用新枚举，不能出现 Invoiced / NeedsReview
InvoiceStatus 必须覆盖 NotIssued / Draft / Issued / Unpaid / PartiallyPaid / Paid / Overdue / Cancelled
EmailStatus 必须覆盖 NotSent / Pending / Sent / Failed / Bounced
Statement 测试数据必须使用 statement_invoices 快照
Production 测试数据可以包含 ProductionBatch，但前端默认使用 current production API
```

## 17.5 数据重置

测试环境需要支持：

```text
reset database
apply migrations
seed test data
```

推荐命令：

```bash
dotnet run --project backend/StoryCoffee.Api -- seed --environment test
```

---

# 18. Kubernetes 设计

## 18.1 Namespace

建议：

```text
storycoffee-dev
storycoffee-staging
storycoffee-prod
```

## 18.2 Kubernetes 资源

至少包含：

```text
Frontend Deployment
Frontend Service
Backend Deployment
Backend Service
Ingress
ConfigMap
Secret
HorizontalPodAutoscaler
ServiceAccount
LivenessProbe
ReadinessProbe
```

PostgreSQL 和 Redis：

```text
local / learning environment 可以部署到 Kubernetes
staging / prod 推荐使用 AWS RDS 和 ElastiCache
```

---

# 19. Kubernetes Backend Deployment

## 19.1 Backend Deployment 示例结构

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: storycoffee-api
spec:
  replicas: 2
  selector:
    matchLabels:
      app: storycoffee-api
  template:
    metadata:
      labels:
        app: storycoffee-api
    spec:
      containers:
        - name: api
          image: <account-id>.dkr.ecr.<region>.amazonaws.com/storycoffee-api:latest
          ports:
            - containerPort: 8080
          envFrom:
            - configMapRef:
                name: storycoffee-api-config
            - secretRef:
                name: storycoffee-api-secret
          livenessProbe:
            httpGet:
              path: /health
              port: 8080
            initialDelaySeconds: 30
            periodSeconds: 10
          readinessProbe:
            httpGet:
              path: /ready
              port: 8080
            initialDelaySeconds: 10
            periodSeconds: 10
          resources:
            requests:
              cpu: "100m"
              memory: "256Mi"
            limits:
              cpu: "500m"
              memory: "512Mi"
```

## 19.2 Backend Service

```yaml
apiVersion: v1
kind: Service
metadata:
  name: storycoffee-api
spec:
  selector:
    app: storycoffee-api
  ports:
    - port: 80
      targetPort: 8080
  type: ClusterIP
```

---

# 20. Kubernetes Frontend Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: storycoffee-frontend
spec:
  replicas: 2
  selector:
    matchLabels:
      app: storycoffee-frontend
  template:
    metadata:
      labels:
        app: storycoffee-frontend
    spec:
      containers:
        - name: frontend
          image: <account-id>.dkr.ecr.<region>.amazonaws.com/storycoffee-frontend:latest
          ports:
            - containerPort: 80
          resources:
            requests:
              cpu: "50m"
              memory: "128Mi"
            limits:
              cpu: "200m"
              memory: "256Mi"
```

---

# 21. Ingress 设计

## 21.1 推荐路由

第一版推荐使用同一域名路径路由：

```text
https://storycoffee.example.com/      → frontend
https://storycoffee.example.com/api   → backend
```

原因：

```text
前端可以统一使用 /api
CORS 更简单
本地和 Kubernetes 配置更一致
```

## 21.2 Ingress Controller

AWS EKS 推荐：

```text
AWS Load Balancer Controller
```

本地学习可以使用：

```text
NGINX Ingress Controller
```

---

# 22. ConfigMap 与 Secret

## 22.1 ConfigMap

存非敏感配置：

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: storycoffee-api-config
data:
  ASPNETCORE_ENVIRONMENT: "Staging"
  ASPNETCORE_URLS: "http://+:8080"
  Storage__BucketName: "storycoffee-documents-staging"
  Storage__Region: "ap-southeast-2"
  Email__Provider: "SES"
  Jwt__Issuer: "storycoffee"
  Jwt__Audience: "storycoffee-web"
  Quartz__Enabled: "true"
```

## 22.2 Secret

存敏感配置：

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: storycoffee-api-secret
type: Opaque
stringData:
  ConnectionStrings__Postgres: "Host=...;Database=...;Username=...;Password=..."
  Redis__ConnectionString: "..."
  Jwt__Secret: "..."
  Storage__AccessKey: "..."
  Storage__SecretKey: "..."
  Email__ApiKey: "..."
```

生产环境建议使用：

```text
AWS Secrets Manager
External Secrets Operator
```

---

# 23. Horizontal Pod Autoscaler

## 23.1 Backend HPA

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: storycoffee-api-hpa
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: storycoffee-api
  minReplicas: 2
  maxReplicas: 5
  metrics:
    - type: Resource
      resource:
        name: cpu
        target:
          type: Utilization
          averageUtilization: 70
```

第一版可以只给 API 配 HPA。

Frontend 通常不需要复杂 HPA，但学习目的可以加。

---

# 24. Helm Chart 设计

## 24.1 目录结构

```text
infra/helm/storycoffee/
├── Chart.yaml
├── values.yaml
├── values-dev.yaml
├── values-staging.yaml
├── templates/
│   ├── backend-deployment.yaml
│   ├── backend-service.yaml
│   ├── frontend-deployment.yaml
│   ├── frontend-service.yaml
│   ├── ingress.yaml
│   ├── configmap.yaml
│   ├── secret.yaml
│   └── hpa.yaml
```

## 24.2 values.yaml 关键配置

```yaml
image:
  backend:
    repository: storycoffee-api
    tag: latest
  frontend:
    repository: storycoffee-frontend
    tag: latest

backend:
  replicas: 2
  resources:
    requests:
      cpu: 100m
      memory: 256Mi
    limits:
      cpu: 500m
      memory: 512Mi

frontend:
  replicas: 2

ingress:
  enabled: true
  host: storycoffee.example.com
```

## 24.3 Helm 命令

```bash
helm lint infra/helm/storycoffee
helm upgrade --install storycoffee infra/helm/storycoffee -n storycoffee-staging -f infra/helm/storycoffee/values-staging.yaml
```

---

# 25. Terraform 设计

## 25.1 Terraform 管理范围

```text
VPC
Subnets
Internet Gateway
NAT Gateway optional
Security Groups
EKS Cluster
EKS Node Group
ECR repositories
RDS PostgreSQL
ElastiCache Redis
S3 Buckets
IAM Roles
CloudWatch Log Groups
Route53 Records
ACM Certificate
```

## 25.2 目录结构

```text
infra/terraform/
├── environments/
│   ├── dev/
│   │   ├── main.tf
│   │   ├── variables.tf
│   │   ├── outputs.tf
│   │   └── terraform.tfvars
│   ├── staging/
│   └── prod/
│
└── modules/
    ├── vpc/
    ├── eks/
    ├── ecr/
    ├── rds/
    ├── redis/
    ├── s3/
    ├── iam/
    └── cloudwatch/
```

## 25.3 AWS 资源命名

```text
storycoffee-dev-vpc
storycoffee-dev-eks
storycoffee-dev-rds
storycoffee-dev-redis
storycoffee-dev-documents
storycoffee-api-dev
storycoffee-frontend-dev
```

## 25.4 Terraform 工作流

```bash
cd infra/terraform/environments/dev
terraform init
terraform fmt
terraform validate
terraform plan
terraform apply
```

PR 阶段只做：

```text
terraform fmt
terraform validate
terraform plan
```

merge 后才允许：

```text
terraform apply
```

---

# 26. AWS 部署架构

## 26.1 AWS 服务

```text
EKS：运行 frontend 和 backend pods
ECR：存 Docker 镜像
RDS PostgreSQL：主数据库
ElastiCache Redis：缓存、锁、限流
S3：存 Invoice / Statement PDF
CloudWatch：日志和监控
Route53：DNS
ACM：HTTPS 证书
IAM：权限管理
```

## 26.2 推荐架构

```text
User
  ↓
Route53
  ↓
ACM HTTPS
  ↓
AWS Load Balancer Controller / ALB
  ↓
EKS Ingress
  ↓
Frontend Service / Backend Service
  ↓
Pods
  ↓
RDS PostgreSQL / ElastiCache Redis / S3 / SES
```

---

# 27. GitHub Actions CI/CD

## 27.1 Pull Request Workflow

文件：

```text
.github/workflows/pull-request.yml
```

PR 阶段必须执行：

```text
Frontend pnpm install
Frontend lint
Frontend unit tests
Frontend build
Backend restore
Backend build
Backend unit tests
Backend integration tests
Docker build check
Helm lint
Terraform fmt / validate / plan
```

## 27.2 pull-request.yml 示例流程

```yaml
name: Pull Request

on:
  pull_request:
    branches: [main]

jobs:
  frontend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: 22
      - uses: pnpm/action-setup@v2
        with:
          version: 8
      - run: pnpm install --frozen-lockfile
        working-directory: frontend
      - run: pnpm lint
        working-directory: frontend
      - run: pnpm test
        working-directory: frontend
      - run: pnpm build
        working-directory: frontend

  backend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - run: dotnet restore backend/StoryCoffee.sln
      - run: dotnet build backend/StoryCoffee.sln --configuration Release --no-restore
      - run: dotnet test backend/StoryCoffee.Tests.Unit --configuration Release
      - run: dotnet test backend/StoryCoffee.Tests.Integration --configuration Release
```

---

# 28. Deploy Workflow

## 28.1 Merge to main

Merge 到 main 后执行：

```text
Run all tests
Build backend Docker image
Build frontend Docker image
Push images to AWS ECR
Terraform apply dev or staging
Helm upgrade deployment
kubectl rollout status
Run smoke tests
```

## 28.2 Docker build with version args

Frontend build args：

```text
VITE_API_BASE_URL=/api
VITE_APP_ENV=staging
VITE_VERSION=1.1.0
VITE_BUILD_TIME=<build-time>
VITE_GIT_COMMIT=<commit-sha>
```

Backend build args：

```text
GIT_COMMIT=<commit-sha>
```

## 28.3 Smoke Tests

部署完成后必须测试：

```text
GET /health
GET /ready
Frontend homepage returns 200
Login API works
```

---

# 29. 数据库迁移策略

## 29.1 EF Core Migrations

数据库 schema 通过 EF Core Migrations 管理。

常用命令：

```bash
dotnet ef migrations add InitialCreate --project StoryCoffee.Infrastructure --startup-project StoryCoffee.Api
dotnet ef database update --project StoryCoffee.Infrastructure --startup-project StoryCoffee.Api
```

## 29.2 部署时迁移

建议第一版：

```text
由 CI/CD 在部署前执行 migration
```

不要让每个 API Pod 启动时自动执行 migration，避免多 Pod 并发 migration。

## 29.3 数据库备份

staging / prod 使用 RDS 自动备份。

建议：

```text
staging retention: 7 days
prod retention: 14-30 days
```

---

# 30. 日志设计

## 30.1 Serilog 字段

后端日志需要结构化，至少包含：

```text
timestamp
level
requestId
traceId
userId
userRole
httpMethod
path
statusCode
durationMs
entityType
entityId
action
errorCode
exception
```

## 30.2 关键日志事件

必须记录：

```text
User login success / failure
Order sent to production
Production item updated
Invoice generated
Invoice PDF generated
Invoice email sent / failed
Payment recorded
Statement generated
Quartz job started / completed / failed
Redis lock acquired / failed
```

## 30.3 日志输出

local：

```text
Console
```

staging / prod：

```text
Console → Kubernetes logs → CloudWatch
```

---

# 31. 监控和告警

## 31.1 CloudWatch Logs

收集：

```text
API logs
Quartz job logs
PDF generation logs
Email sending logs
Kubernetes pod logs
```

## 31.2 关键指标

```text
API request count
API latency p50 / p95 / p99
API 4xx count
API 5xx count
Pod restart count
CPU usage
Memory usage
RDS CPU
RDS connections
Redis memory usage
Email send failures
Quartz job failures
```

## 31.3 告警建议

```text
API 5xx rate > 5% for 5 minutes
Pod restarts > 3 in 10 minutes
Quartz job failed
Email send failed more than 5 times in 10 minutes
RDS CPU > 80%
Redis memory > 80%
```

---

# 32. Security Engineering

## 32.1 JWT

要求：

```text
JWT Secret 不得提交 Git
Access token 设置过期时间
Customer token 中必须包含 customerId
Admin token 中 role = Admin
```

## 32.2 CORS

staging / prod 必须限制 CORS。

如果使用同域名路径路由：

```text
https://storycoffee.example.com
/api
```

CORS 配置可以更简单。

如果前后端分域名：

```text
https://app.storycoffee.example.com
https://api.storycoffee.example.com
```

则 API 只允许前端域名访问。

## 32.3 S3 文件安全

```text
S3 bucket private
不使用公开读权限
下载通过 backend 生成 pre-signed URL
pre-signed URL 默认 300 秒过期
Customer 下载前必须检查 ownership
```

## 32.4 Kubernetes Secret

```text
Secret 不提交 Git
生产环境推荐 External Secrets Operator + AWS Secrets Manager
```

## 32.5 API Rate Limiting

对以下接口启用限流：

```text
POST /api/auth/login
POST /api/admin/invoices/{id}/send-email
POST /api/admin/statements/{id}/send-email
GET /api/customer/invoices/{id}/download-url
GET /api/customer/statements/{id}/download-url
```

---

# 33. Error Handling in Engineering

## 33.1 后端错误响应

所有后端错误必须返回统一格式：

```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Validation failed",
    "details": {}
  }
}
```

## 33.2 前端错误处理

前端需要统一处理：

```text
401 → 跳转登录页
403 → 显示无权限
404 → 显示资源不存在
422 → 显示表单错误
500 → 显示系统错误
```

## 33.3 Error Code 映射

前端建议维护：

```typescript
const errorMessages = {
  INVALID_PAYMENT_AMOUNT: '付款金额无效',
  PAYMENT_EXCEEDS_OUTSTANDING_AMOUNT: '付款金额不能超过未付金额',
  INVALID_ORDER_STATUS: '当前订单状态不允许此操作',
  PDF_NOT_FOUND: 'PDF 文件不存在',
  EMAIL_SEND_FAILED: '邮件发送失败，请稍后重试',
  FORBIDDEN: '你没有权限执行此操作',
};
```

---

# 34. 发布流程

## 34.1 开发分支策略

建议：

```text
main：稳定分支
feature/*：功能分支
fix/*：修复分支
```

开发流程：

```text
创建 feature branch
提交 PR
自动运行测试
Code review
Merge to main
自动部署到 dev / staging
```

## 34.2 版本号

Docker image tag 建议：

```text
commit sha
semantic version optional
```

示例：

```text
storycoffee-api:sha-abc123
storycoffee-frontend:sha-abc123
```

---

# 35. Definition of Done

一个功能完成必须满足：

```text
后端 API 已实现
数据库 migration 已提交
权限校验已实现
AuditLog / EmailLog 已按规则写入
单元测试已覆盖核心业务规则
集成测试已覆盖主要 API
前端已接入真实 API
错误处理已完成
Docker Compose 可运行
CI 通过
文档更新
```

对于涉及 PDF / Email / Payment / Statement 的功能，还必须满足：

```text
PDF 可生成
PDF 可下载
EmailLog 正确记录
AuditLog 正确记录
权限隔离正确
E2E 流程通过
```

---

# 36. 开发优先级

## Phase 1: 本地工程环境

```text
1. Monorepo 结构整理
2. Backend Dockerfile
3. Frontend Dockerfile with pnpm
4. Nginx /api proxy
5. Docker Compose
6. PostgreSQL / Redis / LocalStack / MailHog
7. Health check / Ready check
```

## Phase 2: 自动化测试基础

```text
1. Backend unit test project
2. Backend integration test project
3. Testcontainers setup
4. Frontend Vitest setup
5. MSW API mock setup
6. Playwright setup
7. v1.1 seed data setup
```

## Phase 3: CI Pipeline

```text
1. GitHub Actions PR workflow
2. Frontend pnpm lint / test / build
3. Backend build / unit test / integration test
4. Docker build check
5. Helm lint
6. Terraform fmt / validate / plan
```

## Phase 4: Kubernetes / Helm

```text
1. Backend Deployment / Service
2. Frontend Deployment / Service
3. ConfigMap / Secret
4. Ingress with /api route
5. HPA
6. Helm chart
```

## Phase 5: AWS / Terraform

```text
1. ECR
2. VPC
3. EKS
4. RDS PostgreSQL
5. ElastiCache Redis
6. S3
7. CloudWatch
8. Route53 / ACM optional
```

## Phase 6: CD / Monitoring

```text
1. Push Docker images to ECR
2. Helm upgrade deployment
3. kubectl rollout status
4. Smoke tests
5. CloudWatch logs
6. Basic alarms
7. k6 performance tests
```

---

# 37. 验收标准

Engineering Spec 完成后，系统必须满足：

```text
本地可以通过 Docker Compose 启动完整系统
前端使用 pnpm 构建成功
前端通过 /api 访问后端
后端可以连接 PostgreSQL 和 Redis
LocalStack 可以模拟 S3 PDF 上传
MailHog 可以接收本地测试邮件
/health 和 /ready 可用
Backend unit tests 可以运行
Backend integration tests 可以通过 Testcontainers 运行
Frontend component tests 可以运行
Playwright E2E 可以跑通核心流程
E2E 和 seed 数据使用 v1.1 枚举
k6 可以执行基础性能测试
Docker images 可以构建
Kubernetes manifests 可以部署
Helm chart 可以 lint 和 install
Terraform 可以 fmt / validate / plan
GitHub Actions PR workflow 可以自动运行
部署后 smoke tests 可以通过
CloudWatch 可以看到 API logs
镜像和应用可以显示 version / build time / git commit
```

---

# 38. 给开发人员的实现说明

```text
Please implement the engineering foundation for the StoryCoffee B2B system based on this Engineering Spec v1.1.

The system should support local development with Docker Compose, automated testing, Docker image builds, Kubernetes deployment, Helm templates, Terraform-managed AWS infrastructure, GitHub Actions CI/CD, health checks, logs and basic monitoring.

The frontend project uses pnpm. All frontend Dockerfiles and GitHub Actions workflows must use pnpm install --frozen-lockfile, pnpm lint, pnpm test and pnpm build.

The frontend should call the backend through the relative path /api. Do not hardcode localhost or Docker internal service names into the built frontend. Use Nginx or Ingress to proxy /api to the backend service.

The backend runtime defaults to .NET 8 LTS. If the team chooses another .NET version, update Dockerfile, GitHub Actions and documentation consistently.

Local development must include PostgreSQL 16, Redis 7, LocalStack for S3 simulation and MailHog for local email testing.

The backend must expose /health and /ready endpoints. Kubernetes deployment must use these paths for liveness and readiness probes.

CI must run frontend tests, backend tests, integration tests, Docker build checks, Helm lint and Terraform validation. CD should build images, push to ECR, deploy to Kubernetes with Helm and run smoke tests.

All test seed data and E2E tests must use the v1.1 aligned enums: OrderStatus, InvoiceStatus and EmailStatus.

Secrets must not be committed to Git. Use environment variables locally, Kubernetes Secrets in cluster, and GitHub Actions Secrets in CI/CD.
```
