对，你这个想法是对的。**Production List 这一页不应该只是静态汇总表，它完全可以做成一个可操作的生产管理页面。**

但这里要注意一个边界：

> **Production List 管“产品是否制作完成”；Orders 页面管“订单整体状态、出货、invoice、付款”。**

这样最清楚。

---

# 1. Production List 这一页应该管什么？

这页的核心作用是：

```text
把所有已生成的订单汇总成产品生产清单
```

例如订单里有：

```text
Auckland Cafe: House Blend 1kg x 5
Wellington Coffee House: House Blend 1kg x 3
Christchurch Bistro: House Blend 1kg x 2
```

Production List 就应该汇总成：

```text
House Blend 1kg: total 10
```

这个页面最适合管理：

```text
这个产品要做多少
现在有没有开始做
做了多少
是否完成
完成时间
由谁标记完成
```

所以它可以有交互。

---

# 2. 我建议 Production List 增加这些字段

现在表格是：

```text
Product
SKU
Total Quantity
Related Orders
```

我建议改成：

```text
Product
SKU
Total Quantity
Produced Quantity
Production Status
Related Orders
Actions
```

比如：

| Product             | SKU     | Total Qty | Produced Qty | Status      | Related Orders   | Actions  |
| ------------------- | ------- | --------: | -----------: | ----------- | ---------------- | -------- |
| House Blend 1kg     | HB-1KG  |        10 |            6 | In Progress | ORD-001, ORD-002 | Update   |
| Brazil Espresso 1kg | BR-1KG  |         3 |            3 | Completed   | ORD-003          | Complete |
| Filter Blend 250g   | FB-250G |         4 |            0 | Pending     | ORD-003          | Start    |

---

# 3. Production Status 应该怎么设计？

Production List 里的状态可以是：

```text
Pending
In Progress
Completed
On Hold
```

解释：

```text
Pending = 还没开始制作
In Progress = 正在制作
Completed = 这个产品数量已经制作完成
On Hold = 暂停，比如缺货、需要确认
```

如果你想更细一点，也可以加：

```text
Partially Completed
```

但 MVP 里我建议先用：

```text
Pending / In Progress / Completed
```

够用了。

---

# 4. 这一页怎么和 Orders 页面联动？

这个是重点。

我建议逻辑是：

## Step 1：订单生成后

Orders 页面出现订单：

```text
Order Status = Generated
```

这些订单会进入 Production List 统计范围。

---

## Step 2：Admin 在 Production List 点击 Start

比如对 House Blend 1kg 点击：

```text
Start Production
```

这个产品行状态变成：

```text
Production Status = In Progress
```

同时，所有包含这个产品的相关订单，可以被更新为：

```text
Order Status = In Production
```

---

## Step 3：Admin 在 Production List 标记 Completed

比如 House Blend 1kg 做完了：

```text
Produced Quantity = 10
Production Status = Completed
```

系统检查相关订单：

```text
如果某个订单里的所有产品都 Completed
→ 这个订单状态变成 Ready to Ship
```

但注意：

> 不要直接把订单变成 Shipped。
> 因为“制作完成”和“已经出货”不是同一件事。

---

## Step 4：Orders 页面继续处理出货

当订单变成：

```text
Ready to Ship
```

Admin 再去 Orders 页面点击：

```text
Mark as Shipped
```

然后：

```text
Shipment Status = Shipped
Invoice 可以生成 / 发送
```

所以整个状态流应该是：

```text
Generated
→ In Production
→ Ready to Ship
→ Shipped
→ Invoiced
→ Paid / Unpaid
```

---

# 5. 哪种方式更好？

你刚才问：

> 是不是所有状态都在 Orders 里管，还是 Production List 也可以管一部分？

我的建议是：

## 最好的方式：分层管理

```text
Production List 管产品制作状态
Orders 管订单整体状态
Invoices / Payments 管账单和付款状态
```

不要把所有事情都放到 Orders 页面里。

原因是：

### Orders 页面看的是客户订单

它关心：

```text
这个客户的订单现在走到哪一步？
是否出货？
是否开票？
是否付款？
```

### Production List 页面看的是生产任务

它关心：

```text
今天/本周要做哪些产品？
每个产品要做多少？
哪些已经完成？
哪些还没做？
```

如果全部放在 Orders 页面，生产人员会很难看，因为他们要一个订单一个订单打开。

如果全部放在 Production List，订单状态又会不清楚。

所以最合理是：

```text
Production List 完成产品制作
→ 系统自动更新 Orders 的生产状态
→ Orders 页面继续处理出货和 invoice
```

---

# 6. 你可以让 Figma / 开发这样改

可以直接这样描述：

```text
Update the Production List page to make it interactive.

The Production List should not only display product quantities. It should allow admin staff to manage production progress for each product line.

Add these columns:
- Product
- SKU
- Total Quantity
- Produced Quantity
- Production Status
- Related Orders
- Actions

Production Status options:
- Pending
- In Progress
- Completed
- On Hold

Actions:
- Start Production
- Update Produced Quantity
- Mark as Completed

When a product line is marked as In Progress, related orders should show Order Status = In Production.

When all product lines required by an order are marked as Completed, the related order should automatically become Ready to Ship.

The Production List page should manage product-level production status, while the Orders page should manage order-level status such as Ready to Ship, Shipped, Invoice Sent, and Paid.
```

---

# 7. CSV 是什么？

CSV 全称是：

```text
Comma-Separated Values
```

中文可以理解成：

```text
用逗号分隔的数据文件
```

它是一种很常见的表格导出格式。

比如 Production List 导出成 CSV 后，可以用 Excel 打开，内容像这样：

```text
Product,SKU,Total Quantity,Produced Quantity,Status,Related Orders
House Blend 1kg,HB-1KG,10,6,In Progress,ORD-001
Brazil Espresso 1kg,BR-1KG,3,3,Completed,ORD-003
```

---

# 8. 为什么 Production List 要有 Export CSV？

这个功能是合理的。

因为 StoryCoffee 可能需要：

```text
导出生产清单
用 Excel 查看
打印给制作人员
发给仓库/烘焙人员
保存记录
做每周统计
```

所以：

```text
Print = 直接打印当前生产清单
Export CSV = 导出表格文件，可以用 Excel 打开和编辑
```

如果你觉得客户或老板不懂 CSV，可以按钮文案改成更清楚：

```text
Export to Excel
```

虽然技术上还是 CSV，但用户更容易理解。

我建议按钮可以写：

```text
Print
Export CSV
```

或者更用户友好一点：

```text
Print Production List
Export to Excel
```

---

# 9. 你这一页最终应该怎么定义？

这个页面不是单纯“出货清单”。

更准确应该叫：

```text
Production List
```

中文是：

```text
生产清单
```

它发生在出货之前。

流程是：

```text
Orders generated
→ Production List created
→ Products produced
→ Orders ready to ship
→ Orders shipped
→ Invoice sent
```

所以：

```text
Production List = 管制作
Orders = 管出货和订单状态
Invoices = 管开票
Payments = 管收款
```

---

# 10. 最终建议

你这个想法可以做，而且应该做。

我建议 Production List 页面加交互，但不要让它管理所有订单状态，只让它管理：

```text
产品生产进度
```

然后系统自动影响 Orders 页面：

```text
产品都完成后，订单自动变成 Ready to Ship
```

这样系统会很清楚，也更像真实后台：

```text
Production List 负责生产完成度
Orders 页面负责客户订单处理
Invoice / Payments 负责财务流程
```
