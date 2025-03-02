using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class CrowdAgentManagerMulti : NetworkBehaviour
{
    public GameObject[] agentPrefabs;
    public int initialSpawnCount = 10;

    public Transform agentSpawn;

    [SerializeField]
    private List<GameObject> agentInstances = new List<GameObject>();

    public int maxAgentCount = 50;
    public bool spawnOnStart = false;
    public bool spawnOverTime = true;
    public float spawnRate = 1f;

    // NPC change
    private List<BoxCollider> blockAreas;
    public static CrowdAgentManagerMulti Singleton;

    public bool dummyTrafficLight = false;

    // Newly added (02/05/25)
    private List<BoxCollider> spawnAreas = new List<BoxCollider>(); // FIX: Initialize list
    public int maxAgentCountPerArea = 10;
    public Dictionary<BoxCollider, int> activeAgentCount = new Dictionary<BoxCollider, int>();
    public Dictionary<GameObject, BoxCollider> agentSpawnAreaMap = new Dictionary<GameObject, BoxCollider>(); // Track spawn area for each agent
    private Dictionary<GameObject, Vector3> lastPositions = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, int> stuckCounts = new Dictionary<GameObject, int>();
    private int stuckThreshold = 3;
    private float stuckTimeThreshold = 5f;
    private Dictionary<GameObject, float> stuckTimers = new Dictionary<GameObject, float>();

    private Dictionary<GameObject, Vector3> initialPositions = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, float> lastRerouteTimes = new Dictionary<GameObject, float>();
    private float rerouteCooldown = 3f; // 3-second cooldown

    private ScenarioManagerStartle scenarioManager;

    [Header("Spawn Area Settings")]
    [SerializeField] public string spawnAreaTag = "CrowdAgent"; // Can be "CrowdAgent" or "CrowdAgentCrosswalk"
    


    void Awake()
    {
        // if (Singleton)
        // {
        //     // Destroy(gameObject);
        //     // Instead of destroying the component, just disable it
        //     this.enabled = false;
        //     return;
        // }
        // Singleton = this;
    }

    void Start()
    {
        
        scenarioManager = FindObjectOfType<ScenarioManagerStartle>();

        // Set up block areas
        blockAreas = BlocksManager.GetBuildingBlocks();
        if (blockAreas.Count <= 0)
        {
            Debug.Log("Cannot get building blocks.");
        }

        if (!IsServer)
        {
            // Destroy(this);
            // Instead of destroying the component, just disable it
            this.enabled = false;
            return;
        }

        // Initialize spawn areas
        BoxCollider[] foundSpawnAreas = FindObjectsOfType<BoxCollider>();
        foreach (var area in foundSpawnAreas)
        {
            if (area.gameObject.CompareTag(spawnAreaTag))
            {
                spawnAreas.Add(area);
                activeAgentCount[area] = 0;
                Debug.Log($"Added Spawn area '{area.name}' with tag '{spawnAreaTag}' to manager '{gameObject.name}'");
            }
        }

        if (spawnAreas.Count == 0)
        {
            Debug.LogError($"No spawn areas defined. Make sure to tag the '{spawnAreaTag}' as 'CrowdAgent'.");
            return;
        }

        // Only spawn agents if spawnOnStart is true
        if (spawnOnStart)
        {
            for (int i = 0; i < initialSpawnCount; i++)
            {
                RandomSpawn();
            }

            if (spawnOverTime)
            {
                InvokeRepeating(nameof(RandomSpawn), spawnRate, spawnRate);
            }
        }

    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P) || scenarioManager.isScenarioActive)
        {
            AgentSetup(null);
        }

        // Continuously check and reroute pedestrians
        CheckAndRerouteAgents();
    }

    public void AgentSetup(Transform parent)
    {
        if (parent)
        {
            transform.parent = parent;
            transform.localPosition = Vector3.zero;
        }

        // No need to check for BoxCollider here if spawnAreas are already handled in Start()
        for (int i = 0; i < initialSpawnCount; i++)
        {
            RandomSpawn();
        }

        if (spawnOverTime)
        {
            InvokeRepeating(nameof(RandomSpawn), spawnRate, spawnRate);
        }
    }


    void RandomSpawn()
    {
        if (spawnAreas.Count == 0)
        {
            Debug.LogWarning($"No spawn areas available with tag '{spawnAreaTag}' for manager '{gameObject.name}'");
            return;
        }

        // Get available areas
        List<BoxCollider> availableSpawnAreas = new List<BoxCollider>();
        foreach (var area in spawnAreas)
        {
            if (area != null && activeAgentCount.ContainsKey(area) && activeAgentCount[area] < maxAgentCountPerArea)
            {
                availableSpawnAreas.Add(area);
            }
        }
        
        if (availableSpawnAreas.Count == 0)
        {
            // Debug.LogWarning($"All spawn areas for tag '{spawnAreaTag}' are at capacity");
            return;
        }

        BoxCollider selectedSpawnArea = availableSpawnAreas[Random.Range(0, availableSpawnAreas.Count)];
        Vector3? spawnPosition = SelectRandomBirthplace(selectedSpawnArea);
        if (spawnPosition == null) return;

        // Print the spawn area information
        // Debug.Log($"Spawning agent in area: {selectedSpawnArea.name} with bounds: " +
        //         $"Center({selectedSpawnArea.bounds.center}), " +
        //         $"Size({selectedSpawnArea.bounds.size})");

        GameObject randomPrefab = agentPrefabs[Random.Range(0, agentPrefabs.Length)];
        GameObject agentInstance = Instantiate(randomPrefab, spawnPosition.Value, Quaternion.identity);

        // agentInstance.GetComponent<NetworkObject>().Spawn();

        NetworkObject networkObject = agentInstance.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            networkObject.Spawn();
            
            // AFTER spawning, you can reparent
            agentInstance.transform.parent = agentSpawn;
        }
        else
        {
            // If there's no NetworkObject, you can reparent immediately
            agentInstance.transform.parent = agentSpawn;
        }
    

        // agentInstance.transform.parent = agentSpawn;
        agentInstances.Add(agentInstance);

        // Post-spawn validation
        if (!selectedSpawnArea.bounds.Contains(agentInstance.transform.position) ||
            !NavMesh.SamplePosition(agentInstance.transform.position, out _, 1f, NavMesh.AllAreas))
        {
            // Debug.LogWarning("Agent spawned outside bounds or off NavMesh. Destroying and retrying...");
            RemoveAgent(selectedSpawnArea, agentInstance);
            RandomSpawn(); // Retry spawning
            return;
        }

        // Debug.Log($"Agent spawned at {agentInstance.transform.position} inside {selectedSpawnArea.name}");

        // Store the spawn area for this agent
        agentSpawnAreaMap[agentInstance] = selectedSpawnArea;

        // agentInstance.AddComponent<AgentBoundsHandler>().Initialize(selectedSpawnArea);
        agentInstance.AddComponent<AgentBoundsHandler>().Initialize(selectedSpawnArea, this);

        activeAgentCount[selectedSpawnArea]++;

        initialPositions[agentInstance] = spawnPosition.Value;

    }



    Vector3? SelectRandomBirthplace(BoxCollider spawnArea) // Nullable Vector3
    {
        
        // Add null check at the beginning
        if (spawnArea == null)
        {
            Debug.LogWarning("Attempted to use a destroyed spawn area BoxCollider");
            return null;
        }

        Vector3 randomDest;
        NavMeshHit hit;
        int attempts = 0;

        while (attempts < 50) 
        {
            randomDest = new Vector3(
                Random.Range(spawnArea.bounds.min.x, spawnArea.bounds.max.x),
                spawnArea.transform.position.y,
                Random.Range(spawnArea.bounds.min.z, spawnArea.bounds.max.z)
            );

            if (NavMesh.SamplePosition(randomDest, out hit, 5f, NavMesh.AllAreas) && spawnArea.bounds.Contains(hit.position))
            {
                return hit.position;
            }
            attempts++;
        }

        Debug.LogWarning("No valid spawn point found. Skipping agent creation.");
        return null; // Return null if no valid point is found
    }




    public void RemoveAgent(BoxCollider area, GameObject agent)
    {
        if (agentInstances.Contains(agent))
        {
            Debug.Log($"Removing agent {agent.name} from area {area.name}");
            agentInstances.Remove(agent);
            Destroy(agent);

            if (activeAgentCount.ContainsKey(area))
            {
                activeAgentCount[area]--;
            }
        }
    }

    public void ResetAgentPosition(GameObject agent, BoxCollider spawnArea)
    {
        
        // Before actually rerouting, check if we've done so recently
        if (lastRerouteTimes.TryGetValue(agent, out float lastTime))
        {
            if (Time.time - lastTime < rerouteCooldown)
            {
                // If we're still in cooldown, skip
                return;
            }
        }
            
        
        // Debug.Log($"Agent {agent.name} is being reset to initial spawn area...");

        if (initialPositions.TryGetValue(agent, out Vector3 initialPosition))
        {
            // Debug.Log($"Resetting agent {agent.name} to initial spawn position...");
            agent.GetComponent<NavMeshAgent>().Warp(initialPosition);
            agent.GetComponent<AgentBoundsHandler>().SetNewRandomDestination(spawnArea);

            // Record the time of this reroute
            lastRerouteTimes[agent] = Time.time;
        }
    }

    public void ResetAgentSpawning()
    {
        // Cancel any ongoing spawn invoking
        if (spawnOverTime)
        {
            CancelInvoke(nameof(RandomSpawn));
        }
        
        // Remove all existing agents
        for (int i = agentInstances.Count - 1; i >= 0; i--)
        {
            GameObject agent = agentInstances[i];
            if (agent != null)
            {
                // Get the spawn area for proper cleanup
                if (agentSpawnAreaMap.TryGetValue(agent, out BoxCollider spawnArea))
                {
                    // Proper NetworkObject cleanup before destruction
                    NetworkObject networkObject = agent.GetComponent<NetworkObject>();
                    if (networkObject != null && networkObject.IsSpawned)
                    {
                        networkObject.Despawn();
                    }
                    
                    Destroy(agent);
                    
                    // Update counters
                    if (activeAgentCount.ContainsKey(spawnArea))
                    {
                        activeAgentCount[spawnArea]--;
                    }
                }
            }
        }
        
        // Clear all collections
        agentInstances.Clear();
        agentSpawnAreaMap.Clear();
        lastPositions.Clear();
        stuckCounts.Clear();
        stuckTimers.Clear();
        initialPositions.Clear();
        lastRerouteTimes.Clear();
        
        // Reset area counters
        foreach (BoxCollider area in spawnAreas)
        {
            if (area != null)
            {
                activeAgentCount[area] = 0;
            }
        }
        
        // Restart spawning
        if (spawnOnStart)
        {
            for (int i = 0; i < initialSpawnCount; i++)
            {
                RandomSpawn();
            }
            
            if (spawnOverTime)
            {
                InvokeRepeating(nameof(RandomSpawn), spawnRate, spawnRate);
            }
        }
        
        Debug.Log($"Reset completed for {gameObject.name} with tag {spawnAreaTag}");
    }


    // private void CheckAndRerouteAgents()
    // {
    //     for (int i = agentInstances.Count - 1; i >= 0; i--)
    //     {
    //         GameObject agent = agentInstances[i];
    //         if (agent == null) continue;

    //         if (agentSpawnAreaMap.TryGetValue(agent, out BoxCollider spawnArea))
    //         {
    //             var agentHandler = agent.GetComponent<AgentBoundsHandler>();
    //             if (agentHandler && agentHandler.IsOutOfBounds(agent.transform.position, spawnArea))
    //             {
    //                 Debug.Log($"Agent {agent.name} is out of bounds. Stopping and rerouting...");
    //                 // reroute to a valid position inside bounds
    //                 agentHandler.RerouteToValidPosition(spawnArea);
    //             }
    //         }
    //     }
    // }

    private void CheckAndRerouteAgents()
    {
        foreach (var agent in agentInstances)
        {
            if (agent == null) continue;

            if (agentSpawnAreaMap.TryGetValue(agent, out BoxCollider spawnArea))
            {
                var agentHandler = agent.GetComponent<AgentBoundsHandler>();
                if (agentHandler && agentHandler.IsOutOfBounds(agent.transform.position, spawnArea))
                {
                    RerouteAgent(agent, spawnArea);
                }
                else
                {
                    CheckForStuckAgent(agent, spawnArea);
                }
            }
        }
    }

    private void CheckForStuckAgent(GameObject agent, BoxCollider spawnArea)
    {
        if (!lastPositions.ContainsKey(agent))
        {
            lastPositions[agent] = agent.transform.position;
            stuckCounts[agent] = 0;
            stuckTimers[agent] = Time.time;
            return;
        }

        if (Vector3.Distance(lastPositions[agent], agent.transform.position) < 0.01f)
        {
            stuckCounts[agent]++;
            if (!stuckTimers.ContainsKey(agent)) stuckTimers[agent] = Time.time;
        }
        else
        {
            stuckCounts[agent] = 0;
            stuckTimers[agent] = Time.time;
        }

        lastPositions[agent] = agent.transform.position;

        if (stuckCounts[agent] >= stuckThreshold)
        {   
            AgentBoundsHandler agentHandler = agent.GetComponent<AgentBoundsHandler>();
            if (agentHandler != null && Time.time - stuckTimers[agent] >= stuckTimeThreshold && !agentHandler.IsPlayerNearby())            {
                ResetAgentPosition(agent, spawnArea);
                stuckCounts[agent] = 0;
                stuckTimers[agent] = Time.time;
            }
            else
            {
                RerouteAgent(agent, spawnArea);
            }
        }
    }





    private void RerouteAgent(GameObject agent, BoxCollider spawnArea)
    {
        
        // Before actually rerouting, check if we've done so recently
        if (lastRerouteTimes.TryGetValue(agent, out float lastTime))
        {
            if (Time.time - lastTime < rerouteCooldown)
            {
                // If we're still in cooldown, skip
                return;
            }
        }
        
        // Debug.Log($"Agent {agent.name} is being rerouted...");

        NavMeshAgent navAgent = agent.GetComponent<NavMeshAgent>();
        if (navAgent == null) return;

        Vector3 oppositeDirection = agent.transform.position - spawnArea.bounds.center;
        Vector3 tempPosition = agent.transform.position + oppositeDirection.normalized * 5f;
        if (NavMesh.SamplePosition(tempPosition, out NavMeshHit tempHit, 1f, NavMesh.AllAreas))
        {
            navAgent.ResetPath();
            navAgent.SetDestination(tempHit.position);
        }

        // Record the time of this reroute
        lastRerouteTimes[agent] = Time.time;

        agent.GetComponent<AgentBoundsHandler>().Invoke(nameof(AgentBoundsHandler.RerouteToValidPosition), 2f);
    }
}


