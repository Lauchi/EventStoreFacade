using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microwave.Domain.EventSourcing;
using Microwave.Domain.Exceptions;
using Microwave.Domain.Results;
using Microwave.Persistence.MongoDb.Eventstores;
using Microwave.Persistence.UnitTestsSetup;

namespace Microwave.Persistence.UnitTests.Eventstores
{
    [TestClass]
    public class EventRepositoryTests
    {
        [DataTestMethod]
        [PersistenceTypeTest]
        public async Task AddAndLoadEvents(PersistenceLayerProvider layerProvider)
        {
            BsonMapRegistrationHelpers.AddBsonMapFor<TestEvent1>();
            BsonMapRegistrationHelpers.AddBsonMapFor<TestEvent2>();
            BsonMapRegistrationHelpers.AddBsonMapFor<TestEvent3>();

            var eventRepository = layerProvider.EventRepository;

            var newGuid = Guid.NewGuid();
            var events = new List<IDomainEvent> { new TestEvent1(newGuid), new TestEvent2(newGuid), new TestEvent3(newGuid, "TestName")};
            var res = await eventRepository.AppendAsync(events, 0);
            res.Check();

            var loadEventsByEntity = await eventRepository.LoadEventsByEntity(newGuid.ToString());
            Assert.AreEqual(3, loadEventsByEntity.Value.Count());
            Assert.AreEqual(1, loadEventsByEntity.Value.ToList()[0].EntityStreamVersion);
            var domainEvent = (TestEvent3) loadEventsByEntity.Value.ToList()[2].DomainEvent;
            Assert.AreEqual(newGuid.ToString(), domainEvent.EntityId);
            Assert.AreEqual("TestName", domainEvent.Name);
            Assert.AreEqual(2, loadEventsByEntity.Value.ToList()[1].EntityStreamVersion);
        }

        [DataTestMethod]
        [PersistenceTypeTest]
        public async Task AddAndLoadEvents_OrderIsCorrect(PersistenceLayerProvider layerProvider)
        {
            BsonMapRegistrationHelpers.AddBsonMapFor<TestEvent1>();
            BsonMapRegistrationHelpers.AddBsonMapFor<TestEvent2>();
            BsonMapRegistrationHelpers.AddBsonMapFor<TestEvent3>();

            var eventRepository = layerProvider.EventRepository;

            var newGuid = Guid.NewGuid();
            var events = new List<IDomainEvent> { new TestEvent1(newGuid) };
            var events2 = new List<IDomainEvent> { new TestEvent2(newGuid), new TestEvent3(newGuid, "TestName")};
            var res = await eventRepository.AppendAsync(events, 0);
            var res2 = await eventRepository.AppendAsync(events2, 1);
            res.Check();
            res2.Check();

            var loadEventsByEntity = await eventRepository.LoadEventsByEntity(newGuid.ToString());
            Assert.AreEqual(3, loadEventsByEntity.Value.Count());
            Assert.AreEqual(1, loadEventsByEntity.Value.ToList()[0].EntityStreamVersion);
            Assert.AreEqual(2, loadEventsByEntity.Value.ToList()[1].EntityStreamVersion);
            Assert.AreEqual(3, loadEventsByEntity.Value.ToList()[2].EntityStreamVersion);
        }

        [DataTestMethod]
        [PersistenceTypeTest]
        public async Task LoadWithNulls(PersistenceLayerProvider layerProvider)
        {
            var eventRepository = layerProvider.EventRepository;

            var loadEventsByEntity = await eventRepository.LoadEventsByEntity(null);
            Assert.IsTrue(loadEventsByEntity.Is<NotFound>());
            Assert.ThrowsException<NotFoundException>(() => loadEventsByEntity.Value);
        }

