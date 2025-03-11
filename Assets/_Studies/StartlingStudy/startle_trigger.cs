using UnityEngine;

public class StartleTrigger : MonoBehaviour
{
    public string triggerID;
    private bool hasCarEntered = false;  // Ensures one enter event per trigger
    private ScenarioManagerStartle scenarioManager;


    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasCarEntered)
        {
            Debug.Log($"Car entered trigger zone: {triggerID}");
            hasCarEntered = true;

            if (CommunicationManager.Instance == null)
            {
                Debug.Log("CommunicationManager.Instance is null in StartleTrigger!");
            }
            else
            {
                Debug.Log("CommunicationManager.Instance found in StartleTrigger.");
            }
            // CommunicationManager.Instance.TriggerEntered(triggerID); // Notify broadcaster
            CommunicationManager.Instance.SendMessageToServer($"TRIGGER_ENTERED: {triggerID}");
            
            NavigationScreenSS.TriggerIconChange(triggerID); // Notify NavigationScreenSS to update the GPS screen

        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && hasCarEntered)
        {
            Debug.Log($"Car exited trigger zone: {triggerID}");
            hasCarEntered = false;

            // Two ecomarkers are used to bring the car back from sporty to eco modes during surprise and confusion driving interactions (the startling interaction; but this should not trigger anything in the backend)
            if (!triggerID.Contains("ecomarker"))
            {
                CommunicationManager.Instance.SendMessageToServer($"TRIGGER_EXITED: {triggerID}"); // Notify server
            }
            

            if (triggerID == "frustration_alert_trigger_3_action_end"){
                CommunicationManager.Instance.SendMessageToServer("alert_frustration_stop");
            }
        }
    }
}
