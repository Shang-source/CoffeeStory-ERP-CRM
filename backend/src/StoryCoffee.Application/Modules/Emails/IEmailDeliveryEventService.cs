namespace StoryCoffee.Application.Emails;

public interface IEmailDeliveryEventService
{
    Task<EmailWebhookResult> ProcessSesWebhook(string payload, CancellationToken cancellationToken);
}
