using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class SceneDecorator : EditorWindow
{
    [MenuItem("Hackathon/Setup All Story Scenes (1-4)")]
    public static void SetupAllScenes()
    {
        string[] sceneConfigs = { "1", "2", "3", "4" };
        bool confirm = EditorUtility.DisplayDialog("Setup All Scenes", 
            "This will open setup scenes 1, 2, 3, and 4 sequentially, apply VR setup, make objects interactive, and SAVE them. \n\nMake sure to save your current work first!", "Go!", "Cancel");
        
        if (!confirm) return;

        foreach (string sceneName in sceneConfigs)
        {
            string path = $"Assets/Scenes/{sceneName}.unity";
            if (!File.Exists(path))
            {
                Debug.LogError($"Scene not found: {path}");
                continue;
            }

            Debug.Log($"Processing Scene {sceneName}...");
            EditorSceneManager.OpenScene(path);
            
            // 1. Setup VR 
            SetupVR();
            
            // 2. Make Interactive
            MakeInteractive();
            
            // 3. Save
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
        
        // Add to Build Settings enable
        AddScenesToBuildSettings(sceneConfigs);
        
        Debug.Log("All story scenes (1-4) have been setup for VR!");
    }

    private static void AddScenesToBuildSettings(string[] sceneNames)
    {
        var existing = EditorBuildSettings.scenes.ToList();
        bool changed = false;
        
        foreach (var name in sceneNames)
        {
            string path = $"Assets/Scenes/{name}.unity";
            if (!existing.Any(s => s.path == path))
            {
                existing.Add(new EditorBuildSettingsScene(path, true));
                changed = true;
                Debug.Log($"Added {name} to Build Settings.");
            }
        }
        
        if (changed)
        {
            EditorBuildSettings.scenes = existing.ToArray();
        }
    }

    [MenuItem("Hackathon/Setup VR in Current Scene")]
    public static void SetupVR()
    {
        Debug.Log("Setting up VR...");
        
        // 1. Remove ALL existing cameras to ensure VR view
        Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var cam in cameras)
        {
            if (cam.transform.root.name != "XR Origin (XR Rig)" && cam.transform.root.name != "VR Setup")
            {
                Debug.Log($"Removing existing camera: {cam.name}");
                DestroyImmediate(cam.gameObject);
            }
        }
        
        // 2. Add XRSetup if missing
        if (Object.FindFirstObjectByType<HackathonVR.XRSetup>() == null)
        {
            GameObject setupGO = new GameObject("VR Setup");
            setupGO.transform.position = new Vector3(-8.3f, 0f, 0f); // Position on walkway
            setupGO.AddComponent<HackathonVR.XRSetup>();
            Debug.Log("Added XRSetup on walkway.");
        }
        else
        {
            Debug.Log("XRSetup already present.");
        }

        // 3. Add Music Manager if missing
        if (Object.FindFirstObjectByType<HackathonVR.MusicManager>() == null)
        {
            GameObject musicGO = new GameObject("Music Manager");
            musicGO.AddComponent<HackathonVR.MusicManager>();
            Debug.Log("Added Music Manager.");
        }
        
        Debug.Log("VR Setup Complete! Press Play to initialize rig.");
    }

    [MenuItem("Hackathon/Make Existing Objects Grabbable")]
    public static void MakeInteractive()
    {
        var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
        int count = 0;
        
        foreach (var rend in renderers)
        {
            GameObject go = rend.gameObject;
            if (go.isStatic) continue; // Ignore static objects (walls, floor)
            if (go.GetComponent<MonoBehaviour>() != null && go.name.Contains("XR")) continue; // Ignore VR rig parts
            
            // Check size (arbitrary threshold for "props")
            Vector3 size = rend.bounds.size;
            // < 1.5m implies it's likely a prop (shovel, ball, cone) not a building
            if (size.magnitude < 1.5f) 
            {
                // Add Collider if missing
                if (go.GetComponent<Collider>() == null)
                {
                    go.AddComponent<BoxCollider>();
                }

                // Add Rigidbody if missing
                if (go.GetComponent<Rigidbody>() == null)
                {
                    var rb = go.AddComponent<Rigidbody>();
                    rb.mass = 1f;
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                }
                
                // Add VRGrabInteractable if missing
                if (go.GetComponent<HackathonVR.Interactions.VRGrabInteractable>() == null)
                {
                    go.AddComponent<HackathonVR.Interactions.VRGrabInteractable>();
                    count++;
                }
            }
        }
        Debug.Log($"Made {count} existing objects grabbable! (Shovels, balls, etc.)");
    }

    [MenuItem("Hackathon/Decorate Scene")]
    public static void Decorate()
    {
        Debug.Log("Starting scene decoration...");

        // Ensure VR setup is done first just in case
        SetupVR();

        // 1. Set Skybox
        SetSkybox();

        // 2. Spawn Balloons
        SpawnBalloons();

        // 3. Spawn Grass
        SpawnGrass();
        
        Debug.Log("Scene Decoration Complete!");
    }

    private static void SetSkybox()
    {
        string skyboxPath = "Assets/Saritasa/Skybox/Skybox.mat";
        Material skybox = AssetDatabase.LoadAssetAtPath<Material>(skyboxPath);
        if (skybox != null)
        {
            RenderSettings.skybox = skybox;
            // Enable fog for better depth with skybox
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.5f, 0.6f, 0.7f);
            RenderSettings.fogDensity = 0.02f;
            Debug.Log("Skybox applied.");
        }
        else
        {
            Debug.LogError($"Skybox material not found at {skyboxPath}");
        }
    }

    private static void SpawnBalloons()
    {
        string path = "Assets/Saritasa/Models/Sport_Balls";
        if (!Directory.Exists(path))
        {
            Debug.LogError($"Balloons directory not found: {path}");
            return;
        }

        string[] balloonFiles = Directory.GetFiles(path, "*.prefab", SearchOption.AllDirectories);
        if (balloonFiles.Length == 0)
        {
            Debug.LogWarning("No balloon prefabs found.");
            return;
        }

        GameObject parent = GameObject.Find("Decorations_Balloons");
        if (parent) DestroyImmediate(parent);
        parent = new GameObject("Decorations_Balloons");

        for (int i = 0; i < 15; i++)
        {
            string randomPath = balloonFiles[Random.Range(0, balloonFiles.Length)];
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(randomPath);
            
            if (prefab)
            {
                // Instantiate as prefab link
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.SetParent(parent.transform);
                
                // Random position in air
                instance.transform.position = new Vector3(
                    Random.Range(-8f, 8f), 
                    Random.Range(2f, 8f), 
                    Random.Range(-8f, 8f)
                );
                
                // Ensure physics
                if (instance.GetComponent<Rigidbody>() == null)
                    instance.AddComponent<Rigidbody>();
                    
                // Ensure grabbable
                if (instance.GetComponent<HackathonVR.Interactions.VRGrabInteractable>() == null)
                    instance.AddComponent<HackathonVR.Interactions.VRGrabInteractable>();
            }
        }
        Debug.Log($"Spawned 15 balloons.");
    }

    private static void SpawnGrass()
    {
        string texPath = "Assets/ALP_Assets/GrassFlowersFREE/Textures/GrassFlowers";
        if (!Directory.Exists(texPath))
        {
            Debug.LogError($"Grass texture directory not found: {texPath}");
            return;
        }

        string[] texFiles = Directory.GetFiles(texPath, "*.tga");
        if (texFiles.Length == 0)
        {
            Debug.LogWarning("No grass textures found.");
            return;
        }

        GameObject parent = GameObject.Find("Decorations_Grass");
        if (parent) DestroyImmediate(parent);
        parent = new GameObject("Decorations_Grass");

        // Grass Material Template (using Particles/Standard Unlit or similar for double-sided alpha)
        // Or Standard Shader with Cutout
        Shader grassShader = Shader.Find("Standard");
        
        int grassCount = 3000;
        float range = 15f;

        for (int i = 0; i < grassCount; i++)
        {
            string randomTexPath = texFiles[Random.Range(0, texFiles.Length)];
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(randomTexPath);

            if (tex == null) continue;

            // Create Grass Object
            GameObject grass = GameObject.CreatePrimitive(PrimitiveType.Quad);
            grass.name = "Grass_" + i;
            grass.transform.SetParent(parent.transform);
            DestroyImmediate(grass.GetComponent<Collider>());

            // Position
            Vector3 pos = new Vector3(Random.Range(-range, range), 0, Random.Range(-range, range));
            float scale = Random.Range(0.4f, 1.0f);
            
            grass.transform.position = pos + Vector3.up * (scale * 0.5f); // Half height up
            grass.transform.localScale = new Vector3(scale, scale, 1f);
            
            // Random Y rotation
            grass.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

            // Material Setup
            Material mat = new Material(grassShader);
            mat.mainTexture = tex;
            
            // Configure Fade/Cutout
            mat.SetFloat("_Mode", 1); // 1 = Cutout
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            mat.SetInt("_ZWrite", 1);
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 2450;
            mat.SetFloat("_Cutoff", 0.3f);
            mat.SetFloat("_Glossiness", 0f); // Not shiny
            
            // Tint slightly green for variety
            mat.color = Color.Lerp(Color.white, new Color(0.7f, 1f, 0.7f), Random.value);

            grass.GetComponent<Renderer>().material = mat;
            
            // Cross-Quad for volume (Second quad rotated 90 degrees)
            GameObject grass2 = Instantiate(grass, parent.transform);
            grass2.transform.position = grass.transform.position;
            grass2.transform.rotation = grass.transform.rotation * Quaternion.Euler(0, 90, 0);
            grass2.transform.localScale = grass.transform.localScale;
            grass2.GetComponent<Renderer>().material = mat;
        }
        Debug.Log($"Spawned {grassCount} grass clumps.");
    }
}
