#nullable enable
using MAVLinkSDK.Util;
using MAVLinkSDK.Util.Resource;

namespace MAVLinkSDK.Tests.Util.Resource
{
    public class CleanableExample : Cleanable
    {
        public static readonly AtomicInt Counter = new();

        // default constructor with lifetime argument
        public CleanableExample(
            Lifetime? lifetime = null
        ) : base(lifetime)
        {
        }

        public override void DoClean()
        {
            Counter.Increment();
        }
    }
}