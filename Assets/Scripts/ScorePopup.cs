using TMPro;
using UnityEngine;

public class ScorePopup : MonoBehaviour
{
    public float moveSpeed = 50f;
    public float lifetime = 1f;

    private TextMeshProUGUI textMesh;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();

        canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Setup(int amount)
    {
        textMesh.text = "+" + amount;
    }

    void Update()
    {
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        lifetime -= Time.deltaTime;

        canvasGroup.alpha = lifetime;

        if (lifetime <= 0)
        {
            Destroy(gameObject);
        }
    }
}