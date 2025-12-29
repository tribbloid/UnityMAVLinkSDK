#nullable enable
using System.Collections.Generic;
using System.Linq;
using MAVLinkAPI.Routing;
using MAVLinkAPI.Util;

namespace MAVLinkAPI.API
{
    public class Reader<T> : Pipe<Unit, T>
    {
        public KeyValuePair<Uplink, Pipe<MAVLink.MAVLinkMessage, T>> Pair;

        public Uplink Uplink => Pair.Key;
        public Pipe<MAVLink.MAVLinkMessage, T> Pipe => Pair.Value;

        public IEnumerable<List<T>> ByMessage;

        public Reader(KeyValuePair<Uplink, Pipe<MAVLink.MAVLinkMessage, T>> pair)
        {
            Pair = pair;
            ByMessage = Pair.Key.RawReadSource.Select(message => Pair.Value.Process(message));
        }

        public Reader(Uplink uplink, Pipe<MAVLink.MAVLinkMessage, T> pipe) : this(
            KeyValuePair.Create(uplink, pipe))
        {
        }

        public override int Pressure => Pair.Key.BytesToRead;

        protected override List<T>? PrimaryFn(Unit input)
        {
            using (var byMessageItr = ByMessage.GetEnumerator())
            {
                if (byMessageItr.MoveNext())
                    return byMessageItr.Current;
            }

            return null;
        }
    }
}