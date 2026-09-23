using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PurchaseManager : MonoBehaviour
{
    /*
     * ========================================
     * SINGLETON
     * ========================================
     */

    public static PurchaseManager Instance
    {
        get;
        private set;
    }


    /*
     * ========================================
     * PURCHASE
     * ========================================
     */

    private const string PurchaseKey =
        "GamePurchased";

    private bool isInitialized;
    private bool isGamePurchased;


    /*
     * ========================================
     * EVENTS
     * ========================================
     */

    public event Action OnPurchaseChanged;


    /*
     * ========================================
     * PUBLIC
     * ========================================
     */

    public bool IsInitialized =>
        isInitialized;

    public bool IsGamePurchased =>
        isGamePurchased;


    /*
     * ========================================
     * UNITY
     * ========================================
     */

    private void Awake()
    {
        InitializeSingleton();

        if (Instance != this)
        {
            return;
        }

        DontDestroyOnLoad(
            gameObject
        );

        InitializePurchase();
    }


    private void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }

        Instance = null;
    }


    /*
     * ========================================
     * SINGLETON
     * ========================================
     */

    private void InitializeSingleton()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(
                gameObject
            );

            return;
        }

        Instance = this;
    }


    /*
     * ========================================
     * INITIALIZE
     * ========================================
     */

    private void InitializePurchase()
    {
        /*
         * GIẢ LẬP:
         *
         * Sau này thay phần này bằng quá trình
         * khởi tạo IAP và đồng bộ quyền sở hữu
         * với Google Play / App Store.
         */

        isGamePurchased =
            PlayerPrefs.GetInt(
                PurchaseKey,
                0
            ) == 1;

        isInitialized = true;

        OnPurchaseChanged?.Invoke();
    }


    /*
     * ========================================
     * PURCHASE
     * ========================================
     */

    public void PurchaseGame()
    {
        if (!isInitialized ||
            isGamePurchased)
        {
            return;
        }

        /*
         * GIẢ LẬP:
         *
         * Xem như Store đã xác nhận giao dịch
         * thành công ngay lập tức.
         */

        SetPurchased(
            true
        );
    }


    /*
     * ========================================
     * RESTORE PURCHASE
     * ========================================
     */

    public void RestorePurchase()
    {
        if (!isInitialized)
        {
            return;
        }

        /*
         * GIẢ LẬP:
         *
         * Sau này Store sẽ trả về quyền sở hữu.
         * Hiện tại chỉ đọc trạng thái local.
         */

        bool purchased =
            PlayerPrefs.GetInt(
                PurchaseKey,
                0
            ) == 1;

        SetPurchased(
            purchased
        );
    }


    /*
     * ========================================
     * SET PURCHASE
     * ========================================
     */

    private void SetPurchased(
        bool purchased)
    {
        if (isGamePurchased ==
            purchased)
        {
            return;
        }

        isGamePurchased =
            purchased;

        PlayerPrefs.SetInt(
            PurchaseKey,
            isGamePurchased
                ? 1
                : 0
        );

        PlayerPrefs.Save();

        OnPurchaseChanged?.Invoke();
    }


#if UNITY_EDITOR

    /*
     * ========================================
     * EDITOR TEST
     * ========================================
     */

    public void EditorSetPurchased(
        bool purchased)
    {
        if (!isInitialized)
        {
            return;
        }

        SetPurchased(
            purchased
        );
    }

#endif
}
