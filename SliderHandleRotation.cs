using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SliderHandleRotation : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Slider slider;

    [SerializeField]
    private RectTransform handleVisual;

    // Tốc độ xoay của handle khi giá trị slider thay đổi (đơn vị: độ/giá trị)
    private const float rotationSpeed = 3600f;

    private float previousValue;

    private void Awake()
    {
        if (slider == null)
            slider = GetComponentInParent<Slider>();

        if (slider != null)
            previousValue = slider.value;
    }

    private void LateUpdate()
    {
        if (slider == null || handleVisual == null)
            return;

        float currentValue = slider.value;
        float deltaValue = currentValue - previousValue;

        if (!Mathf.Approximately(deltaValue, 0f))
        {
            float angle = -deltaValue * rotationSpeed;

            handleVisual.Rotate(
                0f,
                0f,
                angle,
                Space.Self
            );
        }

        previousValue = currentValue;
    }
}