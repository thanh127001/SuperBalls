using UnityEngine;

[DisallowMultipleComponent]
public class PanelController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;

    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject playingPanel;
    [SerializeField] private GameObject pausedPanel;
    [SerializeField] private GameObject levelCompletedPanel;
    [SerializeField] private GameObject gameCompletedPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject gameInfoPanel;

    private void Awake()
    {
        FindReferences();
        HideAllPanels();
    }

    private void OnEnable()
    {
        FindReferences();
        SubscribeEvents();
        RefreshState();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        FindReferences();
    }
#endif

    private void FindReferences()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }
    }

    private void SubscribeEvents()
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.OnGameStateChanged -= HandleGameStateChanged;
        gameManager.OnGameStateChanged += HandleGameStateChanged;
    }

    private void UnsubscribeEvents()
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void RefreshState()
    {
        if (gameManager == null)
        {
            HideAllPanels();
            SetPanelActive(gameInfoPanel, false);
            return;
        }

        HandleGameStateChanged();
    }

    private void HandleGameStateChanged()
    {
        if (gameManager == null) return;

        switch (gameManager.State)
        {
            case GameState.Ready:
                SetPanelActive(gameInfoPanel, false);
                ShowOnly(mainMenuPanel);
                break;

            case GameState.Playing:
                ShowOnly(playingPanel);
                break;

            case GameState.Paused:
                ShowOnly(pausedPanel);
                break;

            case GameState.LevelCompleted:
                ShowOnly(levelCompletedPanel);
                break;

            case GameState.GameCompleted:
                ShowOnly(gameCompletedPanel);
                break;

            case GameState.GameOver:
                ShowOnly(gameOverPanel);
                break;

            case GameState.None:
            case GameState.Initializing:
            default:
                HideAllPanels();
                break;
        }
    }

    public void ShowGameInfo()
    {
        SetPanelActive(gameInfoPanel, true);
    }

    public void HideGameInfo()
    {
        SetPanelActive(gameInfoPanel, false);
    }

    private void ShowOnly(GameObject panelToShow)
    {
        SetPanelActive(mainMenuPanel, panelToShow == mainMenuPanel);
        SetPanelActive(playingPanel, panelToShow == playingPanel);
        SetPanelActive(pausedPanel, panelToShow == pausedPanel);
        SetPanelActive(levelCompletedPanel, panelToShow == levelCompletedPanel);
        SetPanelActive(gameCompletedPanel, panelToShow == gameCompletedPanel);
        SetPanelActive(gameOverPanel, panelToShow == gameOverPanel);
    }

    private void HideAllPanels()
    {
        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(playingPanel, false);
        SetPanelActive(pausedPanel, false);
        SetPanelActive(levelCompletedPanel, false);
        SetPanelActive(gameCompletedPanel, false);
        SetPanelActive(gameOverPanel, false);
    }

    private static void SetPanelActive(GameObject panel, bool isActive)
    {
        if (panel == null || panel.activeSelf == isActive)
        {
            return;
        }

        panel.SetActive(isActive);
    }
}
