using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class Ball : MonoBehaviour
{
    /*
     * ========================================
     * EVENTS
     * ========================================
     */

    /// <summary>
    /// Phát khi một va chạm hợp lệ cần collision SFX.
    ///
    /// Ball chỉ thông báo sự kiện gameplay.
    /// Ball không biết SoundManager tồn tại.
    /// </summary>
    public static event Action OnCollisionSoundRequested;

    /*
     * ========================================
     * BALL
     * ========================================
     */

    [Header("Ball")]

    [SerializeField]
    private BallType ballType;


    /*
     * ========================================
     * BOUNCE
     * ========================================
     */

    // Lực bounce tối thiểu.
    private float minimumBounceForce = 12f;

    // Lực bounce tối đa.
    private float maximumBounceForce = 15f;

    /*
     * Khi BouncingBall có tốc độ
     * <= giá trị này thì Collider
     * sẽ được disable.
     */
    private float bouncingDisableColliderSpeed = 2f;


    /*
     * ========================================
     * BOUNCING EFFECT
     * ========================================
     */

    [Header("Bouncing Effect")]

    [Tooltip(
        "Prefab hiệu ứng được tạo khi Ball " +
        "chuyển sang trạng thái Bouncing.")]
    [SerializeField]
    private GameObject bouncingEffectPrefab;


    /*
     * ========================================
     * COLLISION SOUND
     * ========================================
     */
    // Tốc độ va chạm tối thiểu để phát âm thanh.
    private float minimumCollisionSpeed = 5f;


    /*
     * ========================================
     * DESTROY BOUNDARY
     * ========================================
     */
    // Độ lệch Y so với đáy Screen để hủy Ball đang bounce.
    private float destroyYOffset = -2f;


    /*
     * ========================================
     * REFERENCES
     * ========================================
     */

    private BallController ballController;

    private GameManager gameManager;

    private ScreenManager playAreaManager;

    private Rigidbody2D rigidBody;

    private CircleCollider2D circleCollider;

    private SortingGroup sortingGroup;


    /*
     * ========================================
     * STATE
     * ========================================
     */

    /*
     * Ball đã đi vào Play Area.
     */
    private bool hasEnteredPlayArea;


    /*
     * Ball đã chuyển sang
     * trạng thái BouncingBall.
     */
    private bool isBouncing;


    /*
     * Ball đã yêu cầu Destroy.
     */
    private bool isDestroyRequested;


    /*
     * Collider của BouncingBall
     * đã được disable.
     */
    private bool hasDisabledCollider;


    /*
     * ========================================
     * SORTING
     * ========================================
     */

    private const int
        NormalSortingOrder = 0;

    private const int
        BouncingSortingOrder = 100;

    private const int
        BouncingEffectSortingOrder = 0;


    /*
     * ========================================
     * LAYERS
     * ========================================
     */

    private const string
        SelectableBallLayerName =
            "Ball";


    private const string
        BouncingBallLayerName =
            "BouncingBall";


    private const string
        BallColumnGuideLayerName =
            "BallColumnGuide";


    /*
     * ========================================
     * RUNTIME
     * ========================================
     */

    private int selectableBallLayer =
        -1;

    private int selectableBallLayerMask;

    private int ballColumnGuideLayer =
        -1;


    private Coroutine
        disableColliderCoroutine;


    private GameObject
        bouncingEffectInstance;


    /*
     * Các Ball đang overlap tại thời điểm
     * collider vừa được bật.
     */
    private readonly HashSet<int>
        initialOverlapBallIds =
            new();


    /*
     * Buffer dùng lại để tránh allocation
     * khi kiểm tra overlap.
     */
    private readonly Collider2D[]
        overlapResults =
            new Collider2D[16];


    /*
     * ========================================
     * PUBLIC
     * ========================================
     */

    public BallType BallType =>
        ballType;


    public Rigidbody2D Rigidbody =>
        rigidBody;


    public CircleCollider2D CircleCollider =>
        circleCollider;


    public bool HasEnteredPlayArea =>
        hasEnteredPlayArea;


    public bool IsBouncing =>
        isBouncing;


    public bool IsDestroyRequested =>
        isDestroyRequested;


    /*
     * Ball có thể tham gia selection.
     */
    public bool IsSelectable =>
        hasEnteredPlayArea &&
        !isBouncing &&
        !isDestroyRequested &&
        circleCollider != null &&
        circleCollider.enabled;


    /*
     * ========================================
     * UNITY
     * ========================================
     */

    private void Awake()
    {
        CacheComponents();

        FindReferences();

        InitializeLayers();

        ResetRuntimeState();
    }


    private void Start()
    {
        /*
         * BallSpawner RegisterBall() vào GameManager
         * ngay sau Instantiate.
         *
         * Đây là fallback nếu Ball
         * được tạo từ nơi khác.
         */
        RegisterToGameManager();
    }


    private void Update()
    {
        if (isDestroyRequested)
        {
            return;
        }


        if (isBouncing)
        {
            UpdateBouncingDestroyBoundary();

            return;
        }


        if (!hasEnteredPlayArea)
        {
            UpdateEnteringPlayArea();
        }
    }


    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.UnregisterBall(
                this
            );
        }
    }


