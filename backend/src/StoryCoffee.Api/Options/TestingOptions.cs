namespace StoryCoffee.Api.Options;

public sealed class TestingOptions
{
    public bool ResetEnabled { get; init; }
    public string? ResetToken { get; init; }
}
