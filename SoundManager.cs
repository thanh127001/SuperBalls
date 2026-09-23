using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SoundManager : MonoBehaviour
{
    /*
     * ========================================
     * SINGLETON
     * ========================================
     */

    public static SoundManager Instance
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

    [SerializeField]
    private ScoreController scoreController;


    /*
     * ========================================
     * AUDIO SOURCES
     * ========================================
     */

    [Header("Audio Sources")]

    [SerializeField]
    private AudioSource musicSource;

    [SerializeField]
    private AudioSource soundEffectSource;


    /*
     * ========================================
     * MUSIC SETTINGS
     * ========================================
     */

    [Header("Music Settings")]

    [SerializeField]
    [Range(0f, 1f)]
    private float musicVolume = 1f;

    [SerializeField]
    private bool musicEnabled = true;


    /*
     * ========================================
     * BACKGROUND MUSIC
     * ========================================
     */

    [Header("Background Music")]

    [SerializeField]
    private AudioClip backgroundMusic;

    [SerializeField]
    [Range(0f, 1f)]
    private float backgroundMusicVolume =
        1f;


    /*
     * ========================================
     * SFX SETTINGS
     * ========================================
     */

    [Header("SFX Settings")]

    [SerializeField]
    [Range(0f, 1f)]
    private float soundEffectVolume = 1f;

    [SerializeField]
    private bool soundEffectsEnabled = true;


    /*
     * ========================================
     * BALL COLLISION SOUND
     * ========================================
     */

    [Header("Ball Collision Sound")]

    [SerializeField]
    private AudioClip ballCollisionSound;

    [SerializeField]
    [Range(0f, 1f)]
    private float ballCollisionSoundVolume =
        1f;


    /*
     * ========================================
     * SELECT GROUP SOUND
     * ========================================
     */

    [Header("Select Group Sound")]

    [SerializeField]
    private AudioClip selectGroupSound;

    [SerializeField]
    [Range(0f, 1f)]
    private float selectGroupSoundVolume =
        1f;


    /*
     * ========================================
     * COMBO SOUND
     * ========================================
     */

    [Header("Combo Sound")]

    [SerializeField]
    private AudioClip comboSound;

    [SerializeField]
    [Range(0f, 1f)]
    private float comboSoundVolume =
        1f;


    /*
     * ========================================
     * LEVEL COMPLETED SOUND
     * ========================================
     */

    [Header("Level Completed Sound")]

    [SerializeField]
    private AudioClip levelCompletedSound;

    [SerializeField]
    [Range(0f, 1f)]
    private float levelCompletedSoundVolume =
        1f;


    /*
     * ========================================
     * GAME COMPLETED SOUND
     * ========================================
     */

    [Header("Game Completed Sound")]

    [SerializeField]
    private AudioClip gameCompletedSound;

    [SerializeField]
    [Range(0f, 1f)]
    private float gameCompletedSoundVolume =
        1f;


    /*
     * ========================================
     * GAME OVER SOUND
     * ========================================
     */

    [Header("Game Over Sound")]

    [SerializeField]
    private AudioClip gameOverSound;

    [SerializeField]
    [Range(0f, 1f)]
    private float gameOverSoundVolume =
        1f;


    /*
     * ========================================
     * BUTTON CLICK SOUND
     * ========================================
     */

    [Header("Button Click Sound")]

    [SerializeField]
    private AudioClip buttonClickSound;

    [SerializeField]
    [Range(0f, 1f)]
    private float buttonClickSoundVolume =
        1f;


    /*
     * ========================================
     * GAMEPLAY SOUND PRIORITY
     * ========================================
     */

    /*
     * Select / Combo được xử lý cuối frame
     * để SoundManager có thể biết trong
     * cùng frame có xảy ra Combo hoặc
     * Level Completed hay không.
     *
     * Ưu tiên:
     *
     * Level/Game Completed
     *      >
     * Combo
     *      >
     * Select Group
     */

    private bool pendingSelectGroupSound;

    private bool pendingComboSound;

    private Coroutine
        gameplaySoundCoroutine;


    /*
     * ========================================
     * PLAYING SOUNDS
     * ========================================
     */

    /*
     * Lưu thời điểm dự kiến kết thúc
     * của từng AudioClip.
     *
     * Dùng để tránh cùng một AudioClip
     * phát chồng lên chính nó khi dùng
     * AudioSource.PlayOneShot().
     */
    private readonly
        Dictionary<AudioClip, double>
        playingSoundEndTimes =
            new();


    /*
     * ========================================
     * PUBLIC
     * ========================================
     */

    public bool MusicEnabled =>
        musicEnabled;

    public bool SoundEffectsEnabled =>
        soundEffectsEnabled;

    public float MusicVolume =>
        musicVolume;

    public float SoundEffectVolume =>
        soundEffectVolume;


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

        FindReferences();

        InitializeAudioSources();
    }


    private void OnEnable()
    {
        if (Instance != this)
        {
            return;
        }

        FindReferences();

        SubscribeEvents();
    }


    private void Start()
    {
        if (Instance != this)
        {
            return;
        }

        /*
         * Đồng bộ trạng thái ban đầu.
         * Background music chỉ bắt đầu
         * khi GameState là Ready.
         */
        if (gameManager != null)
        {
            ApplyGameState(
                gameManager.State,
                playStateSound: false
            );
        }
    }


    private void OnDisable()
    {
        if (Instance != this)
        {
            return;
        }

        UnsubscribeEvents();

        CancelPendingGameplaySounds();
    }


    private void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }

        UnsubscribeEvents();

        CancelPendingGameplaySounds();

        playingSoundEndTimes.Clear();

        Instance = null;
    }


