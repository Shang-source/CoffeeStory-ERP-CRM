using Microsoft.EntityFrameworkCore;
using StoryCoffee.Domain;

namespace StoryCoffee.Infrastructure.Data;

public static class SeedData
{
    public static readonly Guid AucklandCustomerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid WellingtonCustomerId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static async Task Initialize(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var scopedProvider = services.GetService<AppDbContext>() is null
            ? services.CreateScope()
            : null;
        var provider = scopedProvider?.ServiceProvider ?? services;
        var db = provider.GetRequiredService<AppDbContext>();

        var passwordHasher = provider.GetRequiredService<IPasswordHasher>();
        var auckland = new Customer
        {
            Id = AucklandCustomerId,
            BusinessName = "Auckland Cafe",
            ContactPerson = "John Smith",
            Email = "john@aucklandcafe.co.nz",
            Phone = "+64 9 555 0101",
            BillingAddress = "12 Queen Street, Auckland 1010",
            DeliveryAddress = "12 Queen Street, Auckland 1010",
            PaymentTerms = "Net 14",
            AccountStatus = AccountStatus.Active
        };
        var wellington = new Customer
        {
            Id = WellingtonCustomerId,
            BusinessName = "Wellington Coffee House",
            ContactPerson = "Sarah Taylor",
            Email = "sarah@wellingtoncoffee.co.nz",
            Phone = "+64 4 555 0102",
            BillingAddress = "88 Cuba Street, Wellington 6011",
            DeliveryAddress = "88 Cuba Street, Wellington 6011",
            PaymentTerms = "Net 14",
            AccountStatus = AccountStatus.Active
        };

        if (!await db.Customers.AnyAsync(cancellationToken))
        {
            db.Customers.AddRange(auckland, wellington);
        }

        if (!await db.Products.AnyAsync(cancellationToken))
        {
            db.Products.AddRange(
                new Product
                {
                    Id = ProductIdForSku("HB-1KG"),
                    Sku = "HB-1KG",
                    Name = "House Blend 1kg",
                    Description = "Classic house blend coffee beans",
                    Unit = "kg",
                    Price = 38.00m,
                    Cost = 25.00m,
                    IsActive = true
                },
                new Product
                {
                    Id = ProductIdForSku("DCF-500G"),
                    Sku = "DCF-500G",
                    Name = "Decaf 500g",
                    Description = "Decaffeinated coffee beans",
                    Unit = "g",
                    Price = 22.00m,
                    Cost = 16.00m,
                    IsActive = true
                },
                new Product
                {
                    Id = ProductIdForSku("BR-ESP-1KG"),
                    Sku = "BR-ESP-1KG",
                    Name = "Brazil Espresso 1kg",
                    Description = "Brazilian espresso beans",
                    Unit = "kg",
                    Price = 42.00m,
                    Cost = 30.00m,
                    IsActive = true
                },
                new Product
                {
                    Id = ProductIdForSku("FLT-250G"),
                    Sku = "FLT-250G",
                    Name = "Filter Blend 250g",
                    Description = "Light roast filter coffee",
                    Unit = "g",
                    Price = 13.50m,
                    Cost = 10.00m,
                    IsActive = true
                },
                new Product
                {
                    Id = ProductIdForSku("COL-1KG"),
                    Sku = "COL-1KG",
                    Name = "Colombia Single Origin 1kg",
                    Description = "Single origin Colombian coffee beans",
                    Unit = "kg",
                    Price = 46.00m,
                    Cost = 32.00m,
                    IsActive = true
                });
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!await db.Users.AnyAsync(cancellationToken))
        {
            db.Users.AddRange(
                new User
                {
                    Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    Email = "admin@storycoffee.co.nz",
                    PasswordHash = passwordHasher.Hash("password"),
                    DisplayName = "Admin User",
                    Role = UserRole.Admin
                },
                new User
                {
                    Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    Email = "john@aucklandcafe.co.nz",
                    PasswordHash = passwordHasher.Hash("password"),
                    DisplayName = "Auckland Cafe",
                    Role = UserRole.Customer,
                    CustomerId = auckland.Id
                },
                new User
                {
                    Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                    Email = "sarah@wellingtoncoffee.co.nz",
                    PasswordHash = passwordHasher.Hash("password"),
                    DisplayName = "Wellington Coffee House",
                    Role = UserRole.Customer,
                    CustomerId = wellington.Id
                });
        }

        if (!await db.Orders.AnyAsync(cancellationToken))
        {
            var shippedOrder = CreateOrder("ORD-202605-0004", auckland, OrderStatus.Shipped, ShipmentStatus.Shipped, InvoiceStatus.Unpaid, -5, new[]
            {
                ("Brazil Espresso 1kg", "BR-ESP-1KG", 2, 42.00m),
                ("Filter Blend 250g", "FLT-250G", 6, 13.50m)
            });
            shippedOrder.Invoice = CreateInvoice("INV-202605-0004", shippedOrder, InvoiceStatus.Unpaid, EmailStatus.Sent);

            db.Orders.AddRange(
                CreateOrder("ORD-202605-0001", auckland, OrderStatus.Generated, ShipmentStatus.NotShipped, InvoiceStatus.NotIssued, -8, new[]
                {
                    ("House Blend 1kg", "HB-1KG", 5, 38.00m),
                    ("Decaf 500g", "DCF-500G", 2, 22.00m)
                }),
                CreateOrder("ORD-202605-0002", wellington, OrderStatus.InProduction, ShipmentStatus.NotShipped, InvoiceStatus.NotIssued, -7, new[]
                {
                    ("Brazil Espresso 1kg", "BR-ESP-1KG", 4, 42.00m),
                    ("Filter Blend 250g", "FLT-250G", 8, 13.50m)
                }),
                CreateOrder("ORD-202605-0003", auckland, OrderStatus.ReadyToShip, ShipmentStatus.ReadyToShip, InvoiceStatus.NotIssued, -6, new[]
                {
                    ("House Blend 1kg", "HB-1KG", 6, 38.00m),
                    ("Colombia Single Origin 1kg", "COL-1KG", 3, 46.00m)
                }),
                shippedOrder);
        }

        if (!await db.StandingOrders.AnyAsync(cancellationToken))
        {
            var products = await db.Products.ToDictionaryAsync(product => product.Sku, cancellationToken);
            db.StandingOrders.AddRange(
                CreateStandingOrder(auckland, OrderFrequency.Weekly, DateTimeOffset.UtcNow.AddDays(7), "Deliver every Monday morning", new[]
                {
                    (products["HB-1KG"], 5),
                    (products["DCF-500G"], 2)
                }),
                CreateStandingOrder(wellington, OrderFrequency.Fortnightly, DateTimeOffset.UtcNow.AddDays(10), "Call before delivery", new[]
                {
                    (products["BR-ESP-1KG"], 4),
                    (products["FLT-250G"], 8)
                }));
        }

        await db.SaveChangesAsync(cancellationToken);
        scopedProvider?.Dispose();
    }

