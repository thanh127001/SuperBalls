using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Slider))]
public class SliderAutoVisibility : MonoBehaviour
{
    private Slider slider;
    private RectTransform currentHandle;
    private Graphic[] handleGraphics;
    private float previousValue;
    private bool initialized;

    private void Awake()
    {
        slider = GetComponent<Slider>();
    }

    private void OnEnable()
    {
        if (slider == null)
            slider = GetComponent<Slider>();

        previousValue = slider.value;
        initialized = true;
        RefreshVisibility();
    }

    private void LateUpdate()
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

        float value = slider.value;
        bool decreasing = initialized && value < previousValue && !Mathf.Approximately(value, previousValue);
        bool visible = value > 0f && value < slider.maxValue && !decreasing;

        foreach (Graphic graphic in handleGraphics)
        {
            if (graphic != null && graphic.enabled != visible)
                graphic.enabled = visible;
        }

        previousValue = value;
        initialized = true;
    }
}
