﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microwave.Application;
using Microwave.Application.Results;
using Microwave.Domain;

namespace Microwave.Queries
{
    public interface IEventRepository
    {
        Task<Result<IEnumerable<DomainEventWrapper>>> LoadEventsByEntity(Guid entityId, long from = 0);
        Task<Result> AppendAsync(IEnumerable<IDomainEvent> domainEvents, long currenEntityVersion);
        Task<Result<IEnumerable<DomainEventWrapper>>> LoadEvents(long tickSince = 0);
        Task<Result<IEnumerable<DomainEventWrapper>>> LoadEventsByTypeAsync(string eventType, long version);
    }
}