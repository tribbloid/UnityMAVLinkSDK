#nullable enable
using System.Collections.Generic;

namespace MAVLinkAPI.API.Pipes
{
    public class RawT : FromMsg<MAVLink.MAVLinkMessage>
    {
        protected override IDIndexed<CaseFn> MkTopics()
        {
            return new IDIndexed<CaseFn>();
        }

        protected override CaseFn OtherCase => m => new List<MAVLink.MAVLinkMessage> { m };
    }
}