using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StoryCoffee.Contracts;
using StoryCoffee.Application.Exceptions;
using StoryCoffee.Infrastructure.Options;

namespace StoryCoffee.Api.Controllers;

[ApiController]
[Route("api/webhooks")]
public sealed class EmailWebhooksController(
    IEmailDeliveryEventService emailDeliveryEvents,
    ISnsWebhookSecurityService snsSecurity,
    IOptions<EmailOptions> options) : ControllerBase
{
    [HttpPost("ses")]
    public async Task<EmailWebhookResult> ReceiveSesWebhook([FromBody] JsonElement payload, CancellationToken cancellationToken)
    {
        ValidateWebhookSecret();
        var payloadText = payload.GetRawText();
        var securityResult = await snsSecurity.ValidateAndConfirmIfNeeded(payloadText, cancellationToken);
        var result = await emailDeliveryEvents.ProcessSesWebhook(payloadText, cancellationToken);
        return result with { SubscriptionConfirmationStatus = securityResult.SubscriptionConfirmationStatus };
    }

    private void ValidateWebhookSecret()
    {
        var expectedSecret = options.Value.WebhookSecret;
        if (string.IsNullOrWhiteSpace(expectedSecret))
        {
            return;
        }

        var providedSecret = Request.Headers["X-StoryCoffee-Webhook-Secret"].ToString();
        if (!string.Equals(providedSecret, expectedSecret, StringComparison.Ordinal))
        {
            throw new ApiException(StatusCodes.Status401Unauthorized, "webhook_secret_invalid", "Webhook secret is invalid.");
        }
    }
}
