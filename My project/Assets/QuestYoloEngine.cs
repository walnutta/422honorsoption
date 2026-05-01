using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Android;
using TMPro;
using UnityEngine.UI;

public class QuestYoloEngine : MonoBehaviour
{
    [Header("Network")]
    public TMP_InputField ipInputField;
    public string serverPort = "8765";

    [Header("Adaptive Frame Rate")]
    public float minSendInterval = 0.063f; // max 60fps
    public float maxSendInterval = 0.1f;   // min 10fps

    [Header("UI References")]
    public DetectionPanelView panelView;

    private float currentSendInterval = 0.1f;
    private float lastReceiveTime = 0f;
    private string serverUrl;
    private WebCamTexture questCamera;
    private Texture2D exportTexture;
    private ClientWebSocket ws;
    private CancellationTokenSource cts;
    private float timer = 0f;
    private Queue<string> messageQueue = new Queue<string>();
    private Queue<float> receiveTimeQueue = new Queue<float>();

    void Start()
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += (perm) => RequestHeadsetCamera();
            Permission.RequestUserPermission(Permission.Camera, callbacks);
        }
        else
        {
            RequestHeadsetCamera();
        }

        serverUrl = $"ws://35.21.58.106:8765"; // Hardcoded
        ConnectWebSocket();
    }

    void RequestHeadsetCamera()
    {
        if (!Permission.HasUserAuthorizedPermission("horizonos.permission.HEADSET_CAMERA"))
        {
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += (perm) => StartCoroutine(InitCamera());
            callbacks.PermissionDenied += (perm) => Debug.LogError("Headset camera denied!");
            Permission.RequestUserPermission("horizonos.permission.HEADSET_CAMERA", callbacks);
        }
        else
        {
            StartCoroutine(InitCamera());
        }
    }

    public void OnConnectPressed()
    {
        string ip = ipInputField.text.Trim();
        serverUrl = $"ws://{ip}:{serverPort}";
        Debug.Log("Connecting to: " + serverUrl);
        ConnectWebSocket();
    }

    IEnumerator InitCamera()
    {
        yield return new WaitForSeconds(2f);
        WebCamDevice[] devices = WebCamTexture.devices;
        Debug.Log("=== CAMERA DEBUG === Device count: " + devices.Length);
        foreach (var d in devices)
            Debug.Log("Device: " + d.name);

        if (devices.Length > 0)
        {
            questCamera = new WebCamTexture(devices[2].name, 640, 480, 10);
            questCamera.Play();
            yield return new WaitForSeconds(1f);
            Debug.Log($"Camera playing: {questCamera.isPlaying}, size: {questCamera.width}x{questCamera.height}");
            lastReceiveTime = Time.time;
        }
        else
        {
            Debug.LogError("No camera devices found!");
        }
    }

    async void ConnectWebSocket()
    {
        ws = new ClientWebSocket();
        cts = new CancellationTokenSource();
        try
        {
            await ws.ConnectAsync(new Uri(serverUrl), cts.Token);
            Debug.Log("Connected!");
            _ = ReceiveLoop();
        }
        catch (Exception e)
        {
            Debug.LogError("Connect Error: " + e.Message + "\n" + e.StackTrace);
        }
    }

    async Task ReceiveLoop()
    {
        var buffer = new byte[8192];
        try
        {
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Debug.LogWarning("Server sent close frame");
                    break;
                }
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    lock (messageQueue) { messageQueue.Enqueue(json); }
                    lock (receiveTimeQueue) { receiveTimeQueue.Enqueue(Time.time); }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("ReceiveLoop Error: " + e.Message + "\n" + e.StackTrace);
        }
    }

    void AdaptFrameRate()
    {
        float latency = Time.time - lastReceiveTime;

        if (latency < 0.15f)
            currentSendInterval = Mathf.Max(currentSendInterval - 0.01f, minSendInterval);
        else if (latency > 0.3f)
            currentSendInterval = Mathf.Min(currentSendInterval + 0.05f, maxSendInterval);

      
    }

    void Update()
    {
        lock (receiveTimeQueue)
        {
            while (receiveTimeQueue.Count > 0)
                lastReceiveTime = receiveTimeQueue.Dequeue();
        }

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
            if (timer >= currentSendInterval && questCamera.width > 100)
            {
                AdaptFrameRate();
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
        var segment = new ArraySegment<byte>(exportTexture.EncodeToJPG(30));
        _ = ws.SendAsync(segment, WebSocketMessageType.Binary, true, cts.Token);
    }

    void OnDestroy()
    {
        if (ws != null) ws.Abort();
        if (cts != null) cts.Cancel();
        if (questCamera != null) questCamera.Stop();
    }
}