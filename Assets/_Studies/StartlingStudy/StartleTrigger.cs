// using UnityEngine;

// public class StartleTrigger : MonoBehaviour
// {
//     public string triggerID;
//     private bool hasCarEntered = false;  // Ensures one enter event per trigger

//     void OnTriggerEnter(Collider other)
//     {
//         Debug.Log($"Marker {triggerID} detecting entry of: {other.gameObject.name} with tag {other.gameObject.tag}");

//         if (other.CompareTag("Player") && !hasCarEntered)
//         {
//             Debug.Log($"Car entered trigger zone: {triggerID}");
//             hasCarEntered = true;
            
//             // VehicleDataBroadcaster.Instance.TriggerEntered(triggerID); // Notify broadcaster
            
//             // Directly send message to the server
//             VehicleDataBroadcaster.Instance.SendMessageToServer($"TRIGGER_ENTERED: {triggerID}");


//             NavigationScreenSS.TriggerIconChange(triggerID); // Notify NavigationScreenSS to update the GPS screen

//             // SC_AVFollowSplineEgo.TriggerDrivingModeChange(triggerID); // Notify SC_AVFollowSpline to change driving mode

//         }
//     }

//     void OnTriggerExit(Collider other)
//     {
//         Debug.Log($"Marker {triggerID} detecting exit of: {other.gameObject.name} with tag {other.gameObject.tag}");

//         if (other.CompareTag("Player") && hasCarEntered)
//         {
//             Debug.Log($"Car exited trigger zone: {triggerID}");
//             hasCarEntered = false;
            
//             // VehicleDataBroadcaster.Instance.TriggerExited(triggerID); // Notify broadcaster
            
//             // Directly send message to the server
//             VehicleDataBroadcaster.Instance.SendMessageToServer($"TRIGGER_EXITED: {triggerID}");
        
//         }
//     }
// }
