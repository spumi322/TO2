using Domain.Common;

namespace Application.Contracts
{
    public interface IDomainEventDispatcher
    {
        void QueueEvent(DomainEvent domainEvent);
        Task DispatchQueuedEvents();
    }
}
