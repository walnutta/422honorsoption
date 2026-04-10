using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class StaticImageYoloTest : MonoBehaviour
{
    [Header("Network")]
    public string serverUrl = "ws://127.0.0.1:8765"; // 127.0.0.1 is safe here because we are testing on PC
    public float sendInterval = 2.0f; // Send image every 2 seconds

    [Header("Data")]
    public Texture2D testImage;
    public DetectionPanelView panelView;

    private ClientWebSocket ws;
    private CancellationTokenSource cts;
    private float timer = 0f;
    private Queue<string> messageQueue = new Queue<string>();

    void Start()
    {
        if (testImage == null)
        {
            Debug.LogError("Please assign a Test Image in the Inspector!");
            return;
        }
        ConnectWebSocket();
    }

    async void ConnectWebSocket()
    {
        ws = new ClientWebSocket();
        cts = new CancellationTokenSource();

        try
        {
            await ws.ConnectAsync(new Uri(serverUrl), cts.Token);
            Debug.Log("Connected to Python Server!");
            _ = ReceiveLoop();
        }
        catch (Exception e) { Debug.LogError("Network Error: " + e.Message); }
    }

    async Task ReceiveLoop()
    {
        var buffer = new byte[8192];
        while (ws.State == WebSocketState.Open)
        {
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
            if (result.MessageType == WebSocketMessageType.Text)
            {
                string json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                lock (messageQueue) { messageQueue.Enqueue(json); }
            }
        }
    }

    void Update()
    {
        // Draw UI on the main thread safely
        lock (messageQueue)
        {
            while (messageQueue.Count > 0)
            {
                string json = messageQueue.Dequeue();
                DetectionData data = JsonUtility.FromJson<DetectionData>(json);
                if (panelView != null && data != null) panelView.UpdateData(data);
            }
        }

        // Send image periodically
        if (ws != null && ws.State == WebSocketState.Open)
        {
            timer += Time.deltaTime;
            if (timer >= sendInterval)
            {
                timer = 0f;
                SendImage();
            }
        }
    }

    void SendImage()
    {
        byte[] jpgBytes = testImage.EncodeToJPG(50);
        var segment = new ArraySegment<byte>(jpgBytes);
        _ = ws.SendAsync(segment, WebSocketMessageType.Binary, true, cts.Token);
    }

    void OnDestroy()
    {
        if (ws != null) ws.Abort();
        if (cts != null) cts.Cancel();
    }
}