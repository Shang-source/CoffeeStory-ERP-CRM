namespace StoryCoffee.Infrastructure.Options;

public sealed class EmailOptions
{
    public string Provider { get; init; } = "Stub";
    public string FromAddress { get; init; } = "no-reply@storycoffee.co.nz";
    public string FromName { get; init; } = "StoryCoffee";
    public string SmtpHost { get; init; } = "localhost";
    public int SmtpPort { get; init; } = 1025;
    public string? SmtpUsername { get; init; }
    public string? SmtpPassword { get; init; }
    public bool UseStartTls { get; init; }
    public string SesRegion { get; init; } = "ap-southeast-2";
    public string? SesEndpointUrl { get; init; }
    public string? SesConfigurationSet { get; init; }
    public string? WebhookSecret { get; init; }
    public bool VerifySnsSignature { get; init; } = true;
    public bool AutoConfirmSnsSubscriptions { get; init; }
    public string? SnsTopicArn { get; init; }
}
