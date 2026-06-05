#nullable enable
using System;
using System.Collections.Generic;
using MAVLinkSDK.Util.NullSafety;

namespace MAVLinkSDK.API.Pipes
{
    public static class MsgPipe // this should become the new reader
    {
        /// <summary>
        /// Test utility method to create a mock MAVLink heartbeat message
        /// </summary>
        /// <returns>A MAVLinkMessage containing a heartbeat with quadrotor type configuration</returns>
        public static MAVLink.MAVLinkMessage MockHeartbeat()
        {
            return Mock.MockHeartbeat();
        }

        public static readonly RawT Raw = new();

        public static OnT<T> On<T>(Pipe<MAVLink.MAVLinkMessage, MAVLink.MAVLinkMessage>? prev = null)
            where T : struct // TODO: this should be a shortcut on Uplink
        {
            prev ??= Raw;

            return new OnT<T>(prev);
        }
    }

    public abstract class ByTopics<T> : Pipe<MAVLink.MAVLinkMessage, T>
    {
        public delegate List<T>? CaseFn(MAVLink.MAVLinkMessage message);

        private Maybe<IDIndexed<CaseFn>> _topics;
        public IDIndexed<CaseFn> Topics => _topics.Lazy(MkTopics);

        protected abstract IDIndexed<CaseFn> MkTopics();

        protected virtual CaseFn OtherCase => _ => null;
        // theoretically this will be interned to avoid multiple initialization

        protected override List<T>? PrimaryFn(MAVLink.MAVLinkMessage input)
        {
            var topic = Topics.Get(input.msgid).ValueOrDefault;
            return topic != null ? topic(input) : null;
        }

        protected override List<T>? FallbackFn(MAVLink.MAVLinkMessage input)
        {
            return OtherCase(input);
        }

        public static implicit operator Func<MAVLink.MAVLinkMessage, List<T>?>(ByTopics<T> function)
        {
            return function.Process;
        }
    }
}