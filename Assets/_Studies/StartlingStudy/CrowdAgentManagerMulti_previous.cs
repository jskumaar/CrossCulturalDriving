using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class CrowdAgentManagerMultiPrevious : NetworkBehaviour
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
    public static CrowdAgentManagerMultiPrevious Singleton;

    public bool dummyTrafficLight = false;

    // Newly added (02/05/25)
    private List<BoxCollider> spawnAreas = new List<BoxCollider>(); // FIX: Initialize list
    public int maxAgentCountPerArea = 10;
    private Dictionary<BoxCollider, int> activeAgentCount = new Dictionary<BoxCollider, int>();
    private Dictionary<GameObject, BoxCollider> agentSpawnAreaMap = new Dictionary<GameObject, BoxCollider>(); // Track spawn area for each agent


    void Awake()
    {
        if (Singleton)
        {
            Destroy(gameObject);
            return;
        }
        Singleton = this;
    }

    void Start()
    {
        // Set up block areas
        blockAreas = BlocksManager.GetBuildingBlocks();
        if (blockAreas.Count <= 0)
        {
            Debug.Log("Cannot get building blocks.");
        }

        if (!IsServer)
        {
            Destroy(this);
            return;
        }

        // Initialize spawn areas
        BoxCollider[] foundSpawnAreas = FindObjectsOfType<BoxCollider>();
        foreach (var area in foundSpawnAreas)
        {
            if (area.gameObject.CompareTag("CrowdAgent"))
            {
                spawnAreas.Add(area);
                activeAgentCount[area] = 0;
            }
        }

        if (spawnAreas.Count == 0)
        {
            Debug.LogError("No spawn areas defined. Make sure to tag spawn areas as 'SpawnArea'.");
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
        if (Input.GetKeyDown(KeyCode.P))
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

    // void RandomSpawn()
    // {
    //     if (spawnAreas.Count == 0)
    //     {
    //         Debug.LogError("No spawn areas available.");
    //         return;
    //     }

    //     // Attempt to find a valid spawn area that has not reached its limit
    //     List<BoxCollider> availableSpawnAreas = new List<BoxCollider>();
    //     foreach (var area in spawnAreas)
    //     {
    //         if (activeAgentCount[area] < maxAgentCountPerArea)
    //         {
    //             availableSpawnAreas.Add(area);
    //         }
    //     }

    //     // If no areas are available, stop spawning
    //     if (availableSpawnAreas.Count == 0)
    //     {
    //         Debug.Log("All spawn areas have reached their pedestrian limits.");
    //         return;
    //     }

    //     // Randomly select from available spawn areas
    //     BoxCollider selectedSpawnArea = availableSpawnAreas[Random.Range(0, availableSpawnAreas.Count)];

    //     // Find a valid spawn point within the selected area
    //     Vector3 spawnPosition = SelectRandomBirthplace(selectedSpawnArea);
    //     if (spawnPosition == Vector3.one * -1)
    //     {
    //         Debug.LogWarning("No valid spawn position found.");
    //         return;
    //     }

    //     // Instantiate and register the agent
    //     GameObject randomPrefab = agentPrefabs[Random.Range(0, agentPrefabs.Length)];
    //     GameObject agentInstance = Instantiate(randomPrefab, spawnPosition, Quaternion.identity);

    //     agentInstance.GetComponent<NetworkObject>().Spawn();
    //     agentInstance.transform.parent = agentSpawn;
    //     agentInstances.Add(agentInstance);

    //     // // Assign a callback to remove the agent when destroyed
    //     // CrowdAgent agentComponent = agentInstance.GetComponent<CrowdAgent>();
    //     // if (agentComponent)
    //     // {
    //     //     agentComponent.onDestroyed += () => RemoveAgent(selectedSpawnArea, agentInstance);
    //     // }

    //     // Increase the pedestrian count for this area
    //     activeAgentCount[selectedSpawnArea]++;

    //     if (agentInstances.Count >= maxAgentCount)
    //     {
    //         CancelInvoke(nameof(RandomSpawn));
    //         Debug.Log("Max agent count reached. Stopping spawn.");
    //     }
    // }

    void RandomSpawn()
    {
        if (spawnAreas.Count == 0) return;

        List<BoxCollider> availableSpawnAreas = spawnAreas.FindAll(a => activeAgentCount[a] < maxAgentCountPerArea);
        if (availableSpawnAreas.Count == 0) return;

        BoxCollider selectedSpawnArea = availableSpawnAreas[Random.Range(0, availableSpawnAreas.Count)];
        Vector3? spawnPosition = SelectRandomBirthplace(selectedSpawnArea);
        if (spawnPosition == null) return;

        // Print the spawn area information
        Debug.Log($"Spawning agent in area: {selectedSpawnArea.name} with bounds: " +
                $"Center({selectedSpawnArea.bounds.center}), " +
                $"Size({selectedSpawnArea.bounds.size})");

        GameObject randomPrefab = agentPrefabs[Random.Range(0, agentPrefabs.Length)];
        GameObject agentInstance = Instantiate(randomPrefab, spawnPosition.Value, Quaternion.identity);

        agentInstance.GetComponent<NetworkObject>().Spawn();
        agentInstance.transform.parent = agentSpawn;
        agentInstances.Add(agentInstance);

        // Post-spawn validation
        if (!selectedSpawnArea.bounds.Contains(agentInstance.transform.position) ||
            !NavMesh.SamplePosition(agentInstance.transform.position, out _, 1f, NavMesh.AllAreas))
        {
            Debug.LogWarning("Agent spawned outside bounds or off NavMesh. Destroying and retrying...");
            RemoveAgent(selectedSpawnArea, agentInstance);
            RandomSpawn(); // Retry spawning
            return;
        }

        Debug.Log($"Agent spawned at {agentInstance.transform.position} inside {selectedSpawnArea.name}");

        // Store the spawn area for this agent
        agentSpawnAreaMap[agentInstance] = selectedSpawnArea;

        agentInstance.AddComponent<AgentBoundsHandler>().Initialize(selectedSpawnArea);
        activeAgentCount[selectedSpawnArea]++;
    }


    // Vector3 SelectRandomBirthplace(BoxCollider spawnArea)
    // {
    //     Vector3 randomDest;
    //     NavMeshHit hit;
    //     bool foundValidSpawn = false;
    //     int attempts = 0;

    //     while (!foundValidSpawn && attempts < 10) // Try up to 10 times
    //     {
    //         randomDest = new Vector3(
    //             Random.Range(spawnArea.bounds.min.x, spawnArea.bounds.max.x),
    //             spawnArea.transform.position.y,
    //             Random.Range(spawnArea.bounds.min.z, spawnArea.bounds.max.z)
    //         );

    //         if (NavMesh.SamplePosition(randomDest, out hit, 5f, NavMesh.AllAreas))
    //         {
    //             foundValidSpawn = true;
    //         }
    //         attempts++;
    //     }

    //     return foundValidSpawn ? hit.position : Vector3.one * -1;
    // }

    // Vector3 SelectRandomBirthplace(BoxCollider spawnArea)
    // {
    //     Vector3 randomDest = Vector3.zero;
    //     int attempts = 0;

    //     while (attempts < 10) // Try up to 10 times
    //     {
    //         randomDest = new Vector3(
    //             Random.Range(spawnArea.bounds.min.x, spawnArea.bounds.max.x),
    //             spawnArea.transform.position.y,
    //             Random.Range(spawnArea.bounds.min.z, spawnArea.bounds.max.z)
    //         );

    //         if (NavMesh.SamplePosition(randomDest, out NavMeshHit hit, 5f, NavMesh.AllAreas))
    //         {
    //             return hit.position; // Return immediately when a valid position is found
    //         }
    //         attempts++;
    // }

    // // Return an invalid position if no valid spawn point is found
    // return Vector3.one * -1;
    // }

    // Vector3 SelectRandomBirthplace(BoxCollider spawnArea)
    // {
    //     Vector3 randomDest;
    //     NavMeshHit hit;
    //     int attempts = 0;

    //     while (attempts < 50) // Increased attempts
    //     {
    //         randomDest = new Vector3(
    //             Random.Range(spawnArea.bounds.min.x, spawnArea.bounds.max.x),
    //             spawnArea.transform.position.y,
    //             Random.Range(spawnArea.bounds.min.z, spawnArea.bounds.max.z)
    //         );

    //         if (NavMesh.SamplePosition(randomDest, out hit, 5f, NavMesh.AllAreas))
    //         {
    //             if (spawnArea.bounds.Contains(hit.position)) // Confirm within bounding box
    //             {
    //                 return hit.position;
    //             }
    //         }
    //         attempts++;
    //     }

    //     Debug.LogError("No valid spawn point found after multiple attempts.");
    //     return Vector3.one * -1;
    // }

    Vector3? SelectRandomBirthplace(BoxCollider spawnArea) // Nullable Vector3
    {
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
            agentInstances.Remove(agent);
            Destroy(agent);

            if (activeAgentCount.ContainsKey(area))
            {
                activeAgentCount[area]--;
            }
        }
    }

    private void CheckAndRerouteAgents()
    {
        foreach (var agent in agentInstances)
        {
            if (agentSpawnAreaMap.TryGetValue(agent, out BoxCollider spawnArea))
            {
                var agentHandler = agent.GetComponent<AgentBoundsHandler>();
                if (agentHandler && agentHandler.IsOutOfBounds(agent.transform.position, spawnArea))
                {
                    Debug.Log($"Agent {agent.name} is out of bounds. Rerouting...");
                    agentHandler.RerouteToValidPosition(spawnArea);
                }
            }
        }
    }
}


