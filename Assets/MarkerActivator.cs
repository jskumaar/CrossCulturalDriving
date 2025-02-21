using UnityEngine;
using System.Collections.Generic;

public class MarkerActivator : MonoBehaviour
{
    private ScenarioManagerStartle scenarioManager;
    private int markersPassed = 0;
    private int totalMarkers = 4; // Adjust based on the actual number of ProgressMarkers
    private int currentLap = 1;
    private HashSet<int> passedMarkers = new HashSet<int>();
    private bool interactionMarkersActivated = false;
    private Dictionary<string, GameObject> markerDictionary = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> trafficCarDictionary = new Dictionary<string, GameObject>();

    void Start()
    {
        scenarioManager = FindObjectOfType<ScenarioManagerStartle>();
        Debug.Log($"[MarkerActivator] ScenarioManager: {scenarioManager.currentStimulus}, {scenarioManager.currentScenario}");
        CacheMarkers();
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

    private void TrafficCars()
    {
        // Implement logic to manage traffic cars
        GameObject[] trafficCars = GameObject.FindGameObjectsWithTag("TrafficCar");

        // cache the traffic cars
        foreach (GameObject car in trafficCars)
        {
            trafficCarDictionary[car.name.ToLower()] = car;
            car.SetActive(false);
        }
        Debug.Log($"[MarkerActivator] Cached {trafficCarDictionary.Count} traffic cars.");
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

                // Check lap completion
                float progress = ((float)markersPassed-1) / totalMarkers;
                Debug.Log($"Lap progress: {progress * 100}%");

                if ((progress < 0.2f || progress >= 0.9f) && !interactionMarkersActivated)
                {
                    scenarioManager = FindObjectOfType<ScenarioManagerStartle>(); // Ensure latest instance
                    ActivateInteractionMarkers();
                    // ActivateTrafficCars();
                    interactionMarkersActivated = true;
                }

                // Check if the lap is complete
                if (markersPassed == totalMarkers + 1)
                {
                    CompleteLap();
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

            if (markerName.Contains(scenarioManager.currentStimulus) &&
                markerName.Contains(scenarioManager.currentScenario))
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


    private void ActivateTrafficCars()
    {
        foreach (var kvp in trafficCarDictionary)
        {
            string carName = kvp.Key;
            GameObject car = kvp.Value;

            if (carName.Contains(scenarioManager.currentStimulus) &&
                carName.Contains(scenarioManager.currentScenario))
            {
                car.SetActive(true);
                Debug.Log($"Activated traffic car: {car.name}");
            }
            else
            {
                car.SetActive(false);
            }
        }
    }

    private void CompleteLap()
    {
        Debug.Log("Lap completed. Resetting markers.");
        currentLap++;
        markersPassed = 0;
        passedMarkers.Clear();
        interactionMarkersActivated = false;

        // foreach (var marker in markerDictionary.Values)
        // {
        //     marker.SetActive(false);
        // }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("InteractionMarkers"))
        {
            other.gameObject.SetActive(false);
            Debug.Log($"Marker Passed. Deactivated marker: {other.name}");
        }
    }
}
