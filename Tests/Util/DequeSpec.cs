using MAVLinkSDK.API;
using MAVLinkSDK.Util;
using NUnit.Framework;

namespace MAVLinkSDK.Tests.Util
{
    [TestFixture]
    public class DequeSpec
    {
        [Test]
        public void EnqueueFront_PrependsItems()
        {
            var q = new Deque<int>();

            q.Enqueue(1);
            q.Enqueue(2);
            q.EnqueueFront(0);
            q.EnqueueFront(-1);

            Assert.That(q.Count, Is.EqualTo(4));
            Assert.That(q.Dequeue(), Is.EqualTo(-1));
            Assert.That(q.Dequeue(), Is.EqualTo(0));
            Assert.That(q.Dequeue(), Is.EqualTo(1));
            Assert.That(q.Dequeue(), Is.EqualTo(2));
            Assert.That(q.Count, Is.EqualTo(0));
        }

        [Test]
        public void Peek_DoesNotRemoveItem()
        {
            var q = new Deque<string>();

            q.Enqueue("b");
            q.EnqueueFront("a");

            Assert.That(q.Peek(), Is.EqualTo("a"));
            Assert.That(q.Count, Is.EqualTo(2));
            Assert.That(q.Peek(), Is.EqualTo("a"));
            Assert.That(q.Dequeue(), Is.EqualTo("a"));
            Assert.That(q.Peek(), Is.EqualTo("b"));
            Assert.That(q.Dequeue(), Is.EqualTo("b"));
            Assert.That(q.Count, Is.EqualTo(0));
        }

        [Test]
        public void Clear_RemovesAllItems()
        {
            var q = new Deque<int>();

            q.EnqueueFront(1);
            q.Enqueue(2);
            q.Clear();

            Assert.That(q.Count, Is.EqualTo(0));

            q.Enqueue(3);
            Assert.That(q.Dequeue(), Is.EqualTo(3));
            Assert.That(q.Count, Is.EqualTo(0));
        }
    }
}