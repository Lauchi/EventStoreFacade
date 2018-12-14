using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microwave.Domain;
using Microwave.EventStores;

namespace Microwave.Eventstores.UnitTests
{
    [TestClass]
    public class SnapshotConfigTests
    {
        [TestMethod]
        [DataRow(0, 3, true)]
        [DataRow(0, 4, true)]
        [DataRow(1, 3, true)]
        [DataRow(2, 4, true)]
        [DataRow(3, 4, false)]
        [DataRow(5, 5, false)]
        [DataRow(6, 6, false)]
        public void SnapshotConfig(long last, long current, bool expected)
        {
            var snapShotAfter = new SnapShotAfter(3);
            var doesNeedSnapshot = snapShotAfter.DoesNeedSnapshot(last, current);
            Assert.AreEqual(expected, doesNeedSnapshot);
        }
    }
}