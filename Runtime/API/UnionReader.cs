#nullable enable
using System.Collections.Generic;
using System.Linq;
using MAVLinkAPI.Routing;
using MAVLinkAPI.Util;

namespace MAVLinkAPI.API
{
    [System.Obsolete("SuperReaderV2<T> is deprecated. Use UnionT of Reader<T> directly (per Uplink) instead.")]
    public class UnionReader<T> : Pipe<Unit, T>
    {
        public readonly IDictionary<Uplink, Pipe<MAVLink.MAVLinkMessage, T>> Sources;

        public UnionReader(IDictionary<Uplink, Pipe<MAVLink.MAVLinkMessage, T>> sources)
        {
            Sources = sources;
        }

        protected override List<T>? PrimaryFn(Unit input)
        {
            var list = Sources.SelectMany(pair => new Reader<T>(pair).Drain()).ToList();
            return list;
        }

        public override int Pressure => Sources.Sum(pair => pair.Key.BytesToRead);
    }
}