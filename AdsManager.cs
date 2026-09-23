using UnityEngine;
using Unity.Services.LevelPlay;

[DisallowMultipleComponent]
public class AdsManager : MonoBehaviour
{
    /*
     * ========================================
     * SINGLETON
     * ========================================
     */

    public static AdsManager Instance
    {
        get;
        private set;
    }


    /*
     * ========================================
     * REFERENCES
     * ========================================
     */

    [Header("References")]

    [SerializeField]
    private GameManager gameManager;

    [Tooltip("Panel phủ lên UI khi Interstitial Ads đang được yêu cầu hiển thị.")]
    [SerializeField]
    private GameObject adsPanel;


    /*
     * ========================================
     * SETTINGS
     * ========================================
     */

    // Thời gian thử tải lại ad.
    private float retryLoadInterval = 30f;

    // Thời gian chờ trước khi hiện ad.
    private float adShowDelay = 5f;

    [SerializeField]
    private bool showDebugLogs = true;


    /*
     * ========================================
     * LEVELPLAY
     * ========================================
     */

    [Header("LevelPlay")]

    [SerializeField]
    private string appKey;

    [SerializeField]
    private string interstitialAdUnitId;


    /*
     * ========================================
     * RUNTIME
     * ========================================
     */

    private LevelPlayInterstitialAd interstitialAd;

    private bool sdkInitRequested;
    private bool sdkInitSucceeded;

    private bool isLoading;
    private bool isShowing;


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

        DontDestroyOnLoad(gameObject);

        HideAdsPanel();
    }


    private void OnEnable()
    {
        LevelPlay.OnInitSuccess +=
            HandleInitSuccess;

        LevelPlay.OnInitFailed +=
            HandleInitFailed;

        BindGameManager();
    }


    private void Start()
    {
        InitializeAds();
    }


    private void OnDisable()
    {
        LevelPlay.OnInitSuccess -=
            HandleInitSuccess;

        LevelPlay.OnInitFailed -=
            HandleInitFailed;

        UnbindGameManager();

        CancelInvoke();

        isShowing =
            false;

        HideAdsPanel();
    }


    private void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }

        UnsubscribeInterstitialEvents();

        Instance = null;
    }


#if UNITY_EDITOR

    private void OnValidate()
    {

        retryLoadInterval =
            Mathf.Max(
                1f,
                retryLoadInterval
            );

        adShowDelay =
            Mathf.Max(
                0f,
                adShowDelay
            );
    }

