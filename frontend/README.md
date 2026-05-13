# StoryCoffee B2B 订单与发票管理系统

## 项目概述

StoryCoffee B2B 系统是一个完整的订单与发票管理系统，用于管理批发咖啡业务的订单、生产、发票、付款和对账单流程。

## 系统架构

- **前端**: React 18.3 + TypeScript + Material-UI + React Router
- **后端**: ASP.NET Core Web API + Entity Framework Core + PostgreSQL
- **缓存**: Redis (缓存/限流/分布式锁)
- **定时任务**: Quartz.NET
- **PDF 生成**: QuestPDF
- **邮件发送**: AWS SES / Resend
- **文件存储**: AWS S3 / S3-compatible storage

## 📢 最新更新 (v1.1 - 2026-05-08)

### 🔄 前后端枚举类型对齐完成

- ✅ 移除 `OrderStatus` 中的 `'Invoiced'` 和 `'NeedsReview'`
- ✅ `EmailStatus` 新增 `'Pending'` 状态
- ✅ 补充 `EmailStatus` 格式化函数和颜色映射
- ✅ ProductionBatch 第一版策略明确（前端隐藏批次概念）

**详细变更说明**: [CHANGELOG-v1.1.md](./CHANGELOG-v1.1.md)

---

## 开发文档

### 📘 前端开发文档
**文件**: [PRD-StoryCoffee-B2B系统.md](./PRD-StoryCoffee-B2B系统.md)

**内容**:
- 产品概述与业务流程
- 系统架构与技术栈
- 完整数据模型 (TypeScript interfaces)
- 所有页面详细规格 (19个页面)
  - Admin Dashboard
  - Orders, Production List, Invoices, Payments, Statements
  - Customer Dashboard, Standing Order, Invoices, Statements
- 状态流转图
- 交互规范 (Toast, Dialog, 表单验证, Chip 颜色)
- 开发检查清单 (60+ 验收项)

**适用人员**: 前端开发、产品经理、UI/UX 设计师

---

### 🔧 后端开发文档
**文件**: [Backend-Spec-StoryCoffee-B2B系统.md](./Backend-Spec-StoryCoffee-B2B系统.md)

**内容**:
- 后端项目结构 (分层架构)
- PostgreSQL 数据库表设计 (16张表)
- REST API 完整规格 (含 Request/Response 示例)
- 核心状态机定义
  - Order: Generated → InProduction → ReadyToShip → Shipped → Completed
  - Invoice: NotIssued → Draft → Issued → Unpaid → Paid
  - Production: Pending → InProgress → Completed
  - Statement: Draft → ReadyToSend → Sent
- 业务规则详细说明
- Quartz.NET 定时任务规格
- Redis 缓存/限流/分布式锁策略
- PDF 生成规格 (Invoice/Statement)
- Email 发送规格
- EmailLog 与 AuditLog 设计
- Error Codes 定义
- 开发优先级与验收标准

**适用人员**: 后端开发、数据库管理员、DevOps 工程师

---

### 🚀 部署文档
**文件**: [Engineering Spec v1.1](./Engineering Spec v1.1.md)

**计划内容**:
- Docker / Kubernetes 部署方案
- CI/CD 流程
- 环境配置
- 监控与日志
- 备份与恢复策略

**适用人员**: DevOps 工程师、系统管理员

---

## 核心业务流程

```
Standing Order (固定订购清单)
    ↓ 自动生成 (Quartz 定时任务)
Order (订单)
    ↓ Send to Production
Production List (生产清单)
    ↓ 标记完成
Ready to Ship
    ↓ Mark as Shipped
Invoice (发票)
    ↓ Record Payment
Paid Invoice
    ↓ 自动生成
Statement (对账单)
```

## 快速开始

### 前端开发
```bash
cd src
pnpm install
pnpm dev
```

### 后端开发
(待补充后端项目设置说明)

## 关键技术决策

### 前后端枚举对齐 (v1.1)

**OrderStatus**
```typescript
'Generated' | 'InProduction' | 'ReadyToShip' | 'Shipped' | 'Completed' | 'Cancelled'
```

**InvoiceStatus**
```typescript
'NotIssued' | 'Draft' | 'Issued' | 'Unpaid' | 'PartiallyPaid' | 'Paid' | 'Overdue' | 'Cancelled'
```
重点: `Issued` 表示 PDF 已生成但未发送

**EmailStatus**
```typescript
'NotSent' | 'Pending' | 'Sent' | 'Failed' | 'Bounced'
```

### ProductionBatch 策略
- 后端: 保留 `production_batches` 表管理生产批次
- 前端第一版: 隐藏批次概念，使用 `GET /api/admin/production/current` 获取扁平化数据
- 第二版: 前端增加批次选择器

### Statement 快照机制
- 后端: `statement_invoices` 表保存发票快照
- 前端: 接收快照数据作为 `invoices` 数组
- 重点: 历史 Statement 不随付款更新

## 团队协作

### 代码审查要点
- 前端: 确保所有状态更新符合状态机定义
- 后端: 所有关键操作必须写 AuditLog
- 权限: Customer 只能访问自己的数据 (后端强制校验)

### Git 分支策略
(待补充)

## 联系方式

- 产品负责人: (待补充)
- 技术负责人: (待补充)
- 前端团队: (待补充)
- 后端团队: (待补充)

---

**文档版本**: 1.1  
**最后更新**: 2026-05-08  
**审核状态**: 已审核
