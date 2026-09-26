using System.Collections;
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


    private void HideAchievementText()
    {
        if (achievementText == null)
        {
            return;
        }

        Color color =
            achievementText.color;

        color.a = 0f;

        achievementText.color =
            color;
    }


    /*
     * ========================================
     * ANIMATION
     * ========================================
     */

    private const float ReadyDelay = 1f;
    private const float AnimationDuration = 0.6f;
    private const float StartScale = 0.5f;
    private const float PeakScale = 1.2f;
    private const float NormalScale = 1f;


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

    private Coroutine animationCoroutine;


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

        UpdateAchievementText();
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

        StopAnimation();
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

        UpdateAchievementText();
    }


    /*
     * ========================================
     * READY
     * ========================================
     */

    private void HandleReadyState()
    {
        UpdateAchievementText();
        HideAchievementText();

        if (!hasNewAchievement)
        {
            return;
        }

        PlayAnimation();
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


    /*
     * ========================================
     * ANIMATION
     * ========================================
     */

    private void PlayAnimation()
    {
        if (achievementText == null)
        {
            return;
        }

        StopAnimation();

        animationCoroutine =
            StartCoroutine(
                AnimationRoutine()
            );
    }


    private IEnumerator AnimationRoutine()
    {
        yield return
            new WaitForSecondsRealtime(
                ReadyDelay
            );

        RectTransform textTransform =
            achievementText.rectTransform;

        Vector3 originalScale =
            textTransform.localScale;

        Color originalColor =
            achievementText.color;

        Color transparentColor =
            originalColor;

        transparentColor.a = 0f;

        achievementText.color =
            transparentColor;

        textTransform.localScale =
            originalScale *
            StartScale;

        float elapsed = 0f;

        while (elapsed < AnimationDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / AnimationDuration
                );

            float scale;

            if (progress < 0.7f)
            {
                float zoomProgress =
                    progress / 0.7f;

                scale =
                    Mathf.Lerp(
                        StartScale,
                        PeakScale,
                        zoomProgress
                    );
            }
            else
            {
                float settleProgress =
                    (progress - 0.7f) / 0.3f;

                scale =
                    Mathf.Lerp(
                        PeakScale,
                        NormalScale,
                        settleProgress
                    );
            }

            textTransform.localScale =
                originalScale *
                scale;

            Color color =
                originalColor;

            color.a =
                Mathf.Lerp(
                    0f,
                    originalColor.a,
                    progress
                );

            achievementText.color =
                color;

            yield return null;
        }

        textTransform.localScale =
            originalScale;

        achievementText.color =
            originalColor;

        animationCoroutine = null;

        hasNewAchievement =
            false;

        SaveData();
    }


    private void StopAnimation()
    {
        if (animationCoroutine == null)
        {
            return;
        }

        StopCoroutine(
            animationCoroutine
        );

        animationCoroutine = null;
    }


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
