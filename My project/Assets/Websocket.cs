using System.Collections;
using UnityEngine;
using NativeWebSocket;

public class WebSocketClient : MonoBehaviour
{
    WebSocket ws;
    private string serverIP = "127.0.0.1"; // change to your server's IP
    public int port = 8765;

    async void Start()
    {
        string url = $"ws://{serverIP}:{port}";
        Debug.Log("Attempting to connect to: " + url);

        ws = new WebSocket(url);
        ws.OnOpen += () => Debug.Log("Connected to server!");
        ws.OnError += (e) => Debug.LogError("WS Error: " + e);
        ws.OnClose += (e) => Debug.Log("Disconnected: " + e);
        ws.OnMessage += (bytes) => {
            string json = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log("Response: " + json);
        };

        Debug.Log("Calling Connect...");
        await ws.Connect();
        Debug.Log("Connect returned, state: " + ws.State);
    }
    void Update()
    {
        // Required on non-WebGL platforms to dispatch messages
#if !UNITY_WEBGL || UNITY_EDITOR
        ws.DispatchMessageQueue();
#endif
    }

    public async void SendImage(byte[] jpegBytes)
    {
        if (ws.State == WebSocketState.Open)
            await ws.Send(jpegBytes);
    }

    async void OnApplicationQuit()
    {
        await ws.Close();
    }
}