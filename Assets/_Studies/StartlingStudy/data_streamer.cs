using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class VehicleDataBroadcaster : MonoBehaviour
{
    public static VehicleDataBroadcaster Instance;  // Singleton instance

    private TcpClient client;
    private NetworkStream stream;
    private Thread sendThread;
    private bool isStreaming = true;

    private Queue<string> triggerMessages = new Queue<string>(); // Queue for trigger messages

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        sendThread = new Thread(ConnectionLoop);
        sendThread.Start();
    }

    void ConnectionLoop()
    {
        while (isStreaming)
        {
            if (client == null || !client.Connected)
            {
                Debug.LogWarning("Server not connected. Retrying...");
                ConnectToServer();
                Thread.Sleep(2000);
            }
            else
            {
                SendData();
            }
        }
    }

    void ConnectToServer()
    {
        try
        {
            client = new TcpClient("127.0.0.1", 5006);
            stream = client.GetStream();
            Debug.Log("Connected to TCP server.");
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to connect: " + e.Message);
        }
    }

    void SendData()
    {
        try
        {
            while (triggerMessages.Count > 0)
            {
                string data = triggerMessages.Dequeue();
                byte[] message = Encoding.UTF8.GetBytes(data + "\n");
                stream.Write(message, 0, message.Length);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error sending data: " + e.Message);
            DisconnectServer();
        }

        Thread.Sleep(100);
    }

    public void TriggerEntered(string triggerID)
    {
        string message = $"TRIGGER_ENTERED: {triggerID}";
        Debug.Log(message);
        triggerMessages.Enqueue(message);
    }

    public void TriggerExited(string triggerID)
    {
        string message = $"TRIGGER_EXITED: {triggerID}";
        Debug.Log(message);
        triggerMessages.Enqueue(message);
    }

    void DisconnectServer()
    {
        if (stream != null) stream.Close();
        if (client != null) client.Close();
        client = null;
    }

    void OnApplicationQuit()
    {
        isStreaming = false;
        DisconnectServer();
        if (sendThread != null) sendThread.Abort();
    }
}
