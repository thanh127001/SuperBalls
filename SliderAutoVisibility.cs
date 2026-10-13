using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Slider))]
public class SliderAutoVisibility : MonoBehaviour
{
    private Slider slider;
    private RectTransform currentHandle;
    private CanvasGroup handleCanvasGroup;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        RefreshVisibility();
    }

    private void OnEnable()
    {
        if (slider == null)
            slider = GetComponent<Slider>();

        slider.onValueChanged.AddListener(HandleValueChanged);
        RefreshVisibility();
    }

    private void OnDisable()
    {
        if (slider != null)
            slider.onValueChanged.RemoveListener(HandleValueChanged);
    }

    private void LateUpdate()
    {
        // Also responds when maxValue or handleRect changes.
        RefreshVisibility();
    }

    private void HandleValueChanged(float value)
    {
        RefreshVisibility();
    }

    public void RefreshVisibility()
    {
        if (slider == null)
            return;

        RectTransform handle = slider.handleRect;
        if (handle == null || handle == transform)
            return;

        if (currentHandle != handle || handleCanvasGroup == null)
        {
            currentHandle = handle;
            handleCanvasGroup = handle.GetComponent<CanvasGroup>();
            if (handleCanvasGroup == null)
                handleCanvasGroup = handle.gameObject.AddComponent<CanvasGroup>();
        }

        bool visible = slider.value > 0f && slider.value < slider.maxValue;
        float targetAlpha = visible ? 1f : 0f;

        if (!Mathf.Approximately(handleCanvasGroup.alpha, targetAlpha))
            handleCanvasGroup.alpha = targetAlpha;

        handleCanvasGroup.interactable = visible;
        handleCanvasGroup.blocksRaycasts = visible;
    }
}
