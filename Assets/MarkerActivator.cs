using UnityEngine;
using System.Collections.Generic;

public class MarkerActivator : MonoBehaviour
{
    private ScenarioManagerStartle scenarioManager;
    public int markersPassed = 0;
    private int totalMarkers = 5; // Adjust based on the actual number of ProgressMarkers
    private int currentLap = 0;
    private HashSet<int> passedMarkers = new HashSet<int>();
    private bool interactionMarkersActivated = false;
    private Dictionary<string, GameObject> markerDictionary = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> scenarioTrafficCarDictionary = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> regularTrafficCarDictionary = new Dictionary<string, GameObject>();

    private EngineSoundController soundController;

    private NetworkVehicleController egoCarController;

    

    public bool endTrial = false; // Flag to indicate if the trial should end

    public bool cartActivated = false; // Flag to indicate if the street cart is activated

    void Start()
    {
        scenarioManager = FindObjectOfType<ScenarioManagerStartle>();
        Debug.Log($"[MarkerActivator] ScenarioManager: {scenarioManager.currentStimulus}, {scenarioManager.currentScenario}");
        CacheMarkers();
        CacheScenarioTrafficCars();
        CacheRegularTrafficCars();
        soundController = FindObjectOfType<EngineSoundController>();
        egoCarController = FindObjectOfType<NetworkVehicleController>();
    }

    void Update()
    {
        // // Check if Scenario is not ready
        // if (!scenarioManager.isScenarioReady)
        // {
        //     // Reset the simulation
        //     ResetSimulation();
        //     Debug.Log($"[MarkerActivator] Scenario is not ready. Resetting simulation.");
        // }
    }

    // public void updateScenario(string stimulus, string scenario)
    // {
    //     scenarioManager.currentStimulus = stimulus;
    //     scenarioManager.currentScenario = scenario;
    //     Debug.Log($"[MarkerActivator] Scenario updated to: {stimulus}, {scenario}");
    // }

    // Cache markers for efficient lookups
    private void CacheMarkers()
    {
        // GameObject[] allMarkers = GameObject.FindGameObjectsWithTag("InteractionMarkers");
        GameObject[] allMarkers = GameObject.FindObjectsOfType<GameObject>();
        allMarkers = System.Array.FindAll(allMarkers, marker => 
            marker.CompareTag("InteractionMarkers") ||
            marker.CompareTag("EcoMarker") ||
            marker.CompareTag("StopMarker") ||
            marker.CompareTag("NormalMarker") ||
            marker.CompareTag("SportyMarker") ||
            marker.CompareTag("StopSign")
        );
        foreach (GameObject marker in allMarkers)
        {
            markerDictionary[marker.name.ToLower()] = marker;
            marker.SetActive(false);
        }
        Debug.Log($"[MarkerActivator] Cached {markerDictionary.Count} markers.");

        // Log all marker keys to verify AdditionalStopSign objects are cached
        string markerKeys = string.Join(", ", markerDictionary.Keys);
        Debug.Log($"[MarkerActivator] Cached markers: {markerKeys}");
    }

    private void CacheScenarioTrafficCars()
    {
        // Implement logic to manage traffic cars
        GameObject[] scenarioTrafficCars = GameObject.FindGameObjectsWithTag("TrafficCar");

        // cache the traffic cars
        foreach (GameObject car in scenarioTrafficCars)
        {
            scenarioTrafficCarDictionary[car.name.ToLower()] = car;
            car.SetActive(false);
        }
        Debug.Log($"[MarkerActivator] Cached {scenarioTrafficCarDictionary.Count} scenario traffic cars.");
    }

    private void CacheRegularTrafficCars()
    {
        // Implement logic to manage traffic cars
        GameObject[] regularTrafficCars = GameObject.FindGameObjectsWithTag("RegularTrafficCar");

        // cache the traffic cars
        foreach (GameObject car in regularTrafficCars)
        {
            regularTrafficCarDictionary[car.name.ToLower()] = car;
            Debug.Log($"[MarkerActivator] Cached regular traffic car: {car.name}");
        }
        Debug.Log($"[MarkerActivator] Cached {regularTrafficCarDictionary.Count} other traffic cars.");
    }


    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[MarkerActivator] Triggered by: {other.name}");

