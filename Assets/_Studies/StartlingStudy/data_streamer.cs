// // using System;
// // using System.Collections.Generic;
// // using System.Net.Sockets;
// // using System.Text;
// // using System.Threading;
// // using UnityEngine;

// // public class VehicleDataBroadcaster : MonoBehaviour
// // {
// //     public static VehicleDataBroadcaster Instance;  // Singleton instance

// //     private TcpClient client;
// //     private NetworkStream stream;
// //     private Thread sendThread;
// //     private bool isStreaming = true;

// //     private Queue<string> triggerMessages = new Queue<string>(); // Queue for trigger messages

// //     void Awake()
// //     {
// //         if (Instance == null) Instance = this;
// //     }

// //     void Start()
// //     {
// //         sendThread = new Thread(ConnectionLoop);
// //         sendThread.Start();
// //     }

// //     void ConnectionLoop()
// //     {   
// //         while (isStreaming)
// //         {
// //             Debug.Log($"isStreaming: {isStreaming}");
// //             Debug.Log($"client: {client}");
            
// //             if (client == null || !client.Connected)
// //             {
// //                 Debug.Log("Server not connected. Retrying...");
// //                 ConnectToServer();
// //                 Thread.Sleep(2000);
// //             }
// //             else
// //             {

// //                 SendData();
// //             }
// //         }
// //     }

// //     void ConnectToServer()
// //     {
// //         try
// //         {
// //             client = new TcpClient("127.0.0.1", 5006);
// //             stream = client.GetStream();
// //             Debug.Log("Connected to TCP server.");
// //         }
// //         catch (Exception e)
// //         {
// //             Debug.LogError("Failed to connect: " + e.Message);
// //         }
// //     }

// //     void SendData()
// //     {
// //         try
// //         {
// //             Debug.Log($"Sending data... Still {triggerMessages.Count} messages in queue.");
// //             while (triggerMessages.Count > 0)
// //             {
// //                 string data = triggerMessages.Dequeue();
// //                 byte[] message = Encoding.UTF8.GetBytes(data + "\n");
// //                 stream.Write(message, 0, message.Length);
// //                 Debug.Log($"Sending data... Still {triggerMessages.Count} messages in queue.");
// //             }
// //         }
// //         catch (Exception e)
// //         {
// //             Debug.LogError("Error sending data: " + e.Message);
// //             DisconnectServer();
// //         }

// //         Thread.Sleep(100);
// //     }

// //     public void TriggerEntered(string triggerID)
// //     {
// //         string message = $"TRIGGER_ENTERED: {triggerID}";
// //         Debug.Log(message);
// //         triggerMessages.Enqueue(message);
// //     }

// //     public void TriggerExited(string triggerID)
// //     {
// //         string message = $"TRIGGER_EXITED: {triggerID}";
// //         Debug.Log(message);
// //         triggerMessages.Enqueue(message);
// //     }

// //     void DisconnectServer()
// //     {
// //         if (stream != null) stream.Close();
// //         if (client != null) client.Close();
// //         client = null;
// //     }

// //     void OnApplicationQuit()
// //     {
// //         isStreaming = false;
// //         DisconnectServer();
// //         if (sendThread != null) sendThread.Abort();
// //     }
// // }


// ///////////////////


// // using System;
// // using System.Collections.Generic;
// // using System.Net.Sockets;
// // using System.Text;
// // using UnityEngine;

// // public class VehicleDataBroadcaster : MonoBehaviour
// // {
// //     public static VehicleDataBroadcaster Instance;  // Singleton instance
// //     private TcpClient client;
// //     private NetworkStream stream;

// //     void Awake()
// //     {
// //         if (Instance == null) Instance = this;
// //     }

// //     void Start()
// //     {
// //         ConnectToServer();
// //     }

