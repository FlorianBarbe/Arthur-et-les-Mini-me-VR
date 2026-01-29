using UnityEngine;

namespace HackathonVR.Interactions
{
    /// <summary>
    /// Utility to quickly setup VR interaction system in a scene.
    /// Add this to your scene and call SetupInteractionSystem() to auto-configure.
    /// </summary>
    public class VRInteractionSetup : MonoBehaviour
    {
        [Header("Auto Setup")]
        [SerializeField] private bool autoSetupOnStart = true;
        
        [Header("Configuration")]
        [SerializeField] private Transform leftControllerTransform;
        [SerializeField] private Transform rightControllerTransform;
        
        [Header("Grab Settings")]
        [SerializeField] private float grabRadius = 0.1f;
        [SerializeField] private VRGrabber.GrabInputType grabInput = VRGrabber.GrabInputType.Grip;
        
        [Header("Pointer Settings")]
        [SerializeField] private bool enablePointers = true;
        [SerializeField] private float pointerMaxDistance = 10f;
        
        private VRGrabber leftGrabber;
        private VRGrabber rightGrabber;
        private VRPointer leftPointer;
        private VRPointer rightPointer;
        
        public VRGrabber LeftGrabber => leftGrabber;
        public VRGrabber RightGrabber => rightGrabber;
        
        private void Start()
        {
            if (autoSetupOnStart)
            {
                SetupInteractionSystem();
            }
        }
        
        /// <summary>
        /// Sets up the complete VR interaction system
        /// </summary>
        public void SetupInteractionSystem()
        {
            Debug.Log("[VRInteractionSetup] Setting up VR Interaction System...");
            
            // Find controllers if not assigned
            FindControllers();
            
            // Setup grabbers
            SetupGrabbers();
            
            // Setup pointers
            if (enablePointers)
            {
                SetupPointers();
            }
            
            Debug.Log("[VRInteractionSetup] VR Interaction System ready!");
        }
        
        private void FindControllers()
        {
            if (leftControllerTransform == null || rightControllerTransform == null)
            {
                // Try to find XR Origin and its controllers
                var xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
                
                if (xrOrigin != null)
                {
                    // Find camera offset
                    Transform cameraOffset = xrOrigin.transform.Find("Camera Offset");
                    if (cameraOffset == null)
                    {
                        cameraOffset = xrOrigin.transform;
                    }
                    
                    // Try common controller names
                    string[] leftNames = { "Left Controller", "LeftHand Controller", "LeftHand", "Left" };
                    string[] rightNames = { "Right Controller", "RightHand Controller", "RightHand", "Right" };
                    
                    foreach (string name in leftNames)
                    {
                        Transform found = cameraOffset.Find(name);
                        if (found != null)
                        {
                            leftControllerTransform = found;
                            Debug.Log($"[VRInteractionSetup] Found left controller: {name}");
                            break;
                        }
                    }
                    
                    foreach (string name in rightNames)
                    {
                        Transform found = cameraOffset.Find(name);
                        if (found != null)
                        {
                            rightControllerTransform = found;
                            Debug.Log($"[VRInteractionSetup] Found right controller: {name}");
                            break;
                        }
                    }
                }
                
                // If still not found, create placeholder transforms
                if (leftControllerTransform == null)
                {
                    Debug.LogWarning("[VRInteractionSetup] Left controller not found. Creating placeholder.");
                    var leftObj = new GameObject("LeftController_Placeholder");
                    leftObj.transform.SetParent(Camera.main?.transform.parent ?? transform);
                    leftObj.transform.localPosition = new Vector3(-0.3f, 0f, 0.3f);
                    leftControllerTransform = leftObj.transform;
                }
                
                if (rightControllerTransform == null)
                {
                    Debug.LogWarning("[VRInteractionSetup] Right controller not found. Creating placeholder.");
                    var rightObj = new GameObject("RightController_Placeholder");
                    rightObj.transform.SetParent(Camera.main?.transform.parent ?? transform);
                    rightObj.transform.localPosition = new Vector3(0.3f, 0f, 0.3f);
                    rightControllerTransform = rightObj.transform;
                }
            }
        }
        
