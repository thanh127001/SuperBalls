using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[ExecuteAlways]
[DisallowMultipleComponent]
public class ScreenManager : MonoBehaviour
{
    /*
     * ========================================
     * PLAY AREA SETTINGS
     * ========================================
     */

    /*
     * 100% cấu hình bằng code.
     *
     * Muốn thay đổi kích thước Play Area
     * chỉ cần sửa hai giá trị này.
     *
     * Sau khi Unity compile lại,
     * ExecuteAlways sẽ tự cập nhật Camera,
     * Screen bounds và các Anchor.
     */

    private const float PlayAreaWidthValue = 6.6f;
    private const float PlayAreaHeightValue = 13f;


    /*
     * ========================================
     * CAMERA
     * ========================================
     */

    [Header("Camera")]
    [SerializeField]
    private Camera mainCamera;


    /*
     * ========================================
     * ANCHOR NAMES
     * ========================================
     */

    private const string PlayAreaTopAnchorName =
        "PlayArea_TopAnchor";

    private const string PlayAreaBottomAnchorName =
        "PlayArea_BottomAnchor";

    private const string PlayAreaLeftAnchorName =
        "PlayArea_LeftAnchor";

    private const string PlayAreaRightAnchorName =
        "PlayArea_RightAnchor";

    private const string ScreenTopAnchorName =
        "Screen_TopAnchor";

    private const string ScreenBottomAnchorName =
        "Screen_BottomAnchor";

    private const string ScreenLeftAnchorName =
        "Screen_LeftAnchor";

    private const string ScreenRightAnchorName =
        "Screen_RightAnchor";


    /*
     * ========================================
     * ANCHORS
     * ========================================
     */

    private Transform topAnchor;
    private Transform bottomAnchor;
    private Transform leftAnchor;
    private Transform rightAnchor;

    private Transform screenTopAnchor;
    private Transform screenBottomAnchor;
    private Transform screenLeftAnchor;
    private Transform screenRightAnchor;


    /*
     * ========================================
     * EVENTS
     * ========================================
     */

    public event Action OnLayoutChanged;


    /*
     * ========================================
     * CACHED VALID LAYOUT
     * ========================================
     */

    private Vector2 lastValidCenter;

    private float lastValidScreenWidth;
    private float lastValidScreenHeight;

    private Vector2 lastValidScreenCenter;

    private bool hasValidLayout;


    /*
     * ========================================
     * PREVIOUS COMMITTED LAYOUT
     * ========================================
     */

    private Vector2 previousCommittedCenter;

    private float previousCommittedPlayAreaWidth;
    private float previousCommittedPlayAreaHeight;

    private float previousCommittedScreenWidth;
    private float previousCommittedScreenHeight;

    private Vector2 previousCommittedScreenCenter;

    private bool hasCommittedLayout;


    /*
     * ========================================
     * PUBLIC - PLAY AREA
     * ========================================
     */

    public Vector2 Center
    {
        get
        {
            if (hasValidLayout)
            {
                return lastValidCenter;
            }

            Vector3 position =
                transform.position;

            if (IsFinite(position.x) &&
                IsFinite(position.y))
            {
                return new Vector2(
                    position.x,
                    position.y
                );
            }

            return Vector2.zero;
        }
    }


    /*
     * Không lấy từ cached instance field.
     *
     * Giá trị này luôn đến trực tiếp
     * từ configuration trong code.
     */

    public float PlayAreaWidth =>
        PlayAreaWidthValue;


    public float PlayAreaHeight =>
        PlayAreaHeightValue;


    public float Aspect =>
        PlayAreaWidth /
        PlayAreaHeight;


    public float PlayAreaLeft =>
        Center.x -
        PlayAreaWidth * 0.5f;


    public float PlayAreaRight =>
        Center.x +
        PlayAreaWidth * 0.5f;


    public float PlayAreaBottom =>
        Center.y -
        PlayAreaHeight * 0.5f;