        [DataTestMethod]
        [PersistenceTypeTest]
        public async Task AddAndLoadEvents_Twice(PersistenceLayerProvider layerProvider)
        {
            var eventRepository = layerProvider.EventRepository;

            var newGuid = Guid.NewGuid();
            var events = new List<IDomainEvent> { new TestEvent1(newGuid), new TestEvent2(newGuid), new TestEvent3(newGuid, "TestName")};
            var res = await eventRepository.AppendAsync(events, 0);
            var res2 = await eventRepository.AppendAsync(events, 3);
            res.Check();
            res2.Check();

            var loadEventsByEntity = await eventRepository.LoadEventsByEntity(newGuid.ToString());
            Assert.AreEqual(6, loadEventsByEntity.Value.Count());
        }

        [DataTestMethod]
        [PersistenceTypeTest]
        public async Task LoadDomainEvents_IdAndStuffIsSetCorreclty(PersistenceLayerProvider layerProvider)
        {
            BsonMapRegistrationHelpers.AddBsonMapFor<TestEvent1>();
            BsonMapRegistrationHelpers.AddBsonMapFor<TestEvent2>();

            var eventRepository = layerProvider.EventRepository;

            var newGuid = Guid.NewGuid();
            var testEvent1 = new TestEvent1(newGuid);
            var testEvent2 = new TestEvent2(newGuid);
            var events = new List<IDomainEvent> { testEvent1, testEvent2};

            var res = await eventRepository.AppendAsync(events, 0);
            res.Check();

            var loadEventsByEntity = await eventRepository.LoadEventsByEntity(newGuid.ToString());
            Assert.AreEqual(2, loadEventsByEntity.Value.Count());
            Assert.AreEqual(1, loadEventsByEntity.Value.ToList()[0].OverallVersion);
            Assert.AreEqual(2, loadEventsByEntity.Value.ToList()[1].OverallVersion);
            Assert.AreEqual(1, loadEventsByEntity.Value.ToList()[0].EntityStreamVersion);
            Assert.AreEqual(2, loadEventsByEntity.Value.ToList()[1].EntityStreamVersion);
            Assert.IsTrue(newGuid.ToString() == loadEventsByEntity.Value.ToList()[0].DomainEvent.EntityId);
        }

        [DataTestMethod]
        [PersistenceTypeTest]
        public async Task AddAndLoadEventsConcurrent(PersistenceLayerProvider layerProvider)
        {
            var eventRepository = layerProvider.EventRepository;

            var newGuid = Guid.NewGuid();
            var events = new List<IDomainEvent> { new TestEvent1(newGuid), new TestEvent2(newGuid)};
            var events2 = new List<IDomainEvent> { new TestEvent1(newGuid), new TestEvent2(newGuid)};

            var t1 = eventRepository.AppendAsync(events, 0);
            var t2 = eventRepository.AppendAsync(events2, 0);

            var allResults = await Task.WhenAll(t1, t2);
            var concurrencyException = Assert.ThrowsException<ConcurrencyViolatedException>(() => CheckAllResults(allResults));
            var concurrencyExceptionMessage = concurrencyException.Message;
            Assert.AreEqual("Concurrency violation detected, could not update database. ExpectedVersion: 0, ActualVersion: 2", concurrencyExceptionMessage);

            var loadEvents = await eventRepository.LoadEvents();
            Assert.AreEqual(2, loadEvents.Value.Count());
        }

        [DataTestMethod]
        [PersistenceTypeTest]
        public async Task AddAndLoadEventsConcurrent_AfterNormalAdd(PersistenceLayerProvider layerProvider)
        {
            var eventRepository = layerProvider.EventRepository;

            var newGuid = Guid.NewGuid();
            var events = new List<IDomainEvent> { new TestEvent1(newGuid), new TestEvent2(newGuid)};
            var events2 = new List<IDomainEvent> { new TestEvent1(newGuid), new TestEvent2(newGuid)};

            await eventRepository.AppendAsync(events, 0);
            var t1 = eventRepository.AppendAsync(events, 2);
            var t2 = eventRepository.AppendAsync(events2, 2);

            var allResults = await Task.WhenAll(t1, t2);
            Assert.ThrowsException<ConcurrencyViolatedException>(() => CheckAllResults(allResults));

            var loadEvents = await eventRepository.LoadEvents();
            Assert.AreEqual(4, loadEvents.Value.Count());
        }

