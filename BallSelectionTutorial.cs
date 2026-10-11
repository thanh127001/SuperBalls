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

    [Header("Dotted Lines")]
    [SerializeField, Min(0.001f)] private float dotDiameter = 0.035f;
    [SerializeField, Min(0.001f)] private float dotSpacing = 0.10f;
    [SerializeField] private Color dotColor = Color.white;
    [SerializeField] private string sortingLayerName = "Ball";
    [SerializeField] private int sortingOrder = 32767;

    private Texture2D dotTexture;
    private Sprite dotSprite;

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
        if (hintInstance == null || gameManager == null ||
            gameManager.State != GameState.Playing)
            return;

        if (hintedCenter == null ||
            !BallGroupFinder.TryGetValidGroup(
                hintedCenter, gameManager.Balls, validGroup))
        {
            // Keep the lines static; remove the entire hint when the group breaks.
            // The next Phase 2 spawn may create a replacement.
            ClearHint();
        }
    }

    private void OnDestroy()
    {
        if (dotSprite != null)
            Destroy(dotSprite);
        if (dotTexture != null)
            Destroy(dotTexture);
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

        if (hintInstance != null)
        {
            if (hintedCenter == null ||
                !BallGroupFinder.TryGetValidGroup(
                    hintedCenter, gameManager.Balls, validGroup))
                ClearHint();

            // Do not replace a broken hint on this same spawn.
            return;
        }

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

            CreateDottedLines();
            return;
        }
    }

    private void CreateDottedLines()
    {
        if (hintInstance == null || hintedCenter == null || !EnsureDotSprite())
            return;

        Vector3 center = hintedCenter.transform.position;
        float diameter = Mathf.Max(0.001f, dotDiameter);
        float spacing = Mathf.Max(diameter, dotSpacing);
        int lineIndex = 0;

        GameObject linesRoot = new GameObject("DottedLines");
        linesRoot.transform.SetParent(hintInstance.transform, false);

        for (int i = 0; i < validGroup.Count; i++)
        {
            Ball ball = validGroup[i];
            if (ball == null || ball == hintedCenter)
                continue;

            Vector3 target = ball.transform.position;
            Vector3 delta = target - center;
            delta.z = 0f;
            float length = delta.magnitude;
            if (length <= 0.0001f)
                continue;

            Vector3 direction = delta / length;
            GameObject lineRoot = new GameObject("DottedLine_" + (++lineIndex));
            lineRoot.transform.SetParent(linesRoot.transform, false);

            // Keep a constant diameter and distribute dots evenly along the line.
            int intervals = Mathf.Max(1, Mathf.RoundToInt(length / spacing));
            for (int j = 0; j <= intervals; j++)
            {
                float distance = length * j / intervals;
                Vector3 worldPosition = center + direction * distance;
                GameObject dot = new GameObject("Dot_" + j);
                dot.transform.SetParent(lineRoot.transform, false);
                dot.transform.position = worldPosition;
                dot.transform.localScale = Vector3.one * diameter;

                SpriteRenderer renderer = dot.AddComponent<SpriteRenderer>();
                renderer.sprite = dotSprite;
                renderer.color = dotColor;
                renderer.sortingLayerName = sortingLayerName;
                renderer.sortingOrder = sortingOrder;
            }
        }
    }

    private bool EnsureDotSprite()
    {
        if (dotSprite != null)
            return true;

        const int size = 64;
        dotTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "TutorialRoundDotTexture",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Color[] pixels = new Color[size * size];
        float radius = (size - 2) * 0.5f;
        float center = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(radius - distance);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        dotTexture.SetPixels(pixels);
        dotTexture.Apply(false, true);
        dotSprite = Sprite.Create(
            dotTexture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size);
        dotSprite.name = "TutorialRoundDotSprite";
        return true;
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
