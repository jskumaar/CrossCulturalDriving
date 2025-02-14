// using System;
// using UnityEngine;
// using UnityEngine.Splines;
// using Unity.Mathematics;


// public class SC_AVFollowSplineEgo : MonoBehaviour
// {
//     public SplineContainer splineContainer;
//     public NetworkVehicleController vehicleController; 
//     // public SO_AVFollowSplineConfig config;

//     public SO_AVFollowSplineConfig ecoConfig;
//     public SO_AVFollowSplineConfig sportyConfig;
//     private SO_AVFollowSplineConfig currentConfig;
    
//     private float steeringIntegral = 0f;
//     private float steeringPrevError = 0f;

//     private float speedIntegral = 0f;
//     private float speedPrevError = 0f;
    
//     private Rigidbody rb;

//     // for gizmos
//     private float _closestT;
//     private Vector3 _closestPoint;
//     private float _lookT;
//     private Vector3 _lookPoint;
//     private Vector3 _toTarget;
//     private float _headingError;

//     public bool IsDriving = false;

//     // variable for maintain continuity of the closest point
//     private bool initializedClosestT = false;
//     private float lastClosestT = 0f;

//     void Start()
//     {
//         rb = vehicleController.GetComponent<Rigidbody>();
//         currentConfig = ecoConfig; // Start in Eco Mode
//     }

//     // Change the driving mode based on the trigger ID
//     public static void TriggerDrivingModeChange(string triggerID)
//     {
//         if (triggerID.Contains("EcoMarker"))
//         {
//             currentConfig = ecoConfig;
//             Debug.Log("Switched to Eco Mode");
//         }
//         else if (triggerID.Contains("SportyMarker"))
//         {
//             currentConfig = sportyConfig;
//             Debug.Log("Switched to Sporty Mode");
//         }
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

//         var spline = splineContainer.Spline;
//         bool isClosedLoop = spline.Closed;

//         Vector3 vehiclePos = transform.position;
//         _closestT = FindClosestTOnSpline(spline, vehiclePos, isClosedLoop);
//         _closestPoint = splineContainer.EvaluatePosition(_closestT);

//         float lookDistanceNormalized = currentConfig.lookAheadDistance / spline.GetLength();
        
//         // tentative wrapping solution
//         _lookT = WrapT(_closestT + lookDistanceNormalized, isClosedLoop);
//         _lookPoint = splineContainer.EvaluatePosition(_lookT);

//         _toTarget = (_lookPoint - vehiclePos).normalized;
//         Vector3 vehicleForward = transform.forward;
//         _headingError = Vector3.SignedAngle(vehicleForward, _toTarget, Vector3.up) * Mathf.Deg2Rad;
        
//         float currentSpeed = rb.velocity.magnitude;
//         float speedError = currentConfig.desiredSpeed - currentSpeed;

//         // Steering PID
//         float steeringControl = PIDControl(_headingError, ref steeringIntegral, ref steeringPrevError, 
//                                            currentConfig.Kp_steering, currentConfig.Ki_steering, currentConfig.Kd_steering);
//         steeringControl = Mathf.Clamp(steeringControl, -1f, 1f);

//         // Speed PID
//         float throttleControl = PIDControl(speedError, ref speedIntegral, ref speedPrevError, 
//                                            currentConfig.Kp_speed, currentConfig.Ki_speed, currentConfig.Kd_speed);
//         throttleControl = Mathf.Clamp(throttleControl, -1f, 1f);

//         vehicleController.SteeringInput = steeringControl;
//         vehicleController.ThrottleInput = throttleControl;
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
//         float searchRadius = 0.05f; // to accomodate self intersection like 8 shape course

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
                
//                 // Check for closest distance only
//                 // if (dist < closestDist)
//                 // {
//                 //     closestDist = dist;
//                 //     closestT = t;
//                 // }

//                 // Check for closest distance and direction of spline
//                 Vector3 tangent = (Vector3)math.normalize(spline.EvaluateTangent(t));
//                 float dotProduct = Vector3.Dot(tangent, transform.forward);

//                 // Ensure the tangent direction aligns with vehicle's forward direction
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

//             if (isClosedLoop)
//             {
//                 for (int i = 0; i <= localSampleCount; i++)
//                 {
//                     float lerpT = Mathf.Lerp(startT, endT, i / (float)localSampleCount);
//                     float dist = DistAtT(lerpT);
//                     // Check for closest distance only
//                     // if (dist < closestDist)
//                     // {
//                     //     closestDist = dist;
//                     //     closestT = WrapT(lerpT, isClosedLoop);
//                     // }

//                     // Check for closest distance and direction of spline
//                     Vector3 tangent = (Vector3)math.normalize(spline.EvaluateTangent(lerpT));
//                     float dotProduct = Vector3.Dot(tangent, transform.forward);

//                     if (dist < closestDist && dotProduct > 0f)
//                     {
//                         closestDist = dist;
//                         closestT = WrapT(lerpT, isClosedLoop);
//                     }
//                 }
//             }
//             else
//             {
//                 float clampedStart = Mathf.Clamp01(startT);
//                 float clampedEnd = Mathf.Clamp01(endT);

//                 for (int i = 0; i <= localSampleCount; i++)
//                 {
//                     float lerpT = Mathf.Lerp(clampedStart, clampedEnd, i / (float)localSampleCount);
//                     float dist = DistAtT(lerpT);
//                     // Check for the closest distance only
//                     // if (dist < closestDist)
//                     // {
//                     //     closestDist = dist;
//                     //     closestT = lerpT;
//                     // }

//                     // Check for closest distance and direction of spline
//                     Vector3 tangent = (Vector3)math.normalize(spline.EvaluateTangent(lerpT));
//                     float dotProduct = Vector3.Dot(tangent, transform.forward);

//                     if (dist < closestDist && dotProduct > 0f)
//                     {
//                         closestDist = dist;
//                         closestT = lerpT;
//                     }
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
