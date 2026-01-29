using UnityEngine;
using TMPro;
using HackathonVR.Interactions;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR;

namespace HackathonVR.Gameplay
{
    [RequireComponent(typeof(VRGrabInteractable))]
    public class BookLogic : MonoBehaviour
    {
        [Header("Content")]
        [TextArea(3, 10)]
        public string loreText = "Dernières notes de grand-père, il y a exactement 1 an, le jour de sa disparition, alors qu'il était parti jeter un coup d'oeil dans son télescope : \"J'adorais la légende du télescope, c'était toujours un plaisir de rêver à ce que je pourrais voir à travers. Le vieux Chaman me disait toujours que les plus belles surprises se trouvaient là où on s'y attendait le moins. En suivant sa logique je devrais prendre un télescope pour regarder le sol ... Ce ne sont que des dictons après tout. Ma foi pourquoi ne pas essayer ahah!\"";
        
        [Tooltip("If empty, loreText will be split automatically.")]
        [TextArea(3, 5)]
        public List<string> pages = new List<string>();

        [Header("Settings")]
        
        public float typewriteSpeed = 0.02f;
        public Color panelColor = new Color(0, 0, 0, 0.9f);
        public AudioClip pageFlipSound;
        public ParticleSystem openParticle;

        [Header("Events")]
        public UnityEvent onBookClosed;

        private VRGrabInteractable interactable;
        private AudioSource audioSource;
        private GameObject tooltipObj;
        private GameObject lorePanelObj;
        private TextMeshProUGUI loreTextUI;
        private TextMeshProUGUI helperTextUI;
        private bool boundToStoryManager = false;
        
        private bool isReading = false;
        private int currentPage = 0;
        private bool isTyping = false;
        private Coroutine typewriteCoroutine;
        
        // Input Debounce
        private bool wasPrimaryPressed = false;
        private bool wasSecondaryPressed = false;
        private float inputCooldown = 0.2f;
        private float lastInputTime = 0f;

        // Floating animation
        private Vector3 startPos;
        private float floatSpeed = 1f;
        private float floatAmplitude = 0.1f;

        private void BindStoryManager()
        {
            if (boundToStoryManager) return;

            if (StoryManager.Instance != null)
            {
                onBookClosed.AddListener(StoryManager.Instance.OnBookFinished);
                boundToStoryManager = true;
                Debug.Log("[BookLogic] Bound to StoryManager.OnBookFinished");
            }
        }

        private void Start()
        {
            BindStoryManager();
            interactable = GetComponent<VRGrabInteractable>();
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            
            startPos = transform.position;
            
            SetupPages();
            
            CreateTooltip();
            CreateLorePanel();
            
            // Subscribe to events
            if (interactable != null)
            {
                interactable.OnHoverEnter.AddListener(OnHoverEnter);
                interactable.OnHoverExit.AddListener(OnHoverExit);
                interactable.OnGrabbed.AddListener(OnGrabbed);
                interactable.OnReleased.AddListener(OnReleased);
            }
        }
        
        private void SetupPages()
        {
            // If no manual pages, try to split loreText nicely
            if ((pages == null || pages.Count == 0) && !string.IsNullOrEmpty(loreText))
            {
                if (pages == null) pages = new List<string>();

                // Custom split for the default text to ensure good paging
                if (loreText.StartsWith("Dernières notes de grand-père"))
                {
                    pages.Add("Dernières notes de grand-père, il y a exactement 1 an, le jour de sa disparition, alors qu'il était parti jeter un coup d'oeil dans son télescope :");
                    pages.Add("\"J'adorais la légende du télescope, c'était toujours un plaisir de rêver à ce que je pourrais voir à travers. Le vieux Chaman me disait toujours que les plus belles surprises se trouvaient là où on s'y attendait le moins.");
                    pages.Add("En suivant sa logique je devrais prendre un télescope pour regarder le sol ... Ce ne sont que des dictons après tout. Ma foi pourquoi ne pas essayer ahah!\"");
                }
                else
                {
                    // Fallback: Just add full text
                    pages.Add(loreText);
                }
            }
        }

        private void Update()
        {
            // Floating Animation (only when not held)
            if (interactable != null && !interactable.IsGrabbed)
            {
                float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            }

            // Face camera for tooltip
            if (interactable != null && !interactable.IsGrabbed && tooltipObj != null && tooltipObj.activeSelf)
            {
                tooltipObj.transform.LookAt(Camera.main.transform);
                tooltipObj.transform.Rotate(0, 180, 0); // Fix mirrored
            }
            
            // Input Handling
            if (isReading && interactable.IsGrabbed && interactable.CurrentGrabber != null)
            {
                HandleInput(interactable.CurrentGrabber);
            }
        }
        
        private void HandleInput(VRGrabber grabber)
        {
            if (Time.time - lastInputTime < inputCooldown) return;

            bool primaryPressed = grabber.IsButtonPressed(CommonUsages.primaryButton);     // A / X
            bool secondaryPressed = grabber.IsButtonPressed(CommonUsages.secondaryButton); // B / Y

            // Primary Button (Next)
            if (primaryPressed && !wasPrimaryPressed)
            {
                lastInputTime = Time.time;
                if (isTyping)
                {
                    // Skip typing
                    StopCoroutine(typewriteCoroutine);
                    loreTextUI.text = pages[currentPage];
                    isTyping = false;
                }
                else
                {
                    NextPage();
                }
            }
            
            // Secondary Button (Prev)
            if (secondaryPressed && !wasSecondaryPressed)
            {
                lastInputTime = Time.time;
                if (!isTyping)
                {
                    PrevPage();
                }
            }

            wasPrimaryPressed = primaryPressed;
            wasSecondaryPressed = secondaryPressed;
        }

