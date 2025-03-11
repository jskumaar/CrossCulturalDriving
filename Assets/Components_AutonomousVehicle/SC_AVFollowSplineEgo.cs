using System;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using System.Collections;

public class SC_AVFollowSplineEgo : MonoBehaviour
{
    public SplineContainer defaultSplineContainer;
    public SplineContainer surpriseAlertSplineContainer;
    public SplineContainer confusionDrivingSplineContainer;
    private SplineContainer splineContainer;

    // public SplineContainer splineContainer;
    public NetworkVehicleController vehicleController; 
    public SO_AVFollowSplineConfig normalConfig;
    public SO_AVFollowSplineConfig sportyConfig;
    public SO_AVFollowSplineConfig ecoConfig;
    public SO_AVFollowSplineConfig stopConfig;

    public MarkerActivator markerActivator;

    public string driveMode;

    private SO_AVFollowSplineConfig currentConfig;

    public float steeringControl;
    public float throttleControl;
    public float currentSpeed;
    

    private float steeringIntegral = 0f;
    private float steeringPrevError = 0f;

    private float speedIntegral = 0f;
    private float speedPrevError = 0f;
    
    private Rigidbody rb;

    private float _closestT;
    private Vector3 _closestPoint;
    private float _lookT;
    private Vector3 _lookPoint;
    private Vector3 _toTarget;
    private float _headingError;

    public bool IsDriving = false;

    private bool initializedClosestT = false;
    private float lastClosestT = 0f;
    private bool startButtonPress = false;
    private bool vehicleStopped = false;
    private bool resetToNormalConfig = false;
    private bool configChanged = false;

    private ScenarioManagerStartle scenarioManager;

    private bool checkIgnitionPressFlag = false;

    private float ignitionButtonPressNum = 0;

    public Vector3 originalPos;
    public Quaternion originalRot;
    private SO_AVFollowSplineConfig pendingConfig;
    private float configChangeDelay = 2f; // Adjust delay time in seconds

    private float stopSignWaitTime = 2f;
    private bool configChangePending = false;
    private Coroutine configChangeCoroutine;

    private bool indicatorLeft, indicatorRight, newButtonPress;  // variables to control indicator lights

    private float buttonPressTime = 0f;

    void Start()
    {
        rb = vehicleController.GetComponent<Rigidbody>();
        originalPos = transform.position;
        originalRot = transform.rotation;
        currentConfig = ecoConfig;
        driveMode = "eco";
        splineContainer = defaultSplineContainer;
        scenarioManager = FindObjectOfType<ScenarioManagerStartle>();

        markerActivator = FindObjectOfType<MarkerActivator>();
        newButtonPress = false;
    }

    private void Update() {
        
        // Get updated scenario from ScenarioManager
        scenarioManager = FindObjectOfType<ScenarioManagerStartle>();
        if (scenarioManager.newScenario)
        {
            // Debug.Log("New scenario detected. Checking spline container.");
            if (scenarioManager.currentStimulus == "surprise" && scenarioManager.currentScenario == "alert")
            {
                splineContainer = surpriseAlertSplineContainer;
                // Debug.Log("Switching to surpriseAlertSplineContainer.");
            }
            else if (scenarioManager.currentStimulus == "confusion" && scenarioManager.currentScenario == "driving")
            {
                splineContainer = confusionDrivingSplineContainer;
                // Debug.Log("Switching to confusionDrivingSplineContainer.");
            }
            else
            {
                splineContainer = defaultSplineContainer;
                // Debug.Log("Switching to defaultSplineContainer.");
            }
        }

        
        // if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.D)) {
            
        //     // Ensure car is in original position before toggling driving mode
        //     transform.position = originalPos;
        //     transform.rotation = originalRot;
            
            
        //     IsDriving = !IsDriving;
        //     scenarioManager.isScenarioActive = IsDriving;
        // }

        // // Detect steering wheel button presses
        // for (int i = 0; i <= 19; i++)
        // {
        //     KeyCode keyCode = (KeyCode)System.Enum.Parse(typeof(KeyCode), "JoystickButton" + i);
        //     if (Input.GetKeyDown(keyCode))
        //     {
        //         Debug.Log($"Logitech Xbox button {keyCode} pressed!");
        //     }
        // }

        // // Check if start button is pressed
        // // KeyCode keyCode2 = (KeyCode)System.Enum.Parse(typeof(KeyCode), "JoystickButton" + 2);

        // // if (Input.GetKeyDown(keyCode))
        // if (Input.GetKeyDown(KeyCode.JoystickButton2))
        // {
        //     startButtonPress = true;
        //     Debug.Log($"Start button pressed. startButtonPress: {startButtonPress}, resetToNormalConfig: {resetToNormalConfig}, vehicleStopped: {vehicleStopped}");
        // }
    }



