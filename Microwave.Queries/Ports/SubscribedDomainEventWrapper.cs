using System;

namespace Microwave.Queries.Ports
{
    public class SubscribedDomainEventWrapper
    {
        public string DomainEventType => DomainEvent.GetType().Name;
        public DateTimeOffset Created { get; set; }
        public long Version { get; set; }
        public ISubscribedDomainEvent DomainEvent { get; set; }
    }
}