////////////////////////////////////////////////////////////////////////////////////////////////////////


public class AgentBoundsHandler : MonoBehaviour
{
    private NavMeshAgent agent;
    private BoxCollider currentBounds;
    private CrowdAgentManagerMulti myManager;

    public void Initialize(BoxCollider bounds, CrowdAgentManagerMulti manager)
    {
        myManager = manager;
        agent = GetComponent<NavMeshAgent>();
        currentBounds = bounds;

        if (agent == null)
        {
            Debug.LogError("NavMeshAgent is missing on agent: " + gameObject.name);
            return;
        }
        SetNewRandomDestination(bounds);
    }


    public bool IsOutOfBounds(Vector3 position, BoxCollider bounds)
    {
        return !bounds.bounds.Contains(position);
    }

    // Parameterless method:
    public void RerouteToValidPosition()
    {
        RerouteToValidPosition(currentBounds);
    }

    public void RerouteToValidPosition(BoxCollider bounds)
    {
        if (agent == null) return;

        // Stop the agent before rerouting
        // agent.isStopped = true;
        agent.ResetPath(); // Clear existing path

        // // Move agent in the opposite direction
        // Vector3 oppositeDirection = transform.position - bounds.bounds.center;
        // Vector3 tempPosition = transform.position + oppositeDirection.normalized * 2f; // Move 2 units away
        // if (NavMesh.SamplePosition(tempPosition, out NavMeshHit hit, 1f, NavMesh.AllAreas))
        // {
        //     transform.position = hit.position; // Temporarily reposition to escape tight space
        // }

        // Try to reroute to a valid position inside bounds
        Vector3 newDestination = GetRandomPointInsideBounds(bounds);
        if (newDestination != Vector3.zero) 
        {
            agent.SetDestination(newDestination);
            agent.isStopped = false;
        }
        else
        {
            // If no valid reroute found, check for nearby player before deleting
            if (!IsPlayerNearby())
            {
                // Find the manager and remove the agent properly
                if (myManager != null)
                {
                    if (myManager.agentSpawnAreaMap.TryGetValue(gameObject, out BoxCollider spawnArea))  // ✅ Fixed: Declare `spawnArea` before using it
                    {
                        myManager.RemoveAgent(spawnArea, gameObject);
                    }
                    else
                    {
                        Debug.LogError($"Failed to remove agent {gameObject.name} - Spawn area missing.");
                    }
                }
                else
                {
                    Debug.LogError($"Failed to remove agent {gameObject.name} - Manager is null.");
                }
            }
        }
    }


