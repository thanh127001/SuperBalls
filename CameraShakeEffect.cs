using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public class CameraShakeEffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private GameManager gameManager;

    [SerializeField]
    private ScoreController scoreController;

    [SerializeField]
    private Camera targetCamera;

    // Thời gian rung khi kích hoạt Combo.
    private float comboShakeDuration = 0.15f;

    // Thời gian rung trong lúc đếm ngược Game Over.
    private float gameOverShakeDuration = 1f;

    // Cường độ rung dùng chung cho cả Combo và Game Over.
    private float shakeStrength = 0.15f;

    private bool isPlayable;
    private bool isSpawnable;

    private Coroutine comboCheckCoroutine;
    private Coroutine shakeCoroutine;
    private Coroutine gameOverShakeCoroutine;
    private Vector2 shakeOffset;

    private void Awake()
    {
        FindReferences();
    }

    private void OnEnable()
    {
        FindReferences();

        if (gameManager != null)
        {
            gameManager.OnGameOverStarted -= OnGameOverStarted;
            gameManager.OnGameOverStarted += OnGameOverStarted;

            gameManager.OnPlayingChanged -= OnPlayingChanged;
            gameManager.OnPlayingChanged += OnPlayingChanged;

            isPlayable = false;
            isSpawnable = false;
        }

        if (scoreController != null)
        {
            scoreController.OnComboCompleted -=
                OnComboCompleted;
            scoreController.OnComboCompleted +=
                OnComboCompleted;
        }
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.OnGameOverStarted -= OnGameOverStarted;
            gameManager.OnPlayingChanged -= OnPlayingChanged;
        }

        isPlayable = false;
        isSpawnable = false;

        if (scoreController != null)
        {
            scoreController.OnComboCompleted -=
                OnComboCompleted;
        }

        StopComboCheck();
        StopGameOverShake();
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

        gameOverShakeDuration =
            Mathf.Max(
                0f,
                gameOverShakeDuration
            );

        shakeStrength =
            Mathf.Max(
                0f,
                shakeStrength
            );

        FindReferences();
    }
#endif

    private void LateUpdate()
    {
        if (!Application.isPlaying ||
            targetCamera == null ||
            shakeOffset == Vector2.zero)
        {
            return;
        }

        targetCamera.transform.position +=
            new Vector3(
                shakeOffset.x,
                shakeOffset.y,
                0f
            );
    }

    private void FindReferences()
    {
        if (gameManager == null)
        {
            gameManager =
                FindFirstObjectByType<GameManager>();
        }

        if (scoreController == null)
        {
            scoreController =
                FindFirstObjectByType<ScoreController>();
        }

        if (targetCamera == null)
        {
            targetCamera =
                Camera.main;
        }
    }

    private void OnPlayingChanged()
    {
        if (gameManager == null) return;

        isPlayable = gameManager.EffectivePlayable;
        isSpawnable = gameManager.EffectiveSpawnable;
    }

    /*
     * ========================================
     * GAME OVER SHAKE
     * ========================================
     */
    private void OnGameOverStarted()
    {
        StopGameOverShake();
        StopComboCheck();
        StopShake();

        gameOverShakeCoroutine =
            StartCoroutine(
                GameOverShakeRoutine()
            );
    }

    private IEnumerator GameOverShakeRoutine()
    {
        // Chờ 1 giây trước khi rung
        yield return new WaitForSeconds(1f);

        // Rung theo gameOverShakeDuration và dùng shakeStrength chung
        StartShake(gameOverShakeDuration);

        gameOverShakeCoroutine = null;
    }

    private void StopGameOverShake()
    {
        if (gameOverShakeCoroutine == null)
        {
            return;
        }

        StopCoroutine(
            gameOverShakeCoroutine
        );

        gameOverShakeCoroutine = null;
    }

    /*
     * ========================================
     * COMBO SHAKE
     * ========================================
     */
    private void OnComboCompleted()
    {
        StopComboCheck();

        comboCheckCoroutine =
            StartCoroutine(
                ShakeAfterComboResultRoutine()
            );
    }

    private IEnumerator
        ShakeAfterComboResultRoutine()
    {
        yield return null;

        comboCheckCoroutine = null;

        if (gameManager == null)
        {
            FindReferences();
        }

        if (gameManager == null ||
            !isPlayable)
        {
            yield break;
        }

        // Rung theo comboShakeDuration và dùng shakeStrength chung
        StartShake(comboShakeDuration);
    }

    private void StopComboCheck()
    {
        if (comboCheckCoroutine == null)
        {
            return;
        }

        StopCoroutine(
            comboCheckCoroutine
        );

        comboCheckCoroutine = null;
    }

    /*
     * ========================================
     * COMMON SHAKE LOGIC
     * ========================================
     */
    public void StartShake(float duration)
    {
        if (duration <= 0f ||
            shakeStrength <= 0f)
        {
            return;
        }

        if (shakeCoroutine != null)
        {
            StopCoroutine(
                shakeCoroutine
            );
        }

        shakeCoroutine =
            StartCoroutine(
                ShakeRoutine(duration, shakeStrength)
            );
    }

    private IEnumerator ShakeRoutine(float duration, float strength)
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

        shakeCoroutine = null;
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
    }
}