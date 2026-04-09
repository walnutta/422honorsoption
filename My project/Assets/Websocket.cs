using System.Collections;
using UnityEngine;
using NativeWebSocket;

public class WebSocketClient : MonoBehaviour
{
    WebSocket ws;
    private string serverIP = "127.0.0.1";
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
        await ws.Connect();
        await System.Threading.Tasks.Task.Delay(500); // wait for Unity to stabilize
        Debug.Log("Connect returned, state: " + ws.State);

        if (ws.State == WebSocketState.Open)
        {
            byte[] testImage = System.IO.File.ReadAllBytes(@"C:\Users\abc\Documents\422honorsoption\test.jpg");
            Debug.Log("Sending image, size: " + testImage.Length + " bytes");
            await ws.Send(testImage);
            Debug.Log("Image sent!");
        }
        else
        {
            Debug.LogError("WebSocket not open, state: " + ws.State);
        }
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (ws != null)
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
        if (ws != null)
            await ws.Close();
    }
}