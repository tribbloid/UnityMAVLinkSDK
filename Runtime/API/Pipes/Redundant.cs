#nullable enable
using System;
using System.Collections.Generic;

namespace MAVLinkAPI.API.Pipes
{
    /**
     * works similar to UnionT, but with the following verification routine:
     *
     * - each message is expected to be received in both channels in short succession
     * - when a message is received:
     *   - first time => message is immediately forwarded, a MsgState entry is created
     *     from it and pushed into a cache queue (indexed by MsgKey)
     *   - second time from a different channel => verification successful, MsgState is removed from the cache queue
     *   - second time from the same channel => double transmission, emit an error and increment the error count
     * - if a MsgState is not verified and passed a short deadline, it should be marked as stale: Emit a
     *   warning and increment the warning count
     * - if a MsgState is not verified and passed a long deadline: it should be considered lost:
     *   Emit an error and increment the error count, MsgState is removed from the cache queue
     */
    public class Redundant<TIn, TMsg> :
        Pipe<TIn, RxMessage<TMsg>>.LeftAndRight<
            Pipe<TIn, RxMessage<TMsg>>,
            Pipe<TIn, RxMessage<TMsg>>
        >
    {
        public record MsgKey(
            Signature Sig,
            int Hash
        )
        {
        }

        public record MsgState(
            MsgKey Key,
            DateTime RxTime,
            bool IsStale
        )
        {
        }

        private sealed class CacheEntry
        {
            public MsgState State;
            public bool FromLeft;
            public bool Verified;

            public CacheEntry(MsgState state, bool fromLeft)
            {
                State = state;
                FromLeft = fromLeft;
            }
        }

        private readonly Dictionary<MsgKey, CacheEntry> _cache = new();
        private readonly Queue<CacheEntry> _queue = new();
        private readonly object _lock = new();

        public TimeSpan StaleThreshold { get; }
        public TimeSpan LostThreshold { get; }

        public int WarningCount { get; private set; }
        public int ErrorCount { get; private set; }

        public Redundant() : this(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(2))
        {
        }

        public Redundant(TimeSpan staleThreshold, TimeSpan lostThreshold)
        {
            StaleThreshold = staleThreshold;
            LostThreshold = lostThreshold;
        }

        protected override List<RxMessage<TMsg>>? PrimaryFn(TIn input)
        {
            var leftMessages = Left.Process(input);
            var rightMessages = Right.Process(input);

            List<RxMessage<TMsg>> result;
            lock (_lock)
            {
                result = new List<RxMessage<TMsg>>();

                foreach (var m in HandleChannelMessages(leftMessages, true))
                    result.Add(m);

                foreach (var m in HandleChannelMessages(rightMessages, false))
                    result.Add(m);
            }

            return result.Count == 0 ? null : result;
        }

        private IEnumerable<RxMessage<TMsg>> HandleChannelMessages(IEnumerable<RxMessage<TMsg>> messages, bool fromLeft)
        {
            foreach (var msg in messages)
            {
                Cleanup(msg.RxTime);

                var key = ComputeKey(msg);

                if (_cache.TryGetValue(key, out var existing))
                {
                    if (existing.FromLeft == fromLeft)
                    {
                        ErrorCount++;
                    }
                    else
                    {
                        existing.Verified = true;
                        _cache.Remove(key);
                    }

                    continue;
                }

                var state = new MsgState(key, msg.RxTime, false);
                var entry = new CacheEntry(state, fromLeft);
                _cache[key] = entry;
                _queue.Enqueue(entry);

                yield return msg;
            }
        }

        private void Cleanup(DateTime now)
        {
            while (_queue.Count > 0)
            {
                var entry = _queue.Peek();

                if (entry.Verified)
                {
                    _queue.Dequeue();
                    continue;
                }

                var age = now - entry.State.RxTime;

                if (age >= LostThreshold)
                {
                    _queue.Dequeue();
                    _cache.Remove(entry.State.Key);
                    ErrorCount++;
                    continue;
                }

                if (!entry.State.IsStale && age >= StaleThreshold)
                {
                    entry.State = entry.State with { IsStale = true };
                    WarningCount++;
                }

                break;
            }
        }

        private static MsgKey ComputeKey(RxMessage<TMsg> message)
        {
            var sig = message.Signature;
            var hash = new HashCode();
            hash.Add(message.Data);
            hash.Add(message.RxTime);
            hash.Add(message.Sender.SystemID);
            hash.Add(message.Sender.ComponentID);

            return new MsgKey(sig, hash.ToHashCode());
        }
    }
}