using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Slider), typeof(CanvasGroup))]
public class SliderAutoVisibility : MonoBehaviour
{
    private Slider slider;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        canvasGroup = GetComponent<CanvasGroup>();
        RefreshVisibility();
    }

    private void OnEnable()
    {
        slider.onValueChanged.AddListener(HandleValueChanged);
        RefreshVisibility();
    }

    private void OnDisable()
    {
        slider.onValueChanged.RemoveListener(HandleValueChanged);
    }

    private void HandleValueChanged(float value)
    {
        RefreshVisibility();
    }

    public void RefreshVisibility()
    {
        if (slider == null || canvasGroup == null)
            return;

        bool visible = slider.value > 0f && slider.value < slider.maxValue;
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }
}
