using UnityEngine;

public class StartleTrigger : MonoBehaviour
{
    public string triggerID;
    private bool hasCarEntered = false;  // Ensures one enter event per trigger

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasCarEntered)
        {
            Debug.Log($"Car entered trigger zone: {triggerID}");
            hasCarEntered = true;
            
            VehicleDataBroadcaster.Instance.TriggerEntered(triggerID); // Notify broadcaster
            
            NavigationScreenSS.TriggerIconChange(triggerID); // Notify NavigationScreenSS to update the GPS screen
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && hasCarEntered)
        {
            Debug.Log($"Car exited trigger zone: {triggerID}");
            hasCarEntered = false;
            VehicleDataBroadcaster.Instance.TriggerExited(triggerID); // Notify broadcaster
        }
    }
}
