using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class FullScreenSpriteRendererFitter : MonoBehaviour
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
        "Kích thước cộng thêm vào tổng chiều ngang.")]
    [SerializeField]
    private float expansionX;


    [Tooltip(
        "Kích thước cộng thêm vào tổng chiều dọc.")]
    [SerializeField]
    private float expansionY;


    /*
     * ========================================
     * CACHE
     * ========================================
     */

    private SpriteRenderer spriteRenderer;


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

        ForceUpdateSize();
    }


    private void OnEnable()
    {
        Initialize();

        ForceUpdateSize();
    }


    private void LateUpdate()
    {
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

        ForceUpdateSize();
    }

#endif


    /*
     * ========================================
     * INITIALIZATION
     * ========================================
     */

    private void Initialize()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer =
                GetComponent<SpriteRenderer>();
        }


        /*
         * Không có Sprite:
         *
         * bỏ qua hoàn toàn,
         * không báo lỗi.
         */
        if (spriteRenderer == null ||
            spriteRenderer.sprite == null)
        {
            return;
        }


        /*
         * SpriteRenderer.size chỉ được sử dụng
         * bởi Sliced hoặc Tiled.
         */
        if (spriteRenderer.drawMode ==
            SpriteDrawMode.Simple)
        {
            spriteRenderer.drawMode =
                SpriteDrawMode.Sliced;
        }


        FindCamera();
    }


    /*
     * ========================================
     * CAMERA
     * ========================================
     */

    private void FindCamera()
    {
        if (targetCamera != null)
        {
            return;
        }


        targetCamera =
            Camera.main;
    }


    /*
     * ========================================
     * PUBLIC
     * ========================================
     */

    public void RefreshSize()
    {
        Initialize();

        ForceUpdateSize();
    }


    /*
     * ========================================
     * UPDATE
     * ========================================
     */

    private void ForceUpdateSize()
    {
        ResetCache();

        UpdateSizeIfNeeded();
    }


    private void UpdateSizeIfNeeded()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer =
                GetComponent<SpriteRenderer>();
        }


        /*
         * Không Sprite thì không làm gì.
         */
        if (spriteRenderer == null ||
            spriteRenderer.sprite == null)
        {
            return;
        }


        if (targetCamera == null)
        {
            FindCamera();
        }


        if (targetCamera == null ||
            !targetCamera.orthographic)
        {
            return;
        }


        /*
         * Đảm bảo Size có hiệu lực.
         */
        if (spriteRenderer.drawMode ==
            SpriteDrawMode.Simple)
        {
            spriteRenderer.drawMode =
                SpriteDrawMode.Sliced;
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
            transform.lossyScale;


        if (!IsFiniteVector3(lossyScale))
        {
            return;
        }


        /*
         * ========================================
         * CALCULATE SIZE
         * ========================================
         */

        Vector2 targetSize =
            CalculateTargetSize();


        if (!IsFinitePositive(targetSize.x) ||
            !IsFinitePositive(targetSize.y))
        {
            return;
        }


        Vector2 currentSize =
            spriteRenderer.size;


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
            IsFiniteVector2(currentSize) &&
            Approximately(
                currentSize.x,
                targetSize.x) &&
            Approximately(
                currentSize.y,
                targetSize.y);


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

        spriteRenderer.size =
            targetSize;


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
     * CALCULATE
     * ========================================
     */

    private Vector2 CalculateTargetSize()
    {
        /*
         * Kích thước viewport của
         * Orthographic Camera trong World Space.
         */
        float worldHeight =
            targetCamera.orthographicSize *
            2f;

        float worldWidth =
            worldHeight *
            targetCamera.aspect;


        /*
         * SpriteRenderer.size nằm trong
         * local space của Transform.
         *
         * Chuyển chiều ngang / dọc của Camera
         * từ world-space về local-space của
         * SpriteRenderer.
         *
         * Cách này tự xử lý scale của toàn bộ
         * hierarchy.
         */
        Vector3 localHorizontal =
            transform.InverseTransformVector(
                targetCamera.transform.right *
                worldWidth);

        Vector3 localVertical =
            transform.InverseTransformVector(
                targetCamera.transform.up *
                worldHeight);


        float width =
            localHorizontal.magnitude +
            expansionX;

        float height =
            localVertical.magnitude +
            expansionY;


        return
            new Vector2(
                width,
                height);
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
            new Vector3(
                float.NaN,
                float.NaN,
                float.NaN);
    }
}