#nullable enable
using System.Collections.Generic;

namespace MAVLinkSDK.API.Pipes
{
    public class RawT : ByTopics<MAVLink.MAVLinkMessage>
    {
        protected override IDIndexed<CaseFn> MkTopics()
        {
            return new IDIndexed<CaseFn>();
        }

        protected override CaseFn OtherCase => m => new List<MAVLink.MAVLinkMessage> { m };
    }
}