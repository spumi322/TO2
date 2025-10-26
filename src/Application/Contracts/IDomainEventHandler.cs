using Domain.Common;

namespace Application.Contracts
{
    public interface IDomainEventHandler<T> where T : DomainEvent
    {
        Task HandleAsync(T domainEvent);
    }
}
