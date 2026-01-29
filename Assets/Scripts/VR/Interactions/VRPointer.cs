using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;
using System.Collections.Generic;

namespace HackathonVR.Interactions
{
    /// <summary>
    /// Provides a laser pointer for VR interactions at distance.
    /// Can be used for UI, teleportation, or distant object interaction.
    /// </summary>
    public class VRPointer : MonoBehaviour
    {
        [Header("Pointer Settings")]
        [SerializeField] private float maxDistance = 10f;
        [SerializeField] private LayerMask pointerLayerMask = ~0;
        [SerializeField] private VRGrabber.HandType handType = VRGrabber.HandType.Right;
        
        [Header("Visual Settings")]
        [SerializeField] private bool showLine = true;
        [SerializeField] private float lineWidth = 0.01f;
        [SerializeField] private Color defaultColor = Color.cyan;
        [SerializeField] private Color hoverColor = Color.green;
        [SerializeField] private Color invalidColor = Color.red;
        [SerializeField] private Material lineMaterial;
        
        [Header("Cursor Settings")]
        [SerializeField] private bool showCursor = true;
        [SerializeField] private float cursorSize = 0.05f;
        [SerializeField] private GameObject customCursor;
        
        [Header("Input")]
        [SerializeField] private bool activateOnTrigger = true;
        [SerializeField] private float activationThreshold = 0.1f;
        
        [Header("Events")]
        public UnityEvent<RaycastHit> OnPointerHit;
        public UnityEvent<RaycastHit> OnPointerClick;
        public UnityEvent OnPointerMiss;
        
        // State
        private InputDevice controller;
        private bool controllerFound = false;
        private bool isActive = false;
        private bool isHovering = false;
        private RaycastHit currentHit;
        private GameObject currentHoverObject;
        
        // Visual components
        private LineRenderer lineRenderer;
        private GameObject cursor;
        private MeshRenderer cursorRenderer;
        private Material cursorMaterial;
        
        public bool IsActive => isActive;
        public bool IsHovering => isHovering;
        public RaycastHit CurrentHit => currentHit;
        public GameObject HoverObject => currentHoverObject;
        
        private void Start()
        {
            SetupLineRenderer();
            SetupCursor();
            TryFindController();
        }
        
        private void SetupLineRenderer()
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth * 0.5f;
            lineRenderer.useWorldSpace = true;
            
            if (lineMaterial != null)
            {
                lineRenderer.material = lineMaterial;
            }
            else
            {
                // Create default material
                lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
            }
            
            lineRenderer.startColor = defaultColor;
            lineRenderer.endColor = defaultColor;
            lineRenderer.enabled = false;
        }
        
        private void SetupCursor()
        {
            if (customCursor != null)
            {
                cursor = Instantiate(customCursor);
            }
            else
            {
                cursor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                cursor.name = "PointerCursor";
                Destroy(cursor.GetComponent<Collider>());
                
                cursorRenderer = cursor.GetComponent<MeshRenderer>();
                cursorMaterial = new Material(Shader.Find("Unlit/Color"));
                cursorMaterial.color = defaultColor;
                cursorRenderer.material = cursorMaterial;
            }
            
            cursor.transform.localScale = Vector3.one * cursorSize;
            cursor.SetActive(false);
        }
        
        private void TryFindController()
        {
            InputDeviceCharacteristics characteristics = 
                InputDeviceCharacteristics.Controller |
                (handType == VRGrabber.HandType.Left ? 
                    InputDeviceCharacteristics.Left : 
                    InputDeviceCharacteristics.Right);
            
            List<InputDevice> devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(characteristics, devices);
            
            if (devices.Count > 0)
            {
                controller = devices[0];
                controllerFound = true;
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
            
            UpdateActivation();
            
            if (isActive)
            {
                UpdatePointer();
            }
            
            UpdateVisuals();
        }
        
        private void UpdateActivation()
        {
            if (activateOnTrigger)
            {
                controller.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue);
                isActive = triggerValue > activationThreshold;
            }
            else
            {
                isActive = true; // Always active if not using trigger
            }
        }
        
