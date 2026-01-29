using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using TMPro;

public class SimpleDialogue : MonoBehaviour
{
    public GameObject bubbleRoot;   // Le Panel (bulle)
    public TMP_Text bubbleText;     // Le texte TMP dans la bulle

    [TextArea(2, 4)]
    public List<string> lines = new List<string>();

    public UnityEngine.Events.UnityEvent onFinished;
    public GameObject objectToActivateOnFinish;

    public void SetDialogue(List<string> newLines)
    {
        lines = new List<string>(newLines);
        index = -1;
        isOpen = false;
        Open();
        NextLine();
    }

    public KeyCode nextKey = KeyCode.E; // Keep E as backup
    
    // VR Input
    private InputDevice rightController;
    private bool wasPressed = false;

    int index = -1;
    bool isOpen = false;

    void Start() 
    {
        // Auto-start dialogue
        Open();
        NextLine();
    }

    void Update()
    {
        // 1. Keyboard Input
        if (Input.GetKeyDown(nextKey)) 
        {
            NextLine();
            return;
        }
        
        // 2. VR Input (Right Controller 'A' Button)
        if (!rightController.isValid)
        {
            InitializeController();
        }

        if (rightController.isValid)
        {
            // PrimaryButton is usually 'A' on Oculus Touch right controller
            if (rightController.TryGetFeatureValue(CommonUsages.primaryButton, out bool isPressed))
            {
                // Trigger on button down (rising edge)
                if (isPressed && !wasPressed)
                {
                    if (!isOpen)
                    {
                        Open();
                        NextLine();
                    }
                    else
                    {
                        NextLine();
                    }
                }
                wasPressed = isPressed;
            }
        }
    }

    void InitializeController()
    {
        var devices = new List<InputDevice>();
        // Find Right Controller
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, 
            devices);
            
        if (devices.Count > 0)
        {
            rightController = devices[0];
        }
    }

    void Open()
    {
        isOpen = true;
        if(bubbleRoot != null) bubbleRoot.SetActive(true);
    }

    void Close()
    {
        isOpen = false;
        if(bubbleRoot != null) bubbleRoot.SetActive(false);
        if(bubbleText != null) bubbleText.text = "";
        index = -1;
        onFinished?.Invoke();
        if (objectToActivateOnFinish != null) objectToActivateOnFinish.SetActive(true);
    }

    void NextLine()
    {
        if (lines.Count == 0) { Close(); return; }

        index++;
        if (index >= lines.Count) { Close(); return; }

        if(bubbleText != null) bubbleText.text = lines[index];
    }
}
