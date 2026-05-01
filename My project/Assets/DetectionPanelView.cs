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
    public float fallbackDistance = 1.5f;

    private List<GameObject> activeUIElements = new List<GameObject>();

    public void UpdateData(DetectionData data)
    {
        if (statusText != null)
            statusText.text = $"Objects: {data.count} | Time: {data.inference_ms:0}ms";

        foreach (var element in activeUIElements) Destroy(element);
        activeUIElements.Clear();

        if (data.detections == null) return;

        foreach (var item in data.detections)
        {
            if (item.bbox_norm == null || item.bbox_norm.Length < 4) continue;

            float cx = item.bbox_norm[0];
            float cy = 1f - item.bbox_norm[1];
            float w = item.bbox_norm[2];
            float h = item.bbox_norm[3];

            Vector3 viewportPoint = new Vector3(cx, cy, 0);
            Ray ray = centerEyeCamera.ViewportPointToRay(viewportPoint);

            float distance = fallbackDistance;
            if (Physics.Raycast(ray, out RaycastHit hit, 10f))
                distance = hit.distance;

            Vector3 worldPos = ray.GetPoint(distance);

            GameObject box = Instantiate(
                boundingBox3DPrefab,
                worldPos,
                Quaternion.LookRotation(ray.direction)
            );

            float boxWidth = w * distance * 1.5f;
            float boxHeight = h * distance * 1.5f;
            box.transform.localScale = new Vector3(boxWidth, boxHeight, 0.01f);

            TextMeshPro label = box.GetComponentInChildren<TextMeshPro>();
            if (label != null)
                label.text = $"{item.label} {(item.confidence * 100):0}%";

            activeUIElements.Add(box);
        }
    }
}