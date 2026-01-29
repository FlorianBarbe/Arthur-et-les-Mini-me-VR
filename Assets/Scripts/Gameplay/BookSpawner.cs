using UnityEngine;

namespace HackathonVR.Gameplay
{
    public class BookSpawner : MonoBehaviour
    {
        [Header("Book")]
        public GameObject bookPrefab;

        [Header("Spawn Position (World Space)")]
        public Vector3 spawnPosition;
        public Vector3 spawnRotationEuler;

        private bool spawned = false;

        public void SpawnBook()
        {
            if (spawned) return;
            spawned = true;

            if (bookPrefab == null)
            {
                Debug.LogError("[BookSpawner] Book Prefab is NULL");
                return;
            }

            Quaternion rot = Quaternion.Euler(spawnRotationEuler);
            Instantiate(bookPrefab, spawnPosition, rot);

            Debug.Log($"[BookSpawner] Book spawned at {spawnPosition}");
        }
    }
}