    public float PlayAreaTop =>
        Center.y +
        PlayAreaHeight * 0.5f;


    /*
     * ========================================
     * PUBLIC - SCREEN
     * ========================================
     */

    public Vector2 ScreenCenter =>
        hasValidLayout
            ? lastValidScreenCenter
            : Center;


    public float ScreenWidth =>
        hasValidLayout
            ? lastValidScreenWidth
            : 0f;


    public float ScreenHeight =>
        hasValidLayout
            ? lastValidScreenHeight
            : 0f;


    public float ScreenLeft =>
        ScreenCenter.x -
        ScreenWidth * 0.5f;


    public float ScreenRight =>
        ScreenCenter.x +
        ScreenWidth * 0.5f;


    public float ScreenBottom =>
        ScreenCenter.y -
        ScreenHeight * 0.5f;


    public float ScreenTop =>
        ScreenCenter.y +
        ScreenHeight * 0.5f;


    /*
     * ========================================
     * PUBLIC - ANCHORS
     * ========================================
     */

    public Transform TopAnchor =>
        topAnchor;


    public Transform BottomAnchor =>
        bottomAnchor;


    public Transform LeftAnchor =>
        leftAnchor;


    public Transform RightAnchor =>
        rightAnchor;


    public Transform ScreenTopAnchor =>
        screenTopAnchor;


    public Transform ScreenBottomAnchor =>
        screenBottomAnchor;


    public Transform ScreenLeftAnchor =>
        screenLeftAnchor;


    public Transform ScreenRightAnchor =>
        screenRightAnchor;


    /*
     * ========================================
     * PUBLIC - STATUS
     * ========================================
     */

    public bool HasValidLayout =>
        hasValidLayout;


    /*
     * ========================================
     * UNITY
     * ========================================
     */

    private void Awake()
    {
        FindReferences();
        InitializeAnchors();
        UpdateLayout();
    }


    private void OnEnable()
    {
        FindReferences();
        InitializeAnchors();
        UpdateLayout();
    }


    private void LateUpdate()
    {
        UpdateLayout();
    }


#if UNITY_EDITOR

    private void OnValidate()
    {
        FindReferences();
        InitializeAnchors();
        UpdateLayout();
    }

#endif


    /*
     * ========================================
     * REFERENCES
     * ========================================
     */

    private void FindReferences()
    {
        if (mainCamera == null)
        {
            mainCamera =
                Camera.main;
        }
    }


    /*
     * ========================================
     * LAYOUT
     * ========================================
     */

    private void UpdateLayout()
    {
        InitializeAnchors();


        /*
         * ScreenManager là nơi duy nhất
         * quản lý resize và vị trí cơ sở
         * của Main Camera.
         */

        if (!ConfigureMainCamera())
        {
            RestoreAnchorsToLastValidLayout();

            return;
        }


        if (!TryBuildValidLayout(
                out LayoutData layout))
        {
            RestoreAnchorsToLastValidLayout();

            return;
        }


        CommitLayout(
            layout
        );
    }


    /*
     * ========================================
     * BUILD LAYOUT
     * ========================================
     */

