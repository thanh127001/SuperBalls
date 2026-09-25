using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

[DisallowMultipleComponent]
public class BallController : MonoBehaviour
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
    private ScoreController scoreController;

    [SerializeField]
    private GameObject selectionCirclePrefab;


    /*
     * ========================================
     * SELECTION
     * ========================================
     */

    [Header("Selection")]

    private const string
        SelectableBallLayerName = "Ball";
    /*
     * ========================================
     * BOUNCE
     * ========================================
     */


    /*
     * Thời gian chờ riêng của Combo
     * trước khi bounce đồng loạt
     * toàn bộ Ball cùng loại.
     *
     * 0 = Combo bounce ngay.
     * > 0 = chờ đúng khoảng thời gian này,
     * sau đó toàn bộ Ball combo bounce cùng lúc.
     */
    private float comboBounceDelay = 0.2f;






    /*
     * ========================================
     * RUNTIME
     * ========================================
     */

    private Camera mainCamera;

    private bool isPlayable;
    private bool isSpawnable;

    private int selectableBallLayer =
        -1;

    private int selectableBallLayerMask;



    private ContactFilter2D
        selectionContactFilter;



    /*
     * ========================================
     * SELECTION BUFFERS
     * ========================================
     */

    private readonly List<Collider2D>
        overlapResults =
            new(32);


    private readonly List<Ball>
        connectedBalls =
            new(16);
    /*
     * Buffer dùng cho Combo.
     */
    private readonly List<Ball>
        comboBalls =
            new(64);


    /*
     * ========================================
     * UNITY
     * ========================================
     */

    private void Awake()
    {
        FindReferences();

        InitializeLayers();

        InitializeSelectionFilter();

    }


    private void OnEnable()
    {
        FindReferences();

        InitializeLayers();

        InitializeSelectionFilter();



        if (!Application.isPlaying ||
            gameManager == null)
        {
            return;
        }


        gameManager.OnGameStateChanged +=
            OnGameStateChanged;

        gameManager.OnPlayingChanged +=
            OnPlayingChanged;

        isPlayable = false;
        isSpawnable = false;

        if (scoreController != null)
        {
            scoreController.OnComboBounceRequested +=
                OnComboBounceRequested;
        }


        /*
         * Đồng bộ ngay với state hiện tại.
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

            gameManager.OnPlayingChanged -=
                OnPlayingChanged;
        }

        isPlayable = false;
        isSpawnable = false;

        if (scoreController != null)
        {
            scoreController.OnComboBounceRequested -=
                OnComboBounceRequested;
        }
    }


#if UNITY_EDITOR

    private void OnValidate()
    {
        comboBounceDelay =
            Mathf.Max(
                0f,
                comboBounceDelay
            );



        FindReferences();
    }

#endif


    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }



        if (gameManager == null ||
            !isPlayable)
        {
            return;
        }


        if (!EnsureMainCamera())
        {
            return;
        }


        /*
         * Touch được ưu tiên trước Mouse.
         */
        if (TryHandleTouchInput())
        {
            return;
        }


        TryHandleMouseInput();
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

        if (scoreController == null)
        {
            scoreController =
                FindFirstObjectByType<ScoreController>();
        }


        if (mainCamera == null)
        {
            mainCamera =
                Camera.main;
        }

    }


    private bool EnsureMainCamera()
    {
        if (mainCamera != null)
        {
            return true;
        }


        mainCamera =
            Camera.main;


        return mainCamera != null;
    }



    /*
     * ========================================
     * LAYERS
     * ========================================
     */

    private void InitializeLayers()
    {
        selectableBallLayer =
            LayerMask.NameToLayer(
                SelectableBallLayerName
            );


        if (selectableBallLayer < 0)
        {
            Debug.LogError(
                $"Không tìm thấy Layer " +
                $"'{SelectableBallLayerName}'.",
                this
            );

            selectableBallLayerMask = 0;
        }
        else
        {
            selectableBallLayerMask =
                1 << selectableBallLayer;
        }

    }


    private void InitializeSelectionFilter()
    {
        selectionContactFilter =
            new ContactFilter2D
            {
                useLayerMask = true,
                layerMask =
                    selectableBallLayerMask,

                useTriggers = false
            };
    }


    /*
     * ========================================
     * GAME STATE
     * ========================================
     */

    private void OnComboBounceRequested()
    {
        ScoreController.ComboBounceRequest request =
            scoreController != null
                ? scoreController.CurrentComboBounceRequest
                : null;

        if (request == null)
        {
            return;
        }


        List<Ball> targetBalls =
            GetBallsForCombo(
                request.BallType
            );


        int bouncedBallCount =
            targetBalls.Count;


        if (bouncedBallCount > 0)
        {
            BounceComboBalls(
                targetBalls
            );
        }


        request.ReportBouncedBallCount(
            bouncedBallCount
        );
    }


    private void OnPlayingChanged()
    {
        if (gameManager == null)
        {
            return;
        }

        isPlayable = gameManager.EffectivePlayable;
        isSpawnable = gameManager.EffectiveSpawnable;
    }


    private void OnGameStateChanged()
    {
        if (gameManager == null)
        {
            return;
        }

        switch (gameManager.State)
        {
            case GameState.Initializing:
            case GameState.Ready:

                DestroyAllBalls();

                break;


            case GameState.LevelCompleted:
            case GameState.GameCompleted:
            case GameState.GameOver:

                BounceAllBalls();

                break;


            case GameState.None:
            case GameState.Playing:
            case GameState.Paused:
            default:

                break;
        }
    }


    /*
     * ========================================
     * INPUT - TOUCH
     * ========================================
     */

    private bool TryHandleTouchInput()
    {
        Touchscreen touchscreen =
            Touchscreen.current;


        if (touchscreen == null)
        {
            return false;
        }


        TouchControl primaryTouch =
            touchscreen.primaryTouch;


        if (!primaryTouch.press
            .wasPressedThisFrame)
        {
            return false;
        }


        int touchId =
            primaryTouch.touchId
                .ReadValue();


        if (EventSystem.current != null &&
            EventSystem.current
                .IsPointerOverGameObject(
                    touchId
                ))
        {
            return true;
        }


        Vector2 screenPosition =
            primaryTouch.position
                .ReadValue();


        SelectBallAtScreenPosition(
            screenPosition
        );


        return true;
    }


    /*
     * ========================================
     * INPUT - MOUSE
     * ========================================
     */

    private void TryHandleMouseInput()
    {
        Mouse mouse =
            Mouse.current;


        if (mouse == null)
        {
            return;
        }


        if (!mouse.leftButton
            .wasPressedThisFrame)
        {
            return;
        }


        if (EventSystem.current != null &&
            EventSystem.current
                .IsPointerOverGameObject())
        {
            return;
        }


        Vector2 screenPosition =
            mouse.position.ReadValue();


        SelectBallAtScreenPosition(
            screenPosition
        );
    }


    /*
     * ========================================
     * SELECT FROM SCREEN POSITION
     * ========================================
     */

    private void SelectBallAtScreenPosition(
        Vector2 screenPosition)
    {
        if (gameManager == null ||
            !isPlayable)
        {
            return;
        }


        if (selectableBallLayerMask == 0 ||
            !EnsureMainCamera())
        {
            return;
        }


        Vector3 worldPoint =
            mainCamera.ScreenToWorldPoint(
                new Vector3(
                    screenPosition.x,
                    screenPosition.y,
                    Mathf.Abs(
                        mainCamera
                            .transform
                            .position
                            .z
                    )
                )
            );


        overlapResults.Clear();


        Physics2D.OverlapPoint(
            worldPoint,
            selectionContactFilter,
            overlapResults
        );


        for (int i = 0;
             i < overlapResults.Count;
             i++)
        {
            Collider2D hitCollider =
                overlapResults[i];


            Ball ball =
                GetBallFromCollider(
                    hitCollider
                );


            if (!CanBallReceiveClick(
                    ball))
            {
                continue;
            }


            SpawnSelectionCircle(
                ball
            );


            ball.OnClicked();


            return;
        }
    }


    /*
     * ========================================
     * SELECTION CIRCLE
     * ========================================
     */

    private void SpawnSelectionCircle(
        Ball ball)
    {
        if (ball == null ||
            selectionCirclePrefab == null)
        {
            return;
        }


        Instantiate(
            selectionCirclePrefab,
            ball.transform.position,
            Quaternion.identity
        );
    }


    private static bool IsPlayerBallType(
        BallType ballType)
    {
        return true;
    }


    private bool CanBallReceiveClick(
        Ball ball)
    {
        if (ball == null)
        {
            return false;
        }


        if (!IsPlayerBallType(
                ball.BallType))
        {
            return false;
        }


        if (!ball.IsSelectable)
        {
            return false;
        }


        return ball.gameObject.layer ==
               selectableBallLayer;
    }


    private static Ball GetBallFromCollider(
        Collider2D ballCollider)
    {
        if (ballCollider == null)
        {
            return null;
        }


        if (ballCollider.TryGetComponent(
                out Ball ball))
        {
            return ball;
        }


        return ballCollider
            .GetComponentInParent<Ball>();
    }


    /*
     * ========================================
     * SELECT BALL
     * ========================================
     */

    public void SelectBall(
        Ball centerBall)
    {
        if (gameManager == null ||
            !isPlayable)
        {
            return;
        }

        gameManager.RemoveNullBalls();

        if (!BallGroupFinder.TryGetValidGroup(
                centerBall,
                gameManager.Balls,
                connectedBalls))
        {
            HandleInvalidSelection();
            return;
        }

        if (!isPlayable)
        {
            ClearSelectionCollections();
            return;
        }

        int selectedBallCount =
            connectedBalls.Count;

        BallType selectedBallType =
            centerBall.BallType;

        BounceBalls(
            connectedBalls,
            0f
        );

        if (isPlayable &&
            scoreController != null)
        {
            scoreController.RegisterValidSelection(
                selectedBallType,
                selectedBallCount
            );
        }

        ClearSelectionCollections();
    }


    private void HandleInvalidSelection()
    {
        scoreController?
            .RegisterInvalidSelection();


        ClearSelectionCollections();
    }


    /*
     * ========================================
     * AVAILABLE GROUP
     * ========================================
     */

    public bool HasAvailableGroup()
    {
        if (gameManager == null ||
            !isPlayable)
        {
            return false;
        }

        gameManager.RemoveNullBalls();

        return BallGroupFinder.GetValidGroupCapacity(
            gameManager.Balls,
            1
        ) >= 1;
    }


    public List<BallType>
        GetAvailableBallTypes()
    {
        List<BallType> availableTypes =
            new();

        if (gameManager == null ||
            !isPlayable)
        {
            return availableTypes;
        }

        gameManager.RemoveNullBalls();

        HashSet<BallType> foundTypes =
            new();

        List<Ball> group =
            new(5);

        for (int i = 0;
             i < gameManager.Balls.Count;
             i++)
        {
            Ball centerBall =
                gameManager.Balls[i];

            if (!BallGroupFinder.TryGetValidGroup(
                    centerBall,
                    gameManager.Balls,
                    group))
            {
                continue;
            }

            if (foundTypes.Add(
                    centerBall.BallType))
            {
                availableTypes.Add(
                    centerBall.BallType
                );
            }
        }

        return availableTypes;
    }




    /*
     * ========================================
     * COMBO BALLS
     * ========================================
     */

    private List<Ball> GetBallsForCombo(
        BallType ballType)
    {
        if (!IsPlayerBallType(
                ballType))
        {
            return new List<Ball>();
        }


        gameManager?.RemoveNullBalls();


        comboBalls.Clear();


        for (int i = 0;
             i < gameManager.Balls.Count;
             i++)
        {
            Ball ball =
                gameManager.Balls[i];


            if (ball == null)
            {
                continue;
            }


            if (!ball.IsSelectable)
            {
                continue;
            }


            if (ball.gameObject.layer !=
                selectableBallLayer)
            {
                continue;
            }


            if (ball.BallType !=
                ballType)
            {
                continue;
            }


            comboBalls.Add(
                ball
            );
        }


        return new List<Ball>(
            comboBalls
        );
    }


    /*
     * ========================================
     * BOUNCE BALLS
     * ========================================
     */

    public void BounceBalls(
        IList<Ball> targetBalls,
        float delay = 0f)
    {
        if (targetBalls == null ||
            targetBalls.Count == 0)
        {
            return;
        }



        Ball[] snapshot =
            new Ball[targetBalls.Count];


        for (int i = 0;
             i < targetBalls.Count;
             i++)
        {
            snapshot[i] =
                targetBalls[i];
        }


        float safeDelay =
            Mathf.Max(
                0f,
                delay
            );


        if (safeDelay <= 0f)
        {
            BounceBallSnapshot(
                snapshot
            );

            return;
        }


        StartCoroutine(
            BounceBallsRoutine(
                snapshot,
                safeDelay
            )
        );
    }


    private void BounceComboBalls(
        IList<Ball> targetBalls)
    {
        BounceBalls(
            targetBalls,
            comboBounceDelay
        );
    }





    private IEnumerator BounceBallsRoutine(
        Ball[] targetBalls,
        float delay)
    {
        yield return
            new WaitForSeconds(
                delay
            );


        BounceBallSnapshot(
            targetBalls
        );
    }


    private void BounceBallSnapshot(
        Ball[] targetBalls)
    {
        if (targetBalls == null ||
            targetBalls.Length == 0)
        {
            return;
        }


        for (int i = 0;
             i < targetBalls.Length;
             i++)
        {
            BeginBallBouncing(
                targetBalls[i]
            );
        }
    }


    private void BeginBallBouncing(
        Ball ball)
    {
        if (ball == null ||
            gameManager == null)
        {
            return;
        }


        if (ball.gameObject.layer !=
            selectableBallLayer)
        {
            return;
        }


        if (!gameManager.ContainsBall(ball))
        {
            return;
        }


        /*
         * Ball rời gameplay ngay khi bắt đầu bounce.
         * Từ thời điểm này Ball không còn nằm trong
         * GameManager.Balls và không còn được tính
         * cho BallCount / Phase / Selection / Anti-Stuck.
         */
        gameManager.UnregisterBall(
            ball
        );


        /*
         * BallController chỉ quyết định Ball nào cần bounce.
         * Ball tự xác định direction, force và thời điểm disable collider.
         */
        ball.BeginBouncing();
    }


    /*
     * ========================================
     * BOUNCE ALL
     * ========================================
     */

    public void BounceAllBalls()
    {
        if (gameManager == null)
        {
            return;
        }


        gameManager.RemoveNullBalls();


        /*
         * Snapshot bắt buộc vì BeginBallBouncing()
         * sẽ UnregisterBall() ngay lập tức.
         */
        Ball[] snapshot =
            new Ball[gameManager.Balls.Count];


        for (int i = 0;
             i < snapshot.Length;
             i++)
        {
            snapshot[i] =
                gameManager.Balls[i];
        }


        BounceBallSnapshot(
            snapshot
        );
    }


    /*
     * ========================================
     * DESTROY ALL
     * ========================================
     */

    /*
     * ========================================
     * DESTROY ALL
     * ========================================
     */

    public void DestroyAllBalls()
    {
        if (gameManager != null)
        {
            Ball[] snapshot =
                new Ball[gameManager.Balls.Count];

            for (int i = 0; i < snapshot.Length; i++)
            {
                snapshot[i] = gameManager.Balls[i];
            }

            for (int i = snapshot.Length - 1; i >= 0; i--)
            {
                Ball ball = snapshot[i];

                if (ball == null)
                {
                    continue;
                }

                gameManager.UnregisterBall(ball);
                Destroy(ball.gameObject);
            }
        }

        ClearSelectionCollections();
        comboBalls.Clear();
    }


    /*
     * ========================================
     * COLLECTION
     * ========================================
     */

    private void ClearSelectionCollections()
    {
        connectedBalls.Clear();

        overlapResults.Clear();
    }

}

