using System.Collections;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class GoTextController : MonoBehaviour
{
    /*
     * ========================================
     * REFERENCES
     * ========================================
     */

    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private ScoreController scoreController;

    [Header("Gameplay Message")]
    [Tooltip("TMP Text dùng để hiển thị intro, GO!, GOOD!, GREAT!, EXCELLENT!, AWESOME! và PERFECT!.")]
    [SerializeField] private TMP_Text goText;


    /*
     * ========================================
     * MESSAGE SETTINGS
     * ========================================
     */

    private const string GoMessage = "GO!";

    private const float GoAnimationDelay = 0.5f;
    private const float GoVisibleDuration = 1.5f;

    private const float PraiseAnimationDelay = 0f;
    private const float PraiseVisibleDuration = 0.8f;

    // Animation in/out dùng chung cho mọi message.
    private const float ZoomInDuration = 0.2f;
    private const float SettleDuration = 0.1f;
    private const float ZoomOutDuration = 0.2f;
    private const float MinimumMessageScale = 0.5f;
    private const float MaximumMessageScale = 1.2f;


    /*
     * ========================================
     * RUNTIME STATE
     * ========================================
     */

    private Coroutine messageCoroutine;

    private Vector3 goTextOriginalScale = Vector3.one;

    private bool hasPlayedGoAnimation;
    private bool hasPlayedLevelIntroAnimation;
    private int previousComboScore;
    private bool isPlayable;


    /*
     * ========================================
     * UNITY
     * ========================================
     */

    private void Awake()
    {
        FindReferences();
        InitializeMessage();
        HideMessageImmediately();
    }

    private void OnEnable()
    {
        FindReferences();
        SubscribeEvents();
        RefreshState();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
        StopMessageImmediately();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        FindReferences();
    }
#endif


    /*
     * ========================================
     * REFERENCES
     * ========================================
     */

    private void FindReferences()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        if (scoreController == null)
        {
            scoreController = FindFirstObjectByType<ScoreController>();
        }
    }

    private void InitializeMessage()
    {
        if (goText == null)
        {
            return;
        }

        goTextOriginalScale = goText.rectTransform.localScale;
    }


    /*
     * ========================================
     * EVENTS
     * ========================================
     */

    private void SubscribeEvents()
    {
        if (gameManager != null)
        {
            gameManager.OnGameStateChanged -= HandleGameStateChanged;
            gameManager.OnGameStateChanged += HandleGameStateChanged;

            gameManager.OnPlayingChanged -= HandlePlayingChanged;
            gameManager.OnPlayingChanged += HandlePlayingChanged;

            gameManager.OnLevelChanged -= HandleLevelChanged;
            gameManager.OnLevelChanged += HandleLevelChanged;
        }

        if (scoreController != null)
        {
            scoreController.OnComboChanged -= HandleComboChanged;
            scoreController.OnComboChanged += HandleComboChanged;

            scoreController.OnComboCompleted -= HandleComboCompleted;
            scoreController.OnComboCompleted += HandleComboCompleted;
        }
    }

    private void UnsubscribeEvents()
    {
        if (gameManager != null)
        {
            gameManager.OnGameStateChanged -= HandleGameStateChanged;
            gameManager.OnPlayingChanged -= HandlePlayingChanged;
            gameManager.OnLevelChanged -= HandleLevelChanged;
        }

        if (scoreController != null)
        {
            scoreController.OnComboChanged -= HandleComboChanged;
            scoreController.OnComboCompleted -= HandleComboCompleted;
        }
    }


    /*
     * ========================================
     * STATE
     * ========================================
     */

    private void RefreshState()
    {
        previousComboScore =
            scoreController != null
                ? scoreController.CurrentComboScore
                : 0;

        if (gameManager == null)
        {
            StopMessageImmediately();
            return;
        }

        isPlayable = gameManager.EffectivePlayable;

        HandleGameStateChanged();

        if (isPlayable)
        {
            TryShowGo();
        }
    }

    private void HandleGameStateChanged()
    {
        if (gameManager == null)
        {
            return;
        }

        switch (gameManager.State)
        {
            case GameState.Playing:
                TryShowLevelIntro();
                TryShowGo();
                break;

            case GameState.Ready:
            case GameState.Initializing:
                ResetAllMessageState();
                break;

            case GameState.Paused:
            case GameState.LevelCompleted:
            case GameState.GameCompleted:
            case GameState.GameOver:
            case GameState.None:
            default:
                StopMessageImmediately();
                break;
        }
    }

    private void HandlePlayingChanged()
    {
        if (gameManager == null)
        {
            return;
        }

        isPlayable = gameManager.EffectivePlayable;

        if (!isPlayable)
        {
            return;
        }

        TryShowGo();
    }

    private void HandleLevelChanged()
    {
        hasPlayedGoAnimation = false;
        hasPlayedLevelIntroAnimation = false;
        previousComboScore = 0;

        StopMessageImmediately();
    }


    /*
     * ========================================
     * LEVEL INTRO
     * ========================================
     */

    private void TryShowLevelIntro()
    {
        if (gameManager == null ||
            gameManager.State != GameState.Playing ||
            hasPlayedLevelIntroAnimation)
        {
            return;
        }

        hasPlayedLevelIntroAnimation = true;

        LevelConfig config =
            gameManager.GetCurrentLevelConfig();

        if (config == null ||
            string.IsNullOrWhiteSpace(config.IntroText))
        {
            return;
        }

        // Không truyền visibleDuration:
        // intro được giữ vô thời hạn cho đến khi có message mới.
        ShowText(
            config.IntroText,
            requirePlayerInteraction: false
        );
    }


    /*
     * ========================================
     * GO
     * ========================================
     */

    private void TryShowGo()
    {
        if (gameManager == null ||
            gameManager.State != GameState.Playing ||
            !isPlayable ||
            hasPlayedGoAnimation)
        {
            return;
        }

        hasPlayedGoAnimation = true;

        ShowText(
            GoMessage,
            GoAnimationDelay,
            GoVisibleDuration
        );
    }


    /*
     * ========================================
     * COMBO
     * ========================================
     */

    private void HandleComboChanged()
    {
        if (scoreController == null)
        {
            return;
        }

        int currentComboScore =
            scoreController.CurrentComboScore;

        int requiredComboScore =
            ScoreController.RequiredComboScore;

        bool comboIncreased =
            currentComboScore > previousComboScore;

        previousComboScore = currentComboScore;

        if (!comboIncreased)
        {
            return;
        }

        ShowComboProgressPraise(
            currentComboScore,
            requiredComboScore
        );
    }

    private void HandleComboCompleted()
    {
        ShowComboCompletedPraise();
    }

    private void ShowComboProgressPraise(
        int comboScore,
        int requiredComboScore)
    {
        if (!CanShowGameplayMessage() ||
            comboScore <= 0)
        {
            return;
        }

        if (requiredComboScore > 0 &&
            comboScore >= requiredComboScore)
        {
            return;
        }

        string message;

        if (comboScore <= 3)
        {
            message = "GOOD!";
        }
        else if (comboScore <= 6)
        {
            message = "GREAT!";
        }
        else if (comboScore <= 9)
        {
            message = "EXCELLENT!";
        }
        else
        {
            message = "AWESOME!";
        }

        ShowText(
            message,
            PraiseAnimationDelay,
            PraiseVisibleDuration
        );
    }

    private void ShowComboCompletedPraise()
    {
        if (!CanShowGameplayMessage())
        {
            return;
        }

        ShowText(
            "PERFECT!",
            PraiseAnimationDelay,
            PraiseVisibleDuration
        );
    }


    /*
     * ========================================
     * SHOW TEXT
     * ========================================
     */

    /// <summary>
    /// Hiển thị một gameplay message.
    ///
    /// delay:
    /// - mặc định 0: không delay.
    ///
    /// visibleDuration:
    /// - null: giữ vô thời hạn.
    /// - có giá trị: thời gian giữ sau animation in
    ///   và trước animation out.
    ///
    /// Khi có message mới trong lúc message cũ đang hiển thị,
    /// message cũ sẽ chạy animation out trước,
    /// sau đó message mới mới bắt đầu delay + animation in.
    /// </summary>
    private void ShowText(
        string message,
        float delay = 0f,
        float? visibleDuration = null,
        bool requirePlayerInteraction = true)
    {
        if (goText == null ||
            string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
        }

        messageCoroutine =
            StartCoroutine(
                ShowTextRoutine(
                    message,
                    Mathf.Max(0f, delay),
                    visibleDuration,
                    requirePlayerInteraction
                )
            );
    }

    private IEnumerator ShowTextRoutine(
        string message,
        float delay,
        float? visibleDuration,
        bool requirePlayerInteraction)
    {
        // Nếu message cũ đang hiện, luôn out trước.
        if (goText.gameObject.activeSelf)
        {
            yield return AnimateOut();
        }

        HideMessageImmediately();

        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        if (!CanShowMessage(requirePlayerInteraction))
        {
            messageCoroutine = null;
            yield break;
        }

        goText.text = message;
        SetMessageScale(MinimumMessageScale);
        SetMessageAlpha(0f);
        goText.gameObject.SetActive(true);

        yield return AnimateIn(requirePlayerInteraction);

        if (!CanShowMessage(requirePlayerInteraction))
        {
            HideMessageImmediately();
            messageCoroutine = null;
            yield break;
        }

        // null = hiển thị vô thời hạn.
        if (!visibleDuration.HasValue)
        {
            while (CanShowMessage(requirePlayerInteraction))
            {
                yield return null;
            }

            HideMessageImmediately();
            messageCoroutine = null;
            yield break;
        }

        float waitDuration =
            Mathf.Max(0f, visibleDuration.Value);

        float elapsedTime = 0f;

        while (elapsedTime < waitDuration)
        {
            if (!CanShowMessage(requirePlayerInteraction))
            {
                HideMessageImmediately();
                messageCoroutine = null;
                yield break;
            }

            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        yield return AnimateOut();

        HideMessageImmediately();
        messageCoroutine = null;
    }


    /*
     * ========================================
     * ANIMATION
     * ========================================
     */

    private IEnumerator AnimateIn(
        bool requirePlayerInteraction)
    {
        float elapsedTime = 0f;

        while (elapsedTime < ZoomInDuration)
        {
            if (!CanShowMessage(requirePlayerInteraction))
            {
                yield break;
            }

            elapsedTime += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsedTime / ZoomInDuration
                );

            float easedProgress =
                EaseOutBack(progress);

            float scale =
                Mathf.LerpUnclamped(
                    MinimumMessageScale,
                    MaximumMessageScale,
                    easedProgress
                );

            SetMessageScale(scale);
            SetMessageAlpha(progress);

            yield return null;
        }

        SetMessageScale(MaximumMessageScale);
        SetMessageAlpha(1f);

        elapsedTime = 0f;

        while (elapsedTime < SettleDuration)
        {
            if (!CanShowMessage(requirePlayerInteraction))
            {
                yield break;
            }

            elapsedTime += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsedTime / SettleDuration
                );

            progress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            SetMessageScale(
                Mathf.Lerp(
                    MaximumMessageScale,
                    1f,
                    progress
                )
            );

            yield return null;
        }

        SetMessageScale(1f);
        SetMessageAlpha(1f);
    }

    private IEnumerator AnimateOut()
    {
        if (goText == null ||
            !goText.gameObject.activeSelf)
        {
            yield break;
        }

        Vector3 startScale =
            goText.rectTransform.localScale;

        float startAlpha =
            goText.color.a;

        float elapsedTime = 0f;

        while (elapsedTime < ZoomOutDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsedTime / ZoomOutDuration
                );

            progress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            goText.rectTransform.localScale =
                Vector3.Lerp(
                    startScale,
                    goTextOriginalScale *
                    MinimumMessageScale,
                    progress
                );

            SetMessageAlpha(
                Mathf.Lerp(
                    startAlpha,
                    0f,
                    progress
                )
            );

            yield return null;
        }

        SetMessageScale(MinimumMessageScale);
        SetMessageAlpha(0f);
    }

    private static float EaseOutBack(float progress)
    {
        const float Overshoot = 1.70158f;

        float adjusted =
            progress - 1f;

        return
            1f +
            (Overshoot + 1f) *
            adjusted *
            adjusted *
            adjusted +
            Overshoot *
            adjusted *
            adjusted;
    }


    /*
     * ========================================
     * CONDITIONS
     * ========================================
     */

    private bool CanShowMessage(
        bool requirePlayerInteraction)
    {
        if (gameManager == null ||
            gameManager.State != GameState.Playing)
        {
            return false;
        }

        if (!requirePlayerInteraction)
        {
            return true;
        }

        return isPlayable;
    }

    private bool CanShowGameplayMessage()
    {
        return CanShowMessage(true);
    }


    /*
     * ========================================
     * RESET / HIDE
     * ========================================
     */

    private void ResetAllMessageState()
    {
        hasPlayedGoAnimation = false;
        hasPlayedLevelIntroAnimation = false;
        previousComboScore = 0;

        StopMessageImmediately();
    }

    private void StopMessageImmediately()
    {
        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
            messageCoroutine = null;
        }

        HideMessageImmediately();
    }

    private void HideMessageImmediately()
    {
        if (goText == null)
        {
            return;
        }

        SetMessageAlpha(1f);
        ResetMessageScale();
        goText.text = GoMessage;

        if (goText.gameObject.activeSelf)
        {
            goText.gameObject.SetActive(false);
        }
    }

    private void SetMessageAlpha(float alpha)
    {
        if (goText == null)
        {
            return;
        }

        Color color = goText.color;
        color.a = Mathf.Clamp01(alpha);
        goText.color = color;
    }

    private void SetMessageScale(float multiplier)
    {
        if (goText == null)
        {
            return;
        }

        goText.rectTransform.localScale =
            goTextOriginalScale *
            Mathf.Max(0f, multiplier);
    }

    private void ResetMessageScale()
    {
        if (goText == null)
        {
            return;
        }

        goText.rectTransform.localScale =
            goTextOriginalScale;
    }
}
