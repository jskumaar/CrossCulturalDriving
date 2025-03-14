using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScenarioManagerStartle : MonoBehaviour
{
    public string currentStimulus;
    public string currentScenario;
    public bool isScenarioActive = false;

    public bool isScenarioReady = false; // Indicates if the scenario is ready to start

    public bool newScenario = false; // Indicates if a new scenario is being loaded

    public float newScenarioTime = 0f; // Time when the new scenario starts
    
    // public bool isScenarioReset = false; // Indicates if the scenario is reset

    private bool buttonPressed = false; // Buffer for button press

    private float buttonPressTime = 0f; // Time tracking



    // Start is called before the first frame update
    void Start()
    {
        currentStimulus = "random";
        currentScenario = "random";
    }

    // Update is called once per frame
    void Update()
    {
        // Detect button press and store the timestamp
        if (Input.GetKeyDown(KeyCode.JoystickButton10))
        // if (Input.GetKeyDown(KeyCode.S))  // For debugging
        {
            buttonPressed = true;
            buttonPressTime = Time.time; // Store time when button is pressed
        }

        // If button is pressed and scenario is ready, activate it
        if (buttonPressed && isScenarioReady && !isScenarioActive)
        {
            isScenarioActive = true;
            Debug.Log("Start button pressed!");
            CommunicationManager.Instance.SendMessageToServer("ignition_trigger");
            buttonPressed = false; // Reset button press after activation
        }
        // for resetting the scenario (not used currently)
        // else if (!isScenarioReady)
        // {
        //     // Debug.Log("Scenario is not ready yet.");
        //     if (isScenarioActive)
        //     {
        //         Debug.Log("Pausing scenario....");
        //     }
        //     isScenarioActive = false;
        // }

        // Reset buttonPress if more than 2 second has passed
        if (buttonPressed && Time.time - buttonPressTime > 2f)
        {
            buttonPressed = false;
            Debug.Log("Button press reset due to timeout.");
        }
    }
}
