using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Slider))]
public class SliderAutoVisibility : MonoBehaviour
{
    private Slider slider;
    private RectTransform handle;
    private bool? lastVisible;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        RefreshVisibility();
    }

    private void OnEnable()
    {
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
        // Also detect changes to maxValue or the assigned Handle.
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

        RectTransform currentHandle = slider.handleRect;
        if (handle != currentHandle)
        {
            handle = currentHandle;
            lastVisible = null;
        }

        if (handle == null || handle.gameObject == gameObject)
            return;

        bool visible = slider.value > 0f && slider.value < slider.maxValue;
        if (lastVisible == visible && handle.gameObject.activeSelf == visible)
            return;

        handle.gameObject.SetActive(visible);
        lastVisible = visible;
    }
}
