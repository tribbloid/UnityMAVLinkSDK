#nullable enable
using System;
using MAVLinkSDK.API.Feature;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using MAVLinkSDK.XR;

namespace MAVLinkSDK.API.UI
{
    public class NavPoseProvider : MonoBehaviour
    {
        public bool verboseLogging = false;

        [NonSerialized] public Common.NavigationFeed? ActiveFeed;

        private NavPoseHMD? _device;

        private static bool _layoutRegistered;

        private void Start()
        {
            if (!_layoutRegistered)
            {
                InputSystem.RegisterLayout<NavPoseHMD>();
                _layoutRegistered = true;
            }

            _device = InputSystem.AddDevice<NavPoseHMD>();

            // bind the TrackedPoseDriver on this GameObject to our virtual
            // device; trackingStateInput is left empty so the driver infers
            // tracking validity from whether the device controls resolve
            var driver = GetComponent<TrackedPoseDriver>();
            if (driver != null)
            {
                driver.positionInput = new InputActionProperty(new InputAction(
                    "Position", binding: "<NavPoseHMD>/devicePosition",
                    expectedControlType: "Vector3"));
                driver.rotationInput = new InputActionProperty(new InputAction(
                    "Rotation", binding: "<NavPoseHMD>/deviceRotation",
                    expectedControlType: "Quaternion"));
            }

            Application.onBeforeRender += PushPoseToInputSystem;
        }

        private void Update()
        {
            PushPoseToInputSystem();
        }

        // push a fresh pose right before the driver samples it for rendering;
        // the negative order mirrors the deprecated TrackedPoseDriver
        [BeforeRenderOrder(-30000)]
        private void PushPoseToInputSystem()
        {
            if (_device == null) return;

            var flags = GetPoseFromProvider(out var pose);
            if ((flags & InputTrackingState.Rotation) != 0)
                InputState.Change(_device.deviceRotation, pose.rotation);
            if ((flags & InputTrackingState.Position) != 0)
                InputState.Change(_device.devicePosition, pose.position);
        }

        public void Connect(Common.NavigationFeed daemon)
        {
            if (ActiveFeed != null) Unbind();

            ActiveFeed = daemon;
            ActiveFeed.Start();
        }

        public void Unbind()
        {
            lock (this)
            {
                ActiveFeed?.Dispose();
                ActiveFeed = null;
            }
        }

        private void OnDestroy()
        {
            Application.onBeforeRender -= PushPoseToInputSystem;
            Unbind();
            if (_device != null && _device.added) InputSystem.RemoveDevice(_device);
        }

        // Update Pose
        public InputTrackingState GetPoseFromProvider(out Pose output)
        {
            var d = ActiveFeed;
            if (d != null)
            {
                var qRaw = d.LastAttitude.Value;
                output = new Pose(new Vector3(0, 0, 0), qRaw);

                if (verboseLogging)
                    Debug.Log($"update from XR (Euler): {qRaw.x}, {qRaw.y}, {qRaw.z}, {qRaw.w}");

                return InputTrackingState.Rotation;
            }

            output = Pose.identity;
            return InputTrackingState.None;
        }
    }
}