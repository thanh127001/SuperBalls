using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class SelectionCircle : MonoBehaviour
{
    /*
     * ========================================
     * REFERENCES
     * ========================================
     */

    [Header("References")]

    [SerializeField]
    private SpriteRenderer spriteRenderer;


    /*
     * ========================================
     * EFFECT
     * ========================================
     */

    // Kích thước ban đầu của SelectionCircle.
    private float startDiameter = 1f;

    // Kích thước cuối cùng của SelectionCircle.
    private float finalDiameter = 3.5f;

    // Thời gian hiệu ứng SelectionCircle.
    private float duration = 0.2f;

    /*
     * ========================================
     * RUNTIME
     * ========================================
     */

    private Coroutine effectRoutine;


    /*
     * ========================================
     * UNITY
     * ========================================
     */

    private void Awake()
    {
        FindReferences();
    }


    private void OnEnable()
    {
        Play();
    }


    /*
     * ========================================
     * REFERENCES
     * ========================================
     */

    private void FindReferences()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer =
                GetComponent<SpriteRenderer>();
        }
    }


    /*
     * ========================================
     * PLAY
     * ========================================
     */

    private void Play()
    {
        if (effectRoutine != null)
        {
            StopCoroutine(
                effectRoutine
            );
        }


        effectRoutine =
            StartCoroutine(
                EffectRoutine()
            );
    }


    /*
     * ========================================
     * EFFECT
     * ========================================
     */

    private IEnumerator EffectRoutine()
    {
        Vector3 startScale =
            Vector3.one *
            startDiameter;

        Vector3 finalScale =
            Vector3.one *
            finalDiameter;


        /*
         * Lưu màu gốc.
         */
        Color baseColor =
            spriteRenderer.color;

        baseColor.a = 1f;

        spriteRenderer.color =
            baseColor;


        /*
         * Bắt đầu tại kích thước nhỏ.
         */
        transform.localScale =
            startScale;


        float elapsed = 0f;


        while (elapsed < duration)
        {
            elapsed +=
                Time.deltaTime;


            float normalizedTime =
                Mathf.Clamp01(
                    elapsed /
                    duration
                );


            /*
             * ========================================
             * SCALE
             * ========================================
             */

            float scaleTime =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    normalizedTime
                );


            transform.localScale =
                Vector3.LerpUnclamped(
                    startScale,
                    finalScale,
                    scaleTime
                );


            /*
             * ========================================
             * FADE
             * ========================================
             */

            float alpha =
                1f -
                Mathf.SmoothStep(
                    0f,
                    1f,
                    normalizedTime
                );


            Color color =
                baseColor;

            color.a =
                alpha;

            spriteRenderer.color =
                color;


            yield return null;
        }


        /*
         * Đảm bảo kết thúc chính xác
         * tại kích thước cuối.
         */
        transform.localScale =
            finalScale;


        /*
         * Đảm bảo hoàn toàn trong suốt.
         */
        Color finalColor =
            baseColor;

        finalColor.a = 0f;

        spriteRenderer.color =
            finalColor;


        effectRoutine = null;


        /*
         * Hủy SelectionCircle.
         */
        Destroy(
            gameObject
        );
    }


    /*
     * ========================================
     * VALIDATION
     * ========================================
     */

#if UNITY_EDITOR

    private void OnValidate()
    {
        FindReferences();


        startDiameter =
            Mathf.Max(
                0f,
                startDiameter
            );


        finalDiameter =
            Mathf.Max(
                0f,
                finalDiameter
            );


        duration =
            Mathf.Max(
                0.01f,
                duration
            );
    }

#endif
}
