using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace HackathonVR.Core
{
    public class SceneSpawnManager : MonoBehaviour
    {
        public static SceneSpawnManager Instance { get; private set; }
        
        [System.Serializable]
        public class SceneSpawnData
        {
            public string sceneName;
            public Vector3 position;
            public Vector3 rotation; // Euler angles
            public float scale = 1f;
            public string introDialogueSpeaker;
            public string introDialogueText;
        }
        
        public List<SceneSpawnData> spawnDataList = new List<SceneSpawnData>();
        
        // Default spawns based on user requests/screenshots
        private void Reset()
        {
            spawnDataList = new List<SceneSpawnData>
            {
                new SceneSpawnData 
                { 
                    sceneName = "1", 
                    position = new Vector3(4.1f, 0.1f, 0.25f), 
                    rotation = new Vector3(0, 90, 0),
                    scale = 1f,
                    introDialogueSpeaker = "Narrateur",
                    introDialogueText = "Bienvenue dans le jardin. C'est ici que tout commence."
                },
                new SceneSpawnData 
                { 
                    sceneName = "2", 
                    position = new Vector3(0f, 3.5f, 0f), 
                    rotation = Vector3.zero,
                    scale = 0.05f, // Tiny scale
                    introDialogueSpeaker = "Narrateur",
                    introDialogueText = "Vous avez rétréci ! Attention aux insectes."
                },
                new SceneSpawnData 
                { 
                    sceneName = "3", 
                    position = new Vector3(5.43f, 0.07f, 7.33f), 
                    rotation = Vector3.zero,
                    scale = 0.05f,
                    introDialogueSpeaker = "Narrateur",
                    introDialogueText = "Il fait sombre ici. Utilisez votre lampe torche."
                },
                new SceneSpawnData 
                { 
                    sceneName = "4", 
                    position = Vector3.zero, 
                    rotation = Vector3.zero,
                    scale = 0.05f,
                    introDialogueSpeaker = "Narrateur",
                    introDialogueText = "La fin du voyage approche."
                }
            };
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                if (spawnDataList.Count == 0) Reset();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void ApplySpawnForScene(string sceneName)
        {
            StartCoroutine(ApplySpawnAfterDelay(sceneName));
        }

        private IEnumerator ApplySpawnAfterDelay(string sceneName)
        {
            yield return null; // Wait 1 frame
            
            var data = spawnDataList.Find(x => x.sceneName == sceneName || x.sceneName.EndsWith("/" + sceneName));
            
            if (data != null)
            {
                GameObject rig = GameObject.Find("XR Origin (XR Rig)");
                if (rig == null) rig = GameObject.Find("VR Setup");
                
                if (rig != null)
                {
                    rig.transform.position = data.position;
                    rig.transform.rotation = Quaternion.Euler(data.rotation);
                    rig.transform.localScale = Vector3.one * data.scale;
                    
                    Debug.Log($"[SceneSpawnManager] Spawned at {data.position} with scale {data.scale}");
                    
                    // Show dialogue
                    if (!string.IsNullOrEmpty(data.introDialogueText) && DialogueManager.Instance != null)
                    {
                        DialogueManager.Instance.ShowMessage(data.introDialogueSpeaker, data.introDialogueText);
                    }
                }
            }
        }
    }
}