    private static Order CreateOrder(
        string orderNumber,
        Customer customer,
        OrderStatus orderStatus,
        ShipmentStatus shipmentStatus,
        InvoiceStatus invoiceStatus,
        int generatedDaysOffset,
        IEnumerable<(string Name, string Sku, int Quantity, decimal UnitPrice)> items)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = orderNumber,
            CustomerId = customer.Id,
            Customer = customer,
            StandingOrderId = Guid.NewGuid(),
            GeneratedAt = DateTimeOffset.UtcNow.AddDays(generatedDaysOffset),
            OrderStatus = orderStatus,
            ShipmentStatus = shipmentStatus,
            InvoiceStatus = invoiceStatus
        };

        foreach (var item in items)
        {
            var lineTotal = item.Quantity * item.UnitPrice;
            order.Items.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = ProductIdForSku(item.Sku),
                ProductNameSnapshot = item.Name,
                SkuSnapshot = item.Sku,
                Quantity = item.Quantity,
                UnitPriceSnapshot = item.UnitPrice,
                LineTotal = lineTotal
            });
        }

        order.Subtotal = order.Items.Sum(item => item.LineTotal);
        order.GstAmount = Math.Round(order.Subtotal * 0.15m, 2);
        order.TotalAmount = order.Subtotal + order.GstAmount;
        return order;
    }

    private static Guid ProductIdForSku(string sku)
    {
        return sku switch
        {
            "HB-1KG" => Guid.Parse("10000000-0000-0000-0000-000000000001"),
            "DCF-500G" => Guid.Parse("10000000-0000-0000-0000-000000000002"),
            "BR-ESP-1KG" => Guid.Parse("10000000-0000-0000-0000-000000000003"),
            "FLT-250G" => Guid.Parse("10000000-0000-0000-0000-000000000004"),
            "COL-1KG" => Guid.Parse("10000000-0000-0000-0000-000000000005"),
            _ => Guid.NewGuid()
        };
    }

    private static StandingOrder CreateStandingOrder(
        Customer customer,
        OrderFrequency frequency,
        DateTimeOffset nextClosingDate,
        string deliveryNotes,
        IEnumerable<(Product Product, int Quantity)> items)
    {
        var standingOrder = new StandingOrder
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Customer = customer,
            Frequency = frequency,
            NextClosingDate = nextClosingDate,
            Status = StandingOrderStatus.Active,
            DeliveryNotes = deliveryNotes
        };

        foreach (var item in items)
        {
            standingOrder.Items.Add(new StandingOrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = item.Product.Id,
                Product = item.Product,
                Quantity = item.Quantity,
                UnitPrice = item.Product.Price
            });
        }

        return standingOrder;
    }

    private static Invoice CreateInvoice(string invoiceNumber, Order order, InvoiceStatus status, EmailStatus emailStatus)
    {
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = invoiceNumber,
            CustomerId = order.CustomerId,
            OrderId = order.Id,
            IssueDate = DateTimeOffset.UtcNow.AddDays(-4),
            DueDate = DateTimeOffset.UtcNow.AddDays(10),
            Subtotal = order.Subtotal,
            GstAmount = order.GstAmount,
            TotalAmount = order.TotalAmount,
            PaidAmount = 0,
            OutstandingAmount = order.TotalAmount,
            Status = status,
            EmailStatus = emailStatus
        };

        foreach (var item in order.Items)
        {
            invoice.Items.Add(new InvoiceItem
            {
                Id = Guid.NewGuid(),
                Description = item.ProductNameSnapshot,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPriceSnapshot,
                LineTotal = item.LineTotal
            });
        }

        return invoice;
    }
}
