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

    [Header("Rounded Lines")]
    [SerializeField, Min(0.001f)] private float lineWidth = 0.035f;
    [SerializeField] private Color lineColor = Color.white;
    [SerializeField, Range(0, 16)] private int capVertices = 8;
    [SerializeField] private string sortingLayerName = "Ball";
    [SerializeField] private int sortingOrder = 32767;

    private Material lineMaterial;

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

        // Do not recheck group geometry every frame: bouncing balls can
        // temporarily invalidate a group and cause the hint to flicker.
        // Group validity is checked only when a new ball spawns.
        if (hintedCenter == null ||
            hintedCenter.IsBouncing ||
            hintedCenter.IsDestroyRequested ||
            gameManager == null ||
            gameManager.CurrentLevel != 1)
        {
            ClearHint();
        }
    }

    private void OnDestroy()
    {
        if (lineMaterial != null)
            Destroy(lineMaterial);
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

        // Restore the original spawn-driven revalidation. Keep the hint
        // when its center still has a valid group; otherwise replace it.
        if (hintedCenter != null &&
            BallGroupFinder.TryGetValidGroup(
                hintedCenter, gameManager.Balls, validGroup))
            return;

        ClearHint();

        if (hintPrefab == null)
            return;

        IReadOnlyList<Ball> balls = gameManager.Balls;
        for (int i = 0; i < balls.Count; i++)
        {
            Ball candidate = balls[i];
            if (!BallGroupFinder.TryGetValidGroup(candidate, balls, validGroup))
                continue;

            hintedCenter = candidate;
            hintInstance = Instantiate(
                hintPrefab,
                candidate.transform.position + hintOffset,
                Quaternion.identity);

            CreateRoundedLines();
            return;
        }
    }

    private void CreateRoundedLines()
    {
        if (hintInstance == null || hintedCenter == null)
            return;

        if (lineMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                Debug.LogWarning("Sprites/Default shader not found for tutorial lines.", this);
                return;
            }

            lineMaterial = new Material(shader)
            {
                name = "TutorialRoundedLineMaterial"
            };
        }

        Vector3 center = hintedCenter.transform.position;
        Matrix4x4 toLocal = hintInstance.transform.worldToLocalMatrix;
        int lineIndex = 0;

        GameObject linesRoot = new GameObject("RoundedLines");
        linesRoot.transform.SetParent(hintInstance.transform, false);

        for (int i = 0; i < validGroup.Count; i++)
        {
            Ball ball = validGroup[i];
            if (ball == null || ball == hintedCenter)
                continue;

            Vector3 target = ball.transform.position;
            if (Vector2.Distance(center, target) <= 0.0001f)
                continue;

            GameObject child = new GameObject("RoundedLine_" + (++lineIndex));
            child.transform.SetParent(linesRoot.transform, false);

            LineRenderer line = child.AddComponent<LineRenderer>();
            line.sharedMaterial = lineMaterial;
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.SetPosition(0, toLocal.MultiplyPoint3x4(center));
            line.SetPosition(1, toLocal.MultiplyPoint3x4(target));
            line.startWidth = Mathf.Max(0.001f, lineWidth);
            line.endWidth = Mathf.Max(0.001f, lineWidth);
            line.startColor = lineColor;
            line.endColor = lineColor;
            line.numCapVertices = capVertices;
            line.numCornerVertices = 0;
            line.textureMode = LineTextureMode.Stretch;
            line.alignment = LineAlignment.TransformZ;
            line.sortingLayerName = sortingLayerName;
            line.sortingOrder = sortingOrder;
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
    }
}