    private bool TryBuildValidLayout(
        out LayoutData layout)
    {
        layout =
            default;


        /*
         * ========================================
         * PLAY AREA
         * ========================================
         */

        Vector3 position =
            transform.position;


        if (!IsFiniteVector3(
                position))
        {
            return false;
        }


        if (!IsFinitePositive(
                PlayAreaWidth) ||
            !IsFinitePositive(
                PlayAreaHeight))
        {
            return false;
        }


        Vector2 calculatedCenter =
            new Vector2(
                position.x,
                position.y
            );


        /*
         * ========================================
         * CAMERA / SCREEN
         * ========================================
         */

        if (!EnsureMainCamera())
        {
            return false;
        }


        if (!mainCamera.orthographic)
        {
            return false;
        }


        float orthographicSize =
            mainCamera.orthographicSize;


        float cameraAspect =
            mainCamera.aspect;


        if (!IsFinitePositive(
                orthographicSize) ||
            !IsFinitePositive(
                cameraAspect))
        {
            return false;
        }


        float calculatedScreenHeight =
            orthographicSize *
            2f;


        float calculatedScreenWidth =
            calculatedScreenHeight *
            cameraAspect;


        if (!IsFinitePositive(
                calculatedScreenWidth) ||
            !IsFinitePositive(
                calculatedScreenHeight))
        {
            return false;
        }


        Vector3 cameraPosition =
            mainCamera.transform.position;


        if (!IsFiniteVector3(
                cameraPosition))
        {
            return false;
        }


        Vector2 calculatedScreenCenter =
            new Vector2(
                cameraPosition.x,
                cameraPosition.y
            );


        /*
         * ========================================
         * FINAL VALIDATION
         * ========================================
         */

        float playAreaLeft =
            calculatedCenter.x -
            PlayAreaWidth * 0.5f;


        float playAreaRight =
            calculatedCenter.x +
            PlayAreaWidth * 0.5f;


        float playAreaBottom =
            calculatedCenter.y -
            PlayAreaHeight * 0.5f;


        float playAreaTop =
            calculatedCenter.y +
            PlayAreaHeight * 0.5f;


        float screenLeft =
            calculatedScreenCenter.x -
            calculatedScreenWidth * 0.5f;


        float screenRight =
            calculatedScreenCenter.x +
            calculatedScreenWidth * 0.5f;


        float screenBottom =
            calculatedScreenCenter.y -
            calculatedScreenHeight * 0.5f;


        float screenTop =
            calculatedScreenCenter.y +
            calculatedScreenHeight * 0.5f;


        if (!IsFinite(playAreaLeft) ||
            !IsFinite(playAreaRight) ||
            !IsFinite(playAreaBottom) ||
            !IsFinite(playAreaTop) ||
            !IsFinite(screenLeft) ||
            !IsFinite(screenRight) ||
            !IsFinite(screenBottom) ||
            !IsFinite(screenTop))
        {
            return false;
        }


        layout =
            new LayoutData
            {
                Center =
                    calculatedCenter,

                PlayAreaWidth =
                    PlayAreaWidth,

                PlayAreaHeight =
                    PlayAreaHeight,

                ScreenCenter =
                    calculatedScreenCenter,

                ScreenWidth =
                    calculatedScreenWidth,

                ScreenHeight =
                    calculatedScreenHeight
            };


        return true;
    }


    /*
     * ========================================
     * COMMIT
     * ========================================
     */

    private void CommitLayout(
        LayoutData layout)
    {
        lastValidCenter =
            layout.Center;

        lastValidScreenCenter =
            layout.ScreenCenter;

        lastValidScreenWidth =
            layout.ScreenWidth;

        lastValidScreenHeight =
            layout.ScreenHeight;


        hasValidLayout =
            true;


        UpdatePlayAreaAnchors(
            layout
        );


        UpdateScreenAnchors(
            layout
        );


        /*
         * Chỉ phát event khi layout
         * thực sự thay đổi.
         */

        if (!HasLayoutChanged(
                layout))
        {
            return;
        }


        StoreCommittedLayout(
            layout
        );


        OnLayoutChanged?.Invoke();
    }


    /*
     * ========================================
     * LAYOUT CHANGE CHECK
     * ========================================
     */

