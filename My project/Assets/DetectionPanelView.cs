using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class DetectionPanelView : MonoBehaviour
{
    [Header("Text List UI")]
    public TextMeshProUGUI statusText;

    [Header("3D Placement")]
    public Camera centerEyeCamera;
    public GameObject boundingBox3DPrefab;
    public float fallbackDistance = 2f;
    public float smoothSpeed = 8f;

    private Dictionary<string, GameObject> activeBoxes = new Dictionary<string, GameObject>();
    private Dictionary<string, float> boxLastSeen = new Dictionary<string, float>();
    private float boxTimeout = 1f; // remove box if not seen for 1 second

    void Update()
    {
        // Remove boxes that haven't been seen recently
        List<string> toRemove = new List<string>();
        foreach (var key in boxLastSeen.Keys)
        {
            if (Time.time - boxLastSeen[key] > boxTimeout)
                toRemove.Add(key);
        }
        foreach (var key in toRemove)
        {
            if (activeBoxes.ContainsKey(key))
                Destroy(activeBoxes[key]);
            activeBoxes.Remove(key);
            boxLastSeen.Remove(key);
        }
    }

    public void UpdateData(DetectionData data)
    {
        if (statusText != null)
            statusText.text = $"Objects: {data.count} | Time: {data.inference_ms:0}ms";

        if (data.detections == null) return;

        HashSet<string> seenKeys = new HashSet<string>();

        for (int i = 0; i < data.detections.Count; i++)
        {
            var item = data.detections[i];
            if (item.bbox_norm == null || item.bbox_norm.Length < 4) continue;

            // Use label + index as key to handle multiple same-label objects
            string key = $"{item.label}_{i}";
            seenKeys.Add(key);
            boxLastSeen[key] = Time.time;

            float cx = item.bbox_norm[0];
            float cy = 1f - item.bbox_norm[1];
            float w = item.bbox_norm[2];
            float h = item.bbox_norm[3];

            Vector3 viewportPoint = new Vector3(cx, cy, 0);
            Ray ray = centerEyeCamera.ViewportPointToRay(viewportPoint);

            float distance = fallbackDistance;
            if (Physics.Raycast(ray, out RaycastHit hit, 10f))
                distance = hit.distance;

            Vector3 targetPos = ray.GetPoint(distance);
            Quaternion targetRot = Quaternion.LookRotation(ray.direction);

            float boxWidth = w * distance;
            float boxHeight = h * distance;
            Vector3 targetScale = new Vector3(boxWidth, boxHeight, 0.01f);

            if (!activeBoxes.ContainsKey(key))
            {
                // Spawn new box
                GameObject box = Instantiate(boundingBox3DPrefab, targetPos, targetRot);
                box.transform.localScale = targetScale;
                activeBoxes[key] = box;

                TextMeshPro label = box.GetComponentInChildren<TextMeshPro>();
                if (label != null)
                    label.text = $"{item.label} {(item.confidence * 100):0}%";
            }
            else
            {
                // Smoothly move existing box
                GameObject box = activeBoxes[key];
                box.transform.position = Vector3.Lerp(box.transform.position, targetPos, Time.deltaTime * smoothSpeed);
                box.transform.rotation = Quaternion.Slerp(box.transform.rotation, targetRot, Time.deltaTime * smoothSpeed);
                box.transform.localScale = Vector3.Lerp(box.transform.localScale, targetScale, Time.deltaTime * smoothSpeed);

                TextMeshPro label = box.GetComponentInChildren<TextMeshPro>();
                if (label != null)
                    label.text = $"{item.label} {(item.confidence * 100):0}%";
            }
        }
    }
}