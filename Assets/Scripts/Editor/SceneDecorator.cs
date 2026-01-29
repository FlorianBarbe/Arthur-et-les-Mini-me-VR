using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using HackathonVR.Core;
using HackathonVR.Gameplay;

namespace HackathonVR.Editor
{
    public class SceneDecorator : MonoBehaviour
    {
        [MenuItem("Hackathon/Setup Scene 1 (Jardin - Intro)", false, 1)]
        public static void SetupScene1()
        {
            SetupScene("1", "Assets/Scenes/1.unity");
            SpawnBook();
        }

        [MenuItem("Hackathon/Setup Scene 2 (Réduit)", false, 2)]
        public static void SetupScene2()
        {
            SetupScene("2", "Assets/Scenes/2.unity");
        }

        [MenuItem("Hackathon/Setup Scene 3 (Nuit - Lampe)", false, 3)]
        public static void SetupScene3()
        {
            SetupScene("3", "Assets/Scenes/3.unity");
            SpawnBees();
        }

        [MenuItem("Hackathon/Setup Scene 4", false, 4)]
        public static void SetupScene4()
        {
            SetupScene("4", "Assets/Scenes/4.unity");
        }
        
        [MenuItem("Hackathon/Setup/Force Refresh VR", false, 50)]
        public static void RefreshVR()
        {
            GameObject setup = GameObject.Find("VR Setup");
            if (setup != null) DestroyImmediate(setup);
            
            var go = new GameObject("VR Setup");
            var script = go.AddComponent<XRSetup>();
            script.SetupXR();
        }

        private static void SetupScene(string sceneShortName, string scenePath)
        {
            if (EditorSceneManager.GetActiveScene().path != scenePath)
            {
                if (EditorUtility.DisplayDialog("Open Scene", $"Open Scene {sceneShortName}?", "Yes", "No"))
                {
                    EditorSceneManager.OpenScene(scenePath);
                }
            }

            // 1. Create Managers if needed
            EnsureManager<GameManager>("GameManager");
            EnsureManager<SceneSpawnManager>("SceneSpawnManager");
            EnsureManager<DialogueManager>("DialogueManager");
            EnsureManager<MusicManager>("MusicManager");

            // 2. Setup VR
            GameObject vrSetup = GameObject.Find("VR Setup");
            if (vrSetup == null)
            {
                vrSetup = new GameObject("VR Setup");
                var xrSetup = vrSetup.AddComponent<XRSetup>();
                xrSetup.SetupXR();
            }

            // 3. Apply Spawn
            GameObject spawnMgrGO = GameObject.Find("SceneSpawnManager");
            if (spawnMgrGO != null)
            {
                var mgr = spawnMgrGO.GetComponent<SceneSpawnManager>();
                mgr.ApplySpawnForScene(sceneShortName);
            }
            
            Debug.Log($"[SceneDecorator] Setup complete for Scene {sceneShortName}");
        }

        private static void SpawnBees()
        {
            if (GameObject.Find("Bees_Parent") != null) return;

            GameObject beesParent = new GameObject("Bees_Parent");
            
            // Spawn 3 bees
            for (int i = 0; i < 3; i++)
            {
                if (System.Type.GetType("HackathonVR.Gameplay.BeeChase") == null) continue;

                GameObject bee = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bee.name = $"Bee_{i}";
                bee.transform.SetParent(beesParent.transform);
                // Position randomly around Scene 3 spawn
                bee.transform.position = new Vector3(5.4f + Random.Range(-5, 5), 1f, 7.3f + Random.Range(5, 15));
                bee.transform.localScale = Vector3.one * 0.3f;
                
                var rend = bee.GetComponent<Renderer>();
                if (rend != null) rend.material.color = Color.yellow;
                
                // Add NavMeshAgent
                var agent = bee.AddComponent<UnityEngine.AI.NavMeshAgent>();
                agent.speed = 3.5f;
                agent.radius = 0.2f;
                agent.height = 0.5f;
                
                // Add BeeChase
                bee.AddComponent<BeeChase>();
            }
            
            Debug.Log("[SceneDecorator] Spawned Bees for Scene 3");
            
            // Create HideSpot
            GameObject hideSpot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hideSpot.name = "HideSpot";
            hideSpot.transform.SetParent(beesParent.transform);
            hideSpot.transform.position = new Vector3(5.4f, 0.5f, 15f); // Somewhere ahead
            hideSpot.transform.localScale = new Vector3(2, 2, 2);
            hideSpot.GetComponent<Collider>().isTrigger = true;
            
            var hideMat = hideSpot.GetComponent<Renderer>().material;
            hideMat.color = new Color(0, 1, 0, 0.3f);
            SetTransparent(hideMat);
            
            if (System.Type.GetType("HackathonVR.Gameplay.HideSpot") != null)
            {
                hideSpot.AddComponent<HideSpot>();
            }
        }

        private static void SpawnBook()
        {
            if (GameObject.Find("NarrativeBook") != null) return;
            
            // Create Book visual (Red flattened cube)
            GameObject book = GameObject.CreatePrimitive(PrimitiveType.Cube);
            book.name = "NarrativeBook";
            book.transform.position = new Vector3(4.1f, 0.05f, 1.0f); // In front of user (User is at 4.1, 0.1, 0.25)
            book.transform.localScale = new Vector3(0.3f, 0.05f, 0.4f);
            
            var rend = book.GetComponent<Renderer>();
            if (rend != null) rend.material.color = new Color(0.6f, 0.1f, 0.1f); // Red cover
            
            // Physics
            var rb = book.AddComponent<Rigidbody>();
            rb.mass = 1f;
            
            // Logic
            if (System.Type.GetType("HackathonVR.Interactions.VRGrabInteractable") != null)
            {
                var grab = book.AddComponent<HackathonVR.Interactions.VRGrabInteractable>();
                // Configure highlight
            }
            
            if (System.Type.GetType("HackathonVR.Gameplay.BookLogic") != null)
            {
                book.AddComponent<BookLogic>();
            }
            
            // Wire to Dialogue
            var dialogue = FindFirstObjectByType<SimpleDialogue>();
            if (dialogue != null)
            {
                dialogue.objectToActivateOnFinish = book;
                book.SetActive(false); // Hide until dialogue ends
                Debug.Log("[SceneDecorator] Wired Book to Dialogue end event.");
            }
            else
            {
                Debug.LogWarning("[SceneDecorator] Could not find SimpleDialogue to wire Book event!");
            }
        }

        private static void EnsureManager<T>(string name) where T : Component
        {
            if (GameObject.Find(name) == null)
            {
                var go = new GameObject(name);
                go.AddComponent<T>();
            }
        }
        
        private static void SetTransparent(Material mat)
        {
             mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
             mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
             mat.SetInt("_ZWrite", 0);
             mat.DisableKeyword("_ALPHATEST_ON");
             mat.DisableKeyword("_ALPHABLEND_ON");
             mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
             mat.renderQueue = 3000;
        }
    }
}
