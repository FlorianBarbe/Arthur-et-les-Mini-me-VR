using System.Collections;
using UnityEngine;

public class DelayEnable4DS : MonoBehaviour
{
    public float dt = 2f;
    public Behaviour plugin4ds; // glissez "Plugin 4DS (Script)" ici

    void Awake()
    {
        if (!plugin4ds) plugin4ds = GetComponent<Behaviour>();
        plugin4ds.enabled = false;
    }

    IEnumerator Start()
    {
        yield return new WaitForSeconds(dt);
        plugin4ds.enabled = true;
    }
}
