using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cloud : MonoBehaviour
{
    private static float cloudAnticipationGain = 0.1f;

    private ParticleSystem cloudParticles;
    private GameObject player;
    private Rigidbody playerBody;
    private float cloudAltitude;

    public Vector3 relativePositionToPlayer = Vector3.zero; // set for each Cloud in editor

    void Start()
    {
        cloudParticles = GetComponent<ParticleSystem>();
        cloudParticles.Stop();

        player = GameObject.Find("Player");
        playerBody = player.GetComponent<Rigidbody>();

        GameObject cloudPlane = GameObject.Find("CloudPlane (Top)");
        cloudAltitude = cloudPlane.transform.position.y;
    }

    void Update()
    {
        /* move the cloud with the player, slightly in front, slightly more in front if they're moving */

        Vector3 playerPosition = player.transform.position;

        Quaternion playerFacing = player.transform.rotation;
        Vector3 relativePositionInFrontOfPlayer = playerFacing * relativePositionToPlayer;
        
        Vector3 anticipationFactor = playerBody.velocity * cloudAnticipationGain;
        
        transform.position = (
            playerPosition +
            relativePositionInFrontOfPlayer +
            anticipationFactor
        );

        if (transform.position.y <= cloudAltitude)
        {
            if (cloudParticles.isStopped) 
            {
                cloudParticles.Play();
            }
        } else
        {
            if (cloudParticles.isPlaying)
            {
                cloudParticles.Stop();
            }
        }
    }
}
