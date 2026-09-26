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
    private GameObject achievementContainer;

    [SerializeField]
    private TMP_Text achievementText;


    /*
     * ========================================
     * ANIMATION
     * ========================================
     */

    private const float ShakeDuration = 0.7f;
    private const float ShakeAngle = 8f;
    private const float ShakeFrequency = 5f;


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

    private Coroutine shakeCoroutine;


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

        StopShake();
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

        if (!hasNewAchievement)
        {
            return;
        }

        PlayShake();
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

    private void PlayShake()
    {
        if (achievementContainer == null)
        {
            return;
        }

        StopShake();

        shakeCoroutine =
            StartCoroutine(
                ShakeRoutine()
            );
    }


    private IEnumerator ShakeRoutine()
    {
        Transform containerTransform =
            achievementContainer.transform;

        Quaternion originalRotation =
            containerTransform.localRotation;

        float elapsed = 0f;

        while (elapsed < ShakeDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / ShakeDuration
                );

            float damping =
                1f - progress;

            float angle =
                Mathf.Sin(
                    progress *
                    ShakeFrequency *
                    Mathf.PI *
                    2f
                ) *
                ShakeAngle *
                damping;

            containerTransform.localRotation =
                originalRotation *
                Quaternion.Euler(
                    0f,
                    0f,
                    angle
                );

            yield return null;
        }

        containerTransform.localRotation =
            originalRotation;

        shakeCoroutine = null;

        hasNewAchievement =
            false;

        SaveData();
    }


    private void StopShake()
    {
        if (shakeCoroutine == null)
        {
            return;
        }

        StopCoroutine(
            shakeCoroutine
        );

        shakeCoroutine = null;
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