public class AgentBoundsHandler : MonoBehaviour
{
    private NavMeshAgent agent;

    public void Initialize(BoxCollider bounds)
    {
        agent = GetComponent<NavMeshAgent>();
        SetNewRandomDestination(bounds);
    }

    public bool IsOutOfBounds(Vector3 position, BoxCollider bounds)
    {
        return position.x < bounds.bounds.min.x ||
               position.x > bounds.bounds.max.x ||
               position.z < bounds.bounds.min.z ||
               position.z > bounds.bounds.max.z;
    }

    public void RerouteToValidPosition(BoxCollider bounds)
    {
        Vector3 newDestination = GetRandomPointInsideBounds(bounds);
        agent.SetDestination(newDestination);
    }

    private Vector3 GetRandomPointInsideBounds(BoxCollider bounds)
    {
        Vector3 randomPos;
        NavMeshHit hit;
        int attempts = 0;

        do
        {
            randomPos = new Vector3(
                Random.Range(bounds.bounds.min.x, bounds.bounds.max.x),
                transform.position.y,
                Random.Range(bounds.bounds.min.z, bounds.bounds.max.z)
            );
            attempts++;
        } while (!NavMesh.SamplePosition(randomPos, out hit, 5f, NavMesh.AllAreas) && attempts < 10);

        return hit.position;
    }

    public void SetNewRandomDestination(BoxCollider bounds)
    {
        if (bounds != null && agent != null)
        {
            Vector3 destination = GetRandomPointInsideBounds(bounds);
            agent.SetDestination(destination);
        }
    }
}