        private void SetupGrabbers()
        {
            // Left hand grabber
            leftGrabber = leftControllerTransform.GetComponent<VRGrabber>();
            if (leftGrabber == null)
            {
                leftGrabber = leftControllerTransform.gameObject.AddComponent<VRGrabber>();
            }
            
            // Right hand grabber
            rightGrabber = rightControllerTransform.GetComponent<VRGrabber>();
            if (rightGrabber == null)
            {
                rightGrabber = rightControllerTransform.gameObject.AddComponent<VRGrabber>();
            }
            
            Debug.Log("[VRInteractionSetup] Grabbers configured on both controllers");
        }
        
        private void SetupPointers()
        {
            // Left hand pointer
            leftPointer = leftControllerTransform.GetComponent<VRPointer>();
            if (leftPointer == null)
            {
                leftPointer = leftControllerTransform.gameObject.AddComponent<VRPointer>();
            }
            
            // Right hand pointer
            rightPointer = rightControllerTransform.GetComponent<VRPointer>();
            if (rightPointer == null)
            {
                rightPointer = rightControllerTransform.gameObject.AddComponent<VRPointer>();
            }
            
            Debug.Log("[VRInteractionSetup] Pointers configured on both controllers");
        }
        
        /// <summary>
        /// Create a grabbable cube for testing
        /// </summary>
        [ContextMenu("Create Test Grabbable Cube")]
        public void CreateTestGrabbableCube()
        {
            CreateGrabbableObject(PrimitiveType.Cube, new Vector3(0, 1, 1), Color.red, "TestCube");
        }
        
        /// <summary>
        /// Create a grabbable sphere for testing
        /// </summary>
        [ContextMenu("Create Test Grabbable Sphere")]
        public void CreateTestGrabbableSphere()
        {
            CreateGrabbableObject(PrimitiveType.Sphere, new Vector3(0.5f, 1, 1), Color.blue, "TestSphere");
        }
        
        /// <summary>
        /// Create a socket for testing
        /// </summary>
        [ContextMenu("Create Test Socket")]
        public void CreateTestSocket()
        {
            GameObject socket = new GameObject("TestSocket");
            socket.transform.position = new Vector3(0, 1, 2);
            socket.AddComponent<VRSocketInteractor>();
            
            Debug.Log("[VRInteractionSetup] Created test socket");
        }
        
        /// <summary>
        /// Create a grabbable primitive object
        /// </summary>
        public GameObject CreateGrabbableObject(PrimitiveType type, Vector3 position, Color color, string name = null)
        {
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.name = name ?? $"Grabbable_{type}";
            obj.transform.position = position;
            obj.transform.localScale = Vector3.one * 0.2f;
            
            // Add rigidbody
            var rb = obj.AddComponent<Rigidbody>();
            rb.mass = 0.5f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            
            // Add grabbable component
            obj.AddComponent<VRGrabInteractable>();
            
            // Set color
            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = color;
            }
            
            Debug.Log($"[VRInteractionSetup] Created grabbable object: {obj.name}");
            
            return obj;
        }
        
        /// <summary>
        /// Make an existing object grabbable
        /// </summary>
        public static VRGrabInteractable MakeGrabbable(GameObject obj)
        {
            if (obj == null) return null;
            
            // Ensure it has a collider
            if (obj.GetComponent<Collider>() == null)
            {
                obj.AddComponent<BoxCollider>();
            }
            
            // Add rigidbody if missing
            var rb = obj.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = obj.AddComponent<Rigidbody>();
                rb.mass = 0.5f;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
            }
            
            // Add grabbable component
            var grabbable = obj.GetComponent<VRGrabInteractable>();
            if (grabbable == null)
            {
                grabbable = obj.AddComponent<VRGrabInteractable>();
            }
            
            return grabbable;
        }
    }
}
