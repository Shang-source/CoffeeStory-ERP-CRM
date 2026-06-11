namespace StoryCoffee.Application.Customers;

public sealed class CustomerUseCase(
    ICustomerRepository customers,
    IUnitOfWork unitOfWork,
    IClock clock,
    IEmailSender emailSender,
    IOutboxPublisher outbox,
    IPasswordHasher passwordHasher,
    IPortalLinkProvider portalLinks) : ICustomerService
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
                AccountNumber = await customers.GetNextAccountNumber(token),
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
            throw new ApiException(400, "customer_invite_blocked", "Suspended or archived customers cannot receive invite emails.");
        }

        var temporaryPassword = NormalizeTemporaryPassword(customer.Phone);
        if (temporaryPassword.Length < 8)
        {
            throw new ApiException(400, "customer_invite_missing_contact", "Customer phone must contain at least 8 digits before an invite can be sent.");
        }

        var email = NormalizeEmail(customer.Email);
        var existingPortalUser = GetPortalUser(customer);
        if (existingPortalUser is not null && customer.AccountStatus == AccountStatus.Active && await customers.HasSentCustomerInvite(customer.Id, cancellationToken))
        {
            throw new ApiException(400, "customer_already_invited", "This customer already has a portal account and has already been invited.");
        }

        if (existingPortalUser is null && await customers.UserEmailExists(email, cancellationToken))
        {
            throw new ApiException(400, "customer_email_already_used", "A portal user with this email already exists.");
        }

        var oldValues = CustomerAuditValues(customer);
        var now = clock.UtcNow;
        if (existingPortalUser is null)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = passwordHasher.Hash(temporaryPassword),
                DisplayName = customer.BusinessName,
                Role = UserRole.Customer,
                CustomerId = customer.Id,
                Customer = customer,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };
            customers.AddUser(user);
        }

        var subject = "Welcome to StoryCoffee";
        var emailLog = customers.AddEmailLog("CustomerInvite", customer.Id, customer.Email, subject, EmailStatus.Pending);
        var renderedEmail = BuildInviteEmail(customer, email, temporaryPassword);
        var message = new EmailMessage(customer.Email, subject, renderedEmail.TextBody, HtmlBody: renderedEmail.HtmlBody);
        var outboxMessage = outbox.EnqueueEmail(new OutboxEmailPayload("CustomerInvite", customer.Id, emailLog.Id, message.RecipientEmail, message.Subject, message.Body, HtmlBody: message.HtmlBody));
        var auditAction = existingPortalUser is null ? "SentCustomerInvite" : "ResentCustomerInvite";
        var auditMessage = existingPortalUser is null
            ? $"Created portal account and sent invite email to {customer.Email}"
            : $"Resent invite email to {customer.Email}";
        customers.AddAuditChange(auditAction, "Customer", customer.Id, auditMessage, oldValues, CustomerAuditValues(customer));
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
        if (!sendResult.Succeeded)
        {
            throw new ApiException(
                502,
                "customer_invite_email_failed",
                $"Invite email could not be sent. Check the email provider configuration and retry. Provider error: {emailLog.ErrorMessage}");
        }

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

    private static bool HasPortalUser(Customer customer)
    {
        return GetPortalUser(customer) is not null;
    }

    private static User? GetPortalUser(Customer customer)
    {
        return customer.Users.FirstOrDefault(user => user.Role == UserRole.Customer && user.IsActive);
    }

    private static string NormalizeEmail(string? email)
    {
        return (email ?? "").Trim().ToLowerInvariant();
    }

    private static string NormalizeTemporaryPassword(string? phone)
    {
        return new string((phone ?? "").Where(char.IsDigit).ToArray());
    }

    private RenderedEmail BuildInviteEmail(Customer customer, string email, string temporaryPassword)
    {
        var contactName = string.IsNullOrWhiteSpace(customer.ContactPerson)
            ? customer.BusinessName
            : customer.ContactPerson;
        return StoryCoffeeEmailTemplates.CustomerInvite(contactName, portalLinks.LoginUrl, email, temporaryPassword);
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
            customer.AccountStatus,
            HasPortalUser = HasPortalUser(customer)
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
