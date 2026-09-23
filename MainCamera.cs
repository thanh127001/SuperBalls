using System.Collections;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class MainCamera : MonoBehaviour
{
    /*
     * ========================================
     * REFERENCES
     * ========================================
     */

    [Header("References")]

    [SerializeField]
    private ScreenManager playAreaManager;

    [SerializeField]
    private GameManager gameManager;

    [SerializeField]
    private ScoreController scoreController;


    private Camera cam;

    private bool isPlayable;
    private bool isSpawnable;


    /*
     * ========================================
     * CAMERA SHAKE
     * ========================================
     */

    // Thời gian shake.
    private float comboShakeDuration = 0.15f;

    // Cường độ shake.
    private float comboShakeStrength = 0.15f;

    private Vector2 shakeOffset;

    private Coroutine shakeCoroutine;

    private Coroutine comboShakeCheckCoroutine;


    /*
     * ========================================
     * CACHE
     * ========================================
     */

    private Vector2 lastPlayAreaCenter =
        new(
            float.NaN,
            float.NaN
        );

    private float lastPlayAreaWidth =
        float.NaN;

    private float lastPlayAreaHeight =
        float.NaN;

    private float lastCameraAspect =
        float.NaN;


    private const float Epsilon =
        0.000001f;


    /*
     * ========================================
     * UNITY
     * ========================================
     */

    private void Awake()
    {
        FindReferences();

        ConfigureCamera();

        ForceUpdateCamera();
    }


    private void OnEnable()
    {
        FindReferences();

        ConfigureCamera();

        SubscribeEvents();

        ForceUpdateCamera();
    }


    private void OnDisable()
    {
        UnsubscribeEvents();

        StopComboShakeCheck();

        StopShake();
    }


#if UNITY_EDITOR

    private void OnValidate()
    {
        comboShakeDuration =
            Mathf.Max(
                0f,
                comboShakeDuration
            );

        comboShakeStrength =
            Mathf.Max(
                0f,
                comboShakeStrength
            );


        FindReferences();

        ConfigureCamera();

        ForceUpdateCamera();
    }

#endif


    private void LateUpdate()
    {
        /*
         * LateUpdate được dùng làm fallback
         * cho các trường hợp:
         *
         * - đổi orientation;
         * - Game View thay đổi aspect;
         * - Camera aspect thay đổi;
         * - Transform của PlayAreaManager
         *   thay đổi trực tiếp.
         *
         * Cache bên dưới đảm bảo Camera
         * không bị cập nhật thừa.
         */
        UpdateCameraIfNeeded();

        ApplyCameraPosition();
    }


    /*
     * ========================================
     * REFERENCES
     * ========================================
     */

    private void FindReferences()
    {
        if (cam == null)
        {
            cam =
                GetComponent<Camera>();
        }


        if (playAreaManager == null)
        {
            playAreaManager =
                FindFirstObjectByType
                    <ScreenManager>();
        }


        if (gameManager == null)
        {
            gameManager =
                FindFirstObjectByType
                    <GameManager>();
        }


        if (scoreController == null)
        {
            scoreController =
                FindFirstObjectByType
                    <ScoreController>();
        }
    }


    private bool EnsureReferences()
    {
        if (cam != null &&
            playAreaManager != null)
        {
            return true;
        }


        FindReferences();


        return
            cam != null &&
            playAreaManager != null;
    }


    /*
     * ========================================
     * EVENTS
     * ========================================
     */

    private void SubscribeEvents()
    {
        /*
         * ScreenManager
         */
        if (playAreaManager != null)
        {
            playAreaManager.OnLayoutChanged -=
                HandleLayoutChanged;

            playAreaManager.OnLayoutChanged +=
                HandleLayoutChanged;
        }


        /*
         * GameManager
         */
        if (Application.isPlaying &&
            gameManager != null)
        {
            gameManager.OnPlayingChanged -=
                HandlePlayingChanged;

            gameManager.OnPlayingChanged +=
                HandlePlayingChanged;

            isPlayable = false;
            isSpawnable = false;
        }


        /*
         * ScoreController
         */
        if (Application.isPlaying &&
            scoreController != null)
        {
            scoreController.OnComboCompleted -=
                HandleComboCompleted;

            scoreController.OnComboCompleted +=
                HandleComboCompleted;
        }
    }


    private void UnsubscribeEvents()
    {
        if (playAreaManager != null)
        {
            playAreaManager.OnLayoutChanged -=
                HandleLayoutChanged;
        }


        if (gameManager != null)
        {
            gameManager.OnPlayingChanged -=
                HandlePlayingChanged;
        }

        isPlayable = false;
        isSpawnable = false;


        if (scoreController != null)
        {
            scoreController.OnComboCompleted -=
                HandleComboCompleted;
        }
    }


    private void HandlePlayingChanged()
    {
        if (gameManager == null) return;

        isPlayable = gameManager.EffectivePlayable;
        isSpawnable = gameManager.EffectiveSpawnable;
    }


    private void HandleLayoutChanged()
    {
        ForceUpdateCamera();
    }


    private void HandleComboCompleted()
    {
        if (!Application.isPlaying)
        {
            return;
        }


        /*
         * ScoreController phát OnComboCompleted
         * trước khi hoàn tất cộng Level Score.
         *
         * Vì Combo này có thể đồng thời làm:
         *
         * - LevelCompleted;
         * - GameCompleted.
         *
         * nên chưa shake ngay tại đây.
         *
         * Chờ sang frame kế tiếp để GameManager
         * hoàn tất xử lý kết quả Combo.
         */
        StopComboShakeCheck();


        comboShakeCheckCoroutine =
            StartCoroutine(
                ShakeAfterComboResultRoutine()
            );
    }


    private IEnumerator
        ShakeAfterComboResultRoutine()
    {
        /*
         * Chờ ScoreController và GameManager
         * xử lý xong Combo trong frame hiện tại.
         */
        yield return null;


        comboShakeCheckCoroutine =
            null;


        if (gameManager == null)
        {
            FindReferences();
        }


        /*
         * Nếu Combo đồng thời làm hoàn thành
         * Level hoặc Game thì GameManager
         * không còn ở trạng thái Playing.
         *
         * Trong trường hợp đó không shake.
         */
        if (gameManager == null ||
            !isPlayable)
        {
            yield break;
        }


        ShakeCombo();
    }


    private void StopComboShakeCheck()
    {
        if (comboShakeCheckCoroutine == null)
        {
            return;
        }


        StopCoroutine(
            comboShakeCheckCoroutine
        );

        comboShakeCheckCoroutine =
            null;
    }


    /*
     * ========================================
     * CONFIGURATION
     * ========================================
     */

    private void ConfigureCamera()
    {
        if (cam == null)
        {
            return;
        }


        /*
         * Game sử dụng Camera Orthographic.
         */
        if (!cam.orthographic)
        {
            cam.orthographic =
                true;
        }


        /*
         * Camera luôn render toàn bộ màn hình.
         */
        Rect fullScreenRect =
            new Rect(
                0f,
                0f,
                1f,
                1f
            );


        if (cam.rect !=
            fullScreenRect)
        {
            cam.rect =
                fullScreenRect;
        }


        /*
         * Màu chỉ xuất hiện ở vùng
         * không có Sprite/background.
         */
        if (cam.backgroundColor !=
            Color.gray)
        {
            cam.backgroundColor =
                Color.gray;
        }
    }


    /*
     * ========================================
     * PUBLIC
     * ========================================
     */

    public void RefreshCamera()
    {
        FindReferences();

        ConfigureCamera();

        ForceUpdateCamera();
    }


    public void ShakeCombo()
    {
        Shake(
            comboShakeDuration,
            comboShakeStrength
        );
    }


    public void Shake(
        float duration,
        float strength)
    {
        if (!Application.isPlaying)
        {
            return;
        }


        duration =
            Mathf.Max(
                0f,
                duration
            );

        strength =
            Mathf.Max(
                0f,
                strength
            );


        if (duration <= 0f ||
            strength <= 0f)
        {
            StopShake();

            return;
        }


        if (shakeCoroutine != null)
        {
            StopCoroutine(
                shakeCoroutine
            );

            shakeCoroutine = null;
        }


        shakeOffset =
            Vector2.zero;


        shakeCoroutine =
            StartCoroutine(
                ShakeRoutine(
                    duration,
                    strength
                )
            );
    }


    /*
     * ========================================
     * UPDATE
     * ========================================
     */

    private void ForceUpdateCamera()
    {
        ResetCache();

        UpdateCameraIfNeeded();
    }


    private void UpdateCameraIfNeeded()
    {
        if (!EnsureReferences())
        {
            return;
        }


        /*
         * Chỉ sử dụng layout đã được
         * PlayAreaManager xác nhận hợp lệ.
         *
         * Điều này đặc biệt quan trọng
         * khi chuyển:
         *
         * Portrait <-> Landscape.
         */
        if (!playAreaManager.HasValidLayout)
        {
            return;
        }


        Vector2 center =
            playAreaManager.Center;


        float worldWidth =
            playAreaManager.PlayAreaWidth;


        float worldHeight =
            playAreaManager.PlayAreaHeight;


        float cameraAspect =
            cam.aspect;


        /*
         * ========================================
         * VALIDATE
         * ========================================
         */

        if (!IsFiniteVector2(
                center))
        {
            return;
        }


        if (!IsFinitePositive(
                worldWidth) ||
            !IsFinitePositive(
                worldHeight) ||
            !IsFinitePositive(
                cameraAspect))
        {
            return;
        }


        /*
         * ========================================
         * CACHE CHECK
         * ========================================
         */

        if (Approximately(
                center.x,
                lastPlayAreaCenter.x) &&
            Approximately(
                center.y,
                lastPlayAreaCenter.y) &&
            Approximately(
                worldWidth,
                lastPlayAreaWidth) &&
            Approximately(
                worldHeight,
                lastPlayAreaHeight) &&
            Approximately(
                cameraAspect,
                lastCameraAspect))
        {
            return;
        }


        /*
         * ========================================
         * CALCULATE SIZE
         * ========================================
         */

        float sizeByHeight =
            worldHeight *
            0.5f;


        float sizeByWidth =
            worldWidth /
            (
                2f *
                cameraAspect
            );


        if (!IsFinitePositive(
                sizeByHeight) ||
            !IsFinitePositive(
                sizeByWidth))
        {
            return;
        }


        float targetOrthographicSize =
            Mathf.Max(
                sizeByHeight,
                sizeByWidth
            );


        if (!IsFinitePositive(
                targetOrthographicSize))
        {
            return;
        }


        /*
         * ========================================
         * APPLY SIZE
         * ========================================
         */

        if (!Approximately(
                cam.orthographicSize,
                targetOrthographicSize))
        {
            cam.orthographicSize =
                targetOrthographicSize;
        }


        Rect fullScreenRect =
            new Rect(
                0f,
                0f,
                1f,
                1f
            );


        if (cam.rect !=
            fullScreenRect)
        {
            cam.rect =
                fullScreenRect;
        }


        /*
         * ========================================
         * FINAL VALIDATION
         * ========================================
         */

        if (!IsFinitePositive(
                cam.orthographicSize))
        {
            return;
        }


        float finalAspect =
            cam.aspect;


        if (!IsFinitePositive(
                finalAspect))
        {
            return;
        }


        lastPlayAreaCenter =
            center;


        lastPlayAreaWidth =
            worldWidth;


        lastPlayAreaHeight =
            worldHeight;


        lastCameraAspect =
            finalAspect;
    }


    /*
     * ========================================
     * CAMERA POSITION / SHAKE
     * ========================================
     */

    private void ApplyCameraPosition()
    {
        if (!EnsureReferences() ||
            !playAreaManager.HasValidLayout)
        {
            return;
        }


        Vector2 center =
            playAreaManager.Center;


        if (!IsFiniteVector2(
                center) ||
            !IsFiniteVector2(
                shakeOffset))
        {
            return;
        }


        Vector3 currentPosition =
            transform.position;


        if (!IsFiniteVector3(
                currentPosition))
        {
            return;
        }


        Vector3 targetPosition =
            new Vector3(
                center.x + shakeOffset.x,
                center.y + shakeOffset.y,
                currentPosition.z
            );


        if (!IsFiniteVector3(
                targetPosition))
        {
            return;
        }


        if (
            (
                currentPosition -
                targetPosition
            ).sqrMagnitude >
            Epsilon * Epsilon)
        {
            transform.position =
                targetPosition;
        }
    }


    private IEnumerator ShakeRoutine(
        float duration,
        float strength)
    {
        float elapsed = 0f;


        while (elapsed < duration)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            shakeOffset =
                Random.insideUnitCircle *
                strength;


            yield return null;
        }


        shakeOffset =
            Vector2.zero;

        shakeCoroutine =
            null;


        ApplyCameraPosition();
    }


    private void StopShake()
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(
                shakeCoroutine
            );

            shakeCoroutine = null;
        }


        shakeOffset =
            Vector2.zero;


        if (Application.isPlaying)
        {
            ApplyCameraPosition();
        }
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
            value >
            Epsilon;
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


    /*
     * ========================================
     * CACHE
     * ========================================
     */

    private void ResetCache()
    {
        lastPlayAreaCenter =
            new Vector2(
                float.NaN,
                float.NaN
            );


        lastPlayAreaWidth =
            float.NaN;


        lastPlayAreaHeight =
            float.NaN;


        lastCameraAspect =
            float.NaN;
    }
}