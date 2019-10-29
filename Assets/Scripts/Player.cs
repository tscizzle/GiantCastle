using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    /* unity objects */
    private Rigidbody thisBody;
    private Camera thisCamera;

    /* state */
    private string playerMode = "firstPerson"; // one of [ "firstPerson", "thirdPerson" ]

    void Start()
    {
        thisBody = GetComponent<Rigidbody>();
        thisCamera = GetComponentInChildren<Camera>();
    }

    void FixedUpdate()
    {
        setPlayerMode();

        positionCamera();

        /* may need versions of the below methods specific to third person player mode */

        steer();

        thrust();

        brake();

        antiSliding();
    }

    private void setPlayerMode()
    {
        Vector3 straightDown = new Vector3(0, -1, 0);
        float nearDistance = 2;

        bool isNearOverObject = Physics.Raycast(transform.position, straightDown, nearDistance);
        
        playerMode = isNearOverObject ? "thirdPerson" : "firstPerson";
    }

    private void positionCamera()
    {
        float firstPersonZoom = 0;
        float thirdPersonZoom = -10;
        float zoomSteps = 16;
        float zoomDistancePerStep = (firstPersonZoom - thirdPersonZoom) / zoomSteps;

        Vector3 cameraPosition = thisCamera.transform.localPosition;

        if (playerMode == "firstPerson")
        {
            if (cameraPosition.z != firstPersonZoom)
            {
                float z = Math.Min(cameraPosition.z + zoomDistancePerStep, firstPersonZoom);
                thisCamera.transform.localPosition = new Vector3(0, 0, z);
            }
        } else if (playerMode == "thirdPerson")
        {
            if (cameraPosition.z != thirdPersonZoom)
            {
                float z = Math.Max(cameraPosition.z - zoomDistancePerStep, thirdPersonZoom);
                thisCamera.transform.localPosition = new Vector3(0, 0, z);
            }
        }
    }

    private void steer()
    {
        float yawSpeed = 100;
        float pitchSpeed = 200;

        float thetaDelta = Input.GetAxis("Horizontal") * yawSpeed * Time.deltaTime;
        float phiDelta = Input.GetAxis("Vertical") * pitchSpeed * Time.deltaTime;

        Quaternion thetaRotation = getThetaRotation(thetaDelta);
        Quaternion phiRotation = getPhiRotation(phiDelta);

        Quaternion currentOrientation = transform.rotation;
        Quaternion newOrientation = currentOrientation * thetaRotation * phiRotation;

        thisBody.MoveRotation(newOrientation);
    }

    private void thrust()
    {
        float thrustMagnitude = Physics.gravity.magnitude * 4f;
        float maxSpeed = 100;

        bool isThrustForwardOn = Input.GetKey(KeyCode.Z);
        bool isThrustBackwardOn = Input.GetKey(KeyCode.X);
        
        bool isThrustOn = isThrustForwardOn || isThrustBackwardOn;
        if (!isThrustOn)
        {
            return;
        }

        float thrustSign = isThrustForwardOn ? 1 : -1;
        Vector3 thrustDirection = transform.forward * thrustSign;

        bool isMovingWithThrust = Vector3.Dot(thisBody.velocity, thrustDirection) > 0;
        bool isAtMaxSpeed = isMovingWithThrust && thisBody.velocity.magnitude >= maxSpeed;

        if (!isAtMaxSpeed)
        {
            Vector3 thrustForce = thrustDirection * thrustMagnitude;

            thisBody.AddForce(thrustForce);
        }
    }

    private void brake()
    {
        float brakeDrag = 10;

        bool isBrakeOn = Input.GetKey(KeyCode.Space);
        
        if (isBrakeOn)
        {
            thisBody.drag = brakeDrag;
            thisBody.useGravity = false;
        } else
        {
            thisBody.drag = 0;
            thisBody.useGravity = true;
        }
    }

    /*
    Applies a frictional force opposite to (and proportional to) the component of velocity that's not in line with the
    player's facing direction. This restricts movement to be in the player's facing direction. No "sliding".
    */
    private void antiSliding()
    {
        float antiSlidingFriction = 10;

        Vector3 velocityParallelToForward = Vector3.Project(thisBody.velocity, transform.forward);

        Vector3 velocityNormalToForward = thisBody.velocity - velocityParallelToForward;

        Vector3 frictionForce = -1 * velocityNormalToForward * antiSlidingFriction;
        
        thisBody.AddForce(frictionForce);
    }

    /*
    Adjusts theta in the spherical representation of the object's orientation, i.e. rotating about the world's straight up direction, like
    swiveling your neck right or left.
    */
    Quaternion getThetaRotation(float angleDelta)
    {
        // desired transformation, as Euler angles using world directions. It's a rotation about world y.
        Vector3 globalEuler = Vector3.up * angleDelta;

        // desired transformation, as Euler angles using the object's local directions. This accounts for the object's current orientation.
        Vector3 localEuler = Quaternion.Inverse(transform.rotation) * globalEuler;
        
        // desired transformation, as Quaternion
        Quaternion deltaRotation = Quaternion.Euler(localEuler);
        return deltaRotation;
    }

    /*
    Adjusts phi in the spherical representation of the object's orientation, like moving your eyes up or down.
    Unlike when moving theta, this depends on the object's current orientation, so we don't use Quaternion.Inverse to account for it.
    */
    Quaternion getPhiRotation(float angleDelta)
    {
        // desired transformation, as Euler angles using world directions. It's a rotation about the player's x.
        Vector3 localEuler =  -1 * Vector3.right * angleDelta;
        
        // desired transformation, as Quaternion
        Quaternion deltaRotation = Quaternion.Euler(localEuler);
        return deltaRotation;
    }
}