        [DataTestMethod]
        [PersistenceTypeTest]
        public async Task AddAndLoadEventsConcurrent_AddAfterwardsAgain(PersistenceLayerProvider layerProvider)
        {
            var eventRepository = layerProvider.EventRepository;

            var newGuid = Guid.NewGuid();
            var events = new List<IDomainEvent> { new TestEvent1(newGuid), new TestEvent2(newGuid)};
            var events2 = new List<IDomainEvent> { new TestEvent1(newGuid), new TestEvent2(newGuid)};

            var t1 = eventRepository.AppendAsync(events, 0);
            var t2 = eventRepository.AppendAsync(events2, 0);

            await Task.WhenAll(t1, t2);

            var res = await eventRepository.AppendAsync(events2, 2);

            Assert.IsTrue(res.Is<Ok>());
        }

        [DataTestMethod]
        [PersistenceTypeTest]
        public async Task AddAndLoadEventsConcurrent_MultipleEntitiies_GlobalWorks(PersistenceLayerProvider layerProvider)
        {
            var eventRepository = layerProvider.EventRepository;

            var newGuid = Guid.NewGuid();
            var newGuid2 = Guid.NewGuid();
            var events = new List<IDomainEvent> { new TestEvent1(newGuid), new TestEvent2(newGuid)};
            var events2 = new List<IDomainEvent> { new TestEvent1(newGuid2), new TestEvent2(newGuid2)};

            await eventRepository.AppendAsync(events, 0);
            await eventRepository.AppendAsync(events2, 0);

            var res = await eventRepository.LoadEvents();

            Assert.IsTrue(res.Is<Ok>());
            var domainEventWrappers = res.Value.ToList();
            Assert.AreEqual(1, domainEventWrappers[0].OverallVersion);
            Assert.AreEqual(2, domainEventWrappers[1].OverallVersion);
            Assert.AreEqual(3, domainEventWrappers[2].OverallVersion);
            Assert.AreEqual(4, domainEventWrappers[3].OverallVersion);
        }


        [DataTestMethod]
        [PersistenceTypeTest]
        public async Task AddAndLoadEventsConcurrent_MultipleEntitiies_OnRestartingService_GlobalWorks(PersistenceLayerProvider layerProvider)
        {
            var eventRepository = layerProvider.EventRepository;

            var newGuid = Guid.NewGuid();
            var newGuid2 = Guid.NewGuid();
            var events = new List<IDomainEvent> { new TestEvent1(newGuid), new TestEvent2(newGuid)};
            var events2 = new List<IDomainEvent> { new TestEvent1(newGuid2), new TestEvent2(newGuid2)};

            await eventRepository.AppendAsync(events, 0);

            var eventRepository2 = layerProvider.EventRepositoryNewInstance;
            if (eventRepository2 == null) return;

            await eventRepository2.AppendAsync(events2, 0);

            var res = await eventRepository.LoadEvents();

            Assert.IsTrue(res.Is<Ok>());
            var domainEventWrappers = res.Value.ToList();
            Assert.AreEqual(1, domainEventWrappers[0].OverallVersion);
            Assert.AreEqual(2, domainEventWrappers[1].OverallVersion);
            Assert.AreEqual(3, domainEventWrappers[2].OverallVersion);
            Assert.AreEqual(4, domainEventWrappers[3].OverallVersion);
        }

        [DataTestMethod]
        [PersistenceTypeTest]
        public async Task AddAndLoadEventsConcurrent_AddAfterwardsAgain_DifferentRepo(PersistenceLayerProvider layerProvider)
        {
            var eventRepository = layerProvider.EventRepository;
            var eventRepository2 = layerProvider.EventRepository;

            var newGuid = Guid.NewGuid();
            var events = new List<IDomainEvent> { new TestEvent1(newGuid), new TestEvent2(newGuid)};
            var events2 = new List<IDomainEvent> { new TestEvent1(newGuid), new TestEvent2(newGuid)};

            var t1 = eventRepository.AppendAsync(events, 0);
            var t2 = eventRepository2.AppendAsync(events2, 0);

            await Task.WhenAll(t1, t2);

            var res = await eventRepository.AppendAsync(events2, 2);

            Assert.IsTrue(res.Is<Ok>());
        }

