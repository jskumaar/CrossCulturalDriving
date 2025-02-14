using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using System.Collections.Generic;


public class CommunicationManager : MonoBehaviour
{
    public static CommunicationManager Instance { get; private set; }
    private UdpClient udpClient;
    private IPEndPoint remoteEndPoint;
    private Thread networkThread;
    private Queue<string> messageQueue = new Queue<string>();
    private object lockObject = new object();

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
        networkThread = new Thread(NetworkLoop);
        networkThread.IsBackground = true;  // Make it a background thread
        networkThread.Start();
    }

    private void NetworkLoop()
    {
        udpClient = new UdpClient();
        remoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5006);

        while (true)
        {
            lock (lockObject)
            {
                while (messageQueue.Count > 0)
                {
                    string message = messageQueue.Dequeue();
                    SendMessageOverUDP(message);
                }
            }

            Thread.Sleep(10);  // Prevent 100% CPU usage
        }
    }

    public void SendMessageToServer(string message)
    {
        lock (lockObject)
        {
            messageQueue.Enqueue(message + "\n");
        }
    }

    private void SendMessageOverUDP(string message)
    {
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            udpClient.Send(data, data.Length, remoteEndPoint);
            Debug.Log($"[CommunicationManager] Sent UDP message: {message}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[CommunicationManager] Error sending UDP message: {e.Message}");
        }
    }

    void OnApplicationQuit()
    {
        if (udpClient != null)
        {
            udpClient.Close();
        }

        if (networkThread != null && networkThread.IsAlive)
        {
            networkThread.Abort();
        }
    }
}
