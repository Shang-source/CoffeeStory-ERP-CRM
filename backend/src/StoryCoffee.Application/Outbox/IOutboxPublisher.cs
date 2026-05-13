using StoryCoffee.Domain;

namespace StoryCoffee.Application.Outbox;

public interface IOutboxPublisher
{
    OutboxMessage EnqueueEmail(OutboxEmailPayload payload);
}
