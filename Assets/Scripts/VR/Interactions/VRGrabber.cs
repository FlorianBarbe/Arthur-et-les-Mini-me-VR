using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

namespace HackathonVR.Interactions
{
    /// <summary>
    /// Allows a VR controller/hand to grab VRGrabInteractable objects.
    /// Attach this to your controller GameObjects.
    /// </summary>
    public class VRGrabber : MonoBehaviour
    {
        [Header("Hand Configuration")]
        [SerializeField] private HandType handType = HandType.Right;
        [SerializeField] private Transform grabPoint;
        [SerializeField] private float grabRadius = 0.1f;
        [SerializeField] private LayerMask grabLayerMask = ~0;
        
        [Header("Input Settings")]
        [SerializeField] private GrabInputType grabInput = GrabInputType.Grip;
        [SerializeField] private float grabThreshold = 0.7f;
        [SerializeField] private float releaseThreshold = 0.3f;
        
        [Header("Haptic Feedback")]
        [SerializeField] private bool enableHaptics = true;
        [SerializeField] private float defaultHapticAmplitude = 0.5f;
        [SerializeField] private float defaultHapticDuration = 0.1f;
        
        [Header("Visual Feedback")]
        [SerializeField] private bool showGrabPoint = true;
        [SerializeField] private Color grabPointColor = Color.cyan;
        
        [Header("Debug")]
        [SerializeField] private bool debugMode = false;
        
        public enum HandType { Left, Right }
        public enum GrabInputType { Grip, Trigger, GripOrTrigger }
        
        // State
        private InputDevice controller;
        private bool controllerFound = false;
        private VRGrabInteractable currentlyGrabbed;
        private VRGrabInteractable currentHoverTarget;
        private bool isGrabbing = false;
        private float currentGrabValue = 0f;
        
        // Sphere overlap results buffer
        private Collider[] overlapResults = new Collider[10];
        
        public Transform GrabPoint => grabPoint != null ? grabPoint : transform;
        public bool IsGrabbing => isGrabbing && currentlyGrabbed != null;
        public VRGrabInteractable GrabbedObject => currentlyGrabbed;
        public HandType Hand => handType;
        
        private void Start()
        {
            if (grabPoint == null)
            {
                // Create a default grab point
                GameObject grabPointObj = new GameObject("GrabPoint");
                grabPointObj.transform.SetParent(transform);
                grabPointObj.transform.localPosition = Vector3.zero;
                grabPoint = grabPointObj.transform;
            }
            
            TryFindController();
        }
        
        private void TryFindController()
        {
            InputDeviceCharacteristics characteristics = 
                InputDeviceCharacteristics.Controller |
                (handType == HandType.Left ? 
                    InputDeviceCharacteristics.Left : 
                    InputDeviceCharacteristics.Right);
            
            List<InputDevice> devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(characteristics, devices);
            
            if (devices.Count > 0)
            {
                controller = devices[0];
                controllerFound = true;
                Debug.Log($"[VRGrabber] Found {handType} controller: {controller.name}");
            }
        }
        
        private void Update()
        {
            if (!controllerFound)
            {
                TryFindController();
                return;
            }
            
            if (!controller.isValid)
            {
                controllerFound = false;
                return;
            }
            
            UpdateGrabInput();
            
            if (!isGrabbing)
            {
                CheckForHoverTargets();
            }
        }
        
        private void UpdateGrabInput()
        {
            float gripValue = 0f;
            float triggerValue = 0f;
            
            controller.TryGetFeatureValue(CommonUsages.grip, out gripValue);
            controller.TryGetFeatureValue(CommonUsages.trigger, out triggerValue);
            
            // Determine grab value based on input type
            switch (grabInput)
            {
                case GrabInputType.Grip:
                    currentGrabValue = gripValue;
                    break;
                case GrabInputType.Trigger:
                    currentGrabValue = triggerValue;
                    break;
                case GrabInputType.GripOrTrigger:
                    currentGrabValue = Mathf.Max(gripValue, triggerValue);
                    break;
            }
            
            // Handle grab/release
            if (!isGrabbing && currentGrabValue >= grabThreshold)
            {
                TryGrab();
            }
            else if (isGrabbing && currentGrabValue <= releaseThreshold)
            {
                Release();
            }
        }
        
