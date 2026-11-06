using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFit : MonoBehaviour
{
    [SerializeField] private float minWidth = 8f;     // ความกว้างโลกที่ต้องมองเห็นอย่างน้อย
    [SerializeField] private float minHeight = 16f;   // ความสูงโลกที่ต้องมองเห็นอย่างน้อย

    private void Start()
    {
        Camera cam = GetComponent<Camera>();
        float sizeByWidth = minWidth / (2f * cam.aspect);
        float sizeByHeight = minHeight / 2f;
        cam.orthographicSize = Mathf.Max(sizeByWidth, sizeByHeight);
    }
}