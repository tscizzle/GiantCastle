using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    private static float yawSpeed = 100;
    private static float pitchSpeed = 200;
    private static float thrustMagnitude = Physics.gravity.magnitude * 4f;
    private static float maxSpeed = 60;
    private static float brakeDrag = 10;
    private static float antiSlidingFriction = 10;

    private Rigidbody thisBody;

    void Start()
    {
        thisBody = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        steer();

        thrust();

        brake();

        antiSliding();
    }

    private void steer()
    {
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
        bool isThrustOn = Input.GetKey(KeyCode.Z);

        bool isMovingForward = Vector3.Dot(thisBody.velocity, transform.forward) > 0;
        bool isAtMaxSpeed = isMovingForward && thisBody.velocity.magnitude >= maxSpeed;

        if (isThrustOn && !isAtMaxSpeed)
        {
            Vector3 thrustDirection = transform.forward;
            Vector3 thrustForce = thrustDirection * thrustMagnitude;

            thisBody.AddForce(thrustForce);
        }
    }

    private void brake()
    {
        bool isBrakeOn = Input.GetKey(KeyCode.X);
        
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
