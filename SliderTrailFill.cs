using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Slider))]
public class SliderTrailFill : MonoBehaviour
{
    [Header("Trail")]
    [SerializeField, Range(0.01f, 1f)]
    private float trailLength = 1f;

    [SerializeField, Range(0.1f, 5f)]
    private float fadePower = 1.5f;

    [SerializeField, Range(32, 1024)]
    private int textureWidth = 256;

    private Slider slider;
    private Image fillImage;
    private Sprite originalSprite;
    private Image.Type originalType;
    private bool originalPreserveAspect;
    private Texture2D gradientTexture;
    private Sprite gradientSprite;
    private float lastTrailLength = -1f;
    private float lastFadePower = -1f;

    private void Awake()
    {
        slider = GetComponent<Slider>();
    }

    private void OnEnable()
    {
        if (slider == null)
            slider = GetComponent<Slider>();

        ResolveFill();
        UpdateGradient();
    }

    private void LateUpdate()
    {
        if (fillImage == null)
            ResolveFill();

        if (fillImage != null &&
            (!Mathf.Approximately(lastTrailLength, trailLength) ||
             !Mathf.Approximately(lastFadePower, fadePower)))
        {
            UpdateGradient();
        }
    }

    private void ResolveFill()
    {
        if (slider == null || slider.fillRect == null)
            return;

        Image image = slider.fillRect.GetComponent<Image>();
        if (image == null)
            image = slider.fillRect.GetComponentInChildren<Image>();

        if (image == null || image == fillImage)
            return;

        RestoreFill();
        fillImage = image;
        originalSprite = fillImage.sprite;
        originalType = fillImage.type;
        originalPreserveAspect = fillImage.preserveAspect;
        fillImage.type = Image.Type.Simple;
        fillImage.preserveAspect = false;
    }

    private void UpdateGradient()
    {
        if (fillImage == null)
            return;

        int width = Mathf.Clamp(textureWidth, 32, 1024);
        if (gradientTexture == null || gradientTexture.width != width)
        {
            ReleaseGeneratedAssets();
            gradientTexture = new Texture2D(width, 2, TextureFormat.RGBA32, false)
            {
                name = "Slider Trail Gradient (Runtime)",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            gradientSprite = Sprite.Create(
                gradientTexture,
                new Rect(0, 0, width, 2),
                new Vector2(0.5f, 0.5f),
                100f
            );
            gradientSprite.name = "Slider Trail Fill (Runtime)";
        }

        float length = Mathf.Max(0.01f, trailLength);
        for (int x = 0; x < width; x++)
        {
            float position = (float)x / (width - 1);
            float distanceFromHead = 1f - position;
            float alpha = Mathf.Pow(
                Mathf.Clamp01(1f - distanceFromHead / length),
                fadePower
            );
            Color pixel = new Color(1f, 1f, 1f, alpha);
            gradientTexture.SetPixel(x, 0, pixel);
            gradientTexture.SetPixel(x, 1, pixel);
        }

        gradientTexture.Apply(false, false);
        fillImage.sprite = gradientSprite;
        lastTrailLength = trailLength;
        lastFadePower = fadePower;
    }

    private void OnDestroy()
    {
        RestoreFill();
        ReleaseGeneratedAssets();
    }

    private void RestoreFill()
    {
        if (fillImage == null)
            return;

        fillImage.sprite = originalSprite;
        fillImage.type = originalType;
        fillImage.preserveAspect = originalPreserveAspect;
        fillImage = null;
    }

    private void ReleaseGeneratedAssets()
    {
        if (gradientSprite != null)
        {
            Destroy(gradientSprite);
            gradientSprite = null;
        }

        if (gradientTexture != null)
        {
            Destroy(gradientTexture);
            gradientTexture = null;
        }
    }
}