        private void NextPage()
        {
            if (currentPage < pages.Count - 1)
            {
                currentPage++;
                ShowPage(currentPage);
            }
            else
            {
                // Finished
                FinishBook();
            }
        }

        private void PrevPage()
        {
            if (currentPage > 0)
            {
                currentPage--;
                ShowPage(currentPage);
            }
        }

        private void FinishBook()
        {
            Debug.Log("[BookLogic] Book finished.");
            StopReading();
            
            onBookClosed?.Invoke();
            
            // Helpful auto-call
            if (StoryManager.Instance != null && onBookClosed.GetPersistentEventCount() == 0)
            {
                StoryManager.Instance.OnBookFinished();
            }
            
            // Reset page for next time
            currentPage = 0; 
        }

        private void ShowPage(int index)
        {
            if (pages == null || pages.Count == 0) return;
            
            if (typewriteCoroutine != null) StopCoroutine(typewriteCoroutine);
            
            PlaySound();
            typewriteCoroutine = StartCoroutine(TypewriteText(pages[index]));
            
            // Update helper text
            if (index < pages.Count - 1)
                helperTextUI.text = "(A) Suivant  " + (index > 0 ? "(B) Retour" : "");
            else
                helperTextUI.text = "(A) Fermer  (B) Retour";
                
            // Update page number
             helperTextUI.text += $"\nPage {index + 1}/{pages.Count}";
        }
        
        private IEnumerator TypewriteText(string text)
        {
            isTyping = true;
            loreTextUI.text = "";
            foreach (char c in text)
            {
                loreTextUI.text += c;
                yield return new WaitForSeconds(typewriteSpeed);
            }
            isTyping = false;
        }

        private void CreateTooltip()
        {
            tooltipObj = new GameObject("Tooltip_Lire");
            tooltipObj.transform.SetParent(transform);
            tooltipObj.transform.localPosition = new Vector3(0, 0.2f, 0); 
            tooltipObj.transform.localScale = Vector3.one * 0.15f; 
            
            var canvas = tooltipObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = canvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300, 50); 
            
            var txt = tooltipObj.AddComponent<TextMeshProUGUI>();
            txt.text = "Lire (Grab)"; 
            txt.fontSize = 24;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;
            
            tooltipObj.SetActive(false);
        }
        
        private void CreateLorePanel()
        {
            lorePanelObj = new GameObject("LorePanel");
            lorePanelObj.transform.SetParent(transform);
            lorePanelObj.transform.localPosition = new Vector3(0, 0.35f, 0.15f); 
            lorePanelObj.transform.localRotation = Quaternion.Euler(-30, 0, 0); 
            lorePanelObj.transform.localScale = Vector3.one * 0.002f; // Adjusted scale for UI consistency with TMP default
            
            var canvas = lorePanelObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = canvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(600, 400); // Larger canvas for better res
            lorePanelObj.transform.localScale = Vector3.one * 0.001f; // Scaled down to match world size units

            // Background
            var bgObj = new GameObject("BG");
            bgObj.transform.SetParent(lorePanelObj.transform, false);
            var bgImg = bgObj.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = panelColor;
            bgObj.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            bgObj.GetComponent<RectTransform>().anchorMax = Vector2.one;
            
            // Text Lore
            var txtObj = new GameObject("TextLore");
            txtObj.transform.SetParent(lorePanelObj.transform, false);
            loreTextUI = txtObj.AddComponent<TextMeshProUGUI>();
            loreTextUI.fontSize = 36;
            loreTextUI.color = Color.white;
            loreTextUI.alignment = TextAlignmentOptions.TopLeft;
            loreTextUI.enableWordWrapping = true;
            
            var txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(30, 80); // Bottom padding for helper text
            txtRt.offsetMax = new Vector2(-30, -30); // Top/Right padding
            
            // Text Helper
            var helpObj = new GameObject("TextHelper");
            helpObj.transform.SetParent(lorePanelObj.transform, false);
            helperTextUI = helpObj.AddComponent<TextMeshProUGUI>();
            helperTextUI.fontSize = 24;
            helperTextUI.color = Color.yellow;
            helperTextUI.alignment = TextAlignmentOptions.Bottom;
            
            var helpRt = helpObj.GetComponent<RectTransform>();
            helpRt.anchorMin = new Vector2(0, 0);
            helpRt.anchorMax = new Vector2(1, 0.2f); // Bottom 20%
            helpRt.offsetMin = Vector2.zero;
            helpRt.offsetMax = Vector2.zero;
            
            lorePanelObj.SetActive(false);
        }
        
        private void OnHoverEnter()
        {
            if (tooltipObj != null && !interactable.IsGrabbed)
                tooltipObj.SetActive(true);
        }
        
        private void OnHoverExit()
        {
            if (tooltipObj != null)
                tooltipObj.SetActive(false);
        }
        
        private void OnGrabbed()
        {
            InitReading();
        }
        
        private void OnReleased()
        {
            // Just hide it, don't finish
            StopReading();
        }

        private void InitReading()
        {
            if (tooltipObj != null) tooltipObj.SetActive(false);
            if (lorePanelObj != null) lorePanelObj.SetActive(true);
            
            isReading = true;
            if (openParticle != null) openParticle.Play();
            
            ShowPage(currentPage);
        }

        private void StopReading()
        {
            if (lorePanelObj != null) lorePanelObj.SetActive(false);
            if (typewriteCoroutine != null) StopCoroutine(typewriteCoroutine);
            isReading = false;
        }
        
        private void PlaySound()
        {
            if (audioSource != null && pageFlipSound != null)
            {
                audioSource.PlayOneShot(pageFlipSound);
            }
        }
    }
}
