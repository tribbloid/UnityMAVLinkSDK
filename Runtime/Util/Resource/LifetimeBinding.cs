using MAVLinkSDK.Util.NullSafety;
using UnityEngine;

namespace MAVLinkSDK.Util.Resource
{
    public class LifetimeBinding : MonoBehaviour
    {
        protected Maybe<Lifetime> lifetimeExisting;

        public virtual Lifetime Lifetime => lifetimeExisting.Lazy(() => new Lifetime());

        // OnDestroy will dispose lifetime
        private void OnDestroy()
        {
            Lifetime.Dispose();
        }
    }
}