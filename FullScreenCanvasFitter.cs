using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(RectTransform))]
public class FullScreenCanvasFitter : MonoBehaviour
{
    /*
     * ========================================
     * REFERENCES
     * ========================================
     */

    [Header("References")]

    [SerializeField]
    private Camera targetCamera;


    /*
     * ========================================
     * EXPANSION
     * ========================================
     */

    [Header("Expansion")]

    [Tooltip(
        "Kích thước cộng thêm vào tổng chiều ngang, " +
        "tính theo world unit.")]
    [SerializeField]
    private float expansionX;


    [Tooltip(
        "Kích thước cộng thêm vào tổng chiều dọc, " +
        "tính theo world unit.")]
    [SerializeField]
    private float expansionY;


    /*
     * ========================================
     * CACHE
     * ========================================
     */

    private Canvas canvas;

    private RectTransform rectTransform;

    private DrivenRectTransformTracker
        rectTransformTracker;


    private float lastAspect =
        float.NaN;

    private float lastOrthographicSize =
        float.NaN;

    private float lastExpansionX =
        float.NaN;

    private float lastExpansionY =
        float.NaN;

    private Vector3 lastLossyScale =
        new(
            float.NaN,
            float.NaN,
            float.NaN);


    private RenderMode lastRenderMode;

    private bool hasLastRenderMode;


    private const float Epsilon =
        0.0001f;


    /*
     * ========================================
     * UNITY
     * ========================================
     */

    private void Awake()
    {
        Initialize();

        RefreshState();
    }


    private void OnEnable()
    {
        Initialize();

        RefreshState();
    }


    private void OnDisable()
    {
        ClearRectTransformLock();

        ResetCache();
    }


    private void OnDestroy()
    {
        ClearRectTransformLock();
    }


    private void LateUpdate()
    {
        if (!EnsureComponents())
        {
            return;
        }


        /*
         * Nếu Render Mode thay đổi trong runtime
         * hoặc Inspector, cập nhật trạng thái
         * lock ngay.
         */
        if (!hasLastRenderMode ||
            canvas.renderMode != lastRenderMode)
        {
            RefreshState();

            return;
        }


        /*
         * Canvas không phải World Space
         * thì bỏ qua hoàn toàn.
         */
        if (!IsWorldSpaceCanvas)
        {
            return;
        }


        UpdateSizeIfNeeded();
    }


#if UNITY_EDITOR

    private void OnValidate()
    {
        if (!IsFinite(expansionX))
        {
            expansionX = 0f;
        }


        if (!IsFinite(expansionY))
        {
            expansionY = 0f;
        }


        Initialize();

        RefreshState();
    }

#endif


    /*
     * ========================================
     * STATE
     * ========================================
     */

    private bool IsWorldSpaceCanvas =>
        canvas != null &&
        canvas.renderMode == RenderMode.WorldSpace;


    private void RefreshState()
    {
        if (!EnsureComponents())
        {
            ClearRectTransformLock();

            ResetCache();

            return;
        }


        lastRenderMode =
            canvas.renderMode;

        hasLastRenderMode =
            true;


        /*
         * Canvas không phải World Space:
         *
         * - bỏ lock;
         * - không resize;
         * - không ảnh hưởng RectTransform.
         */
        if (!IsWorldSpaceCanvas)
        {
            ClearRectTransformLock();

            ResetCache();

            return;
        }


        ApplyRectTransformLock();

        FindCamera();

        ForceUpdateSize();
    }


    /*
     * ========================================
     * INITIALIZATION
     * ========================================
     */

    private void Initialize()
    {
        EnsureComponents();

        FindCamera();
    }


    private bool EnsureComponents()
    {
        if (canvas == null)
        {
            canvas =
                GetComponent<Canvas>();
        }


        if (rectTransform == null)
        {
            rectTransform =
                GetComponent<RectTransform>();
        }


        return
            canvas != null &&
            rectTransform != null;
    }


    /*
     * ========================================
     * CAMERA
     * ========================================
     */

    private void FindCamera()
    {
        if (!IsWorldSpaceCanvas)
        {
            return;
        }


        if (targetCamera != null)
        {
            return;
        }


        if (canvas.worldCamera != null)
        {
            targetCamera =
                canvas.worldCamera;

            return;
        }


        targetCamera =
            Camera.main;
    }


    private bool EnsureCamera()
    {
        if (targetCamera != null)
        {
            return true;
        }


        FindCamera();


        return
            targetCamera != null;
    }


    /*
     * ========================================
     * RECT TRANSFORM LOCK
     * ========================================
     */

    private void ApplyRectTransformLock()
    {
        rectTransformTracker.Clear();


        if (!IsWorldSpaceCanvas ||
            rectTransform == null)
        {
            return;
        }


        /*
         * Chỉ Width và Height được script
         * quản lý.
         *
         * Position, Rotation và Scale
         * vẫn chỉnh bình thường.
         */
        rectTransformTracker.Add(
            this,
            rectTransform,
            DrivenTransformProperties.SizeDeltaX |
            DrivenTransformProperties.SizeDeltaY);
    }


    private void ClearRectTransformLock()
    {
        rectTransformTracker.Clear();
    }


    /*
     * ========================================
     * PUBLIC
     * ========================================
     */

