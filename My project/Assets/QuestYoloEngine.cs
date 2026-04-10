using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Android;

public class QuestYoloEngine : MonoBehaviour
{
    [Header("Network (CHANGE THIS)")]
    public string serverUrl = "ws://192.168.1.50:8765"; // PUT YOUR PC'S LOCAL WI-FI IP HERE
    public float sendInterval = 0.5f;

    [Header("UI References")]
    public DetectionPanelView panelView;

    private WebCamTexture questCamera;
    private Texture2D exportTexture;
    private ClientWebSocket ws;
    private CancellationTokenSource cts;
    private float timer = 0f;
    private Queue<string> messageQueue = new Queue<string>();

    void Start()
    {
        // Ask headset for camera access
        if (!Permission.HasUserAuthorizedPermission("horizonos.permission.HEADSET_CAMERA"))
            Permission.RequestUserPermission("horizonos.permission.HEADSET_CAMERA");

        StartCoroutine(InitCamera());
        ConnectWebSocket();
    }

    IEnumerator InitCamera()
    {
        yield return new WaitForSeconds(1f);
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length > 0)
        {
            questCamera = new WebCamTexture(devices[0].name, 640, 480, 10);
            questCamera.Play();
        }
    }

    async void ConnectWebSocket()
    {
        ws = new ClientWebSocket();
        cts = new CancellationTokenSource();
        try
        {
            await ws.ConnectAsync(new Uri(serverUrl), cts.Token);
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
        lock (messageQueue)
        {
            while (messageQueue.Count > 0)
            {
                DetectionData data = JsonUtility.FromJson<DetectionData>(messageQueue.Dequeue());
                if (panelView != null && data != null) panelView.UpdateData(data);
            }
        }

        if (ws != null && ws.State == WebSocketState.Open && questCamera != null && questCamera.isPlaying)
        {
            timer += Time.deltaTime;
            if (timer >= sendInterval && questCamera.width > 100)
            {
                timer = 0f;
                SendFrame();
            }
        }
    }

    void SendFrame()
    {
        if (exportTexture == null || exportTexture.width != questCamera.width)
            exportTexture = new Texture2D(questCamera.width, questCamera.height, TextureFormat.RGB24, false);

        exportTexture.SetPixels(questCamera.GetPixels());
        exportTexture.Apply();

        var segment = new ArraySegment<byte>(exportTexture.EncodeToJPG(40));
        _ = ws.SendAsync(segment, WebSocketMessageType.Binary, true, cts.Token);
    }

    void OnDestroy()
    {
        if (ws != null) ws.Abort();
        if (cts != null) cts.Cancel();
        if (questCamera != null) questCamera.Stop();
    }
}