using System;

namespace MAVLinkSDK.log4net
{
    internal class LogManager
    {
        internal static ILog GetLogger(Type declaringType)
        {
            return new Log();
        }
    }
}