    public void RefreshSize()
    {
        Initialize();

        RefreshState();
    }


    /*
     * ========================================
     * UPDATE SIZE
     * ========================================
     */

    private void ForceUpdateSize()
    {
        ResetCache();

        UpdateSizeIfNeeded();
    }


    private void UpdateSizeIfNeeded()
    {
        if (!IsWorldSpaceCanvas)
        {
            return;
        }


        if (!EnsureCamera())
        {
            return;
        }


        /*
         * Script dành cho game 2D
         * sử dụng Orthographic Camera.
         */
        if (!targetCamera.orthographic)
        {
            return;
        }


        float aspect =
            targetCamera.aspect;

        float orthographicSize =
            targetCamera.orthographicSize;


        if (!IsFinitePositive(aspect) ||
            !IsFinitePositive(orthographicSize))
        {
            return;
        }


        Vector3 lossyScale =
            rectTransform.lossyScale;


        if (!IsFiniteVector3(lossyScale))
        {
            return;
        }


        float scaleX =
            Mathf.Abs(lossyScale.x);

        float scaleY =
            Mathf.Abs(lossyScale.y);


        if (!IsFinitePositive(scaleX) ||
            !IsFinitePositive(scaleY))
        {
            return;
        }


        /*
         * ========================================
         * CALCULATE WORLD SIZE
         * ========================================
         */

        float worldHeight =
            orthographicSize * 2f;

        float worldWidth =
            worldHeight * aspect;


        float targetWorldWidth =
            worldWidth + expansionX;

        float targetWorldHeight =
            worldHeight + expansionY;


        if (!IsFinitePositive(
                targetWorldWidth) ||
            !IsFinitePositive(
                targetWorldHeight))
        {
            return;
        }


        /*
         * ========================================
         * CONVERT TO RECT SIZE
         * ========================================
         *
         * worldSize =
         * rectSize * lossyScale
         */

        float targetWidth =
            targetWorldWidth /
            scaleX;

        float targetHeight =
            targetWorldHeight /
            scaleY;


        if (!IsFinitePositive(targetWidth) ||
            !IsFinitePositive(targetHeight))
        {
            return;
        }


        Vector2 currentSize =
            rectTransform.rect.size;


        if (!IsFiniteVector2(currentSize))
        {
            return;
        }


        /*
         * ========================================
         * CACHE CHECK
         * ========================================
         */

        bool configurationUnchanged =
            Approximately(
                aspect,
                lastAspect) &&
            Approximately(
                orthographicSize,
                lastOrthographicSize) &&
            Approximately(
                expansionX,
                lastExpansionX) &&
            Approximately(
                expansionY,
                lastExpansionY) &&
            ApproximatelyVector3(
                lossyScale,
                lastLossyScale);


        bool sizeCorrect =
            Approximately(
                currentSize.x,
                targetWidth) &&
            Approximately(
                currentSize.y,
                targetHeight);


        if (configurationUnchanged &&
            sizeCorrect)
        {
            return;
        }


        /*
         * ========================================
         * APPLY
         * ========================================
         */

        if (!Approximately(
                currentSize.x,
                targetWidth))
        {
            rectTransform
                .SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Horizontal,
                    targetWidth);
        }


        currentSize =
            rectTransform.rect.size;


        if (!IsFiniteVector2(currentSize))
        {
            return;
        }


        if (!Approximately(
                currentSize.y,
                targetHeight))
        {
            rectTransform
                .SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical,
                    targetHeight);
        }


        /*
         * ========================================
         * CACHE
         * ========================================
         */

        lastAspect =
            aspect;

        lastOrthographicSize =
            orthographicSize;

        lastExpansionX =
            expansionX;

        lastExpansionY =
            expansionY;

        lastLossyScale =
            lossyScale;
    }


    /*
     * ========================================
     * VALIDATION
     * ========================================
     */

    private static bool IsFinite(
        float value)
    {
        return
            !float.IsNaN(value) &&
            !float.IsInfinity(value);
    }


    private static bool IsFinitePositive(
        float value)
    {
        return
            IsFinite(value) &&
            value > Epsilon;
    }


    private static bool IsFiniteVector2(
        Vector2 value)
    {
        return
            IsFinite(value.x) &&
            IsFinite(value.y);
    }


    private static bool IsFiniteVector3(
        Vector3 value)
    {
        return
            IsFinite(value.x) &&
            IsFinite(value.y) &&
            IsFinite(value.z);
    }


    private static bool Approximately(
        float a,
        float b)
    {
        if (!IsFinite(a) ||
            !IsFinite(b))
        {
            return false;
        }


        return
            Mathf.Abs(a - b) <=
            Epsilon;
    }


    private static bool ApproximatelyVector3(
        Vector3 a,
        Vector3 b)
    {
        return
            Approximately(a.x, b.x) &&
            Approximately(a.y, b.y) &&
            Approximately(a.z, b.z);
    }


    /*
     * ========================================
     * CACHE
     * ========================================
     */

    private void ResetCache()
    {
        lastAspect =
            float.NaN;

        lastOrthographicSize =
            float.NaN;

        lastExpansionX =
            float.NaN;

        lastExpansionY =
            float.NaN;

        lastLossyScale =
            new(
                float.NaN,
                float.NaN,
                float.NaN);
    }
}