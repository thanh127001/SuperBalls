using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class DelayedButton : MonoBehaviour
{
    [Header("Settings")]

    [SerializeField]
    [Min(0f)]
    private float delay = 3f;


    [Header("References")]

    [SerializeField]
    private TMP_Text countdownText;


    [Header("Events")]

    [SerializeField]
    private UnityEvent onDelayedClick;


    private Button button;
    private Coroutine delayCoroutine;


    private void Awake()
    {
        button = GetComponent<Button>();

        button.onClick.AddListener(HandleClick);

        HideCountdown();
    }


    private void OnDisable()
    {
        CancelDelay();
    }


    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }
    }


    private void HandleClick()
    {
        // Đang chờ -> click lần nữa để hủy.
        if (delayCoroutine != null)
        {
            CancelDelay();
            return;
        }

        // Click lần đầu -> bắt đầu chờ.
        delayCoroutine = StartCoroutine(DelayRoutine());
    }


    private IEnumerator DelayRoutine()
    {
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
        }

        float remainingTime = delay;

        while (remainingTime > 0f)
        {
            UpdateCountdownText(remainingTime);
            // SỬA TẠI ĐÂY: Dùng unscaledDeltaTime thay vì deltaTime
            remainingTime -= Time.unscaledDeltaTime;
            yield return null;
        }

        delayCoroutine = null;
        HideCountdown();

        onDelayedClick?.Invoke();
    }


    private void CancelDelay()
    {
        if (delayCoroutine != null)
        {
            StopCoroutine(delayCoroutine);
            delayCoroutine = null;
        }

        HideCountdown();
    }


    private void UpdateCountdownText(float remainingTime)
    {
        if (countdownText == null)
        {
            return;
        }

        int seconds = Mathf.CeilToInt(remainingTime);

        countdownText.text = seconds.ToString();
    }


    private void HideCountdown()
    {
        if (countdownText == null)
        {
            return;
        }

        countdownText.text = string.Empty;
        countdownText.gameObject.SetActive(false);
    }
}