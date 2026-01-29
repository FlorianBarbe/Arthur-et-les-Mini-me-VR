using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

namespace HackathonVR.Core
{
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [Header("UI References")]
        public GameObject dialoguePanel;
        public TextMeshProUGUI speakerNameText;
        public TextMeshProUGUI dialogueText;

        private Queue<DialogueLine> dialogueQueue = new Queue<DialogueLine>();
        private bool isDisplaying = false;

        public struct DialogueLine
        {
            public string speaker;
            public string message;
            public float duration;
        }

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

            if (dialoguePanel != null) dialoguePanel.SetActive(false);
        }

        public void ShowMessage(string speaker, string message, float duration = 4f)
        {
            dialogueQueue.Enqueue(new DialogueLine { speaker = speaker, message = message, duration = duration });
            if (!isDisplaying)
            {
                StartCoroutine(ProcessQueue());
            }
        }

        private IEnumerator ProcessQueue()
        {
            isDisplaying = true;
            if (dialoguePanel != null) dialoguePanel.SetActive(true);

            while (dialogueQueue.Count > 0)
            {
                DialogueLine line = dialogueQueue.Dequeue();
                if (speakerNameText != null) speakerNameText.text = line.speaker;
                if (dialogueText != null) dialogueText.text = line.message;

                yield return new WaitForSeconds(line.duration);
            }

            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            isDisplaying = false;
        }
    }
}
