#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using MAVLinkAPI.API;
using MAVLinkAPI.API.Pipes;
using NUnit.Framework;

namespace MAVLinkAPI.Tests.API.Pipes
{
    [TestFixture]
    public class DelayTSpec
    {
        private static ulong NowSigTimestamp()
        {
            return (ulong)((DateTime.UtcNow - new DateTime(2015, 1, 1)).TotalMilliseconds * 100);
        }

        private static ulong LatencyToSigTimestamp(TimeSpan latency)
        {
            if (latency <= TimeSpan.Zero) return 0;

            var v = latency.TotalMilliseconds * 100.0;
            if (v <= 0) return 0;

            return (ulong)Math.Ceiling(v);
        }

        private static RxMessage<MAVLink.mavlink_heartbeat_t> MkSignedHeartbeat(ulong sigTimestamp)
        {
            var heartbeat = new MAVLink.mavlink_heartbeat_t
            {
                type = (byte)MAVLink.MAV_TYPE.QUADROTOR,
                autopilot = (byte)MAVLink.MAV_AUTOPILOT.GENERIC,
                base_mode = (byte)MAVLink.MAV_MODE_FLAG.MANUAL_INPUT_ENABLED,
                custom_mode = 0,
                system_status = (byte)MAVLink.MAV_STATE.STANDBY,
                mavlink_version = 3
            };

            var parser = new MAVLink.MavlinkParse();
            var packetBytes = parser.GenerateMAVLinkPacket20(MAVLink.MAVLINK_MSG_ID.HEARTBEAT, heartbeat, true);

            var sigOffset = packetBytes.Length - MAVLink.MAVLINK_SIGNATURE_BLOCK_LEN;
            packetBytes[sigOffset] = 0;

            var tsBytes = BitConverter.GetBytes(sigTimestamp);
            Array.Copy(tsBytes, 0, packetBytes, sigOffset + 1, 6);

            var raw = new MAVLink.MAVLinkMessage(packetBytes);
            return new RxMessage<MAVLink.mavlink_heartbeat_t>(raw);
        }

        private sealed class OneShotPipe : Pipe<int, RxMessage<MAVLink.mavlink_heartbeat_t>>
        {
            private readonly RxMessage<MAVLink.mavlink_heartbeat_t> _msg;
            private bool _sent;

            public OneShotPipe(RxMessage<MAVLink.mavlink_heartbeat_t> msg)
            {
                _msg = msg;
            }

            protected override List<RxMessage<MAVLink.mavlink_heartbeat_t>>? PrimaryFn(int input)
            {
                if (_sent) return null;
                _sent = true;
                return new List<RxMessage<MAVLink.mavlink_heartbeat_t>> { _msg };
            }
        }

        private sealed class BatchPipe : Pipe<int, RxMessage<MAVLink.mavlink_heartbeat_t>>
        {
            private readonly List<RxMessage<MAVLink.mavlink_heartbeat_t>> _msgs;
            private bool _sent;

            public BatchPipe(List<RxMessage<MAVLink.mavlink_heartbeat_t>> msgs)
            {
                _msgs = msgs;
            }

            protected override List<RxMessage<MAVLink.mavlink_heartbeat_t>>? PrimaryFn(int input)
            {
                if (_sent) return null;
                _sent = true;
                return _msgs;
            }
        }

        private static List<ulong> Timestamps(IEnumerable<RxMessage<MAVLink.mavlink_heartbeat_t>> messages)
        {
            var result = new List<ulong>();
            foreach (var m in messages) result.Add(m.Signature.Timestamp);
            return result;
        }

        [Test]
        public void ProcessOrNull_ReturnsNullBeforeLatency()
        {
            var latency = TimeSpan.FromMilliseconds(100);
            var msgTimestamp = NowSigTimestamp();

            var delay = new DelayT<int, MAVLink.mavlink_heartbeat_t>(
                new OneShotPipe(MkSignedHeartbeat(msgTimestamp)),
                latency);

            var first = delay.ProcessOrNull(0);

            Assert.That(first, Is.Null);
        }

        [Test]
        public void ProcessOrNull_EmitsAfterLatency()
        {
            var latency = TimeSpan.FromMilliseconds(100);
            var msgTimestamp = NowSigTimestamp();

            var delay = new DelayT<int, MAVLink.mavlink_heartbeat_t>(
                new OneShotPipe(MkSignedHeartbeat(msgTimestamp)),
                latency);

            var first = delay.ProcessOrNull(0);
            Thread.Sleep(250);
            var second = delay.ProcessOrNull(0);
            var third = delay.ProcessOrNull(0);

            Assert.That(first, Is.Null);
            Assert.That(second, Is.Not.Null);
            Assert.That(second!.Count, Is.EqualTo(1));
            Assert.That(third, Is.Null);
        }

