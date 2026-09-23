using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ScoreController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;

    public const int RequiredComboScore = 13;

    public event Action OnValidSelection;
    public event Action OnComboChanged;
    public event Action OnComboCompleted;
    public event Action OnComboBounceRequested;

    private int currentComboScore;
    private BallType lastSelectedBallType;
    private bool hasLastSelectedBallType;
    private float comboDecayTimer;
    private ComboBounceRequest currentComboBounceRequest;

    public bool HasLastSelectedBallType => hasLastSelectedBallType;
    public BallType LastSelectedBallType => lastSelectedBallType;
    public ComboBounceRequest CurrentComboBounceRequest => currentComboBounceRequest;

    public int CurrentComboScore => currentComboScore;

    public float ComboDecayInterval
    {
        get
        {
            LevelConfig config =
                gameManager != null
                    ? gameManager.GetCurrentLevelConfig()
                    : null;

            return Mathf.Max(
                0.01f,
                config?.ComboDecayInterval ?? 1f
            );
        }
    }

    public float ComboProgress
    {
        get
        {
            if (currentComboScore <= 0)
            {
                return 0f;
            }

            float visualComboScore = currentComboScore;

            if (gameManager != null &&
                gameManager.CanPlayerInteract())
            {
                float decayProgress =
                    Mathf.Clamp01(
                        comboDecayTimer /
                        ComboDecayInterval
                    );

                visualComboScore -= decayProgress;
            }

            return Mathf.Clamp01(
                Mathf.Max(0f, visualComboScore) /
                RequiredComboScore
            );
        }
    }

    public sealed class ComboBounceRequest
    {
        public BallType BallType { get; }

        public int BouncedBallCount
        {
            get;
            private set;
        }

        public ComboBounceRequest(BallType ballType)
        {
            BallType = ballType;
        }

        public void ReportBouncedBallCount(int count)
        {
            BouncedBallCount = Mathf.Max(0, count);
        }
    }

    [Header("Score Progress")]
    [SerializeField] private Slider scoreProgress;
    [SerializeField] private TMP_Text levelText;

    private float scoreProgressAnimationDuration = 0.35f;

    [Header("Combo Progress")]
    [SerializeField] private Slider comboProgress;
    [SerializeField] private ParticleSystem comboEffect;


    private float completedComboProgressDuration = 2f;


    private float comboProgressAnimationDuration = 0.2f;

    private Coroutine scoreProgressAnimationCoroutine;
    private Coroutine comboProgressAnimationCoroutine;
    private Coroutine completedComboProgressCoroutine;

    private int previousComboScore;

    private void Awake()
    {
        FindReferences();
        InitializeScoreProgress();
        InitializeComboProgress();
    }

    private void OnEnable()
    {
        FindReferences();
        SubscribeEvents();
        RefreshAll();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
        StopAllAnimations();
    }

    private void Update()
    {
        UpdateComboDecay();
        UpdateComboProgressDecay();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        scoreProgressAnimationDuration =
            Mathf.Max(
                0.01f,
                scoreProgressAnimationDuration
            );

        completedComboProgressDuration =
            Mathf.Max(
                0f,
                completedComboProgressDuration
            );

        comboProgressAnimationDuration =
            Mathf.Max(
                0.01f,
                comboProgressAnimationDuration
            );

        FindReferences();
    }
