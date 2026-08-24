using OmniCore.Shared.Events;

namespace OmniCore.OrderService.Messaging.Producers;

public interface IOrderEventPublisher
{
    Task PublishOrderCreatedAsync(
        OrderCreatedEvent orderCreatedEvent,
        CancellationToken cancellationToken = default);
}