using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace HackathonVR
{
    public class FlashlightController : MonoBehaviour
    {
        public InputHelpers.Button toggleButton = InputHelpers.Button.SecondaryButton; // Y or B
        public float toggleCooldown = 0.5f;
        
        private Light flashlight;
        private InputDevice controller;
        private bool isLightOn = false;
        private float lastToggleTime = 0f;
        private bool controllerFound = false;

        private void Start()
        {
            flashlight = GetComponent<Light>();
            TryFindController();
        }

        private void Update()
        {
            if (!controllerFound)
            {
                TryFindController();
                return;
            }

            if (Time.time - lastToggleTime < toggleCooldown) return;

            bool pressed;
            // Check secondary button (Y/B)
            if (controller.TryGetFeatureValue(CommonUsages.secondaryButton, out pressed) && pressed)
            {
                ToggleLight();
                lastToggleTime = Time.time;
            }
        }
        
        private void ToggleLight()
        {
            isLightOn = !isLightOn;
            if (flashlight != null) flashlight.enabled = isLightOn;
        }

        private void TryFindController()
        {
            var parent = transform.parent;
            if (parent == null) return;
            
            // Determine side based on parent name
            bool isLeft = parent.name.Contains("Left");
            var characteristics = isLeft ? InputDeviceCharacteristics.Left : InputDeviceCharacteristics.Right;
            characteristics |= InputDeviceCharacteristics.Controller;
            
            var devices = new System.Collections.Generic.List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(characteristics, devices);
            
            if (devices.Count > 0)
            {
                controller = devices[0];
                controllerFound = true;
            }
        }
    }
}
