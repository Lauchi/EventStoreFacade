using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microwave.Domain;

namespace Microwave.Queries
{
    public class AsyncEventHandler<T> : IAsyncEventHandler where T : IDomainEvent
    {
        private readonly IEventFeed<AsyncEventHandler<T>>  _eventFeed;
        private readonly IEnumerable<IHandleAsync<T>> _handles;
        private readonly IVersionRepository _versionRepository;

        public AsyncEventHandler(
            IVersionRepository versionRepository,
            IEventFeed<AsyncEventHandler<T>> eventFeed,
            IEnumerable<IHandleAsync<T>> handles)
        {
            _versionRepository = versionRepository;
            _eventFeed = eventFeed;
            _handles = handles;
        }

        public async Task Update()
        {
            foreach (var handle in _handles)
            {
                var handleType = handle.GetType();
                var domainEventType = $"{handleType.Name}-{typeof(T).Name}";
                var lastVersion = await _versionRepository.GetVersionAsync(domainEventType);
                var latestEvents = await _eventFeed.GetEventsAsync(lastVersion);
                foreach (var latestEvent in latestEvents)
                {
                    var methodInfos = handleType.GetMethods();
                    var methodInfo = methodInfos.FirstOrDefault(m => m.GetParameters().Length == 1
                                                                     && m.Name == "HandleAsync"
                                                                     && m.GetParameters().First().ParameterType == latestEvent.DomainEvent.GetType());
                    if (methodInfo == null) continue;
                    await (Task) methodInfo.Invoke(handle, new object[] { latestEvent.DomainEvent });
                    await _versionRepository.SaveVersion(new LastProcessedVersion(domainEventType, latestEvent.Created));
                }
            }
        }
    }
}