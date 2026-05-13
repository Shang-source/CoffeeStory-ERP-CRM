using System.Text.Json;
using StoryCoffee.Application.Common;
using StoryCoffee.Infrastructure.Data;
using StoryCoffee.Domain;
using StoryCoffee.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace StoryCoffee.Infrastructure.Services;

public sealed class OutboxPublisher(AppDbContext db, IClock clock, IOptions<OutboxOptions> options) : IOutboxPublisher
{
    private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);

    public OutboxMessage EnqueueEmail(OutboxEmailPayload payload)
    {
        var now = clock.UtcNow;
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = OutboxMessageTypes.Email,
            Payload = JsonSerializer.Serialize(payload, jsonOptions),
            Status = OutboxStatus.Pending,
            MaxAttempts = options.Value.MaxAttempts,
            AvailableAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.OutboxMessages.Add(message);
        return message;
    }
}
