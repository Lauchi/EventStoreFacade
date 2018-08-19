﻿using System;
using System.Collections.Generic;
using Domain.Framework;

namespace Domain.Seasons
{
    public class Season : Entity
    {
        public string SeasonName { get; private set; }
        public int TimeForGame { get; private set; }
        public DateTimeOffset StartDate { get; private set; }
        public DateTimeOffset EndDate { get; private set; }

        public static DomainResult Create(string seasonName, int maxDaysBetweenGames)
        {
            var seasonCreatedEvent = new SeasonCreatedEvent(Guid.NewGuid(), seasonName, maxDaysBetweenGames);
            return DomainResult.OkResult(seasonCreatedEvent);
        }

        public void Apply(SeasonCreatedEvent domainEvent)
        {
            SeasonName = domainEvent.InitialName;
            TimeForGame = domainEvent.MaxDaysBetweenGames;
            Id = domainEvent.EntityId;
        }

        public void Apply(SeasonDateChangedEvent domainEvent)
        {
            StartDate = domainEvent.StartDate;
            EndDate = domainEvent.EndDate;
        }

        public DomainResult ChangeDate(DateTimeOffset startDate, DateTimeOffset endDate)
        {
            return DomainResult.OkResult(new SeasonDateChangedEvent(Id, startDate, endDate));
        }
    }

    public class SeasonDateChangedEvent : DomainEvent
    {
        public DateTimeOffset StartDate { get; }
        public DateTimeOffset EndDate { get; }

        public SeasonDateChangedEvent(Guid id, DateTimeOffset startDate, DateTimeOffset endDate) : base(id)
        {
            StartDate = startDate;
            EndDate = endDate;
        }
    }

    public class SeasonCreatedEvent : DomainEvent
    {
        public string InitialName { get; }
        public int MaxDaysBetweenGames { get; }

        public SeasonCreatedEvent(Guid entityId, string initialName, int daysBetweenGames) : base(entityId)
        {
            InitialName = initialName;
            MaxDaysBetweenGames = daysBetweenGames;
        }
    }
}