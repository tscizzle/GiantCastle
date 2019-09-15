using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateClouds : MonoBehaviour
{
    private static float cloudSpacing = 50;
    private static float cloudLayerY = 100;
    
    private GameObject clouds;
    public GameObject cloudPrefab;

    void Start()
    {
        clouds = GameObject.Find("Clouds");

        for (int x = -3; x <= 3; x++)
        {
            for (int z = -3; z <= 3; z++)
            {
                Vector3 cloudPosition = new Vector3(x * cloudSpacing, cloudLayerY, z * cloudSpacing);
                GameObject cloud = Instantiate(cloudPrefab, cloudPosition, Quaternion.identity);
                cloud.transform.parent = clouds.transform;
            }
        }
    }
}
