// using System;
// using System.Net;
// using System.Net.Sockets;
// using System.Text;
// using System.Threading;
// using UnityEngine;
// using System.Collections.Generic;

// public class CommunicationManager : MonoBehaviour
// {
//     public static CommunicationManager Instance { get; private set; }
//     private UdpClient udpClient;
//     private UdpClient listener;
//     private IPEndPoint remoteEndPoint;
//     private Thread sendThread;
//     private Thread receiveThread;
//     private Queue<string> messageQueue = new Queue<string>();
//     private Queue<string> receivedMessages = new Queue<string>();  // Queue for received messages
//     private object lockObject = new object();
//     private object receiveLock = new object();
//     private int localPort = 5007;  // Port to listen for incoming messages

//     void Awake()
//     {
//         if (Instance == null)
//         {
//             Instance = this;
//             DontDestroyOnLoad(gameObject);
//         }
//         else
//         {
//             Destroy(gameObject);
//             return;
//         }
//     }

//     void Start()
//     {
//         // Start sending thread
//         sendThread = new Thread(NetworkLoop);
//         sendThread.IsBackground = true;
//         sendThread.Start();

//         // Start receiving thread
//         receiveThread = new Thread(ReceiveLoop);
//         receiveThread.IsBackground = true;
//         receiveThread.Start();
//     }

//     private void NetworkLoop()
//     {
//         udpClient = new UdpClient();
//         remoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5006);

//         while (true)
//         {
//             lock (lockObject)
//             {
//                 while (messageQueue.Count > 0)
//                 {
//                     string message = messageQueue.Dequeue();
//                     SendMessageOverUDP(message);
//                 }
//             }

//             Thread.Sleep(10);  // Prevent 100% CPU usage
//         }
//     }

//     private void ReceiveLoop()
//     {
//         listener = new UdpClient(localPort);
//         IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, localPort);

//         while (true)
//         {
//             try
//             {
//                 byte[] data = listener.Receive(ref endPoint);
//                 string receivedMessage = Encoding.UTF8.GetString(data);

//                 lock (receiveLock)
//                 {
//                     receivedMessages.Enqueue(receivedMessage);
//                 }

//                 Debug.Log($"[CommunicationManager] Received UDP message: {receivedMessage}");
//             }
//             catch (Exception e)
//             {
//                 Debug.LogError($"[CommunicationManager] Error receiving UDP message: {e.Message}");
//             }
//         }
//     }

//     public void SendMessageToServer(string message)
//     {
//         lock (lockObject)
//         {
//             messageQueue.Enqueue(message + "\n");
//         }
//     }

//     private void SendMessageOverUDP(string message)
//     {
//         try
//         {
//             byte[] data = Encoding.UTF8.GetBytes(message);
//             udpClient.Send(data, data.Length, remoteEndPoint);
//             Debug.Log($"[CommunicationManager] Sent UDP message: {message}");
//         }
//         catch (Exception e)
//         {
//             Debug.LogError($"[CommunicationManager] Error sending UDP message: {e.Message}");
//         }
//     }

//     void Update()
//     {
//         lock (receiveLock)
//         {
//             while (receivedMessages.Count > 0)
//             {
//                 string message = receivedMessages.Dequeue();
//                 ProcessReceivedMessage(message);
//             }
//         }
//     }

//     private void ProcessReceivedMessage(string message)
//     {
//         Debug.Log($"[CommunicationManager] Processing received message: {message}");

//         // Define possible stimuli and scenarios
//         string[] stimuli = { "surprise", "confusion", "frustration" };
//         string[] scenarios = { "alert", "driving" };

//         string selectedStimulus = null;
//         string selectedScenario = null;

//         // Parse message for stimulus and scenario
//         foreach (string stim in stimuli)
//         {
//             if (message.ToLower().Contains(stim))
//             {
//                 selectedStimulus = stim;
//                 break;
//             }
//         }

//         foreach (string scenario in scenarios)
//         {
//             if (message.ToLower().Contains(scenario))
//             {
//                 selectedScenario = scenario;
//                 break;
//             }
//         }

//         if (selectedStimulus != null && selectedScenario != null)
//         {
//             GameObject[] allMarkers = GameObject.FindGameObjectsWithTag("InteractionMarkers");

