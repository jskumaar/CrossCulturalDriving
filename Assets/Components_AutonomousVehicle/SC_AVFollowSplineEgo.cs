using System;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

public class SC_AVFollowSplineEgo : MonoBehaviour
{
    // public SplineContainer defaultSplineContainer;
    // public SplineContainer surpriseAlertSplineContainer;
    // private SplineContainer splineContainer;

    public SplineContainer splineContainer;
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


    void Start()
    {
        rb = vehicleController.GetComponent<Rigidbody>();
        originalPos = transform.position;
        originalRot = transform.rotation;
        currentConfig = ecoConfig;
        driveMode = "eco";
        // splineContainer = defaultSplineContainer;
        scenarioManager = FindObjectOfType<ScenarioManagerStartle>();

        markerActivator = FindObjectOfType<MarkerActivator>();
    }

    private void Update() {
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.D)) {
            
            // Ensure car is in original position before toggling driving mode
            transform.position = originalPos;
            transform.rotation = originalRot;
            
            
            IsDriving = !IsDriving;
            scenarioManager.isScenarioActive = IsDriving;
        }

        

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

        if (splineContainer == null || splineContainer.Splines.Count == 0 || !IsDriving) {
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
            Debug.Log($"Current speed: {currentSpeed}, Target speed: {targetSpeed}, Desired speed: , {currentConfig.desiredSpeed}");
            configChanged = false;
        }

        // Gradually adjust speed based on acceleration or deceleration rate
        if (targetSpeed > currentSpeed + 2)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, -currentConfig.decelerationRate * Time.deltaTime);
        }
        else if (targetSpeed < currentSpeed - 2)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, currentConfig.decelerationRate * Time.deltaTime);
        }

        float speedError = targetSpeed - currentSpeed;

        steeringControl = PIDControl(_headingError, ref steeringIntegral, ref steeringPrevError, 
                                           currentConfig.Kp_steering, currentConfig.Ki_steering, currentConfig.Kd_steering);
        steeringControl = Mathf.Clamp(steeringControl, -1f, 1f);

        throttleControl = PIDControl(speedError, ref speedIntegral, ref speedPrevError, 
                                           currentConfig.Kp_speed, currentConfig.Ki_speed, currentConfig.Kd_speed);
        throttleControl = Mathf.Clamp(throttleControl, -1f, 1f);

        vehicleController.SteeringInput = steeringControl;
        vehicleController.ThrottleInput = throttleControl;

        // if (currentConfig.desiredSpeed == 0){
        //     Debug.Log($"Steering: {steeringControl}, Throttle: {throttleControl}, Speed: {currentSpeed}, Target Speed: {targetSpeed}");
        // }

        if (checkIgnitionPressFlag){
            if (Input.GetKeyDown(KeyCode.JoystickButton10))
            {
                ignitionButtonPressNum++;
                Debug.Log("Ignition button press count: " + ignitionButtonPressNum);
            }

            
            if (ignitionButtonPressNum > 10){
                checkIgnitionPressFlag = false;
                ignitionButtonPressNum = 0;
                ResetToNormalConfig();
            }
        }


        // Deactivate Scenario if trial ended and vehicle has stopped
        if (markerActivator.endTrial && currentSpeed < 0.1f)
        {
            scenarioManager.isScenarioActive = false;
            Debug.Log("Vehicle stopped. Deactivating scenario.");
        }

    }


    // private void UpdateSplineBasedOnScenario()
    // {
    //     // If no scenarioManager found, skip
    //     if (scenarioManager == null) return;

    //     // Convert scenario name to lowercase to avoid case-sensitivity issues 
    //     string scenarioName = scenarioManager.currentScenario.ToLower();

    //     // If scenario name includes "surprise" or "alert", switch to that SplineContainer
    //     if (scenarioName.Contains("surprise") && scenarioName.Contains("alert"))
    //     {
    //         splineContainer = surpriseAlertSplineContainer;
    //     }

    // }

    public void ResetEgoCar()
    {
        // Reset position and rotation to original values
        transform.position = originalPos;
        transform.rotation = originalRot;
        
        // Reset vehicle controller parameters
        currentSpeed = 0f;
        throttleControl = 0f;
        vehicleController.ThrottleInput = 0f;
        vehicleController.SteeringInput = 0f;
        
        // Reset PID controllers
        steeringIntegral = 0f;
        steeringPrevError = 0f;
        speedIntegral = 0f;
        speedPrevError = 0f;
        
        // Reset other state variables
        initializedClosestT = false;
        lastClosestT = 0f;
        vehicleStopped = false;
        resetToNormalConfig = false;
        
        Debug.Log("Ego car reset to original position and state.");
    }


    private void UpdateConfigBasedOnMarker()
    {
        
        if (markerActivator.endTrial){
            currentConfig = stopConfig;
            configChanged = true;
            vehicleStopped = true;
            driveMode = "stop";
            Debug.Log("End trial marker detected. Changing to stopConfig.");
            Debug.Log("Desired speed: " + currentConfig.desiredSpeed);
            return;
        }
        
        
        
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 5f);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("NormalMarker"))
            {
                currentConfig = normalConfig;
                configChanged = true;
                driveMode = "normal";
                Debug.Log("Normal marker detected. Changing to normalConfig.");
                Debug.Log("Desired speed: " + currentConfig.desiredSpeed);
                break;
            }
            else if (hitCollider.CompareTag("SportyMarker"))
            {
                currentConfig = sportyConfig;
                configChanged = true;
                driveMode = "sporty";
                Debug.Log("Sporty marker detected. Changing to sportyConfig.");
                Debug.Log("Desired speed: " + currentConfig.desiredSpeed);
                break;
            }
            else if (hitCollider.CompareTag("EcoMarker"))
            {
                currentConfig = ecoConfig;
                configChanged = true;
                driveMode = "eco";
                Debug.Log("Eco marker detected. Changing to ecoConfig.");
                Debug.Log("Desired speed: " + currentConfig.desiredSpeed);
                break;
            }
            else if (hitCollider.CompareTag("StopMarker") && !vehicleStopped)
            {
                currentConfig = stopConfig;
                configChanged = true;
                vehicleStopped = true;
                driveMode = "stop";
                Debug.Log("Stop marker detected. Changing to stopConfig.");
                Debug.Log("Desired speed: " + currentConfig.desiredSpeed);
                // For Debugging
                // Invoke("ResetToNormalConfig", 10f);
                checkIgnitionPressFlag = true;
                ignitionButtonPressNum = 0;

                if (hitCollider.name.Contains("Frustration Driving 3")){
                    // Invoke("ResetToNormalConfig", 40f); // Change back to normal config after 20 seconds
                    Debug.Log("Frustration Driving 3 marker detected. Changing to stopConfig.");
                    checkIgnitionPressFlag = true;
                    ignitionButtonPressNum = 0;
                }

                break;
            }
            // else if (hitCollider.CompareTag("InteractionMarkers") && hitCollider.name.Contains("Frustration Alert 3") && !vehicleStopped)
            // {
            //     currentConfig = stopConfig;
            //     configChanged = true;
            //     vehicleStopped = true;
            //     Debug.Log("Frustration Alert 3 marker detected. Changing to stopConfig.");
            //     Debug.Log("Desired speed: " + currentConfig.desiredSpeed);
            //     Invoke("ResetToNormalConfig", 20f); // Change back to normal config after 20 seconds
            //     break;
            // }
        }

        // // If stopped. Reset to normal config after 20 seconds.
        // if (vehicleStopped && startButtonPress && !resetToNormalConfig)
        // {
        //     Invoke("ResetToNormalConfig", 10f); // Change back to normal config after 10 seconds
        //     startButtonPress = false;
        //     resetToNormalConfig = true;
        // }
    }

    private void ResetToNormalConfig()
    {
        currentConfig = normalConfig;
        // Debug.Log("Reset to normal config after 20 seconds.");
        Debug.Log("Desired speed: " + currentConfig.desiredSpeed);
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


// using System;
// using UnityEngine;
// using UnityEngine.Splines;
// using Unity.Mathematics;

// public class SC_AVFollowSplineEgo : MonoBehaviour
// {
//     public SplineContainer splineContainer;
//     public NetworkVehicleController vehicleController; 
//     public SO_AVFollowSplineConfig normalConfig;
//     public SO_AVFollowSplineConfig sportyConfig;
//     public SO_AVFollowSplineConfig ecoConfig;
//     public SO_AVFollowSplineConfig stopConfig;

//     private SO_AVFollowSplineConfig currentConfig;

//     private float steeringIntegral = 0f;
//     private float steeringPrevError = 0f;

//     private float speedIntegral = 0f;
//     private float speedPrevError = 0f;
    
//     private Rigidbody rb;

//     private float _closestT;
//     private Vector3 _closestPoint;
//     private float _lookT;
//     private Vector3 _lookPoint;
//     private Vector3 _toTarget;
//     private float _headingError;

//     public bool IsDriving = false;
//     private bool configChanged = false;

//     private bool initializedClosestT = false;
//     private float lastClosestT = 0f;

//     void Start()
//     {
//         rb = vehicleController.GetComponent<Rigidbody>();
//         currentConfig = normalConfig;
//     }

//     private void Update() {
//         if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.D)) {
//             IsDriving = !IsDriving;
//         }
//     }

//     void FixedUpdate()
//     {
//         if (splineContainer == null || splineContainer.Splines.Count == 0 || !IsDriving) {
//             return;
//         }

//         configChanged = false;

//         UpdateConfigBasedOnMarker();

//         var spline = splineContainer.Spline;
//         bool isClosedLoop = spline.Closed;

//         Vector3 vehiclePos = transform.position;
//         _closestT = FindClosestTOnSpline(spline, vehiclePos, isClosedLoop);
//         _closestPoint = splineContainer.EvaluatePosition(_closestT);

//         float lookDistanceNormalized = currentConfig.lookAheadDistance / spline.GetLength();
//         _lookT = WrapT(_closestT + lookDistanceNormalized, isClosedLoop);
//         _lookPoint = splineContainer.EvaluatePosition(_lookT);

//         _toTarget = (_lookPoint - vehiclePos).normalized;
//         Vector3 vehicleForward = transform.forward;
//         _headingError = Vector3.SignedAngle(vehicleForward, _toTarget, Vector3.up) * Mathf.Deg2Rad;
        
//         float currentSpeed = rb.velocity.magnitude;
//         float targetSpeed = currentConfig.desiredSpeed;

//         if (configChanged){
//             Debug.Log($"Current speed: {currentSpeed}, Target speed: {targetSpeed}, Desired speed: , {currentConfig.desiredSpeed}");
//         }

//         // Gradually reduce speed when targetSpeed is zero
//         if (targetSpeed == 0 && currentSpeed > 1f)
//         {
//             targetSpeed = Mathf.Max(0, currentSpeed - Time.deltaTime * currentConfig.decelerationRate);
//         }

//         float speedError = targetSpeed - currentSpeed;

//         float steeringControl = PIDControl(_headingError, ref steeringIntegral, ref steeringPrevError, 
//                                            currentConfig.Kp_steering, currentConfig.Ki_steering, currentConfig.Kd_steering);
//         steeringControl = Mathf.Clamp(steeringControl, -1f, 1f);

//         float throttleControl = PIDControl(speedError, ref speedIntegral, ref speedPrevError, 
//                                            currentConfig.Kp_speed, currentConfig.Ki_speed, currentConfig.Kd_speed);
//         throttleControl = Mathf.Clamp(throttleControl, -1f, 1f);

//         if (configChanged)
//         {
//             Debug.Log($"Steering: {steeringControl}, Throttle: {throttleControl}, Speed: {currentSpeed}, Target Speed: {targetSpeed}");
//         }

//         vehicleController.SteeringInput = steeringControl;
//         vehicleController.ThrottleInput = throttleControl;
//     }


//     private void UpdateConfigBasedOnMarker()
//     {
//         Collider[] hitColliders = Physics.OverlapSphere(transform.position, 5f);
//         foreach (var hitCollider in hitColliders)
//         {
//             // if (hitCollider.CompareTag("NormalMarker"))
//             // {
//             //     currentConfig = normalConfig;
//             //     break;
//             // }
//             // else if (hitCollider.CompareTag("SportyMarker"))
//             // {
//             //     currentConfig = sportyConfig;
//             //     break;
//             // }
//             // else if (hitCollider.CompareTag("EcoMarker"))
//             // {
//             //     currentConfig = ecoConfig;
//             //     break;
//             // }
//             // else if (hitCollider.CompareTag("StopMarker"))
//             // {
//             //     currentConfig = stopConfig;
//             //     break;
//             // }
//             if (hitCollider.CompareTag("InteractionMarkers") && hitCollider.name.Contains("Frustration Alert 3"))
//             {
//                 currentConfig = stopConfig;
//                 configChanged = true;

//                 Debug.Log("Frustration Alert 3 marker detected. Changing to stopConfig.");
//                 Invoke("ResetToNormalConfig", 5f); // Change back to normal config after 20 seconds

//                 break;
//             }
//         }
//     }

//     private void ResetToNormalConfig()
//     {
//         currentConfig = normalConfig;
//         Debug.Log("Reset to normal config after 20 seconds.");
//         Debug.Log("Desired speed: " + currentConfig.desiredSpeed);
//     }


//     private float PIDControl(float error, ref float integral, ref float prevError, float Kp, float Ki, float Kd)
//     {
//         float dt = Time.fixedDeltaTime;

//         integral += error * dt;
//         float derivative = (error - prevError) / dt;
//         float output = Kp * error + Ki * integral + Kd * derivative;
//         prevError = error;

//         return output;
//     }

//     private float FindClosestTOnSpline(Spline spline, Vector3 point, bool isClosedLoop)
//     {
//         int fullSampleCount = 200; 
//         int localSampleCount = 50;
//         float searchRadius = 0.05f;

//         float DistAtT(float t)
//         {
//             t = WrapT(t, isClosedLoop);
//             Vector3 splinePoint = splineContainer.EvaluatePosition(t);
//             return Vector3.SqrMagnitude(splinePoint - point);
//         }

//         if (!initializedClosestT)
//         {
//             float closestT = 0f;
//             float closestDist = Mathf.Infinity;

//             for (int i = 0; i <= fullSampleCount; i++)
//             {
//                 float t = i / (float)fullSampleCount;
//                 float dist = DistAtT(t);
//                 Vector3 tangent = (Vector3)math.normalize(spline.EvaluateTangent(t));
//                 float dotProduct = Vector3.Dot(tangent, transform.forward);

//                 if (dist < closestDist && dotProduct > 0f) 
//                 {
//                     closestDist = dist;
//                     closestT = t;
//                 }

//             }

//             lastClosestT = closestT;
//             initializedClosestT = true;
//             return closestT;
//         }
//         else
//         {
//             float startT = lastClosestT - searchRadius;
//             float endT = lastClosestT + searchRadius;

//             float closestT = lastClosestT;
//             float closestDist = DistAtT(lastClosestT);

//             for (int i = 0; i <= localSampleCount; i++)
//             {
//                 float lerpT = Mathf.Lerp(startT, endT, i / (float)localSampleCount);
//                 float dist = DistAtT(lerpT);
//                 Vector3 tangent = (Vector3)math.normalize(spline.EvaluateTangent(lerpT));
//                 float dotProduct = Vector3.Dot(tangent, transform.forward);

//                 if (dist < closestDist && dotProduct > 0f)
//                 {
//                     closestDist = dist;
//                     closestT = WrapT(lerpT, isClosedLoop);
//                 }
//             }

//             lastClosestT = closestT;
//             return closestT;
//         }
//     }

//     private float WrapT(float t, bool isClosedLoop)
//     {
//         if (isClosedLoop)
//         {
//             t = t % 1f;
//             if (t < 0f) t += 1f;
//         }
//         else
//         {
//             t = Mathf.Clamp01(t);
//         }

//         return t;
//     }

//     private void OnDrawGizmos() 
//     {
//         if (splineContainer != null && splineContainer.Spline != null && IsDriving) 
//         {
//             Gizmos.color = Color.red;
//             Gizmos.DrawSphere(_closestPoint, 0.5f);

//             Gizmos.color = Color.green;
//             Gizmos.DrawSphere(_lookPoint, 0.5f);

//             Vector3 errorVector = Quaternion.AngleAxis(_headingError * Mathf.Rad2Deg, Vector3.up) * transform.forward;

//             Gizmos.color = Color.red;
//             Gizmos.DrawLine(transform.position, transform.position + errorVector * 5f);
//         }
//     }
// } 