using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ParticleSystem))]
public class ComboEffect : MonoBehaviour
{
    private ScoreController scoreController;
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

        if (scoreController != null)
        {
            scoreController.OnComboCompleted +=
                OnComboCompleted;
        }
    }


    private void OnDisable()
    {
        if (scoreController != null)
        {
            scoreController.OnComboCompleted -=
                OnComboCompleted;
        }
    }


    private void FindReferences()
    {
        if (scoreController == null)
        {
            scoreController =
                FindFirstObjectByType<ScoreController>();
        }
    }


    private void OnComboCompleted()
    {
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