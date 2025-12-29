using System;
using System.Collections.Generic;
using System.Linq;
using MAVLinkAPI.Routing;
using MAVLinkAPI.Util;
using MAVLinkAPI.Util.NullSafety;
using MAVLinkAPI.Util.Resource;
using UnityEngine;

namespace MAVLinkAPI.API.Feature
{
    // TODO: how about making it a normal class with metrics?
    public static class Common
    {
        public static Reader<Quaternion> ReadAttitude(
            this Uplink uplink
        )
        {
            var getAttitudeQ = MsgPipe.On<MAVLink.mavlink_attitude_t>()
                .Select((_, msg) =>
                {
                    var data = msg.Data;

                    var q = UnityQuaternionExtensions.AeronauticFrame.FromEulerRadian(
                        -data.pitch, data.yaw, -data.roll
                    );

                    return q;
                });


            return uplink.Read(getAttitudeQ);
        }

        // experimental, supported only by ArduPilot 4.1.0+
        public static Reader<Quaternion> ReadAttitudeQ(
            this Uplink uplink
        )
        {
            // var backup = Pipe.On<MAVLink.mavlink_attitude_quaternion_cov_t>(); TODO: switch to this for health check
            var getAttitudeQ = MsgPipe.On<MAVLink.mavlink_attitude_quaternion_t>()
                .Select((_, msg) =>
                {
                    var data = msg.Data;

                    // receiving quaternion in WXYZ order, FRD frame when facing north (a.k.a NED frame) (right-hand)
                    // FRD = Fowward-Right-Down
                    // NED = North-East-Down
                    // see MAVLink common.xml

                    // converting to XYZW order

                    // var q = new Quaternion(data.q1, data.q2, data.q3, data.q4);
                    // var q = new Quaternion(
                    //     -data.q2, -data.q4, -data.q3, data.q1
                    // ); // chiral conversion
                    var q = UnityQuaternionExtensions.AeronauticFrame.From(
                        data.q1, data.q2, data.q3, data.q4
                    );

                    return q;
                });


            return uplink.Read(getAttitudeQ);
        }

        public class NavigationFeed : RecurrentDaemon
        {
            public static NavigationFeed OfUplink(Lifetime lifetime, Uplink uplink)
            {
                var watchDog = Minimal.WatchDog(uplink);
                var attitudeReader = ReadAttitude(uplink);

                var result = new NavigationFeed(lifetime)
                {
                    WatchDog = watchDog,
                    AttitudeReader = attitudeReader
                };

                return result;
            }

            private NavigationFeed(Lifetime lifetime) : base(lifetime)
            {
            }

            public Reader<RxMessage<MAVLink.mavlink_heartbeat_t>> WatchDog { get; init; }
            public Reader<Quaternion> AttitudeReader { get; init; }


            // TODO: need latency
            public Atomic<DateTime> LastHeartBeat = new(DateTime.MinValue);

            // TODO: need covariance
            public Atomic<Quaternion> LastAttitude = new(Quaternion.identity);

            private Maybe<Reader<object>> _updater;

            public Reader<object> Updater => _updater.Lazy(() =>
            {
                var newSources = new Dictionary<Uplink, Pipe<MAVLink.MAVLinkMessage, object>>();

                void Add(Uplink uplink, Pipe<MAVLink.MAVLinkMessage, object> pipe)
                {
                    if (newSources.TryGetValue(uplink, out var existing))
                        newSources[uplink] = existing.Cat(pipe);
                    else
                        newSources[uplink] = pipe;
                }

                Add(
                    WatchDog.Pair.Key,
                    WatchDog.Pair.Value.SelectMany((_, v) =>
                    {
                        LastHeartBeat.Value = v.RxTime;
                        return new List<object> { };
                    })
                );

                Add(
                    AttitudeReader.Pair.Key,
                    AttitudeReader.Pair.Value.SelectMany((_, v) =>
                    {
                        LastAttitude.Value = v;
                        return new List<object> { };
                    })
                );

                var combined = newSources.Single();
                return new Reader<object>(combined);
            });


            protected override void Iterate()
            {
                Updater.Drain();
            }

            public override void DoClean()
            {
                base.DoClean();
                var uplinks = new HashSet<Uplink>
                {
                    WatchDog.Uplink,
                    AttitudeReader.Uplink
                };

                foreach (var uplink in uplinks) uplink.Dispose();
            }

            public override IEnumerable<string> GetStatusDetail()
            {
                var list = new List<string>
                {
                    $"    - heartbeat count : {LastHeartBeat.UpdateCount}",
                    $"    - attitude count : {LastAttitude.UpdateCount}"
                };

                return list.Union(base.GetStatusDetail());
            }
        }
    }
}