        [DataTestMethod]
        [PersistenceTypeTest]
        public async Task LoadEntityId_NotFoundTIsCorrect(PersistenceLayerProvider layerProvider)
        {
            var eventRepository = layerProvider.EventRepository;

            var entityId = Guid.NewGuid();
            var result = await eventRepository.LoadEventsByEntity(entityId.ToString());

            var notFoundException = Assert.ThrowsException<NotFoundException>(() => result.Value);
            Assert.AreEqual($"Could not find DomainEvents with ID {entityId}", notFoundException.Message);
        }

        [DataTestMethod]
        [PersistenceTypeTest]
        public async Task LoadType_VersionTooHIgh_NotFoundIsOk(PersistenceLayerProvider layerProvider)
        {
            var eventRepository = layerProvider.EventRepository;

            var entityId = new Guid();
            var events = new List<IDomainEvent> { new TestEvent1(entityId), new TestEvent2(entityId)};
            await eventRepository.AppendAsync(events, 0);

            var result = await eventRepository.LoadEventsByTypeAsync(nameof(TestEvent1), 1000);

            Assert.IsTrue(result.Is<Ok>());
            Assert.AreEqual(0, result.Value.Count());
        }

        [DataTestMethod]
        [PersistenceTypeTest]
        public async Task LoadType_EmptyListWhenNoEventsPresentButAreBeingPublished(PersistenceLayerProvider layerProvider)
        {
            var eventRepository = layerProvider.EventRepository;

            var result = await eventRepository.LoadEventsByTypeAsync("TypeNotInsertedButSubscribed");

            Assert.AreEqual(0, result.Value.Count());
        }

        [DataTestMethod]
        [PersistenceTypeTest]
        public async Task AddEmptyEventList(PersistenceLayerProvider layerProvider)
        {
            var eventRepository = layerProvider.EventRepository;

            var appendAsync = await eventRepository.AppendAsync(new List<IDomainEvent>(), 0);
            appendAsync.Check();
        }

        [DataTestMethod]
        [PersistenceTypeTest]
        public async Task LoadEventsByTypeAsync(PersistenceLayerProvider layerProvider)
        {
            var eventRepository = layerProvider.EventRepository;

            var newGuid = Guid.NewGuid();
            var events = new List<IDomainEvent> { new TestEvent1(newGuid), new TestEvent2(newGuid), new TestEvent2(newGuid)};

            await eventRepository.AppendAsync(events, 0);
            var eventsLoaded = await eventRepository.LoadEventsByTypeAsync(typeof(TestEvent2).Name);

            Assert.AreEqual(2, eventsLoaded.Value.Count());
        }

        [DataTestMethod]
        [PersistenceTypeTest]
        public async Task AddAndLoadEventsByTimeStamp(PersistenceLayerProvider layerProvider)
        {
            BsonMapRegistrationHelpers.AddBsonMapFor<TestEvent1>();
            BsonMapRegistrationHelpers.AddBsonMapFor<TestEvent2>();

            var eventRepository = layerProvider.EventRepository;

            var newGuid = Guid.NewGuid();
            var events = new List<IDomainEvent> { new TestEvent1(newGuid), new TestEvent2(newGuid), new TestEvent1(newGuid), new TestEvent2(newGuid)};

            await eventRepository.AppendAsync(events, 0);

            var result = await eventRepository.LoadEvents();
            Assert.AreEqual(4, result.Value.Count());
            Assert.AreEqual(1, result.Value.ToList()[0].EntityStreamVersion);
            Assert.AreEqual(newGuid.ToString(), result.Value.ToList()[0].DomainEvent.EntityId);
        }