#if UNITY_EDITOR

    private void OnValidate()
    {
        minimumCollisionSpeed =
            Mathf.Max(
                0f,
                minimumCollisionSpeed
            );


        minimumBounceForce =
            Mathf.Max(
                0f,
                minimumBounceForce
            );

        maximumBounceForce =
            Mathf.Max(
                minimumBounceForce,
                maximumBounceForce
            );

        bouncingDisableColliderSpeed =
            Mathf.Max(
                0f,
                bouncingDisableColliderSpeed
            );
    }

#endif


    /*
     * ========================================
     * COMPONENTS
     * ========================================
     */

    private void CacheComponents()
    {
        rigidBody =
            GetComponent<Rigidbody2D>();


        circleCollider =
            GetComponent<CircleCollider2D>();


        sortingGroup =
            GetComponent<SortingGroup>();
    }


    /*
     * ========================================
     * REFERENCES
     * ========================================
     */

    private void FindReferences()
    {
        if (ballController == null)
        {
            ballController =
                FindFirstObjectByType
                    <BallController>();
        }


        if (gameManager == null)
        {
            gameManager =
                FindFirstObjectByType
                    <GameManager>();
        }


        if (playAreaManager == null)
        {
            playAreaManager =
                FindFirstObjectByType
                    <ScreenManager>();
        }
    }


    private void RegisterToGameManager()
    {
        if (gameManager == null)
        {
            gameManager =
                FindFirstObjectByType
                    <GameManager>();
        }

        gameManager?.RegisterBall(this);
    }


    /*
     * ========================================
     * INITIALIZATION
     * ========================================
     */

    private void InitializeLayers()
    {
        /*
         * Ball layer.
         */
        selectableBallLayer =
            LayerMask.NameToLayer(
                SelectableBallLayerName
            );


        if (selectableBallLayer < 0)
        {
            selectableBallLayerMask =
                0;


            Debug.LogError(
                $"{name}: Không tìm thấy Layer " +
                $"'{SelectableBallLayerName}'.",
                this
            );
        }
        else
        {
            selectableBallLayerMask =
                1 << selectableBallLayer;
        }


        /*
         * Column Guide layer.
         */
        ballColumnGuideLayer =
            LayerMask.NameToLayer(
                BallColumnGuideLayerName
            );


        if (ballColumnGuideLayer < 0)
        {
            Debug.LogError(
                $"{name}: Không tìm thấy Layer " +
                $"'{BallColumnGuideLayerName}'.",
                this
            );
        }
    }


    private void ResetRuntimeState()
    {
        hasEnteredPlayArea =
            false;


        isBouncing =
            false;


        isDestroyRequested =
            false;


        hasDisabledCollider =
            false;


        initialOverlapBallIds.Clear();


        bouncingEffectInstance =
            null;


        /*
         * Ball vừa spawn ở phía trên
         * Play Area nên collider chưa bật.
         */
        if (circleCollider != null)
        {
            circleCollider.enabled =
                false;
        }


        if (sortingGroup != null)
        {
            sortingGroup.sortingOrder =
                NormalSortingOrder;
        }
    }


    /*
     * ========================================
     * ENTER PLAY AREA
     * ========================================
     */

    private void UpdateEnteringPlayArea()
    {
        if (playAreaManager == null)
        {
            playAreaManager =
                FindFirstObjectByType
                    <ScreenManager>();


            if (playAreaManager == null)
            {
                return;
            }
        }


        if (!playAreaManager.HasValidLayout)
        {
            return;
        }


        float playAreaTop =
            playAreaManager.PlayAreaTop;


        if (!IsFinite(
                playAreaTop))
        {
            return;
        }


        float currentY =
            transform.position.y;


        if (!IsFinite(
                currentY))
        {
            return;
        }


        if (currentY >
            playAreaTop - 1f)
        {
            return;
        }


        EnterPlayArea();
    }


    private void EnterPlayArea()
    {
        if (hasEnteredPlayArea ||
            isBouncing ||
            isDestroyRequested)
        {
            return;
        }


        hasEnteredPlayArea =
            true;


        /*
         * Đưa Ball vào đúng Layer
         * trước khi kiểm tra overlap.
         */
        if (selectableBallLayer >= 0)
        {
            gameObject.layer =
                selectableBallLayer;
        }


        if (circleCollider != null)
        {
            circleCollider.enabled =
                true;


            /*
             * Nếu Ball đang đè lên Ball khác
             * tại thời điểm collider được bật,
             * ghi nhận các Ball đó để bỏ qua
             * collision sound ban đầu.
             */
            CacheInitialOverlappingBalls();
        }
    }


    /*
     * ========================================
     * INITIAL OVERLAP
     * ========================================
     */

    private void CacheInitialOverlappingBalls()
    {
        initialOverlapBallIds.Clear();


        if (circleCollider == null ||
            selectableBallLayerMask == 0)
        {
            return;
        }


        Vector2 center =
            transform.TransformPoint(
                circleCollider.offset
            );


        Vector3 scale =
            transform.lossyScale;


        float radius =
            circleCollider.radius *
            Mathf.Max(
                Mathf.Abs(scale.x),
                Mathf.Abs(scale.y)
            );


        if (!IsFinite(center.x) ||
            !IsFinite(center.y) ||
            !IsFinite(radius) ||
            radius <= 0f)
        {
            return;
        }


        ContactFilter2D filter =
            new ContactFilter2D
            {
                useLayerMask = true,
                layerMask =
                    selectableBallLayerMask
            };


        int overlapCount =
            Physics2D.OverlapCircle(
                center,
                radius,
                filter,
                overlapResults
            );


        for (int i = 0;
             i < overlapCount;
             i++)
        {
            Collider2D overlap =
                overlapResults[i];


            if (overlap == null ||
                overlap == circleCollider)
            {
                continue;
            }


            if (!overlap.TryGetComponent(
                    out Ball otherBall))
            {
                continue;
            }


            if (otherBall == this ||
                otherBall.isDestroyRequested)
            {
                continue;
            }


            initialOverlapBallIds.Add(
                otherBall.GetInstanceID()
            );
        }


        /*
         * Xóa reference trong buffer.
         */
        for (int i = 0;
             i < overlapCount;
             i++)
        {
            overlapResults[i] =
                null;
        }
    }


    /*
     * ========================================
     * CLICK
     * ========================================
     */

    public void OnClicked()
    {
        if (!IsSelectable)
        {
            return;
        }


        if (ballController == null)
        {
            ballController =
                FindFirstObjectByType
                    <BallController>();


            if (ballController == null)
            {
                return;
            }
        }


        ballController.SelectBall(
            this
        );
    }


    /*
     * ========================================
     * COLLISION SOUND
     * ========================================
     */

    private void OnCollisionEnter2D(
        Collision2D collision)
    {
        /*
         * Chỉ Ball đang ở Layer "Ball"
         * mới được phát collision sound.
         */
        if (gameObject.layer !=
            selectableBallLayer)
        {
            return;
        }


        if (isDestroyRequested)
        {
            return;
        }


        /*
         * ========================================
         * COLUMN GUIDE
         * ========================================
         *
         * Ball vẫn collision vật lý với Guide,
         * nhưng tuyệt đối không phát sound.
         */

        if (ballColumnGuideLayer >= 0 &&
            collision.gameObject.layer ==
            ballColumnGuideLayer)
        {
            return;
        }


        /*
         * ========================================
         * IMPACT SPEED
         * ========================================
         */

        if (collision.contactCount <= 0)
        {
            return;
        }


        ContactPoint2D contact =
            collision.GetContact(0);


        /*
         * Chỉ lấy thành phần velocity
         * hướng trực tiếp vào bề mặt.
         */
        float impactSpeed =
            Mathf.Abs(
                Vector2.Dot(
                    collision.relativeVelocity,
                    contact.normal
                )
            );


        if (!IsFinite(impactSpeed) ||
            impactSpeed <
            minimumCollisionSpeed)
        {
            return;
        }


        /*
         * ========================================
         * BALL ↔ BALL
         * ========================================
         */

        if (collision.gameObject.TryGetComponent(
                out Ball otherBall))
        {
            if (otherBall == this ||
                otherBall.isDestroyRequested)
            {
                return;
            }


            /*
             * Cả hai Ball đều nhận
             * OnCollisionEnter2D.
             *
             * Chỉ một Ball chịu trách nhiệm
             * xử lý collision sound.
             */
            if (GetInstanceID() >
                otherBall.GetInstanceID())
            {
                return;
            }


            int thisId =
                GetInstanceID();


            int otherId =
                otherBall.GetInstanceID();


            /*
             * Nếu hai Ball đã overlap ngay lúc
             * collider của một trong hai Ball
             * được bật thì bỏ qua sound.
             */
            bool wasInitiallyOverlapping =
                initialOverlapBallIds.Contains(
                    otherId
                ) ||
                otherBall
                    .initialOverlapBallIds
                    .Contains(
                        thisId
                    );


            if (wasInitiallyOverlapping)
            {
                initialOverlapBallIds.Remove(
                    otherId
                );


                otherBall
                    .initialOverlapBallIds
                    .Remove(
                        thisId
                    );


                return;
            }
        }


        /*
         * ========================================
         * SOUND
         * ========================================
         */

        OnCollisionSoundRequested?.Invoke();
    }


    /*
     * ========================================
     * BEGIN BOUNCING
     * ========================================
     */

    public void BeginBouncing()
    {
        if (isBouncing ||
            isDestroyRequested)
        {
            return;
        }


        isBouncing =
            true;


        hasDisabledCollider =
            false;


        /*
         * Ball tự thực hiện toàn bộ trạng thái bounce.
         */
        SetBouncingLayer();


        SetBouncingSorting();


        CreateBouncingEffect();


        if (circleCollider != null)
        {
            circleCollider.enabled =
                true;
        }


        Vector2 bounceDirection =
            CreateBounceDirection();


        float bounceForce =
            CreateBounceForce();


        ApplyBounceForce(
            bounceDirection,
            bounceForce
        );


        StartDisableColliderRoutine(
            bouncingDisableColliderSpeed
        );
    }


    /*
     * ========================================
     * BOUNCING EFFECT
     * ========================================
     */

    private void CreateBouncingEffect()
    {
        if (bouncingEffectPrefab == null ||
            bouncingEffectInstance != null)
        {
            return;
        }


        bouncingEffectInstance =
            Instantiate(
                bouncingEffectPrefab,
                transform
            );


        Transform effectTransform =
            bouncingEffectInstance.transform;


        effectTransform.localPosition =
            Vector3.zero;


        effectTransform.localRotation =
            Quaternion.identity;


        /*
         * Giữ scale gốc của Prefab.
         */
        effectTransform.localScale =
            bouncingEffectPrefab
                .transform
                .localScale;


        /*
         * Nếu prefab có SortingGroup,
         * đặt Order in Layer = 0.
         */
        SortingGroup[] effectSortingGroups =
            bouncingEffectInstance
                .GetComponentsInChildren
                    <SortingGroup>(true);


        for (int i = 0;
             i < effectSortingGroups.Length;
             i++)
        {
            SortingGroup effectSortingGroup =
                effectSortingGroups[i];


            if (effectSortingGroup == null)
            {
                continue;
            }


            effectSortingGroup.sortingOrder =
                BouncingEffectSortingOrder;
        }


        /*
         * Đồng thời xử lý các Renderer
         * không nằm dưới một SortingGroup
         * riêng.
         */
        Renderer[] effectRenderers =
            bouncingEffectInstance
                .GetComponentsInChildren
                    <Renderer>(true);


        for (int i = 0;
             i < effectRenderers.Length;
             i++)
        {
            Renderer effectRenderer =
                effectRenderers[i];


            if (effectRenderer == null)
            {
                continue;
            }


            effectRenderer.sortingOrder =
                BouncingEffectSortingOrder;
        }
    }


    /*
     * ========================================
     * BOUNCING LAYER
     * ========================================
     */

    private void SetBouncingLayer()
    {
        int layer =
            LayerMask.NameToLayer(
                BouncingBallLayerName
            );


        if (layer < 0)
        {
            Debug.LogWarning(
                $"{name}: Không tìm thấy Layer " +
                $"'{BouncingBallLayerName}'.",
                this
            );

            return;
        }


        gameObject.layer =
            layer;
    }


    private void SetBouncingSorting()
    {
        int sortingLayerId =
            SortingLayer.NameToID(
                BouncingBallLayerName
            );


        if (sortingGroup != null)
        {
            sortingGroup.sortingLayerID =
                sortingLayerId;

            sortingGroup.sortingOrder =
                BouncingSortingOrder;

            return;
        }


        Renderer[] renderers =
            GetComponentsInChildren<Renderer>(
                true
            );


        for (int i = 0;
             i < renderers.Length;
             i++)
        {
            Renderer ballRenderer =
                renderers[i];


            if (ballRenderer == null)
            {
                continue;
            }


            ballRenderer.sortingLayerID =
                sortingLayerId;

            ballRenderer.sortingOrder =
                BouncingSortingOrder;
        }
    }


    /*
     * ========================================
     * BOUNCE FORCE
     * ========================================
     */

    private static Vector2
        CreateBounceDirection()
    {
        Vector2 direction =
            new Vector2(
                UnityEngine.Random.Range(
                    -1f,
                    1f
                ),
                UnityEngine.Random.Range(
                    0.5f,
                    1f
                )
            );


        if (direction.sqrMagnitude <=
            Mathf.Epsilon)
        {
            return Vector2.up;
        }


        return direction.normalized;
    }


    private float CreateBounceForce()
    {
        float minimumForce =
            Mathf.Max(
                0f,
                Mathf.Min(
                    minimumBounceForce,
                    maximumBounceForce
                )
            );


        float maximumForce =
            Mathf.Max(
                minimumForce,
                Mathf.Max(
                    minimumBounceForce,
                    maximumBounceForce
                )
            );


        /*
         * Giữ phân bố lực cũ:
         * phần lớn Ball gần minimumBounceForce,
         * một số Ball nhận lực lớn hơn.
         */
        const float forceBias = 3f;


        float forceRatio =
            Mathf.Pow(
                UnityEngine.Random.value,
                forceBias
            );


        return Mathf.Lerp(
            minimumForce,
            maximumForce,
            forceRatio
        );
    }


    private void ApplyBounceForce(
        Vector2 direction,
        float force)
    {
        if (rigidBody == null)
        {
            return;
        }


        if (!IsFinite(direction.x) ||
            !IsFinite(direction.y) ||
            direction.sqrMagnitude <=
            Mathf.Epsilon)
        {
            direction =
                Vector2.up;
        }
        else
        {
            direction.Normalize();
        }


        float safeForce =
            IsFinite(force)
                ? Mathf.Max(0f, force)
                : 0f;


        rigidBody.WakeUp();


        rigidBody.AddForce(
            direction *
            safeForce,
            ForceMode2D.Impulse
        );
    }


    /*
     * ========================================
     * DISABLE COLLIDER ROUTINE
     * ========================================
     */

    private void StartDisableColliderRoutine(
        float maximumSpeed)
    {
        if (disableColliderCoroutine !=
            null)
        {
            StopCoroutine(
                disableColliderCoroutine
            );
        }


        disableColliderCoroutine =
            StartCoroutine(
                WaitForLowSpeedAndDisableCollider(
                    maximumSpeed
                )
            );
    }


    private IEnumerator
        WaitForLowSpeedAndDisableCollider(
            float maximumSpeed)
    {
        float speedThreshold =
            Mathf.Max(
                0f,
                maximumSpeed
            );


        float speedThresholdSquared =
            speedThreshold *
            speedThreshold;


        /*
         * Chờ Physics xử lý lực bounce.
         */
        yield return
            new WaitForFixedUpdate();


        while (isBouncing &&
               !isDestroyRequested)
        {
            if (rigidBody == null)
            {
                break;
            }


            if (circleCollider == null ||
                !circleCollider.enabled)
            {
                hasDisabledCollider =
                    true;


                break;
            }


            /*
             * Dùng sqrMagnitude để
             * tránh phép sqrt.
             */
            if (rigidBody
                    .linearVelocity
                    .sqrMagnitude <=
                speedThresholdSquared)
            {
                DisableColliderForFalling();


                break;
            }


            yield return
                new WaitForFixedUpdate();
        }


        disableColliderCoroutine =
            null;
    }


    /*
     * ========================================
     * DISABLE COLLIDER
     * ========================================
     */

    private void DisableColliderForFalling()
    {
        if (hasDisabledCollider)
        {
            return;
        }


        hasDisabledCollider =
            true;


        if (circleCollider != null)
        {
            circleCollider.enabled =
                false;
        }


        /*
         * Không thay đổi:
         *
         * - velocity
         * - gravityScale
         * - Rigidbody2D body type
         *
         * Ball tiếp tục rơi tự nhiên.
         */
    }


    /*
     * ========================================
     * BOUNCING DESTROY BOUNDARY
     * ========================================
     */

    private void UpdateBouncingDestroyBoundary()
    {
        if (!isBouncing ||
            isDestroyRequested)
        {
            return;
        }


        if (playAreaManager == null)
        {
            playAreaManager =
                FindFirstObjectByType
                    <ScreenManager>();


            if (playAreaManager == null)
            {
                return;
            }
        }


        if (!playAreaManager.HasValidLayout)
        {
            return;
        }


        float destroyY =
            playAreaManager.ScreenBottom +
            destroyYOffset;


        float currentY =
            transform.position.y;


        if (!IsFinite(destroyY) ||
            !IsFinite(currentY) ||
            currentY > destroyY)
        {
            return;
        }


        RequestDestroy();
    }


    /*
     * ========================================
     * DESTROY
     * ========================================
     */

    public void RequestDestroy()
    {
        if (isDestroyRequested)
        {
            return;
        }


        isDestroyRequested =
            true;


        initialOverlapBallIds.Clear();


        if (disableColliderCoroutine !=
            null)
        {
            StopCoroutine(
                disableColliderCoroutine
            );


            disableColliderCoroutine =
                null;
        }


        if (circleCollider != null)
        {
            circleCollider.enabled =
                false;
        }


        Destroy(
            gameObject
        );
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
}
