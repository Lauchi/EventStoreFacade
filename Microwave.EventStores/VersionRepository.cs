using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microwave.Application;
using Microwave.Application.Ports;

namespace Microwave.EventStores
{
    public class VersionRepository : IVersionRepository
    {
        private readonly EventStoreContext _subscriptionContext;

        public VersionRepository(EventStoreContext subscriptionContext)
        {
            _subscriptionContext = subscriptionContext;
        }

        public async Task<long> GetVersionAsync(string domainEventType)
        {
            var lastProcessedVersion =
                await _subscriptionContext.ProcessedVersions.FirstOrDefaultAsync(version =>
                    version.EventType == domainEventType);
            if (lastProcessedVersion == null) return 0L;
            return lastProcessedVersion.LastVersion;
        }

        public async Task SaveVersion(LastProcessedVersion version)
        {
            var lastProcessedVersionDbo =
                _subscriptionContext.ProcessedVersions.FirstOrDefault(ev => ev.EventType == version.EventType);
            if (lastProcessedVersionDbo == null)
            {
                _subscriptionContext.ProcessedVersions.Add(new LastProcessedVersionDbo(version.EventType,
                    version.LastVersion));
            }
            else
            {
                var processedVersionDbo =
                    _subscriptionContext.ProcessedVersions.Single(e => e.EventType == version.EventType);
                processedVersionDbo.LastVersion = version.LastVersion;
                _subscriptionContext.Update(processedVersionDbo);
            }

            await _subscriptionContext.SaveChangesAsync();
        }
    }
}