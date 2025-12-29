namespace MAVLinkAPI.API
{
    public static class Mock
    {
        public static MAVLink.MAVLinkMessage MockHeartbeat()
        {
            var heartbeat = new MAVLink.mavlink_heartbeat_t
            {
                type = (byte)MAVLink.MAV_TYPE.QUADROTOR,
                autopilot = (byte)MAVLink.MAV_AUTOPILOT.GENERIC,
                base_mode = (byte)MAVLink.MAV_MODE_FLAG.MANUAL_INPUT_ENABLED,
                custom_mode = 0,
                system_status = (byte)MAVLink.MAV_STATE.STANDBY,
                mavlink_version = 3
            };

            var parser = new MAVLink.MavlinkParse();
            var packetBytes = parser.GenerateMAVLinkPacket20(MAVLink.MAVLINK_MSG_ID.HEARTBEAT, heartbeat);
            return new MAVLink.MAVLinkMessage(packetBytes);
        }

        public static MAVLink.MAVLinkMessage MockSystemTime()
        {
            var systemTime = new MAVLink.mavlink_system_time_t
            {
                time_unix_usec = 1,
                time_boot_ms = 2
            };

            var parser = new MAVLink.MavlinkParse();
            var packetBytes = parser.GenerateMAVLinkPacket20(MAVLink.MAVLINK_MSG_ID.SYSTEM_TIME, systemTime);
            return new MAVLink.MAVLinkMessage(packetBytes);
        }
    }
}