using System.Text.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StoryCoffee.Contracts;
using StoryCoffee.Infrastructure.Data;
using StoryCoffee.Domain;

namespace StoryCoffee.Tests;

public sealed class ApiIntegrationTests(TestingWebAppFactory factory) : IClassFixture<TestingWebAppFactory>, IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Login_IssuesTokenAndAllowsAdminOrderRead()
    {
        var client = factory.CreateClient();
        var login = await Login(client, "admin@storycoffee.co.nz");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var orders = await client.GetFromJsonAsync<List<OrderDto>>("/api/admin/orders");

        Assert.Equal(UserRole.Admin, login.Role);
        Assert.NotNull(orders);
        Assert.True(orders.Count >= 3);
    }

    [Fact]
    public async Task CustomerOrderRead_IsRestrictedToOwnCustomer()
    {
        var client = factory.CreateClient();
        var login = await Login(client, "john@aucklandcafe.co.nz");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var orders = await client.GetFromJsonAsync<List<OrderDto>>("/api/customer/orders");

        Assert.NotNull(orders);
        Assert.NotEmpty(orders);
        Assert.All(orders, order => Assert.Equal(login.UserProfile.CustomerId, order.CustomerId));
    }

    [Fact]
    public async Task CustomerInvoiceRead_IsRestrictedToOwnCustomer()
    {
        var client = factory.CreateClient();
        var login = await Login(client, "john@aucklandcafe.co.nz");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var invoices = await client.GetFromJsonAsync<List<InvoiceDto>>("/api/customer/invoices");

        Assert.NotNull(invoices);
        Assert.NotEmpty(invoices);
        Assert.All(invoices, invoice => Assert.Equal(login.UserProfile.CustomerId, invoice.CustomerId));
    }

    [Fact]
    public async Task CustomerCanReadOwnInvoiceAndStatementDetailsOnly()
    {
        var adminClient = factory.CreateClient();
        var adminLogin = await Login(adminClient, "admin@storycoffee.co.nz");
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminLogin.AccessToken);
        await adminClient.PostAsync("/api/admin/statements/generate-weekly", null);
        var adminInvoices = await adminClient.GetFromJsonAsync<List<InvoiceDto>>("/api/admin/invoices");
        var adminStatements = await adminClient.GetFromJsonAsync<List<StatementDto>>("/api/admin/statements");
        var otherCustomerInvoice = adminInvoices!.FirstOrDefault(invoice => invoice.CustomerId != SeedData.AucklandCustomerId)
            ?? await CreateOtherCustomerInvoice();
        var otherCustomerStatementId = adminStatements!
            .FirstOrDefault(statement => statement.CustomerId != SeedData.AucklandCustomerId)
            ?.Id ?? await CreateOtherCustomerStatement(otherCustomerInvoice);

        var client = factory.CreateClient();
        var login = await Login(client, "john@aucklandcafe.co.nz");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var invoices = await client.GetFromJsonAsync<List<InvoiceDto>>("/api/customer/invoices");
        var ownInvoice = invoices!.First();
        var statements = await client.GetFromJsonAsync<List<StatementDto>>("/api/customer/statements");
        var ownStatement = statements!.First();

        var invoiceDetail = await client.GetFromJsonAsync<InvoiceDto>($"/api/customer/invoices/{ownInvoice.Id}");
        var statementDetail = await client.GetFromJsonAsync<StatementDto>($"/api/customer/statements/{ownStatement.Id}");
        var otherInvoiceResponse = await client.GetAsync($"/api/customer/invoices/{otherCustomerInvoice.Id}");
        var otherStatementResponse = await client.GetAsync($"/api/customer/statements/{otherCustomerStatementId}");

        Assert.Equal(ownInvoice.Id, invoiceDetail!.Id);
        Assert.Equal(login.UserProfile.CustomerId, invoiceDetail.CustomerId);
        Assert.NotEmpty(invoiceDetail.Items);
        Assert.Equal(ownStatement.Id, statementDetail!.Id);
        Assert.Equal(login.UserProfile.CustomerId, statementDetail.CustomerId);
        Assert.NotEmpty(statementDetail.Invoices);
        Assert.Equal(HttpStatusCode.NotFound, otherInvoiceResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, otherStatementResponse.StatusCode);
    }

    private async Task<InvoiceDto> CreateOtherCustomerInvoice()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var customer = await db.Customers.FirstAsync(customer => customer.Id == SeedData.WellingtonCustomerId);
        var standingOrder = await db.StandingOrders.FirstAsync(order => order.CustomerId == customer.Id);
        var product = await db.Products.FirstAsync();
        var now = DateTimeOffset.UtcNow;
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = $"ORD-TEST-{Guid.NewGuid():N}"[..17],
            CustomerId = customer.Id,
            Customer = customer,
            StandingOrderId = standingOrder.Id,
            GeneratedAt = now,
            OrderStatus = OrderStatus.Shipped,
            InvoiceStatus = InvoiceStatus.Unpaid,
            ShipmentStatus = ShipmentStatus.Shipped,
            Subtotal = 10,
            GstAmount = 1.5m,
            TotalAmount = 11.5m,
            CreatedAt = now,
            UpdatedAt = now
        };
        order.Items.Add(new OrderItem
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            ProductNameSnapshot = product.Name,
            SkuSnapshot = product.Sku,
            Quantity = 1,
            UnitPriceSnapshot = 10,
            LineTotal = 10
        });
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = $"INV-TEST-{Guid.NewGuid():N}"[..17],
            CustomerId = customer.Id,
            Customer = customer,
            OrderId = order.Id,
            Order = order,
            IssueDate = now,
            DueDate = now.AddDays(14),
            Subtotal = 10,
            GstAmount = 1.5m,
            TotalAmount = 11.5m,
            PaidAmount = 0,
            OutstandingAmount = 11.5m,
            Status = InvoiceStatus.Unpaid,
            EmailStatus = EmailStatus.NotSent,
            CreatedAt = now,
            UpdatedAt = now
        };
        invoice.Items.Add(new InvoiceItem
        {
            Id = Guid.NewGuid(),
            Description = product.Name,
            Quantity = 1,
            UnitPrice = 10,
            LineTotal = 10
        });
        db.Orders.Add(order);
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
        return new InvoiceDto(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.CustomerId,
            null,
            invoice.OrderId,
            invoice.IssueDate,
            invoice.DueDate,
            invoice.Subtotal,
            invoice.GstAmount,
            invoice.TotalAmount,
            invoice.PaidAmount,
            invoice.OutstandingAmount,
            invoice.Status,
            invoice.EmailStatus,
            [new InvoiceItemDto(invoice.Items.First().Id, product.Name, 1, 10, 10)],
            []);
    }

    private async Task<Guid> CreateOtherCustomerStatement(InvoiceDto invoice)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var statement = new Statement
        {
            Id = Guid.NewGuid(),
            StatementNumber = $"STMT-TEST-{Guid.NewGuid():N}"[..18],
            CustomerId = invoice.CustomerId,
            StatementDate = DateTimeOffset.UtcNow,
            PeriodStart = invoice.IssueDate,
            PeriodEnd = DateTimeOffset.UtcNow,
            TotalOutstanding = invoice.OutstandingAmount,
            Status = StatementStatus.ReadyToSend,
            EmailStatus = EmailStatus.NotSent
        };
        statement.Invoices.Add(new StatementInvoice
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoice.Id,
            InvoiceNumberSnapshot = invoice.InvoiceNumber,
            IssueDateSnapshot = invoice.IssueDate,
            DueDateSnapshot = invoice.DueDate,
            TotalAmountSnapshot = invoice.TotalAmount,
            OutstandingAmountSnapshot = invoice.OutstandingAmount,
            StatusSnapshot = invoice.Status
        });
        db.Statements.Add(statement);
        await db.SaveChangesAsync();
        return statement.Id;
    }

    [Fact]
    public async Task AdminCanRecordInvoicePayment()
    {
        var client = factory.CreateClient();
        var login = await Login(client, "admin@storycoffee.co.nz");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var invoices = await client.GetFromJsonAsync<List<InvoiceDto>>("/api/admin/invoices");
        var unpaidInvoice = invoices!.First(invoice => invoice.Status == InvoiceStatus.Unpaid);
        var paymentAmount = Math.Round(unpaidInvoice.OutstandingAmount / 2, 2);

        var response = await client.PostAsJsonAsync($"/api/admin/invoices/{unpaidInvoice.Id}/payments", new RecordPaymentRequest(
            paymentAmount,
            DateTimeOffset.UtcNow,
            "BankTransfer",
            "API-TEST",
            null));

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        var voidResponse = await client.PostAsJsonAsync($"/api/admin/invoices/{unpaidInvoice.Id}/payments/{body!.Payment.Id}/void", new VoidPaymentRequest("API duplicate"));
        voidResponse.EnsureSuccessStatusCode();
        var voided = await voidResponse.Content.ReadFromJsonAsync<PaymentResponse>();

        var overdueResponse = await client.PostAsync("/api/admin/invoices/mark-overdue", null);
        overdueResponse.EnsureSuccessStatusCode();

        Assert.Equal(InvoiceStatus.PartiallyPaid, body.Invoice.Status);
        Assert.True(voided!.Payment.IsVoided);
    }

    [Fact]
    public async Task AdminCanDownloadInvoicePdf()
    {
        var client = factory.CreateClient();
        var login = await Login(client, "admin@storycoffee.co.nz");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var invoices = await client.GetFromJsonAsync<List<InvoiceDto>>("/api/admin/invoices");
        var invoice = invoices!.First();

        var metadataResponse = await client.GetAsync($"/api/admin/invoices/{invoice.Id}/download-url");
        metadataResponse.EnsureSuccessStatusCode();
        var metadata = await metadataResponse.Content.ReadFromJsonAsync<PdfDownloadDto>();
        var pdfResponse = await client.GetAsync(metadata!.DownloadUrl);

        pdfResponse.EnsureSuccessStatusCode();
        Assert.Equal("application/pdf", pdfResponse.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task AdminCanReadAuditAndEmailLogs()
    {
        var client = factory.CreateClient();
        var login = await Login(client, "admin@storycoffee.co.nz");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var orders = await client.GetFromJsonAsync<List<OrderDto>>("/api/admin/orders");
        var readyOrder = orders!.First(order => order.OrderStatus == OrderStatus.ReadyToShip);
        var shipResponse = await client.PostAsync($"/api/admin/orders/{readyOrder.Id}/mark-shipped", null);
        shipResponse.EnsureSuccessStatusCode();
        var invoices = await client.GetFromJsonAsync<List<InvoiceDto>>("/api/admin/invoices");
        var draftInvoice = invoices!.First(invoice => invoice.OrderId == readyOrder.Id);
        var sendResponse = await client.PostAsync($"/api/admin/invoices/{draftInvoice.Id}/send-email", null);
        sendResponse.EnsureSuccessStatusCode();

        var auditLogs = await client.GetFromJsonAsync<PagedResult<AuditLogDto>>("/api/admin/logs/audit?action=SentInvoiceEmail&page=1&pageSize=10");
        var emailLogs = await client.GetFromJsonAsync<PagedResult<EmailLogDto>>("/api/admin/logs/email?status=Sent&entityType=Invoice&page=1&pageSize=10");
        var exportResponse = await client.GetAsync("/api/admin/logs/audit/export?action=SentInvoiceEmail");

        Assert.NotNull(auditLogs);
        Assert.NotEmpty(auditLogs.Items);
        Assert.True(auditLogs.TotalCount >= 1);
        Assert.Contains(auditLogs.Items, log => log.Action == "SentInvoiceEmail");
        Assert.NotNull(emailLogs);
        Assert.Contains(emailLogs.Items, log => log.RelatedEntityType == "Invoice");
        exportResponse.EnsureSuccessStatusCode();
        Assert.Equal("text/csv", exportResponse.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task AdminCanGenerateAndSendStatements()
    {
        var client = factory.CreateClient();
        var login = await Login(client, "admin@storycoffee.co.nz");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var generateResponse = await client.PostAsync("/api/admin/statements/generate-weekly", null);
        generateResponse.EnsureSuccessStatusCode();
        var statements = await generateResponse.Content.ReadFromJsonAsync<List<StatementDto>>();
        var statement = statements!.First();

        var sendResponse = await client.PostAsync($"/api/admin/statements/{statement.Id}/send-email", null);
        sendResponse.EnsureSuccessStatusCode();
        var sent = await sendResponse.Content.ReadFromJsonAsync<StatementDto>();

        Assert.Equal(StatementStatus.Sent, sent!.Status);
        Assert.Equal(EmailStatus.Sent, sent.EmailStatus);
    }

    [Fact]
    public async Task CustomerCannotDownloadAnotherCustomersInvoicePdf()
    {
        var adminClient = factory.CreateClient();
        var adminLogin = await Login(adminClient, "admin@storycoffee.co.nz");
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminLogin.AccessToken);
        var orders = await adminClient.GetFromJsonAsync<List<OrderDto>>("/api/admin/orders");
        var otherCustomerOrder = orders!.First(order => order.CustomerId != SeedData.AucklandCustomerId && order.OrderStatus == OrderStatus.InProduction);
        await adminClient.PostAsync($"/api/admin/orders/{otherCustomerOrder.Id}/mark-ready-to-ship", null);
        await adminClient.PostAsync($"/api/admin/orders/{otherCustomerOrder.Id}/mark-shipped", null);
        var invoices = await adminClient.GetFromJsonAsync<List<InvoiceDto>>("/api/admin/invoices");
        var otherCustomerInvoice = invoices!.First(invoice => invoice.CustomerId != SeedData.AucklandCustomerId);

        var client = factory.CreateClient();
        var login = await Login(client, "john@aucklandcafe.co.nz");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var response = await client.GetAsync($"/api/customer/invoices/{otherCustomerInvoice.Id}/download-url");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AdminCanReadAndCompleteProductionItem()
    {
        var client = factory.CreateClient();
        var login = await Login(client, "admin@storycoffee.co.nz");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var items = await client.GetFromJsonAsync<List<ProductionItemDto>>("/api/admin/production/current");
        var item = items!.First(productionItem => productionItem.Status == ProductionStatus.Pending);

        var startResponse = await client.PostAsync($"/api/admin/production/{item.ProductId}/start", null);
        startResponse.EnsureSuccessStatusCode();
        var started = await startResponse.Content.ReadFromJsonAsync<ProductionItemDto>();

        var completeResponse = await client.PostAsync($"/api/admin/production/{item.ProductId}/complete", null);
        completeResponse.EnsureSuccessStatusCode();
        var completed = await completeResponse.Content.ReadFromJsonAsync<ProductionItemDto>();

        Assert.Equal(ProductionStatus.InProgress, started!.Status);
        Assert.Equal(ProductionStatus.Completed, completed!.Status);
    }

    [Fact]
    public async Task CustomerCanReadProductsAndStandingOrder()
    {
        var client = factory.CreateClient();
        var login = await Login(client, "john@aucklandcafe.co.nz");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var products = await client.GetFromJsonAsync<List<CustomerProductDto>>("/api/customer/products");
        var standingOrder = await client.GetFromJsonAsync<StandingOrderDto>("/api/customer/standing-order");

        Assert.NotNull(products);
        Assert.NotEmpty(products);
        Assert.All(products, product => Assert.True(product.EffectivePrice > 0));
        Assert.NotNull(standingOrder);
        Assert.Equal(login.UserProfile.CustomerId, standingOrder.CustomerId);
        Assert.NotEmpty(standingOrder.Items);
    }

    [Fact]
    public async Task AdminCanManageCustomerPriceBookAndGeneratedOrderUsesEffectivePrice()
    {
        var client = factory.CreateClient();
        var login = await Login(client, "admin@storycoffee.co.nz");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var products = await client.GetFromJsonAsync<List<ProductDto>>("/api/products");
        var product = products!.First(item => item.Sku == "HB-1KG");

        var updateResponse = await client.PutAsJsonAsync($"/api/admin/customers/{SeedData.AucklandCustomerId}/price-book", new UpdateCustomerPriceBookRequest([
            new UpdateCustomerPriceBookItemRequest(product.Id, 35.25m, true, "Integration override")
        ]));
        updateResponse.EnsureSuccessStatusCode();
        var priceBook = await updateResponse.Content.ReadFromJsonAsync<CustomerPriceBookDto>();
        var standingOrders = await client.GetFromJsonAsync<List<StandingOrderDto>>("/api/admin/standing-orders");
        var standingOrder = standingOrders!.First(order => order.CustomerId == SeedData.AucklandCustomerId);
        var generateResponse = await client.PostAsync($"/api/admin/standing-orders/{standingOrder.Id}/generate-now", null);
        generateResponse.EnsureSuccessStatusCode();
        var generatedOrder = await generateResponse.Content.ReadFromJsonAsync<OrderDto>();

        Assert.Contains(priceBook!.Items, item => item.ProductId == product.Id && item.EffectivePrice == 35.25m && item.HasOverride);
        Assert.Contains(generatedOrder!.Items, item => item.ProductId == product.Id && item.UnitPriceSnapshot == 35.25m);
    }

    [Fact]
    public async Task AdminCanCreateAndUpdateCustomer()
    {
        var client = factory.CreateClient();
        var login = await Login(client, "admin@storycoffee.co.nz");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var createResponse = await client.PostAsJsonAsync("/api/admin/customers", new CreateCustomerRequest(
            "North Shore Cafe",
            "Emma Wilson",
            "emma@northshorecafe.co.nz",
            "+64 9 555 0188",
            "20 Lake Road, Auckland 0622",
            "20 Lake Road, Auckland 0622",
            "Net 14",
            AccountStatus.Draft));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<CustomerDto>();

        var updateResponse = await client.PatchAsJsonAsync($"/api/admin/customers/{created!.Id}", new UpdateCustomerRequest(
            "North Shore Cafe",
            "Emma Wilson",
            "accounts@northshorecafe.co.nz",
            "+64 9 555 0188",
            "20 Lake Road, Auckland 0622",
            "20 Lake Road, Auckland 0622",
            "Net 30",
            AccountStatus.Active));
        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<CustomerDto>();

        Assert.Equal(AccountStatus.Active, updated!.AccountStatus);
        Assert.Equal("Net 30", updated.PaymentTerms);
        Assert.Equal("accounts@northshorecafe.co.nz", updated.Email);
    }

    [Fact]
    public async Task SuspendedCustomerCannotLoginOrUseExistingCustomerToken()
    {
        var customerClient = factory.CreateClient();
        var customerLogin = await Login(customerClient, "john@aucklandcafe.co.nz");
        customerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customerLogin.AccessToken);

        var adminClient = factory.CreateClient();
        var adminLogin = await Login(adminClient, "admin@storycoffee.co.nz");
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminLogin.AccessToken);
        var updateResponse = await adminClient.PatchAsJsonAsync($"/api/admin/customers/{SeedData.AucklandCustomerId}", new UpdateCustomerRequest(
            "Auckland Cafe",
            "John Smith",
            "john@aucklandcafe.co.nz",
            "+64 9 555 0101",
            "12 Queen Street, Auckland 1010",
            "12 Queen Street, Auckland 1010",
            "Net 14",
            AccountStatus.Suspended));
        updateResponse.EnsureSuccessStatusCode();

        var existingTokenResponse = await customerClient.GetAsync("/api/customer/orders");
        var loginResponse = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest("john@aucklandcafe.co.nz", "password"));
        var error = await existingTokenResponse.Content.ReadFromJsonAsync<ApiError>();
        var loginError = await loginResponse.Content.ReadFromJsonAsync<ApiError>();

        Assert.Equal(HttpStatusCode.Forbidden, existingTokenResponse.StatusCode);
        Assert.Equal("customer_account_inactive", error!.Code);
        Assert.Equal(HttpStatusCode.Forbidden, loginResponse.StatusCode);
        Assert.Equal("customer_account_inactive", loginError!.Code);
    }

    [Fact]
    public async Task AdminCanSendCustomerInviteAndReadDashboard()
    {
        var client = factory.CreateClient();
        var login = await Login(client, "admin@storycoffee.co.nz");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var createResponse = await client.PostAsJsonAsync("/api/admin/customers", new CreateCustomerRequest(
            $"Invite Cafe {suffix}",
            "Ivy Chen",
            $"ivy.{suffix}@invitecafe.co.nz",
            "+64 9 555 7001",
            "1 Invite Road, Auckland",
            "1 Invite Road, Auckland",
            "Net 14",
            AccountStatus.Draft));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<CustomerDto>();

        var inviteResponse = await client.PostAsync($"/api/admin/customers/{created!.Id}/send-invite", null);
        inviteResponse.EnsureSuccessStatusCode();
        var invited = await inviteResponse.Content.ReadFromJsonAsync<CustomerDto>();
        var dashboard = await client.GetFromJsonAsync<AdminDashboardDto>("/api/admin/dashboard");
        await using var verifyScope = factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Equal(AccountStatus.Invited, invited!.AccountStatus);
        Assert.NotNull(dashboard);
        Assert.True(dashboard.Metrics.TotalCustomerCount >= 3);
        Assert.Contains(await verifyDb.EmailLogs.ToListAsync(), log => log.RelatedEntityType == "CustomerInvite" && log.RelatedEntityId == created.Id);
    }

    [Fact]
    public async Task AdminCanCreateAndUpdateProduct()
    {
        var client = factory.CreateClient();
        var login = await Login(client, "admin@storycoffee.co.nz");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var createResponse = await client.PostAsJsonAsync("/api/admin/products", new CreateProductRequest(
            "PNG-1KG",
            "Papua New Guinea 1kg",
            "Single origin PNG coffee beans",
            "kg",
            48,
            34,
            true));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>();

        var updateResponse = await client.PatchAsJsonAsync($"/api/admin/products/{created!.Id}", new UpdateProductRequest(
            "PNG-1KG",
            "Papua New Guinea 1kg",
            "Updated single origin PNG coffee beans",
            "kg",
            49,
            35,
            false));
        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<ProductDto>();

        var archiveResponse = await client.PostAsync($"/api/admin/products/{created!.Id}/archive", null);
        archiveResponse.EnsureSuccessStatusCode();
        var archived = await archiveResponse.Content.ReadFromJsonAsync<ProductDto>();

        Assert.Equal(49, updated!.Price);
        Assert.False(updated.IsActive);
        Assert.Equal("PNG-1KG", updated.Sku);
        Assert.False(archived!.IsActive);
    }

    [Fact]
    public async Task CustomerCanReadAndUpdateOwnProfile()
    {
        var client = factory.CreateClient();
        var login = await Login(client, "john@aucklandcafe.co.nz");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var profile = await client.GetFromJsonAsync<CustomerDto>("/api/customer/profile");
        var response = await client.PutAsJsonAsync("/api/customer/profile", new UpdateCustomerProfileRequest(
            "Auckland Cafe Profile",
            "John Profile",
            "john.profile@aucklandcafe.co.nz",
            "+64 9 555 0177",
            "14 Queen Street, Auckland 1010",
            "14 Queen Street, Auckland 1010"));

        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<CustomerDto>();
        Assert.Equal(login.UserProfile.CustomerId, profile!.Id);
        Assert.Equal(login.UserProfile.CustomerId, updated!.Id);
        Assert.Equal("Auckland Cafe Profile", updated.BusinessName);
        Assert.Equal(profile.PaymentTerms, updated.PaymentTerms);
        Assert.Equal(profile.AccountStatus, updated.AccountStatus);
    }

    [Fact]
    public async Task CustomerCanChangePassword()
    {
        var email = $"password-test-{Guid.NewGuid():N}@storycoffee.co.nz";
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                BusinessName = "Password Test Cafe",
                ContactPerson = "Casey Morgan",
                Email = email,
                Phone = "+64 9 555 6100",
                BillingAddress = "1 Test Lane, Auckland",
                DeliveryAddress = "1 Test Lane, Auckland",
                PaymentTerms = "Net 14",
                AccountStatus = AccountStatus.Active
            };
            db.Customers.Add(customer);
            db.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = passwordHasher.Hash("old-password"),
                DisplayName = "Casey Morgan",
                Role = UserRole.Customer,
                CustomerId = customer.Id,
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        var client = factory.CreateClient();
        var login = await Login(client, email, "old-password");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var response = await client.PostAsJsonAsync("/api/customer/password", new ChangePasswordRequest(
            "old-password",
            "new-password",
            "new-password"));

        response.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = null;
        var oldLoginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "old-password"));
        var newLogin = await Login(client, email, "new-password");
        await using var verifyScope = factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Equal(HttpStatusCode.Unauthorized, oldLoginResponse.StatusCode);
        Assert.Equal(email, newLogin.UserProfile.Email);
        Assert.Contains(await verifyDb.AuditLogs.ToListAsync(), log => log.Action == "ChangedPassword" && log.Message.Contains(email));
    }

    [Fact]
    public async Task CustomerCanUpdateOwnStandingOrder()
    {
        var client = factory.CreateClient();
        var login = await Login(client, "sarah@wellingtoncoffee.co.nz");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var products = await client.GetFromJsonAsync<List<ProductDto>>("/api/products");
        var product = products!.First(product => product.Sku == "COL-1KG");

        var response = await client.PutAsJsonAsync("/api/customer/standing-order", new UpdateStandingOrderRequest(
            OrderFrequency.Monthly,
            "Use customer entrance",
            [new UpdateStandingOrderItemRequest(product.Id, 2, "Whole beans")]));

        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<StandingOrderDto>();
        Assert.Equal(login.UserProfile.CustomerId, updated!.CustomerId);
        Assert.Equal(OrderFrequency.Monthly, updated.Frequency);
        Assert.Collection(updated.Items, item =>
        {
            Assert.Equal(product.Id, item.ProductId);
            Assert.Equal(2, item.Quantity);
        });
    }

    [Fact]
    public async Task AdminCanGenerateStandingOrderNow()
    {
        var client = factory.CreateClient();
        var login = await Login(client, "admin@storycoffee.co.nz");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var standingOrders = await client.GetFromJsonAsync<List<StandingOrderDto>>("/api/admin/standing-orders");
        var standingOrder = standingOrders!.First();

        var response = await client.PostAsync($"/api/admin/standing-orders/{standingOrder.Id}/generate-now", null);

        response.EnsureSuccessStatusCode();
        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        Assert.Equal(standingOrder.Id, order!.StandingOrderId);
        Assert.Equal(OrderStatus.Generated, order.OrderStatus);
        Assert.Equal(InvoiceStatus.NotIssued, order.InvoiceStatus);
        Assert.NotEmpty(order.Items);
    }

    [Fact]
    public async Task AdminCanCreateAndUpdateStandingOrder()
    {
        var client = factory.CreateClient();
        var login = await Login(client, "admin@storycoffee.co.nz");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var createCustomerResponse = await client.PostAsJsonAsync("/api/admin/customers", new CreateCustomerRequest(
            $"Ponsonby Coffee {suffix}",
            "Morgan Lee",
            $"morgan.{suffix}@ponsonbycoffee.co.nz",
            "+64 9 555 4001",
            "10 Ponsonby Road, Auckland",
            "10 Ponsonby Road, Auckland",
            "Net 14",
            AccountStatus.Active));
        createCustomerResponse.EnsureSuccessStatusCode();
        var customer = await createCustomerResponse.Content.ReadFromJsonAsync<CustomerDto>();
        var products = await client.GetFromJsonAsync<List<ProductDto>>("/api/products");
        var product = products!.First(product => product.IsActive);

        var createResponse = await client.PostAsJsonAsync("/api/admin/standing-orders", new CreateAdminStandingOrderRequest(
            customer!.Id,
            OrderFrequency.Weekly,
            DateTimeOffset.UtcNow.Date.AddDays(2),
            StandingOrderStatus.Active,
            "Morning delivery",
            "VIP account",
            [new UpdateStandingOrderItemRequest(product.Id, 2, "Whole beans")]));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<StandingOrderDto>();

        var updateResponse = await client.PatchAsJsonAsync($"/api/admin/standing-orders/{created!.Id}", new UpdateAdminStandingOrderRequest(
            OrderFrequency.Monthly,
            created.NextClosingDate.AddDays(14),
            StandingOrderStatus.Paused,
            "Afternoon delivery",
            "Review before next run",
            [new UpdateStandingOrderItemRequest(product.Id, 3, "Ground")]));
        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<StandingOrderDto>();

        Assert.Equal(customer.Id, created.CustomerId);
        Assert.Equal(OrderFrequency.Weekly, created.Frequency);
        Assert.Equal(OrderFrequency.Monthly, updated!.Frequency);
        Assert.Equal(StandingOrderStatus.Paused, updated.Status);
        Assert.Collection(updated.Items, item => Assert.Equal(3, item.Quantity));
    }

    [Fact]
    public async Task AdminCanPauseAndResumeStandingOrder()
    {
        var client = factory.CreateClient();
        var login = await Login(client, "admin@storycoffee.co.nz");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var standingOrders = await client.GetFromJsonAsync<List<StandingOrderDto>>("/api/admin/standing-orders");
        var standingOrder = standingOrders!.First(order => order.Status == StandingOrderStatus.Active);

        var pauseResponse = await client.PostAsync($"/api/admin/standing-orders/{standingOrder.Id}/pause", null);
        pauseResponse.EnsureSuccessStatusCode();
        var paused = await pauseResponse.Content.ReadFromJsonAsync<StandingOrderDto>();

        var generateResponse = await client.PostAsync($"/api/admin/standing-orders/{standingOrder.Id}/generate-now", null);

        var resumeResponse = await client.PostAsync($"/api/admin/standing-orders/{standingOrder.Id}/resume", null);
        resumeResponse.EnsureSuccessStatusCode();
        var resumed = await resumeResponse.Content.ReadFromJsonAsync<StandingOrderDto>();

        Assert.Equal(StandingOrderStatus.Paused, paused!.Status);
        Assert.Equal(HttpStatusCode.BadRequest, generateResponse.StatusCode);
        Assert.Equal(StandingOrderStatus.Active, resumed!.Status);
    }

    [Fact]
    public async Task AdminCanRunStandingOrderGenerationJob()
    {
        var client = factory.CreateClient();
        var login = await Login(client, "admin@storycoffee.co.nz");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var createCustomerResponse = await client.PostAsJsonAsync("/api/admin/customers", new CreateCustomerRequest(
            $"Grey Lynn Coffee {suffix}",
            "Taylor Brown",
            $"taylor.{suffix}@greylynncoffee.co.nz",
            "+64 9 555 5001",
            "20 Great North Road, Auckland",
            "20 Great North Road, Auckland",
            "Net 14",
            AccountStatus.Active));
        createCustomerResponse.EnsureSuccessStatusCode();
        var customer = await createCustomerResponse.Content.ReadFromJsonAsync<CustomerDto>();
        var products = await client.GetFromJsonAsync<List<ProductDto>>("/api/products");
        var product = products!.First(product => product.IsActive);
        var createStandingOrderResponse = await client.PostAsJsonAsync("/api/admin/standing-orders", new CreateAdminStandingOrderRequest(
            customer!.Id,
            OrderFrequency.Weekly,
            DateTimeOffset.UtcNow.AddDays(-1),
            StandingOrderStatus.Active,
            "Leave at reception",
            null,
            [new UpdateStandingOrderItemRequest(product.Id, 1, null)]));
        createStandingOrderResponse.EnsureSuccessStatusCode();

        var runResponse = await client.PostAsync("/api/admin/jobs/standing-orders/run", null);
        runResponse.EnsureSuccessStatusCode();
        var execution = await runResponse.Content.ReadFromJsonAsync<JobExecutionLogDto>();
        var executions = await client.GetFromJsonAsync<List<JobExecutionLogDto>>("/api/admin/jobs/executions");

        Assert.Equal(JobExecutionStatus.Succeeded, execution!.Status);
        Assert.True(execution.ItemsSucceeded >= 1);
        Assert.NotNull(executions);
        Assert.Contains(executions, log => log.Id == execution.Id);
    }

    [Fact]
    public async Task CustomerStatementRead_IsRestrictedToOwnCustomer()
    {
        var adminClient = factory.CreateClient();
        var adminLogin = await Login(adminClient, "admin@storycoffee.co.nz");
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminLogin.AccessToken);
        await adminClient.PostAsync("/api/admin/statements/generate-weekly", null);

        var client = factory.CreateClient();
        var login = await Login(client, "john@aucklandcafe.co.nz");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var statements = await client.GetFromJsonAsync<List<StatementDto>>("/api/customer/statements");

        Assert.NotNull(statements);
        Assert.NotEmpty(statements);
        Assert.All(statements, statement => Assert.Equal(login.UserProfile.CustomerId, statement.CustomerId));
    }

    [Fact]
    public async Task AdminOrderAction_UpdatesState()
    {
        var client = factory.CreateClient();
        var login = await Login(client, "admin@storycoffee.co.nz");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var orders = await client.GetFromJsonAsync<List<OrderDto>>("/api/admin/orders");
        var generatedOrder = orders!.First(order => order.OrderStatus == OrderStatus.Generated);

        var response = await client.PostAsync($"/api/admin/orders/{generatedOrder.Id}/send-to-production", null);
        var updated = await response.Content.ReadFromJsonAsync<OrderDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(OrderStatus.InProduction, updated!.OrderStatus);
    }

    [Fact]
    public async Task AdminBatchToProduction_UpdatesOrdersAndReturnsBatch()
    {
        var client = factory.CreateClient();
        var login = await Login(client, "admin@storycoffee.co.nz");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var orders = await client.GetFromJsonAsync<List<OrderDto>>("/api/admin/orders");
        var generatedOrder = orders!.First(order => order.OrderStatus == OrderStatus.Generated);

        var response = await client.PostAsJsonAsync("/api/admin/orders/batch-to-production", new BatchToProductionRequest([generatedOrder.Id]));

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<BatchToProductionResponse>();
        Assert.Equal(1, result!.Updated);
        Assert.Contains(result.Orders, order => order.Id == generatedOrder.Id && order.OrderStatus == OrderStatus.InProduction);
        Assert.Equal(ProductionBatchStatus.Open, result.ProductionBatch.Status);
    }

    [Fact]
    public async Task OutboxProcessor_ReclaimsStaleEmailLockAndMarksEmailSent()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();
        var now = DateTimeOffset.UtcNow;
        var emailLog = new EmailLog
        {
            Id = Guid.NewGuid(),
            RelatedEntityType = "Test",
            RelatedEntityId = Guid.NewGuid(),
            RecipientEmail = "ops@storycoffee.co.nz",
            Subject = "StoryCoffee outbox test",
            Status = EmailStatus.Pending,
            CreatedAt = now
        };
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = OutboxMessageTypes.Email,
            Payload = JsonSerializer.Serialize(new OutboxEmailPayload(
                emailLog.RelatedEntityType,
                emailLog.RelatedEntityId,
                emailLog.Id,
                emailLog.RecipientEmail,
                emailLog.Subject,
                "Outbox test body"),
                new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            Status = OutboxStatus.Processing,
            Attempts = 1,
            MaxAttempts = 5,
            AvailableAt = now.AddMinutes(-10),
            LockedAt = now.AddMinutes(-10),
            CreatedAt = now.AddMinutes(-10),
            UpdatedAt = now.AddMinutes(-10)
        };
        db.EmailLogs.Add(emailLog);
        db.OutboxMessages.Add(outboxMessage);
        await db.SaveChangesAsync();

        var processed = await processor.ProcessBatch(CancellationToken.None);

        await db.Entry(emailLog).ReloadAsync();
        await db.Entry(outboxMessage).ReloadAsync();
        Assert.Equal(1, processed);
        Assert.Equal(EmailStatus.Sent, emailLog.Status);
        Assert.NotNull(emailLog.SentAt);
        Assert.Equal(OutboxStatus.Succeeded, outboxMessage.Status);
        Assert.Null(outboxMessage.LockedAt);
        Assert.NotNull(outboxMessage.ProcessedAt);
    }

    [Fact]
    public async Task SesWebhook_BounceReconcilesEmailLogAndInvoiceStatus()
    {
        var client = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var invoice = await db.Invoices.FirstAsync();
        invoice.EmailStatus = EmailStatus.Sent;
        var emailLog = new EmailLog
        {
            Id = Guid.NewGuid(),
            RelatedEntityType = "Invoice",
            RelatedEntityId = invoice.Id,
            RecipientEmail = "bounce-target@storycoffee.co.nz",
            Subject = $"StoryCoffee invoice {invoice.InvoiceNumber}",
            Status = EmailStatus.Sent,
            Provider = "SES",
            ProviderMessageId = $"ses-{Guid.NewGuid():N}",
            CreatedAt = DateTimeOffset.UtcNow,
            SentAt = DateTimeOffset.UtcNow
        };
        db.EmailLogs.Add(emailLog);
        await db.SaveChangesAsync();
        var snsMessageId = $"sns-{Guid.NewGuid():N}";
        var webhookPayload = new
        {
            Type = "Notification",
            MessageId = snsMessageId,
            Message = JsonSerializer.Serialize(new
            {
                notificationType = "Bounce",
                mail = new
                {
                    timestamp = DateTimeOffset.UtcNow.ToString("O"),
                    source = "no-reply@storycoffee.co.nz",
                    messageId = emailLog.ProviderMessageId,
                    destination = new[] { emailLog.RecipientEmail }
                },
                bounce = new
                {
                    bounceType = "Permanent",
                    timestamp = DateTimeOffset.UtcNow.ToString("O"),
                    bouncedRecipients = new[]
                    {
                        new
                        {
                            emailAddress = emailLog.RecipientEmail,
                            diagnosticCode = "smtp; 550 mailbox unavailable"
                        }
                    }
                }
            })
        };

        var webhookResponse = await client.PostAsJsonAsync("/api/webhooks/ses", webhookPayload);
        webhookResponse.EnsureSuccessStatusCode();
        var result = await webhookResponse.Content.ReadFromJsonAsync<EmailWebhookResult>();
        var duplicateResponse = await client.PostAsJsonAsync("/api/webhooks/ses", webhookPayload);
        var duplicate = await duplicateResponse.Content.ReadFromJsonAsync<EmailWebhookResult>();

        await db.Entry(emailLog).ReloadAsync();
        await db.Entry(invoice).ReloadAsync();
        var eventCount = await db.EmailDeliveryEvents.CountAsync(deliveryEvent => deliveryEvent.ProviderEventId == snsMessageId);
        Assert.Equal(EmailStatus.Bounced, result!.EmailStatus);
        Assert.False(result.Duplicate);
        Assert.True(duplicate!.Duplicate);
        Assert.Equal(1, eventCount);
        Assert.Equal(EmailStatus.Bounced, emailLog.Status);
        Assert.Equal("Bounce", emailLog.LastProviderEventType);
        Assert.Contains("550 mailbox unavailable", emailLog.ErrorMessage);
        Assert.Equal(EmailStatus.Bounced, invoice.EmailStatus);
    }

    [Fact]
    public async Task AdminRoutes_RejectAnonymousCalls()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/admin/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<LoginResponse> Login(HttpClient client, string email)
    {
        return await Login(client, email, "password");
    }

    private static async Task<LoginResponse> Login(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
    }

    private sealed record PaymentResponse(InvoiceDto Invoice, PaymentRecordDto Payment);
}