        [DataTestMethod]
        [PersistenceTypeTest]
        public async Task AddEvents_FirstEventAfterCreationHasWrongRowVersionBug(PersistenceLayerProvider layerProvider)
        {
            BsonMapRegistrationHelpers.AddBsonMapFor<TestEvent1>();
            BsonMapRegistrationHelpers.AddBsonMapFor<TestEvent2>();

            var eventRepository = layerProvider.EventRepository;

            var newGuid = Guid.NewGuid();
            var events = new List<IDomainEvent> { new TestEvent1(newGuid), new TestEvent2(newGuid), new TestEvent1(newGuid), new TestEvent2(newGuid)};

            await eventRepository.AppendAsync(events, 0);

            var result = await eventRepository.LoadEvents();
            Assert.AreEqual(4, result.Value.Count());
            Assert.AreEqual(1, result.Value.ToList()[0].EntityStreamVersion);
            Assert.AreEqual(2, result.Value.ToList()[1].EntityStreamVersion);
            Assert.AreEqual(3, result.Value.ToList()[2].EntityStreamVersion);
            Assert.AreEqual(newGuid.ToString(), result.Value.ToList()[0].DomainEvent.EntityId);
        }

        [DataTestMethod]
        [PersistenceTypeTest]
        public async Task AddEvents_VersionTooHigh(PersistenceLayerProvider layerProvider)
        {
            var eventRepository = layerProvider.EventRepository;

            var newGuid = Guid.NewGuid();
            var events = new List<IDomainEvent> { new TestEvent1(newGuid), new TestEvent2(newGuid), new TestEvent1(newGuid), new TestEvent2(newGuid)};

            var result = await eventRepository.AppendAsync(events, 1);

            Assert.IsTrue(result.Is<ConcurrencyError>());
        }

        [DataTestMethod]
        [PersistenceTypeTest]
        public async Task AddEvents_VersionWayTooHigh(PersistenceLayerProvider layerProvider)
        {
            var eventRepository = layerProvider.EventRepository;

            var newGuid = Guid.NewGuid();
            var events = new List<IDomainEvent> { new TestEvent1(newGuid), new TestEvent2(newGuid), new TestEvent1(newGuid), new TestEvent2(newGuid)};

            var result = await eventRepository.AppendAsync(events, 5);
            Assert.IsTrue(result.Is<ConcurrencyError>());
        }

        [DataTestMethod]
        [PersistenceTypeTest]
        public async Task AddAndLoadEventsByTimeStamp_SavedAsType(PersistenceLayerProvider layerProvider)
        {
            BsonMapRegistrationHelpers.AddBsonMapFor<TestEvent1>();

            var eventRepository = layerProvider.EventRepository;

            var newGuid = Guid.NewGuid();
            var domainEvent = new TestEvent1(newGuid);

            await eventRepository.AppendAsync(new List<IDomainEvent> { domainEvent }, 0);

            var result = await eventRepository.LoadEventsByTypeAsync(typeof(TestEvent1).Name);
            Assert.AreEqual(1, result.Value.Count());
            Assert.AreEqual(1, result.Value.ToList()[0].EntityStreamVersion);
            Assert.AreEqual(newGuid.ToString(), result.Value.ToList()[0].DomainEvent.EntityId);
            Assert.AreEqual(typeof(TestEvent1), result.Value.ToList()[0].DomainEvent.GetType());
        }

        [DataTestMethod]
        [PersistenceTypeTest]
        public async Task AddEvents_IdSet(PersistenceLayerProvider layerProvider)
        {
            BsonMapRegistrationHelpers.AddBsonMapFor<TestEvent1>();

            var eventRepository = layerProvider.EventRepository;

            var testEvent1 = new TestEvent1(Guid.NewGuid());
            await eventRepository.AppendAsync(new[] {testEvent1}, 0);

            var result = await eventRepository.LoadEventsByEntity(testEvent1.EntityId);
            var domainEvent = result.Value.Single().DomainEvent;

            Assert.IsTrue(domainEvent.EntityId.Equals(testEvent1.EntityId));
        }

