using UnityEngine;
using UnityEngine.SceneManagement;

namespace HackathonVR.Gameplay
{
    /// <summary>
    /// Debug object: Grab this to skip to the next scene.
    /// Place this in each scene as a quick way to test scene transitions.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DebugNextScene : MonoBehaviour
    {
        [Header("Visual")]
        public Color glowColor = Color.cyan;
        
        private Renderer rend;
        private bool hasBeenGrabbed = false;

        private void Start()
        {
            // Make it glow so it's visible
            rend = GetComponent<Renderer>();
            if (rend != null)
            {
                var mat = new Material(Shader.Find("Standard"));
                mat.color = glowColor;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", glowColor * 0.5f);
                rend.material = mat;
            }

            // Add VRGrabInteractable if not present
            var grab = GetComponent<HackathonVR.Interactions.VRGrabInteractable>();
            if (grab == null)
            {
                grab = gameObject.AddComponent<HackathonVR.Interactions.VRGrabInteractable>();
            }

            // Subscribe to grab event
            if (grab.OnGrabbed == null)
                grab.OnGrabbed = new UnityEngine.Events.UnityEvent();
            
            grab.OnGrabbed.AddListener(OnGrabbed);
        }

        private void OnGrabbed()
        {
            if (hasBeenGrabbed) return;
            hasBeenGrabbed = true;

            Debug.Log("[DebugNextScene] Skipping to next scene!");

            var gm = HackathonVR.Core.GameManager.Instance;
            if (gm != null)
            {
                gm.LoadNextScene();
            }
            else
            {
                // Fallback: just load next scene index
                int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
                if (nextIndex < SceneManager.sceneCountInBuildSettings)
                {
                    SceneManager.LoadScene(nextIndex);
                }
                else
                {
                    Debug.Log("[DebugNextScene] Already at last scene!");
                }
            }
        }
    }
}
