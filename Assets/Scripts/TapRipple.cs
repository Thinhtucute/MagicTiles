using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class TapRipple : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private float duration = 0.35f;
    [SerializeField] private float startScale = 0.2f;
    [SerializeField] private float endScale = 2.0f;
    [SerializeField] private float startAlpha = 0.65f;
    [SerializeField] private float endAlpha = 0f;

    [Header("Visual")]
    [SerializeField] private Image rippleImage;
    [SerializeField] private Color rippleColor = Color.white;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Coroutine playRoutine;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (rippleImage == null)
        {
            rippleImage = GetComponent<Image>();
        }

        if (rippleImage != null)
        {
            rippleImage.raycastTarget = false;
        }

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0f;
    }

    public void Play(Vector2 screenPosition, Canvas canvas)
    {
        if (canvas == null)
        {
            Destroy(gameObject);
            return;
        }

        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;

        RectTransform canvasRect = canvas.transform as RectTransform;
        if (canvasRect == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.SetParent(canvas.transform, false);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, uiCamera, out Vector2 localPoint))
        {
            rectTransform.anchoredPosition = localPoint;
        }

        rectTransform.localScale = Vector3.one * startScale;
        canvasGroup.alpha = startAlpha;
        canvasGroup.blocksRaycasts = false;

        if (rippleImage != null)
        {
            rippleImage.color = rippleColor;
        }

        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
        }

        gameObject.SetActive(true);
        playRoutine = StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);

            rectTransform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, eased);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, eased);

            yield return null;
        }

        playRoutine = null;
        Destroy(gameObject);
    }
}
