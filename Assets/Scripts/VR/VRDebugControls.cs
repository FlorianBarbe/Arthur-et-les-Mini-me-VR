using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

namespace HackathonVR
{
    /// <summary>
    /// Debug controls for VR:
    /// - Press both grip buttons to skip to next scene
    /// - Use right joystick: UP to teleport, LEFT/RIGHT to rotate
    /// </summary>
    public class VRDebugControls : MonoBehaviour
    {
        [Header("Debug Scene Skip")]
        [Tooltip("Hold both grips for this duration to skip to next scene")]
        public float holdDuration = 2f;
        
        [Header("Joystick Movement")]
        public float teleportDistance = 2f;
        public float snapTurnAngle = 45f;
        public float actionCooldown = 0.3f;

        private float bothGripsHeldTime = 0f;
        private float lastActionTime = 0f;
        private bool hasSkipped = false;

        private InputDevice leftController;
        private InputDevice rightController;
        private bool controllersFound = false;

        private void Update()
        {
            if (!controllersFound)
            {
                TryFindControllers();
            }

            if (controllersFound)
            {
                HandleDebugSceneSkip();
                HandleJoystickMovement();
            }
        }

        private void TryFindControllers()
        {
            List<InputDevice> leftDevices = new List<InputDevice>();
            List<InputDevice> rightDevices = new List<InputDevice>();

            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, 
                leftDevices);
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, 
                rightDevices);

            if (leftDevices.Count > 0 && rightDevices.Count > 0)
            {
                leftController = leftDevices[0];
                rightController = rightDevices[0];
                controllersFound = true;
                Debug.Log("[VRDebugControls] Controllers found!");
            }
        }

        private void HandleDebugSceneSkip()
        {
            bool leftGrip = false;
            bool rightGrip = false;

            leftController.TryGetFeatureValue(CommonUsages.gripButton, out leftGrip);
            rightController.TryGetFeatureValue(CommonUsages.gripButton, out rightGrip);

            if (leftGrip && rightGrip)
            {
                bothGripsHeldTime += Time.deltaTime;

                if (bothGripsHeldTime >= holdDuration && !hasSkipped)
                {
                    hasSkipped = true;
                    SkipToNextScene();
                }
            }
            else
            {
                bothGripsHeldTime = 0f;
                hasSkipped = false;
            }
        }

        private void HandleJoystickMovement()
        {
            if (Time.time - lastActionTime < actionCooldown) return;

            Vector2 joystick = Vector2.zero;
            rightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out joystick);

            // Joystick UP - Teleport forward
            if (joystick.y > 0.7f)
            {
                TeleportForward();
                lastActionTime = Time.time;
            }
            // Joystick LEFT - Rotate left
            else if (joystick.x < -0.7f)
            {
                SnapTurn(-snapTurnAngle);
                lastActionTime = Time.time;
            }
            // Joystick RIGHT - Rotate right
            else if (joystick.x > 0.7f)
            {
                SnapTurn(snapTurnAngle);
                lastActionTime = Time.time;
            }
        }

        private void TeleportForward()
        {
            Camera vrCam = Camera.main;
            if (vrCam == null) return;

            // Get forward direction (horizontal only)
            Vector3 forward = vrCam.transform.forward;
            forward.y = 0;
            forward.Normalize();

            GameObject vrRig = GameObject.Find("XR Origin (XR Rig)");
            if (vrRig == null) vrRig = GameObject.Find("VR Setup");

            if (vrRig != null)
            {
                vrRig.transform.position += forward * teleportDistance;
            }
        }

        private void SnapTurn(float angle)
        {
            GameObject vrRig = GameObject.Find("XR Origin (XR Rig)");
            if (vrRig == null) vrRig = GameObject.Find("VR Setup");

            if (vrRig != null)
            {
                vrRig.transform.Rotate(0, angle, 0);
            }
        }

        private void SkipToNextScene()
        {
            var gameManager = Core.GameManager.Instance;
            if (gameManager != null)
            {
                Debug.Log("[VRDebugControls] DEBUG: Skipping to next scene!");
                gameManager.LoadNextScene();
            }
            else
            {
                Debug.LogWarning("[VRDebugControls] No GameManager found!");
            }
        }
    }
}
