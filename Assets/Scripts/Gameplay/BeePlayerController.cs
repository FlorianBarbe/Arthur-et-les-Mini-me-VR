using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

/// <summary>
/// Drone-style controller for a Bee in VR.
/// NEW SCHEME (as requested):
/// LEFT stick:  X = continuous yaw (turn around Y), Y = altitude up/down
/// RIGHT stick: Y = forward/back, X = strafe left/right
/// Also supports "riding" (bee attached to player rig).
/// Robust to controller disconnect/reconnect.
/// </summary>
public class BeePlayerController : MonoBehaviour
{
    [Header("Speeds")]
    public float moveSpeed = 5.0f;       // m/s
    public float turnSpeed = 60.0f;      // deg/s
    public float verticalSpeed = 3.0f;   // m/s

    [Header("Input Tuning")]
    [Range(0f, 0.5f)] public float deadzone = 0.12f;
    [Tooltip("If true, forward/strafe ignores pitch/roll (flat drone).")]
    public bool flatMovement = true;
    [Tooltip("0 = no smoothing, higher = smoother (e.g. 10-20).")]
    public float inputSmoothing = 12f;

    [Header("Rider / Player Attach")]
    [Tooltip("Rig root moved with the bee (XR Origin / VR Setup). If null, auto-detect from Camera.main.root.")]
    public Transform playerRigRoot;
    [Tooltip("Head/camera transform. If null, uses Camera.main.")]
    public Transform playerHead;
    [Tooltip("Seat anchor on the bee. If null, uses this transform.")]
    public Transform seat;
    [Tooltip("If true, keep the rig aligned to the seat every frame (strong attachment).")]
    public bool keepRiderAttached = true;
    [Tooltip("If true, aligns the player's head to the seat (best illusion). If false, aligns rig root to seat.")]
    public bool alignHeadToSeat = true;
    [Tooltip("If true, copies bee yaw to rig yaw.")]
    public bool matchYawToBee = true;
    [Tooltip("Scale of the rider. 1 = normal, 0.1 = tiny (e.g. Minimoys).")]
    public float playerScale = 0.1f;

    [Header("Optional Altitude Clamp")]
    public bool clampAltitude = false;
    public float minY = 0.0f;
    public float maxY = 50.0f;

    // XR devices
    private InputDevice leftDevice;
    private InputDevice rightDevice;

    // Smoothed input
    private Vector2 leftAxisSmoothed;
    private Vector2 rightAxisSmoothed;

