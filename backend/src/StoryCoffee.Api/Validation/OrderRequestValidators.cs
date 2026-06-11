using FluentValidation;
using StoryCoffee.Contracts;

namespace StoryCoffee.Api.Validation;

public sealed class BatchToProductionRequestValidator : AbstractValidator<BatchToProductionRequest>
{
    public BatchToProductionRequestValidator()
    {
        RuleFor(request => request.OrderIds)
            .NotEmpty()
            .WithMessage("At least one order is required.");
        RuleForEach(request => request.OrderIds)
            .NotEmpty()
            .WithMessage("Order id is required.");
    }
}

public sealed class BatchShipAndInvoiceRequestValidator : AbstractValidator<BatchShipAndInvoiceRequest>
{
    public BatchShipAndInvoiceRequestValidator()
    {
        RuleFor(request => request.OrderIds)
            .NotEmpty()
            .WithMessage("At least one order is required.");
        RuleForEach(request => request.OrderIds)
            .NotEmpty()
            .WithMessage("Order id is required.");
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("Email must be valid.");
        RuleFor(request => request.Password)
            .NotEmpty()
            .WithMessage("Password is required.");
    }
}

public sealed class UpdateProductionItemRequestValidator : AbstractValidator<UpdateProductionItemRequest>
{
    public UpdateProductionItemRequestValidator()
    {
        RuleFor(request => request.ProducedQuantity)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Produced quantity cannot be negative.");
    }
}

public sealed class RecordPaymentRequestValidator : AbstractValidator<RecordPaymentRequest>
{
    public RecordPaymentRequestValidator()
    {
        RuleFor(request => request.Amount)
            .GreaterThan(0)
            .WithMessage("Payment amount must be greater than zero.");
    }
}

public sealed class BatchRecordPaymentsRequestValidator : AbstractValidator<BatchRecordPaymentsRequest>
{
    public BatchRecordPaymentsRequestValidator()
    {
        RuleFor(request => request.InvoiceIds)
            .NotEmpty()
            .WithMessage("At least one invoice is required.");
        RuleForEach(request => request.InvoiceIds)
            .NotEmpty()
            .WithMessage("Invoice id is required.");
        RuleFor(request => request.PaymentDate)
            .NotEmpty()
            .WithMessage("Payment date is required.");
    }
}

public sealed class VoidPaymentRequestValidator : AbstractValidator<VoidPaymentRequest>
{
    public VoidPaymentRequestValidator()
    {
        RuleFor(request => request.Reason)
            .NotEmpty()
            .WithMessage("Void reason is required.");
    }
}
