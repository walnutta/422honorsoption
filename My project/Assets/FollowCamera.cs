using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public Transform cameraTransform;
    public float distance = 2f;

    void Update()
    {
        if (cameraTransform == null) return;
        transform.position = cameraTransform.position + cameraTransform.forward * distance;
        transform.rotation = Quaternion.LookRotation(transform.position - cameraTransform.position);
    }
}