using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Image), typeof(RectTransform))]
public class FullCanvasImageFitter : MonoBehaviour
{
    [Header("Fit")]
    [Tooltip("Keep the source sprite aspect ratio and enlarge it until it covers the entire Canvas.")]
    [SerializeField] private bool preserveAspectRatio = true;

    [Header("Expansion")]
    [Tooltip("Extra width in the Image RectTransform's local units.")]
    [SerializeField] private float expansionX;

    [Tooltip("Extra height in the Image RectTransform's local units.")]
    [SerializeField] private float expansionY;

    private Image image;
    private RectTransform imageRect;
    private RectTransform canvasRect;
    private Canvas canvas;

    private const float Epsilon = 0.0001f;

    private void Awake() => RefreshSize();
    private void OnEnable() => RefreshSize();
    private void OnTransformParentChanged() => RefreshSize();
    private void LateUpdate() => UpdateSize();

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!IsFinite(expansionX)) expansionX = 0f;
        if (!IsFinite(expansionY)) expansionY = 0f;
        RefreshSize();
    }
#endif

    public void RefreshSize()
    {
        image = GetComponent<Image>();
        imageRect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
        UpdateSize();
    }

    private void UpdateSize()
    {
        if (image == null || imageRect == null || canvasRect == null)
        {
            RefreshReferences();
            if (image == null || imageRect == null || canvasRect == null) return;
        }

        // The Canvas and Image must be aligned (no relative rotation or shear).
        Vector2 canvasSize = canvasRect.rect.size;
        if (!ValidPositive(canvasSize.x) || !ValidPositive(canvasSize.y)) return;

        Vector3 localHorizontal = imageRect.InverseTransformVector(
            canvasRect.TransformVector(Vector3.right * canvasSize.x));
        Vector3 localVertical = imageRect.InverseTransformVector(
            canvasRect.TransformVector(Vector3.up * canvasSize.y));

        float width = localHorizontal.magnitude + expansionX;
        float height = localVertical.magnitude + expansionY;
        if (!ValidPositive(width) || !ValidPositive(height)) return;

        if (preserveAspectRatio && image.sprite != null)
        {
            Rect spriteRect = image.sprite.rect;
            if (ValidPositive(spriteRect.width) && ValidPositive(spriteRect.height))
            {
                float spriteAspect = spriteRect.width / spriteRect.height;
                float scale = Mathf.Max(width / spriteAspect, height);
                width = scale * spriteAspect;
                height = scale;
            }
        }

        if (!ValidPositive(width) || !ValidPositive(height)) return;

        // Image.preserveAspect uses a contain-like behavior; cover is calculated here.
        if (image.preserveAspect) image.preserveAspect = false;

        // Center the image on the Canvas, even if it is nested under other UI objects.
        Vector3 canvasCenterWorld = canvasRect.TransformPoint(canvasRect.rect.center);
        Vector3 imageCenterLocal = imageRect.InverseTransformPoint(canvasCenterWorld);
        Vector3 pivotOffset = new Vector3(
            (0.5f - imageRect.pivot.x) * width,
            (0.5f - imageRect.pivot.y) * height, 0f);
        Vector3 correction = imageCenterLocal - pivotOffset;
        if (correction.sqrMagnitude > Epsilon * Epsilon)
            imageRect.position += imageRect.TransformVector(correction);

        Vector2 current = imageRect.rect.size;
        if (Mathf.Abs(current.x - width) > Epsilon)
            imageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        if (Mathf.Abs(current.y - height) > Epsilon)
            imageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
    }

    private void RefreshReferences()
    {
        image = GetComponent<Image>();
        imageRect = GetComponent<RectTransform>();
        Canvas currentCanvas = GetComponentInParent<Canvas>();
        if (canvas != currentCanvas || canvasRect == null)
        {
            canvas = currentCanvas;
            canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
        }
    }

    private static bool IsFinite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
    private static bool ValidPositive(float v) => IsFinite(v) && v > Epsilon;
}