#endif

    private void FindReferences()
    {
        if (gameManager == null)
        {
            gameManager =
                FindFirstObjectByType<GameManager>();
        }
    }

    private void SubscribeEvents()
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.OnGameStateChanged -=
            HandleGameStateChanged;
        gameManager.OnGameStateChanged +=
            HandleGameStateChanged;

        gameManager.OnLevelChanged -=
            HandleLevelChanged;
        gameManager.OnLevelChanged +=
            HandleLevelChanged;

        gameManager.OnScoreChanged -=
            HandleScoreChanged;
        gameManager.OnScoreChanged +=
            HandleScoreChanged;
    }

    private void UnsubscribeEvents()
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.OnGameStateChanged -=
            HandleGameStateChanged;

        gameManager.OnLevelChanged -=
            HandleLevelChanged;

        gameManager.OnScoreChanged -=
            HandleScoreChanged;
    }

    private void InitializeScoreProgress()
    {
        if (scoreProgress == null)
        {
            return;
        }

        scoreProgress.minValue = 0f;
        scoreProgress.maxValue = 1f;
        scoreProgress.value = 0f;
        scoreProgress.wholeNumbers = false;
        scoreProgress.interactable = false;
    }

    private void InitializeComboProgress()
    {
        if (comboProgress == null)
        {
            return;
        }

        comboProgress.minValue = 0f;
        comboProgress.maxValue = 1f;
        comboProgress.value = 0f;
        comboProgress.wholeNumbers = false;
        comboProgress.interactable = false;
    }

    private void RefreshAll()
    {
        if (gameManager == null)
        {
            UpdateLevelText(1);
            SetScoreImmediately(0, 1);
            previousComboScore = 0;
            SetComboImmediately(0f);
            return;
        }

        UpdateLevelText(
            gameManager.CurrentLevel
        );

        SetScoreImmediately(
            gameManager.CurrentScore,
            gameManager.ScoreToPass
        );

        previousComboScore =
            CurrentComboScore;

        SetComboImmediately(
            ComboProgress
        );
    }

    private void HandleGameStateChanged()
    {
        if (gameManager == null)
        {
            return;
        }

        GameState state = gameManager.State;
        if (state == GameState.Initializing ||
            state == GameState.Ready)
        {
            ResetComboInternal(
                notify: true
            );
        }

        switch (state)
        {
            case GameState.Ready:
            case GameState.Initializing:
                StopComboCompletionPresentation();

                if (gameManager != null)
                {
                    previousComboScore =
                        CurrentComboScore;

                    UpdateLevelText(
                        gameManager.CurrentLevel
                    );

                    SetScoreImmediately(
                        gameManager.CurrentScore,
                        gameManager.ScoreToPass
                    );
                }

                break;

            case GameState.LevelCompleted:
            case GameState.GameCompleted:
            case GameState.GameOver:
                /*
                 * Không reset ScoreProgress.
                 * Chỉ dừng presentation Combo.
                 */
                StopComboCompletionPresentation();
                break;
        }
    }

    private void HandleLevelChanged()
    {
        int level = gameManager != null
            ? gameManager.CurrentLevel
            : 1;
        ResetComboInternal(
            notify: true
        );

        UpdateLevelText(level);

        StopComboCompletionPresentation();

        previousComboScore = 0;

        SetComboImmediately(0f);

        if (gameManager != null)
        {
            SetScoreImmediately(
                gameManager.CurrentScore,
                gameManager.ScoreToPass
            );
        }
        else
        {
            SetScoreImmediately(0, 1);
        }
    }

    private void UpdateLevelText(int level)
    {
        if (levelText == null)
        {
            return;
        }

        levelText.text =
            $"LEVEL {level}";
    }

    private void HandleScoreChanged()
    {
        int currentScore = gameManager != null
            ? gameManager.CurrentScore
            : 0;
        int scoreToPass = gameManager != null
            ? gameManager.ScoreToPass
            : 1;
        if (gameManager != null)
        {
            UpdateLevelText(
                gameManager.CurrentLevel
            );
        }

        if (scoreProgress == null)
        {
            return;
        }

        float safeMaximum =
            Mathf.Max(1, scoreToPass);

        scoreProgress.maxValue =
            safeMaximum;

        float targetValue =
            Mathf.Clamp(
                currentScore,
                0,
                safeMaximum
            );

        AnimateScoreProgress(
            targetValue
        );
    }

    private void SetScoreImmediately(
        int currentScore,
        int scoreToPass)
    {
        StopScoreProgressAnimation();

        if (scoreProgress == null)
        {
            return;
        }

        float safeMaximum =
            Mathf.Max(1, scoreToPass);

        scoreProgress.maxValue =
            safeMaximum;

        scoreProgress.value =
            Mathf.Clamp(
                currentScore,
                0,
                safeMaximum
            );
    }

    private void AnimateScoreProgress(
        float targetValue)
    {
        if (scoreProgress == null)
        {
            return;
        }

        StopScoreProgressAnimation();

        scoreProgressAnimationCoroutine =
            StartCoroutine(
                AnimateScoreProgressRoutine(
                    targetValue
                )
            );
    }

    private IEnumerator
        AnimateScoreProgressRoutine(
            float targetValue)
    {
        float startValue =
            scoreProgress.value;

        float duration =
            Mathf.Max(
                0.01f,
                scoreProgressAnimationDuration
            );

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsedTime /
                    duration
                );

            progress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            scoreProgress.value =
                Mathf.Lerp(
                    startValue,
                    targetValue,
                    progress
                );

            yield return null;
        }

        scoreProgress.value =
            targetValue;

        scoreProgressAnimationCoroutine =
            null;
    }

    private void StopScoreProgressAnimation()
    {
        if (scoreProgressAnimationCoroutine ==
            null)
        {
            return;
        }

        StopCoroutine(
            scoreProgressAnimationCoroutine
        );

        scoreProgressAnimationCoroutine =
            null;
    }

    private void HandleComboChanged(
        int currentComboScore,
        int requiredComboScore)
    {
        bool comboIncreased =
            currentComboScore >
            previousComboScore;

        previousComboScore =
            currentComboScore;

        if (currentComboScore <= 0 &&
            completedComboProgressCoroutine !=
            null)
        {
            return;
        }

        if (currentComboScore > 0 &&
            completedComboProgressCoroutine !=
            null)
        {
            CancelCompletedComboProgressCoroutine();
        }

        if (requiredComboScore <= 0)
        {
            AnimateComboProgress(0f);
            return;
        }

        float targetValue =
            Mathf.Clamp01(
                (float)currentComboScore /
                requiredComboScore
            );

        if (comboIncreased)
        {
            AnimateComboProgress(
                targetValue
            );

            return;
        }

        if (currentComboScore <= 0)
        {
            AnimateComboProgress(0f);
        }
    }

    private void UpdateComboProgressDecay()
    {
        if (comboProgress == null ||
            gameManager == null)
        {
            return;
        }

        if (completedComboProgressCoroutine !=
                null ||
            comboProgressAnimationCoroutine !=
                null)
        {
            return;
        }

        comboProgress.value =
            Mathf.Clamp01(
                ComboProgress
            );
    }

    private void HandleComboCompleted(
        BallType ballType)
    {
        StopComboProgressAnimation();

        CancelCompletedComboProgressCoroutine();

        if (comboProgress != null)
        {
            comboProgress.value = 1f;
        }

        if (comboEffect != null)
        {
            comboEffect.Stop(
                true,
                ParticleSystemStopBehavior
                    .StopEmittingAndClear
            );

            comboEffect.Play();
        }

        completedComboProgressCoroutine =
            StartCoroutine(
                CompletedComboProgressRoutine()
            );
    }

    private IEnumerator
        CompletedComboProgressRoutine()
    {
        float duration =
            Mathf.Max(
                0f,
                completedComboProgressDuration
            );

        if (duration > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    duration
                );
        }

        completedComboProgressCoroutine =
            null;

        /*
         * Giữ đúng cơ chế hiện tại:
         * sau thời gian hold 100%,
         * ComboProgress về 0 ngay lập tức,
         * không chạy ngược Handle.
         */
        if (comboProgress != null)
        {
            comboProgress.value = 0f;
        }
    }

    private void AnimateComboProgress(
        float targetValue)
    {
        if (comboProgress == null)
        {
            return;
        }

        StopComboProgressAnimation();

        comboProgressAnimationCoroutine =
            StartCoroutine(
                AnimateComboProgressRoutine(
                    Mathf.Clamp01(
                        targetValue
                    )
                )
            );
    }

    private IEnumerator
        AnimateComboProgressRoutine(
            float targetValue)
    {
        float startValue =
            comboProgress.value;

        float duration =
            Mathf.Max(
                0.01f,
                comboProgressAnimationDuration
            );

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsedTime /
                    duration
                );

            progress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            comboProgress.value =
                Mathf.Lerp(
                    startValue,
                    targetValue,
                    progress
                );

            yield return null;
        }

        comboProgress.value =
            targetValue;

        comboProgressAnimationCoroutine =
            null;
    }

    private void SetComboImmediately(
        float value)
    {
        StopComboProgressAnimation();

        CancelCompletedComboProgressCoroutine();

        if (comboProgress == null)
        {
            return;
        }

        comboProgress.value =
            Mathf.Clamp01(value);
    }

    private void StopComboProgressAnimation()
    {
        if (comboProgressAnimationCoroutine ==
            null)
        {
            return;
        }

        StopCoroutine(
            comboProgressAnimationCoroutine
        );

        comboProgressAnimationCoroutine =
            null;
    }

    private void
        CancelCompletedComboProgressCoroutine()
    {
        if (completedComboProgressCoroutine ==
            null)
        {
            return;
        }

        StopCoroutine(
            completedComboProgressCoroutine
        );

        completedComboProgressCoroutine =
            null;
    }

    private void
        StopComboCompletionPresentation()
    {
        StopComboProgressAnimation();

        CancelCompletedComboProgressCoroutine();

        if (comboProgress != null)
        {
            comboProgress.value = 0f;
        }

        if (comboEffect != null)
        {
            comboEffect.Stop(
                true,
                ParticleSystemStopBehavior
                    .StopEmittingAndClear
            );
        }
    }

    private void StopAllAnimations()
    {
        StopScoreProgressAnimation();
        StopComboCompletionPresentation();
    }

    /*
     * ========================================
     * COMBO GAMEPLAY
     * ========================================
     */

    public void RegisterValidSelection(
        BallType ballType,
        int selectedBallCount)
    {
        if (selectedBallCount < 3 ||
            gameManager == null ||
            !gameManager.CanPlayerInteract())
        {
            return;
        }

        OnValidSelection?.Invoke();

        int earnedComboScore =
            CalculateComboScore(
                selectedBallCount
            );

        if (earnedComboScore <= 0)
        {
            return;
        }

        lastSelectedBallType = ballType;
        hasLastSelectedBallType = true;

        currentComboScore +=
            earnedComboScore;

        comboDecayTimer = 0f;

        NotifyComboChanged();

        if (currentComboScore >=
            RequiredComboScore)
        {
            CompleteCurrentCombo();
        }
    }

    public void RegisterInvalidSelection()
    {
    }

    public void ResetCombo()
    {
        ResetComboInternal(
            notify: true
        );
    }

    private void UpdateComboDecay()
    {
        if (gameManager == null ||
            !gameManager.CanPlayerInteract() ||
            currentComboScore <= 0)
        {
            return;
        }

        comboDecayTimer += Time.deltaTime;

        while (comboDecayTimer >=
               ComboDecayInterval &&
               currentComboScore > 0)
        {
            comboDecayTimer -=
                ComboDecayInterval;

            currentComboScore--;

            if (currentComboScore <= 0)
            {
                currentComboScore = 0;
                comboDecayTimer = 0f;
                lastSelectedBallType = default;
                hasLastSelectedBallType = false;
            }

            NotifyComboChanged();
        }
    }

    private void CompleteCurrentCombo()
    {
        if (!hasLastSelectedBallType ||
            gameManager == null)
        {
            return;
        }

        BallType completedBallType =
            lastSelectedBallType;

        int comboScore =
            currentComboScore;

        HandleComboCompleted(
            completedBallType
        );

        OnComboCompleted?.Invoke();

        ComboBounceRequest request =
            new(
                completedBallType
            );

        currentComboBounceRequest = request;
        OnComboBounceRequested?.Invoke();
        currentComboBounceRequest = null;

        int earnedLevelScore =
            comboScore +
            request.BouncedBallCount;

        /*
         * ScoreController chỉ tính điểm.
         * GameManager là nơi duy nhất cập nhật
         * CurrentScore / TotalScore.
         *
         * GameManager.AddScore() sẽ phát
         * OnScoreChanged và ScoreController
         * cập nhật Score UI từ event đó.
         */
        gameManager.AddScore(
            earnedLevelScore
        );

        ResetComboInternal(
            notify: true
        );
    }

    private static int CalculateComboScore(
        int selectedBallCount)
    {
        return selectedBallCount >= 3
            ? selectedBallCount
            : 0;
    }

    private void ResetComboInternal(
        bool notify)
    {
        currentComboScore = 0;
        lastSelectedBallType = default;
        hasLastSelectedBallType = false;
        comboDecayTimer = 0f;

        if (notify)
        {
            NotifyComboChanged();
        }
    }

    private void NotifyComboChanged()
    {
        HandleComboChanged(
            currentComboScore,
            RequiredComboScore
        );

        OnComboChanged?.Invoke();
    }

}
