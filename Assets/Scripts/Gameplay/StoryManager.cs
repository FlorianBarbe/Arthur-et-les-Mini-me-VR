using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using HackathonVR.Core;

namespace HackathonVR.Gameplay
{
    public class StoryManager : MonoBehaviour
    {
        public static StoryManager Instance;

        [Header("References")]
        public AudioSource audioSource;
        public AudioClip sfxFlash;
        public AudioClip sfxScream;
        
        [Header("Narjisse")]
        public GameObject narjisseObject;
        public SimpleDialogue narjisseDialogue; // Reuse the dialogue component?
        
        [Header("Telescope")]
        public Transform telescopeLookPoint;
        public float lookTriggerDistance = 0.5f;
        public float lookDuration = 2.0f;
        
        [Header("UI")]
        public GameObject flashPanel; // White screen UI

        private bool waitingForPlayerLook = false;
        private float currentLookTime = 0f;
        private Transform vrCamera;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            vrCamera = Camera.main.transform;
            if (flashPanel != null) flashPanel.SetActive(false);
        }

        // Called when Book is Closed/Released
        public void OnBookFinished()
        {
            StartCoroutine(Sequence_NarjisseEvent());
        }

        private IEnumerator Sequence_NarjisseEvent()
        {
            Debug.Log("[StoryManager] Book finished. Sequence START.");

            // 0. Auto-find objects if missing (User helpers)
            if (narjisseObject == null) narjisseObject = GameObject.Find("anim_narjisse7204ds");
            if (telescopeLookPoint == null)
            {
                var t = GameObject.Find("telescope");
                if (t != null) telescopeLookPoint = t.transform;
            }

            // 1. Move Narjisse towards telescope
            if (narjisseObject != null && telescopeLookPoint != null)
            {
                Debug.Log("[StoryManager] Moving Narjisse...");
                Vector3 startPos = narjisseObject.transform.position;
                // Target: slightly in front of telescope, same height
                Vector3 targetPos = telescopeLookPoint.position - telescopeLookPoint.forward * 0.8f; 
                targetPos.y = startPos.y; 

                float moveSpeed = 1.0f; // Slow walk
                float dist = Vector3.Distance(startPos, targetPos);
                float duration = dist / moveSpeed;
                
                float t = 0f;
                while (t < duration)
                {
                    t += Time.deltaTime;
                    narjisseObject.transform.position = Vector3.Lerp(startPos, targetPos, t / duration);
                    
                    // Look at telescope
                    narjisseObject.transform.LookAt(new Vector3(telescopeLookPoint.position.x, narjisseObject.transform.position.y, telescopeLookPoint.position.z));
                    
                    yield return null;
                }
            }
            else
            {
                Debug.LogWarning("[StoryManager] Missing Narjisse or Telescope object!");
                yield return new WaitForSeconds(2f);
            }

            // 2. Dialogue triggers AFTER movement
            Debug.Log("[StoryManager] Narjisse Arrived. Speaking...");
            yield return new WaitForSeconds(0.5f);

            string text = "Allez, regardons dedans...";
            if (narjisseDialogue != null)
            {
                narjisseDialogue.SetDialogue(new System.Collections.Generic.List<string>() { text });
            }
            
            // Wait for text to be read (arbitrary delay)
            yield return new WaitForSeconds(4.0f);

            // 3. Scream & Flash & Disappear
            Debug.Log("[StoryManager] SCREAM!");
            if (audioSource != null && sfxScream != null) audioSource.PlayOneShot(sfxScream);
            
            if (narjisseObject != null) narjisseObject.SetActive(false);
            
            yield return FlashScreen(0.5f); 
            
            // 4. Transition
            Debug.Log("[StoryManager] Transitioning to Scene 2...");
            UnityEngine.SceneManagement.SceneManager.LoadScene("2");
        }

        // Removed CheckPlayerLook logic as User asked for "après gros flash... et on spawn dans la zone 2" directly after she screams/disappears?
        // "Elle s'y déplace... regarde... flash ahhhh... et on spawn".
        // Sounds like automatic transition, not waiting for player to look?
        // "Regardons dans le telescope etc et APRES gros flash... et on spawn".
        // So no player input required on telescope?
        // I will comment out the player look requirement for this sequence.

        private IEnumerator FlashScreen(float duration)
        {
            if (flashPanel != null)
            {
                flashPanel.SetActive(true);
                yield return new WaitForSeconds(duration);
                flashPanel.SetActive(false); // Or keep active if scene loads
            }
        }
    }
}
