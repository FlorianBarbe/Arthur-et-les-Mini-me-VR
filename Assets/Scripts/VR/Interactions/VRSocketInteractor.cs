using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace HackathonVR.Interactions
{
    /// <summary>
    /// A socket where VRGrabInteractable objects can be placed.
    /// Useful for puzzles, item storage, or any "slot" mechanic.
    /// </summary>
    public class VRSocketInteractor : MonoBehaviour
    {
        [Header("Socket Settings")]
        [SerializeField] private float socketRadius = 0.15f;
        [SerializeField] private bool snapOnRelease = true;
        [SerializeField] private float snapSpeed = 10f;
        [SerializeField] private bool lockOnSnap = false;
        
        [Header("Filter Settings")]
        [SerializeField] private bool useTagFilter = false;
        [SerializeField] private string requiredTag = "";
        [SerializeField] private bool useNameFilter = false;
        [SerializeField] private string requiredNameContains = "";
        
        [Header("Visual Feedback")]
        [SerializeField] private bool showSocketVisual = true;
        [SerializeField] private Color emptyColor = new Color(0.2f, 0.6f, 1f, 0.5f);
        [SerializeField] private Color hoverColor = new Color(1f, 0.8f, 0.2f, 0.7f);
        [SerializeField] private Color occupiedColor = new Color(0.2f, 1f, 0.2f, 0.5f);
        [SerializeField] private Color invalidColor = new Color(1f, 0.2f, 0.2f, 0.5f);
        
        [Header("Events")]
        public UnityEvent<VRGrabInteractable> OnObjectPlaced;
        public UnityEvent<VRGrabInteractable> OnObjectRemoved;
        public UnityEvent OnCorrectObjectPlaced;
        
        // State
        private VRGrabInteractable socketedObject;
        private VRGrabInteractable hoveringObject;
        private bool isOccupied = false;
        private bool isSnapping = false;
        
        // Visual
        private GameObject visualIndicator;
        private MeshRenderer visualRenderer;
        private Material visualMaterial;
        
        public bool IsOccupied => isOccupied;
        public VRGrabInteractable SocketedObject => socketedObject;
        
        private void Awake()
        {
            if (showSocketVisual)
            {
                CreateVisualIndicator();
            }
        }
        
        private void CreateVisualIndicator()
        {
            visualIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visualIndicator.name = "SocketVisual";
            visualIndicator.transform.SetParent(transform);
            visualIndicator.transform.localPosition = Vector3.zero;
            visualIndicator.transform.localScale = Vector3.one * socketRadius * 2f;
            
            // Remove collider from visual
            Destroy(visualIndicator.GetComponent<Collider>());
            
            // Setup transparent material
            visualRenderer = visualIndicator.GetComponent<MeshRenderer>();
            visualMaterial = new Material(Shader.Find("Standard"));
            visualMaterial.SetFloat("_Mode", 3); // Transparent
            visualMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            visualMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            visualMaterial.SetInt("_ZWrite", 0);
            visualMaterial.DisableKeyword("_ALPHATEST_ON");
            visualMaterial.EnableKeyword("_ALPHABLEND_ON");
            visualMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            visualMaterial.renderQueue = 3000;
            visualMaterial.color = emptyColor;
            visualRenderer.material = visualMaterial;
        }
        
        private void Update()
        {
            if (!isOccupied)
            {
                CheckForHoveringObjects();
            }
            
            if (isSnapping)
            {
                UpdateSnapping();
            }
            
            UpdateVisual();
        }
        
        private void CheckForHoveringObjects()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, socketRadius);
            
            VRGrabInteractable closest = null;
            float closestDist = float.MaxValue;
            
            foreach (var col in colliders)
            {
                VRGrabInteractable grabbable = col.GetComponentInParent<VRGrabInteractable>();
                
                if (grabbable != null && grabbable.IsGrabbed)
                {
                    float dist = Vector3.Distance(transform.position, grabbable.transform.position);
                    if (dist < closestDist && IsValidObject(grabbable))
                    {
                        closest = grabbable;
                        closestDist = dist;
                    }
                }
            }
            
            // Update hovering state
            if (closest != hoveringObject)
            {
                hoveringObject = closest;
                
                if (hoveringObject != null)
                {
                    // Provide haptic feedback to indicate socket nearby
                    var grabber = hoveringObject.CurrentGrabber;
                    if (grabber != null)
                    {
                        grabber.TriggerHaptic(0.2f, 0.1f);
                    }
                }
            }
            
            // Check if hovering object was released
            if (hoveringObject != null && !hoveringObject.IsGrabbed && snapOnRelease)
            {
                SnapObject(hoveringObject);
                hoveringObject = null;
            }
        }
        
        private bool IsValidObject(VRGrabInteractable obj)
        {
            if (useTagFilter && !string.IsNullOrEmpty(requiredTag))
            {
                if (!obj.CompareTag(requiredTag))
                {
                    return false;
                }
            }
            
            if (useNameFilter && !string.IsNullOrEmpty(requiredNameContains))
            {
                if (!obj.gameObject.name.Contains(requiredNameContains))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        private void SnapObject(VRGrabInteractable obj)
        {
            if (isOccupied) return;
            
            socketedObject = obj;
            isOccupied = true;
            isSnapping = true;
            
            // Disable physics during snap
            var rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            Debug.Log($"[VRSocketInteractor] Snapping {obj.gameObject.name} to socket {gameObject.name}");
        }
        
        private void UpdateSnapping()
        {
            if (socketedObject == null)
            {
                isSnapping = false;
                return;
            }
            
            float distance = Vector3.Distance(socketedObject.transform.position, transform.position);
            float angle = Quaternion.Angle(socketedObject.transform.rotation, transform.rotation);
            
            if (distance < 0.01f && angle < 1f)
            {
                // Snap complete
                socketedObject.transform.position = transform.position;
                socketedObject.transform.rotation = transform.rotation;
                isSnapping = false;
                
                if (lockOnSnap)
                {
                    socketedObject.SetGrabbable(false);
                }
                
                OnObjectPlaced?.Invoke(socketedObject);
                
                // Check if it's the "correct" object
                if (IsValidObject(socketedObject))
                {
                    OnCorrectObjectPlaced?.Invoke();
                }
                
                Debug.Log($"[VRSocketInteractor] {socketedObject.gameObject.name} placed in socket {gameObject.name}");
            }
            else
            {
                // Lerp to socket position
                socketedObject.transform.position = Vector3.Lerp(
                    socketedObject.transform.position, 
                    transform.position, 
                    snapSpeed * Time.deltaTime
                );
                socketedObject.transform.rotation = Quaternion.Slerp(
                    socketedObject.transform.rotation, 
                    transform.rotation, 
                    snapSpeed * Time.deltaTime
                );
            }
        }
        
        private void UpdateVisual()
        {
            if (!showSocketVisual || visualMaterial == null) return;
            
            Color targetColor;
            
            if (isOccupied)
            {
                targetColor = occupiedColor;
            }
            else if (hoveringObject != null)
            {
                targetColor = IsValidObject(hoveringObject) ? hoverColor : invalidColor;
            }
            else
            {
                targetColor = emptyColor;
            }
            
            visualMaterial.color = Color.Lerp(visualMaterial.color, targetColor, 10f * Time.deltaTime);
            
            // Pulse animation when hovering
            if (hoveringObject != null && !isOccupied)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 5f) * 0.1f;
                visualIndicator.transform.localScale = Vector3.one * socketRadius * 2f * pulse;
            }
            else
            {
                visualIndicator.transform.localScale = Vector3.one * socketRadius * 2f;
            }
        }
        
        /// <summary>
        /// Manually place an object in this socket
        /// </summary>
        public void PlaceObject(VRGrabInteractable obj)
        {
            if (isOccupied || obj == null) return;
            
            if (obj.IsGrabbed)
            {
                obj.ForceDrop();
            }
            
            SnapObject(obj);
        }
        
        /// <summary>
        /// Remove the currently socketed object
        /// </summary>
        public VRGrabInteractable RemoveObject()
        {
            if (!isOccupied || socketedObject == null) return null;
            
            VRGrabInteractable removed = socketedObject;
            
            // Re-enable physics
            var rb = removed.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
            }
            
            if (lockOnSnap)
            {
                removed.SetGrabbable(true);
            }
            
            OnObjectRemoved?.Invoke(removed);
            
            socketedObject = null;
            isOccupied = false;
            
            Debug.Log($"[VRSocketInteractor] Removed {removed.gameObject.name} from socket {gameObject.name}");
            
            return removed;
        }
        
        /// <summary>
        /// Check if a specific object is currently in this socket
        /// </summary>
        public bool ContainsObject(VRGrabInteractable obj)
        {
            return isOccupied && socketedObject == obj;
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = isOccupied ? occupiedColor : emptyColor;
            Gizmos.DrawWireSphere(transform.position, socketRadius);
            
            // Draw direction indicator
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.forward * socketRadius * 0.5f);
        }
        
        private void OnDestroy()
        {
            if (visualMaterial != null)
            {
                Destroy(visualMaterial);
            }
        }
    }
}