        [DataTestMethod]
        [PersistenceTypeTest]
        public async Task AddEvents_IdOfTypeSet(PersistenceLayerProvider layerProvider)
        {
            BsonMapRegistrationHelpers.AddBsonMapFor<TestEvent1>();

            var eventRepository = layerProvider.EventRepository;

            var testEvent1 = new TestEvent1(Guid.NewGuid());
            await eventRepository.AppendAsync(new List<IDomainEvent> { testEvent1 }, 0);

            var result = await eventRepository.LoadEventsByTypeAsync(testEvent1.GetType().Name);
            var domainEvent = result.Value.Single().DomainEvent;

            Assert.IsTrue(domainEvent.EntityId == testEvent1.EntityId);
        }

        [DataTestMethod]
        [PersistenceTypeTest]
        public async Task AddEvents_RunTypeProjection(PersistenceLayerProvider layerProvider)
        {
            BsonMapRegistrationHelpers.AddBsonMapFor<TestEvent1>();
            BsonMapRegistrationHelpers.AddBsonMapFor<TestEvent2>();

            var eventRepository = layerProvider.EventRepository;

            var newGuid = Guid.NewGuid();
            var events = new List<IDomainEvent> { new TestEvent1(newGuid), new TestEvent2(newGuid), new TestEvent1(newGuid), new TestEvent2(newGuid)};

            await eventRepository.AppendAsync(events, 0);

            var result = await eventRepository.LoadEventsByTypeAsync(typeof(TestEvent1).Name);

            Assert.AreEqual(2, result.Value.Count());
            Assert.AreEqual(1, result.Value.ToList()[0].EntityStreamVersion);
            Assert.AreEqual(3, result.Value.ToList()[1].EntityStreamVersion);
            Assert.AreEqual(newGuid.ToString(), result.Value.ToList()[0].DomainEvent.EntityId);
            Assert.AreEqual(typeof(TestEvent1), result.Value.ToList()[0].DomainEvent.GetType());
        }

        [DataTestMethod]
        [PersistenceTypeTest]
        public async Task AddMultipleEvents_GlobalVersionIsUpdatedCorrectly(PersistenceLayerProvider layerProvider)
        {
            var eventRepository = layerProvider.EventRepository;

            var newGuid = Guid.NewGuid();
            var domainEvents = new List<IDomainEvent>();
            var domainEvents2 = new List<IDomainEvent>();
            for (int i = 0; i < 100; i++)
            {
                domainEvents.Add(new TestEvent1(newGuid));
                domainEvents2.Add(new TestEvent2(newGuid));
            }


            await eventRepository.AppendAsync(domainEvents, 0);
            await eventRepository.AppendAsync(domainEvents, 100);

            var result = await eventRepository.LoadEvents();
            var dateTimeOffset = result.Value.Skip(78).First().OverallVersion;

            Assert.AreEqual(200, result.Value.Count());

            for (int i = 0; i < 10; i++)
            {
                var result2 = await eventRepository.LoadEvents(dateTimeOffset);
                Assert.AreEqual(121, result2.Value.Count());
            }
        }

        private static void CheckAllResults(Result[] whenAll)
        {
            foreach (var result in whenAll)
            {
                result.Check();
            }
        }
    }

    public class TestEvent1 : IDomainEvent
    {
        public Guid Id { get; }
        public TestEvent1(Guid id)
        {
            Id = id;
        }

        public string EntityId => Id.ToString();
    }

    public class TestEvent2 : IDomainEvent
    {
        public TestEvent2(Guid id)
        {
            Id = id;
        }

        public string EntityId => Id.ToString();

        public Guid Id { get; }
    }

    public class TestEvent3 : IDomainEvent
    {
        public TestEvent3(Guid id, string name)
        {
            Id = id;
            Name = name;
        }

        public string EntityId => Id.ToString();
        public string Name { get; }

        public Guid Id { get; }
    }
}