// //     void ConnectToServer()
// //     {
// //         try
// //         {
// //             client = new TcpClient("127.0.0.1", 5006);
// //             stream = client.GetStream();
// //             Debug.Log("[VDB] Connected to TCP server.");
// //         }
// //         catch (Exception e)
// //         {
// //             Debug.LogError("[VDB] Failed to connect: " + e.Message);
// //         }
// //     }

// //     void SendData(string data)
// //     {
// //         try
// //         {
// //             if (client == null || !client.Connected)
// //             {
// //                 Debug.Log("[VDB] Reconnecting to server...");
// //                 ConnectToServer();
// //             }

// //             if (client != null && client.Connected)
// //             {
// //                 byte[] message = Encoding.UTF8.GetBytes(data + "\n");
// //                 stream.Write(message, 0, message.Length);
// //                 stream.Flush();  // Ensure data is sent immediately
// //                 Debug.Log($"[VDB] Sent message: {data}");
// //             }
// //         }
// //         catch (Exception e)
// //         {
// //             Debug.LogError("[VDB] Error sending data: " + e.Message);
// //             DisconnectServer();
// //         }
// //     }

// //     public void TriggerEntered(string triggerID)
// //     {
// //         string message = $"TRIGGER_ENTERED: {triggerID}";
// //         Debug.Log($"[VDB] Sending: {message}");
// //         SendData(message);
// //     }

// //     public void TriggerExited(string triggerID)
// //     {
// //         string message = $"TRIGGER_EXITED: {triggerID}";
// //         Debug.Log($"[VDB] Sending: {message}");
// //         SendData(message);
// //     }

// //     void DisconnectServer()
// //     {
// //         if (stream != null) stream.Close();
// //         if (client != null) client.Close();
// //         client = null;
// //     }

// //     void OnApplicationQuit()
// //     {
// //         DisconnectServer();
// //     }
// // }

// ////////////////////

// using System;
// using System.Net.Sockets;
// using System.Text;
// using System.Threading;
// using UnityEngine;

// public class VehicleDataBroadcaster : MonoBehaviour
// {
//     public static VehicleDataBroadcaster Instance;  // Singleton instance
//     private TcpClient client;
//     private NetworkStream stream;

//     void Awake()
//     {
//         if (Instance == null) 
//         {
//             Instance = this;
//         }
//         else if (Instance != this)
//         {
//             Destroy(this);  // Destroy only the script instance, not the entire GameObject
//         }
//     }

//     // void Awake()
//     // {
//     //     if (Instance == null)
//     //     {
//     //         Instance = this;
//     //         DontDestroyOnLoad(gameObject);  // Keep instance across scenes
//     //     }
//     //     else
//     //     {
//     //         Destroy(gameObject);  // Prevent multiple instances
//     //     }
//     //     Debug.Log("[VDB] Instance created");
//     // }

//     void Start()
//     {
//         ConnectToServer();
//     }

//     void Update()
//     {
//         if (client != null)
//         {
//             Debug.Log($"[VDB] Client connected: {client.Connected}");
//         }
//         else
//         {
//             Debug.Log("[VDB] Client is null.");
//         }

//         if (stream != null)
//         {
//             Debug.Log("[VDB] Stream is initialized.");
//         }
//         else
//         {
//             Debug.Log("[VDB] Stream is null.");
//         }
//     }


//     void ConnectToServer()
//     {
//         try
//         {
//             client = new TcpClient();
//             client.NoDelay = true;  // Disable Nagle's Algorithm for immediate sending
//             client.Connect("127.0.0.1", 5006);
//             stream = client.GetStream();
//             Debug.Log("[VDB] Connected to TCP server.");
//         }
//         catch (Exception e)
//         {
//             Debug.LogError("[VDB] Failed to connect: " + e.Message);
//         }
//     }

//     // void SendData(string data)
//     // {
//     //     // Run the network sending on a new thread
//     //     new Thread(() =>
//     //     {
//     //         try
//     //         {
//     //             if (client == null || !client.Connected)
//     //             {
//     //                 Debug.Log("[VDB] Reconnecting to server...");
//     //                 ConnectToServer();
//     //             }

