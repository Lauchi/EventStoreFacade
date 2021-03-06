using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microwave.Queries.Exceptions;
using Microwave.Queries.Polling;

namespace Microwave.Queries.UnitTests
{
    [TestClass]
    public class PollingIntervalTests
    {
        [TestMethod]
        public void Constructor()
        {
            var attribute = new PollingInterval<FakeThing>("* * * * *");
            var attributeNext = attribute.Next;

            Assert.AreEqual(attributeNext.Minute, DateTime.UtcNow.AddMinutes(1).Minute);
        }

        [TestMethod]
        public void ConstructorSeconds()
        {
            var attribute = new PollingInterval<FakeThing>(20);
            var attributeNext = attribute.Next;

            Assert.IsTrue(attributeNext.Second % 20 == 0);
        }

        [TestMethod]
        public void ConstructorSeconds_1()
        {
            var attribute = new PollingInterval<FakeThing>(1);
            var attributeNext = attribute.Next;

            Assert.IsTrue(attributeNext.Second % 1 == 0);
        }

        [TestMethod]
        public void ConstructorSeconds_0()
        {
            Assert.ThrowsException<InvalidTimeNotationException>(() => new PollingInterval<FakeThing>(0));
        }

        [TestMethod]
        [DataRow(1, 0, 1, 0)]
        [DataRow(10, 12, 20, 0)]
        [DataRow(15, 12, 15, 0)]
        [DataRow(15, 17, 30, 0)]
        [DataRow(15, 58, 0, 1)]
        [DataRow(5, 3, 5, 0)]
        [DataRow(60, 0, 0, 1)]
        [DataRow(25, 50, 0, 1)]
        public void AttributeCombinationTests(int secondsInput, int secondsForTest, int secondsOutput, int minuteOffset)
        {
            var updateEveryAttribute = CreatPolInterval(secondsInput, secondsForTest);
            var dateTime = updateEveryAttribute.Next;
            Assert.AreEqual(secondsOutput, dateTime.Second);
            Assert.AreEqual(minuteOffset, dateTime.Minute);
        }

        private PollingInterval<FakeThing> CreatPolInterval(in int secondsInput, in int secondsForTest)
        {
            var pollingInterval = new PollingInterval<FakeThing>();
            var type = typeof(PollingInterval<FakeThing>);
            var nowTime = new DateTime(1, 1, 1, 1, 0, secondsForTest);
            var second = secondsInput;

            var nowtTimeField = type.GetField("_nowTime", BindingFlags.Instance | BindingFlags.NonPublic);
            var secondField = type.GetField("_second", BindingFlags.Instance | BindingFlags.NonPublic);

            nowtTimeField.SetValue(pollingInterval, nowTime);
            secondField.SetValue(pollingInterval, second);

            return pollingInterval;
        }
    }

    public class FakeThing
    {
    }
}