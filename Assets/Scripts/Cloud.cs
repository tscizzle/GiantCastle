using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cloud : MonoBehaviour
{
    private static float cloudsAltitude = 200;

    private MeshRenderer cloudRenderer;

    void Start()
    {
        cloudRenderer = GetComponent<MeshRenderer>();
        cloudRenderer.enabled = false;
    }

    void Update()
    {
        if (transform.position.y <= cloudsAltitude)
        {
            cloudRenderer.enabled = true;
        } else
        {
            cloudRenderer.enabled = false;
        }
    }
}