        private void UpdatePointer()
        {
            Ray ray = new Ray(transform.position, transform.forward);
            
            if (Physics.Raycast(ray, out currentHit, maxDistance, pointerLayerMask))
            {
                isHovering = true;
                
                // Track hover object changes
                if (currentHit.collider.gameObject != currentHoverObject)
                {
                    OnHoverExit();
                    currentHoverObject = currentHit.collider.gameObject;
                    OnHoverEnter();
                }
                
                OnPointerHit?.Invoke(currentHit);
                
                // Check for click
                if (CheckForClick())
                {
                    OnPointerClick?.Invoke(currentHit);
                    TriggerHaptic(0.3f, 0.1f);
                }
            }
            else
            {
                if (isHovering)
                {
                    OnHoverExit();
                    currentHoverObject = null;
                }
                isHovering = false;
                OnPointerMiss?.Invoke();
            }
        }
        
        private void OnHoverEnter()
        {
            if (currentHoverObject == null) return;
            
            // Light haptic on hover
            TriggerHaptic(0.1f, 0.05f);
            
            // Notify IPointerHoverable if implemented
            var hoverable = currentHoverObject.GetComponent<IPointerHoverable>();
            if (hoverable != null)
            {
                hoverable.OnPointerEnter(this);
            }
        }
        
        private void OnHoverExit()
        {
            if (currentHoverObject == null) return;
            
            var hoverable = currentHoverObject.GetComponent<IPointerHoverable>();
            if (hoverable != null)
            {
                hoverable.OnPointerExit(this);
            }
        }
        
        private bool previousTriggerState = false;
        private bool CheckForClick()
        {
            controller.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerPressed);
            
            bool clicked = triggerPressed && !previousTriggerState;
            previousTriggerState = triggerPressed;
            
            return clicked;
        }
        
        private void UpdateVisuals()
        {
            if (showLine)
            {
                lineRenderer.enabled = isActive;
                
                if (isActive)
                {
                    lineRenderer.SetPosition(0, transform.position);
                    
                    if (isHovering)
                    {
                        lineRenderer.SetPosition(1, currentHit.point);
                        SetLineColor(hoverColor);
                    }
                    else
                    {
                        lineRenderer.SetPosition(1, transform.position + transform.forward * maxDistance);
                        SetLineColor(defaultColor);
                    }
                }
            }
            
            if (showCursor)
            {
                cursor.SetActive(isActive && isHovering);
                
                if (isActive && isHovering)
                {
                    cursor.transform.position = currentHit.point + currentHit.normal * 0.001f;
                    cursor.transform.rotation = Quaternion.LookRotation(currentHit.normal);
                    
                    if (cursorMaterial != null)
                    {
                        cursorMaterial.color = hoverColor;
                    }
                }
            }
        }
        
        private void SetLineColor(Color color)
        {
            lineRenderer.startColor = color;
            lineRenderer.endColor = color * 0.7f;
            lineRenderer.material.color = color;
        }
        
        private void TriggerHaptic(float amplitude, float duration)
        {
            if (!controllerFound || !controller.isValid) return;
            
            HapticCapabilities capabilities;
            if (controller.TryGetHapticCapabilities(out capabilities) && capabilities.supportsImpulse)
            {
                controller.SendHapticImpulse(0, amplitude, duration);
            }
        }
        
        /// <summary>
        /// Force activate/deactivate the pointer
        /// </summary>
        public void SetActive(bool active)
        {
            activateOnTrigger = false;
            isActive = active;
        }
        
        /// <summary>
        /// Set the maximum raycast distance
        /// </summary>
        public void SetMaxDistance(float distance)
        {
            maxDistance = distance;
        }
        
        private void OnDestroy()
        {
            if (cursor != null) Destroy(cursor);
            if (cursorMaterial != null) Destroy(cursorMaterial);
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = defaultColor;
            Gizmos.DrawRay(transform.position, transform.forward * maxDistance);
        }
    }
    
    /// <summary>
    /// Interface for objects that respond to pointer hover
    /// </summary>
    public interface IPointerHoverable
    {
        void OnPointerEnter(VRPointer pointer);
        void OnPointerExit(VRPointer pointer);
    }
}
