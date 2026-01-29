using UnityEngine;

namespace HackathonVR.Interactions
{
    public class VRClimbable : MonoBehaviour
    {
        // Tag script to identify objects the player can climb on
        [Header("Climbing Settings")]
        public float gripStrength = 1.0f;
        
        private void Start()
        {
            // Ensure there is a collider for the hands to touch
            if (GetComponent<Collider>() == null)
            {
                Debug.LogWarning($"[VRClimbable] {name} needs a Collider to be climbable!");
            }
        }
    }
}