    // Rigidbody (optional)
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        TryInitializeDevices();
        InputDevices.deviceConnected += OnDeviceConnected;
        InputDevices.deviceDisconnected += OnDeviceDisconnected;
    }

    private void OnDisable()
    {
        InputDevices.deviceConnected -= OnDeviceConnected;
        InputDevices.deviceDisconnected -= OnDeviceDisconnected;
    }

    private void Update()
    {
        if (!leftDevice.isValid || !rightDevice.isValid)
            TryInitializeDevices();

        // Read sticks (primary2DAxis OR secondary2DAxis fallback)
        Vector2 leftAxisRaw = ReadStick(leftDevice);
        Vector2 rightAxisRaw = ReadStick(rightDevice);

        // Deadzone + rescale
        Vector2 leftAxis = ApplyDeadzoneRescale(leftAxisRaw, deadzone);
        Vector2 rightAxis = ApplyDeadzoneRescale(rightAxisRaw, deadzone);

        // Smoothing
        if (inputSmoothing > 0f)
        {
            float k = 1f - Mathf.Exp(-inputSmoothing * Time.deltaTime);
            leftAxisSmoothed = Vector2.Lerp(leftAxisSmoothed, leftAxis, k);
            rightAxisSmoothed = Vector2.Lerp(rightAxisSmoothed, rightAxis, k);
        }
        else
        {
            leftAxisSmoothed = leftAxis;
            rightAxisSmoothed = rightAxis;
        }

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // NEW INPUT MAPPING:
        // Left stick:  X = yaw, Y = altitude
        // Right stick: X = strafe, Y = forward/back
        float yawInput = leftAxisSmoothed.x;
        float altitudeInput = leftAxisSmoothed.y;

        float forwardInput = rightAxisSmoothed.y;
        float strafeInput = rightAxisSmoothed.x;

        float yawDeltaDeg = yawInput * turnSpeed * dt;

        Vector3 translationDelta = ComputeTranslationDelta(forwardInput, strafeInput, altitudeInput, dt);

        ApplyBeeMotion(translationDelta, yawDeltaDeg);

        if (keepRiderAttached)
            AttachRiderToSeat();
    }

    // ---------------- Movement ----------------

    private Vector3 ComputeTranslationDelta(float forwardInput, float strafeInput, float altitudeInput, float dt)
    {
        Vector3 forwardDir;
        Vector3 rightDir;

        if (flatMovement)
        {
            Vector3 f = transform.forward; f.y = 0f;
            Vector3 r = transform.right;   r.y = 0f;

            forwardDir = f.sqrMagnitude > 0.0001f ? f.normalized : Vector3.forward;
            rightDir = r.sqrMagnitude > 0.0001f ? r.normalized : Vector3.right;
        }
        else
        {
            forwardDir = transform.forward;
            rightDir = transform.right;
        }

        Vector3 horizontal = (forwardDir * forwardInput + rightDir * strafeInput) * moveSpeed * dt;
        Vector3 vertical = Vector3.up * (altitudeInput * verticalSpeed * dt);

        return horizontal + vertical;
    }

    private void ApplyBeeMotion(Vector3 deltaPos, float deltaYawDeg)
    {
        Vector3 targetPos = transform.position + deltaPos;
        if (clampAltitude)
            targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);

        Quaternion targetRot = transform.rotation * Quaternion.Euler(0f, deltaYawDeg, 0f);

        if (rb != null && !rb.isKinematic)
        {
            rb.MovePosition(targetPos);
            rb.MoveRotation(targetRot);
        }
        else
        {
            transform.position = targetPos;
            transform.rotation = targetRot;
        }
    }

    // ---------------- Rider Attachment ----------------

    private void ResolveReferences()
    {
        if (playerHead == null && Camera.main != null)
            playerHead = Camera.main.transform;

        if (playerRigRoot == null && playerHead != null)
            playerRigRoot = playerHead.root;

        if (seat == null)
            seat = transform;
    }

    private void AttachRiderToSeat()
    {
        ResolveReferences();
        if (playerRigRoot == null || playerHead == null || seat == null) return;

        // Align rig so that the head ends up at the seat position
        if (alignHeadToSeat)
        {
            Vector3 headToRig = playerHead.position - playerRigRoot.position;
            Vector3 targetRigPos = seat.position - headToRig;
            playerRigRoot.position = targetRigPos;
        }
        else
        {
            playerRigRoot.position = seat.position;
        }

        if (matchYawToBee)
        {
            Vector3 rigEuler = playerRigRoot.eulerAngles;
            rigEuler.y = transform.eulerAngles.y;
            playerRigRoot.eulerAngles = rigEuler;
        }

        playerRigRoot.localScale = Vector3.one * playerScale;
    }

    // ---------------- XR Devices ----------------

    private void TryInitializeDevices()
    {
        if (!leftDevice.isValid)
            leftDevice = GetFirstDeviceAtNode(XRNode.LeftHand);

        if (!rightDevice.isValid)
            rightDevice = GetFirstDeviceAtNode(XRNode.RightHand);
    }

    private void OnDeviceConnected(InputDevice device)
    {
        if ((device.characteristics & InputDeviceCharacteristics.Controller) == 0)
            return;

        if (!leftDevice.isValid)
            leftDevice = GetFirstDeviceAtNode(XRNode.LeftHand);

        if (!rightDevice.isValid)
            rightDevice = GetFirstDeviceAtNode(XRNode.RightHand);
    }

    private void OnDeviceDisconnected(InputDevice device)
    {
        if (leftDevice.isValid && device == leftDevice) leftDevice = default;
        if (rightDevice.isValid && device == rightDevice) rightDevice = default;
    }

    private static InputDevice GetFirstDeviceAtNode(XRNode node)
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(node, devices);
        return devices.Count > 0 ? devices[0] : default;
    }

    // ---------------- Input Helpers ----------------

    /// <summary>
    /// Reads stick from primary2DAxis, falls back to secondary2DAxis if needed.
    /// </summary>
    private static Vector2 ReadStick(InputDevice device)
    {
        if (!device.isValid) return Vector2.zero;

        if (device.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 v))
        {
            if (v.sqrMagnitude > 0.000001f) return v;
        }

        if (device.TryGetFeatureValue(CommonUsages.secondary2DAxis, out v))
            return v;

        return Vector2.zero;
    }

    private static Vector2 ApplyDeadzoneRescale(Vector2 v, float dz)
    {
        float m = v.magnitude;
        if (m < dz) return Vector2.zero;

        float newMag = Mathf.InverseLerp(dz, 1f, m);
        return v.normalized * newMag;
    }
}
