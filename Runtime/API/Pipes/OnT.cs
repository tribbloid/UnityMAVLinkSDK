#nullable enable
using System.Linq;

namespace MAVLinkSDK.API.Pipes
{
    public class OnT<T> : ByTopics<RxMessage<T>> where T : struct
    {
        public readonly Pipe<MAVLink.MAVLinkMessage, MAVLink.MAVLinkMessage> Prev;

        public OnT(Pipe<MAVLink.MAVLinkMessage, MAVLink.MAVLinkMessage> prev)
        {
            Prev = prev;
        }

        protected override IDIndexed<CaseFn> MkTopics()
        {
            var result = new IDIndexed<CaseFn>();
            CaseFn topic = message =>
            {
                var ms = Prev.Process(message);
                var res = ms.Select(m => new RxMessage<T>(m)).ToList();

                return res;
            };
            result.Get<T>().Value = topic;
            return result;
        }
    }
}