using TMPro;
using UnityEngine;

public class TMPFadeFlicker : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private float speed = 5f;

    private void Awake()
    {
        if (text == null)
        {
            text = GetComponent<TMP_Text>();
        }
    }

    private void Update()
    {
        if (text == null)
        {
            return;
        }

        float alpha = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;
        Color color = text.color;
        color.a = alpha;
        text.color = color;
    }
}