//     //             if (client != null && client.Connected)
//     //             {
//     //                 byte[] message = Encoding.UTF8.GetBytes(data + "\n");
//     //                 stream.Write(message, 0, message.Length);
//     //                 stream.Flush();  // Ensure data is sent immediately
//     //                 Debug.Log($"[VDB] Sent message: {data}");
//     //             }
//     //         }
//     //         catch (Exception e)
//     //         {
//     //             Debug.LogError("[VDB] Error sending data: " + e.Message);
//     //             DisconnectServer();
//     //         }
//     //     }).Start();  // Start the thread immediately
//     // }

//     // public void TriggerEntered(string triggerID)
//     // {
//     //     string message = $"TRIGGER_ENTERED: {triggerID}";
//     //     Debug.Log($"[VDB] Sending: {message}");
//     //     SendData(message);
//     // }

//     // public void TriggerExited(string triggerID)
//     // {
//     //     string message = $"TRIGGER_EXITED: {triggerID}";
//     //     Debug.Log($"[VDB] Sending: {message}");
//     //     SendData(message);
//     // }


//     public void SendMessageToServer(string message)
//     {
//         Debug.Log("Sending message to server: " + message);
//         Debug.Log($"Client object is {(client == null ? "NULL" : "NOT NULL")}");
//         Debug.Log($"Stream object is {(stream == null ? "NULL" : "NOT NULL")}");
//         if (client != null && client.Connected)
//         {
//             Debug.Log("Client is connected. Sending message...");
//             byte[] messageBytes = Encoding.UTF8.GetBytes(message + "\n");
//         }

//         try
//         {
//             if (client == null || !client.Connected)
//             {
//                 Debug.Log("[VDB] Reconnecting to server...");
//                 ConnectToServer();
//             }

//             if (client != null && client.Connected)
//             {
//                 byte[] messageBytes = Encoding.UTF8.GetBytes(message + "\n");
//                 stream.Write(messageBytes, 0, messageBytes.Length);
//                 stream.Flush();  // Ensure data is sent immediately
//                 Debug.Log("Sent: " + message);
//             }
//         }
//         catch (Exception e)
//         {
//             Debug.Log("Error sending message: " + e.Message);
//             DisconnectServer();
//         }

//     }

//     void DisconnectServer()
//     {
//         if (stream != null) stream.Close();
//         if (client != null) client.Close();
//         client = null;
//     }

//     void OnApplicationQuit()
//     {
//         DisconnectServer();
//     }
// }


// // //////////////   With UDP   //////////////////////

// // // Unity (C#) UDP Client Code
// // using System;
// // using System.Net;
// // using System.Net.Sockets;
// // using System.Text;
// // using UnityEngine;

// // public class VehicleDataBroadcaster : MonoBehaviour
// // {
// //     public static VehicleDataBroadcaster Instance;
// //     private UdpClient udpClient;
// //     private IPEndPoint remoteEndPoint;

//     // void Awake()
//     // {
//     //     if (Instance == null) 
//     //     {
//     //         Instance = this;
//     //     }
//     //     else if (Instance != this)
//     //     {
//     //         Destroy(this);  // Destroy only the script instance, not the entire GameObject
//     //     }
//     // }

// //     void Start()
// //     {
// //         udpClient = new UdpClient();
// //         remoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5006);
// //     }

// //     public void SendMessageToServer(string message)
// //     {
// //         try
// //         {
// //             byte[] data = Encoding.UTF8.GetBytes(message);
// //             udpClient.Send(data, data.Length, remoteEndPoint);
// //             Debug.Log($"Sent UDP message: {message}");
// //         }
// //         catch (Exception e)
// //         {
// //             Debug.LogError($"Error sending UDP message: {e.Message}");
// //         }
// //     }

// //     void OnApplicationQuit()
// //     {
// //         if (udpClient != null)
// //         {
// //             udpClient.Close();
// //         }
// //     }
// // }
