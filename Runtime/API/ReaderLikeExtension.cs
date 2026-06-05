#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using MAVLinkSDK.Util;


namespace MAVLinkSDK.API
{
    public static class ReaderLikeExtensions
    {
        public static List<T> Drain<T>(this Pipe<Unit, T> reader, int leftover = 8)
        {
            var list = new List<T>();

            while (reader.Pressure > leftover)
            {
                var current = reader.ProcessOrNull(Unit.Value);
                if (current != null)
                    list.AddRange(current);
            }

            return list;
        }
    }
}