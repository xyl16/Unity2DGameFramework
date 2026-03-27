using System;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class SocketClient : MonoBehaviour
{
    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;
    private bool isConnected;
    private bool isDisconnecting;
    private bool isApplicationQuitting = false;

    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<string> OnError;
    public event Action<byte[]> OnDataReceived;

    public bool IsConnected { get { return isConnected; } }

    private string currentIP;
    private int currentPort;

    public void Connect(string ip, int port)
    {
        if (isConnected)
        {
            Disconnect();
        }

        currentIP = ip;
        currentPort = port;
        isDisconnecting = false;

        try
        {
            client = new TcpClient();
            client.ReceiveTimeout = 30000;
            client.SendTimeout = 30000;
            client.Connect(ip, port);

            stream = client.GetStream();
            isConnected = true;

            receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();

            OnConnected?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"Connection failed: {e.Message}");
            isConnected = false;
            OnError?.Invoke(e.Message);
            OnDisconnected?.Invoke();
        }
    }

    public void Disconnect()
    {
        isDisconnecting = true;
        isConnected = false;

        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Join(1000);
            if (receiveThread.IsAlive)
            {
                receiveThread.Interrupt();
            }
        }

        if (stream != null)
        {
            try
            {
                stream.Close();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error closing stream: {e.Message}");
            }
        }

        if (client != null)
        {
            try
            {
                client.Close();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error closing client: {e.Message}");
            }
        }

        // 只在非应用退出时触发断开连接事件
        if (!isApplicationQuitting)
        {
            OnDisconnected?.Invoke();
        }
    }

    public void Send(byte[] data)
    {
        if (isConnected && stream != null && !isDisconnecting)
        {
            try
            {
                stream.Write(data, 0, data.Length);
                stream.Flush();
            }
            catch (Exception e)
            {
                Debug.LogError($"Send failed: {e.Message}");
                Disconnect();
            }
        }
        else
        {
            Debug.LogWarning("Cannot send: not connected");
        }
    }

    private void ReceiveData()
    {
        byte[] buffer = new byte[8192];

        while (isConnected && !isDisconnecting)
        {
            try
            {
                if (stream != null && stream.DataAvailable)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        byte[] data = new byte[bytesRead];
                        Array.Copy(buffer, data, bytesRead);
                        OnDataReceived?.Invoke(data);
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
            catch (ThreadInterruptedException)
            {
                break;
            }
            catch (Exception e)
            {
                if (!isDisconnecting)
                {
                    Debug.LogError($"Receive failed: {e.Message}");
                }
                break;
            }
        }

        if (!isDisconnecting)
        {
            Disconnect();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && isConnected)
        {
            Disconnect();
        }
    }

    private void OnApplicationQuit()
    {
        isApplicationQuitting = true;
        Disconnect();
    }

    private void OnDestroy()
    {
        isApplicationQuitting = true;
        Disconnect();
    }
}