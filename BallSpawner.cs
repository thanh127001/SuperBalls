using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BallSpawner : MonoBehaviour
{
    /*
     * ========================================
     * REFERENCES
     * ========================================
     */

    [Header("References")]

    [SerializeField]
    private GameManager gameManager;

    [SerializeField]
    [Tooltip("Object dùng làm tọa độ gốc để spawn Ball. Nếu để trống, sẽ dùng Transform của BallSpawner.")]
    private Transform spawnOrigin;


    /*
     * ========================================
     * PREFABS
     * ========================================
     */

    [Serializable]
    private class BallPrefabEntry
    {
        [SerializeField]
        private BallType ballType;

        [SerializeField]
        private Ball prefab;


        public BallType BallType =>
            ballType;

        public Ball Prefab =>
            prefab;
    }


    [Header("Prefabs")]
    [SerializeField]
    private List<BallPrefabEntry> ballPrefabs =
        new();


    /*
     * ========================================
     * SPAWN
     * ========================================
     */

    private int columnCount = 6;

    private float maximumSpawnRotation = 45f;


    private const float SpawnPointSpacing = 1f;

    private const float SpawnYOffset = 2f;


    /*
     * ========================================
     * PHASE SPAWN INTERVALS
     * ========================================
     */

    private float phase1SpawnInterval = 0.05f;

    private float phase2SpawnInterval = 0.5f;

    private float phase3SpawnInterval = 1f;


    /*
     * ========================================
     * GIZMOS
     * ========================================
     */
    
    // Gizmos
    private float spawnPointRadius = 0.08f;


    /*
     * ========================================
     * RUNTIME
     * ========================================
     */

    private Coroutine spawnCoroutine;

    private bool isPlayable;
    private bool isSpawnable;

    private int[] columnBallCounts;

    private readonly List<int> availableColumns =
        new(8);


    /*
     * ========================================
     * ANTI-STUCK RUNTIME
     * ========================================
     */

    private const string SelectableBallLayerName =
        "Ball";

    private int selectableBallLayer = -1;

    private ContactFilter2D antiStuckContactFilter;

    private readonly RaycastHit2D[] antiStuckRaycastResults =
        new RaycastHit2D[64];

    private readonly List<Ball> antiStuckCandidates =
        new(64);


    /*
     * ========================================
     * WAIT CACHE
     * ========================================
     */

    private WaitForSeconds phase1SpawnWait;

    private WaitForSeconds phase2SpawnWait;

    private WaitForSeconds phase3SpawnWait;


    private GamePhase currentPhase =
        GamePhase.Phase1;


    /*
     * ========================================
     * PUBLIC PROPERTIES
     * ========================================
     */

    public float SpawnLineY =>
        SpawnOriginPosition.y +
        SpawnYOffset;


    private Vector3 SpawnOriginPosition =>
        spawnOrigin != null
            ? spawnOrigin.position
            : transform.position;


    public int MaxBallCount =>
        gameManager != null
            ? gameManager.MaxBallCount
            : 0;


    public int ColumnCount =>
        columnCount;


    public float SpawnPointSpacingValue =>
        SpawnPointSpacing;


    /*
     * ========================================
     * UNITY
     * ========================================
     */

    private void Awake()
    {
        FindReferences();

        InitializeRuntimeData();
    }


    private void OnEnable()
    {
        FindReferences();

        InitializeRuntimeData();


        if (!Application.isPlaying)
        {
            return;
        }


        if (gameManager != null)
        {
            gameManager.OnPlayingChanged +=
                OnPlayingChanged;

            gameManager.OnPhaseChanged +=
                OnPhaseChanged;

            currentPhase =
                gameManager.CurrentPhase;

            isPlayable = false;
            isSpawnable = false;

            RefreshSpawningState();
        }
    }


    private void OnDisable()
    {
        if (Application.isPlaying &&
            gameManager != null)
        {
            gameManager.OnPlayingChanged -=
                OnPlayingChanged;

            gameManager.OnPhaseChanged -=
                OnPhaseChanged;
        }

        isPlayable = false;
        isSpawnable = false;



        StopSpawning();
    }


#if UNITY_EDITOR

    private void OnValidate()
    {
        columnCount =
            Mathf.Max(
                1,
                columnCount
            );


        maximumSpawnRotation =
            Mathf.Clamp(
                maximumSpawnRotation,
                0f,
                180f
            );


        phase1SpawnInterval =
            Mathf.Max(
                0.01f,
                phase1SpawnInterval
            );


        phase2SpawnInterval =
            Mathf.Max(
                0.01f,
                phase2SpawnInterval
            );


        phase3SpawnInterval =
            Mathf.Max(
                0.01f,
                phase3SpawnInterval
            );


        spawnPointRadius =
            Mathf.Max(
                0.01f,
                spawnPointRadius
            );


        FindReferences();

        InitializeRuntimeData();
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
            gameManager =
                FindFirstObjectByType
                    <GameManager>();
        }
    }


    private bool EnsureReferences()
    {
        if (gameManager != null)
        {
            return true;
        }


        FindReferences();


        return gameManager != null;
    }


    /*
     * ========================================
     * INITIALIZATION
     * ========================================
     */

    private void InitializeRuntimeData()
    {
        columnCount =
            Mathf.Max(
                1,
                columnCount
            );


        EnsureColumnBuffer();


        phase1SpawnWait =
            new WaitForSeconds(
                Mathf.Max(
                    0.01f,
                    phase1SpawnInterval
                )
            );


        phase2SpawnWait =
            new WaitForSeconds(
                Mathf.Max(
                    0.01f,
                    phase2SpawnInterval
                )
            );


        phase3SpawnWait =
            new WaitForSeconds(
                Mathf.Max(
                    0.01f,
                    phase3SpawnInterval
                )
            );


        InitializeAntiStuckFilter();
    }


    /*
     * ========================================
     * PLAYING STATE
     * ========================================
     */

    private void OnPlayingChanged()
    {
        if (gameManager == null)
        {
            return;
        }

        isPlayable = gameManager.EffectivePlayable;
        isSpawnable = gameManager.EffectiveSpawnable;

        RefreshSpawningState();
    }


    private void RefreshSpawningState()
    {
        if (CanSpawnBalls())
        {
            StartSpawning();
        }
        else
        {
            StopSpawning();
        }
    }


    /*
     * ========================================
     * PHASE
     * ========================================
     */

    private void OnPhaseChanged()
    {
        if (gameManager == null)
        {
            return;
        }

        currentPhase =
            gameManager.CurrentPhase;
    }


    /*
     * ========================================
     * START / STOP
     * ========================================
     */

    private void StartSpawning()
    {
        if (!Application.isPlaying)
        {
            return;
        }


        if (!EnsureReferences())
        {
            return;
        }


        if (!CanSpawnBalls())
        {
            return;
        }


        if (spawnCoroutine != null)
        {
            return;
        }


        spawnCoroutine =
            StartCoroutine(
                SpawnRoutine()
            );
    }


    private void StopSpawning()
    {
        if (spawnCoroutine == null)
        {
            return;
        }


        StopCoroutine(
            spawnCoroutine
        );


        spawnCoroutine = null;
    }


    private bool CanSpawnBalls()
    {
        return
            gameManager != null &&
            isSpawnable;
    }


    /*
     * ========================================
     * SPAWN ROUTINE
     * ========================================
     */

    private IEnumerator SpawnRoutine()
    {
        while (CanSpawnBalls())
        {
            if (!EnsureReferences())
            {
                break;
            }


            ProcessAntiStuckBeforeSpawn();


            SpawnBall();


            if (!CanSpawnBalls())
            {
                break;
            }


            yield return
                GetSpawnWaitInstruction();
        }


        spawnCoroutine = null;
    }


    /*
     * ========================================
     * SPAWN BALL
     * ========================================
     */

    private Ball SpawnBall()
    {
        if (!CanSpawnBalls())
        {
            return null;
        }


        if (gameManager == null)
        {
            return null;
        }


        if (ballPrefabs == null ||
            ballPrefabs.Count == 0)
        {
            Debug.LogWarning(
                $"{nameof(BallSpawner)} chưa có " +
                "Ball Prefab.",
                this
            );

            return null;
        }


        if (!TryGetSpawnX(
                out float spawnX))
        {
            return null;
        }


        if (!TryGetSpawnBallType(
                out BallType ballType))
        {
            return null;
        }


        if (!TryGetBallPrefab(
                ballType,
                out Ball selectedPrefab))
        {
            Debug.LogWarning(
                $"Không tìm thấy Ball Prefab cho " +
                $"{ballType} ở Level " +
                $"{gameManager.CurrentLevel}.",
                this
            );

            return null;
        }


        Vector3 spawnPosition =
            new(
                spawnX,
                SpawnLineY,
                SpawnOriginPosition.z
            );


        float rotationZ =
            UnityEngine.Random.Range(
                -maximumSpawnRotation,
                maximumSpawnRotation
            );


        Quaternion spawnRotation =
            Quaternion.Euler(
                0f,
                0f,
                rotationZ
            );


        Ball spawnedBall =
            Instantiate(
                selectedPrefab,
                spawnPosition,
                spawnRotation
            );


        if (spawnedBall == null)
        {
            return null;
        }


        gameManager.RegisterBall(
            spawnedBall
        );


        return spawnedBall;
    }


    /*
     * ========================================
     * SPAWN BALL TYPE
     * ========================================
     */

    private bool TryGetSpawnBallType(
        out BallType ballType)
    {
        ballType = default;


        if (gameManager == null)
        {
            return false;
        }


        LevelConfig config =
            gameManager.GetCurrentLevelConfig();


        if (config == null ||
            config.BallTypes == null ||
            config.BallTypes.Length == 0)
        {
            Debug.LogWarning(
                $"Level {gameManager.CurrentLevel} " +
                "không có BallType để spawn.",
                this
            );

            return false;
        }


        ballType =
            config.GetRandomBallType();


        return true;
    }


    private bool TryGetBallPrefab(
        BallType ballType,
        out Ball prefab)
    {
        prefab = null;


        if (ballPrefabs == null ||
            ballPrefabs.Count == 0)
        {
            return false;
        }


        for (int i = 0;
             i < ballPrefabs.Count;
             i++)
        {
            BallPrefabEntry entry =
                ballPrefabs[i];


            if (entry == null ||
                entry.BallType != ballType ||
                entry.Prefab == null)
            {
                continue;
            }


            prefab =
                entry.Prefab;


            return true;
        }


        return false;
    }


    /*
     * ========================================
     * ANTI-STUCK
     * ========================================
     */

    private void InitializeAntiStuckFilter()
    {
        selectableBallLayer =
            LayerMask.NameToLayer(
                SelectableBallLayerName
            );


        int layerMask =
            selectableBallLayer >= 0
                ? 1 << selectableBallLayer
                : 0;


        antiStuckContactFilter =
            new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = layerMask,
                useTriggers = true
            };
    }


    private bool ProcessAntiStuckBeforeSpawn()
    {
        /*
         * Không cần kiểm tra State riêng ở đây.
         *
         * ProcessAntiStuckBeforeSpawn chỉ được
         * gọi bởi SpawnRoutine, mà SpawnRoutine
         * chỉ chạy khi CanSpawnBalls() == true.
         */
        if (gameManager == null ||
            !isPlayable ||
            currentPhase != GamePhase.Phase3 ||
            selectableBallLayer < 0)
        {
            return false;
        }


        gameManager.RemoveNullBalls();


        if (HasPotentialGroupInternal())
        {
            return false;
        }


        antiStuckCandidates.Clear();


        float lowestY =
            float.PositiveInfinity;


        IReadOnlyList<Ball> balls =
            gameManager.Balls;


        for (int i = 0;
             i < balls.Count;
             i++)
        {
            Ball candidate =
                balls[i];


            if (!CanBallParticipateInAntiStuck(
                    candidate))
            {
                continue;
            }


            float y =
                candidate.transform.position.y;


            if (y < lowestY)
            {
                lowestY = y;
            }
        }


        if (float.IsPositiveInfinity(
                lowestY))
        {
            return false;
        }


        const float LowestYTolerance =
            0.05f;


        for (int i = 0;
             i < balls.Count;
             i++)
        {
            Ball candidate =
                balls[i];


            if (!CanBallParticipateInAntiStuck(
                    candidate))
            {
                continue;
            }


            float y =
                candidate.transform.position.y;


            if (Mathf.Abs(
                    y - lowestY) <=
                LowestYTolerance)
            {
                antiStuckCandidates.Add(
                    candidate
                );
            }
        }


        if (antiStuckCandidates.Count == 0)
        {
            return false;
        }


        int randomIndex =
            UnityEngine.Random.Range(
                0,
                antiStuckCandidates.Count
            );


        Ball selectedBall =
            antiStuckCandidates[
                randomIndex
            ];


        antiStuckCandidates.Clear();


        if (selectedBall == null)
        {
            return false;
        }


        /*
         * Ball rời gameplay ngay.
         */
        gameManager.UnregisterBall(
            selectedBall
        );


        /*
         * Ball tự quản lý toàn bộ bounce.
         */
        selectedBall.BeginBouncing();


        return true;
    }


    private bool HasPotentialGroupInternal()
    {
        if (gameManager == null)
        {
            return false;
        }


        IReadOnlyList<Ball> balls =
            gameManager.Balls;


        for (int i = 0;
             i < balls.Count;
             i++)
        {
            Ball centerBall =
                balls[i];


            if (!CanBallParticipateInAntiStuck(
                    centerBall))
            {
                continue;
            }


            int sameTypeNeighborCount =
                CountSameTypeVisibleNeighbors(
                    centerBall
                );


            if (sameTypeNeighborCount >= 2)
            {
                return true;
            }
        }


        return false;
    }


    private int CountSameTypeVisibleNeighbors(
        Ball candidate)
    {
        if (candidate == null)
        {
            return 0;
        }


        BallType ballType =
            candidate.BallType;


        int count = 0;


        if (IsFirstVisibleBallSameType(
                candidate,
                Vector2.left,
                ballType) &&
            ++count >= 2)
        {
            return count;
        }


        if (IsFirstVisibleBallSameType(
                candidate,
                Vector2.right,
                ballType) &&
            ++count >= 2)
        {
            return count;
        }


        if (IsFirstVisibleBallSameType(
                candidate,
                Vector2.up,
                ballType) &&
            ++count >= 2)
        {
            return count;
        }


        if (IsFirstVisibleBallSameType(
                candidate,
                Vector2.down,
                ballType))
        {
            count++;
        }


        return count;
    }


    private bool IsFirstVisibleBallSameType(
        Ball candidate,
        Vector2 direction,
        BallType targetType)
    {
        if (candidate == null)
        {
            return false;
        }


        int hitCount =
            Physics2D.Raycast(
                candidate.transform.position,
                direction,
                antiStuckContactFilter,
                antiStuckRaycastResults,
                Mathf.Infinity
            );


        if (hitCount <= 0)
        {
            return false;
        }


        Ball nearestBall = null;


        float nearestDistance =
            float.PositiveInfinity;


        for (int i = 0;
             i < hitCount;
             i++)
        {
            RaycastHit2D hit =
                antiStuckRaycastResults[i];


            if (hit.collider == null)
            {
                continue;
            }


            Ball hitBall =
                hit.collider
                    .GetComponentInParent<Ball>();


            if (hitBall == null ||
                hitBall == candidate ||
                !CanBallParticipateInAntiStuck(
                    hitBall) ||
                hit.distance >= nearestDistance)
            {
                continue;
            }


            nearestDistance =
                hit.distance;

            nearestBall =
                hitBall;
        }


        ClearAntiStuckRaycastBuffer(
            hitCount
        );


        return
            nearestBall != null &&
            nearestBall.BallType ==
                targetType;
    }


    private bool CanBallParticipateInAntiStuck(
        Ball ball)
    {
        return
            ball != null &&
            !ball.IsBouncing &&
            !ball.IsDestroyRequested &&
            ball.HasEnteredPlayArea &&
            ball.gameObject.layer ==
                selectableBallLayer;
    }


    private void ClearAntiStuckRaycastBuffer(
        int hitCount)
    {
        int clearCount =
            Mathf.Min(
                hitCount,
                antiStuckRaycastResults.Length
            );


        for (int i = 0;
             i < clearCount;
             i++)
        {
            antiStuckRaycastResults[i] =
                default;
        }
    }


    /*
     * ========================================
     * SPAWN COLUMN
     * ========================================
     */

    private bool TryGetSpawnX(
        out float spawnX)
    {
        spawnX = 0f;


        if (gameManager == null)
        {
            return false;
        }


        EnsureColumnBuffer();


        Array.Clear(
            columnBallCounts,
            0,
            columnBallCounts.Length
        );


        float firstSpawnX =
            GetFirstSpawnX();


        IReadOnlyList<Ball> balls =
            gameManager.Balls;


        for (int i = 0;
             i < balls.Count;
             i++)
        {
            Ball ball =
                balls[i];


            if (ball == null ||
                ball.IsBouncing ||
                ball.IsDestroyRequested)
            {
                continue;
            }


            float ballX =
                ball.transform.position.x;


            if (!IsFinite(ballX))
            {
                continue;
            }


            int columnIndex =
                GetColumnIndex(
                    ballX,
                    firstSpawnX
                );


            if (columnIndex < 0 ||
                columnIndex >= columnCount)
            {
                continue;
            }


            columnBallCounts[
                columnIndex
            ]++;
        }


        int minimumCount =
            columnBallCounts[0];

        int maximumCount =
            columnBallCounts[0];


        for (int i = 1;
             i < columnCount;
             i++)
        {
            int count =
                columnBallCounts[i];


            if (count < minimumCount)
            {
                minimumCount =
                    count;
            }


            if (count > maximumCount)
            {
                maximumCount =
                    count;
            }
        }


        bool allColumnsEqual =
            minimumCount ==
            maximumCount;


        availableColumns.Clear();


        for (int i = 0;
             i < columnCount;
             i++)
        {
            if (allColumnsEqual)
            {
                availableColumns.Add(i);

                continue;
            }


            if (columnBallCounts[i] ==
                maximumCount)
            {
                continue;
            }


            availableColumns.Add(i);
        }


        if (availableColumns.Count == 0)
        {
            return false;
        }


        int randomIndex =
            UnityEngine.Random.Range(
                0,
                availableColumns.Count
            );


        int selectedColumn =
            availableColumns[
                randomIndex
            ];


        spawnX =
            GetSpawnX(
                firstSpawnX,
                selectedColumn
            );


        return IsFinite(
            spawnX
        );
    }


    /*
     * ========================================
     * SPAWN POINT CALCULATION
     * ========================================
     */

    private float GetFirstSpawnX()
    {
        return
            SpawnOriginPosition.x -
            (columnCount - 1) *
            SpawnPointSpacing *
            0.5f;
    }


    private static float GetSpawnX(
        float firstSpawnX,
        int columnIndex)
    {
        return
            firstSpawnX +
            columnIndex *
            SpawnPointSpacing;
    }


    private int GetColumnIndex(
        float worldX,
        float firstSpawnX)
    {
        float columnPosition =
            (worldX - firstSpawnX) /
            SpawnPointSpacing;


        if (columnPosition < -0.5f ||
            columnPosition >
            columnCount - 0.5f)
        {
            return -1;
        }


        return Mathf.RoundToInt(
            columnPosition
        );
    }


    /*
     * ========================================
     * COLUMN BUFFER
     * ========================================
     */

    private void EnsureColumnBuffer()
    {
        if (columnBallCounts != null &&
            columnBallCounts.Length ==
            columnCount)
        {
            return;
        }


        columnBallCounts =
            new int[columnCount];
    }


    /*
     * ========================================
     * SPAWN INTERVAL
     * ========================================
     */

    private YieldInstruction
        GetSpawnWaitInstruction()
    {
        return currentPhase switch
        {
            GamePhase.Phase1 =>
                phase1SpawnWait,

            GamePhase.Phase2 =>
                phase2SpawnWait,

            _ =>
                phase3SpawnWait
        };
    }


    /*
     * ========================================
     * VALIDATION
     * ========================================
     */

    private static bool IsFinite(
        float value)
    {
        return
            !float.IsNaN(value) &&
            !float.IsInfinity(value);
    }


    /*
     * ========================================
     * GIZMOS
     * ========================================
     */

    private void OnDrawGizmos()
    {
        int safeColumnCount =
            Mathf.Max(
                1,
                columnCount
            );


        Vector3 originPosition =
            SpawnOriginPosition;


        float firstSpawnX =
            originPosition.x -
            (safeColumnCount - 1) *
            SpawnPointSpacing *
            0.5f;


        Gizmos.color =
            Color.yellow;


        for (int i = 0;
             i < safeColumnCount;
             i++)
        {
            float spawnX =
                firstSpawnX +
                i *
                SpawnPointSpacing;


            Vector3 spawnPoint =
                new(
                    spawnX,
                    SpawnLineY,
                    originPosition.z
                );


            Gizmos.DrawWireSphere(
                spawnPoint,
                spawnPointRadius
            );
        }
    }
}