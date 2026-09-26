using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class AchievementManager : MonoBehaviour
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
    private TMP_Text achievementText;


    /*
     * ========================================
     * SAVE
     * ========================================
     */

    private const string AchievementCountKey =
        "AchievementCount";

    private const string HasNewAchievementKey =
        "HasNewAchievement";


    /*
     * ========================================
     * RUNTIME
     * ========================================
     */

    private int achievementCount;

    private bool hasNewAchievement;


    /*
     * ========================================
     * PUBLIC
     * ========================================
     */

    public int AchievementCount =>
        achievementCount;

    public bool HasNewAchievement =>
        hasNewAchievement;


    /*
     * ========================================
     * UNITY
     * ========================================
     */

    private void Awake()
    {
        FindReferences();

        LoadData();

        HideAchievementText();
    }


    private void OnEnable()
    {
        FindReferences();

        if (gameManager != null)
        {
            gameManager.OnGameStateChanged -=
                HandleGameStateChanged;

            gameManager.OnGameStateChanged +=
                HandleGameStateChanged;
        }
    }


    private void Start()
    {
        if (gameManager != null &&
            gameManager.State == GameState.Ready)
        {
            HandleReadyState();
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


#if UNITY_EDITOR

    private void OnValidate()
    {
        FindReferences();
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
        if (gameManager == null)
        {
            return;
        }

        switch (gameManager.State)
        {
            case GameState.GameCompleted:

                RegisterGameCompleted();

                break;


            case GameState.Ready:

                HandleReadyState();

                break;
        }
    }


    /*
     * ========================================
     * GAME COMPLETED
     * ========================================
     */

    private void RegisterGameCompleted()
    {
        achievementCount++;

        hasNewAchievement =
            true;

        SaveData();
    }


    /*
     * ========================================
     * READY
     * ========================================
     */

    private void HandleReadyState()
    {
        UpdateAchievementText();

        if (achievementCount <= 0)
        {
            HideAchievementText();
            return;
        }

        ShowAchievementText();

        if (hasNewAchievement)
        {
            hasNewAchievement =
                false;

            SaveData();
        }
    }


    /*
     * ========================================
     * UI
     * ========================================
     */

    private void UpdateAchievementText()
    {
        if (achievementText == null)
        {
            return;
        }

        achievementText.text =
            achievementCount.ToString();
    }


    private void ShowAchievementText()
    {
        if (achievementText == null)
        {
            return;
        }

        achievementText.gameObject.SetActive(
            true
        );
    }


    private void HideAchievementText()
    {
        if (achievementText == null)
        {
            return;
        }

        achievementText.gameObject.SetActive(
            false
        );
    }


#if UNITY_EDITOR

    public void ClearAchievementData()
    {
        PlayerPrefs.DeleteKey(
            AchievementCountKey
        );

        PlayerPrefs.DeleteKey(
            HasNewAchievementKey
        );

        PlayerPrefs.Save();

        achievementCount = 0;
        hasNewAchievement = false;

        UpdateAchievementText();
        HideAchievementText();
    }

#endif


    /*
     * ========================================
     * SAVE / LOAD
     * ========================================
     */

    private void LoadData()
    {
        achievementCount =
            Mathf.Max(
                0,
                PlayerPrefs.GetInt(
                    AchievementCountKey,
                    0
                )
            );

        hasNewAchievement =
            PlayerPrefs.GetInt(
                HasNewAchievementKey,
                0
            ) != 0;
    }


    private void SaveData()
    {
        PlayerPrefs.SetInt(
            AchievementCountKey,
            achievementCount
        );

        PlayerPrefs.SetInt(
            HasNewAchievementKey,
            hasNewAchievement
                ? 1
                : 0
        );

        PlayerPrefs.Save();
    }
}
