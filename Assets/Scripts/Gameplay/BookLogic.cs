using UnityEngine;
using TMPro;
using HackathonVR.Interactions;

namespace HackathonVR.Gameplay
{
    public class BookLogic : MonoBehaviour
    {
        [TextArea(3, 5)]
        public string loreText = "Mon grand-père adorait la légende du télescope. Il disait qu'on pouvait voir la Terre sous un autre œil avec. Cela fait 1 an qu'il a disparu après être allé regarder dedans comme d'habitude...";
        
        private VRGrabInteractable interactable;
        private GameObject tooltipObj;
        private GameObject lorePanelObj;
        
        private void Start()
        {
            interactable = GetComponent<VRGrabInteractable>();
            
            // Create Tooltip "Attraper (A)"
            CreateTooltip();
            
            // Create Lore Panel (Hidden by default)
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
        
        private void CreateTooltip()
        {
            tooltipObj = new GameObject("Tooltip_Attraper");
            tooltipObj.transform.SetParent(transform);
            tooltipObj.transform.localPosition = new Vector3(0, 0.2f, 0); // Above book
            tooltipObj.transform.localScale = Vector3.one * 0.15f; // reduced scale was 0.05
            
            var canvas = tooltipObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = canvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(250, 50); // enhanced
            
            var txt = tooltipObj.AddComponent<TextMeshProUGUI>();
            txt.text = "Attraper (A)"; // Changed from Gâchette to A to match request? actually grabbing is usually Grip/Trigger. 
            // VRGrabber uses Grip/Trigger. The user prompt said: "y a ecrit attraper (A) à coter"
            // So I write "Attraper (A)".
            txt.fontSize = 24;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;
            
            tooltipObj.SetActive(false);
        }
        
        private void CreateLorePanel()
        {
            lorePanelObj = new GameObject("LorePanel");
            lorePanelObj.transform.SetParent(transform);
            lorePanelObj.transform.localPosition = new Vector3(0, 0.2f, 0); // Above book when held
            lorePanelObj.transform.localRotation = Quaternion.Euler(-30, 0, 0); // Tilted up
            lorePanelObj.transform.localScale = Vector3.one * 0.2f;
            
            var canvas = lorePanelObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = canvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400, 300);
            
            // Background
            var bgObj = new GameObject("BG");
            bgObj.transform.SetParent(lorePanelObj.transform, false);
            var bgImg = bgObj.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = new Color(0, 0, 0, 0.8f);
            bgObj.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            bgObj.GetComponent<RectTransform>().anchorMax = Vector2.one;
            
            // Text
            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(lorePanelObj.transform, false);
            var txt = txtObj.AddComponent<TextMeshProUGUI>();
            txt.text = loreText;
            txt.fontSize = 20;
            txt.color = Color.white;
            txt.alignment = TextAlignmentOptions.Center;
            txt.enableWordWrapping = true;
            
            var txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(10, 10);
            txtRt.offsetMax = new Vector2(-10, -10);
            
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
            if (tooltipObj != null) tooltipObj.SetActive(false);
            if (lorePanelObj != null) lorePanelObj.SetActive(true);
        }
        
        private void OnReleased()
        {
            if (lorePanelObj != null) lorePanelObj.SetActive(false);
        }
        
        private void Update()
        {
            // Face camera for tooltip
            if (tooltipObj != null && tooltipObj.activeSelf)
            {
                tooltipObj.transform.LookAt(Camera.main.transform);
                tooltipObj.transform.Rotate(0, 180, 0); // Fix mirrored
            }
        }
    }
}
