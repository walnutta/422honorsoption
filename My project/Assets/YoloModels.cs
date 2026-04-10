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
    public int count;
    public float inference_ms;
}