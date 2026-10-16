using System.Collections;
using UnityEngine;

#if UNITY_ANDROID && !UNITY_EDITOR
using Google.Play.AppUpdate;
#endif

[DisallowMultipleComponent]
public class UpdateManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameObject updatePanel;

    // Chỉ tồn tại trong bộ nhớ của phiên chạy hiện tại.
    private bool hasCheckedForUpdate;

    private void Awake()
    {
        FindReferences();
        ApplyUpdateState(false);
    }

    private void OnEnable()
    {
        FindReferences();

        if (gameManager == null)
        {
            return;
        }

        gameManager.OnGameStateChanged += HandleGameStateChanged;

        // GameManager có thể đã vào Ready trước khi đăng ký sự kiện.
        TryCheckForUpdate();
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
        TryCheckForUpdate();
    }

    private void TryCheckForUpdate()
    {
        if (hasCheckedForUpdate ||
            gameManager == null ||
            gameManager.State != GameState.Ready)
        {
            return;
        }

        // Đánh dấu ngay tại lần Ready đầu tiên, kể cả khi mất mạng.
        // Không kiểm tra lại trong cùng phiên chạy ứng dụng.
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
        AppUpdateManager appUpdateManager;
        GetAppUpdateInfoOperation operation;

        try
        {
            appUpdateManager = new AppUpdateManager();
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

                if (updateInfo != null &&
                    updateInfo.UpdateAvailability == UpdateAvailability.UpdateAvailable)
                {
                    ApplyUpdateState(true);
                }
            }
        }
        catch (System.Exception)
        {
            // Lỗi kiểm tra cập nhật không được ảnh hưởng đến gameplay.
        }
    }
#endif

    private void ApplyUpdateState(bool hasUpdate)
    {
        if (updatePanel != null)
        {
            updatePanel.SetActive(hasUpdate);
        }
    }
}
