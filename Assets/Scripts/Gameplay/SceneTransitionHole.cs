using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_XR_COREUTILS
using Unity.XR.CoreUtils;
#endif

/// <summary>
/// Place this on a GameObject in Scene 2 (ex: "HoleTrigger").
/// Add a BoxCollider (isTrigger = true) that fills the hole.
/// When the HEAD (Main Camera) enters: fade -> load Scene 3 async -> reposition XR Origin -> fade in.
/// </summary>
public class SceneTransitionHole : MonoBehaviour
{
    [Header("Scene")]
    [Tooltip("Name of the scene to load (must be in Build Settings).")]
    public string targetSceneName = "3";

    [Header("Spawn in Target Scene")]
    [Tooltip("In Scene 3, create an empty GameObject named like this, at the spawn position.")]
    public string targetSpawnName = "Scene3_Spawn";

    [Header("Fade")]
    [Range(0f, 2f)] public float fadeOutDuration = 0.35f;
    [Range(0f, 2f)] public float fadeInDuration  = 0.35f;
    public bool startBlackDuringLoad = true;

    [Header("VR Comfort")]
    [Tooltip("Optional: disable these components during transition (locomotion, grab, etc.).")]
    public Behaviour[] disableDuringTransition;

    private bool triggered;
    private Transform xrOrigin;
    private Transform xrHead;
    private FadeQuad fader;

    private void Awake()
    {
        // Build a fader attached to the head (camera)
        xrHead = FindHead();
        xrOrigin = FindXROrigin(xrHead);

        if (xrHead != null)
        {
            fader = FadeQuad.Create(xrHead);
            fader.SetAlpha(0f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        // We only trigger when the headset/camera enters the hole.
        // Best setup: put a small trigger collider on the head object OR rely on name match.
        // Here: accept MainCamera object or anything with a Camera in parent.
        bool isHead =
            other.GetComponentInParent<Camera>() != null ||
            other.CompareTag("MainCamera") ||
            other.name.Contains("Main Camera") ||
            other.name.Contains("Camera");

        if (!isHead) return;

        triggered = true;
        StartCoroutine(DoTransition());
    }

    private IEnumerator DoTransition()
    {
        SetDisabled(true);

        if (fader != null)
            yield return fader.FadeTo(1f, fadeOutDuration);

        // Load Scene 3 async
        var op = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Single);
        if (startBlackDuringLoad && fader != null) fader.SetAlpha(1f);

        while (op != null && !op.isDone)
            yield return null;

        // After load, reacquire head/origin in the new scene
        xrHead = FindHead();
        xrOrigin = FindXROrigin(xrHead);

        // Ensure fader exists in new scene (head changed)
        if (xrHead != null)
        {
            if (fader == null || fader.Head != xrHead)
                fader = FadeQuad.Create(xrHead);
            fader.SetAlpha(1f);
        }

        // Move XR Origin to the spawn point in Scene 3
        RepositionToSpawn();

        // Fade in
        if (fader != null)
            yield return fader.FadeTo(0f, fadeInDuration);

        SetDisabled(false);
    }

    private void RepositionToSpawn()
    {
        if (xrOrigin == null || xrHead == null) return;

        var spawn = GameObject.Find(targetSpawnName);
        if (spawn == null)
        {
            Debug.LogWarning($"[SceneTransitionHole] Spawn '{targetSpawnName}' not found in scene '{targetSceneName}'.");
            return;
        }

        Transform spawnT = spawn.transform;

        // --- VR-safe reposition ---
        // We want the HEAD to end up at spawn position, without directly moving the head transform.
        // So we shift the XR Origin by the delta needed to bring head to spawn.
        Vector3 headPos = xrHead.position;
        Vector3 originPos = xrOrigin.position;

        Vector3 originToHead = headPos - originPos;
        Vector3 newOriginPos = spawnT.position - originToHead;
        xrOrigin.position = newOriginPos;

        // Optional: align yaw to spawn yaw (comfortable; no roll/pitch).
        float currentYaw = xrOrigin.eulerAngles.y;
        float targetYaw = spawnT.eulerAngles.y;
        float deltaYaw = Mathf.DeltaAngle(currentYaw, targetYaw);

        // Rotate origin around head to preserve head position while changing yaw.
        xrOrigin.RotateAround(xrHead.position, Vector3.up, deltaYaw);
    }

    private void SetDisabled(bool disabled)
    {
        if (disableDuringTransition == null) return;
        foreach (var b in disableDuringTransition)
            if (b != null) b.enabled = !disabled;
    }

    private Transform FindHead()
    {
        // Prefer Camera.main
        if (Camera.main != null) return Camera.main.transform;

        // Fallback: any camera
        var cam = FindFirstObjectByType<Camera>();
        if (cam != null) return cam.transform;

        return null;
    }

    private Transform FindXROrigin(Transform head)
    {
#if UNITY_XR_COREUTILS
        var origin = FindFirstObjectByType<XROrigin>();
        if (origin != null) return origin.transform;
#endif
        // Fallback by name used in your project
        var go = GameObject.Find("XR Origin (XR Rig)");
        if (go == null) go = GameObject.Find("VR Setup");
        if (go != null) return go.transform;

        // Last fallback: use head root
        if (head != null) return head.root;

        return null;
    }

    /// Minimal “black quad” fader in front of the headset.
    private class FadeQuad
    {
        public Transform Head { get; private set; }
        private readonly Material mat;
        private readonly GameObject quad;

        private FadeQuad(Transform head, GameObject quad, Material mat)
        {
            Head = head;
            this.quad = quad;
            this.mat = mat;
        }

        public static FadeQuad Create(Transform head)
        {
            // Create a quad in front of the head (local space)
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "__FadeQuad";
            Object.Destroy(quad.GetComponent<Collider>());

            quad.transform.SetParent(head, false);
            quad.transform.localPosition = new Vector3(0f, 0f, 0.25f);
            quad.transform.localRotation = Quaternion.identity;
            quad.transform.localScale = new Vector3(2f, 2f, 1f);

            // Unlit transparent material
            var shader = Shader.Find("Unlit/Color");
            var mat = new Material(shader);
            mat.color = new Color(0f, 0f, 0f, 0f);

            var r = quad.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;

            // Make sure it renders on top (sometimes needed)
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;

            // Keep across scene load? (scene is Single, so it will be destroyed)
            // That is fine: we recreate after load.

            return new FadeQuad(head, quad, mat);
        }

        public void SetAlpha(float a)
        {
            if (mat == null) return;
            Color c = mat.color;
            c.a = Mathf.Clamp01(a);
            mat.color = c;
        }

        public IEnumerator FadeTo(float targetAlpha, float duration)
        {
            float start = mat.color.a;
            if (duration <= 0f)
            {
                SetAlpha(targetAlpha);
                yield break;
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                SetAlpha(Mathf.Lerp(start, targetAlpha, k));
                yield return null;
            }

            SetAlpha(targetAlpha);
        }
    }
}
