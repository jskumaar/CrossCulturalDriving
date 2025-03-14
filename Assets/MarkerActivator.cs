using UnityEngine;
using System.Collections.Generic;

public class MarkerActivator : MonoBehaviour
{
    private ScenarioManagerStartle scenarioManager;
    private CommunicationManager communicationManager;
    public int markersPassed = 0;
    private int totalMarkers = 5; // Adjust based on the actual number of ProgressMarkers
    private int currentLap = 0;
    private HashSet<int> passedMarkers = new HashSet<int>();
    private bool interactionMarkersActivated = false;
    private Dictionary<string, GameObject> markerDictionary = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> scenarioTrafficCarDictionary = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> regularTrafficCarDictionary = new Dictionary<string, GameObject>();

    private TireSoundController soundController;

    // private EgoVehicleController egoCarController;

    

    public bool endTrial = false; // Flag to indicate if the trial should end

    public bool cartActivated = false; // Flag to indicate if the street cart is activated

    void Start()
    {
        scenarioManager = FindObjectOfType<ScenarioManagerStartle>();
        communicationManager = FindObjectOfType<CommunicationManager>();
        Debug.Log($"[MarkerActivator] ScenarioManager: {scenarioManager.currentStimulus}, {scenarioManager.currentScenario}");
        CacheMarkers();
        CacheScenarioTrafficCars();
        CacheRegularTrafficCars();
        soundController = FindObjectOfType<TireSoundController>();
    }


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
            marker.CompareTag("AdditionalStopSign") ||
            marker.CompareTag("StreetCart") 
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

                    
                if ( ((scenarioManager.currentScenario == "alert" && scenarioManager.currentStimulus == "confusion") && (markerNumber >= totalMarkers - 1))   ||
                    (markerNumber < 1 || (markerNumber >= totalMarkers - 1)) )  // Confusion Alert 1st marker is after Progress Marker 0 and before Progress Marker 1
                {
                    if (scenarioManager.newScenario)
                    {
                        ActivateNewScenario();
                        scenarioManager.newScenario = false; // Reset new scenario flag
                        Debug.Log($"[MarkerActivator] New scenario activated: {scenarioManager.currentStimulus}, {scenarioManager.currentScenario}");
                    }

                    else if (Time.time - scenarioManager.newScenarioTime > 250f) // Ensure a minimum time has passed before sending reset message
                    {
                        communicationManager.SendMessageToServer("need_to_reset_sim");
                    }

                }
                
                // Check if the lap is complete and reset traffic cars
                if (markerNumber == totalMarkers)
                {
                    ResetRegularTrafficCars();
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
            Debug.Log($"[MarkerActivator] Car took a sharp turn at marker: {other.name}");
            if (soundController != null)
            {
                soundController.PlayTireScreech();
            }
        }

        if (other.CompareTag("PedActivate"))
        {
            // Activate Pedestrian Manager
            ActivatePedestrianManager();
            Debug.Log($"[MarkerActivator] Activating pedestrians.");
        }


        // deactivate/reset pedestrians towards end of lap
        if (other.CompareTag("PedReset"))
        {
            if (scenarioManager.currentScenario == "alert" && scenarioManager.currentStimulus == "frustration")
            {
                // Deactivate Pedestrian Manager
                ResetPedestrians();
                Debug.Log($"[MarkerActivator] Resetting pedestrians.");
            }
            else
            {
                // Reset Pedestrian Manager
                DeactivatePedestrianManager();
                Debug.Log($"[MarkerActivator] Deactivating pedestrians.");
            }
        }

