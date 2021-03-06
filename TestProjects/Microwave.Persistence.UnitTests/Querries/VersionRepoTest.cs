using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microwave.Persistence.UnitTestsSetup;
using Microwave.Queries.Ports;

namespace Microwave.Persistence.UnitTests.Querries
{
    [TestClass]
    public class VersionRepoTest
    {
        [DataTestMethod]
        [PersistenceTypeTest]
        public async Task VersionRepo_DuplicateUpdate(PersistenceLayerProvider layerProvider)
        {
            var versionRepository = layerProvider.VersionRepository;

            var dateTimeOffset = 0;
            await versionRepository.SaveVersion(new LastProcessedVersion("Type", dateTimeOffset));
            await versionRepository.SaveVersion(new LastProcessedVersion("Type", dateTimeOffset));

            var count = await versionRepository.GetVersionAsync("Type");
            Assert.AreEqual(dateTimeOffset, count);
        }

        [DataTestMethod]
        [PersistenceTypeTest]
        public async Task LoadWithNull(PersistenceLayerProvider layerProvider)
        {
            var versionRepository = layerProvider.VersionRepository;
            var count = await versionRepository.GetVersionAsync(null);
            Assert.AreEqual(0, count);
        }
    }
}