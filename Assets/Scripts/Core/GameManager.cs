using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace HackathonVR.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Scene Sequence")]
        public string[] sceneSequence = { "1", "2", "3", "4" };
        private int currentSceneIndex = 0;

        [Header("Story State")]
        public bool isTiny = false;
        public int friendsFound = 0;
        public int totalFriendsToFind = 3;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void LoadNextScene()
        {
            currentSceneIndex++;
            if (currentSceneIndex < sceneSequence.Length)
            {
                StartCoroutine(TransitionToScene(sceneSequence[currentSceneIndex]));
            }
            else
            {
                Debug.Log("Game Finished! No more scenes.");
            }
        }

        public void LoadScene(string sceneName)
        {
            StartCoroutine(TransitionToScene(sceneName));
        }

        public void FindFriend()
        {
            friendsFound++;
            Debug.Log($"[GameManager] Friend found! ({friendsFound}/{totalFriendsToFind})");
            DialogueManager.Instance.ShowMessage("Friend", "Tu m'as trouvé ! Cherchons les autres.");
        }

        public void SetTiny(bool tiny)
        {
            isTiny = tiny;
            // In a real implementation, we would scale the player or the world
            Debug.Log($"[GameManager] Player tiny status: {tiny}");
        }

        private IEnumerator TransitionToScene(string sceneName)
        {
            Debug.Log($"[GameManager] Transitioning to scene: {sceneName}");
            
            // Here we could add a fade-to-black effect if we have a UI Canvas
            
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            Debug.Log($"[GameManager] Scene {sceneName} loaded.");
        }
    }
}
