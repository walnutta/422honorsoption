using UnityEngine;
using TMPro;

public class DetectionRowView : MonoBehaviour
{
    public TextMeshProUGUI labelText;
    public TextMeshProUGUI confidenceText;
    public TextMeshProUGUI bboxText;

    public void Setup(DetectionItem item)
    {
        labelText.text = item.label;
        confidenceText.text = $"{(item.confidence * 100):0}%";
        if (item.bbox != null && item.bbox.Length == 4)
            bboxText.text = $"[{item.bbox[0]:0}, {item.bbox[1]:0}, {item.bbox[2]:0}, {item.bbox[3]:0}]";
    }
}