//             foreach (GameObject marker in allMarkers)
//             {
//                 string markerName = marker.name.ToLower();
//                 Debug.Log($"Marker name: {markerName}");

//                 // Check if the marker's name contains both the selected stimulus and scenario
//                 if (markerName.Contains(selectedStimulus) && markerName.Contains(selectedScenario))
//                 {
//                     marker.SetActive(true);
//                     Debug.Log($"Activated marker: {marker.name}");
//                 }
//                 else
//                 {
//                     marker.SetActive(false);
//                     Debug.Log($"Deactivated marker: {marker.name}");
//                 }
//             }
//         }
//         else
//         {
//             Debug.Log("No valid stimulus or scenario found in the message.");
//         }
//     }


//     void OnApplicationQuit()
//     {
//         if (udpClient != null)
//         {
//             udpClient.Close();
//         }

//         if (listener != null)
//         {
//             listener.Close();
//         }

//         if (sendThread != null && sendThread.IsAlive)
//         {
//             sendThread.Abort();
//         }

//         if (receiveThread != null && receiveThread.IsAlive)
//         {
//             receiveThread.Abort();
//         }
//     }
// }


//////////////////////


using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class CommunicationManager : MonoBehaviour
{
    public static CommunicationManager Instance { get; private set; }

    private UdpClient udpClient;
    private UdpClient listener;
    private IPEndPoint remoteEndPoint;

    private ConcurrentQueue<string> receivedMessages = new ConcurrentQueue<string>();
    private ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();

    private int localPort = 5007;
    private Dictionary<string, GameObject> markerDictionary = new Dictionary<string, GameObject>();

    private Thread receiveThread;
    private Thread sendThread;

    private bool isRunning = true;
    private ManualResetEvent messageEvent = new ManualResetEvent(false);
    private ManualResetEvent receiveEvent = new ManualResetEvent(false);

    private ScenarioManagerStartle scenarioManager;

    private NavigationScreenSS navigationScreen;

    private MarkerActivator markerActivator;

    private bool resetButtonPressed = false;
    private float buttonPressTime = 0f;

    private float trialAlertStartTime = 0f;

    private bool alertTrial = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        scenarioManager = FindObjectOfType<ScenarioManagerStartle>();

        navigationScreen = FindObjectOfType<NavigationScreenSS>();

        markerActivator = FindObjectOfType<MarkerActivator>();

        // Initialize UDP client for sending
        udpClient = new UdpClient();
        remoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5006);

        // Start receiving thread
        receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
        receiveThread.Start();

        // Start sending thread
        sendThread = new Thread(SendLoop) { IsBackground = true };
        sendThread.Start();
    }

    // Threaded message receiving
    private void ReceiveLoop()
    {
        listener = new UdpClient(localPort);
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, localPort);

        while (isRunning)
        {
            try
            {
                if (listener.Available > 0)
                {
                    byte[] data = listener.Receive(ref endPoint);
                    string receivedMessage = Encoding.UTF8.GetString(data);
                    receivedMessages.Enqueue(receivedMessage);
                    receiveEvent.Set();
                    Debug.Log($"[CommunicationManager] Received: {receivedMessage}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[CommunicationManager] Receive error: {e.Message}");
            }
            Thread.Sleep(5);
        }
    }

    // Threaded message sending
    private void SendLoop()
    {
        while (isRunning)
        {
            messageEvent.WaitOne(); // Wait for a signal to process messages

            while (messageQueue.TryDequeue(out string message))
            {
                SendMessageOverUDP(message);
            }

            messageEvent.Reset(); // Reset the event after processing messages
            Thread.Sleep(5);
        }
    }

    // Public method to send messages
    public void SendMessageToServer(string message)
    {
        messageQueue.Enqueue(message + "\n");
        messageEvent.Set(); // Signal the send thread
    }

    // Unity's Update method only processes messages when available
    void Update()
    {
        if (receiveEvent.WaitOne(0)) // Check if we have received messages
        {
            while (receivedMessages.TryDequeue(out string message))
            {
                ProcessReceivedMessage(message);
            }
            receiveEvent.Reset(); // Reset the event after processing
        }

        // Detect button press and store the timestamp
        if (Input.GetKeyDown(KeyCode.JoystickButton8))
        // if (Input.GetKeyDown(KeyCode.R))  // For Debugging
        {
            resetButtonPressed = true;
            buttonPressTime = Time.time; // Store time when button is pressed
            Debug.Log("Confirm Alert button pressed...");
        }


        // Clear Navigation Screen Alert Icon for all alert scenarios except frustration
        if (scenarioManager.currentScenario == "alert" && resetButtonPressed)
        { 
            markerActivator = FindObjectOfType<MarkerActivator>();
            Debug.Log("Progress in lap for alert: " + markerActivator.markersPassed);
            // if (scenarioManager.currentStimulus != "frustration" || markerActivator.markersPassed!=3)
            // {
                
            if (navigationScreen.previousIconType.ToString().ToLower().Contains("clear"))
            {
                Debug.Log("Navigation screen set to blank due to steering wheel button press");
                SendMessageToServer("confirm_alert_button_pressed");
                navigationScreen.ClearAlertButtonPress();
            }
            // }
        }


        // Reset buttonPress if more than 2 second has passed
        if (resetButtonPressed && Time.time - buttonPressTime > 2f)
        {
            resetButtonPressed = false;
            Debug.Log("Clear Alert Button press reset due to timeout.");
        }


        //

        if (alertTrial && Time.time - trialAlertStartTime > 5f)
        {
            Debug.Log("Alert trial ended.");
            alertTrial = false;
            navigationScreen.SetIconByString("TrialDropoffClear");
        }

    }

    // Process received messages
    private void ProcessReceivedMessage(string message)
    {
        string[] stimuli = { "surprise", "confusion", "frustration", "trial" };
        string[] scenarios = { "alert", "driving", "music", "auxiliary" };

        foreach (var stim in stimuli)
            if (message.ToLower().Contains(stim))
                scenarioManager.currentStimulus = stim;

        foreach (var scenario in scenarios)
            if (message.ToLower().Contains(scenario))
                scenarioManager.currentScenario = scenario;
                scenarioManager.newScenario = true;
                scenarioManager.newScenarioTime = Time.time;
        
        // Listen for 'Start Unity Simulation' message from the server
        // if (message.ToLower().Contains("start") && message.ToLower().Contains("unity"))
        if (message.ToLower().Contains("initialization") && message.ToLower().Contains("done"))
        {
            Debug.Log("Received start command from server. Activating scenario.");
            scenarioManager.isScenarioReady = true;
        }

        // Listen for Pause simulation message from the server
        if (message.ToLower().Contains("stop") && message.ToLower().Contains("vehicle"))
        {
            Debug.Log("Received pause command from server. Pausing scenario.");
            scenarioManager.isScenarioReady = false;
        }

        Debug.Log($"Scenario updated: {scenarioManager.currentStimulus}, {scenarioManager.currentScenario}");

        if (message.ToLower().Contains("listening"))
        {
            navigationScreen.SetIconByString("Listening");
        }

        if (message.ToLower().Contains("standby"))
        {
            navigationScreen.SetIconByString("Standby");
        }

        // For trial alert
        if (!alertTrial && message.ToLower().Contains("start_alert_trial"))
        {
            navigationScreen.SetIconByString("TrialDropoff");
            trialAlertStartTime = Time.time;
            alertTrial = true;
        }

        // For reset cars
        if (message.ToLower().Contains("reset_sim_now"))
        {
            Debug.Log("Received reset cars command from server. Resetting cars.");
            markerActivator.ResetSimulation();
        }
        
    }


    // Send a message via UDP
    private void SendMessageOverUDP(string message)
    {
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            udpClient.Send(data, data.Length, remoteEndPoint);
            Debug.Log($"[CommunicationManager] Sent: {message}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[CommunicationManager] Send error: {e.Message}");
        }
    }

    // Clean up resources on application quit
    void OnApplicationQuit()
    {
        isRunning = false;
        messageEvent.Set();  // Unblock threads to allow exit
        receiveEvent.Set();

        udpClient?.Close();
        listener?.Close();

        if (receiveThread?.IsAlive == true)
            receiveThread.Join();

        if (sendThread?.IsAlive == true)
            sendThread.Join();
    }
}
