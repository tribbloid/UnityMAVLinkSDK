using System.Collections.Generic;
using System.Linq;
using MAVLinkAPI.API;
using MAVLinkAPI.Routing;
using NUnit.Framework;

namespace MAVLinkAPI.Tests.API
{
    public class ReaderSpec
    {
        [Test]
        public void RawReadSource_EmitsCorrectMessage()
        {
            // Arrange
            var message = Mock.MockHeartbeat();
            var uplink = new Uplink.Dummy(message);

            // Act
            var result = uplink.RawReadSource.ToList();

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(message.msgid, result[0].msgid);
        }


        [Test]
        public void DirectOutput()
        {
            // Arrange
            var message = Mock.MockHeartbeat();
            var uplink = new Uplink.Dummy(message);
            var pipe = MsgPipe.On<MAVLink.mavlink_heartbeat_t>();
            var reader = uplink.Read(pipe);

            var result = reader.Drain();

            // Assert
            Assert.AreEqual(1, result.Count);
            // Assert.AreEqual("Transformed", result[0]);
        }

        [Test]
        public void Select_TransformsOutput()
        {
            // Arrange
            var message = Mock.MockHeartbeat();
            var uplink = new Uplink.Dummy(message);
            var pipe = MsgPipe.On<MAVLink.mavlink_heartbeat_t>().Select((m, p) => 1);
            var reader = uplink.Read(pipe);

            // Act
            var stringReader = reader.Select((_, i) => "Transformed");
            var result = stringReader.Drain();

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Transformed", result[0]);
        }

        [Test]
        public void SelectMany_TransformsAndFlattensOutput()
        {
            // Arrange
            var message = Mock.MockHeartbeat();
            var uplink = new Uplink.Dummy(message);
            var pipe = MsgPipe.On<MAVLink.mavlink_heartbeat_t>().Select((m, p) => 1);
            var reader = uplink.Read(pipe);

            // Act
            var listReader = reader.SelectMany((_, i) => new List<string> { "A", "B" });
            var result = listReader.Drain();

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("A"));
            Assert.IsTrue(result.Contains("B"));
        }

        [Test]
        public void Cat_WithDifferentUplinks_CombinesSources()
        {
            // Arrange
            var uplink1 = new Uplink.Dummy(Mock.MockHeartbeat());
            var uplink2 = new Uplink.Dummy(Mock.MockHeartbeat());
            var func = MsgPipe.On<MAVLink.mavlink_heartbeat_t>().Select((m, p) => 1);
            var reader1 = uplink1.Read(func);
            var reader2 = uplink2.Read(func);

            // Act
            var catReader = reader1.Cat(reader2);
            var result = catReader.Drain();

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.All(v => v == 1));
        }

        [Test]
        public void Cat_WithSameUplink_CombinesFunctions()
        {
            // Arrange
            var uplink = new Uplink.Dummy(Mock.MockHeartbeat(), Mock.MockSystemTime());
            var func1 = MsgPipe.On<MAVLink.mavlink_heartbeat_t>().Select((m, p) => 1);
            var func2 = MsgPipe.On<MAVLink.mavlink_system_time_t>().Select((m, p) => 2);
            var catReader = uplink.Read(func1.Cat(func2));
            var result = catReader.Drain();

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(1));
            Assert.IsTrue(result.Contains(2));
        }


        [Test]
        public void Cat_WithSameMessage_CombinesFunctions()
        {
            // Arrange
            var uplink = new Uplink.Dummy(Mock.MockHeartbeat());
            var func1 = MsgPipe.On<MAVLink.mavlink_heartbeat_t>().Select((m, p) => 1);
            var func2 = MsgPipe.On<MAVLink.mavlink_heartbeat_t>().Select((m, p) => 2);
            var catReader = uplink.Read(func1.Cat(func2));
            var result = catReader.Drain();

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(1));
            Assert.IsTrue(result.Contains(2));
        }

        [Test]
        public void Union_WithDifferentUplinks_RemovesDuplicates()
        {
            // Arrange
            var uplink1 = new Uplink.Dummy(Mock.MockHeartbeat());
            var uplink2 = new Uplink.Dummy(Mock.MockHeartbeat());
            var func = MsgPipe.On<MAVLink.mavlink_heartbeat_t>().Select((m, p) => 1);
            var reader1 = uplink1.Read(func);
            var reader2 = uplink2.Read(func);

            // Act
            var unionReader = reader1.Union(reader2);
            var result = unionReader.Drain();

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.All(v => v == 1));
        }

        [Test]
        public void Union_WithSameUplink_CombinesFunctions()
        {
            // Arrange
            var uplink = new Uplink.Dummy(Mock.MockHeartbeat(), Mock.MockSystemTime());
            var func1 = MsgPipe.On<MAVLink.mavlink_heartbeat_t>().Select((m, p) => 1);
            var func2 = MsgPipe.On<MAVLink.mavlink_system_time_t>().Select((m, p) => 2);
            var unionReader = uplink.Read(func1.Union(func2));
            var result = unionReader.Drain();

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(1));
            Assert.IsTrue(result.Contains(2));
        }

        [Test]
        public void Union_WithSameMessage_CombinesFunctions()
        {
            // Arrange
            var uplink = new Uplink.Dummy(Mock.MockHeartbeat());
            var func1 = MsgPipe.On<MAVLink.mavlink_heartbeat_t>().Select((m, p) => 1);
            var func2 = MsgPipe.On<MAVLink.mavlink_heartbeat_t>().Select((m, p) => 2);
            var unionReader = uplink.Read(func1.Union(func2));
            var result = unionReader.Drain();

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(1));
            Assert.IsTrue(result.Contains(2));
        }

        [Test]
        public void OrElse_WithDifferentUplinks_CombinesSources()
        {
            // Arrange
            var uplink1 = new Uplink.Dummy(Mock.MockHeartbeat());
            var uplink2 = new Uplink.Dummy(Mock.MockHeartbeat());
            var func = MsgPipe.On<MAVLink.mavlink_heartbeat_t>().Select((m, p) => 1);
            var reader1 = uplink1.Read(func);
            var reader2 = uplink2.Read(func);

            // Act
            var orElseReader = reader1.OrElse(reader2);
            var result = orElseReader.Drain();

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.All(v => v == 1));
        }


        [Test]
        public void OrElse_WithSameUplink_CombinesFunctions()
        {
            // Arrange
            var uplink = new Uplink.Dummy(Mock.MockHeartbeat(), Mock.MockSystemTime());
            var func1 = MsgPipe.On<MAVLink.mavlink_heartbeat_t>().Select((m, p) => 1);
            var func2 = MsgPipe.On<MAVLink.mavlink_system_time_t>().Select((m, p) => 2);
            var orElseReader = uplink.Read(func1.OrElse(func2));
            var result = orElseReader.Drain();

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(1));
            Assert.IsTrue(result.Contains(2));
        }
    }
}