﻿using Microsoft.EntityFrameworkCore;

namespace Microwave.EventStores
{
    public sealed class EventStoreContext : DbContext
    {
        public DbSet<DomainEventDbo> EntityStreams { get; set; }

        public EventStoreContext(DbContextOptions<EventStoreContext> options) :
            base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DomainEventDbo>()
                .HasKey(p => new {p.EntityId , p.Version});
        }
    }

    public class DomainEventDbo
    {
        public string EntityId { get; set; }
        public string Payload { get; set; }
        public long Created { get; set; }
        public long Version { get; set; }
        public string EventType { get; set; }
    }
}