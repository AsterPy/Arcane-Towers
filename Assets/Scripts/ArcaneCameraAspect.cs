using UnityEngine;

[ExecuteAlways]
public class ArcaneCameraAspect : MonoBehaviour
{
    // Exact 1:1 match with 1080 x 1920 Canvas (100 PPU -> 10.8 x 19.2 units)
    public float targetWidth = 10.8f;
    public float targetHeight = 19.2f;
    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        Adjust();
    }

    private void Update()
    {
        Adjust();
    }

    public void Adjust()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (cam == null) return;

        float targetAspect = targetWidth / targetHeight; // 9:16 = 0.5625f
        float windowAspect = (float)Screen.width / Mathf.Max(1, Screen.height);

        if (windowAspect > targetAspect)
        {
            // Pillarbox: screen is wider than 9:16 (black bars on left and right)
            float inset = 1f - (targetAspect / windowAspect);
            cam.rect = new Rect(inset * 0.5f, 0f, 1f - inset, 1f);
        }
        else
        {
            // Letterbox: screen is taller/narrower than 9:16 (black bars top and bottom)
            float inset = 1f - (windowAspect / targetAspect);
            cam.rect = new Rect(0f, inset * 0.5f, 1f, 1f - inset);
        }

        cam.orthographicSize = targetHeight * 0.5f; // Always exactly 9.6f (19.2 units tall)
    }
}