    private bool HasLayoutChanged(
        LayoutData layout)
    {
        if (!hasCommittedLayout)
        {
            return true;
        }


        const float epsilon =
            0.0001f;


        if (
            (
                layout.Center -
                previousCommittedCenter
            ).sqrMagnitude >
            epsilon * epsilon)
        {
            return true;
        }


        if (
            (
                layout.ScreenCenter -
                previousCommittedScreenCenter
            ).sqrMagnitude >
            epsilon * epsilon)
        {
            return true;
        }


        if (!Mathf.Approximately(
                layout.PlayAreaWidth,
                previousCommittedPlayAreaWidth))
        {
            return true;
        }


        if (!Mathf.Approximately(
                layout.PlayAreaHeight,
                previousCommittedPlayAreaHeight))
        {
            return true;
        }


        if (!Mathf.Approximately(
                layout.ScreenWidth,
                previousCommittedScreenWidth))
        {
            return true;
        }


        if (!Mathf.Approximately(
                layout.ScreenHeight,
                previousCommittedScreenHeight))
        {
            return true;
        }


        return false;
    }


    private void StoreCommittedLayout(
        LayoutData layout)
    {
        previousCommittedCenter =
            layout.Center;

        previousCommittedPlayAreaWidth =
            layout.PlayAreaWidth;

        previousCommittedPlayAreaHeight =
            layout.PlayAreaHeight;

        previousCommittedScreenCenter =
            layout.ScreenCenter;

        previousCommittedScreenWidth =
            layout.ScreenWidth;

        previousCommittedScreenHeight =
            layout.ScreenHeight;

        hasCommittedLayout =
            true;
    }


    /*
     * ========================================
     * CAMERA
     * ========================================
     */

