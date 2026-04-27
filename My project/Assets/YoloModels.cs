using System;
using System.Collections.Generic;

[Serializable]
public class DetectionItem
{
    public string label;
    public float confidence;
    public float[] bbox;
}

[Serializable]
public class DetectionData
{
    public List<DetectionItem> detections;
    public float inference_ms;
    public int[] image_size;
    public int count => detections != null ? detections.Count : 0;
}