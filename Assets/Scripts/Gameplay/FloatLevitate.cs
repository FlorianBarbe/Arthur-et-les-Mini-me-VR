using UnityEngine;

namespace HackathonVR.Gameplay
{
    public class FloatLevitate : MonoBehaviour
    {
        [Header("Vertical Float")]
        public float amplitude = 0.08f;   // hauteur du va-et-vient (en mètres)
        public float frequency = 0.8f;    // vitesse (Hz)

        [Header("Optional Sway")]
        public bool rotateSlightly = true;
        public float rotateAmplitudeDeg = 2.5f;
        public float rotateFrequency = 0.6f;

        private Vector3 basePos;
        private Quaternion baseRot;

        private void Awake()
        {
            basePos = transform.position;
            baseRot = transform.rotation;
        }

        private void OnEnable()
        {
            // Si l'objet est réactivé ou respawn, on recale la base
            basePos = transform.position;
            baseRot = transform.rotation;
        }

        private void Update()
        {
            float t = Time.time;

            // Vertical bob
            float yOffset = Mathf.Sin(t * Mathf.PI * 2f * frequency) * amplitude;
            transform.position = basePos + new Vector3(0f, yOffset, 0f);

            // Tiny rotation sway (optional)
            if (rotateSlightly)
            {
                float sway = Mathf.Sin(t * Mathf.PI * 2f * rotateFrequency) * rotateAmplitudeDeg;
                transform.rotation = baseRot * Quaternion.Euler(0f, sway, 0f);
            }
        }
    }
}