    void FixedUpdate()
    {
        
        IsDriving = scenarioManager.isScenarioActive;

        indicatorLeft = false;
        indicatorRight = false;

        if (splineContainer == null || splineContainer.Splines.Count == 0 || !IsDriving) {
            Debug.LogWarning("Spline" + (splineContainer == null ? " is null" : " has no splines. Driving? ") + IsDriving);
            return;
        }

        UpdateConfigBasedOnMarker();

        var spline = splineContainer.Spline;
        bool isClosedLoop = spline.Closed;

        Vector3 vehiclePos = transform.position;
        _closestT = FindClosestTOnSpline(spline, vehiclePos, isClosedLoop);
        _closestPoint = splineContainer.EvaluatePosition(_closestT);

        float lookDistanceNormalized = currentConfig.lookAheadDistance / spline.GetLength();
        _lookT = WrapT(_closestT + lookDistanceNormalized, isClosedLoop);
        _lookPoint = splineContainer.EvaluatePosition(_lookT);

        _toTarget = (_lookPoint - vehiclePos).normalized;
        Vector3 vehicleForward = transform.forward;
        _headingError = Vector3.SignedAngle(vehicleForward, _toTarget, Vector3.up) * Mathf.Deg2Rad;
        
        currentSpeed = rb.velocity.magnitude;
        float targetSpeed = currentConfig.desiredSpeed;

        if (configChanged){
            // Debug.Log($"Current speed: {currentSpeed}, Target speed: {targetSpeed}, Desired speed: , {currentConfig.desiredSpeed}");
            configChanged = false;
        }

        // Determine if we're trying to stop (when target speed is near zero)
        bool tryingToStop = targetSpeed < 0.1f;

        // reset vehicle stopped flag
        if (vehicleStopped && currentSpeed > 4.95f)
        {
            vehicleStopped = false;
            Debug.Log("Vehicle moving again. Resetting vehicleStopped flag.");
        }

        if (tryingToStop)
        {
            // Apply much stronger braking when trying to stop completely
            float emergencyBrakingForce = currentConfig.decelerationRate * 1.0f;
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, emergencyBrakingForce * Time.deltaTime);
            
            // Override PID control with direct maximum braking when speed is still significant
            if (currentSpeed > 0.1f)
            {
                throttleControl = -0.7f; // Maximum braking
                
                // Reset speed PID to prevent integral windup
                speedIntegral = 0f;
                speedPrevError = 0f;
                
                // Skip regular PID calculation for throttle when in emergency stop mode
            }
            else if (currentSpeed <= 0.1f)
            {
                // When nearly stopped, set speed to exactly zero and apply full brake
                currentSpeed = 0f;
                throttleControl = 0f;
                
                // Apply parking brake effect by adding resistance
                rb.drag = 10f; // Temporarily increase drag to simulate parking brake
            }
        }
        else
        {
            // Reset drag when not stopping
            rb.drag = 0.01f; // Use your normal drag value here
            
            // Normal acceleration/deceleration logic
            if (targetSpeed > currentSpeed)
            {
                // Acceleration
                currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, currentConfig.accelerationRate * Time.deltaTime);
            }
            else if (targetSpeed < currentSpeed)
            {
                // Normal deceleration
                currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, -currentConfig.decelerationRate * Time.deltaTime);
            }
            
            // Calculate speed error and use PID for normal driving
            float speedError = targetSpeed - currentSpeed;
            throttleControl = PIDControl(speedError, ref speedIntegral, ref speedPrevError, 
                                            currentConfig.Kp_speed, currentConfig.Ki_speed, currentConfig.Kd_speed);
            throttleControl = Mathf.Clamp(throttleControl, -1f, 1f);
        }

        // Steering control is always calculated (even when stopping)
        steeringControl = PIDControl(_headingError, ref steeringIntegral, ref steeringPrevError, 
                                        currentConfig.Kp_steering, currentConfig.Ki_steering, currentConfig.Kd_steering);
        steeringControl = Mathf.Clamp(steeringControl, -1f, 1f);

        // Check for left/right turn indicators based on steering control
        if (steeringControl > 0.1f)
        {
            indicatorRight = true;
            // Debug.Log($"Heading error: {_headingError}, Steering: {steeringControl}, Throttle: {throttleControl}, Speed: {currentSpeed}, Target Speed: {targetSpeed}");

        }
        else if (steeringControl < -0.1f)
        {
            indicatorLeft = true;
            // Debug.Log($"Heading error: {_headingError}, Steering: {steeringControl}, Throttle: {throttleControl}, Speed: {currentSpeed}, Target Speed: {targetSpeed}");
        }


        // Apply the calculated controls to the vehicle
        vehicleController.SteeringInput = steeringControl;
        vehicleController.ThrottleInput = throttleControl;
        // vehicleController.tempLeft = indicatorLeft;
        // vehicleController.tempRight = indicatorRight;

        // check if indicator is active and if indicatorLeft or indicatorRight is still true or false after 5 seconds (enough time to start making a turn?)
        if ((!newButtonPress) && (vehicleController.tempLeft || vehicleController.tempRight))
        {
            newButtonPress = true;
            buttonPressTime = Time.time;
            Debug.Log("Indicator active.");
        }
        
        if (newButtonPress && Time.time - buttonPressTime > 5f)
        {
            vehicleController.tempLeft = indicatorLeft;
            vehicleController.tempRight = indicatorRight;

            if (!indicatorLeft && !indicatorRight)
            {
                newButtonPress = false;
                Debug.Log("Indicator off.");
            }
        }

        // // Add debug logging when in stopping mode
        // if (tryingToStop && currentSpeed > 0.05f)
        // {
        //     Debug.Log($"Emergency Stop: Speed={currentSpeed:F2}, Throttle={throttleControl:F2}, BrakingForce={currentConfig.decelerationRate * 5.0f:F2}");
        // }


        if (checkIgnitionPressFlag){
            if (Input.GetKeyDown(KeyCode.JoystickButton10))
            {
                ignitionButtonPressNum++;
                Debug.Log("Ignition button press count: " + ignitionButtonPressNum);
            }

            
            if (ignitionButtonPressNum > 20){
                checkIgnitionPressFlag = false;
                ignitionButtonPressNum = 0;
                CommunicationManager.Instance.SendMessageToServer("driving_frustration_stop");
                ResetToEcoConfig();
            }
        }


        // Deactivate Scenario if trial ended and vehicle has stopped
        if (markerActivator.endTrial && currentSpeed < 0.1f)
        {
            scenarioManager.isScenarioActive = false;
            Debug.Log("Vehicle stopped. Deactivating scenario.");
        }

    }


    public void ResetEgoCar()
    {
        // First, temporarily disable the FixedUpdate logic to prevent any position calculations
        bool wasDriving = IsDriving;
        IsDriving = false;
        
        // Reset position and rotation to original values
        transform.position = originalPos;
        transform.rotation = originalRot;
        
        // Reset vehicle controller parameters
        currentSpeed = 0f;
        throttleControl = 0f;
        steeringControl = 0f;
        vehicleController.ThrottleInput = 0f;
        vehicleController.SteeringInput = 0f;
        
        // Reset PID controllers
        steeringIntegral = 0f;
        steeringPrevError = 0f;
        speedIntegral = 0f;
        speedPrevError = 0f;

        // Completely reset spline following states
        _closestT = 0f;
        _closestPoint = originalPos; // Use a valid position instead of zero
        _lookT = 0f;
        _lookPoint = originalPos + transform.forward * 10f; // Look ahead in current direction
        _toTarget = transform.forward; // Default to current forward
        _headingError = 0f;
        
        // Force recalculation of spline positioning
        initializedClosestT = false;
        lastClosestT = 0f;
        
        // Reset behavior flags
        vehicleStopped = false;
        resetToNormalConfig = false;
        configChanged = false;
        checkIgnitionPressFlag = false;
        ignitionButtonPressNum = 0;
        
        // Reset physics state
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            // Also reset any forces and torques
            rb.ResetCenterOfMass();
            rb.ResetInertiaTensor();
        }
        
        // Set to default configuration
        currentConfig = ecoConfig;
        driveMode = "eco";
        
        // Wait one frame before allowing driving again
        StartCoroutine(ReenableDrivingAfterReset(wasDriving));
        
        Debug.Log("Ego car reset to original position and state with comprehensive reset.");

        if (configChangeCoroutine != null)
        {
            StopCoroutine(configChangeCoroutine);
        }
        configChangePending = false;
    }

    private IEnumerator ReenableDrivingAfterReset(bool shouldDrive)
    {
        // Wait for two physics updates to ensure everything is settled
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        
        // After waiting, if we should be driving, reenable it
        IsDriving = shouldDrive;
    }

    private void UpdateConfigBasedOnMarker()
    {
        
        if (markerActivator.endTrial){
            ScheduleConfigChange(stopConfig, "stop");
            // ChangeConfigImmediately(stopConfig, "stop");
            // vehicleStopped = true;
            Debug.Log("End trial marker detected. Changing to stopConfig.");
            return;
        }

        if (scenarioManager.isScenarioReady == false)
        {
            ScheduleConfigChange(stopConfig, "stop");
            // ChangeConfigImmediately(stopConfig, "stop");
            // vehicleStopped = true;
            Debug.Log("Simulation Pause Message received. Changing to stopConfig.");
            return;
        }
        
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 5f);
        foreach (var hitCollider in hitColliders)
        {   
            if (hitCollider.CompareTag("NormalMarker"))
            {
                ScheduleConfigChange(normalConfig, "normal");
                Debug.Log("Normal marker detected. Scheduling change to normalConfig.");
                break;
            }
            else if (hitCollider.CompareTag("SportyMarker"))
            {
                ScheduleConfigChange(sportyConfig, "sporty");
                Debug.Log("Sporty marker detected. Scheduling change to sportyConfig.");
                break;
            }
            else if (hitCollider.CompareTag("EcoMarker"))
            {
                ScheduleConfigChange(ecoConfig, "eco");
                Debug.Log("Eco marker detected. Scheduling change to ecoConfig.");
                break;
            }
            else if (hitCollider.CompareTag("StopMarker") && !vehicleStopped)
            {
                // For stop markers, still make the change immediate
                ScheduleConfigChange(stopConfig, "stop");
                // ChangeConfigImmediately(stopConfig, "stop");
                // vehicleStopped = true;
                Debug.Log("Stop marker detected. Changing to stopConfig.");
                // checkIgnitionPressFlag = true;
                // ignitionButtonPressNum = 0;

                if (hitCollider.name.Contains("Frustration Driving 3")){
                    Debug.Log("Frustration Driving 3 marker detected.");
                    checkIgnitionPressFlag = true;
                    ignitionButtonPressNum = 0;
                }
                break;
            }
            else if ((hitCollider.CompareTag("StopSign") || (hitCollider.CompareTag("AdditionalStopSign"))) && !vehicleStopped)
            {
                // For stop markers, still make the change immediate
                // ScheduleConfigChange(stopConfig, "stop");
                ChangeConfigImmediately(stopConfig, "stop");
                vehicleStopped = true;
                Debug.Log("Stop sign detected. Changing to stopConfig.");
                Invoke("ResetToEcoConfig", stopSignWaitTime);
                break;
            }
        }
    }
    
    private void ChangeConfigImmediately(SO_AVFollowSplineConfig newConfig, string mode)
    {
        // Cancel any pending config changes
        if (configChangeCoroutine != null)
        {
            StopCoroutine(configChangeCoroutine);
            configChangePending = false;
        }
        
        currentConfig = newConfig;
        configChanged = true;
        driveMode = mode;
        Debug.Log($"Config changed immediately to {mode}. Desired speed: {currentConfig.desiredSpeed}");
    }

    private void ScheduleConfigChange(SO_AVFollowSplineConfig newConfig, string mode)
    {
        // If we're already transitioning to this config, don't restart the coroutine
        if (configChangePending && pendingConfig == newConfig)
            return;
            
        // Cancel any existing pending changes
        if (configChangeCoroutine != null)
        {
            StopCoroutine(configChangeCoroutine);
        }
        
        // Schedule the new change
        pendingConfig = newConfig;
        configChangePending = true;
        configChangeCoroutine = StartCoroutine(DelayedConfigChange(newConfig, mode));
    }

    private IEnumerator DelayedConfigChange(SO_AVFollowSplineConfig newConfig, string mode)
    {
        Debug.Log($"Waiting {configChangeDelay} seconds to change to {mode} config...");
        yield return new WaitForSeconds(configChangeDelay);
        
        currentConfig = newConfig;
        configChanged = true;
        driveMode = mode;
        configChangePending = false;
        
        if (mode == "stop")
        {
            vehicleStopped = true;
        }

        Debug.Log($"Config changed to {mode} after delay. Desired speed: {currentConfig.desiredSpeed}");
    }
        
    private void ResetToNormalConfig()
    {
        currentConfig = normalConfig;
        configChanged = true;
        // Debug.Log("Reset to normal config after 20 seconds.");
        Debug.Log("Desired speed: " + currentConfig.desiredSpeed);
    }

    private void ResetToEcoConfig()
    {
        currentConfig = ecoConfig;
        configChanged = true;
        // Debug.Log("Reset to normal config after 20 seconds.");
        Debug.Log("Desired speed: " + currentConfig.desiredSpeed);
        // vehicleStopped = false;
    }

    private void checkNButtonPresses()
    {
        if (Input.GetKeyDown(KeyCode.JoystickButton10))
        {
            Debug.Log("Ignition button pressed.");
            checkIgnitionPressFlag = false;
        }
    }


    private float PIDControl(float error, ref float integral, ref float prevError, float Kp, float Ki, float Kd)
    {
        float dt = Time.fixedDeltaTime;

        integral += error * dt;
        float derivative = (error - prevError) / dt;
        float output = Kp * error + Ki * integral + Kd * derivative;
        prevError = error;

        return output;
    }

    private float FindClosestTOnSpline(Spline spline, Vector3 point, bool isClosedLoop)
    {
        int fullSampleCount = 200; 
        int localSampleCount = 50;
        float searchRadius = 0.05f;

        float DistAtT(float t)
        {
            t = WrapT(t, isClosedLoop);
            Vector3 splinePoint = splineContainer.EvaluatePosition(t);
            return Vector3.SqrMagnitude(splinePoint - point);
        }

        if (!initializedClosestT)
        {
            float closestT = 0f;
            float closestDist = Mathf.Infinity;

            for (int i = 0; i <= fullSampleCount; i++)
            {
                float t = i / (float)fullSampleCount;
                float dist = DistAtT(t);
                Vector3 tangent = (Vector3)math.normalize(spline.EvaluateTangent(t));
                float dotProduct = Vector3.Dot(tangent, transform.forward);

                if (dist < closestDist && dotProduct > 0f) 
                {
                    closestDist = dist;
                    closestT = t;
                }

            }

            lastClosestT = closestT;
            initializedClosestT = true;
            return closestT;
        }
        else
        {
            float startT = lastClosestT - searchRadius;
            float endT = lastClosestT + searchRadius;

            float closestT = lastClosestT;
            float closestDist = DistAtT(lastClosestT);

            for (int i = 0; i <= localSampleCount; i++)
            {
                float lerpT = Mathf.Lerp(startT, endT, i / (float)localSampleCount);
                float dist = DistAtT(lerpT);
                Vector3 tangent = (Vector3)math.normalize(spline.EvaluateTangent(lerpT));
                float dotProduct = Vector3.Dot(tangent, transform.forward);

                if (dist < closestDist && dotProduct > 0f)
                {
                    closestDist = dist;
                    closestT = WrapT(lerpT, isClosedLoop);
                }
            }

            lastClosestT = closestT;
            return closestT;
        }
    }

    private float WrapT(float t, bool isClosedLoop)
    {
        if (isClosedLoop)
        {
            t = t % 1f;
            if (t < 0f) t += 1f;
        }
        else
        {
            t = Mathf.Clamp01(t);
        }

        return t;
    }

    private void OnDrawGizmos() 
    {
        if (splineContainer != null && splineContainer.Spline != null && IsDriving) 
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_closestPoint, 0.5f);

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(_lookPoint, 0.5f);

            Vector3 errorVector = Quaternion.AngleAxis(_headingError * Mathf.Rad2Deg, Vector3.up) * transform.forward;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + errorVector * 5f);
        }
    }
} 