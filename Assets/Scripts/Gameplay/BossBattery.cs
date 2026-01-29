using UnityEngine;

namespace HackathonVR.Gameplay
{
    public class BossBattery : MonoBehaviour
    {
        public bool isUnplugged = false;
        public GameObject electricityEffect;
        public GameObject batteryObject;

        public void Unplug()
        {
            if (isUnplugged) return;

            isUnplugged = true;
            if (electricityEffect != null) electricityEffect.SetActive(false);
            if (batteryObject != null)
            {
                // Make it fall or move
                batteryObject.transform.Translate(Vector3.down * 0.2f);
            }

            Core.DialogueManager.Instance.ShowMessage("Coéquipier", "Incroyable ! La batterie est débranchée !");
            Core.DialogueManager.Instance.ShowMessage("Coéquipier", "On a gagné !");
            
            Debug.Log("[BossBattery] Boss defeated!");
        }
    }
}
