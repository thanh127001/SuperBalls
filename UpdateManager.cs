using System.Collections;
using UnityEngine;

#if UNITY_ANDROID && !UNITY_EDITOR
using Google.Play.AppUpdate;
using Google.Play.Common;
#endif

[DisallowMultipleComponent]
public class UpdateManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameObject updatePanel;

    // Cache trong RAM, dùng chung trong suốt phiên chạy ứng dụng.
    private static bool hasCheckedForUpdate;
    private static bool hasUpdateAvailable;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSessionCache()
    {
        hasCheckedForUpdate = false;
        hasUpdateAvailable = false;
    }

    private void Awake()
    {
        FindReferences();
        RefreshUpdatePanel();
    }

    private void OnEnable()
    {
        FindReferences();

        if (gameManager == null)
        {
            RefreshUpdatePanel();
            return;
        }

        gameManager.OnGameStateChanged += HandleGameStateChanged;
        HandleGameStateChanged();
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.OnGameStateChanged -= HandleGameStateChanged;
        }
    }

    private void FindReferences()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }
    }

    private void HandleGameStateChanged()
    {
        RefreshUpdatePanel();
        TryCheckForUpdate();
    }

    private void RefreshUpdatePanel()
    {
        if (updatePanel != null)
        {
            updatePanel.SetActive(hasUpdateAvailable &&
                gameManager != null &&
                gameManager.State == GameState.Ready);
        }
    }

    private void TryCheckForUpdate()
    {
        if (hasCheckedForUpdate ||
            gameManager == null ||
            gameManager.State != GameState.Ready)
        {
            return;
        }

        // Chỉ kiểm tra một lần trong mỗi phiên, kể cả khi mất mạng hoặc API lỗi.
        hasCheckedForUpdate = true;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            return;
        }

        StartCoroutine(CheckForUpdate());
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private IEnumerator CheckForUpdate()
    {
        PlayAsyncOperation<AppUpdateInfo, AppUpdateErrorCode> operation;

        try
        {
            var appUpdateManager = new AppUpdateManager();
            operation = appUpdateManager.GetAppUpdateInfo();
        }
        catch (System.Exception)
        {
            yield break;
        }

        yield return operation;

        if (!isActiveAndEnabled)
        {
            yield break;
        }

        try
        {
            if (operation.IsSuccessful)
            {
                var updateInfo = operation.GetResult();
                hasUpdateAvailable = updateInfo != null &&
                    updateInfo.UpdateAvailability == UpdateAvailability.UpdateAvailable;
            }
        }
        catch (System.Exception)
        {
            // Lỗi kiểm tra cập nhật không được ảnh hưởng đến gameplay.
        }

        // Nếu đã vào Playing thì chỉ lưu kết quả; panel chờ đến lần Ready tiếp theo.
        RefreshUpdatePanel();
    }
#endif
}
