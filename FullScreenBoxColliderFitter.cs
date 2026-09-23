using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public class FullScreenBoxColliderFitter : MonoBehaviour
{
    public enum ScaleMode
    {
        Horizontal,
        Vertical,
        HorizontalAndVertical
    }


    /*
     * ========================================
     * REFERENCES
     * ========================================
     */

    [Header("References")]

    [SerializeField]
    private ScreenManager screenManager;


    /*
     * ========================================
     * SCALE MODE
     * ========================================
     */

    [Header("Scale Mode")]

    [SerializeField]
    private ScaleMode scaleMode =
        ScaleMode.HorizontalAndVertical;


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

    private BoxCollider2D boxCollider;


    private Vector2 lastValidSize =
        Vector2.one;

    private bool hasLastValidSize;


    private float lastScreenWidth =
        float.NaN;

    private float lastScreenHeight =
        float.NaN;

    private float lastExpansionX =
        float.NaN;

    private float lastExpansionY =
        float.NaN;

    private ScaleMode lastScaleMode;


    private const float Epsilon =
        0.000001f;


    /*
     * ========================================
     * MODE
     * ========================================
     */

    private bool UsesHorizontal =>
        scaleMode ==
            ScaleMode.Horizontal ||
        scaleMode ==
            ScaleMode.HorizontalAndVertical;


    private bool UsesVertical =>
        scaleMode ==
            ScaleMode.Vertical ||
        scaleMode ==
            ScaleMode.HorizontalAndVertical;


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

        SubscribeEvents();

        ForceUpdateSize();
    }


    private void OnDisable()
    {
        UnsubscribeEvents();
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


        /*
         * Chiều không được scale
         * không sử dụng Expansion.
         */
        if (!UsesHorizontal)
        {
            expansionX = 0f;
        }


        if (!UsesVertical)
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
        FindReferences();


        if (boxCollider == null)
        {
            return;
        }


        Vector2 currentSize =
            boxCollider.size;


        if (!IsFiniteVector2(currentSize))
        {
            return;
        }


        if (!hasLastValidSize)
        {
            lastValidSize =
                currentSize;

            hasLastValidSize =
                true;
        }
    }


    /*
     * ========================================
     * REFERENCES
     * ========================================
     */

    private void FindReferences()
    {
        if (boxCollider == null)
        {
            boxCollider =
                GetComponent<BoxCollider2D>();
        }


        if (screenManager == null)
        {
            screenManager =
                FindFirstObjectByType
                    <ScreenManager>();
        }
    }


    private bool EnsureReferences()
    {
        if (boxCollider != null &&
            screenManager != null)
        {
            return true;
        }


        FindReferences();


        return
            boxCollider != null &&
            screenManager != null;
    }


    /*
     * ========================================
     * EVENTS
     * ========================================
     */

    private void SubscribeEvents()
    {
        if (screenManager == null)
        {
            return;
        }


        screenManager.OnLayoutChanged -=
            HandleLayoutChanged;

        screenManager.OnLayoutChanged +=
            HandleLayoutChanged;
    }


    private void UnsubscribeEvents()
    {
        if (screenManager == null)
        {
            return;
        }


        screenManager.OnLayoutChanged -=
            HandleLayoutChanged;
    }


    private void HandleLayoutChanged()
    {
        ForceUpdateSize();
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
        if (!EnsureReferences())
        {
            RestoreLastValidSize();

            return;
        }


        if (!screenManager.HasValidLayout)
        {
            RestoreLastValidSize();

            return;
        }


        float screenWidth =
            screenManager.ScreenWidth;

        float screenHeight =
            screenManager.ScreenHeight;


        if (!IsFinitePositive(screenWidth) ||
            !IsFinitePositive(screenHeight))
        {
            RestoreLastValidSize();

            return;
        }


        if (!TryCalculateTargetSize(
                screenWidth,
                screenHeight,
                out Vector2 targetSize))
        {
            RestoreLastValidSize();

            return;
        }


        bool layoutUnchanged =
            lastScaleMode ==
                scaleMode &&
            Approximately(
                screenWidth,
                lastScreenWidth) &&
            Approximately(
                screenHeight,
                lastScreenHeight) &&
            Approximately(
                expansionX,
                lastExpansionX) &&
            Approximately(
                expansionY,
                lastExpansionY);


        bool managedSizeCorrect =
            IsManagedSizeCorrect(
                targetSize
            );


        if (layoutUnchanged &&
            managedSizeCorrect)
        {
            return;
        }


        if (!ApplySize(targetSize))
        {
            RestoreLastValidSize();

            return;
        }


        lastValidSize =
            boxCollider.size;

        hasLastValidSize =
            true;


        lastScreenWidth =
            screenWidth;

        lastScreenHeight =
            screenHeight;

        lastExpansionX =
            expansionX;

        lastExpansionY =
            expansionY;

        lastScaleMode =
            scaleMode;
    }


    /*
     * ========================================
     * CALCULATE
     * ========================================
     */

    private bool TryCalculateTargetSize(
        float screenWidth,
        float screenHeight,
        out Vector2 targetSize)
    {
        Vector2 currentSize =
            boxCollider.size;


        if (!IsFiniteVector2(currentSize))
        {
            currentSize =
                hasLastValidSize
                    ? lastValidSize
                    : Vector2.one;
        }


        float sizeX =
            currentSize.x;

        float sizeY =
            currentSize.y;


        if (UsesHorizontal)
        {
            float targetWidth =
                screenWidth +
                expansionX;


            if (!IsFinitePositive(
                    targetWidth))
            {
                targetSize =
                    currentSize;

                return false;
            }


            sizeX =
                targetWidth;
        }


        if (UsesVertical)
        {
            float targetHeight =
                screenHeight +
                expansionY;


            if (!IsFinitePositive(
                    targetHeight))
            {
                targetSize =
                    currentSize;

                return false;
            }


            sizeY =
                targetHeight;
        }


        targetSize =
            new Vector2(
                sizeX,
                sizeY
            );


        if (!IsFiniteVector2(targetSize))
        {
            return false;
        }


        if (UsesHorizontal &&
            targetSize.x <= Epsilon)
        {
            return false;
        }


        if (UsesVertical &&
            targetSize.y <= Epsilon)
        {
            return false;
        }


        return true;
    }


    /*
     * ========================================
     * MANAGED SIZE CHECK
     * ========================================
     */

    private bool IsManagedSizeCorrect(
        Vector2 targetSize)
    {
        if (boxCollider == null)
        {
            return false;
        }


        Vector2 currentSize =
            boxCollider.size;


        if (!IsFiniteVector2(currentSize))
        {
            return false;
        }


        if (UsesHorizontal &&
            !Approximately(
                currentSize.x,
                targetSize.x))
        {
            return false;
        }


        if (UsesVertical &&
            !Approximately(
                currentSize.y,
                targetSize.y))
        {
            return false;
        }


        return true;
    }


    /*
     * ========================================
     * APPLY
     * ========================================
     */

    private bool ApplySize(
        Vector2 targetSize)
    {
        if (boxCollider == null ||
            !IsFiniteVector2(targetSize))
        {
            return false;
        }


        Vector2 currentSize =
            boxCollider.size;


        if (!IsFiniteVector2(currentSize))
        {
            currentSize =
                hasLastValidSize
                    ? lastValidSize
                    : Vector2.one;
        }


        Vector2 finalSize =
            currentSize;


        if (UsesHorizontal)
        {
            finalSize.x =
                targetSize.x;
        }


        if (UsesVertical)
        {
            finalSize.y =
                targetSize.y;
        }


        if (!IsFiniteVector2(finalSize))
        {
            return false;
        }


        if (finalSize.x <= Epsilon ||
            finalSize.y <= Epsilon)
        {
            return false;
        }


        if ((currentSize - finalSize)
                .sqrMagnitude <=
            Epsilon * Epsilon)
        {
            return true;
        }


        boxCollider.size =
            finalSize;


        return
            IsFiniteVector2(
                boxCollider.size
            );
    }


    /*
     * ========================================
     * RESTORE
     * ========================================
     */

    private void RestoreLastValidSize()
    {
        if (!hasLastValidSize ||
            boxCollider == null ||
            !IsFiniteVector2(
                lastValidSize))
        {
            return;
        }


        Vector2 currentSize =
            boxCollider.size;


        if (!IsFiniteVector2(currentSize))
        {
            boxCollider.size =
                lastValidSize;

            return;
        }


        Vector2 restoredSize =
            currentSize;


        if (UsesHorizontal)
        {
            restoredSize.x =
                lastValidSize.x;
        }


        if (UsesVertical)
        {
            restoredSize.y =
                lastValidSize.y;
        }


        if (!IsFiniteVector2(restoredSize))
        {
            return;
        }


        if (restoredSize.x <= Epsilon ||
            restoredSize.y <= Epsilon)
        {
            return;
        }


        if ((currentSize - restoredSize)
                .sqrMagnitude <=
            Epsilon * Epsilon)
        {
            return;
        }


        boxCollider.size =
            restoredSize;
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


    /*
     * ========================================
     * CACHE
     * ========================================
     */

    private void ResetCache()
    {
        lastScreenWidth =
            float.NaN;

        lastScreenHeight =
            float.NaN;

        lastExpansionX =
            float.NaN;

        lastExpansionY =
            float.NaN;
    }
}