using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class UIHandleRotation : MonoBehaviour
{
    [SerializeField, Min(0f)]
    private float degreesPerUnit = 180f;

    [SerializeField, Min(0f)]
    private float maxDegreesPerSecond = 1440f;

    [SerializeField, Min(0.01f)]
    private float smoothTime = 0.15f;

    private float currentAngularVelocity;
    private float angularVelocitySmoothing;

    private RectTransform rectTransform;
    private Vector3 previousPosition;
    private bool hasPreviousPosition;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        previousPosition = rectTransform.position;
        currentAngularVelocity = 0f;
        angularVelocitySmoothing = 0f;
        hasPreviousPosition = true;
    }

    private void LateUpdate()
    {
        Vector3 currentPosition = rectTransform.position;

        if (!hasPreviousPosition)
        {
            previousPosition = currentPosition;
            hasPreviousPosition = true;
            return;
        }

        float deltaTime = Time.unscaledDeltaTime;
        if (deltaTime <= 0f)
        {
            previousPosition = currentPosition;
            return;
        }

        // Convert world displacement into the Handle parent's local X axis.
        Vector3 movement = currentPosition - previousPosition;
        Transform parent = rectTransform.parent;
        float deltaX = parent != null
            ? parent.InverseTransformVector(movement).x
            : movement.x;

        float targetAngularVelocity = Mathf.Clamp(
            -deltaX * degreesPerUnit / deltaTime,
            -maxDegreesPerSecond,
            maxDegreesPerSecond
        );

        currentAngularVelocity = Mathf.SmoothDamp(
            currentAngularVelocity,
            targetAngularVelocity,
            ref angularVelocitySmoothing,
            smoothTime,
            Mathf.Infinity,
            deltaTime
        );

        rectTransform.Rotate(0f, 0f, currentAngularVelocity * deltaTime, Space.Self);
        previousPosition = currentPosition;
    }

    private void OnDisable()
    {
        hasPreviousPosition = false;
        currentAngularVelocity = 0f;
        angularVelocitySmoothing = 0f;
    }
}
