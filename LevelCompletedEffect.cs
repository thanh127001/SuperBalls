using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ParticleSystem))]
public class LevelCompletedEffect : MonoBehaviour
{
    private GameManager gameManager;
    private ParticleSystem particleSystem;


    private void Awake()
    {
        particleSystem =
            GetComponent<ParticleSystem>();

        FindReferences();
    }


    private void OnEnable()
    {
        FindReferences();

        if (gameManager != null)
        {
            gameManager.OnGameStateChanged +=
                OnGameStateChanged;
        }
    }


    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.OnGameStateChanged -=
                OnGameStateChanged;
        }
    }


    private void FindReferences()
    {
        if (gameManager == null)
        {
            gameManager =
                FindFirstObjectByType<GameManager>();
        }
    }


    private void OnGameStateChanged()
    {
        if (gameManager == null ||
            gameManager.State != GameState.LevelCompleted)
        {
            return;
        }

        Play();
    }


    private void Play()
    {
        if (particleSystem == null)
        {
            return;
        }

        particleSystem.Play();
    }
}