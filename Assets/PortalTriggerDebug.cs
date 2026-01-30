using UnityEngine;

public class PortalTriggerDebug : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("[PORTAL] Trigger ENTER by: " + other.name + " (root=" + other.transform.root.name + ")");
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log("[PORTAL] Trigger EXIT by: " + other.name + " (root=" + other.transform.root.name + ")");
    }
}
