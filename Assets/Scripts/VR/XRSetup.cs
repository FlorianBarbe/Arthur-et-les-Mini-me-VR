using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;
using System.Collections.Generic;

namespace HackathonVR
{
    /// <summary>
    /// Automatically sets up a complete XR Rig at runtime.
    /// Just add this script to an empty GameObject in your scene.
    /// </summary>
    public class XRSetup : MonoBehaviour
    {
        [Header("Setup Options")]
        [SerializeField] private bool createFloor = true;
        [SerializeField] private bool createInteractionManager = true;
        
        private Camera vrCamera;
        private Transform cameraTransform;
        
        private void Awake()
        {
            SetupXR();
        }
        
        private void SetupXR()
        {
            // Create XR Interaction Manager if needed
            if (createInteractionManager && FindFirstObjectByType<XRInteractionManager>() == null)
            {
                var interactionManager = new GameObject("XR Interaction Manager");
                interactionManager.AddComponent<XRInteractionManager>();
                Debug.Log("[XRSetup] Created XR Interaction Manager");
            }
            
            // Check if XR Origin already exists
            if (FindFirstObjectByType<XROrigin>() != null)
            {
                Debug.Log("[XRSetup] XR Origin already exists in scene");
                return;
            }
            
            // Create XR Origin
            var xrOrigin = new GameObject("XR Origin (XR Rig)");
            var origin = xrOrigin.AddComponent<XROrigin>();
            
            // Create Camera Offset
            var cameraOffset = new GameObject("Camera Offset");
            cameraOffset.transform.SetParent(xrOrigin.transform);
            cameraOffset.transform.localPosition = Vector3.zero;
            
            // Create Main Camera
            var cameraGO = new GameObject("Main Camera");
            cameraGO.tag = "MainCamera";
            cameraGO.transform.SetParent(cameraOffset.transform);
            cameraGO.transform.localPosition = Vector3.zero; // Will be updated by tracking
            
            vrCamera = cameraGO.AddComponent<Camera>();
            vrCamera.clearFlags = CameraClearFlags.Skybox;
            vrCamera.nearClipPlane = 0.1f;
            vrCamera.farClipPlane = 1000f;
            vrCamera.stereoTargetEye = StereoTargetEyeMask.Both;
            
            cameraGO.AddComponent<AudioListener>();
            cameraTransform = cameraGO.transform;
            
            // Configure XR Origin
            origin.Camera = vrCamera;
            origin.CameraFloorOffsetObject = cameraOffset;
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
            
            // Create Left Controller
            var leftController = CreateController("Left Controller", cameraOffset.transform, true);
            
            // Create Right Controller  
            var rightController = CreateController("Right Controller", cameraOffset.transform, false);
            
            Debug.Log("[XRSetup] XR Origin created successfully!");
            
            // Create floor if needed
            if (createFloor)
            {
                CreateFloor();
            }
            
            // Destroy any old cameras that aren't ours
            foreach (var cam in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (cam != vrCamera && cam.gameObject != cameraGO)
                {
                    Debug.Log($"[XRSetup] Removing duplicate camera: {cam.gameObject.name}");
                    Destroy(cam.gameObject);
                }
            }
        }
        
        private void Update()
        {
            // Manual head tracking update using XR InputDevice API
            UpdateHeadTracking();
        }
        
        private void UpdateHeadTracking()
        {
            if (cameraTransform == null) return;
            
            List<InputDevice> devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, devices);
            
            if (devices.Count > 0)
            {
                InputDevice headDevice = devices[0];
                
                // Update position
                if (headDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position))
                {
                    cameraTransform.localPosition = position;
                }
                
                // Update rotation
                if (headDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
                {
                    cameraTransform.localRotation = rotation;
                }
            }
        }
        
        private GameObject CreateController(string name, Transform parent, bool isLeft)
        {
            var controller = new GameObject(name);
            controller.transform.SetParent(parent);
            controller.transform.localPosition = Vector3.zero;
            
            // Add TrackedPoseDriver for controller tracking
            var characteristics = isLeft 
                ? InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller
                : InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller;
            
            var tracker = controller.AddComponent<ControllerTracker>();
            tracker.Initialize(characteristics, isLeft);
            
            // Add visual representation (simple sphere)
            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Visual";
            visual.transform.SetParent(controller.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = Vector3.one * 0.05f;
            
            // Remove collider from visual
            var visualCollider = visual.GetComponent<Collider>();
            if (visualCollider != null) Destroy(visualCollider);
            
            // Set material color
            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = isLeft ? Color.blue : Color.red;
            }
            
            return controller;
        }
        
        private void CreateFloor()
        {
            // Check if floor already exists
            if (GameObject.Find("Floor") != null) return;
            
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.position = new Vector3(0, -0.05f, 0);
            floor.transform.localScale = new Vector3(20, 0.1f, 20);
            
            var renderer = floor.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = new Color(0.3f, 0.3f, 0.35f);
            }
            
            Debug.Log("[XRSetup] Floor created");
        }
    }
    
    /// <summary>
    /// Simple controller tracker using XR InputDevice API
    /// </summary>
    public class ControllerTracker : MonoBehaviour
    {
        private InputDeviceCharacteristics characteristics;
        private InputDevice device;
        private bool deviceFound = false;
        private bool isLeft;
        
        public void Initialize(InputDeviceCharacteristics chars, bool left)
        {
            characteristics = chars;
            isLeft = left;
            TryFindDevice();
        }
        
        private void TryFindDevice()
        {
            List<InputDevice> devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(characteristics, devices);
            
            if (devices.Count > 0)
            {
                device = devices[0];
                deviceFound = true;
                Debug.Log($"[ControllerTracker] Found {(isLeft ? "left" : "right")} controller: {device.name}");
            }
        }
        
        private void Update()
        {
            if (!deviceFound || !device.isValid)
            {
                TryFindDevice();
                return;
            }
            
            // Update position
            if (device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position))
            {
                transform.localPosition = position;
            }
            
            // Update rotation
            if (device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
            {
                transform.localRotation = rotation;
            }
        }
    }
}
