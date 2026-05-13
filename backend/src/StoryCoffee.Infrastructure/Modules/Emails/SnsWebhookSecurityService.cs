using Amazon.SimpleNotificationService.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using StoryCoffee.Application.Exceptions;
using StoryCoffee.Infrastructure.Options;

namespace StoryCoffee.Infrastructure.Emails;

public sealed class SnsWebhookSecurityService(
    IOptions<EmailOptions> options,
    ISnsSubscriptionConfirmer subscriptionConfirmer) : ISnsWebhookSecurityService
{
    public async Task<SnsWebhookSecurityResult> ValidateAndConfirmIfNeeded(string payload, CancellationToken cancellationToken)
    {
        if (!options.Value.VerifySnsSignature)
        {
            return new SnsWebhookSecurityResult(null, null, null, null);
        }

        var message = ParseMessage(payload);
        ValidateTopic(message);
        ValidateSigningCertificateUrl(message.SigningCertURL);
        if (!message.IsMessageSignatureValid())
        {
            throw new ApiException(StatusCodes.Status401Unauthorized, "sns_signature_invalid", "SNS message signature is invalid.");
        }

        var confirmationStatus = await ConfirmSubscriptionIfNeeded(message, cancellationToken);
        return new SnsWebhookSecurityResult(message.Type, message.TopicArn, message.MessageId, confirmationStatus);
    }

    private static Message ParseMessage(string payload)
    {
        try
        {
            return Message.ParseMessage(payload);
        }
        catch (Exception ex)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "sns_payload_invalid", $"SNS payload is invalid: {ex.Message}");
        }
    }

    private void ValidateTopic(Message message)
    {
        var expectedTopicArn = options.Value.SnsTopicArn;
        if (string.IsNullOrWhiteSpace(expectedTopicArn))
        {
            return;
        }

        if (!string.Equals(message.TopicArn, expectedTopicArn, StringComparison.Ordinal))
        {
            throw new ApiException(StatusCodes.Status403Forbidden, "sns_topic_not_allowed", "SNS topic is not allowed.");
        }
    }

    private static void ValidateSigningCertificateUrl(string signingCertUrl)
    {
        if (!Uri.TryCreate(signingCertUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            !IsAwsSnsHost(uri.Host) ||
            !uri.AbsolutePath.EndsWith(".pem", StringComparison.OrdinalIgnoreCase))
        {
            throw new ApiException(StatusCodes.Status401Unauthorized, "sns_signing_cert_invalid", "SNS signing certificate URL is invalid.");
        }
    }

    private async Task<string?> ConfirmSubscriptionIfNeeded(Message message, CancellationToken cancellationToken)
    {
        if (!message.IsSubscriptionType)
        {
            return null;
        }

        if (!options.Value.AutoConfirmSnsSubscriptions)
        {
            return "ManualConfirmationRequired";
        }

        await subscriptionConfirmer.ConfirmSubscription(message.SubscribeURL, cancellationToken);
        return "Confirmed";
    }

    private static bool IsAwsSnsHost(string host)
    {
        return host.StartsWith("sns.", StringComparison.OrdinalIgnoreCase)
            && (host.EndsWith(".amazonaws.com", StringComparison.OrdinalIgnoreCase)
                || host.EndsWith(".amazonaws.com.cn", StringComparison.OrdinalIgnoreCase));
    }
}

public interface ISnsSubscriptionConfirmer
{
    Task ConfirmSubscription(string subscribeUrl, CancellationToken cancellationToken);
}

public sealed class SnsSubscriptionConfirmer(HttpClient httpClient) : ISnsSubscriptionConfirmer
{
    public async Task ConfirmSubscription(string subscribeUrl, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(subscribeUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            !IsAwsSnsHost(uri.Host))
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "sns_subscribe_url_invalid", "SNS SubscribeURL is invalid.");
        }

        using var response = await httpClient.GetAsync(uri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(StatusCodes.Status502BadGateway, "sns_subscription_confirmation_failed", "SNS subscription confirmation failed.");
        }
    }

    private static bool IsAwsSnsHost(string host)
    {
        return host.StartsWith("sns.", StringComparison.OrdinalIgnoreCase)
            && (host.EndsWith(".amazonaws.com", StringComparison.OrdinalIgnoreCase)
                || host.EndsWith(".amazonaws.com.cn", StringComparison.OrdinalIgnoreCase));
    }
}