        [Test]
        public void ProcessOrNull_MultipleInOrder_EmitsOnlyReadyThenAllAfterWait()
        {
            var latency = TimeSpan.FromMilliseconds(200);
            var now = NowSigTimestamp();
            var latencyTs = LatencyToSigTimestamp(latency);

            var t1 = now - (latencyTs + 50000);
            var t2 = now - (latencyTs + 40000);
            var t3 = now - (latencyTs / 2);
            var t4 = now - (latencyTs / 3);
            var t5 = now;

            var delay = new DelayT<int, MAVLink.mavlink_heartbeat_t>(
                new BatchPipe(new List<RxMessage<MAVLink.mavlink_heartbeat_t>>
                {
                    MkSignedHeartbeat(t1),
                    MkSignedHeartbeat(t2),
                    MkSignedHeartbeat(t3),
                    MkSignedHeartbeat(t4),
                    MkSignedHeartbeat(t5)
                }),
                latency);

            var first = delay.ProcessOrNull(0);
            var second = delay.ProcessOrNull(0);
            var third = delay.ProcessOrNull(0);

            Assert.That(first, Is.Not.Null);
            Assert.That(first!.Count, Is.EqualTo(1));
            Assert.That(Timestamps(first), Is.EquivalentTo(new[] { t1 }));

            Assert.That(second, Is.Not.Null);
            Assert.That(second!.Count, Is.EqualTo(1));
            Assert.That(Timestamps(second), Is.EquivalentTo(new[] { t2 }));

            Assert.That(third, Is.Null);

            Thread.Sleep(350);
            var fourth = delay.ProcessOrNull(0);
            var fifth = delay.ProcessOrNull(0);
            var sixth = delay.ProcessOrNull(0);
            var seventh = delay.ProcessOrNull(0);

            Assert.That(fourth, Is.Not.Null);
            Assert.That(fourth!.Count, Is.EqualTo(1));
            Assert.That(Timestamps(fourth), Is.EquivalentTo(new[] { t3 }));

            Assert.That(fifth, Is.Not.Null);
            Assert.That(fifth!.Count, Is.EqualTo(1));
            Assert.That(Timestamps(fifth), Is.EquivalentTo(new[] { t4 }));

            Assert.That(sixth, Is.Not.Null);
            Assert.That(sixth!.Count, Is.EqualTo(1));
            Assert.That(Timestamps(sixth), Is.EquivalentTo(new[] { t5 }));

            Assert.That(seventh, Is.Null);
        }

        [Test]
        public void ProcessOrNull_MultipleOutOfOrder_EmitsReadySubsetThenAllAfterWait()
        {
            var latency = TimeSpan.FromMilliseconds(200);
            var now = NowSigTimestamp();
            var latencyTs = LatencyToSigTimestamp(latency);

            var readyA = now - (latencyTs + 50000);
            var readyB = now - (latencyTs + 30000);
            var notReadyA = now;
            var notReadyB = now - (latencyTs / 2);

            var delay = new DelayT<int, MAVLink.mavlink_heartbeat_t>(
                new BatchPipe(new List<RxMessage<MAVLink.mavlink_heartbeat_t>>
                {
                    MkSignedHeartbeat(notReadyA),
                    MkSignedHeartbeat(readyA),
                    MkSignedHeartbeat(notReadyB),
                    MkSignedHeartbeat(readyB)
                }),
                latency);

            var first = delay.ProcessOrNull(0);
            var second = delay.ProcessOrNull(0);
            var third = delay.ProcessOrNull(0);

            Assert.That(first, Is.Not.Null);
            Assert.That(first!.Count, Is.EqualTo(1));
            Assert.That(Timestamps(first), Is.EquivalentTo(new[] { readyA }));

            Assert.That(second, Is.Not.Null);
            Assert.That(second!.Count, Is.EqualTo(1));
            Assert.That(Timestamps(second), Is.EquivalentTo(new[] { readyB }));

            Assert.That(third, Is.Null);

            Thread.Sleep(350);
            var fourth = delay.ProcessOrNull(0);
            var fifth = delay.ProcessOrNull(0);
            var sixth = delay.ProcessOrNull(0);

            Assert.That(fourth, Is.Not.Null);
            Assert.That(fourth!.Count, Is.EqualTo(1));
            Assert.That(Timestamps(fourth), Is.EquivalentTo(new[] { notReadyA }));

            Assert.That(fifth, Is.Not.Null);
            Assert.That(fifth!.Count, Is.EqualTo(1));
            Assert.That(Timestamps(fifth), Is.EquivalentTo(new[] { notReadyB }));

            Assert.That(sixth, Is.Null);
        }
    }
}
