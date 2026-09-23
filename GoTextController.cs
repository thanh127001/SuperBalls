using System.Collections;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class GoTextController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private ScoreController scoreController;

    [Header("Gameplay Message")]
    [Tooltip("TMP Text dùng để hiển thị intro, GO!, GOOD!, GREAT!, EXCELLENT!, AWESOME! và PERFECT!.")]
    [SerializeField] private TMP_Text goText;

    // Intro text animation settings
    private float introAnimationDelay = 0.5f;
    private float introVisibleDuration = 2f;

    // GO! text animation settings
    private float goAnimationDelay = 0.5f;
    private float goVisibleDuration = 1.5f;

    // Praise text animation settings
    private float praiseAnimationDelay = 0f;
    private float praiseVisibleDuration = 0.8f;

    // Animation settings
    private float zoomInDuration = 0.2f;
    private float settleDuration = 0.1f;
    private float zoomOutDuration = 0.2f;
    private float minimumMessageScale = 0.5f;
    private float maximumMessageScale = 1.2f;

    private Coroutine messageAnimationCoroutine;
    private string originalGoTextContent = "GO!";
    private Vector3 goTextOriginalScale = Vector3.one;

    private bool hasPlayedGoAnimation;
    private bool hasPlayedLevelIntroAnimation;
    private bool isPlayingLevelIntroAnimation;
    private int previousComboScore;

    private bool isPlayable;
    private bool isSpawnable;

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
        StopMessageAnimation();
        HideMessageImmediately();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        introAnimationDelay = Mathf.Max(0f, introAnimationDelay);
        introVisibleDuration = Mathf.Max(0f, introVisibleDuration);
        goAnimationDelay = Mathf.Max(0f, goAnimationDelay);
        goVisibleDuration = Mathf.Max(0f, goVisibleDuration);
        praiseAnimationDelay = Mathf.Max(0f, praiseAnimationDelay);
        praiseVisibleDuration = Mathf.Max(0f, praiseVisibleDuration);
        zoomInDuration = Mathf.Max(0.01f, zoomInDuration);
        settleDuration = Mathf.Max(0f, settleDuration);
        zoomOutDuration = Mathf.Max(0.01f, zoomOutDuration);
        minimumMessageScale = Mathf.Max(0f, minimumMessageScale);
        maximumMessageScale = Mathf.Max(0f, maximumMessageScale);
        FindReferences();
    }
