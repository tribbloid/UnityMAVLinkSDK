#nullable enable
using System.Collections.Generic;

namespace MAVLinkAPI.API
{
    public interface IPipe<in TIn, out TOut>
    {
        IEnumerable<TOut> Process(TIn input);

        IEnumerable<TOut>? ProcessOrNull(TIn input);
    }
}