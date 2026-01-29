using UnityEngine;

namespace HackathonVR.Core
{
    public class SceneTransitionTrigger : MonoBehaviour
    {
        public enum TriggerType { OnCollision, OnButtonPressed, OnInteract }
        public TriggerType triggerType = TriggerType.OnCollision;
        
        [Header("Specific Scene (Optional)")]
        public bool loadSpecificScene = false;
        public string sceneName;

        private void OnTriggerEnter(Collider other)
        {
            if (triggerType == TriggerType.OnCollision)
            {
                // Check if it's the player (Main Camera or XR Origin)
                if (other.CompareTag("MainCamera") || other.GetComponentInParent<HackathonVR.XRSetup>() != null)
                {
                    ExecuteTransition();
                }
            }
        }

        public void ExecuteTransition()
        {
            if (GameManager.Instance != null)
            {
                if (loadSpecificScene)
                {
                    GameManager.Instance.LoadScene(sceneName);
                }
                else
                {
                    GameManager.Instance.LoadNextScene();
                }
            }
            else
            {
                Debug.LogWarning("[SceneTransitionTrigger] No GameManager found in scene!");
            }
        }
    }
}
