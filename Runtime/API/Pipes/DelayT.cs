#nullable enable

using System;
using System.Collections.Generic;
using MAVLinkAPI.Util;

namespace MAVLinkAPI.API.Pipes
{
    /**
     * add a small delay to `Base`, message emit from it are first commit into a cyclic buffer
     * instead of being returned by `ProcessOrNull` immediately.
     *
     * `ProcessOrNull` only returns a message if its Signature.timestamp has passed the latency
     *
     * if no message is available, `ProcessOrNull` returns null
     */
    public class DelayT<TIn, TMsg> : Pipe<TIn, RxMessage<TMsg>>
    {
        public readonly TimeSpan Latency;

        public readonly Pipe<TIn, RxMessage<TMsg>> Base;

        private readonly Deque<RxMessage<TMsg>> _buffer = new();
        private readonly object _lock = new();
        private readonly int _capacity;

        public override int Pressure
        {
            get
            {
                lock (_lock)
                {
                    return Base.Pressure + _buffer.Count;
                }
            }
        }

        public DelayT(Pipe<TIn, RxMessage<TMsg>> @base, TimeSpan latency, int capacity = 64)
        {
            Base = @base;
            Latency = latency;
            _capacity = capacity <= 0 ? 1 : capacity;
        }

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

        private static bool IsReady(RxMessage<TMsg> message, ulong nowTimestamp, ulong latencyTimestamp)
        {
            if (latencyTimestamp == 0) return true;

            var msgTimestamp = message.Signature.Timestamp;
            if (nowTimestamp < msgTimestamp) return false;

            return nowTimestamp - msgTimestamp >= latencyTimestamp;
        }

        protected override List<RxMessage<TMsg>>? PrimaryFn(TIn input)
        {
            var incoming = Base.ProcessOrNull(input);
            var nowTimestamp = NowSigTimestamp();
            var latencyTimestamp = LatencyToSigTimestamp(Latency);

            lock (_lock)
            {
                if (incoming != null)
                {
                    foreach (var msg in incoming)
                    {
                        _buffer.Enqueue(msg);
                        if (_buffer.Count > _capacity) _buffer.Dequeue();
                    }
                }

                List<RxMessage<TMsg>>? result = null;
                var n = _buffer.Count;
                List<RxMessage<TMsg>>? skipped = null;
                for (var i = 0; i < n; i++)
                {
                    var next = _buffer.Dequeue();
                    if (IsReady(next, nowTimestamp, latencyTimestamp))
                    {
                        result = new List<RxMessage<TMsg>> { next };
                        break;
                    }

                    skipped ??= new List<RxMessage<TMsg>>();
                    skipped.Add(next);
                }

                if (skipped != null)
                {
                    for (var i = skipped.Count - 1; i >= 0; i--)
                    {
                        _buffer.EnqueueFront(skipped[i]);
                    }
                }

                return result;
            }
        }
    }
}