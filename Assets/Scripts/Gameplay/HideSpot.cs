using UnityEngine;

namespace HackathonVR.Gameplay
{
    public class HideSpot : MonoBehaviour
    {
        public string enterMessage = "Vous êtes caché !";
        public string exitMessage = "Vous êtes visible !";
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") || other.name.Contains("XR Origin") || other.name.Contains("VR Setup"))
            {
                BeeChase.SetPlayerHidden(true);
                Debug.Log("[HideSpot] Player is hidden");
                
                if (Core.DialogueManager.Instance != null)
                    Core.DialogueManager.Instance.ShowMessage("Système", enterMessage, 2f);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") || other.name.Contains("XR Origin") || other.name.Contains("VR Setup"))
            {
                BeeChase.SetPlayerHidden(false);
                Debug.Log("[HideSpot] Player is visible");
                
                if (Core.DialogueManager.Instance != null)
                    Core.DialogueManager.Instance.ShowMessage("Système", exitMessage, 2f);
            }
        }
    }
}
