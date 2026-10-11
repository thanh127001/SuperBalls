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

    private Material lineMaterial;
    private Texture2D dotTexture;

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
        if (lineMaterial != null)
            Destroy(lineMaterial);
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
        if (hintInstance == null || hintedCenter == null)
            return;

        if (!EnsureDotMaterial())
            return;

        Vector3 center = hintedCenter.transform.position;
        Matrix4x4 toLocal = hintInstance.transform.worldToLocalMatrix;
        float diameter = Mathf.Max(0.001f, dotDiameter);
        float spacing = Mathf.Max(diameter, dotSpacing);
        int lineIndex = 0;

        for (int i = 0; i < validGroup.Count; i++)
        {
            Ball ball = validGroup[i];
            if (ball == null || ball == hintedCenter)
                continue;

            Vector3 target = ball.transform.position;
            float length = Vector2.Distance(center, target);
            if (length <= 0.0001f)
                continue;

            GameObject child = new GameObject("DottedLine_" + (++lineIndex));
            child.transform.SetParent(hintInstance.transform, false);

            LineRenderer line = child.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.SetPosition(0, toLocal.MultiplyPoint3x4(center));
            line.SetPosition(1, toLocal.MultiplyPoint3x4(target));
            line.startWidth = diameter;
            line.endWidth = diameter;
            line.startColor = dotColor;
            line.endColor = dotColor;
            line.alignment = LineAlignment.TransformZ;
            line.textureMode = LineTextureMode.Tile;
            // Each repeat is one round dot and its transparent gap.
            line.textureScale = new Vector2(length / spacing, 1f);
            line.numCapVertices = 0;
            line.numCornerVertices = 0;
            line.sharedMaterial = lineMaterial;
            line.sortingLayerName = sortingLayerName;
            line.sortingOrder = sortingOrder;
        }
    }

    private bool EnsureDotMaterial()
    {
        if (lineMaterial != null)
            return true;

        // Sprites/Default supports alpha blending and LineRenderer vertex colors.
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Debug.LogWarning("Sprites/Default shader not found for tutorial dots.", this);
            return false;
        }

        const int size = 64;
        dotTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "TutorialRoundDotTexture",
            wrapMode = TextureWrapMode.Repeat,
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

        lineMaterial = new Material(shader)
        {
            name = "TutorialRoundDotMaterial",
            mainTexture = dotTexture
        };
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
