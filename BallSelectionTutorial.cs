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

    [Header("Dashed Lines")]
    [SerializeField, Min(0.001f)] private float lineWidth = 0.035f;
    [SerializeField, Min(0.001f)] private float dashLength = 0.08f;
    [SerializeField, Min(0f)] private float gapLength = 0.05f;
    [SerializeField] private Color lineColor = Color.white;
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

            CreateDashedLines();
            return;
        }
    }

    private void CreateDashedLines()
    {
        if (hintInstance == null || hintedCenter == null)
            return;

        List<Vector3> vertices = new(128);
        List<int> triangles = new(192);
        float dash = Mathf.Max(0.001f, dashLength);
        float step = dash + Mathf.Max(0f, gapLength);
        float halfWidth = Mathf.Max(0.001f, lineWidth) * 0.5f;
        Vector3 center = hintedCenter.transform.position;
        Matrix4x4 worldToLocal = hintInstance.transform.worldToLocalMatrix;

        for (int i = 0; i < validGroup.Count; i++)
        {
            Ball ball = validGroup[i];
            if (ball == null || ball == hintedCenter)
                continue;

            Vector3 delta = ball.transform.position - center;
            delta.z = 0f;
            float length = delta.magnitude;
            if (length <= 0.0001f)
                continue;

            Vector3 direction = delta / length;
            Vector3 side = new Vector3(-direction.y, direction.x, 0f) * halfWidth;

            for (float distance = 0f; distance < length; distance += step)
            {
                Vector3 p0 = center + direction * distance;
                Vector3 p1 = center + direction * Mathf.Min(distance + dash, length);
                int index = vertices.Count;
                vertices.Add(worldToLocal.MultiplyPoint3x4(p0 - side));
                vertices.Add(worldToLocal.MultiplyPoint3x4(p0 + side));
                vertices.Add(worldToLocal.MultiplyPoint3x4(p1 - side));
                vertices.Add(worldToLocal.MultiplyPoint3x4(p1 + side));
                triangles.Add(index);
                triangles.Add(index + 1);
                triangles.Add(index + 2);
                triangles.Add(index + 2);
                triangles.Add(index + 1);
                triangles.Add(index + 3);
            }
        }

        if (vertices.Count == 0)
            return;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Debug.LogWarning("No shader available for tutorial dashed lines.", this);
            return;
        }

        if (lineMaterial == null)
            lineMaterial = new Material(shader) { name = "TutorialDashedLineMaterial" };

        GameObject lines = new GameObject("DashedLines");
        lines.transform.SetParent(hintInstance.transform, false);
        Mesh mesh = new Mesh { name = "TutorialDashedLines" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        lines.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = lines.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = lineMaterial;

        MaterialPropertyBlock properties = new MaterialPropertyBlock();
        properties.SetColor("_BaseColor", lineColor);
        properties.SetColor("_Color", lineColor);
        renderer.SetPropertyBlock(properties);
        renderer.sortingLayerName = sortingLayerName;
        renderer.sortingOrder = sortingOrder;
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
        {
            Transform lines = hintInstance.transform.Find("DashedLines");
            if (lines != null)
            {
                MeshFilter filter = lines.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh != null)
                    Destroy(filter.sharedMesh);
            }
            Destroy(hintInstance);
        }

        hintInstance = null;
    }
}