        if (other.CompareTag("ProgressMarkers") && other.name.StartsWith("ProgressMarker_"))
        {
            int markerNumber;
            if (int.TryParse(other.name.Split('_')[1], out markerNumber) && !passedMarkers.Contains(markerNumber))
            {
                passedMarkers.Add(markerNumber);
                markersPassed++;
                Debug.Log($"Passed ProgressMarker: {markerNumber}");

                // Check and activate markers for a new scenario
                scenarioManager = FindObjectOfType<ScenarioManagerStartle>(); // Ensure latest instance
                if (scenarioManager.newScenario)
                {
                    
                    if ( ((scenarioManager.currentScenario == "alert" && scenarioManager.currentStimulus == "confusion") && (markerNumber >= totalMarkers - 1))   ||
                        (markerNumber < 1 || (markerNumber >= totalMarkers - 1)) )  // Confusion Alert 1st marker is after Progress Marker 0 and before Progress Marker 1
                    {
                        ActivateInteractionMarkers();
                        ActivateScenarioTrafficCars();
                        ActivateRegularStopMarkers();
                        ActivatePedestrianManager();
                        RemoveUnwantedCars();

                        // Activate additional stop signs for music frustration scenario
                        if (scenarioManager.currentScenario == "music" && scenarioManager.currentStimulus == "frustration")
                        {
                            ActivateAdditionalStopMarkers();
                        }
                        else
                        {
                            DeactivateAdditionalStopMarkers();
                        }



                        if (scenarioManager.currentScenario == "alert" && scenarioManager.currentStimulus == "surprise")
                        {
                            ActivateStreetCart();
                            cartActivated = true;
                        }
                        else if (cartActivated)
                        {
                            // Deactivate street cart
                            GameObject[] streetCarts = GameObject.FindGameObjectsWithTag("StreetCart");
                            foreach (GameObject cart in streetCarts)
                            {
                                cart.SetActive(false);
                                Debug.Log($"Deactivated street cart: {cart.name}");
                            }
                        }

                        scenarioManager.newScenario = false; // Reset new scenario flag
                    }

                }
                
                // Check if the lap is complete
                if (markerNumber == totalMarkers)
                {
                    CompleteLap();
                    if (currentLap > 4){
                        endTrial = true;
                        Debug.Log("End of trial reached.");
                    }
                }
            }
        }
    
    
        // Play tire screeching sound when car takes sharp turns
        if (other.name.ToLower().Contains("tire") && other.name.ToLower().Contains("screech"))
        {
            // Debug.Log($"[MarkerActivator] Car took a sharp turn at marker: {other.name}");
            if (soundController != null)
            {
                soundController.PlayTireScreech();
            }
        }

        //Turn Markers
        if (other.CompareTag("TurnMarker"))
        {
            Debug.Log($"[MarkerActivator] Car took a turn at marker: {other.name}");
            if (other.name.ToLower().Contains("left"))
            {
                egoCarController.tempLeft = true;
                egoCarController.tempRight = false;
            }
            else if (other.name.ToLower().Contains("right"))
            {
                egoCarController.tempRight = true;
                egoCarController.tempLeft = false;
            }
        }

