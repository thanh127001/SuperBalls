using UnityEngine;

[DisallowMultipleComponent]
public class UpdateManager : MonoBehaviour
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
    private GameObject updatePanel;


    /*
     * ========================================
     * UPDATE
     * ========================================
     */

    [Header("Update")]

    [Tooltip("Tạm thời dùng để giả lập kết quả kiểm tra update.")]
    [SerializeField]
    private bool hasUpdate = false;

    private bool hasCheckedForUpdate;


    /*
     * ========================================
     * UNITY
     * ========================================
     */

    private void Awake()
    {
        FindReferences();

        ApplyUpdateState();
    }


    private void OnEnable()
    {
        FindReferences();

        if (gameManager == null)
        {
            return;
        }

        gameManager.OnGameStateChanged +=
            HandleGameStateChanged;


        /*
         * Trường hợp GameManager đã vào Ready
         * trước khi UpdateManager subscribe event.
         */
        if (gameManager.State == GameState.Ready)
        {
            CheckForUpdate();
        }
    }


    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.OnGameStateChanged -=
                HandleGameStateChanged;
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

    private void HandleGameStateChanged()
    {
        if (gameManager == null ||
            gameManager.State != GameState.Ready)
        {
            return;
        }

        CheckForUpdate();
    }


    /*
     * ========================================
     * UPDATE CHECK
     * ========================================
     */

    private void CheckForUpdate()
    {
        if (hasCheckedForUpdate)
        {
            return;
        }

        hasCheckedForUpdate = true;

        /*
         * Chỉ kiểm tra update một lần trong
         * vòng đời hiện tại của ứng dụng:
         * lần đầu tiên game vào Ready.
         *
         * TODO:
         * Sau này thay bằng logic kiểm tra
         * update thật.
         *
         * Ví dụ:
         *
         * hasUpdate = ...;
         */

        ApplyUpdateState();
    }


    /*
     * ========================================
     * UI
     * ========================================
     */

    private void ApplyUpdateState()
    {
        if (updatePanel == null)
        {
            return;
        }

        updatePanel.SetActive(hasUpdate);
    }
}