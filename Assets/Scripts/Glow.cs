using UnityEngine;

public class Glow : MonoBehaviour
{
    [SerializeField] private SpriteRenderer glowRenderer;

    [Header("Pulse")]
    [SerializeField] private float pulseSpeed = 4f;
    [SerializeField] private float minAlpha = 0.2f;
    [SerializeField] private float maxAlpha = 0.7f;

    [Header("Scale")]
    [SerializeField] private float minScale = 0.95f;
    [SerializeField] private float maxScale = 1.15f;

    private Vector3 baseScale;

    private void Awake()
    {
        if (glowRenderer != null)
        {
            baseScale = glowRenderer.transform.localScale;
        }
    }

    private void Update()
    {
        if (glowRenderer == null)
        {
            return;
        }

        float pulse = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f;

        Color color = glowRenderer.color;
        color.a = Mathf.Lerp(minAlpha, maxAlpha, pulse);
        glowRenderer.color = color;

        float scale = Mathf.Lerp(minScale, maxScale, pulse);
        glowRenderer.transform.localScale = baseScale * scale;
    }
}