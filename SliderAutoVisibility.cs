using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Slider))]
public class SliderAutoVisibility : MonoBehaviour
{
    private Slider slider;
    private RectTransform currentHandle;
    private Graphic[] handleGraphics;

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
        // Detect maxValue and Handle Rect changes as well as value changes.
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

        if (currentHandle != handle || handleGraphics == null)
        {
            currentHandle = handle;
            handleGraphics = handle.GetComponentsInChildren<Graphic>(true);
        }

        bool visible = slider.value > 0f && slider.value < slider.maxValue;
        foreach (Graphic graphic in handleGraphics)
        {
            if (graphic != null && graphic.enabled != visible)
                graphic.enabled = visible;
        }
    }
}
