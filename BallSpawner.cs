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

    private const int MinimumValidGroupCount = 3;

    private readonly Dictionary<BallType, int>
        antiStuckTypeCounts =
            new();

    private readonly List<BallType>
        antiStuckHighestTypes =
            new();

    private readonly List<Ball>
        antiStuckBounceBalls =
            new(64);

    private readonly List<Ball>
        antiStuckGroup =
            new(5);

    private readonly List<Ball>
        antiStuckRemainingBalls =
            new(64);

    private readonly List<Ball>
        antiStuckBestRemainingBalls =
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

    private bool ProcessAntiStuckBeforeSpawn()
    {
        if (gameManager == null ||
            !isPlayable ||
            currentPhase != GamePhase.Phase3)
        {
            return false;
        }

        gameManager.RemoveNullBalls();

        int validGroupCount =
            GetValidGroupCount(
                MinimumValidGroupCount
            );

        if (validGroupCount >=
            MinimumValidGroupCount)
        {
            return false;
        }

        IReadOnlyList<Ball> balls =
            gameManager.Balls;

        antiStuckTypeCounts.Clear();
        antiStuckHighestTypes.Clear();
        antiStuckBounceBalls.Clear();

        for (int i = 0;
             i < balls.Count;
             i++)
        {
            Ball ball =
                balls[i];

            if (!CanBallParticipateInAntiStuck(
                    ball))
            {
                continue;
            }

            if (antiStuckTypeCounts.TryGetValue(
                    ball.BallType,
                    out int count))
            {
                antiStuckTypeCounts[
                    ball.BallType
                ] = count + 1;
            }
            else
            {
                antiStuckTypeCounts.Add(
                    ball.BallType,
                    1
                );
            }
        }

        if (antiStuckTypeCounts.Count == 0)
        {
            return false;
        }

        int maximumTypeCount =
            int.MinValue;

        foreach (KeyValuePair<BallType, int> pair
                 in antiStuckTypeCounts)
        {
            if (pair.Value > maximumTypeCount)
            {
                maximumTypeCount =
                    pair.Value;

                antiStuckHighestTypes.Clear();

                antiStuckHighestTypes.Add(
                    pair.Key
                );
            }
            else if (pair.Value ==
                     maximumTypeCount)
            {
                antiStuckHighestTypes.Add(
                    pair.Key
                );
            }
        }

        if (antiStuckHighestTypes.Count == 0)
        {
            return false;
        }

        BallType selectedType =
            antiStuckHighestTypes[
                UnityEngine.Random.Range(
                    0,
                    antiStuckHighestTypes.Count
                )
            ];

        for (int i = 0;
             i < balls.Count;
             i++)
        {
            Ball ball =
                balls[i];

            if (CanBallParticipateInAntiStuck(
                    ball) &&
                ball.BallType == selectedType)
            {
                antiStuckBounceBalls.Add(
                    ball
                );
            }
        }

        if (antiStuckBounceBalls.Count == 0)
        {
            return false;
        }

        for (int i = 0;
             i < antiStuckBounceBalls.Count;
             i++)
        {
            Ball ball =
                antiStuckBounceBalls[i];

            if (ball == null)
            {
                continue;
            }

            gameManager.UnregisterBall(
                ball
            );

            ball.BeginBouncing();
        }

        antiStuckBounceBalls.Clear();

        return true;
    }


    /*
     * BallSpawner tự chịu trách nhiệm xác định
     * bàn chơi có tối đa bao nhiêu nhóm hợp lệ.
     *
     * BallGroupFinder chỉ được hỏi:
     * "Center Ball này có tạo thành nhóm hợp lệ không?"
     */
    private int GetValidGroupCount(
        int maximumCount)
    {
        antiStuckRemainingBalls.Clear();

        IReadOnlyList<Ball> balls =
            gameManager.Balls;

        for (int i = 0;
             i < balls.Count;
             i++)
        {
            Ball ball =
                balls[i];

            if (CanBallParticipateInAntiStuck(
                    ball))
            {
                antiStuckRemainingBalls.Add(
                    ball
                );
            }
        }

        return FindMaximumValidGroupCount(
            antiStuckRemainingBalls,
            maximumCount
        );
    }


    private int FindMaximumValidGroupCount(
        List<Ball> remainingBalls,
        int remainingLimit)
    {
        if (remainingLimit <= 0 ||
            remainingBalls == null ||
            remainingBalls.Count == 0)
        {
            return 0;
        }

        int bestCount = 0;

        for (int i = 0;
             i < remainingBalls.Count;
             i++)
        {
            Ball centerBall =
                remainingBalls[i];

            if (!BallGroupFinder.TryGetValidGroup(
                    centerBall,
                    remainingBalls,
                    antiStuckGroup))
            {
                continue;
            }

            antiStuckBestRemainingBalls.Clear();

            for (int j = 0;
                 j < remainingBalls.Count;
                 j++)
            {
                Ball ball =
                    remainingBalls[j];

                if (!antiStuckGroup.Contains(
                        ball))
                {
                    antiStuckBestRemainingBalls.Add(
                        ball
                    );
                }
            }

            List<Ball> nextBalls =
                new(
                    antiStuckBestRemainingBalls
                );

            int count =
                1 +
                FindMaximumValidGroupCount(
                    nextBalls,
                    remainingLimit - 1
                );

            if (count > bestCount)
            {
                bestCount =
                    count;
            }

            if (bestCount >= remainingLimit)
            {
                break;
            }
        }

        antiStuckGroup.Clear();

        return bestCount;
    }


    private static bool CanBallParticipateInAntiStuck(
        Ball ball)
    {
        return
            ball != null &&
            ball.IsSelectable &&
            !ball.IsBouncing &&
            !ball.IsDestroyRequested;
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