#endif

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

    private void SubscribeEvents()
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.OnGameStateChanged -= HandleGameStateChanged;
        gameManager.OnGameStateChanged += HandleGameStateChanged;

        gameManager.OnPlayingChanged -= HandlePlayingChanged;
        gameManager.OnPlayingChanged += HandlePlayingChanged;

        gameManager.OnLevelChanged -= HandleLevelChanged;
        gameManager.OnLevelChanged += HandleLevelChanged;

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
        if (gameManager == null)
        {
            return;
        }

        gameManager.OnGameStateChanged -= HandleGameStateChanged;
        gameManager.OnPlayingChanged -= HandlePlayingChanged;
        gameManager.OnLevelChanged -= HandleLevelChanged;
        if (scoreController != null)
        {
            scoreController.OnComboChanged -= HandleComboChanged;
            scoreController.OnComboCompleted -= HandleComboCompleted;
        }
    }

    private void InitializeMessage()
    {
        if (goText == null)
        {
            return;
        }

        goTextOriginalScale = goText.rectTransform.localScale;

        if (!string.IsNullOrWhiteSpace(goText.text))
        {
            originalGoTextContent = goText.text;
        }
    }

    private void RefreshState()
    {
        previousComboScore = scoreController != null
            ? scoreController.CurrentComboScore
            : 0;

        if (gameManager == null)
        {
            HideMessageImmediately();
            return;
        }

        HandleGameStateChanged();

        if (isPlayable)
        {
            TryPlayGoAnimation();
        }
    }

    private void HandleGameStateChanged()
    {
        if (gameManager == null) return;

        switch (gameManager.State)
        {
            case GameState.Playing:
                TryPlayLevelIntroAnimation();
                TryPlayGoAnimation();
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
                StopMessageAnimation();
                HideMessageImmediately();
                break;
        }
    }

    private void HandlePlayingChanged()
    {
        if (gameManager == null) return;

        isPlayable = gameManager.EffectivePlayable;
        isSpawnable = gameManager.EffectiveSpawnable;

        if (!isPlayable)
        {
            /*
             * Khi level intro đang chạy, playable vẫn có thể false.
             * Không hủy intro chỉ vì event false.
             */
            if (!isPlayingLevelIntroAnimation)
            {
                StopMessageAnimation();
                HideMessageImmediately();
            }

            return;
        }

        TryPlayGoAnimation();
    }

    private void HandleLevelChanged()
    {
        hasPlayedGoAnimation = false;
        hasPlayedLevelIntroAnimation = false;
        isPlayingLevelIntroAnimation = false;
        previousComboScore = 0;

        StopMessageAnimation();
        HideMessageImmediately();
    }

    private void HandleComboChanged()
    {
        if (scoreController == null) return;

        int currentComboScore = scoreController.CurrentComboScore;
        int requiredComboScore = ScoreController.RequiredComboScore;
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
        ShowCombo();
    }

    private void TryPlayLevelIntroAnimation()
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
            isPlayingLevelIntroAnimation = false;
            TryPlayGoAnimation();
            return;
        }

        isPlayingLevelIntroAnimation = true;

        PlayMessageAnimation(
            config.IntroText,
            introAnimationDelay,
            introVisibleDuration,
            requirePlayerInteraction: false,
            isLevelIntro: true
        );
    }

    private void TryPlayGoAnimation()
    {
        if (gameManager == null ||
            !isPlayable)
        {
            return;
        }

        if (hasPlayedGoAnimation ||
            isPlayingLevelIntroAnimation)
        {
            return;
        }

        hasPlayedGoAnimation = true;

        PlayMessageAnimation(
            originalGoTextContent,
            goAnimationDelay,
            goVisibleDuration
        );
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

        PlayMessageAnimation(
            message,
            praiseAnimationDelay,
            praiseVisibleDuration
        );
    }

    private void ShowCombo()
    {
        if (!CanShowGameplayMessage())
        {
            return;
        }

        PlayMessageAnimation(
            "PERFECT!",
            praiseAnimationDelay,
            praiseVisibleDuration
        );
    }

    private void PlayMessageAnimation(
        string message,
        float delay,
        float visibleDuration,
        bool requirePlayerInteraction = true,
        bool isLevelIntro = false)
    {
        if (goText == null ||
            string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        StopMessageAnimation();

        messageAnimationCoroutine =
            StartCoroutine(
                PlayMessageAnimationRoutine(
                    message,
                    delay,
                    visibleDuration,
                    requirePlayerInteraction,
                    isLevelIntro
                )
            );
    }

    private IEnumerator PlayMessageAnimationRoutine(
        string message,
        float delay,
        float visibleDuration,
        bool requirePlayerInteraction,
        bool isLevelIntro)
    {
        HideMessageImmediately();

        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        if (!CanShowMessage(requirePlayerInteraction))
        {
            FinishAbortedMessage(isLevelIntro);
            yield break;
        }

        goText.text = message;
        SetMessageScale(minimumMessageScale);
        SetMessageAlpha(0f);

        if (!goText.gameObject.activeSelf)
        {
            goText.gameObject.SetActive(true);
        }

        float elapsedTime = 0f;

        while (elapsedTime < zoomInDuration)
        {
            if (!CanShowMessage(requirePlayerInteraction))
            {
                FinishAbortedMessage(isLevelIntro);
                yield break;
            }

            elapsedTime += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(elapsedTime / zoomInDuration);

            float easedProgress =
                EaseOutBack(progress);

            float scale =
                Mathf.LerpUnclamped(
                    minimumMessageScale,
                    maximumMessageScale,
                    easedProgress
                );

            SetMessageScale(scale);
            SetMessageAlpha(progress);

            yield return null;
        }

        SetMessageScale(maximumMessageScale);
        SetMessageAlpha(1f);

        elapsedTime = 0f;

        while (elapsedTime < settleDuration)
        {
            if (!CanShowMessage(requirePlayerInteraction))
            {
                FinishAbortedMessage(isLevelIntro);
                yield break;
            }

            elapsedTime += Time.unscaledDeltaTime;

            float progress =
                settleDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(
                        elapsedTime / settleDuration
                    );

            progress =
                Mathf.SmoothStep(0f, 1f, progress);

            SetMessageScale(
                Mathf.Lerp(
                    maximumMessageScale,
                    1f,
                    progress
                )
            );

            yield return null;
        }

        SetMessageScale(1f);
        SetMessageAlpha(1f);

        elapsedTime = 0f;

        while (elapsedTime < visibleDuration)
        {
            if (!CanShowMessage(requirePlayerInteraction))
            {
                FinishAbortedMessage(isLevelIntro);
                yield break;
            }

            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        elapsedTime = 0f;

        while (elapsedTime < zoomOutDuration)
        {
            if (!CanShowMessage(requirePlayerInteraction))
            {
                FinishAbortedMessage(isLevelIntro);
                yield break;
            }

            elapsedTime += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(elapsedTime / zoomOutDuration);

            progress =
                Mathf.SmoothStep(0f, 1f, progress);

            SetMessageScale(
                Mathf.Lerp(
                    1f,
                    minimumMessageScale,
                    progress
                )
            );

            SetMessageAlpha(1f - progress);

            yield return null;
        }

        HideMessageImmediately();
        messageAnimationCoroutine = null;

        if (isLevelIntro)
        {
            isPlayingLevelIntroAnimation = false;
            TryPlayGoAnimation();
        }
    }

    private void FinishAbortedMessage(bool isLevelIntro)
    {
        HideMessageImmediately();
        messageAnimationCoroutine = null;

        if (isLevelIntro)
        {
            isPlayingLevelIntroAnimation = false;
        }
    }

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

    private static float EaseOutBack(float progress)
    {
        const float overshoot = 1.70158f;

        float adjusted = progress - 1f;

        return
            1f +
            (overshoot + 1f) *
            adjusted *
            adjusted *
            adjusted +
            overshoot *
            adjusted *
            adjusted;
    }

    private void StopMessageAnimation()
    {
        if (messageAnimationCoroutine == null)
        {
            return;
        }

        StopCoroutine(messageAnimationCoroutine);
        messageAnimationCoroutine = null;
    }

    private void ResetAllMessageState()
    {
        StopMessageAnimation();

        hasPlayedGoAnimation = false;
        hasPlayedLevelIntroAnimation = false;
        isPlayingLevelIntroAnimation = false;
        previousComboScore = 0;

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
        goText.text = originalGoTextContent;

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
