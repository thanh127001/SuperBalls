using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Slider Trail")]
[RequireComponent(typeof(CanvasRenderer))]
public class UISliderTrail : MaskableGraphic
{
    [SerializeField] private Slider slider;
    [SerializeField, Min(0.01f)] private float lifetime = 0.35f;
    [SerializeField, Min(1f)] private float width = 18f;
    [SerializeField, Min(0.5f)] private float minDistance = 2f;
    [SerializeField, Range(0f, 1f)] private float tailAlpha = 0f;

    private struct Sample
    {
        public Vector2 position;
        public float time;
        public Sample(Vector2 position, float time)
        {
            this.position = position;
            this.time = time;
        }
    }

    private readonly List<Sample> samples = new List<Sample>();
    private float lastSliderValue;
    private bool hasPreviousValue;

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
        if (slider == null)
            slider = GetComponentInParent<Slider>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        samples.Clear();
        hasPreviousValue = false;
        SetVerticesDirty();
    }

    private void LateUpdate()
    {
        if (slider == null || slider.handleRect == null)
            return;

        float value = slider.value;
        float now = Time.unscaledTime;
        if (!hasPreviousValue)
        {
            lastSliderValue = value;
            hasPreviousValue = true;
            return;
        }

        // A decreasing slider value means decay or reset. Do not draw a reverse trail.
        if (value < lastSliderValue - 0.00001f ||
            value <= slider.minValue + 0.00001f)
        {
            if (samples.Count > 0)
            {
                samples.Clear();
                SetVerticesDirty();
            }
            lastSliderValue = value;
            return;
        }

        bool changed = false;
        for (int i = samples.Count - 1; i >= 0; i--)
        {
            if (now - samples[i].time > lifetime)
            {
                samples.RemoveAt(i);
                changed = true;
            }
        }

        if (value > lastSliderValue + 0.00001f)
        {
            Vector3 worldPosition = slider.handleRect.TransformPoint(slider.handleRect.rect.center);
            Vector2 localPosition = rectTransform.InverseTransformPoint(worldPosition);

            if (samples.Count == 0 ||
                Vector2.Distance(samples[samples.Count - 1].position, localPosition) >= minDistance)
            {
                samples.Add(new Sample(localPosition, now));
                changed = true;
            }
        }

        lastSliderValue = value;
        if (changed)
            SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (samples.Count < 2)
            return;

        float now = Time.unscaledTime;
        for (int i = 1; i < samples.Count; i++)
        {
            Sample a = samples[i - 1];
            Sample b = samples[i];
            Vector2 direction = b.position - a.position;
            if (direction.sqrMagnitude < 0.0001f)
                continue;

            Vector2 normal = new Vector2(-direction.y, direction.x).normalized * (width * 0.5f);
            float ageA = Mathf.Clamp01((now - a.time) / lifetime);
            float ageB = Mathf.Clamp01((now - b.time) / lifetime);
            Color32 ca = ColorWithAlpha(1f - ageA);
            Color32 cb = ColorWithAlpha(1f - ageB);

            int index = vh.currentVertCount;
            vh.AddVert(a.position - normal, ca, Vector2.zero);
            vh.AddVert(a.position + normal, ca, Vector2.zero);
            vh.AddVert(b.position - normal, cb, Vector2.zero);
            vh.AddVert(b.position + normal, cb, Vector2.zero);
            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index + 2, index + 1, index + 3);
        }
    }

    private Color32 ColorWithAlpha(float remaining)
    {
        Color c = color;
        c.a *= Mathf.Lerp(tailAlpha, 1f, Mathf.Clamp01(remaining));
        return c;
    }

    public void ClearTrail()
    {
        samples.Clear();
        hasPreviousValue = false;
        SetVerticesDirty();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        lifetime = Mathf.Max(0.01f, lifetime);
        width = Mathf.Max(1f, width);
        minDistance = Mathf.Max(0.5f, minDistance);
    }
#endif
}
