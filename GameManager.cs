using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class GameManager : MonoBehaviour
{
    /*
     * ========================================
     * LEVEL CONFIG
     * ========================================
     */
    private static readonly LevelConfig[] LevelConfigs =
    {
        // Level 1
        new LevelConfig(
            scoreToPass: 150,
            ballTypes: new[]
            {
                BallType.RedBall,
                BallType.GreenBall,
                BallType.BlueBall
            },
            comboDecayInterval: 1f,
            introText: "READY?"
        ),
        // Level 2
        new LevelConfig(
            scoreToPass: 150,
            ballTypes: new[]
            {
                BallType.RedBall,
                BallType.GreenBall,
                BallType.BlueBall,
                BallType.YellowBall
            },
            comboDecayInterval: 1f,
            introText: "READY?"
        ),
        // Level 3
        new LevelConfig(
            scoreToPass: 150,
            ballTypes: new[]
            {
                BallType.RedBall,
                BallType.GreenBall,
                BallType.BlueBall,
                BallType.YellowBall,
                BallType.PurpleBall
            },
            comboDecayInterval: 1f,
            introText: "READY?"
        ),
        // Level 4
        new LevelConfig(
            scoreToPass: 150,
            ballTypes: new[]
            {
                BallType.RedBall,
                BallType.GreenBall,
                BallType.BlueBall,
            },
            comboDecayInterval: 0.8f,
            introText: "FASTER!"
        ),
        // Level 5
        new LevelConfig(
            scoreToPass: 150,
            ballTypes: new[]
            {
                BallType.RedBall,
                BallType.GreenBall,
                BallType.BlueBall,
                BallType.YellowBall,
            },
            comboDecayInterval: 0.8f,
            introText: "READY?"
        ),
        // Level 6
        new LevelConfig(
            scoreToPass: 150,
            ballTypes: new[]
            {
                BallType.RedBall,
                BallType.GreenBall,
                BallType.BlueBall,
                BallType.YellowBall,
                BallType.PurpleBall,
            },
            comboDecayInterval: 0.8f,
            introText: "READY?"
        ),
        // Level 7
        new LevelConfig(
            scoreToPass: 150,
            ballTypes: new[]
            {
                BallType.RedBall,
                BallType.GreenBall,
                BallType.BlueBall,
            },
            comboDecayInterval: 0.6f,
            introText: "MAX SPEED!"
        ),
        // Level 8
        new LevelConfig(
            scoreToPass: 150,
            ballTypes: new[]
            {
                BallType.RedBall,
                BallType.GreenBall,
                BallType.BlueBall,
                BallType.YellowBall,
            },
            comboDecayInterval: 0.6f,
            introText: "READY?"
        ),
        // Level 9
        new LevelConfig(
            scoreToPass: 150,
            ballTypes: new[]
            {
                BallType.RedBall,
                BallType.GreenBall,
                BallType.BlueBall,
                BallType.YellowBall,
                BallType.PurpleBall,
            },
            comboDecayInterval: 0.6f,
            introText: "FINAL LEVEL!"
        ),
    };

    /*
     * ========================================
     * EDITOR TEST
     * ========================================
     */
#if UNITY_EDITOR
    public enum EditorTestMode
    {
        Normal,
        LevelCompleted,
        GameCompleted
    }

    [HideInInspector]
    [SerializeField]
    private EditorTestMode editorTestMode =
        EditorTestMode.Normal;

    public EditorTestMode CurrentEditorTestMode =>
        editorTestMode;

    public void SetEditorTestMode(
        EditorTestMode testMode)
    {
        if (editorTestMode == testMode)
        {
            return;
        }

        editorTestMode = testMode;

        if (!Application.isPlaying)
        {
            return;
        }

        switch (editorTestMode)
        {
            case EditorTestMode.LevelCompleted:
                CompleteLevel();
                break;

            case EditorTestMode.GameCompleted:
                CompleteGame();
                break;
        }
    }
#endif

    /*
     * ========================================
     * GAME EVENTS
     * ========================================
     */
    public event Action
        OnGameStateChanged;
    public event Action
        OnGameOverStarted;
    public event Action
        OnPlayingChanged;
    public event Action
        OnPhaseChanged;
    public event Action
        OnBallCountChanged;

    /*
     * ========================================
     * LEVEL EVENTS
     * ========================================
     */
    public event Action
        OnLevelChanged;
    public event Action
        OnScoreChanged;

    /*
     * ========================================
     * GAME STATE
     * ========================================
     */
    public GameState State
    {
        get;
        private set;
    } = GameState.None;

    public bool IsPlayable
    {
        get;
        private set;
    }

    public bool IsSpawnable
    {
        get;
        private set;
    }

    /*
     * ========================================
     * GAME OVER
     * ========================================
     */
    private const float GameOverDelay = 3f;
    private Coroutine gameOverCoroutine;

    /*
     * ========================================
     * LEVEL
     * ========================================
     */
    private int currentLevel = 1;

    public int CurrentLevel =>
        currentLevel;

    public int Level =>
        currentLevel;

    public int LevelCount =>
        LevelConfigs.Length;

    public bool IsLastLevel =>
        currentLevel >= LevelCount;

    /*
     * ========================================
     * SCORE
     * ========================================
     */
    public int CurrentScore
    {
        get;
        private set;
    }

    public int LevelScore =>
        CurrentScore;

    public int TotalScore
    {
        get;
        private set;
    }

    public int ScoreToPass
    {
        get
        {
            LevelConfig config =
                GetCurrentLevelConfig();

            return
                config?.ScoreToPass ?? 0;
        }
    }

    /*
     * ========================================
     * BALL
     * ========================================
     */
    private int maxBallCount = 60;
    private float phase1EndRatio = 0.6f;
    private float phase2EndRatio = 0.8f;

    private readonly List<Ball> balls =
        new(64);

    public IReadOnlyList<Ball> Balls =>
        balls;

    public int BallCount
    {
        get
        {
            RemoveNullBalls();
            return balls.Count;
        }
    }

    public int ActiveBallCount
    {
        get
        {
            RemoveNullBalls();
            return balls.Count;
        }
    }

    public int MaxBallCount =>
        maxBallCount;

    public float BallRatio =>
        maxBallCount > 0
            ? (float)ActiveBallCount / maxBallCount
            : 0f;

    public bool EffectivePlayable =>
        State == GameState.Playing && IsPlayable;

    public bool EffectiveSpawnable =>
        State == GameState.Playing && IsSpawnable;

    public GamePhase CurrentPhase
    {
        get;
        private set;
    } = GamePhase.Phase1;

    public bool ContainsBall(
        Ball ball)
    {
        return
            ball != null &&
            balls.Contains(ball);
    }

    public void RegisterBall(
        Ball ball)
    {
        if (ball == null ||
            balls.Contains(ball))
        {
            return;
        }

        balls.Add(ball);
        NotifyBallCountChanged();
        RefreshPhase();

        if (State == GameState.Playing &&
            IsSpawnable &&
            ActiveBallCount >= maxBallCount)
        {
            GameOver();
        }
    }

    public void UnregisterBall(
        Ball ball)
    {
        if (ball == null ||
            !balls.Remove(ball))
        {
            return;
        }

        NotifyBallCountChanged();
        RefreshPhase();
    }

    public void RemoveNullBalls()
    {
        bool changed = false;

        for (int i = balls.Count - 1;
             i >= 0;
             i--)
        {
            if (balls[i] != null)
            {
                continue;
            }

            balls.RemoveAt(i);
            changed = true;
        }

        if (changed)
        {
            NotifyBallCountChanged();
            RefreshPhase();
        }
    }

    /*
     * ========================================
     * PHASE
     * ========================================
     */
    private void RefreshPhase()
    {
        GamePhase newPhase =
            CalculatePhase();

        if (CurrentPhase == newPhase)
        {
            return;
        }

        CurrentPhase =
            newPhase;

        OnPhaseChanged?.Invoke();

        if (State == GameState.Playing &&
            !IsPlayable &&
            IsSpawnable &&
            CurrentPhase == GamePhase.Phase2)
        {
            EnablePlayable();
        }
    }

    private GamePhase CalculatePhase()
    {
        float ratio =
            BallRatio;

        if (ratio <= phase1EndRatio)
        {
            return GamePhase.Phase1;
        }

        if (ratio <= phase2EndRatio)
        {
            return GamePhase.Phase2;
        }

        return GamePhase.Phase3;
    }

    private void ResetBallPhase()
    {
        balls.Clear();
        NotifyBallCountChanged();

        if (CurrentPhase ==
            GamePhase.Phase1)
        {
            return;
        }

        CurrentPhase =
            GamePhase.Phase1;

        OnPhaseChanged?.Invoke();
    }

    private void NotifyBallCountChanged()
    {
        OnBallCountChanged?.Invoke();
    }

    /*
     * ========================================
     * UNITY
     * ========================================
     */
    private void Awake()
    {
        ValidateCurrentLevel();
        Screen.sleepTimeout =
            SleepTimeout.NeverSleep;
        NewGame();
    }

    private void OnApplicationFocus(
        bool hasFocus)
    {
        if (!hasFocus)
        {
            PauseGame();
        }
    }

    private void OnApplicationPause(
        bool pauseStatus)
    {
        if (pauseStatus)
        {
            PauseGame();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ValidateCurrentLevel();

        maxBallCount =
            Mathf.Max(
                1,
                maxBallCount
            );

        phase1EndRatio =
            Mathf.Clamp(
                phase1EndRatio,
                0.01f,
                1f
            );

        phase2EndRatio =
            Mathf.Clamp(
                phase2EndRatio,
                phase1EndRatio,
                1f
            );
    }
#endif

    /*
     * ========================================
     * INITIALIZATION
     * ========================================
     */
    public void InitializeGame()
    {
        RestoreTimeScale();
        CancelGameOverRoutine();
        EnterInitializingState();
        ResetCurrentLevel();

        ChangeState(
            GameState.Ready
        );
    }

    public void NewGame()
    {
        RestoreTimeScale();
        CancelGameOverRoutine();
        EnterInitializingState();
        ResetGameProgress();

        ChangeState(
            GameState.Ready
        );
    }

    /*
     * ========================================
     * START GAME
     * ========================================
     */
    public void StartGame()
    {
        if (State != GameState.Ready)
        {
            return;
        }

        StartCurrentLevel();
    }

    private void StartCurrentLevel()
    {
        CancelGameOverRoutine();
        SetPlayable(
            false
        );
        SetSpawnable(
            false
        );

        ChangeState(
            GameState.Playing
        );

        SetSpawnable(
            true
        );
    }

    /*
     * ========================================
     * PLAYABLE
     * ========================================
     */
    public void EnablePlayable()
    {
        if (State != GameState.Playing)
        {
            return;
        }

        SetPlayable(
            true
        );
    }

    public void DisablePlayable()
    {
        SetPlayable(
            false
        );
    }

    public bool CanPlayerInteract()
    {
        return
            State == GameState.Playing &&
            IsPlayable;
    }

    /*
     * ========================================
     * SPAWNABLE
     * ========================================
     */
    public void EnableSpawnable()
    {
        if (State != GameState.Playing)
        {
            return;
        }

        SetSpawnable(
            true
        );
    }

    public void DisableSpawnable()
    {
        SetSpawnable(
            false
        );
    }

    /*
     * ========================================
     * PAUSE
     * ========================================
     */
    public void PauseGame()
    {
        if (State != GameState.Playing)
        {
            return;
        }

        Time.timeScale = 0f;
        ChangeState(
            GameState.Paused
        );
    }

    public void ResumeGame()
    {
        if (State != GameState.Paused)
        {
            return;
        }

        Time.timeScale = 1f;
        ChangeState(
            GameState.Playing
        );
    }

    /*
     * ========================================
     * LEVEL COMPLETED
     * ========================================
     */
    public void CompleteLevel()
    {
        if (!CanPlayerInteract())
        {
            return;
        }

        CancelGameOverRoutine();
        SetPlayable(
            false
        );
        SetSpawnable(
            false
        );

        if (IsLastLevel)
        {
            CompleteGame();
            return;
        }

        ChangeState(
            GameState.LevelCompleted
        );
    }

    public void NextLevel()
    {
        if (State !=
            GameState.LevelCompleted)
        {
            return;
        }

        EnterInitializingState();

        if (!AdvanceToNextLevel())
        {
            CompleteGame();
            return;
        }

        StartCurrentLevel();
    }

    private bool AdvanceToNextLevel()
    {
        if (IsLastLevel)
        {
            return false;
        }

        currentLevel++;
        CurrentScore = 0;

        NotifyLevelChanged();
        NotifyScoreChanged();

        return true;
    }

    /*
     * ========================================
     * GAME COMPLETED
     * ========================================
     */
    public void CompleteGame()
    {
        if (State != GameState.Playing &&
            State != GameState.LevelCompleted)
        {
            return;
        }

        CancelGameOverRoutine();
        SetPlayable(
            false
        );
        SetSpawnable(
            false
        );

        ChangeState(
            GameState.GameCompleted
        );
    }

    /*
     * ========================================
     * GAME OVER
     * ========================================
     */
    public void GameOver()
    {
        if (State != GameState.Playing ||
            gameOverCoroutine != null)
        {
            return;
        }

        SetPlayable(
            false
        );
        SetSpawnable(
            false
        );

        OnGameOverStarted?.Invoke();

        gameOverCoroutine =
            StartCoroutine(
                GameOverRoutine()
            );
    }

    private IEnumerator GameOverRoutine()
    {
        yield return
            new WaitForSeconds(
                GameOverDelay
            );

        gameOverCoroutine = null;

        ChangeState(
            GameState.GameOver
        );
    }

    private void CancelGameOverRoutine()
    {
        if (gameOverCoroutine == null)
        {
            return;
        }

        StopCoroutine(
            gameOverCoroutine
        );
        gameOverCoroutine = null;
    }

    public void LoseGame()
    {
        GameOver();
    }

    /*
     * ========================================
     * RETRY
     * ========================================
     */
    public void RetryLevel()
    {
        if (State != GameState.GameOver)
        {
            return;
        }

        RestoreTimeScale();
        CancelGameOverRoutine();
        EnterInitializingState();
        ResetCurrentLevel();
        StartCurrentLevel();
    }

    /*
     * ========================================
     * RESTART GAME
     * ========================================
     */
    public void RestartGame()
    {
        RestoreTimeScale();
        CancelGameOverRoutine();
        EnterInitializingState();
        ResetGameProgress();
        StartCurrentLevel();
    }

    /*
     * ========================================
     * MAIN MENU
     * ========================================
     */
    public void BackToMainMenu()
    {
        RestoreTimeScale();
        CancelGameOverRoutine();
        EnterInitializingState();
        ResetGameProgress();

        ChangeState(
            GameState.Ready
        );
    }

    /*
     * ========================================
     * GAME PROGRESS
     * ========================================
     */
    public void ResetGameProgress()
    {
        currentLevel = 1;
        CurrentScore = 0;
        TotalScore = 0;

        NotifyAllDataChanged();
    }

    public void ResetCurrentLevel()
    {
        RemoveCurrentLevelScoreFromTotal();
        CurrentScore = 0;

        NotifyScoreChanged();
    }

    /*
     * ========================================
     * LEVEL CONFIG
     * ========================================
     */
    public LevelConfig GetCurrentLevelConfig()
    {
        int index =
            currentLevel - 1;

        if (index < 0 ||
            index >= LevelConfigs.Length)
        {
            return null;
        }

        return
            LevelConfigs[index];
    }

    public BallType GetRandomBallType()
    {
        LevelConfig config =
            GetCurrentLevelConfig();

        if (config == null)
        {
            Debug.LogWarning(
                $"Khong tim thay Level {currentLevel}.",
                this
            );
            return default;
        }

        return
            config.GetRandomBallType();
    }

    /*
     * ========================================
     * SCORE
     * ========================================
     */
    public void AddScore(
        int amount)
    {
        if (amount <= 0 ||
            !CanPlayerInteract())
        {
            return;
        }

        CurrentScore += amount;
        TotalScore += amount;

        NotifyScoreChanged();

        if (CurrentScore >= ScoreToPass)
        {
            CompleteLevel();
        }
    }

    /*
     * ========================================
     * SCORE RESET
     * ========================================
     */
    private void
        RemoveCurrentLevelScoreFromTotal()
    {
        if (CurrentScore <= 0)
        {
            return;
        }

        TotalScore =
            Mathf.Max(
                0,
                TotalScore -
                CurrentScore
            );
    }

    /*
     * ========================================
     * INTERNAL STATE
     * ========================================
     */
    private void EnterInitializingState()
    {
        CancelGameOverRoutine();
        SetPlayable(
            false
        );
        SetSpawnable(
            false
        );

        ChangeState(
            GameState.Initializing
        );
    }

    private void SetPlayable(
        bool playable)
    {
        if (IsPlayable == playable)
        {
            return;
        }

        /*
         * IsPlayable lưu trạng thái gameplay gốc.
         * Pause / Resume không thay đổi giá trị này.
         */
        IsPlayable =
            playable;

        NotifyPlayingChanged();
    }

    private void SetSpawnable(
        bool spawnable)
    {
        if (IsSpawnable == spawnable)
        {
            return;
        }

        /*
         * IsSpawnable lưu trạng thái gameplay gốc.
         * Pause / Resume không thay đổi giá trị này.
         */
        IsSpawnable =
            spawnable;

        NotifyPlayingChanged();
    }

    private void NotifyPlayingChanged()
    {
        OnPlayingChanged?.Invoke();
    }

    private void ChangeState(
        GameState newState)
    {
        if (State == newState)
        {
            return;
        }

        State =
            newState;

        OnGameStateChanged?.Invoke();

        /*
         * State cũng ảnh hưởng trực tiếp đến trạng thái
         * hiệu lực của playable / spawnable.
         *
         * Đặc biệt:
         * Playing -> Paused  : false, false
         * Paused  -> Playing : khôi phục giá trị gốc.
         */
        NotifyPlayingChanged();
    }

    /*
     * ========================================
     * VALIDATION
     * ========================================
     */
    private void ValidateCurrentLevel()
    {
        int levelCount =
            LevelCount;

        if (levelCount <= 0)
        {
            currentLevel = 1;
            return;
        }

        currentLevel =
            Mathf.Clamp(
                currentLevel,
                1,
                levelCount
            );
    }

    /*
     * ========================================
     * EVENTS
     * ========================================
     */
    private void NotifyAllDataChanged()
    {
        NotifyLevelChanged();
        NotifyScoreChanged();
    }

    private void NotifyLevelChanged()
    {
        OnLevelChanged?.Invoke();
    }

    private void NotifyScoreChanged()
    {
        OnScoreChanged?.Invoke();
    }

    /*
     * ========================================
     * TIME
     * ========================================
     */
    private static void RestoreTimeScale()
    {
        if (!Mathf.Approximately(
                Time.timeScale,
                1f))
        {
            Time.timeScale = 1f;
        }
    }
}