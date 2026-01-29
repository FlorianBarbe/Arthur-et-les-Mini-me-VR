using UnityEngine;
using UnityEngine.Events;

namespace HackathonVR.Interactions
{
    /// <summary>
    /// A simple button that can be pressed in VR by touching or poking it.
    /// Great for control panels, puzzles, and interactive elements.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class VRButton : MonoBehaviour, IPointerHoverable
    {
        [Header("Button Settings")]
        [SerializeField] private float pressDepth = 0.02f;
        [SerializeField] private float returnSpeed = 5f;
        [SerializeField] private float cooldown = 0.5f;
        [SerializeField] private bool stayPressed = false;
        
        [Header("Visual Settings")]
        [SerializeField] private Color normalColor = Color.gray;
        [SerializeField] private Color hoverColor = Color.white;
        [SerializeField] private Color pressedColor = Color.green;
        
        [Header("Audio")]
        [SerializeField] private AudioClip pressSound;
        [SerializeField] private AudioClip releaseSound;
        [SerializeField] private float soundVolume = 0.5f;
        
        [Header("Haptic Feedback")]
        [SerializeField] private float hapticAmplitude = 0.5f;
        [SerializeField] private float hapticDuration = 0.1f;
        
        [Header("Events")]
        public UnityEvent OnButtonPressed;
        public UnityEvent OnButtonReleased;
        
        // State
        private bool isPressed = false;
        private bool isHovered = false;
        private float lastPressTime = 0f;
        private Vector3 originalLocalPosition;
        private Vector3 pressedLocalPosition;
        
        // Components
        private Renderer buttonRenderer;
        private Material buttonMaterial;
        private AudioSource audioSource;
        private VRGrabber currentPresser;
        
        public bool IsPressed => isPressed;
        
        private void Awake()
        {
            originalLocalPosition = transform.localPosition;
            pressedLocalPosition = originalLocalPosition - transform.up * pressDepth;
            
            // Setup renderer
            buttonRenderer = GetComponent<Renderer>();
            if (buttonRenderer != null)
            {
                buttonMaterial = new Material(buttonRenderer.material);
                buttonRenderer.material = buttonMaterial;
                buttonMaterial.color = normalColor;
            }
            
            // Setup audio
            if (pressSound != null || releaseSound != null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f; // 3D sound
            }
        }
        
        private void Update()
        {
            // Animate button return
            if (!isPressed || (isPressed && !stayPressed && Time.time - lastPressTime > 0.2f))
            {
                if (!stayPressed || !isPressed)
                {
                    transform.localPosition = Vector3.Lerp(
                        transform.localPosition, 
                        originalLocalPosition, 
                        returnSpeed * Time.deltaTime
                    );
                }
            }
            
            UpdateColor();
        }
        
        private void UpdateColor()
        {
            if (buttonMaterial == null) return;
            
            Color targetColor;
            if (isPressed)
            {
                targetColor = pressedColor;
            }
            else if (isHovered)
            {
                targetColor = hoverColor;
            }
            else
            {
                targetColor = normalColor;
            }
            
            buttonMaterial.color = Color.Lerp(buttonMaterial.color, targetColor, 10f * Time.deltaTime);
        }
        
        private void OnTriggerEnter(Collider other)
        {
            // Check if it's a controller/hand
            VRGrabber grabber = other.GetComponentInParent<VRGrabber>();
            if (grabber != null)
            {
                Press(grabber);
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            VRGrabber grabber = other.GetComponentInParent<VRGrabber>();
            if (grabber != null && !stayPressed)
            {
                Release();
            }
        }
        
        /// <summary>
        /// Press the button programmatically
        /// </summary>
        public void Press(VRGrabber presser = null)
        {
            if (isPressed && !stayPressed) return;
            if (Time.time - lastPressTime < cooldown) return;
            
            isPressed = true;
            lastPressTime = Time.time;
            currentPresser = presser;
            
            // Animate to pressed position
            transform.localPosition = pressedLocalPosition;
            
            // Play sound
            if (audioSource != null && pressSound != null)
            {
                audioSource.PlayOneShot(pressSound, soundVolume);
            }
            
            // Haptic feedback
            if (presser != null)
            {
                presser.TriggerHaptic(hapticAmplitude, hapticDuration);
            }
            
            OnButtonPressed?.Invoke();
            Debug.Log($"[VRButton] {gameObject.name} pressed");
        }
        
        /// <summary>
        /// Release the button
        /// </summary>
        public void Release()
        {
            if (!isPressed) return;
            
            isPressed = false;
            
            // Play sound
            if (audioSource != null && releaseSound != null)
            {
                audioSource.PlayOneShot(releaseSound, soundVolume);
            }
            
            OnButtonReleased?.Invoke();
            Debug.Log($"[VRButton] {gameObject.name} released");
        }
        
        /// <summary>
        /// Toggle the button state (for stayPressed mode)
        /// </summary>
        public void Toggle()
        {
            if (isPressed)
            {
                Release();
            }
            else
            {
                Press();
            }
        }
        
        // IPointerHoverable implementation
        public void OnPointerEnter(VRPointer pointer)
        {
            isHovered = true;
        }
        
        public void OnPointerExit(VRPointer pointer)
        {
            isHovered = false;
        }
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Vector3 pressPos = transform.position - transform.up * pressDepth;
            Gizmos.DrawLine(transform.position, pressPos);
            Gizmos.DrawWireSphere(pressPos, 0.01f);
        }
        
        private void OnDestroy()
        {
            if (buttonMaterial != null)
            {
                Destroy(buttonMaterial);
            }
        }
    }
}