#endif


    /*
     * ========================================
     * GAME MANAGER
     * ========================================
     */

    private void BindGameManager()
    {
        if (gameManager == null)
        {
            gameManager =
                FindFirstObjectByType<GameManager>();
        }

        if (gameManager == null)
        {
            LogWarning(
                "GameManager not found."
            );

            return;
        }

        gameManager.OnGameStateChanged -=
            HandleGameStateChanged;

        gameManager.OnGameStateChanged +=
            HandleGameStateChanged;

        Log(
            "Bound to GameManager."
        );
    }


    private void UnbindGameManager()
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.OnGameStateChanged -=
            HandleGameStateChanged;
    }


    private void HandleGameStateChanged()
    {
        if (gameManager == null) return;

        GameState state = gameManager.State;

        Log(
            $"GameState changed: {state}"
        );

        switch (state)
        {
            case GameState.LevelCompleted:

                /*
                 * Level 1 không hiện quảng cáo
                 * khi hoàn thành level.
                 *
                 * Bắt đầu thử hiện quảng cáo từ
                 * LevelCompleted của Level 2 trở đi.
                 */
                if (gameManager != null &&
                    gameManager.CurrentLevel >= 2)
                {
                    TryShowInterstitial();
                }
                else
                {
                    Log(
                        "Ad skipped: Level 1 completed."
                    );
                }

                break;


            case GameState.GameCompleted:

                TryShowInterstitial();

                break;


            case GameState.GameOver:

                TryShowInterstitial();

                break;
        }
    }


    /*
     * ========================================
     * INITIALIZE
     * ========================================
     */

    private void InitializeAds()
    {
        if (sdkInitRequested)
        {
            return;
        }

        sdkInitRequested =
            true;

#if UNITY_EDITOR

        /*
         * Unity LevelPlay Preview Mock Ads.
         *
         * Unity's current mock-ad documentation
         * uses "editor" as the App Key.
         */
        Log(
            "Initializing LevelPlay Editor Mock Ads..."
        );

        LevelPlay.Init(
            "editor"
        );


        /*
         * IMPORTANT:
         *
         * In the Editor we create the mock
         * Interstitial immediately as well.
         *
         * This avoids depending entirely on
         * OnInitSuccess when LevelPlay was
         * already initialized earlier by
         * Mediation Settings / editor tooling.
         *
         * LoadAd() will simply fail/skip and
         * retry if the mock platform is not
         * ready yet.
         */
        CreateInterstitialAd();

#else

        if (string.IsNullOrWhiteSpace(
                appKey))
        {
            sdkInitRequested =
                false;

            LogWarning(
                "App Key is empty."
            );

            return;
        }

        Log(
            "Initializing LevelPlay..."
        );

        LevelPlay.Init(
            appKey
        );

#endif
    }


    private void HandleInitSuccess(
        LevelPlayConfiguration configuration)
    {
        sdkInitSucceeded =
            true;

        Log(
            "LevelPlay INIT SUCCESS."
        );

        CreateInterstitialAd();

        LoadInterstitial();
    }


    private void HandleInitFailed(
        LevelPlayInitError error)
    {
        sdkInitSucceeded =
            false;

        LogWarning(
            $"LevelPlay INIT FAILED: {error}"
        );

        /*
         * Không block gameplay.
         *
         * Khi init lỗi thì ads ở lần này
         * đơn giản sẽ không xuất hiện.
         */
    }


    /*
     * ========================================
     * CREATE INTERSTITIAL
     * ========================================
     */

    private void CreateInterstitialAd()
    {
        if (interstitialAd != null)
        {
            return;
        }

#if UNITY_EDITOR

        interstitialAd =
            new LevelPlayInterstitialAd(
                "editor_interstitial"
            );

#else

        if (string.IsNullOrWhiteSpace(
                interstitialAdUnitId))
        {
            LogWarning(
                "Interstitial Ad Unit ID is empty."
            );

            return;
        }

        interstitialAd =
            new LevelPlayInterstitialAd(
                interstitialAdUnitId
            );

#endif

        SubscribeInterstitialEvents();

        Log(
            "Interstitial object created."
        );


#if UNITY_EDITOR

        /*
         * Với Editor Mock Ads, thử preload ngay.
         */
        LoadInterstitial();

#endif
    }


    /*
     * ========================================
     * INTERSTITIAL EVENTS
     * ========================================
     */

    private void SubscribeInterstitialEvents()
    {
        if (interstitialAd == null)
        {
            return;
        }

        interstitialAd.OnAdLoaded +=
            HandleAdLoaded;

        interstitialAd.OnAdLoadFailed +=
            HandleAdLoadFailed;

        interstitialAd.OnAdDisplayed +=
            HandleAdDisplayed;

        interstitialAd.OnAdDisplayFailed +=
            HandleAdDisplayFailed;

        interstitialAd.OnAdClosed +=
            HandleAdClosed;
    }


    private void UnsubscribeInterstitialEvents()
    {
        if (interstitialAd == null)
        {
            return;
        }

        interstitialAd.OnAdLoaded -=
            HandleAdLoaded;

        interstitialAd.OnAdLoadFailed -=
            HandleAdLoadFailed;

        interstitialAd.OnAdDisplayed -=
            HandleAdDisplayed;

        interstitialAd.OnAdDisplayFailed -=
            HandleAdDisplayFailed;

        interstitialAd.OnAdClosed -=
            HandleAdClosed;
    }


    /*
     * ========================================
     * LOAD
     * ========================================
     */

    private void LoadInterstitial()
    {
        if (interstitialAd == null)
        {
            return;
        }

        if (isLoading ||
            isShowing)
        {
            return;
        }

        if (interstitialAd.IsAdReady())
        {
            Log(
                "Interstitial already READY."
            );

            return;
        }

        CancelInvoke(
            nameof(RetryLoadInterstitial)
        );

        isLoading =
            true;

        Log(
            "Loading Interstitial..."
        );

        interstitialAd.LoadAd();
    }


    private void RetryLoadInterstitial()
    {
        LoadInterstitial();
    }


    private void HandleAdLoaded(
        LevelPlayAdInfo adInfo)
    {
        isLoading =
            false;

        CancelInvoke(
            nameof(RetryLoadInterstitial)
        );

        Log(
            "Interstitial READY."
        );
    }


    private void HandleAdLoadFailed(
        LevelPlayAdError error)
    {
        isLoading =
            false;

        LogWarning(
            $"Interstitial LOAD FAILED: {error}"
        );

        /*
         * Không ảnh hưởng gameplay.
         * Chỉ thử preload lại sau.
         */
        CancelInvoke(
            nameof(RetryLoadInterstitial)
        );

        Invoke(
            nameof(RetryLoadInterstitial),
            retryLoadInterval
        );
    }


    /*
     * ========================================
     * SHOW
     * ========================================
     */

    private void TryShowInterstitial()
    {
        if (!CanShowAds())
        {
            Log(
                "Ad skipped: purchase state does not allow ads."
            );

            return;
        }

        if (isShowing)
        {
            Log(
                "Ad skipped: already showing."
            );

            return;
        }



        if (interstitialAd == null)
        {
            Log(
                "Ad skipped: Interstitial does not exist."
            );

            CreateInterstitialAd();

            return;
        }


        /*
         * Pacing / Capping được quản lý
         * tập trung bởi LevelPlay.
         *
         * IsAdReady() chỉ cho phép đi tiếp
         * khi quảng cáo đã load và Ad Unit
         * hiện không bị LevelPlay giới hạn.
         */
        bool ready =
            interstitialAd.IsAdReady();

        Log(
            $"Interstitial IsAdReady = {ready}"
        );


        /*
         * Không có ad sẵn:
         * bỏ qua ngay.
         *
         * Không chờ.
         * Không khóa UI.
         * Không khóa nút Next.
         */
        if (!ready)
        {
            Log(
                "Ad skipped: not ready."
            );

            LoadInterstitial();

            return;
        }


        isShowing =
            true;

        /*
         * Đủ điều kiện:
         *
         * 1. Hiện Ads Panel ngay lập tức.
         * 2. Chờ adShowDelay.
         * 3. Kiểm tra LevelPlay lại lần nữa.
         * 4. Nếu vẫn Ready thì show Ad thật.
         */
        ShowAdsPanel();

        CancelInvoke(
            nameof(ShowInterstitialAfterDelay)
        );

        Invoke(
            nameof(ShowInterstitialAfterDelay),
            adShowDelay
        );

        Log(
            $"Ads Panel shown. " +
            $"Interstitial will show after {adShowDelay:F1}s."
        );
    }


    private void ShowInterstitialAfterDelay()
    {
        if (!CanShowAds())
        {
            isShowing = false;

            HideAdsPanel();

            Log(
                "Interstitial cancelled: purchase state does not allow ads."
            );

            return;
        }

        if (!isShowing)
        {
            HideAdsPanel();
            return;
        }

        if (interstitialAd == null)
        {
            isShowing =
                false;

            HideAdsPanel();

            LogWarning(
                "Interstitial cancelled after delay: " +
                "Interstitial does not exist."
            );

            CreateInterstitialAd();
            return;
        }

        /*
         * Trong thời gian chờ, trạng thái
         * Pacing / Capping hoặc Ad Ready
         * có thể thay đổi.
         *
         * Vì vậy phải kiểm tra lại.
         */
        if (!interstitialAd.IsAdReady())
        {
            isShowing =
                false;

            HideAdsPanel();

            Log(
                "Interstitial cancelled after delay: " +
                "ad is no longer ready."
            );

            LoadInterstitial();
            return;
        }

        Log(
            "SHOW Interstitial."
        );

        interstitialAd.ShowAd();
    }


    /*
     * ========================================
     * DISPLAY CALLBACKS
     * ========================================
     */

    private void HandleAdDisplayed(
        LevelPlayAdInfo adInfo)
    {


        /*
         * Tạm dừng nhạc game khi quảng cáo
         * toàn màn hình đang hiển thị.
         */
        SoundManager.Instance?.
            PauseBackgroundMusic();


        Log(
            "Interstitial DISPLAYED."
        );
    }


    private void HandleAdDisplayFailed(
        LevelPlayAdInfo adInfo,
        LevelPlayAdError error)
    {
        isShowing =
            false;

        HideAdsPanel();


        SoundManager.Instance?.
            ResumeBackgroundMusic();


        LogWarning(
            $"Interstitial DISPLAY FAILED: {error}"
        );


        /*
         * Show thất bại:
         * bỏ qua và chuẩn bị quảng cáo tiếp theo.
         */
        LoadInterstitial();
    }


    private void HandleAdClosed(
        LevelPlayAdInfo adInfo)
    {
        SoundManager.Instance?.
            ResumeBackgroundMusic();


        isShowing =
            false;

        HideAdsPanel();


        Log(
            "Interstitial CLOSED."
        );


        /*
         * Chuẩn bị quảng cáo tiếp theo.
         */
        LoadInterstitial();
    }


    /*
     * ========================================
     * ADS PANEL
     * ========================================
     */

    private void ShowAdsPanel()
    {
        if (adsPanel == null)
        {
            return;
        }

        adsPanel.SetActive(true);
    }


    private void HideAdsPanel()
    {
        if (adsPanel == null)
        {
            return;
        }

        adsPanel.SetActive(false);
    }


    /*
     * ========================================
     * PUBLIC
     * ========================================
     */

    public bool IsInterstitialReady()
    {
        return
            CanShowAds() &&
            interstitialAd != null &&
            !isShowing &&
            interstitialAd.IsAdReady();
    }


    private bool CanShowAds()
    {
        PurchaseManager purchaseManager =
            PurchaseManager.Instance;

        return
            purchaseManager != null &&
            purchaseManager.IsInitialized &&
            !purchaseManager.IsGamePurchased;
    }


    /*
     * ========================================
     * DEBUG
     * ========================================
     */

    private void Log(
        string message)
    {
        if (!showDebugLogs)
        {
            return;
        }

        Debug.Log(
            $"[AdsManager] {message}",
            this
        );
    }


    private void LogWarning(
        string message)
    {
        Debug.LogWarning(
            $"[AdsManager] {message}",
            this
        );
    }
}
