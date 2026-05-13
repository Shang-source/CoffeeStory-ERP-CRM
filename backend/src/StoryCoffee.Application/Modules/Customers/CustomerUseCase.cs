namespace StoryCoffee.Application.Customers;

public sealed class CustomerUseCase(
    ICustomerRepository customers,
    IUnitOfWork unitOfWork,
    IClock clock,
    IEmailSender emailSender,
    IOutboxPublisher outbox) : ICustomerService
{
    public async Task<IReadOnlyList<CustomerDto>> GetCustomers(CancellationToken cancellationToken)
    {
        var result = await customers.GetCustomers(cancellationToken);
        return result.Select(customer => customer.ToDto()).ToList();
    }

    public async Task<CustomerDto> GetCustomer(Guid customerId, CancellationToken cancellationToken)
    {
        var customer = await customers.GetCustomer(customerId, cancellationToken)
            ?? throw new KeyNotFoundException("Customer not found.");
        return customer.ToDto();
    }

    public Task<CustomerDto> CreateCustomer(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        return unitOfWork.ExecuteInTransaction(async token =>
        {
            ValidateCustomerFields(request.BusinessName, request.Email);
            var normalizedEmail = request.Email.Trim();
            if (await customers.CustomerEmailExists(null, normalizedEmail, token))
            {
                throw new InvalidOperationException("A customer with this email already exists.");
            }

            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                BusinessName = request.BusinessName.Trim(),
                ContactPerson = request.ContactPerson.Trim(),
                Email = normalizedEmail,
                Phone = request.Phone.Trim(),
                BillingAddress = request.BillingAddress.Trim(),
                DeliveryAddress = request.DeliveryAddress.Trim(),
                PaymentTerms = NormalizePaymentTerms(request.PaymentTerms),
                AccountStatus = request.AccountStatus,
                CreatedAt = clock.UtcNow
            };

            customers.AddCustomer(customer);
            customers.AddAuditChange("CreatedCustomer", "Customer", customer.Id, $"Created customer {customer.BusinessName}", null, CustomerAuditValues(customer));
            return customer.ToDto();
        }, cancellationToken);
    }

    public Task<CustomerDto> UpdateCustomer(Guid customerId, UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        return unitOfWork.ExecuteInTransaction(async token =>
        {
            var customer = await customers.GetCustomer(customerId, token)
                ?? throw new KeyNotFoundException("Customer not found.");
            ValidateCustomerFields(request.BusinessName, request.Email);
            await EnsureUniqueCustomerEmail(customerId, request.Email.Trim(), token);
            if (request.AccountStatus == AccountStatus.Archived && customer.AccountStatus != AccountStatus.Archived)
            {
                await EnsureCustomerCanBeArchived(customerId, token);
            }

            var oldValues = CustomerAuditValues(customer);

            customer.BusinessName = request.BusinessName.Trim();
            customer.ContactPerson = request.ContactPerson.Trim();
            customer.Email = request.Email.Trim();
            customer.Phone = request.Phone.Trim();
            customer.BillingAddress = request.BillingAddress.Trim();
            customer.DeliveryAddress = request.DeliveryAddress.Trim();
            customer.PaymentTerms = NormalizePaymentTerms(request.PaymentTerms);
            customer.AccountStatus = request.AccountStatus;
            customers.AddAuditChange("UpdatedCustomer", "Customer", customer.Id, $"Updated customer {customer.BusinessName}", oldValues, CustomerAuditValues(customer));

            return customer.ToDto();
        }, cancellationToken);
    }

    public async Task<CustomerDto> SendCustomerInvite(Guid customerId, CancellationToken cancellationToken)
    {
        var customer = await customers.GetCustomer(customerId, cancellationToken)
            ?? throw new KeyNotFoundException("Customer not found.");
        if (customer.AccountStatus is AccountStatus.Archived or AccountStatus.Suspended)
        {
            throw new InvalidOperationException("Suspended or archived customers cannot receive invite emails.");
        }

        var oldValues = CustomerAuditValues(customer);
        if (customer.AccountStatus == AccountStatus.Draft)
        {
            customer.AccountStatus = AccountStatus.Invited;
        }

        var subject = "Welcome to StoryCoffee";
        var emailLog = customers.AddEmailLog("CustomerInvite", customer.Id, customer.Email, subject, EmailStatus.Pending);
        var message = new EmailMessage(customer.Email, subject, "Your StoryCoffee customer account is ready. Please sign in to manage your coffee orders.");
        var outboxMessage = outbox.EnqueueEmail(new OutboxEmailPayload("CustomerInvite", customer.Id, emailLog.Id, message.RecipientEmail, message.Subject, message.Body));
        customers.AddAuditChange("SentCustomerInvite", "Customer", customer.Id, $"Sent invite email to {customer.Email}", oldValues, CustomerAuditValues(customer));
        await unitOfWork.SaveChanges(cancellationToken);

        var sendResult = await emailSender.Send(message, cancellationToken);
        emailLog.Provider = emailSender.ProviderName;
        emailLog.ProviderMessageId = sendResult.ProviderMessageId;
        if (sendResult.Succeeded)
        {
            emailLog.Status = EmailStatus.Sent;
            emailLog.SentAt = clock.UtcNow;
            emailLog.ErrorMessage = null;
            outboxMessage.Status = OutboxStatus.Succeeded;
            outboxMessage.ProcessedAt = clock.UtcNow;
            outboxMessage.UpdatedAt = clock.UtcNow;
        }
        else
        {
            emailLog.Status = EmailStatus.Failed;
            emailLog.ErrorMessage = sendResult.ErrorMessage ?? "Email provider failed.";
            outboxMessage.Attempts = 1;
            outboxMessage.ErrorMessage = emailLog.ErrorMessage;
            outboxMessage.UpdatedAt = clock.UtcNow;
        }

        await unitOfWork.SaveChanges(cancellationToken);
        return customer.ToDto();
    }

    public Task<CustomerDto> UpdateCustomerProfile(Guid customerId, UpdateCustomerProfileRequest request, CancellationToken cancellationToken)
    {
        return unitOfWork.ExecuteInTransaction(async token =>
        {
            var customer = await customers.GetCustomer(customerId, token)
                ?? throw new KeyNotFoundException("Customer not found.");
            ValidateCustomerFields(request.BusinessName, request.Email);
            await EnsureUniqueCustomerEmail(customerId, request.Email.Trim(), token);
            var oldValues = CustomerProfileAuditValues(customer);

            customer.BusinessName = request.BusinessName.Trim();
            customer.ContactPerson = request.ContactPerson.Trim();
            customer.Email = request.Email.Trim();
            customer.Phone = request.Phone.Trim();
            customer.BillingAddress = request.BillingAddress.Trim();
            customer.DeliveryAddress = request.DeliveryAddress.Trim();
            customers.AddAuditChange("UpdatedCustomerProfile", "Customer", customer.Id, $"Updated customer profile for {customer.BusinessName}", oldValues, CustomerProfileAuditValues(customer));

            return customer.ToDto();
        }, cancellationToken);
    }

    private async Task EnsureUniqueCustomerEmail(Guid customerId, string email, CancellationToken cancellationToken)
    {
        if (await customers.CustomerEmailExists(customerId, email, cancellationToken))
        {
            throw new InvalidOperationException("A customer with this email already exists.");
        }
    }

    private async Task EnsureCustomerCanBeArchived(Guid customerId, CancellationToken cancellationToken)
    {
        var blockers = await customers.GetArchiveBlockers(customerId, cancellationToken);
        var reasons = new List<string>();
        if (blockers.ActiveStandingOrders > 0)
        {
            reasons.Add($"{blockers.ActiveStandingOrders} active or paused standing order(s)");
        }

        if (blockers.OpenOrders > 0)
        {
            reasons.Add($"{blockers.OpenOrders} non-terminal order(s)");
        }

        if (blockers.UnsettledInvoices > 0)
        {
            reasons.Add($"{blockers.UnsettledInvoices} unsettled invoice(s)");
        }

        if (reasons.Count > 0)
        {
            throw new ApiException(
                400,
                "customer_archive_blocked",
                $"Customer cannot be archived while it has {string.Join(", ", reasons)}.");
        }
    }

    private static void ValidateCustomerFields(string businessName, string email)
    {
        if (string.IsNullOrWhiteSpace(businessName))
        {
            throw new InvalidOperationException("Business name is required.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Email is required.");
        }
    }

    private static string NormalizePaymentTerms(string paymentTerms)
    {
        return string.IsNullOrWhiteSpace(paymentTerms) ? "Net 14" : paymentTerms.Trim();
    }

    private static object CustomerAuditValues(Customer customer)
    {
        return new
        {
            customer.BusinessName,
            customer.ContactPerson,
            customer.Email,
            customer.Phone,
            customer.BillingAddress,
            customer.DeliveryAddress,
            customer.PaymentTerms,
            customer.AccountStatus
        };
    }

    private static object CustomerProfileAuditValues(Customer customer)
    {
        return new
        {
            customer.BusinessName,
            customer.ContactPerson,
            customer.Email,
            customer.Phone,
            customer.BillingAddress,
            customer.DeliveryAddress
        };
    }
}
