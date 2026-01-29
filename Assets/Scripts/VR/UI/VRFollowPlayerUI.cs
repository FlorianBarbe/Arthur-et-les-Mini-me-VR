using UnityEngine;

namespace HackathonVR.UI
{
    public class VRFollowPlayerUI : MonoBehaviour
    {
        public Transform targetCamera;
        public float distance = 2.0f;
        public float heightOffset = -0.5f;
        public float smoothSpeed = 5.0f;

        private void Start()
        {
            if (targetCamera == null)
            {
                Camera cam = Camera.main;
                if (cam != null) targetCamera = cam.transform;
            }
        }

        private void LateUpdate()
        {
            if (targetCamera == null) return;

            // Target position in front of camera
            Vector3 targetPos = targetCamera.position + targetCamera.forward * distance;
            targetPos.y += heightOffset;

            // Smooth move
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * smoothSpeed);

            // Face the camera
            transform.LookAt(targetCamera);
            transform.Rotate(0, 180, 0); // Correct rotation so text is readable
        }
    }
}
