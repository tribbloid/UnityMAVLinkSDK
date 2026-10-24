#nullable enable
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;

namespace MAVLinkSDK.XR
{
    /// <summary>
    /// A virtual tracked device that receives the navigation pose published by
    /// the MAVLink feed. NavPoseProvider writes poses into it each frame, and
    /// the Input System TrackedPoseDriver on the same GameObject drives its
    /// Transform from the inherited devicePosition and deviceRotation controls.
    /// updateBeforeRender opts the device into the Input System's
    /// BeforeRender update so the driver can sample a fresh pose for rendering.
    /// </summary>
    [InputControlLayout(displayName = "Nav Pose HMD", updateBeforeRender = true)]
    public class NavPoseHMD : TrackedDevice
    {
    }
}
