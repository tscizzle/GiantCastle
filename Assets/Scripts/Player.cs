using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    /* unity objects */
    private Rigidbody thisBody;
    private Camera thisCamera;
    private Animator thisAnimator;
    private GameObject thisModel;

    /* constants */
    private float runningPitchAngle = 9;
    private float diveTimeLength;
    private float modelOffsetY;

    /* state */
    private string playerMode; // one of [ "flyingMode", "runningMode", "divingMode" ]
    private float recentDiveStartTime;

    void Start()
    {
        thisBody = GetComponent<Rigidbody>();
        thisCamera = GetComponentInChildren<Camera>();
        thisAnimator = GetComponentInChildren<Animator>();
        thisModel = GameObject.Find("PlayerModel");

        // find diveTimeLength
        foreach (AnimationClip clip in thisAnimator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == "Dive")
            {
                diveTimeLength = clip.length;
            }
        }

        // find modelOffsetY
        modelOffsetY = thisModel.transform.localPosition.y;
    }

    void FixedUpdate()
    {
        setPlayerMode();

        positionCamera();

        steer();

        thrust();

        brake();

        antiSliding();

        animationStuff();
    }

    private void setPlayerMode()
    {
        Vector3 straightDown = new Vector3(0, -1, 0);
        float nearDistance = 1;

        /*
        use a layer mask to only look for collisions with objects the player can run on top of
        (see https://docs.unity3d.com/ScriptReference/Physics.Raycast.html)
        */
        // Bit shift the index of the layer (8) to get a bit mask
        int layerMask = 1 << 8;

        bool isNearOverObject = Physics.Raycast(transform.position, straightDown, nearDistance, layerMask);
        
        if (isNearOverObject)
        {
            playerMode = "runningMode";
        } else
        {
            if (playerMode == "runningMode")
            {
                // If previously in "runningMode" and no longer near over an object, go into
                // "divingMode" and set diveStartTime.
                playerMode = "divingMode";
                recentDiveStartTime = Time.time;
            } else if (playerMode == "divingMode")
            {
                // If previously in "divingMode" stay in "divingMode" or go into "flyingMode" if it's been
                // long enough since the dive started.
                float now = Time.time;
                float timeSinceDiveStart = now - recentDiveStartTime;
                if (timeSinceDiveStart >= diveTimeLength)
                {
                    playerMode = "flyingMode";
                }
            } else if (playerMode == "flyingMode")
            {
                // If previously in "flyingMode" stay in "flyingMode".
                playerMode = "flyingMode";
            }
        }
    }

    private void positionCamera()
    {
        float firstPersonZoom = 0;
        float thirdPersonZoom = -7;
        float zoomSteps = 10;
        float zoomDistancePerStep = (firstPersonZoom - thirdPersonZoom) / zoomSteps;

        Vector3 cameraPosition = thisCamera.transform.localPosition;

        // By default the camera is along the line (0, 0, z) and lined up with the player's orientation.
        // In flying mode and running mode this remains true.
        // In diving mode, it depends on when in the dive. At the start, the player leaps upward but
        //      the camera stays at the angle is was in during running (instead of pointing upward).
        //      Once the player pitches forward the camera goes back to lining up with the player.

        if (playerMode == "flyingMode")
        {
            if (cameraPosition.z != firstPersonZoom)
            {
                float z = Math.Min(cameraPosition.z + zoomDistancePerStep, firstPersonZoom);
                thisCamera.transform.localPosition = new Vector3(0, modelOffsetY, z);
            }
            
        } else if (playerMode == "runningMode")
        {
            if (cameraPosition.z != thirdPersonZoom)
            {
                float z = Math.Max(cameraPosition.z - zoomDistancePerStep, thirdPersonZoom);
                thisCamera.transform.localPosition = new Vector3(0, 0, z);
            }
            
            thisCamera.transform.LookAt(transform.position);

        } else if (playerMode == "divingMode")
        {
            Vector3 currentLocalEuler = transform.localEulerAngles;
            float currentPitch = angle180To180(currentLocalEuler.x);
            bool isPitchingForward = currentPitch >= runningPitchAngle;

            if (isPitchingForward)
            {
                // have the camera act normally, which is lining up with the player

                thisCamera.transform.localPosition = new Vector3(0, 0, thirdPersonZoom);
            } else
            {
                // have the camera stay at the angle and relative position it was at during running, despite
                // the player's orientation pointing upward

                Vector3 currentOrientation = transform.localEulerAngles;
                // rotateAngle will be about the player's x axis.
                // It must be enough to get to flat (-currentOrientation.x) and then to the running angle
                float rotateAngle = (-currentOrientation.x % 360) + runningPitchAngle;
                Vector3 straightBehind = new Vector3(0, 0, thirdPersonZoom);
                Vector3 newCameraPosition = Quaternion.AngleAxis(rotateAngle, Vector3.right) * straightBehind;
                thisCamera.transform.localPosition = newCameraPosition;
            }

            thisCamera.transform.LookAt(transform.position);
        }
    }

    private void steer()
    {
        float yawSpeed = 100;
        float pitchSpeed = 200;

        float thetaDelta = Input.GetAxis("Horizontal") * yawSpeed * Time.deltaTime;
        float phiDelta = Input.GetAxis("Vertical") * pitchSpeed * Time.deltaTime;

        // while diving, don't allow steering
        if (playerMode == "divingMode")
        {
            thetaDelta = 0;
            phiDelta = 0;
        }

        Quaternion thetaRotation = getThetaRotation(thetaDelta);
        Quaternion phiRotation = getPhiRotation(phiDelta);

        Quaternion currentOrientation = transform.rotation;
        Quaternion newOrientation = currentOrientation * thetaRotation * phiRotation;
        
        if (playerMode == "runningMode") {
            // if running on a surface, latch to a constant pitch angle
            Vector3 newEuler = newOrientation.eulerAngles;
            newOrientation = Quaternion.Euler(runningPitchAngle, newEuler.y, newEuler.z);
        } else if (playerMode == "divingMode")
        {
            // while diving, ignore user input for rotating up and down and instead hardcode the dive mechanics
            float newPitchAngle = getPitchAngleDuringDive();
            Vector3 newEuler = newOrientation.eulerAngles;
            newOrientation = Quaternion.Euler(newPitchAngle, newEuler.y, newEuler.z);
        }

        thisBody.MoveRotation(newOrientation);
    }

    private void thrust()
    {
        float thrustMagnitude = Physics.gravity.magnitude * 4f;
        float maxSpeed = 100;

        bool isThrustForwardOn = Input.GetKey(KeyCode.Z);
        bool isThrustBackwardOn = Input.GetKey(KeyCode.X);

        // while diving, automatically keep going forward
        if (playerMode == "divingMode")
        {
            isThrustForwardOn = true;
        }
        
        bool isThrustOn = isThrustForwardOn || isThrustBackwardOn;
        if (!isThrustOn)
        {
            return;
        }
        // don't allow going backward while in running mode
        if (!isThrustForwardOn && playerMode == "runningMode") {
            return;
        }

        float thrustSign = isThrustForwardOn ? 1 : -1;
        Vector3 thrustDirection = transform.forward * thrustSign;

        bool isMovingWithThrustDirection = Vector3.Dot(thisBody.velocity, thrustDirection) > 0;
        bool isAtMaxSpeed = isMovingWithThrustDirection && thisBody.velocity.magnitude >= maxSpeed;

        if (isAtMaxSpeed) {
            return;
        }

        Vector3 thrustForce = thrustDirection * thrustMagnitude;

        thisBody.AddForce(thrustForce);
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

    private void animationStuff()
    {   
        // set animator parameters (for transitions)
        
        float forwardSpeed = Math.Abs(Vector3.Dot(thisBody.velocity, transform.forward));
        bool isRunning = playerMode == "runningMode" && forwardSpeed >= 0.8;
        bool isStanding = playerMode == "runningMode" && forwardSpeed < 0.8;
        bool isDiving = playerMode == "divingMode";
        thisAnimator.SetBool("isRunning", isRunning);
        thisAnimator.SetBool("isStanding", isStanding);
        thisAnimator.SetBool("isDiving", isDiving);

        // lay flat forward while flying (but when switching between laying flat and not, do it smoothly)
        
        float flyingTilt = 90;
        float runningTilt = 0;
        float tiltSteps = 10;
        float tiltDegreesPerStep = (flyingTilt - runningTilt) / tiltSteps;

        Vector3 currentLocalEuler = thisModel.transform.localEulerAngles;
        
        if (playerMode == "flyingMode" || playerMode == "divingMode")
        {
            if (currentLocalEuler.x != flyingTilt)
            {
                float x = Math.Min(currentLocalEuler.x + tiltDegreesPerStep, flyingTilt);
                thisModel.transform.localEulerAngles = new Vector3(x, 0, 0);
            }
        } else if (playerMode == "runningMode")
        {
            if (currentLocalEuler.x != runningTilt)
            {
                float x = Math.Max(currentLocalEuler.x - tiltDegreesPerStep, runningTilt);
                thisModel.transform.localEulerAngles = new Vector3(x, 0, 0);
            }
        }
    }

    /*
    Adjusts theta in the spherical representation of the object's orientation, i.e. rotating about the world's straight up direction, like
    swiveling your neck right or left.
    */
    private Quaternion getThetaRotation(float angleDelta)
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
    private Quaternion getPhiRotation(float angleDelta)
    {
        // desired transformation, as Euler angles using world directions. It's a rotation about the player's x.
        Vector3 localEuler =  -1 * Vector3.right * angleDelta;
        
        // desired transformation, as Quaternion
        Quaternion deltaRotation = Quaternion.Euler(localEuler);
        return deltaRotation;
    }

    /*
    During the dive, we ignore user input for rotating up and down and instead directly set the pitch angle througout the dive.
    Get how far we are through the dive using the current time and recentDiveStartTime and diveTimeLength, and based on how far
    through the dive we are, return a pitch angle.
    */
    private float getPitchAngleDuringDive()
    {
        float startingDiveAngle = -60;
        float endingDiveAngle = 80;

        float now = Time.time;
        float timeSinceDiveStart = now - recentDiveStartTime;
        float howFarThroughDive = timeSinceDiveStart / diveTimeLength;
        float newPitchAngle = startingDiveAngle + (endingDiveAngle - startingDiveAngle) * howFarThroughDive;
        return newPitchAngle;
    }

    private float angle180To180(float degrees)
    {
        float newDegrees = degrees % 360 < 0 ? degrees % 360 + 360 : degrees % 360;
        if (newDegrees <= -180)
        {
            return newDegrees + 360;
        } else if (newDegrees > 180)
        {
            return newDegrees - 360;
        } else
        {
            return newDegrees;
        }
    }
}
