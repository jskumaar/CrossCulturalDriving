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

//     public int maxAgentCount = 50;
//     public bool spawnOnStart = false;
//     public bool spawnOverTime = true;
//     public float spawnRate = 1f;

//     private List<BoxCollider> blockAreas;
//     public static CrowdAgentManagerMulti Singleton;

//     private List<BoxCollider> spawnAreas = new List<BoxCollider>();
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

//         BoxCollider[] foundSpawnAreas = FindObjectsOfType<BoxCollider>();
//         foreach (var area in foundSpawnAreas)
//         {
//             spawnAreas.Add(area);
//             activeAgentCount[area] = 0;
//         }

//         if (spawnAreas.Count == 0)
//         {
//             Debug.LogError("No spawn areas defined. Make sure to tag spawn areas as 'SpawnArea'.");
//             return;
//         }

//         for (int i = 0; i < initialSpawnCount; i++)
//         {
//             RandomSpawn();
//         }

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

//         // Continuously check and reroute pedestrians
//         CheckAndRerouteAgents();
//     }

//     public void AgentSetup(Transform parent)
//     {
//         if (parent)
//         {
//             transform.parent = parent;
//             transform.localPosition = Vector3.zero;
//         }

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

//         List<BoxCollider> availableSpawnAreas = new List<BoxCollider>();
//         foreach (var area in spawnAreas)
//         {
//             if (activeAgentCount[area] < maxAgentCountPerArea)
//             {
//                 availableSpawnAreas.Add(area);
//             }
//         }

//         if (availableSpawnAreas.Count == 0)
//         {
//             Debug.Log("All spawn areas have reached their pedestrian limits.");
//             return;
//         }

//         BoxCollider selectedSpawnArea = availableSpawnAreas[Random.Range(0, availableSpawnAreas.Count)];
//         Vector3 spawnPosition = SelectRandomBirthplace(selectedSpawnArea);
//         if (spawnPosition == Vector3.one * -1)
//         {
//             Debug.LogWarning("No valid spawn position found.");
//             return;
//         }

//         GameObject randomPrefab = agentPrefabs[Random.Range(0, agentPrefabs.Length)];
//         GameObject agentInstance = Instantiate(randomPrefab, spawnPosition, Quaternion.identity);

//         agentInstance.GetComponent<NetworkObject>().Spawn();
//         agentInstance.transform.parent = agentSpawn;
//         agentInstances.Add(agentInstance);

//         // Assign movement bounds via a component (or directly here)
//         agentInstance.AddComponent<AgentBoundsHandler>().Initialize(selectedSpawnArea);

//         activeAgentCount[selectedSpawnArea]++;

//         if (agentInstances.Count >= maxAgentCount)
//         {
//             CancelInvoke(nameof(RandomSpawn));
//             Debug.Log("Max agent count reached. Stopping spawn.");
//         }
//     }

//     Vector3 SelectRandomBirthplace(BoxCollider spawnArea)
//     {
//         Vector3 randomDest = Vector3.zero;
//         int attempts = 0;

//         while (attempts < 10)
//         {
//             randomDest = new Vector3(
//                 Random.Range(spawnArea.bounds.min.x, spawnArea.bounds.max.x),
//                 spawnArea.transform.position.y,
//                 Random.Range(spawnArea.bounds.min.z, spawnArea.bounds.max.z)
//             );

//             if (NavMesh.SamplePosition(randomDest, out NavMeshHit hit, 5f, NavMesh.AllAreas))
//             {
//                 return hit.position;
//             }
//             attempts++;
//         }

//         return Vector3.one * -1;
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

//     private void CheckAndRerouteAgents()
//     {
//         foreach (var agent in agentInstances)
//         {
//             var agentHandler = agent.GetComponent<AgentBoundsHandler>();
//             if (agentHandler && agentHandler.IsOutOfBounds(agent.transform.position))
//             {
//                 agentHandler.RerouteToValidPosition();
//             }
//         }
//     }
// }

// public class AgentBoundsHandler : MonoBehaviour
// {
//     private BoxCollider movementBounds;
//     private NavMeshAgent agent;

//     public void Initialize(BoxCollider bounds)
//     {
//         movementBounds = bounds;
//         agent = GetComponent<NavMeshAgent>();
//         SetNewRandomDestination();
//     }

//     public bool IsOutOfBounds(Vector3 position)
//     {
//         return position.x < movementBounds.bounds.min.x ||
//                position.x > movementBounds.bounds.max.x ||
//                position.z < movementBounds.bounds.min.z ||
//                position.z > movementBounds.bounds.max.z;
//     }

//     public void RerouteToValidPosition()
//     {
//         Vector3 newDestination = GetRandomPointInsideBounds();
//         agent.SetDestination(newDestination);
//     }

//     private Vector3 GetRandomPointInsideBounds()
//     {
//         Vector3 randomPos;
//         NavMeshHit hit;
//         int attempts = 0;

//         do
//         {
//             randomPos = new Vector3(
//                 Random.Range(movementBounds.bounds.min.x, movementBounds.bounds.max.x),
//                 transform.position.y,
//                 Random.Range(movementBounds.bounds.min.z, movementBounds.bounds.max.z)
//             );
//             attempts++;
//         } while (!NavMesh.SamplePosition(randomPos, out hit, 5f, NavMesh.AllAreas) && attempts < 10);

//         return hit.position;
//     }

//     public void SetNewRandomDestination()
//     {
//         if (movementBounds != null && agent != null)
//         {
//             Vector3 destination = GetRandomPointInsideBounds();
//             agent.SetDestination(destination);
//         }
//     }
// }