#if UNITY_EDITOR

    private void OnValidate()
    {
        FindReferences();

        musicVolume =
            Mathf.Clamp01(
                musicVolume
            );

        backgroundMusicVolume =
            Mathf.Clamp01(
                backgroundMusicVolume
            );

        soundEffectVolume =
            Mathf.Clamp01(
                soundEffectVolume
            );

        ballCollisionSoundVolume =
            Mathf.Clamp01(
                ballCollisionSoundVolume
            );

        selectGroupSoundVolume =
            Mathf.Clamp01(
                selectGroupSoundVolume
            );

        comboSoundVolume =
            Mathf.Clamp01(
                comboSoundVolume
            );

        levelCompletedSoundVolume =
            Mathf.Clamp01(
                levelCompletedSoundVolume
            );

        gameCompletedSoundVolume =
            Mathf.Clamp01(
                gameCompletedSoundVolume
            );

        gameOverSoundVolume =
            Mathf.Clamp01(
                gameOverSoundVolume
            );

        buttonClickSoundVolume =
            Mathf.Clamp01(
                buttonClickSoundVolume
            );


        if (musicSource != null)
        {
            ConfigureMusicSource(
                musicSource
            );

            UpdateMusicSourceVolume();
        }


        if (soundEffectSource != null)
        {
            ConfigureSoundEffectSource(
                soundEffectSource
            );
        }
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

        if (scoreController == null)
        {
            scoreController =
                FindFirstObjectByType<ScoreController>();
        }
    }


    /*
     * ========================================
     * EVENTS
     * ========================================
     */

    private void SubscribeEvents()
    {
        if (gameManager == null)
        {
            return;
        }

        /*
         * State
         */
        gameManager.OnGameStateChanged -=
            HandleGameStateChanged;

        gameManager.OnGameStateChanged +=
            HandleGameStateChanged;


        /*
         * Ball collision
         *
         * Ball phát static gameplay event,
         * SoundManager tự lắng nghe.
         */
        Ball.OnCollisionSoundRequested -=
            HandleBallCollisionSoundRequested;

        Ball.OnCollisionSoundRequested +=
            HandleBallCollisionSoundRequested;


        if (scoreController != null)
        {
            /*
             * Selection
             */
            scoreController.OnValidSelection -=
                HandleValidSelection;

            scoreController.OnValidSelection +=
                HandleValidSelection;


            /*
             * Combo
             */
            scoreController.OnComboCompleted -=
                HandleComboCompleted;

            scoreController.OnComboCompleted +=
                HandleComboCompleted;
        }
    }


    private void UnsubscribeEvents()
    {
        Ball.OnCollisionSoundRequested -=
            HandleBallCollisionSoundRequested;

        if (gameManager != null)
        {
            gameManager.OnGameStateChanged -=
                HandleGameStateChanged;
        }

        if (scoreController != null)
        {
            scoreController.OnValidSelection -=
                HandleValidSelection;

            scoreController.OnComboCompleted -=
                HandleComboCompleted;
        }
    }


    /*
     * ========================================
     * BALL COLLISION EVENT
     * ========================================
     */

    private void HandleBallCollisionSoundRequested()
    {
        PlayBallCollisionSound();
    }


    /*
     * ========================================
     * SELECTION EVENT
     * ========================================
     */

    private void HandleValidSelection()
    {
        pendingSelectGroupSound =
            true;

        StartGameplaySoundEvaluation();
    }


    /*
     * ========================================
     * COMBO EVENT
     * ========================================
     */

    private void HandleComboCompleted()
    {
        /*
         * Combo có ưu tiên cao hơn
         * Select Group.
         */
        pendingSelectGroupSound =
            false;

        pendingComboSound =
            true;

        StartGameplaySoundEvaluation();
    }


    /*
     * ========================================
     * GAMEPLAY SOUND EVALUATION
     * ========================================
     */

    private void StartGameplaySoundEvaluation()
    {
        if (gameplaySoundCoroutine != null)
        {
            return;
        }

        gameplaySoundCoroutine =
            StartCoroutine(
                EvaluateGameplaySounds()
            );
    }


    private IEnumerator EvaluateGameplaySounds()
    {
        /*
         * Chờ đến cuối frame để tất cả event
         * gameplay trong frame hiện tại
         * được xử lý xong.
         */
        yield return new WaitForEndOfFrame();

        gameplaySoundCoroutine =
            null;


        /*
         * Nếu gameplay đã kết thúc trong
         * cùng frame thì state sound
         * được ưu tiên hoàn toàn.
         */
        if (gameManager == null ||
            gameManager.State !=
                GameState.Playing)
        {
            pendingSelectGroupSound =
                false;

            pendingComboSound =
                false;

            yield break;
        }


        /*
         * Combo > Select Group.
         */
        if (pendingComboSound)
        {
            PlayComboSound();
        }
        else if (pendingSelectGroupSound)
        {
            PlaySelectGroupSound();
        }


        pendingSelectGroupSound =
            false;

        pendingComboSound =
            false;
    }


    private void CancelPendingGameplaySounds()
    {
        pendingSelectGroupSound =
            false;

        pendingComboSound =
            false;

        if (gameplaySoundCoroutine ==
            null)
        {
            return;
        }

        StopCoroutine(
            gameplaySoundCoroutine
        );

        gameplaySoundCoroutine =
            null;
    }


    /*
     * ========================================
     * GAME STATE
     * ========================================
     */

    private void HandleGameStateChanged()
    {
        if (gameManager == null) return;

        ApplyGameState(
            gameManager.State,
            playStateSound: true
        );
    }


    private void ApplyGameState(
        GameState state,
        bool playStateSound)
    {
        switch (state)
        {
            case GameState.Playing:

                break;


            case GameState.Paused:

                CancelPendingGameplaySounds();

                break;


            case GameState.LevelCompleted:

                /*
                 * Level Completed có ưu tiên
                 * cao hơn Select / Combo.
                 */
                CancelPendingGameplaySounds();

                if (playStateSound)
                {
                    PlayLevelCompletedSound();
                }

                break;


            case GameState.GameCompleted:

                /*
                 * Game Completed có ưu tiên
                 * cao hơn Select / Combo.
                 */
                CancelPendingGameplaySounds();

                if (playStateSound)
                {
                    PlayGameCompletedSound();
                }

                break;


            case GameState.GameOver:

                CancelPendingGameplaySounds();

                if (playStateSound)
                {
                    PlayGameOverSound();
                }

                break;


            case GameState.Ready:

                CancelPendingGameplaySounds();

                /*
                 * Mỗi lần GameState trở thành Ready,
                 * background music bắt đầu lại từ đầu.
                 * AudioSource được cấu hình loop = true.
                 */
                RestartBackgroundMusic();

                break;


            case GameState.None:
            case GameState.Initializing:
            default:

                CancelPendingGameplaySounds();

                break;
        }
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
     * AUDIO SOURCES
     * ========================================
     */

    private void InitializeAudioSources()
    {
        InitializeMusicSource();

        InitializeSoundEffectSource();
    }


    private void InitializeMusicSource()
    {
        if (musicSource == null)
        {
            AudioSource[] sources =
                GetComponents<AudioSource>();

            for (int i = 0;
                 i < sources.Length;
                 i++)
            {
                AudioSource source =
                    sources[i];

                if (source == null ||
                    source ==
                    soundEffectSource)
                {
                    continue;
                }

                if (source.loop)
                {
                    musicSource =
                        source;

                    break;
                }
            }
        }


        if (musicSource == null)
        {
            musicSource =
                gameObject.AddComponent
                    <AudioSource>();
        }


        ConfigureMusicSource(
            musicSource
        );

        UpdateMusicSourceVolume();
    }


    private void InitializeSoundEffectSource()
    {
        if (soundEffectSource == null)
        {
            AudioSource[] sources =
                GetComponents<AudioSource>();

            for (int i = 0;
                 i < sources.Length;
                 i++)
            {
                AudioSource source =
                    sources[i];

                if (source == null ||
                    source ==
                    musicSource)
                {
                    continue;
                }

                soundEffectSource =
                    source;

                break;
            }
        }


        if (soundEffectSource == null)
        {
            soundEffectSource =
                gameObject.AddComponent
                    <AudioSource>();
        }


        ConfigureSoundEffectSource(
            soundEffectSource
        );
    }


    private static void ConfigureMusicSource(
        AudioSource audioSource)
    {
        if (audioSource == null)
        {
            return;
        }

        audioSource.playOnAwake =
            false;

        audioSource.loop =
            true;

        audioSource.spatialBlend =
            0f;
    }


    private static void ConfigureSoundEffectSource(
        AudioSource audioSource)
    {
        if (audioSource == null)
        {
            return;
        }

        audioSource.playOnAwake =
            false;

        audioSource.loop =
            false;

        audioSource.spatialBlend =
            0f;

        audioSource.clip =
            null;
    }


    /*
     * ========================================
     * BACKGROUND MUSIC
     * ========================================
     */

    public void PlayBackgroundMusic()
    {
        PlayMusic(
            backgroundMusic,
            backgroundMusicVolume
        );
    }


    public void RestartBackgroundMusic()
    {
        RestartMusic(
            backgroundMusic,
            backgroundMusicVolume
        );
    }


    /*
     * ========================================
     * GENERIC MUSIC
     * ========================================
     */

    private void PlayMusic(
        AudioClip audioClip,
        float volumeScale)
    {
        if (audioClip == null)
        {
            return;
        }

        EnsureMusicSource();

        if (musicSource == null ||
            !musicSource.enabled ||
            !musicSource.gameObject
                .activeInHierarchy)
        {
            return;
        }

        if (musicSource.clip ==
                audioClip &&
            musicSource.isPlaying)
        {
            SetMusicSourceVolume(
                volumeScale
            );

            return;
        }

        musicSource.clip =
            audioClip;

        musicSource.loop =
            true;

        SetMusicSourceVolume(
            volumeScale
        );

        musicSource.Play();
    }


    private void RestartMusic(
        AudioClip audioClip,
        float volumeScale)
    {
        if (audioClip == null)
        {
            return;
        }

        EnsureMusicSource();

        if (musicSource == null ||
            !musicSource.enabled ||
            !musicSource.gameObject
                .activeInHierarchy)
        {
            return;
        }

        musicSource.Stop();

        musicSource.clip =
            audioClip;

        musicSource.loop =
            true;

        musicSource.time =
            0f;

        SetMusicSourceVolume(
            volumeScale
        );

        musicSource.Play();
    }


    private void SetMusicSourceVolume(
        float volumeScale)
    {
        if (musicSource == null)
        {
            return;
        }

        float finalVolume =
            musicVolume *
            Mathf.Clamp01(
                volumeScale
            );

        musicSource.volume =
            musicEnabled
                ? finalVolume
                : 0f;
    }


    private void UpdateMusicSourceVolume()
    {
        if (musicSource == null)
        {
            return;
        }

        float volumeScale =
            musicSource.clip ==
                backgroundMusic
                ? backgroundMusicVolume
                : 1f;

        SetMusicSourceVolume(
            volumeScale
        );
    }


    public void StopBackgroundMusic()
    {
        if (musicSource == null)
        {
            return;
        }

        musicSource.Stop();
    }


    public void PauseBackgroundMusic()
    {
        if (musicSource == null ||
            !musicSource.isPlaying)
        {
            return;
        }

        musicSource.Pause();
    }


    public void ResumeBackgroundMusic()
    {
        if (!musicEnabled)
        {
            return;
        }

        EnsureMusicSource();

        if (musicSource == null)
        {
            return;
        }

        if (musicSource.clip == null)
        {
            PlayBackgroundMusic();

            return;
        }

        UpdateMusicSourceVolume();

        musicSource.UnPause();
    }


    /*
     * ========================================
     * BALL COLLISION
     * ========================================
     */

    private void PlayBallCollisionSound()
    {
        PlaySound(
            ballCollisionSound,
            ballCollisionSoundVolume
        );
    }


    /*
     * ========================================
     * SELECT GROUP
     * ========================================
     */

    private void PlaySelectGroupSound()
    {
        PlaySound(
            selectGroupSound,
            selectGroupSoundVolume
        );
    }


    /*
     * ========================================
     * COMBO
     * ========================================
     */

    private void PlayComboSound()
    {
        PlaySound(
            comboSound,
            comboSoundVolume
        );
    }


    /*
     * ========================================
     * LEVEL COMPLETED
     * ========================================
     */

    private void PlayLevelCompletedSound()
    {
        PlaySound(
            levelCompletedSound,
            levelCompletedSoundVolume
        );
    }


    /*
     * ========================================
     * GAME COMPLETED
     * ========================================
     */

    private void PlayGameCompletedSound()
    {
        PlaySound(
            gameCompletedSound,
            gameCompletedSoundVolume
        );
    }


    /*
     * ========================================
     * GAME OVER
     * ========================================
     */

    private void PlayGameOverSound()
    {
        PlaySound(
            gameOverSound,
            gameOverSoundVolume
        );
    }


    /*
     * ========================================
     * BUTTON
     * ========================================
     */

    public void PlayButtonClickSound()
    {
        PlaySound(
            buttonClickSound,
            buttonClickSoundVolume
        );
    }


    /*
     * ========================================
     * GENERIC SFX
     * ========================================
     */

    private void PlaySound(
        AudioClip audioClip,
        float volumeScale)
    {
        if (!soundEffectsEnabled ||
            audioClip == null)
        {
            return;
        }

        float clampedVolumeScale =
            Mathf.Clamp01(
                volumeScale
            );

        if (clampedVolumeScale <= 0f ||
            soundEffectVolume <= 0f)
        {
            return;
        }

        EnsureSoundEffectSource();

        if (soundEffectSource == null ||
            !soundEffectSource.enabled ||
            !soundEffectSource.gameObject
                .activeInHierarchy)
        {
            return;
        }

        double currentTime =
            AudioSettings.dspTime;

        /*
         * Không cho cùng một AudioClip
         * chồng lên chính nó.
         */
        if (playingSoundEndTimes.TryGetValue(
                audioClip,
                out double endTime))
        {
            if (currentTime < endTime)
            {
                return;
            }

            playingSoundEndTimes.Remove(
                audioClip
            );
        }

        float pitch =
            Mathf.Abs(
                soundEffectSource.pitch
            );

        if (pitch <= Mathf.Epsilon)
        {
            return;
        }

        float finalVolume =
            soundEffectVolume *
            clampedVolumeScale;

        if (finalVolume <= 0f)
        {
            return;
        }

        playingSoundEndTimes[audioClip] =
            currentTime +
            (audioClip.length / pitch);

        soundEffectSource.PlayOneShot(
            audioClip,
            finalVolume
        );
    }


    /*
     * ========================================
     * MUSIC ENABLE
     * ========================================
     */

    public void SetMusicEnabled(
        bool enabled)
    {
        if (musicEnabled == enabled)
        {
            return;
        }

        musicEnabled =
            enabled;

        EnsureMusicSource();

        if (musicSource == null)
        {
            return;
        }

        UpdateMusicSourceVolume();

        if (musicEnabled &&
            !musicSource.isPlaying)
        {
            if (musicSource.clip != null)
            {
                musicSource.UnPause();
            }
            else
            {
                PlayBackgroundMusic();
            }
        }
    }


    public void ToggleMusic()
    {
        SetMusicEnabled(
            !musicEnabled
        );
    }


    /*
     * ========================================
     * SFX ENABLE
     * ========================================
     */

    public void SetSoundEffectsEnabled(
        bool enabled)
    {
        if (soundEffectsEnabled ==
            enabled)
        {
            return;
        }

        soundEffectsEnabled =
            enabled;

        if (!soundEffectsEnabled)
        {
            StopAllSoundEffects();
        }
    }


    public void ToggleSoundEffects()
    {
        SetSoundEffectsEnabled(
            !soundEffectsEnabled
        );
    }


    /*
     * ========================================
     * MASTER SOUND
     * ========================================
     */

    public void SetSoundEnabled(
        bool enabled)
    {
        SetMusicEnabled(
            enabled
        );

        SetSoundEffectsEnabled(
            enabled
        );
    }


    public void ToggleSound()
    {
        bool shouldEnable =
            !musicEnabled ||
            !soundEffectsEnabled;

        SetSoundEnabled(
            shouldEnable
        );
    }


    /*
     * ========================================
     * VOLUME
     * ========================================
     */

    public void SetMusicVolume(
        float volume)
    {
        musicVolume =
            Mathf.Clamp01(
                volume
            );

        UpdateMusicSourceVolume();
    }


    public void SetSoundEffectVolume(
        float volume)
    {
        soundEffectVolume =
            Mathf.Clamp01(
                volume
            );
    }


    /*
     * ========================================
     * STOP
     * ========================================
     */

    public void StopAllSoundEffects()
    {
        if (soundEffectSource != null)
        {
            soundEffectSource.Stop();
        }

        playingSoundEndTimes.Clear();

        CancelPendingGameplaySounds();
    }


    public void StopAllSounds()
    {
        StopBackgroundMusic();

        StopAllSoundEffects();
    }


    /*
     * ========================================
     * ENSURE AUDIO SOURCES
     * ========================================
     */

    private void EnsureMusicSource()
    {
        if (musicSource != null)
        {
            return;
        }

        InitializeMusicSource();
    }


    private void EnsureSoundEffectSource()
    {
        if (soundEffectSource != null)
        {
            return;
        }

        InitializeSoundEffectSource();
    }
}