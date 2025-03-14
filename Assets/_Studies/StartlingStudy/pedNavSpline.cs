// using UnityEngine;
// using Unity.Splines;
// using Unity.Mathematics; // Required for some spline operations

// public class BuiltInSplineFollower : MonoBehaviour
// {
//     public SplineContainer splineContainer; // Reference to the SplineContainer with your spline.
//     public float speed = 1f; // Speed multiplier for spline traversal.

//     private float t = 0f;
//     private bool isMoving = false;

//     // Trigger the movement along the spline.
//     public void TriggerMovement()
//     {
//         if (splineContainer == null)
//         {
//             Debug.LogError("SplineContainer is not assigned.");
//             return;
//         }
//         t = 0f;
//         isMoving = true;
//     }

//     void Update()
//     {
//         if (!isMoving)
//             return;

//         t += Time.deltaTime * speed;
//         if (t >= 1f)
//         {
//             t = 1f;
//             isMoving = false;
//         }
        
//         // Evaluate the spline position at normalized parameter t.
//         // Note: Ensure you have the Unity.Splines package installed.
//         transform.position = SplineUtility.EvaluatePosition(splineContainer.Spline, t);
//     }
// }