        // Activate Scenario traffic cars 
        if (other.CompareTag("InteractionMarkers"))
        {
            if (other.name.ToLower().Contains("confusion") &&
                other.name.ToLower().Contains("alert") && other.name.ToLower().Contains("4") && !other.name.ToLower().Contains("end"))
            {
                // Activate Scenario traffic cars for confusion alert
                ActivateScenarioTrafficCars();
                Debug.Log($"[MarkerActivator] Activating scenario traffic cars.");
            }

            else if (other.name.ToLower().Contains("surprise") &&
                other.name.ToLower().Contains("alert") && other.name.ToLower().Contains("2") && !other.name.ToLower().Contains("end"))
            {
                // Activate Scenario traffic cars for surprise alert
                ActivateScenarioTrafficCars();
                Debug.Log($"[MarkerActivator] Activating scenario traffic cars.");
            }

            else if (other.name.ToLower().Contains("frustration") &&
                other.name.ToLower().Contains("alert") && other.name.ToLower().Contains("2") && !other.name.ToLower().Contains("end"))
            {
                // Activate Scenario traffic cars for frustration alert
                ActivateScenarioTrafficCars();
                Debug.Log($"[MarkerActivator] Activating scenario traffic cars.");
            }
            else 
            {
                // Deactivate all scenario traffic cars
                DeactivateAllScenarioTrafficCars();
                Debug.Log($"[MarkerActivator] Deactivating scenario traffic cars.");
            }



            if (other.name.ToLower().Contains("surprise") &&
                other.name.ToLower().Contains("alert") && other.name.ToLower().Contains("1"))
            {
                // Activate pedestrians for surprise alert
                ActivatePedestrianManager();
                Debug.Log($"[MarkerActivator] Activating pedestrians.");
            }

            if (other.name.ToLower().Contains("confusion") &&
                other.name.ToLower().Contains("alert") && other.name.ToLower().Contains("end") && other.name.ToLower().Contains("1"))
            {
                // Activate pedestrians for frustration alert
                DeactivatePedestrianManager();
                Debug.Log($"[MarkerActivator] Activating pedestrians.");
            }


        }
    
    }


    private void ActivateNewScenario()
    {
        ActivateInteractionObjects();
        RemoveUnwantedCars();

        // Activate pedestrians towards the start of the lap only for confusion alert
        if (scenarioManager.currentScenario == "alert" && scenarioManager.currentStimulus == "confusion")
        {
            ActivatePedestrianManager();
            Debug.Log($"[MarkerActivator] Activating pedestrians.");
        }
        else
        {
            // Deactivate pedestrian manager (if already activated)
            DeactivatePedestrianManager();
            Debug.Log($"[MarkerActivator] Deactivating pedestrians.");
        }

        // // Activate additional stop signs for music frustration scenario
        // if (scenarioManager.currentScenario == "music" && scenarioManager.currentStimulus == "frustration")
        // {
        //     ActivateAdditionalStopMarkers();
        // }
        // else
        // {
        //     DeactivateAdditionalStopMarkers();
        // }


        
        // if (scenarioManager.currentScenario == "alert" && scenarioManager.currentStimulus == "surprise")
        // {
        //     ActivateStreetCart();
        //     cartActivated = true;
        // }
        // else
        // {
        //     // Deactivate street cart
        //     GameObject[] streetCarts = GameObject.FindGameObjectsWithTag("StreetCart");
        //     foreach (GameObject cart in streetCarts)
        //     {
        //         cart.SetActive(false);
        //         Debug.Log($"Deactivated street cart: {cart.name}");
        //     }
        // }
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
            // if (manager.name.ToLower().Contains(scenarioManager.currentStimulus) &&
            //     manager.name.ToLower().Contains(scenarioManager.currentScenario))
            // {
            //     manager.gameObject.SetActive(true);
            //     Debug.Log($"[MarkerActivator] Activated crowd manager: {manager.name}");

            //     // Deactivate other crowd managers
            //     foreach (CrowdAgentManagerMulti otherManager in crowdManagers)
            //     {
            //         if (otherManager != manager)
            //         {
            //             otherManager.gameObject.SetActive(false);
            //             Debug.Log($"[MarkerActivator] Deactivated crowd manager: {otherManager.name}");
            //         }
            //     }
            //     break;
            // }

            // if no manager is found, activate the default manager
            if (manager.name.ToLower().Contains("default"))
            {
                manager.ActivateAgentSpawn();
                Debug.Log($"[MarkerActivator] Activated default crowd manager: {manager.name}");
            }

        }

    }

    private void ActivateInteractionObjects()
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

    // private void ActivateRegularStopMarkers()
    // {
    //     foreach (var kvp in markerDictionary)
    //     {
    //         string markerName = kvp.Key;
    //         GameObject marker = kvp.Value;

    //         if (marker.CompareTag("StopSign"))
    //         {
    //             marker.SetActive(true);
    //             Debug.Log($"Activated marker: {marker.name}");
    //         }
    //     }
    // }

    // private void ActivateAdditionalStopMarkers()
    // {
        
    //     GameObject[] stopSignMarkersObjects = GameObject.FindGameObjectsWithTag("AdditionalStopSign");
    //     foreach (GameObject stopSignMarker in stopSignMarkersObjects)
    //     {
    //         stopSignMarker.SetActive(true);
    //         Debug.Log($"Activated additional stop marker/object: {stopSignMarker.name}");
    //     }
    // }

    // private void DeactivateAdditionalStopMarkers()
    // {
        
    //     GameObject[] stopSignMarkersObjects = GameObject.FindGameObjectsWithTag("AdditionalStopSign");
    //     foreach (GameObject stopSignMarker in stopSignMarkersObjects)
    //     {
    //         stopSignMarker.SetActive(false);
    //         Debug.Log($"Activated additional stop marker/object: {stopSignMarker.name}");
    //     }
    // }


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
                Debug.Log($"Activated traffic car: {car.name}");
            }
        }
    }


    private void DeactivateAllScenarioTrafficCars()
    {
        foreach (var kvp in scenarioTrafficCarDictionary)
        {
            string carName = kvp.Key;
            GameObject car = kvp.Value;
            car.SetActive(false);
        }
    }


    // private void ActivateStreetCart()
    // {
    //     // Implement logic to manage traffic cars
    //     GameObject[] streetCarts = GameObject.FindGameObjectsWithTag("StreetCart");

    //     Debug.Log($"[MarkerActivator] Found {streetCarts.Length} street carts.");

    //     foreach (GameObject cart in streetCarts)
    //     {
    //         cart.SetActive(true);
    //     }
    // }


    // private void DeactivateStreetCart()
    // {
    //     // Implement logic to manage traffic cars
    //     GameObject[] streetCarts = GameObject.FindGameObjectsWithTag("StreetCart");

    //     Debug.Log($"[MarkerActivator] Found {streetCarts.Length} street carts.");

    //     foreach (GameObject cart in streetCarts)
    //     {
    //         cart.SetActive(false);
    //     }
    // }

    private void ResetRegularTrafficCars()
    {
        foreach (var kvp in regularTrafficCarDictionary)
        {
            string carName = kvp.Key;
            GameObject car = kvp.Value;
            
            SC_AVFollowSpline carController = car.GetComponent<SC_AVFollowSpline>();
            carController.ResetCar();
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

    private void DeactivatePedestrianManager()
    {
        CrowdAgentManagerMulti[] crowdManagers = FindObjectsOfType<CrowdAgentManagerMulti>();
        foreach (CrowdAgentManagerMulti manager in crowdManagers)
        {
            // if (manager.name.ToLower().Contains(scenarioManager.currentStimulus) &&
            //     manager.name.ToLower().Contains(scenarioManager.currentScenario))
            // {
            //     manager.gameObject.SetActive(false);
            //     Debug.Log($"[MarkerActivator] Deactivated crowd manager: {manager.name}");
            // }
            manager.DeactivateAgentSpawn();
            Debug.Log($"[MarkerActivator] Deactivated crowd manager: {manager.name}");
        }
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


    // private void CompleteLap()
    // {
        
    //     currentLap++;
    //     markersPassed = 0;
    //     passedMarkers.Clear();
    //     interactionMarkersActivated = false;

    //     // Reset Traffic and pedestrians after each lap
    //     ResetRegularTrafficCars();
    //     // ResetScenarioTrafficCars();
    //     // ResetPedestrians();
    // }



    public void ResetSimulation()
    {
        
        markersPassed = 0;
        passedMarkers.Clear();
        
        // Reset regular traffic cars
        ResetRegularTrafficCars();

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

    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("InteractionMarkers") || other.CompareTag("EcoMarker") || other.CompareTag("StopMarker") || other.CompareTag("NormalMarker") || other.CompareTag("SportyMarker"))
        {
            // Deactivate the marker
            other.gameObject.SetActive(false);
            Debug.Log($"Deactivated marker: {other.name}");
        }
    }
}