        // When any action is completed; set the indicators to false
        if (other.name.ToLower().Contains("action_end"))
        {
            egoCarController.tempRight = false;
            egoCarController.tempLeft = false;
        }
    
    }

    private void RemoveUnwantedCars()
    {
        
        // For Frustration Driving Scenario, remove the 6th car
        if ((scenarioManager.currentScenario == "driving") && (scenarioManager.currentStimulus == "frustration"))
        {
            // Remove Traffic cars
                foreach (var kvp in regularTrafficCarDictionary)
                {
                    string carName = kvp.Key;
                    GameObject car = kvp.Value;
                    if (carName.ToLower().Contains("6"))
                    {
                        car.SetActive(false);
                        break;
                    }
                }
        }
    }

    private void ActivatePedestrianManager()
    {
        // Activate Pedestrian Manager
        CrowdAgentManagerMulti[] crowdManagers = FindObjectsOfType<CrowdAgentManagerMulti>();
        foreach (CrowdAgentManagerMulti manager in crowdManagers)
        {
            if (manager.name.ToLower().Contains(scenarioManager.currentStimulus) &&
                manager.name.ToLower().Contains(scenarioManager.currentScenario))
            {
                manager.gameObject.SetActive(true);
                Debug.Log($"[MarkerActivator] Activated crowd manager: {manager.name}");

                // Deactivate other crowd managers
                foreach (CrowdAgentManagerMulti otherManager in crowdManagers)
                {
                    if (otherManager != manager)
                    {
                        otherManager.gameObject.SetActive(false);
                        Debug.Log($"[MarkerActivator] Deactivated crowd manager: {otherManager.name}");
                    }
                }
                break;
            }

            // if no manager is found, activate the default manager
            if (manager.name.ToLower().Contains("default"))
            {
                manager.gameObject.SetActive(true);
                Debug.Log($"[MarkerActivator] Activated default crowd manager: {manager.name}");
            }

        }

    }

    private void ActivateInteractionMarkers()
    {

        foreach (var kvp in markerDictionary)
        {
            string markerName = kvp.Key;
            GameObject marker = kvp.Value;

            if (markerName.ToLower().Contains(scenarioManager.currentStimulus) &&
                markerName.ToLower().Contains(scenarioManager.currentScenario))
            {
                marker.SetActive(true);
                Debug.Log($"Activated marker: {marker.name}");
            }
            else
            {
                marker.SetActive(false);
            }
        }
    }

    private void ActivateRegularStopMarkers()
    {
        foreach (var kvp in markerDictionary)
        {
            string markerName = kvp.Key;
            GameObject marker = kvp.Value;

            if (marker.CompareTag("StopSign"))
            {
                marker.SetActive(true);
                Debug.Log($"Activated marker: {marker.name}");
            }
        }
    }

    private void ActivateAdditionalStopMarkers()
    {
        
        GameObject[] stopSignMarkersObjects = GameObject.FindGameObjectsWithTag("AdditionalStopSign");
        foreach (GameObject stopSignMarker in stopSignMarkersObjects)
        {
            stopSignMarker.SetActive(true);
            Debug.Log($"Activated additional stop marker/object: {stopSignMarker.name}");
        }
    }

    private void DeactivateAdditionalStopMarkers()
    {
        
        GameObject[] stopSignMarkersObjects = GameObject.FindGameObjectsWithTag("AdditionalStopSign");
        foreach (GameObject stopSignMarker in stopSignMarkersObjects)
        {
            stopSignMarker.SetActive(false);
            Debug.Log($"Activated additional stop marker/object: {stopSignMarker.name}");
        }
    }


    private void ActivateScenarioTrafficCars()
    {
        foreach (var kvp in scenarioTrafficCarDictionary)
        {
            string carName = kvp.Key;
            GameObject car = kvp.Value;

            if (carName.ToLower().Contains(scenarioManager.currentStimulus) &&
                carName.ToLower().Contains(scenarioManager.currentScenario))
            {
                car.SetActive(true);
                // Debug.Log($"Activated traffic car: {car.name}");
            }
            else
            {
                car.SetActive(false);
            }
        }
    }


    private void ActivateStreetCart()
    {
        // Implement logic to manage traffic cars
        GameObject[] streetCarts = GameObject.FindGameObjectsWithTag("StreetCart");

        Debug.Log($"[MarkerActivator] Found {streetCarts.Length} street carts.");

        foreach (GameObject cart in streetCarts)
        {
            // Debug.Log($"[MarkerActivator] Checking street cart: {cart.name}, Current Stimulus: {scenarioManager.currentStimulus}, Current Scenario: {scenarioManager.currentScenario}");
            // Debug.Log($"[MarkerActivator] {cart.name.ToLower().Contains(scenarioManager.currentStimulus)}: {cart.name.ToLower().Contains(scenarioManager.currentScenario)}");
            // if (cart.name.ToLower().Contains(scenarioManager.currentStimulus) &&
            //     cart.name.ToLower().Contains(scenarioManager.currentScenario))
            // {
            //     cart.SetActive(true);
            //     Debug.Log($"Activated street cart: {cart.name}");
            // }
            // else
            // {
            //     cart.SetActive(false);
            // }
            cart.SetActive(true);
        }
    }

    private void ResetRegularTrafficCars()
    {
        foreach (var kvp in regularTrafficCarDictionary)
        {
            string carName = kvp.Key;
            GameObject car = kvp.Value;
            
            SC_AVFollowSpline carController = car.GetComponent<SC_AVFollowSpline>();
            carController.ResetCar();

            // car.transform.position = car.GetComponent<SC_AVFollowSpline>().originalPos;
            // car.transform.rotation = car.GetComponent<SC_AVFollowSpline>().originalRot;

            // car.GetComponent<SC_AVFollowSpline>().vehicleController.SteeringInput = 0f;
            // car.GetComponent<SC_AVFollowSpline>().vehicleController.ThrottleInput = 0f;

        }
        Debug.Log($"[MarkerActivator] Reset regular traffic cars to original positions.");
    }

    private void ResetScenarioTrafficCars()
    {
        foreach (var kvp in scenarioTrafficCarDictionary)
        {
            string carName = kvp.Key;
            GameObject car = kvp.Value;
            
            SC_AVFollowSpline carController = car.GetComponent<SC_AVFollowSpline>();

            carController.ResetCar();
        }
        Debug.Log($"[MarkerActivator] Reset scenario traffic cars to original positions.");
    }


    private void ResetPedestrians()
    {
        CrowdAgentManagerMulti[] crowdManagers = FindObjectsOfType<CrowdAgentManagerMulti>();
        foreach (CrowdAgentManagerMulti manager in crowdManagers)
        {
            Debug.Log($"Resetting crowd agents for manager: {manager.name} with tag: {manager.GetComponent<CrowdAgentManagerMulti>().spawnAreaTag}");
            manager.ResetAgentSpawning();
        }
        
        Debug.Log($"[MarkerActivator]. Reset all crowd agents.");

    }


    private void CompleteLap()
    {
        
        currentLap++;
        markersPassed = 0;
        passedMarkers.Clear();
        interactionMarkersActivated = false;

        // Reset Traffic and pedestrians after each lap
        ResetRegularTrafficCars();
        // ResetScenarioTrafficCars();
        ResetPedestrians();
    }



    private void ResetSimulation()
    {
        // Reset regular traffic cars
        ResetRegularTrafficCars();

        // // Reset scenario traffic cars
        // ResetScenarioTrafficCars();

        // Reset ego car
        SC_AVFollowSplineEgo egoCar = FindObjectOfType<SC_AVFollowSplineEgo>();
        if (egoCar != null)
        {
            egoCar.ResetEgoCar();
            Debug.Log("[MarkerActivator] Reset ego car to original position.");
        }
        else
        {
            Debug.LogWarning("[MarkerActivator] Could not find ego car to reset!");
        }

        // Reset pedestrians
        ResetPedestrians();

        // scenarioManager.isScenarioReset = true; // Set the reset flag to true

    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("InteractionMarkers") || other.CompareTag("EcoMarker") || other.CompareTag("StopMarker") || other.CompareTag("NormalMarker") || other.CompareTag("SportyMarker") || other.CompareTag("AdditionalStopSign"))
        {
            // Deactivate the marker
            other.gameObject.SetActive(false);
            Debug.Log($"Deactivated marker: {other.name}");
        }
    }
}
