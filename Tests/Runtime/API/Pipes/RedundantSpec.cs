using System;
using MAVLinkSDK.API;
using MAVLinkSDK.API.Pipes;
using NUnit.Framework;

namespace MAVLinkSDK.Tests.API.Pipes
{
    [TestFixture]
    public class RedundantSpec
    {
        private class ReadHeartbeat : ByTopics<RxMessage<MAVLink.mavlink_heartbeat_t>>
        {
            protected override IDIndexed<CaseFn> MkTopics()
            {
                return new IDIndexed<CaseFn>();
            }

            protected override CaseFn OtherCase => _ => null;
        }

        private static Redundant<MAVLink.MAVLinkMessage, MAVLink.mavlink_heartbeat_t> CreateHaLeftOnly()
        {
            var left = MsgPipe.On<MAVLink.mavlink_heartbeat_t>();
            var right = new ReadHeartbeat();
            return new Redundant<MAVLink.MAVLinkMessage, MAVLink.mavlink_heartbeat_t>
            {
                Left = left,
                Right = right
            };
        }

        private static Redundant<MAVLink.MAVLinkMessage, MAVLink.mavlink_heartbeat_t> CreateHaBothChannels()
        {
            var left = MsgPipe.On<MAVLink.mavlink_heartbeat_t>();
            var right = MsgPipe.On<MAVLink.mavlink_heartbeat_t>();
            return new Redundant<MAVLink.MAVLinkMessage, MAVLink.mavlink_heartbeat_t>
            {
                Left = left,
                Right = right
            };
        }

        [Test]
        public void FirstMessage_FromSingleChannel_IsForwarded_WithNoWarningsOrErrors()
        {
            var ha = CreateHaLeftOnly();
            var message = Mock.MockHeartbeat();

            var result = ha.Process(message);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Count, Is.EqualTo(1));
            Assert.That(ha.WarningCount, Is.EqualTo(0));
            Assert.That(ha.ErrorCount, Is.EqualTo(0));
        }

        [Test]
        public void SecondMessage_FromSameChannel_IncrementsErrorCount_AndIsNotForwarded()
        {
            var ha = CreateHaLeftOnly();
            var baseTime = DateTime.UtcNow;

            var message1 = Mock.MockHeartbeat();
            message1.rxtime = baseTime;

            var message2 = Mock.MockHeartbeat();
            message2.rxtime = baseTime;

            var first = ha.Process(message1);
            var second = ha.Process(message2);

            Assert.That(first, Is.Not.Null);
            Assert.That(first!.Count, Is.EqualTo(1));
            Assert.That(second, Is.Not.Null);
            Assert.That(second!.Count, Is.EqualTo(0));
            Assert.That(ha.ErrorCount, Is.EqualTo(1));
            Assert.That(ha.WarningCount, Is.EqualTo(0));
        }

        [Test]
        public void Message_SeenOnBothChannels_IsVerifiedWithoutErrors()
        {
            var ha = CreateHaBothChannels();
            var message = Mock.MockHeartbeat();

            var result = ha.Process(message);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Count, Is.EqualTo(1));
            Assert.That(ha.ErrorCount, Is.EqualTo(0));
            Assert.That(ha.WarningCount, Is.EqualTo(0));
        }

        [Test]
        public void UnverifiedMessage_BecomesStaleAfterStaleThreshold()
        {
            var ha = CreateHaLeftOnly();
            var baseTime = DateTime.UtcNow;

            var message1 = Mock.MockHeartbeat();
            message1.rxtime = baseTime;

            var deltaTicks = (ha.LostThreshold - ha.StaleThreshold).Ticks / 2;
            var message2 = Mock.MockHeartbeat();
            message2.rxtime = baseTime + ha.StaleThreshold + TimeSpan.FromTicks(deltaTicks);

            var first = ha.Process(message1);
            var second = ha.Process(message2);

            Assert.That(first, Is.Not.Null);
            Assert.That(first!.Count, Is.EqualTo(1));
            Assert.That(second, Is.Not.Null);
            Assert.That(second!.Count, Is.EqualTo(1));
            Assert.That(ha.WarningCount, Is.EqualTo(1));
            Assert.That(ha.ErrorCount, Is.EqualTo(0));
        }

        [Test]
        public void UnverifiedMessage_BecomesLostAfterLostThreshold()
        {
            var ha = CreateHaLeftOnly();
            var baseTime = DateTime.UtcNow;

            var message1 = Mock.MockHeartbeat();
            message1.rxtime = baseTime;

            var message2 = Mock.MockHeartbeat();
            message2.rxtime = baseTime + ha.LostThreshold + TimeSpan.FromSeconds(1);

            var first = ha.Process(message1);
            var second = ha.Process(message2);

            Assert.That(first, Is.Not.Null);
            Assert.That(first!.Count, Is.EqualTo(1));
            Assert.That(second, Is.Not.Null);
            Assert.That(second!.Count, Is.EqualTo(1));
            Assert.That(ha.ErrorCount, Is.EqualTo(1));
            Assert.That(ha.WarningCount, Is.EqualTo(0));
        }
    }
}