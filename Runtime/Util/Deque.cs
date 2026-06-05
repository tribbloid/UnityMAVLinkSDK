#nullable enable
using System.Collections.Generic;

namespace MAVLinkSDK.Util
{
    public class Deque<T> : Queue<T>
    {
        private readonly LinkedList<T> _front = new();

        public new int Count => _front.Count + base.Count;

        public void EnqueueFront(T item)
        {
            _front.AddFirst(item);
        }

        public new void Enqueue(T item)
        {
            base.Enqueue(item);
        }

        public new T Dequeue()
        {
            if (_front.Count > 0)
            {
                var v = _front.First!.Value;
                _front.RemoveFirst();
                return v;
            }

            return base.Dequeue();
        }

        public new T Peek()
        {
            if (_front.Count > 0) return _front.First!.Value;
            return base.Peek();
        }

        public new void Clear()
        {
            _front.Clear();
            base.Clear();
        }
    }
}