#nullable enable
using System.Collections.Generic;
using MAVLinkSDK.API;
using MAVLinkSDK.API.Pipes;
using MAVLinkSDK.Routing;
using NUnit.Framework;

namespace MAVLinkSDK.Tests.API.Pipes
{
    [TestFixture]
    public class RawTSpec
    {
        [Test]
        public void Process_EmitsInputMessage()
        {
            var raw = new RawT();
            var message = Mock.MockHeartbeat();

            var result = raw.Process(message);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].msgid, Is.EqualTo(message.msgid));
        }

        [Test]
        public void ReadRaw_UsesRawTPipe()
        {
            var message = Mock.MockHeartbeat();
            var uplink = new Uplink.Dummy(new List<MAVLink.MAVLinkMessage> { message });

            var reader = uplink.ReadRaw();
            var result = reader.Drain();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].msgid, Is.EqualTo(message.msgid));
        }
    }
}