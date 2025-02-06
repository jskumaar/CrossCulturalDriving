// using System;
// using System.Collections.Generic;
// using Unity.Netcode;
// using UnityEngine;
// using UnityEngine.AI;
// using Random = UnityEngine.Random;

// public class CrowdAgentManagerMulti : NetworkBehaviour
// {
//     public GameObject[] agentPrefabs;
//     public int initialSpawnCount = 10;

//     public Transform agentSpawn;

//     [SerializeField]
//     private List<GameObject> agentInstances = new List<GameObject>();

//     public int maxAgentCount = 10;
//     public bool spawnOnStart = true;
//     public bool spawnOverTime = true;
//     public float spawnRate = 1f;

//     // NPC change
//     private List<BoxCollider> blockAreas;
//     public static CrowdAgentManagerMulti Singleton;

//     public bool dummyTrafficLight = false;

//     // Newly added (02/05/25)
//     private List<BoxCollider> spawnAreas = new List<BoxCollider>(); // FIX: Initialize list
//     public int maxAgentCountPerArea = 10;
//     private Dictionary<BoxCollider, int> activeAgentCount = new Dictionary<BoxCollider, int>();

//     void Awake()
//     {
//         if (Singleton)
//         {
//             Destroy(gameObject);
//             return;
//         }
//         Singleton = this;
//     }

//     void Start()
//     {
//         // Set up block areas
//         blockAreas = BlocksManager.GetBuildingBlocks();
//         if (blockAreas.Count <= 0)
//         {
//             Debug.Log("Cannot get building blocks.");
//         }

//         if (!IsServer)
//         {
//             Destroy(this);
//             return;
//         }

//         // Initialize spawn areas
//         BoxCollider[] foundSpawnAreas = FindObjectsOfType<BoxCollider>();
//         foreach (var area in foundSpawnAreas)
//         {
//             if (area.gameObject.CompareTag("SpawnArea")) // Ensure only valid spawn areas are used
//             {
//                 spawnAreas.Add(area);
//                 activeAgentCount[area] = 0; // Initialize pedestrian count per area
//             }
//         }

//         if (spawnAreas.Count == 0)
//         {
//             Debug.LogError("No spawn areas defined. Make sure to tag spawn areas as 'SpawnArea'.");
//             return;
//         }

//         // Spawn pedestrians at the start
//         for (int i = 0; i < initialSpawnCount; i++)
//         {
//             RandomSpawn();
//         }

//         // Continue spawning over time
//         if (spawnOverTime)
//         {
//             InvokeRepeating(nameof(RandomSpawn), spawnRate, spawnRate);
//         }
//     }

//     private void Update()
//     {
//         if (Input.GetKeyDown(KeyCode.P))
//         {
//             AgentSetup(null);
//         }
//     }

//     public void AgentSetup(Transform parent)
//     {
//         if (parent)
//         {
//             transform.parent = parent;
//             transform.localPosition = Vector3.zero;
//         }

//         // Make sure a BoxCollider is attached
//         BoxCollider spawnArea = gameObject.GetComponent<BoxCollider>();
//         if (!spawnArea)
//         {
//             enabled = false;
//             return;
//         }
//         spawnArea.isTrigger = true;

//         for (int i = 0; i < initialSpawnCount; i++)
//         {
//             RandomSpawn();
//         }

//         if (spawnOverTime)
//         {
//             InvokeRepeating(nameof(RandomSpawn), spawnRate, spawnRate);
//         }
//     }

//     void RandomSpawn()
//     {
//         if (spawnAreas.Count == 0)
//         {
//             Debug.LogError("No spawn areas available.");
//             return;
//         }

//         // Attempt to find a valid spawn area that has not reached its limit
//         List<BoxCollider> availableSpawnAreas = new List<BoxCollider>();
//         foreach (var area in spawnAreas)
//         {
//             if (activeAgentCount[area] < maxAgentCountPerArea)
//             {
//                 availableSpawnAreas.Add(area);
//             }
//         }

//         // If no areas are available, stop spawning
//         if (availableSpawnAreas.Count == 0)
//         {
//             Debug.Log("All spawn areas have reached their pedestrian limits.");
//             return;
//         }

//         // Randomly select from available spawn areas
//         BoxCollider selectedSpawnArea = availableSpawnAreas[Random.Range(0, availableSpawnAreas.Count)];

//         // Find a valid spawn point within the selected area
//         Vector3 spawnPosition = SelectRandomBirthplace(selectedSpawnArea);
//         if (spawnPosition == Vector3.one * -1)
//         {
//             Debug.LogWarning("No valid spawn position found.");
//             return;
//         }

//         // Instantiate and register the agent
//         GameObject randomPrefab = agentPrefabs[Random.Range(0, agentPrefabs.Length)];
//         GameObject agentInstance = Instantiate(randomPrefab, spawnPosition, Quaternion.identity);

//         agentInstance.GetComponent<NetworkObject>().Spawn();
//         agentInstance.transform.parent = agentSpawn;
//         agentInstances.Add(agentInstance);

//         // Assign a callback to remove the agent when destroyed
//         CrowdAgent agentComponent = agentInstance.GetComponent<CrowdAgent>();
//         if (agentComponent)
//         {
//             agentComponent.onDestroyed += () => RemoveAgent(selectedSpawnArea, agentInstance);
//         }

//         // Increase the pedestrian count for this area
//         activeAgentCount[selectedSpawnArea]++;
//     }

//     Vector3 SelectRandomBirthplace(BoxCollider spawnArea)
//     {
//         Vector3 randomDest;
//         NavMeshHit hit;
//         bool foundValidSpawn = false;
//         int attempts = 0;

//         while (!foundValidSpawn && attempts < 10) // Try up to 10 times
//         {
//             randomDest = new Vector3(
//                 Random.Range(spawnArea.bounds.min.x, spawnArea.bounds.max.x),
//                 spawnArea.transform.position.y,
//                 Random.Range(spawnArea.bounds.min.z, spawnArea.bounds.max.z)
//             );

//             if (NavMesh.SamplePosition(randomDest, out hit, 5f, NavMesh.AllAreas))
//             {
//                 foundValidSpawn = true;
//             }
//             attempts++;
//         }

//         return foundValidSpawn ? hit.position : Vector3.one * -1;
//     }

//     public void RemoveAgent(BoxCollider area, GameObject agent)
//     {
//         if (agentInstances.Contains(agent))
//         {
//             agentInstances.Remove(agent);
//             Destroy(agent);

//             if (activeAgentCount.ContainsKey(area))
//             {
//                 activeAgentCount[area]--;
//             }
//         }
//     }
// }
