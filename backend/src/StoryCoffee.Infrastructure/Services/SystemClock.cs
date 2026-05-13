using StoryCoffee.Application.Common;

namespace StoryCoffee.Infrastructure.Services;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
