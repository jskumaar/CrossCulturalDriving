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


            if (this.gameObject.name.Contains("3 action end"))
            {
                Debug.Log("All interactions completed for this scenario.");
                scenarioManager = FindObjectOfType<ScenarioManagerStartle>();
                scenarioManager.currentStimulus = "random";
                scenarioManager.currentScenario = "random";
            }


        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && hasCarEntered)
        {
            Debug.Log($"Car exited trigger zone: {triggerID}");
            hasCarEntered = false;

            // CommunicationManager.Instance.TriggerExited(triggerID); // Notify broadcaster
            CommunicationManager.Instance.SendMessageToServer($"TRIGGER_EXITED: {triggerID}");
        }
    }
}