    private bool ConfigureMainCamera()
    {
        if (!EnsureMainCamera())
        {
            return false;
        }


        if (!mainCamera.orthographic)
        {
            mainCamera.orthographic =
                true;
        }


        Rect fullScreenRect =
            new Rect(
                0f,
                0f,
                1f,
                1f
            );


        if (mainCamera.rect !=
            fullScreenRect)
        {
            mainCamera.rect =
                fullScreenRect;
        }


        float cameraAspect =
            mainCamera.aspect;


        if (!IsFinitePositive(
                cameraAspect))
        {
            return false;
        }


        /*
         * Camera phải chứa toàn bộ
         * Play Area theo cả Width và Height.
         */

        float targetSizeByHeight =
            PlayAreaHeight *
            0.5f;


        float targetSizeByWidth =
            PlayAreaWidth /
            (
                2f *
                cameraAspect
            );


        float targetOrthographicSize =
            Mathf.Max(
                targetSizeByHeight,
                targetSizeByWidth
            );


        if (!IsFinitePositive(
                targetOrthographicSize))
        {
            return false;
        }


        if (!Mathf.Approximately(
                mainCamera.orthographicSize,
                targetOrthographicSize))
        {
            mainCamera.orthographicSize =
                targetOrthographicSize;
        }


        Vector3 playAreaPosition =
            transform.position;


        Vector3 cameraPosition =
            mainCamera.transform.position;


        if (!IsFiniteVector3(
                playAreaPosition) ||
            !IsFiniteVector3(
                cameraPosition))
        {
            return false;
        }


        Vector3 targetCameraPosition =
            new Vector3(
                playAreaPosition.x,
                playAreaPosition.y,
                cameraPosition.z
            );


        if (
            (
                cameraPosition -
                targetCameraPosition
            ).sqrMagnitude >
            0.00000001f)
        {
            mainCamera.transform.position =
                targetCameraPosition;
        }


        return true;
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
     * INITIALIZE ANCHORS
     * ========================================
     */

    private void InitializeAnchors()
    {
        topAnchor =
            GetOrCreateAnchor(
                topAnchor,
                PlayAreaTopAnchorName
            );


        bottomAnchor =
            GetOrCreateAnchor(
                bottomAnchor,
                PlayAreaBottomAnchorName
            );


        leftAnchor =
            GetOrCreateAnchor(
                leftAnchor,
                PlayAreaLeftAnchorName
            );


        rightAnchor =
            GetOrCreateAnchor(
                rightAnchor,
                PlayAreaRightAnchorName
            );


        screenTopAnchor =
            GetOrCreateAnchor(
                screenTopAnchor,
                ScreenTopAnchorName
            );


        screenBottomAnchor =
            GetOrCreateAnchor(
                screenBottomAnchor,
                ScreenBottomAnchorName
            );


        screenLeftAnchor =
            GetOrCreateAnchor(
                screenLeftAnchor,
                ScreenLeftAnchorName
            );


        screenRightAnchor =
            GetOrCreateAnchor(
                screenRightAnchor,
                ScreenRightAnchorName
            );
    }


    /*
     * ========================================
     * GET OR CREATE ANCHOR
     * ========================================
     */

    private Transform GetOrCreateAnchor(
        Transform currentAnchor,
        string anchorName)
    {
        Scene currentScene =
            gameObject.scene;


        if (!currentScene.IsValid() ||
            !currentScene.isLoaded)
        {
            return currentAnchor;
        }


        if (currentAnchor != null)
        {
            EnsureAnchorAtSceneRoot(
                currentAnchor,
                currentScene
            );

            return currentAnchor;
        }


        Transform childAnchor =
            transform.Find(
                anchorName
            );


        if (childAnchor != null)
        {
            EnsureAnchorAtSceneRoot(
                childAnchor,
                currentScene
            );

            return childAnchor;
        }


        GameObject[] rootObjects =
            currentScene.GetRootGameObjects();


        for (int i = 0;
             i < rootObjects.Length;
             i++)
        {
            GameObject rootObject =
                rootObjects[i];


            if (rootObject == null ||
                rootObject.name !=
                anchorName)
            {
                continue;
            }


            return rootObject.transform;
        }


        GameObject anchorObject =
            new GameObject(
                anchorName
            );


        if (anchorObject.scene !=
            currentScene)
        {
            SceneManager.MoveGameObjectToScene(
                anchorObject,
                currentScene
            );
        }


        Transform anchorTransform =
            anchorObject.transform;


        anchorTransform.SetParent(
            null,
            true
        );


        anchorTransform.position =
            Vector3.zero;


        anchorTransform.rotation =
            Quaternion.identity;


        anchorTransform.localScale =
            Vector3.one;


        return anchorTransform;
    }


    private static void EnsureAnchorAtSceneRoot(
        Transform anchor,
        Scene targetScene)
    {
        if (anchor == null)
        {
            return;
        }


        if (anchor.parent != null)
        {
            anchor.SetParent(
                null,
                true
            );
        }


        Scene anchorScene =
            anchor.gameObject.scene;


        if (!anchorScene.IsValid() ||
            anchorScene ==
            targetScene)
        {
            return;
        }


        SceneManager.MoveGameObjectToScene(
            anchor.gameObject,
            targetScene
        );
    }


    /*
     * ========================================
     * UPDATE PLAY AREA ANCHORS
     * ========================================
     */

    private void UpdatePlayAreaAnchors(
        LayoutData layout)
    {
        if (topAnchor == null ||
            bottomAnchor == null ||
            leftAnchor == null ||
            rightAnchor == null)
        {
            return;
        }


        float halfWidth =
            layout.PlayAreaWidth *
            0.5f;


        float halfHeight =
            layout.PlayAreaHeight *
            0.5f;


        float left =
            layout.Center.x -
            halfWidth;


        float right =
            layout.Center.x +
            halfWidth;


        float bottom =
            layout.Center.y -
            halfHeight;


        float top =
            layout.Center.y +
            halfHeight;


        UpdateAnchorPosition(
            topAnchor,
            layout.Center.x,
            top
        );


        UpdateAnchorPosition(
            bottomAnchor,
            layout.Center.x,
            bottom
        );


        UpdateAnchorPosition(
            leftAnchor,
            left,
            layout.Center.y
        );


        UpdateAnchorPosition(
            rightAnchor,
            right,
            layout.Center.y
        );
    }


    /*
     * ========================================
     * UPDATE SCREEN ANCHORS
     * ========================================
     */

    private void UpdateScreenAnchors(
        LayoutData layout)
    {
        if (screenTopAnchor == null ||
            screenBottomAnchor == null ||
            screenLeftAnchor == null ||
            screenRightAnchor == null)
        {
            return;
        }


        float halfWidth =
            layout.ScreenWidth *
            0.5f;


        float halfHeight =
            layout.ScreenHeight *
            0.5f;


        float left =
            layout.ScreenCenter.x -
            halfWidth;


        float right =
            layout.ScreenCenter.x +
            halfWidth;


        float bottom =
            layout.ScreenCenter.y -
            halfHeight;


        float top =
            layout.ScreenCenter.y +
            halfHeight;


        UpdateAnchorPosition(
            screenTopAnchor,
            layout.ScreenCenter.x,
            top
        );


        UpdateAnchorPosition(
            screenBottomAnchor,
            layout.ScreenCenter.x,
            bottom
        );


        UpdateAnchorPosition(
            screenLeftAnchor,
            left,
            layout.ScreenCenter.y
        );


        UpdateAnchorPosition(
            screenRightAnchor,
            right,
            layout.ScreenCenter.y
        );
    }


    /*
     * ========================================
     * RESTORE LAST VALID ANCHORS
     * ========================================
     */

    private void RestoreAnchorsToLastValidLayout()
    {
        if (!hasValidLayout)
        {
            return;
        }


        LayoutData layout =
            new LayoutData
            {
                Center =
                    lastValidCenter,

                PlayAreaWidth =
                    PlayAreaWidth,

                PlayAreaHeight =
                    PlayAreaHeight,

                ScreenCenter =
                    lastValidScreenCenter,

                ScreenWidth =
                    lastValidScreenWidth,

                ScreenHeight =
                    lastValidScreenHeight
            };


        UpdatePlayAreaAnchors(
            layout
        );


        UpdateScreenAnchors(
            layout
        );
    }


    /*
     * ========================================
     * UPDATE ANCHOR POSITION
     * ========================================
     */

    private static void UpdateAnchorPosition(
        Transform anchor,
        float positionX,
        float positionY)
    {
        if (anchor == null)
        {
            return;
        }


        if (!IsFinite(positionX) ||
            !IsFinite(positionY))
        {
            return;
        }


        Vector3 oldPosition =
            anchor.position;


        if (!IsFinite(
                oldPosition.z))
        {
            oldPosition.z =
                0f;
        }


        Vector3 newPosition =
            new Vector3(
                positionX,
                positionY,
                oldPosition.z
            );


        if (!IsFiniteVector3(
                newPosition))
        {
            return;
        }


        if (
            (
                oldPosition -
                newPosition
            ).sqrMagnitude <=
            0.00000001f)
        {
            return;
        }


        anchor.position =
            newPosition;
    }


    /*
     * ========================================
     * FORCE REFRESH
     * ========================================
     */

    public void RefreshLayout()
    {
        FindReferences();

        InitializeAnchors();

        UpdateLayout();
    }


    /*
     * ========================================
     * VALIDATION HELPERS
     * ========================================
     */

    private static bool IsFinite(
        float value)
    {
        return
            !float.IsNaN(value) &&
            !float.IsInfinity(value);
    }


    private static bool IsFinitePositive(
        float value)
    {
        return
            IsFinite(value) &&
            value >
            Mathf.Epsilon;
    }


    private static bool IsFiniteVector3(
        Vector3 value)
    {
        return
            IsFinite(value.x) &&
            IsFinite(value.y) &&
            IsFinite(value.z);
    }


    /*
     * ========================================
     * LAYOUT DATA
     * ========================================
     */

    private struct LayoutData
    {
        public Vector2 Center;

        public float PlayAreaWidth;
        public float PlayAreaHeight;

        public Vector2 ScreenCenter;

        public float ScreenWidth;
        public float ScreenHeight;
    }
}