    private Vector3 GetRandomPointInsideBounds(BoxCollider bounds)
    {
        Vector3 randomPos;
        NavMeshHit hit;
        int attempts = 0;

        while (attempts < 10) // Try up to 10 times
        {
            randomPos = new Vector3(
                Random.Range(bounds.bounds.min.x, bounds.bounds.max.x),
                bounds.transform.position.y,
                Random.Range(bounds.bounds.min.z, bounds.bounds.max.z)
            );

            if (NavMesh.SamplePosition(randomPos, out hit, 5f, NavMesh.AllAreas) && bounds.bounds.Contains(hit.position))
            {
                return hit.position;
            }

            attempts++;
        }

        Debug.LogError("No valid spawn point found after multiple attempts.");
        return Vector3.zero; // Return zero to indicate failure
    }

    public void SetNewRandomDestination(BoxCollider bounds)
    {
        if (bounds != null && agent != null)
        {
            Vector3 destination = GetRandomPointInsideBounds(bounds);
            if (destination != Vector3.zero)
            {
                agent.SetDestination(destination);
            }
        }
    }

    public bool IsPlayerNearby()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && agent != null)
        {
            float distance = Vector3.Distance(agent.transform.position, player.transform.position);
            return distance < 10f;
        }
        return false;
    }


}
