using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BallDanceController : MonoBehaviour
{
    /*
     * ========================================
     * SINGLETON
     * ========================================
     */

    public static BallDanceController Instance { get; private set; }


    /*
     * ========================================
     * REFERENCES
     * ========================================
     */

    [Header("References")]

    [SerializeField]
    private GameManager gameManager;


    /*
     * ========================================
     * BALL PREFABS
     * ========================================
     */

    [Header("Ball Prefabs")]

    [SerializeField]
    private List<GameObject> ballPrefabs = new();


    /*
     * ========================================
     * MOVE AREA
     * ========================================
     */

    // Kích thước hình chữ nhật mà Ball sẽ di chuyển bên trong.
    private const float AreaWidth = 6f;

    // Kích thước hình chữ nhật mà Ball sẽ di chuyển bên trong.
    private const float AreaHeight = 10f;

    // Khoảng cách từ mép Ball đến mép hình chữ nhật để Ball được destroy.
    private const float OutsideOffset = 2f;

    [SerializeField]
    private bool showMoveArea = true;


    /*
     * ========================================
     * BALL SIZE
     * ========================================
     */

    // Đường kính của Ball.
    private const float BallDiameter = 1f;

    // Bán kính của Ball.
    private const float BallRadius = BallDiameter * 0.5f;


    /*
     * ========================================
     * SPAWN
     * ========================================
     */

    // Số lượng Ball tối đa có thể tồn tại cùng lúc.
    private int maximumBallCount = 20;

    // Thời gian giữa các lần spawn Ball.
    private float spawnInterval = 0.2f;

    // Thời gian delay trước khi bắt đầu spawn Ball.
    private float spawnStartDelay = 0f;


    /*
     * ========================================
     * COLLISION
     * ========================================
     */

    // Số lần va chạm với tường trước khi Ball có thể thoát ra ngoài.
    private int collisionsToEscape = 5;


    /*
     * ========================================
     * MOVE
     * ========================================
     */

    // Tốc độ di chuyển tối thiểu của Ball.
    private float minimumMoveSpeed = 2f;

    // Tốc độ di chuyển tối đa của Ball.
    private float maximumMoveSpeed = 10f;


    /*
     * ========================================
     * ROTATION
     * ========================================
     */

    // Tốc độ xoay tối thiểu của Ball.
    private float minimumRotationSpeed = 20f;

    // Tốc độ xoay tối đa của Ball.
    private float maximumRotationSpeed = 90f;


    /*
     * ========================================
     * STATE
     * ========================================
     */

    private readonly List<DanceBall> danceBalls =
        new();

    private Coroutine spawnCoroutine;

    private bool isPlaying;


    /*
     * ========================================
     * SPAWN EDGE
     * ========================================
     */

    private enum SpawnEdge
    {
        Left,
        Right,
        Bottom,
        Top
    }


    /*
     * ========================================
     * DANCE BALL
     * ========================================
     */

    private sealed class DanceBall
    {
        public GameObject GameObject;

        public Vector2 Velocity;

        public float RotationSpeed;

        public int CollisionCount;

        public bool HasEnteredArea;

        public bool CanEscape;
    }


    /*
     * ========================================
     * UNITY
     * ========================================
     */

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        FindReferences();
    }

    private void OnEnable()
    {
        FindReferences();

        if (!Application.isPlaying ||
            gameManager == null)
        {
            return;
        }

        gameManager.OnGameStateChanged +=
            OnGameStateChanged;

        /*
         * Đồng bộ với state hiện tại.
         * Nếu object được enable khi game đã
         * GameCompleted thì effect vẫn chạy.
         */
        OnGameStateChanged();
    }

    private void OnDisable()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (gameManager != null)
        {
            gameManager.OnGameStateChanged -=
                OnGameStateChanged;
        }

        Stop();
    }

    private void Update()
    {
        if (!isPlaying)
        {
            return;
        }

        UpdateBalls();

        ResolveBallCollisions();

        RemoveDestroyedBalls();
    }

    private void OnDestroy()
    {
        StopSpawnCoroutine();

        DestroyBalls();

        if (Instance == this)
        {
            Instance = null;
        }
    }


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
                FindFirstObjectByType<GameManager>();
        }
    }


    /*
     * ========================================
     * GAME STATE
     * ========================================
     */

    private void OnGameStateChanged()
    {
        if (gameManager == null)
        {
            return;
        }

        switch (gameManager.State)
        {
            case GameState.GameCompleted:

                Play();

                break;


            case GameState.Initializing:
            case GameState.Ready:

                Stop();

                break;


            case GameState.None:
            case GameState.Playing:
            case GameState.Paused:
            case GameState.LevelCompleted:
            case GameState.GameOver:
            default:

                break;
        }
    }


    /*
     * ========================================
     * PUBLIC
     * ========================================
     */

    public void Play()
    {
        if (isPlaying)
        {
            return;
        }

        DestroyBalls();

        isPlaying = true;

        StartSpawnCoroutine();
    }

    public void Stop()
    {
        isPlaying = false;

        StopSpawnCoroutine();

        DestroyBalls();
    }


    /*
     * ========================================
     * SPAWN ROUTINE
     * ========================================
     */

    private void StartSpawnCoroutine()
    {
        StopSpawnCoroutine();

        spawnCoroutine =
            StartCoroutine(
                SpawnRoutine()
            );
    }

    private void StopSpawnCoroutine()
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

    private IEnumerator SpawnRoutine()
    {
        /*
         * ========================================
         * START DELAY
         * ========================================
         */

        if (spawnStartDelay > 0f)
        {
            yield return new WaitForSeconds(
                spawnStartDelay
            );
        }

        while (isPlaying)
        {
            RemoveDestroyedBalls();

            if (danceBalls.Count <
                maximumBallCount)
            {
                SpawnBall();
            }

            yield return new WaitForSeconds(
                spawnInterval
            );
        }

        spawnCoroutine = null;
    }


    /*
     * ========================================
     * SPAWN BALL
     * ========================================
     */

    private void SpawnBall()
    {
        if (ballPrefabs.Count == 0)
        {
            return;
        }

        GameObject prefab =
            GetRandomPrefab();

        if (prefab == null)
        {
            return;
        }

        CreateBall(
            prefab
        );
    }

    private GameObject GetRandomPrefab()
    {
        int validPrefabCount = 0;

        for (int i = 0;
             i < ballPrefabs.Count;
             i++)
        {
            if (ballPrefabs[i] != null)
            {
                validPrefabCount++;
            }
        }

        if (validPrefabCount == 0)
        {
            return null;
        }

        int randomIndex =
            Random.Range(
                0,
                validPrefabCount
            );

        for (int i = 0;
             i < ballPrefabs.Count;
             i++)
        {
            GameObject prefab =
                ballPrefabs[i];

            if (prefab == null)
            {
                continue;
            }

            if (randomIndex == 0)
            {
                return prefab;
            }

            randomIndex--;
        }

        return null;
    }

    private void CreateBall(
        GameObject prefab)
    {
        SpawnEdge spawnEdge =
            (SpawnEdge)Random.Range(
                0,
                4
            );

        Vector2 position =
            GetEdgeSpawnPosition(
                spawnEdge
            );

        float moveSpeed =
            Random.Range(
                minimumMoveSpeed,
                maximumMoveSpeed
            );

        Vector2 direction =
            GetSpawnDirection(
                spawnEdge
            );

        Vector2 velocity =
            direction *
            moveSpeed;

        float rotationSpeed =
            Random.Range(
                minimumRotationSpeed,
                maximumRotationSpeed
            );

        if (Random.value < 0.5f)
        {
            rotationSpeed =
                -rotationSpeed;
        }

        GameObject ballObject =
            Instantiate(
                prefab,
                position,
                Quaternion.identity
            );

        RemoveGameplayComponents(
            ballObject
        );

        DanceBall danceBall =
            new DanceBall
            {
                GameObject =
                    ballObject,

                Velocity =
                    velocity,

                RotationSpeed =
                    rotationSpeed,

                CollisionCount =
                    0,

                HasEnteredArea =
                    false,

                CanEscape =
                    false
            };

        danceBalls.Add(
            danceBall
        );
    }


    /*
     * ========================================
     * REMOVE GAMEPLAY COMPONENTS
     * ========================================
     */

    private static void RemoveGameplayComponents(
        GameObject ballObject)
    {
        if (ballObject == null)
        {
            return;
        }

        CircleCollider2D[] circleColliders =
            ballObject.GetComponentsInChildren<CircleCollider2D>(true);

        for (int i = 0;
             i < circleColliders.Length;
             i++)
        {
            if (circleColliders[i] != null)
            {
                Destroy(
                    circleColliders[i]
                );
            }
        }

        Rigidbody2D[] rigidbodies =
            ballObject.GetComponentsInChildren<Rigidbody2D>(true);

        for (int i = 0;
             i < rigidbodies.Length;
             i++)
        {
            if (rigidbodies[i] != null)
            {
                Destroy(
                    rigidbodies[i]
                );
            }
        }

        Ball[] ballScripts =
            ballObject.GetComponentsInChildren<Ball>(true);

        for (int i = 0;
             i < ballScripts.Length;
             i++)
        {
            if (ballScripts[i] != null)
            {
                Destroy(
                    ballScripts[i]
                );
            }
        }
    }


    /*
     * ========================================
     * SPAWN POSITION
     * ========================================
     */

    private Vector2 GetEdgeSpawnPosition(
        SpawnEdge spawnEdge)
    {
        GetAreaBounds(
            out float left,
            out float right,
            out float bottom,
            out float top
        );

        float outsideDistance =
            BallRadius +
            OutsideOffset;

        float x;
        float y;

        switch (spawnEdge)
        {
            case SpawnEdge.Left:

                x =
                    left -
                    outsideDistance;

                y =
                    Random.Range(
                        bottom + BallRadius,
                        top - BallRadius
                    );

                break;


            case SpawnEdge.Right:

                x =
                    right +
                    outsideDistance;

                y =
                    Random.Range(
                        bottom + BallRadius,
                        top - BallRadius
                    );

                break;


            case SpawnEdge.Bottom:

                x =
                    Random.Range(
                        left + BallRadius,
                        right - BallRadius
                    );

                y =
                    bottom -
                    outsideDistance;

                break;


            case SpawnEdge.Top:

                x =
                    Random.Range(
                        left + BallRadius,
                        right - BallRadius
                    );

                y =
                    top +
                    outsideDistance;

                break;


            default:

                x =
                    left -
                    outsideDistance;

                y =
                    transform.position.y;

                break;
        }

        return new Vector2(
            x,
            y
        );
    }


    /*
     * ========================================
     * SPAWN DIRECTION
     * ========================================
     */

    private Vector2 GetSpawnDirection(
        SpawnEdge spawnEdge)
    {
        switch (spawnEdge)
        {
            case SpawnEdge.Left:
                return Vector2.right;

            case SpawnEdge.Right:
                return Vector2.left;

            case SpawnEdge.Bottom:
                return Vector2.up;

            case SpawnEdge.Top:
                return Vector2.down;

            default:
                return Vector2.right;
        }
    }


    /*
     * ========================================
     * UPDATE BALLS
     * ========================================
     */

    private void UpdateBalls()
    {
        GetMovementBounds(
            out float left,
            out float right,
            out float bottom,
            out float top
        );

        for (int i = danceBalls.Count - 1;
             i >= 0;
             i--)
        {
            DanceBall ball =
                danceBalls[i];

            if (ball.GameObject == null)
            {
                continue;
            }

            Transform ballTransform =
                ball.GameObject.transform;

            Vector2 position =
                ballTransform.position;


            /*
             * ========================================
             * MOVE
             * ========================================
             */

            position +=
                ball.Velocity *
                Time.deltaTime;


            /*
             * ========================================
             * ENTER AREA
             * ========================================
             */

            if (!ball.HasEnteredArea)
            {
                if (IsBallCompletelyInsideArea(
                    position))
                {
                    ball.HasEnteredArea =
                        true;
                }
            }


            /*
             * ========================================
             * WALL COLLISION
             * ========================================
             */

            else if (!ball.CanEscape)
            {
                ResolveWallCollisions(
                    ball,
                    ref position,
                    left,
                    right,
                    bottom,
                    top
                );
            }


            /*
             * ========================================
             * APPLY POSITION
             * ========================================
             */

            ballTransform.position =
                position;


            /*
             * ========================================
             * ROTATION
             * ========================================
             */

            ballTransform.Rotate(
                0f,
                0f,
                ball.RotationSpeed *
                Time.deltaTime
            );


            /*
             * ========================================
             * DESTROY
             * ========================================
             */

            if (ball.CanEscape &&
                IsBallBeyondDestroyBounds(
                    position))
            {
                Destroy(
                    ball.GameObject
                );
            }
        }
    }


    /*
     * ========================================
     * WALL COLLISION
     * ========================================
     */

    private void ResolveWallCollisions(
        DanceBall ball,
        ref Vector2 position,
        float left,
        float right,
        float bottom,
        float top)
    {
        /*
         * LEFT
         */

        if (position.x <= left)
        {
            position.x =
                left;

            RegisterCollision(
                ball
            );

            if (ball.CanEscape)
            {
                return;
            }

            ball.Velocity =
                new Vector2(
                    Mathf.Abs(
                        ball.Velocity.x
                    ),
                    ball.Velocity.y
                );
        }


        /*
         * RIGHT
         */

        else if (position.x >= right)
        {
            position.x =
                right;

            RegisterCollision(
                ball
            );

            if (ball.CanEscape)
            {
                return;
            }

            ball.Velocity =
                new Vector2(
                    -Mathf.Abs(
                        ball.Velocity.x
                    ),
                    ball.Velocity.y
                );
        }


        /*
         * BOTTOM
         */

        if (position.y <= bottom)
        {
            position.y =
                bottom;

            RegisterCollision(
                ball
            );

            if (ball.CanEscape)
            {
                return;
            }

            ball.Velocity =
                new Vector2(
                    ball.Velocity.x,
                    Mathf.Abs(
                        ball.Velocity.y
                    )
                );
        }


        /*
         * TOP
         */

        else if (position.y >= top)
        {
            position.y =
                top;

            RegisterCollision(
                ball
            );

            if (ball.CanEscape)
            {
                return;
            }

            ball.Velocity =
                new Vector2(
                    ball.Velocity.x,
                    -Mathf.Abs(
                        ball.Velocity.y
                    )
                );
        }
    }


    /*
     * ========================================
     * BALL COLLISIONS
     * ========================================
     */

    private void ResolveBallCollisions()
    {
        float minimumDistance =
            BallDiameter;

        float minimumDistanceSquared =
            minimumDistance *
            minimumDistance;

        for (int i = 0;
             i < danceBalls.Count - 1;
             i++)
        {
            DanceBall ballA =
                danceBalls[i];

            if (ballA.GameObject == null ||
                !ballA.HasEnteredArea)
            {
                continue;
            }

            for (int j = i + 1;
                 j < danceBalls.Count;
                 j++)
            {
                DanceBall ballB =
                    danceBalls[j];

                if (ballB.GameObject == null ||
                    !ballB.HasEnteredArea)
                {
                    continue;
                }

                ResolveBallCollision(
                    ballA,
                    ballB,
                    minimumDistance,
                    minimumDistanceSquared
                );
            }
        }
    }

    private void ResolveBallCollision(
        DanceBall ballA,
        DanceBall ballB,
        float minimumDistance,
        float minimumDistanceSquared)
    {
        Vector2 positionA =
            ballA.GameObject.transform.position;

        Vector2 positionB =
            ballB.GameObject.transform.position;

        Vector2 delta =
            positionB -
            positionA;

        float distanceSquared =
            delta.sqrMagnitude;

        if (distanceSquared >
            minimumDistanceSquared)
        {
            return;
        }


        /*
         * COLLISION NORMAL
         */

        Vector2 normal;

        float distance;

        if (distanceSquared >
            0.000001f)
        {
            distance =
                Mathf.Sqrt(
                    distanceSquared
                );

            normal =
                delta /
                distance;
        }
        else
        {
            normal =
                GetRandomDirection();

            distance =
                0f;
        }


        /*
         * SEPARATE OVERLAP
         */

        float overlap =
            minimumDistance -
            distance;

        if (overlap > 0f)
        {
            Vector2 correction =
                normal *
                (
                    overlap *
                    0.5f
                );

            positionA -=
                correction;

            positionB +=
                correction;

            ballA.GameObject.transform.position =
                positionA;

            ballB.GameObject.transform.position =
                positionB;
        }


        /*
         * CHECK APPROACHING
         */

        Vector2 relativeVelocity =
            ballB.Velocity -
            ballA.Velocity;

        float velocityAlongNormal =
            Vector2.Dot(
                relativeVelocity,
                normal
            );

        if (velocityAlongNormal >= 0f)
        {
            ClampNormalBallInsideArea(
                ballA
            );

            ClampNormalBallInsideArea(
                ballB
            );

            return;
        }


        /*
         * ELASTIC COLLISION
         */

        float velocityAAlongNormal =
            Vector2.Dot(
                ballA.Velocity,
                normal
            );

        float velocityBAlongNormal =
            Vector2.Dot(
                ballB.Velocity,
                normal
            );

        Vector2 velocityAChange =
            normal *
            (
                velocityBAlongNormal -
                velocityAAlongNormal
            );

        Vector2 velocityBChange =
            normal *
            (
                velocityAAlongNormal -
                velocityBAlongNormal
            );

        ballA.Velocity +=
            velocityAChange;

        ballB.Velocity +=
            velocityBChange;


        /*
         * COLLISION COUNT
         */

        RegisterCollision(
            ballA
        );

        RegisterCollision(
            ballB
        );


        /*
         * KEEP NORMAL BALL INSIDE
         */

        ClampNormalBallInsideArea(
            ballA
        );

        ClampNormalBallInsideArea(
            ballB
        );
    }


    /*
     * ========================================
     * REGISTER COLLISION
     * ========================================
     */

    private void RegisterCollision(
        DanceBall ball)
    {
        if (ball == null ||
            ball.GameObject == null ||
            ball.CanEscape)
        {
            return;
        }

        ball.CollisionCount++;

        if (ball.CollisionCount <
            collisionsToEscape)
        {
            return;
        }

        ball.CanEscape =
            true;
    }


    /*
     * ========================================
     * NORMAL BALL CLAMP
     * ========================================
     */

    private void ClampNormalBallInsideArea(
        DanceBall ball)
    {
        if (ball.GameObject == null ||
            ball.CanEscape ||
            !ball.HasEnteredArea)
        {
            return;
        }

        GetMovementBounds(
            out float left,
            out float right,
            out float bottom,
            out float top
        );

        Vector2 position =
            ball.GameObject.transform.position;

        position.x =
            Mathf.Clamp(
                position.x,
                left,
                right
            );

        position.y =
            Mathf.Clamp(
                position.y,
                bottom,
                top
            );

        ball.GameObject.transform.position =
            position;
    }


    /*
     * ========================================
     * COMPLETELY INSIDE
     * ========================================
     */

    private bool IsBallCompletelyInsideArea(
        Vector2 position)
    {
        GetMovementBounds(
            out float left,
            out float right,
            out float bottom,
            out float top
        );

        return
            position.x >= left &&
            position.x <= right &&
            position.y >= bottom &&
            position.y <= top;
    }


    /*
     * ========================================
     * DESTROY BOUNDS
     * ========================================
     */

    private bool IsBallBeyondDestroyBounds(
        Vector2 position)
    {
        GetAreaBounds(
            out float left,
            out float right,
            out float bottom,
            out float top
        );

        /*
         * Ball chỉ bị destroy khi toàn bộ Ball
         * đã nằm ngoài area và mép Ball
         * cách area tối thiểu OutsideOffset.
         */


        /*
         * LEFT
         */

        if (position.x + BallRadius <=
            left - OutsideOffset)
        {
            return true;
        }


        /*
         * RIGHT
         */

        if (position.x - BallRadius >=
            right + OutsideOffset)
        {
            return true;
        }


        /*
         * BOTTOM
         */

        if (position.y + BallRadius <=
            bottom - OutsideOffset)
        {
            return true;
        }


        /*
         * TOP
         */

        if (position.y - BallRadius >=
            top + OutsideOffset)
        {
            return true;
        }

        return false;
    }


    /*
     * ========================================
     * MOVEMENT BOUNDS
     * ========================================
     */

    private void GetMovementBounds(
        out float left,
        out float right,
        out float bottom,
        out float top)
    {
        GetAreaBounds(
            out float areaLeft,
            out float areaRight,
            out float areaBottom,
            out float areaTop
        );

        left =
            areaLeft +
            BallRadius;

        right =
            areaRight -
            BallRadius;

        bottom =
            areaBottom +
            BallRadius;

        top =
            areaTop -
            BallRadius;
    }


    /*
     * ========================================
     * AREA BOUNDS
     * ========================================
     */

    private void GetAreaBounds(
        out float left,
        out float right,
        out float bottom,
        out float top)
    {
        Vector2 center =
            transform.position;

        float halfWidth =
            AreaWidth * 0.5f;

        float halfHeight =
            AreaHeight * 0.5f;

        left =
            center.x -
            halfWidth;

        right =
            center.x +
            halfWidth;

        bottom =
            center.y -
            halfHeight;

        top =
            center.y +
            halfHeight;
    }


    /*
     * ========================================
     * RANDOM DIRECTION
     * ========================================
     */

    private Vector2 GetRandomDirection()
    {
        float angle =
            Random.Range(
                0f,
                Mathf.PI * 2f
            );

        return new Vector2(
            Mathf.Cos(angle),
            Mathf.Sin(angle)
        );
    }


    /*
     * ========================================
     * REMOVE DESTROYED BALLS
     * ========================================
     */

    private void RemoveDestroyedBalls()
    {
        for (int i = danceBalls.Count - 1;
             i >= 0;
             i--)
        {
            if (danceBalls[i].GameObject != null)
            {
                continue;
            }

            danceBalls.RemoveAt(
                i
            );
        }
    }


    /*
     * ========================================
     * DESTROY ALL
     * ========================================
     */

    private void DestroyBalls()
    {
        for (int i = 0;
             i < danceBalls.Count;
             i++)
        {
            GameObject ballObject =
                danceBalls[i].GameObject;

            if (ballObject != null)
            {
                Destroy(
                    ballObject
                );
            }
        }

        danceBalls.Clear();
    }


    /*
     * ========================================
     * VALIDATION
     * ========================================
     */

    private void OnValidate()
    {
        maximumBallCount =
            Mathf.Max(
                1,
                maximumBallCount
            );

        spawnInterval =
            Mathf.Max(
                0.01f,
                spawnInterval
            );

        spawnStartDelay =
            Mathf.Max(
                0f,
                spawnStartDelay
            );

        collisionsToEscape =
            Mathf.Max(
                1,
                collisionsToEscape
            );

        minimumMoveSpeed =
            Mathf.Max(
                0f,
                minimumMoveSpeed
            );

        maximumMoveSpeed =
            Mathf.Max(
                minimumMoveSpeed,
                maximumMoveSpeed
            );

        minimumRotationSpeed =
            Mathf.Max(
                0f,
                minimumRotationSpeed
            );

        maximumRotationSpeed =
            Mathf.Max(
                minimumRotationSpeed,
                maximumRotationSpeed
            );
    }


    /*
     * ========================================
     * GIZMOS
     * ========================================
     */

    private void OnDrawGizmos()
    {
        if (!showMoveArea)
        {
            return;
        }


        /*
         * Hình chữ nhật chính.
         */

        Gizmos.DrawWireCube(
            transform.position,
            new Vector3(
                AreaWidth,
                AreaHeight,
                0f
            )
        );


        /*
         * Giới hạn spawn / destroy bên ngoài.
         */

        float outsideExpansion =
            (
                BallRadius +
                OutsideOffset
            ) *
            2f;

        Gizmos.DrawWireCube(
            transform.position,
            new Vector3(
                AreaWidth +
                outsideExpansion,

                AreaHeight +
                outsideExpansion,

                0f
            )
        );
    }
}