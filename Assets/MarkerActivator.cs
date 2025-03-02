using UnityEngine;
using System.Collections.Generic;

public class MarkerActivator : MonoBehaviour
{
    private ScenarioManagerStartle scenarioManager;
    private int markersPassed = 0;
    private int totalMarkers = 5; // Adjust based on the actual number of ProgressMarkers
    private int currentLap = 4;
    private HashSet<int> passedMarkers = new HashSet<int>();
    private bool interactionMarkersActivated = false;
    private Dictionary<string, GameObject> markerDictionary = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> scenarioTrafficCarDictionary = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> regularTrafficCarDictionary = new Dictionary<string, GameObject>();

    

    public bool endTrial = false; // Flag to indicate if the trial should end

    void Start()
    {
        scenarioManager = FindObjectOfType<ScenarioManagerStartle>();
        Debug.Log($"[MarkerActivator] ScenarioManager: {scenarioManager.currentStimulus}, {scenarioManager.currentScenario}");
        CacheMarkers();
        CacheScenarioTrafficCars();
        CacheRegularTrafficCars();
    }

    void Update()
    {
        // Check if Scenario is not ready
        if (!scenarioManager.isScenarioReady)
        {
            // Reset the simulation
            ResetSimulation();
            Debug.Log($"[MarkerActivator] Scenario is not ready. Resetting simulation.");
        }
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
            marker.CompareTag("SportyMarker")
        );
        foreach (GameObject marker in allMarkers)
        {
            markerDictionary[marker.name.ToLower()] = marker;
            marker.SetActive(false);
        }
        Debug.Log($"[MarkerActivator] Cached {markerDictionary.Count} markers.");
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
        
        if (other.CompareTag("ProgressMarkers") && other.name.StartsWith("ProgressMarker_"))
        {
            int markerNumber;
            if (int.TryParse(other.name.Split('_')[1], out markerNumber) && !passedMarkers.Contains(markerNumber))
            {
                passedMarkers.Add(markerNumber);
                markersPassed++;
                Debug.Log($"Passed ProgressMarker: {markerNumber}");


                // For first scenario
                if (markerNumber==0 && currentLap ==1)
                {
                    scenarioManager = FindObjectOfType<ScenarioManagerStartle>(); // Ensure latest instance
                    ActivateInteractionMarkers();
                    ActivateScenarioTrafficCars();
                    ActivateStreetCart();

                }

                // For subsequent scenarios
                if (markerNumber >= totalMarkers - 1)
                {
                    scenarioManager = FindObjectOfType<ScenarioManagerStartle>(); // Ensure latest instance
                    ActivateInteractionMarkers();
                    ActivateScenarioTrafficCars();
                    ActivateStreetCart();
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
            Debug.Log($"[MarkerActivator] Checking street cart: {cart.name}, Current Stimulus: {scenarioManager.currentStimulus}, Current Scenario: {scenarioManager.currentScenario}");
            Debug.Log($"[MarkerActivator] {cart.name.ToLower().Contains(scenarioManager.currentStimulus)}: {cart.name.ToLower().Contains(scenarioManager.currentScenario)}");
            if (cart.name.ToLower().Contains(scenarioManager.currentStimulus) &&
                cart.name.ToLower().Contains(scenarioManager.currentScenario))
            {
                cart.SetActive(true);
                Debug.Log($"Activated street cart: {cart.name}");
            }
            else
            {
                cart.SetActive(false);
            }
        }
    }

    private void ResetRegularTrafficCars()
    {
        foreach (var kvp in regularTrafficCarDictionary)
        {
            string carName = kvp.Key;
            GameObject car = kvp.Value;
            
            car.transform.position = car.GetComponent<SC_AVFollowSpline>().originalPos;
            car.transform.rotation = car.GetComponent<SC_AVFollowSpline>().originalRot;
        }
        Debug.Log($"[MarkerActivator] Reset regular traffic cars to original positions.");
    }

    private void ResetScenarioTrafficCars()
    {
        foreach (var kvp in scenarioTrafficCarDictionary)
        {
            string carName = kvp.Key;
            GameObject car = kvp.Value;
            
            car.transform.position = car.GetComponent<SC_AVFollowSpline>().originalPos;
            car.transform.rotation = car.GetComponent<SC_AVFollowSpline>().originalRot;
        }
        Debug.Log($"[MarkerActivator] Reset regular traffic cars to original positions.");
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
        ResetScenarioTrafficCars();
        ResetPedestrians();
    }



    private void ResetSimulation()
    {
        // Reset regular traffic cars
        ResetRegularTrafficCars();

        // Reset scenario traffic cars
        ResetScenarioTrafficCars();

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
        if (other.CompareTag("InteractionMarkers") || other.CompareTag("EcoMarker") || other.CompareTag("StopMarker") || other.CompareTag("NormalMarker") || other.CompareTag("SportyMarker"))
        {
            // Deactivate the marker
            other.gameObject.SetActive(false);
            Debug.Log($"Deactivated marker: {other.name}");
        }
    }
}
