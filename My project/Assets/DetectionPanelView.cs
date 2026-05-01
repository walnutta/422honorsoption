using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class DetectionPanelView : MonoBehaviour
{
    [Header("Text List UI")]
    public TextMeshProUGUI statusText;

    [Header("3D Placement")]
    public Camera centerEyeCamera;       // CenterEyeAnchor from OVRCameraRig
    public GameObject boundingBox3DPrefab; // 3D quad prefab with outline shader
    public float fallbackDistance = 1.5f;  // depth to place box if no raycast hit

    private List<GameObject> activeUIElements = new List<GameObject>();

    public void UpdateData(DetectionData data)
    {
        // Update status text with detection count and inference time
        if (statusText != null)
            statusText.text = $"Objects: {data.count} | Time: {data.inference_ms:0}ms";

        // Clear previous frame's bounding boxes
        foreach (var element in activeUIElements) Destroy(element);
        activeUIElements.Clear();

        if (data.detections == null) return;

        foreach (var item in data.detections)
        {
            if (item.bbox_norm == null || item.bbox_norm.Length < 4) continue;

            // bbox_norm = [cx, cy, width, height] in 0-1 normalized coords
            float cx = item.bbox_norm[0];
            float cy = 1f - item.bbox_norm[1]; // flip Y (Unity Y is up, image Y is down)
            float w = item.bbox_norm[2];
            float h = item.bbox_norm[3];

            // Correct for camera-eye parallax offset
            // Objects on the left appear shifted right and vice versa
            float centerOffset = cx - 0.5f;
            float correctedCx = cx - (centerOffset * 0.15f); // tweak 0.15f to adjust

            // Cast a ray from the camera through the viewport point
            Vector3 viewportPoint = new Vector3(correctedCx, cy, 0);
            Ray ray = centerEyeCamera.ViewportPointToRay(viewportPoint);

            // Use raycast depth if available, otherwise use fallback distance
            float distance = fallbackDistance;
            if (Physics.Raycast(ray, out RaycastHit hit, 10f))
                distance = hit.distance;

            // Place box at the point along the ray at the given distance
            Vector3 worldPos = ray.GetPoint(distance);

            // Spawn bounding box prefab facing the camera
            GameObject box = Instantiate(
                boundingBox3DPrefab,
                worldPos,
                Quaternion.LookRotation(ray.direction)
            );

            // Scale box proportionally to distance and detection size
            float boxWidth = w * distance * 1.5f;
            float boxHeight = h * distance * 1.5f;
            box.transform.localScale = new Vector3(boxWidth, boxHeight, 0.01f);

            // Set label text with object name and confidence percentage
            TextMeshPro label = box.GetComponentInChildren<TextMeshPro>();
            if (label != null)
                label.text = $"{item.label} {(item.confidence * 100):0}%";

            activeUIElements.Add(box);
        }
    }
}