        private void CheckForHoverTargets()
        {
            // Find nearby grabbable objects
            int hitCount = Physics.OverlapSphereNonAlloc(
                GrabPoint.position, 
                grabRadius, 
                overlapResults, 
                grabLayerMask,
                QueryTriggerInteraction.Ignore
            );
            
            VRGrabInteractable closestGrabbable = null;
            float closestDistance = float.MaxValue;
            
            for (int i = 0; i < hitCount; i++)
            {
                VRGrabInteractable grabbable = overlapResults[i].GetComponentInParent<VRGrabInteractable>();
                
                if (grabbable != null && grabbable.CanBeGrabbed)
                {
                    float distance = Vector3.Distance(GrabPoint.position, overlapResults[i].transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestGrabbable = grabbable;
                    }
                }
            }
            
            // Update hover state
            if (closestGrabbable != currentHoverTarget)
            {
                // Exit old hover
                if (currentHoverTarget != null)
                {
                    currentHoverTarget.OnHoverEnd(this);
                }
                
                // Enter new hover
                currentHoverTarget = closestGrabbable;
                
                if (currentHoverTarget != null)
                {
                    currentHoverTarget.OnHoverStart(this);
                }
            }
        }
        
        private void TryGrab()
        {
            if (currentHoverTarget != null && currentHoverTarget.CanBeGrabbed)
            {
                currentlyGrabbed = currentHoverTarget;
                currentHoverTarget.OnHoverEnd(this);
                currentHoverTarget = null;
                
                currentlyGrabbed.OnGrab(this);
                isGrabbing = true;
                
                if (debugMode)
                {
                    Debug.Log($"[VRGrabber] {handType} hand grabbed {currentlyGrabbed.gameObject.name}");
                }
            }
        }
        
        private void Release()
        {
            if (currentlyGrabbed != null)
            {
                if (debugMode)
                {
                    Debug.Log($"[VRGrabber] {handType} hand released {currentlyGrabbed.gameObject.name}");
                }
                
                currentlyGrabbed.OnRelease(this);
                currentlyGrabbed = null;
            }
            
            isGrabbing = false;
        }
        
        /// <summary>
        /// Force release the currently grabbed object
        /// </summary>
        public void ForceRelease()
        {
            Release();
        }
        
        /// <summary>
        /// Trigger haptic feedback on this controller
        /// </summary>
        public void TriggerHaptic()
        {
            TriggerHaptic(defaultHapticAmplitude, defaultHapticDuration);
        }
        
        /// <summary>
        /// Trigger haptic feedback with custom parameters
        /// </summary>
        public void TriggerHaptic(float amplitude, float duration)
        {
            if (!enableHaptics || !controllerFound || !controller.isValid) return;
            
            HapticCapabilities capabilities;
            if (controller.TryGetHapticCapabilities(out capabilities) && capabilities.supportsImpulse)
            {
                controller.SendHapticImpulse(0, amplitude, duration);
            }
        }
        
        /// <summary>
        /// Check if a specific button is pressed
        /// </summary>
        public bool IsButtonPressed(InputFeatureUsage<bool> button)
        {
            if (controller.TryGetFeatureValue(button, out bool pressed))
            {
                return pressed;
            }
            return false;
        }
        
        /// <summary>
        /// Get the thumbstick value
        /// </summary>
        public Vector2 GetThumbstickValue()
        {
            if (controller.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 value))
            {
                return value;
            }
            return Vector2.zero;
        }
        
        private void OnDrawGizmos()
        {
            if (!showGrabPoint) return;
            
            Transform point = grabPoint != null ? grabPoint : transform;
            
            Gizmos.color = isGrabbing ? Color.green : (currentHoverTarget != null ? Color.yellow : grabPointColor);
            Gizmos.DrawWireSphere(point.position, grabRadius);
            
            if (currentHoverTarget != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(point.position, currentHoverTarget.transform.position);
            }
        }
    }
}
