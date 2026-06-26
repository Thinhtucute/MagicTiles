using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class UIPopPanel : MonoBehaviour
{
    [SerializeField] private float showDuration = 0.18f;
    [SerializeField] private float hideDuration = 0.12f;
    [SerializeField] private float hiddenScale = 0.5f;
    [SerializeField] private float shownScale = 1f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Coroutine animationRoutine;
    private bool visible;

    private readonly List<RectTransform> animatedChildren = new();

    private void Awake()
    {
        EnsureComponents();
        CacheAnimatedChildren();
    }

    public void Show()
    {
        EnsureComponents();
        gameObject.SetActive(true);
        StartAnimation(true);
    }

    public void Hide()
    {
        EnsureComponents();
        StartAnimation(false);
    }

    public void SetInstantState(bool show)
    {
        EnsureComponents();

        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        gameObject.SetActive(show);

        float scale = show ? shownScale : hiddenScale;
        SetChildrenScale(scale);

        canvasGroup.alpha = show ? 1f : 0f;
        canvasGroup.interactable = show;
        canvasGroup.blocksRaycasts = show;

        visible = show;
    }

    private void StartAnimation(bool show)
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
        }

        animationRoutine = StartCoroutine(Animate(show));
    }

    private IEnumerator Animate(bool show)
    {
        float duration = show ? showDuration : hideDuration;
        float elapsed = 0f;

        float fromScale = show ? hiddenScale : shownScale;
        float toScale = show ? shownScale : hiddenScale;

        float fromAlpha = canvasGroup.alpha;
        float toAlpha = show ? 1f : 0f;

        if (show)
        {
            SetChildrenScale(hiddenScale);
        }

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            SetChildrenScale(Mathf.Lerp(fromScale, toScale, eased));
            canvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, eased);

            yield return null;
        }

        SetChildrenScale(toScale);
        canvasGroup.alpha = toAlpha;

        visible = show;
        canvasGroup.interactable = show;
        canvasGroup.blocksRaycasts = show;

        if (!show)
        {
            gameObject.SetActive(false);
        }

        animationRoutine = null;
    }

    private void CacheAnimatedChildren()
    {
        animatedChildren.Clear();

        foreach (Transform child in transform)
        {
            if (child.name == "Dim")
            {
                continue;
            }

            RectTransform rect = child as RectTransform;

            if (rect != null)
            {
                animatedChildren.Add(rect);
            }
        }
    }

    private void SetChildrenScale(float scale)
    {
        Vector3 targetScale = Vector3.one * scale;

        foreach (RectTransform child in animatedChildren)
        {
            if (child != null)
            {
                child.localScale = targetScale;
            }
        }
    }

    private void EnsureComponents()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }
}