using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class DetectionPanelView : MonoBehaviour
{
    [Header("Text List UI")]
    public Transform contentParent;
    public GameObject textRowPrefab;
    public TextMeshProUGUI statusText;

    [Header("Visual Box UI")]
    public RectTransform imageContainer; // Drag your DisplayImage here
    public GameObject boundingBoxPrefab; // Drag your BoundingBox prefab here

    private List<GameObject> activeUIElements = new List<GameObject>();

    public void UpdateData(DetectionData data)
    {
        statusText.text = $"Objects: {data.count} | Time: {data.inference_ms:0}ms";

        foreach (var element in activeUIElements) Destroy(element);
        activeUIElements.Clear();

        if (data.detections != null)
        {
            // --- NEW MATH LOGIC ---
            // Get the raw pixel size of the image, and the current UI size of the container
            RawImage rawImg = imageContainer.GetComponent<RawImage>();
            float originalImageWidth = rawImg.texture.width;
            float originalImageHeight = rawImg.texture.height;
            float uiBoxWidth = imageContainer.rect.width;
            float uiBoxHeight = imageContainer.rect.height;

            // Calculate the difference in scale
            float scaleX = uiBoxWidth / originalImageWidth;
            float scaleY = uiBoxHeight / originalImageHeight;
            // ----------------------

            foreach (var item in data.detections)
            {
                // Spawn Text Row
                if (textRowPrefab != null && contentParent != null)
                {
                    GameObject newRow = Instantiate(textRowPrefab, contentParent);
                    newRow.GetComponent<DetectionRowView>().Setup(item);
                    activeUIElements.Add(newRow);
                }

                // Spawn Visual Bounding Box
                if (boundingBoxPrefab != null && imageContainer != null && item.bbox.Length == 4)
                {
                    GameObject newBox = Instantiate(boundingBoxPrefab, imageContainer);
                    activeUIElements.Add(newBox);

                    RectTransform rt = newBox.GetComponent<RectTransform>();

                    // Force anchors to Top-Left
                    rt.anchorMin = new Vector2(0, 1);
                    rt.anchorMax = new Vector2(0, 1);
                    rt.pivot = new Vector2(0, 1);

                    // Multiply the Python coordinates by our Scale math
                    float xMin = item.bbox[0] * scaleX;
                    float yMin = item.bbox[1] * scaleY;
                    float width = (item.bbox[2] - item.bbox[0]) * scaleX;
                    float height = (item.bbox[3] - item.bbox[1]) * scaleY;

                    rt.anchoredPosition = new Vector2(xMin, -yMin);
                    rt.sizeDelta = new Vector2(width, height);

                    TextMeshProUGUI label = newBox.GetComponentInChildren<TextMeshProUGUI>();
                    if (label != null) label.text = $"{item.label} {(item.confidence * 100):0}%";
                }
            }
        }
    }
}