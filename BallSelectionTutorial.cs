using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BallSelectionTutorial : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private BallSpawner ballSpawner;
    [SerializeField] private BallController ballController;

    [Header("Hint")]
    [SerializeField] private GameObject hintPrefab;
    [SerializeField] private Vector3 hintOffset = Vector3.zero;

    private readonly List<Ball> validGroup = new(5);
    private Ball hintedCenter;
    private GameObject hintInstance;
    private bool completed;
    private bool waitingForPhase2;
    private Vector3 hintBaseScale;
    private float pulseElapsed;

    [Header("Press Animation")]
    [SerializeField, Min(1f)] private float pulseScale = 1.2f;
    [SerializeField, Min(0.05f)] private float pulseDuration = 0.6f;

    private void Awake()
    {
        FindReferences();
    }

    private void OnEnable()
    {
        FindReferences();

        if (gameManager != null)
        {
            gameManager.OnGameStateChanged += HandleGameStateChanged;
            gameManager.OnPhaseChanged += HandlePhaseChanged;
        }

        if (ballSpawner != null)
            ballSpawner.OnBallSpawned += HandleBallSpawned;

        if (ballController != null)
            ballController.OnValidGroupSelected += HandleValidSelection;

        SyncState();
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.OnGameStateChanged -= HandleGameStateChanged;
            gameManager.OnPhaseChanged -= HandlePhaseChanged;
        }

        if (ballSpawner != null)
            ballSpawner.OnBallSpawned -= HandleBallSpawned;

        if (ballController != null)
            ballController.OnValidGroupSelected -= HandleValidSelection;

        ClearHint();
    }

    private void Update()
    {
        if (hintInstance == null)
            return;

        // Hint selection and removal are handled by spawn/state events only.
        // Update is dedicated to the press animation.
        if (gameManager == null || gameManager.State != GameState.Playing)
            return;

        // Repeating press gesture: grow smoothly, then return to original size.
        pulseElapsed += Time.deltaTime;
        float duration = Mathf.Max(0.05f, pulseDuration);
        float phase = Mathf.Repeat(pulseElapsed / duration, 1f);
        float triangle = 1f - Mathf.Abs(phase * 2f - 1f);
        float eased = triangle * triangle * (3f - 2f * triangle);
        hintInstance.transform.localScale =
            hintBaseScale * Mathf.Lerp(1f, Mathf.Max(1f, pulseScale), eased);
    }

    private void FindReferences()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        if (ballSpawner == null)
            ballSpawner = FindFirstObjectByType<BallSpawner>();

        if (ballController == null)
            ballController = FindFirstObjectByType<BallController>();
    }

    private void HandleGameStateChanged()
    {
        if (gameManager == null)
            return;

        // Every new Level 1 run gets its own tutorial.
        if (gameManager.State == GameState.Initializing ||
            gameManager.State == GameState.Ready)
        {
            completed = false;
            waitingForPhase2 = false;
            ClearHint();
            return;
        }

        if (gameManager.State != GameState.Playing &&
            gameManager.State != GameState.Paused)
            ClearHint();

        SyncState();
    }

    private void HandlePhaseChanged()
    {
        SyncState();
    }

    private void SyncState()
    {
        if (gameManager == null || completed ||
            gameManager.CurrentLevel != 1 ||
            gameManager.State != GameState.Playing)
        {
            waitingForPhase2 = false;
            ClearHint();
            return;
        }

        // Phase 2 is entered for the first time after Phase 1.
        if (gameManager.CurrentPhase == GamePhase.Phase2)
            waitingForPhase2 = true;

        // Once armed, remain active until the first valid selection.
    }

    private void HandleBallSpawned()
    {
        if (!waitingForPhase2 || completed ||
            gameManager == null ||
            gameManager.CurrentLevel != 1 ||
            gameManager.State != GameState.Playing ||
            gameManager.CurrentPhase != GamePhase.Phase2)
            return;

        // Once a pointer is shown, keep it until the tutorial ends
        // or another lifecycle event clears it. Do not revalidate on spawn.
        if (hintInstance != null)
            return;

        if (hintPrefab == null)
            return;

        IReadOnlyList<Ball> balls = gameManager.Balls;
        for (int i = 0; i < balls.Count; i++)
        {
            Ball candidate = balls[i];
            if (!BallGroupFinder.TryGetValidGroup(candidate, balls, validGroup))
                continue;

            hintedCenter = candidate;
            // Parent the hint to the center ball so it follows its movement.
            // Preserve the prefab's intended world orientation and offset.
            hintInstance = Instantiate(
                hintPrefab,
                candidate.transform.position + hintOffset,
                Quaternion.identity,
                candidate.transform);

            hintBaseScale = hintInstance.transform.localScale;
            pulseElapsed = 0f;
            return;
        }
    }

    private void HandleValidSelection()
    {
        if (!waitingForPhase2 || completed ||
            gameManager == null || gameManager.CurrentLevel != 1)
            return;

        completed = true;
        waitingForPhase2 = false;
        ClearHint();
    }

    private void ClearHint()
    {
        hintedCenter = null;
        validGroup.Clear();

        if (hintInstance != null)
            Destroy(hintInstance);

        hintInstance = null;
        pulseElapsed = 0f;
    }
}
