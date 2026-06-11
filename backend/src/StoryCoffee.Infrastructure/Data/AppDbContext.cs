using Microsoft.EntityFrameworkCore;
using StoryCoffee.Domain;

namespace StoryCoffee.Infrastructure.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<CustomerProductPrice> CustomerProductPrices => Set<CustomerProductPrice>();
    public DbSet<User> Users => Set<User>();
    public DbSet<StandingOrder> StandingOrders => Set<StandingOrder>();
    public DbSet<StandingOrderItem> StandingOrderItems => Set<StandingOrderItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<PaymentRecord> PaymentRecords => Set<PaymentRecord>();
    public DbSet<Statement> Statements => Set<Statement>();
    public DbSet<StatementInvoice> StatementInvoices => Set<StatementInvoice>();
    public DbSet<ProductionBatch> ProductionBatches => Set<ProductionBatch>();
    public DbSet<ProductionItem> ProductionItems => Set<ProductionItem>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();
    public DbSet<EmailDeliveryEvent> EmailDeliveryEvents => Set<EmailDeliveryEvent>();
    public DbSet<JobExecutionLog> JobExecutionLogs => Set<JobExecutionLog>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("customers");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.AccountNumber).IsUnique();
            entity.Property(x => x.AccountNumber).HasMaxLength(20).IsRequired();
            entity.Property(x => x.BusinessName).HasMaxLength(255).IsRequired();
            entity.Property(x => x.ContactPerson).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(255).IsRequired();
            entity.Property(x => x.AccountStatus).HasConversion<string>().HasMaxLength(30);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.Email).HasMaxLength(255).IsRequired();
            entity.Property(x => x.PasswordHash).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(30);
            entity.HasOne(x => x.Customer).WithMany(x => x.Users).HasForeignKey(x => x.CustomerId);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Sku).IsUnique();
            entity.Property(x => x.Sku).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Unit).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Price).HasPrecision(12, 2);
            entity.Property(x => x.Cost).HasPrecision(12, 2);
        });

        modelBuilder.Entity<CustomerProductPrice>(entity =>
        {
            entity.ToTable("customer_product_prices");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.CustomerId, x.ProductId }).IsUnique();
            entity.Property(x => x.OverridePrice).HasPrecision(12, 2);
            entity.Property(x => x.Notes).HasMaxLength(500);
            entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId);
            entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId);
        });

        modelBuilder.Entity<StandingOrder>(entity =>
        {
            entity.ToTable("standing_orders");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Frequency).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
            entity.HasOne(x => x.Customer).WithMany(x => x.StandingOrders).HasForeignKey(x => x.CustomerId);
        });

        modelBuilder.Entity<StandingOrderItem>(entity =>
        {
            entity.ToTable("standing_order_items");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UnitPrice).HasPrecision(12, 2);
            entity.HasOne(x => x.StandingOrder).WithMany(x => x.Items).HasForeignKey(x => x.StandingOrderId);
            entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.OrderNumber).IsUnique();
            entity.Property(x => x.OrderNumber).HasMaxLength(40).IsRequired();
            entity.Property(x => x.OrderStatus).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.InvoiceStatus).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.ShipmentStatus).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.Subtotal).HasPrecision(12, 2);
            entity.Property(x => x.GstAmount).HasPrecision(12, 2);
            entity.Property(x => x.TotalAmount).HasPrecision(12, 2);
            entity.HasOne(x => x.Customer).WithMany(x => x.Orders).HasForeignKey(x => x.CustomerId);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("order_items");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ProductNameSnapshot).HasMaxLength(255).IsRequired();
            entity.Property(x => x.SkuSnapshot).HasMaxLength(80).IsRequired();
            entity.Property(x => x.UnitPriceSnapshot).HasPrecision(12, 2);
            entity.Property(x => x.LineTotal).HasPrecision(12, 2);
            entity.HasOne(x => x.Order).WithMany(x => x.Items).HasForeignKey(x => x.OrderId);
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.ToTable("invoices");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.InvoiceNumber).IsUnique();
            entity.HasIndex(x => x.OrderId).IsUnique();
            entity.Property(x => x.InvoiceNumber).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.EmailStatus).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.Subtotal).HasPrecision(12, 2);
            entity.Property(x => x.GstAmount).HasPrecision(12, 2);
            entity.Property(x => x.TotalAmount).HasPrecision(12, 2);
            entity.Property(x => x.PaidAmount).HasPrecision(12, 2);
            entity.Property(x => x.OutstandingAmount).HasPrecision(12, 2);
            entity.Property(x => x.PdfFileKey).HasMaxLength(500);
            entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId);
            entity.HasOne(x => x.Order).WithOne(x => x.Invoice).HasForeignKey<Invoice>(x => x.OrderId);
        });

        modelBuilder.Entity<InvoiceItem>(entity =>
        {
            entity.ToTable("invoice_items");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Description).IsRequired();
            entity.Property(x => x.UnitPrice).HasPrecision(12, 2);
            entity.Property(x => x.LineTotal).HasPrecision(12, 2);
            entity.HasOne(x => x.Invoice).WithMany(x => x.Items).HasForeignKey(x => x.InvoiceId);
        });

        modelBuilder.Entity<PaymentRecord>(entity =>
        {
            entity.ToTable("payment_records");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Amount).HasPrecision(12, 2);
            entity.Property(x => x.PaymentMethod).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Reference).HasMaxLength(255);
            entity.Property(x => x.VoidReason).HasMaxLength(500);
            entity.HasOne(x => x.Invoice).WithMany(x => x.Payments).HasForeignKey(x => x.InvoiceId);
            entity.HasOne(x => x.MarkedByUser).WithMany().HasForeignKey(x => x.MarkedByUserId);
        });

        modelBuilder.Entity<Statement>(entity =>
        {
            entity.ToTable("statements");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.StatementNumber).IsUnique();
            entity.Property(x => x.StatementNumber).HasMaxLength(40).IsRequired();
            entity.Property(x => x.TotalOutstanding).HasPrecision(12, 2);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.EmailStatus).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.PdfFileKey).HasMaxLength(500);
            entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId);
        });

        modelBuilder.Entity<StatementInvoice>(entity =>
        {
            entity.ToTable("statement_invoices");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.InvoiceNumberSnapshot).HasMaxLength(40).IsRequired();
            entity.Property(x => x.TotalAmountSnapshot).HasPrecision(12, 2);
            entity.Property(x => x.OutstandingAmountSnapshot).HasPrecision(12, 2);
            entity.Property(x => x.StatusSnapshot).HasConversion<string>().HasMaxLength(40);
            entity.HasOne(x => x.Statement).WithMany(x => x.Invoices).HasForeignKey(x => x.StatementId);
        });

        modelBuilder.Entity<ProductionBatch>(entity =>
        {
            entity.ToTable("production_batches");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.BatchNumber).IsUnique();
            entity.Property(x => x.BatchNumber).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ProductionPeriod).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        });

        modelBuilder.Entity<ProductionItem>(entity =>
        {
            entity.ToTable("production_items");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ProductionBatchId, x.ProductId }).IsUnique();
            entity.Property(x => x.ProductNameSnapshot).HasMaxLength(255).IsRequired();
            entity.Property(x => x.SkuSnapshot).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            entity.HasOne(x => x.ProductionBatch).WithMany(x => x.Items).HasForeignKey(x => x.ProductionBatchId);
            entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.CreatedAt);
            entity.Property(x => x.ActorRole).HasMaxLength(40);
            entity.Property(x => x.Action).HasMaxLength(120).IsRequired();
            entity.Property(x => x.EntityType).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.OldValues);
            entity.Property(x => x.NewValues);
        });

        modelBuilder.Entity<EmailLog>(entity =>
        {
            entity.ToTable("email_logs");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.CreatedAt);
            entity.Property(x => x.RelatedEntityType).HasMaxLength(120).IsRequired();
            entity.Property(x => x.RecipientEmail).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Subject).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.Provider).HasMaxLength(60);
            entity.Property(x => x.ProviderMessageId).HasMaxLength(255);
            entity.Property(x => x.LastProviderEventType).HasMaxLength(80);
            entity.Property(x => x.ErrorMessage).HasMaxLength(1000);
        });

        modelBuilder.Entity<EmailDeliveryEvent>(entity =>
        {
            entity.ToTable("email_delivery_events");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.Provider, x.ProviderMessageId });
            entity.HasIndex(x => new { x.Provider, x.ProviderEventId }).IsUnique();
            entity.Property(x => x.Provider).HasMaxLength(60).IsRequired();
            entity.Property(x => x.ProviderEventId).HasMaxLength(255);
            entity.Property(x => x.ProviderMessageId).HasMaxLength(255).IsRequired();
            entity.Property(x => x.EventType).HasMaxLength(80).IsRequired();
            entity.Property(x => x.RecipientEmail).HasMaxLength(255);
            entity.Property(x => x.Reason).HasMaxLength(1000);
            entity.Property(x => x.Payload).IsRequired();
            entity.HasOne(x => x.EmailLog).WithMany().HasForeignKey(x => x.EmailLogId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<JobExecutionLog>(entity =>
        {
            entity.ToTable("job_execution_logs");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.StartedAt);
            entity.Property(x => x.JobName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.ErrorMessage).HasMaxLength(2000);
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox_messages");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.Status, x.AvailableAt });
            entity.Property(x => x.Type).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Payload).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.ErrorMessage).HasMaxLength(2000);
        });
    }
}
