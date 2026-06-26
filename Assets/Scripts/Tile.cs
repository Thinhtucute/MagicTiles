using System.Collections;
using UnityEngine;

public class Tile : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Feedback")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color normalColor = Color.black;
    [SerializeField] private Color validTapColor = Color.white;
    [SerializeField] private float validTapDelay = 0.1f;
    [SerializeField] private Color invalidTapColor = new(1f, 0.2f, 0.2f);
    [SerializeField] private float flashDuration = 0.08f;

    protected GameManager gameManager;
    private TileSpawner tileSpawner;
    private Transform despawnLine;
    private float flashTimer;
    private bool hasMissed;
    private bool isConsumed;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    protected virtual void Update()
    {
        if (isConsumed || (gameManager != null && gameManager.IsEndingSequence))
        {
            return;
        }

        transform.position += Vector3.down * moveSpeed * Time.deltaTime;

        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f && spriteRenderer != null)
            {
                spriteRenderer.color = normalColor;
            }
        }

        if (!hasMissed && despawnLine != null && transform.position.y <= despawnLine.position.y)
        {
            hasMissed = true;
            gameManager.MissTile(this);
        }
    }

    protected virtual void OnMouseDown()
    {
        HandleTap();
    }

    public virtual void Initialize(GameManager ownerGameManager, TileSpawner ownerSpawner, Transform ownerDespawnLine, float ownerMoveSpeed)
    {
        gameManager = ownerGameManager;
        tileSpawner = ownerSpawner;
        despawnLine = ownerDespawnLine;
        moveSpeed = ownerMoveSpeed;
        hasMissed = false;
        flashTimer = 0f;
        isConsumed = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = normalColor;
        }
    }

    public virtual void HandleTap()
    {
        if (gameManager == null || gameManager.IsGameOver || isConsumed || gameManager.IsEndingSequence)
        {
            return;
        }

        gameManager.SpawnTapRipple(Input.mousePosition);

        if (gameManager.TryTapTile(this))
        {
            StartCoroutine(PlayValidTapSequence());
            return;
        }

        PlayInvalidTapFeedback();
    }

    public void ReturnToPool()
    {
        if (gameManager != null)
        {
            gameManager.UnregisterTile(this);
        }

        if (tileSpawner != null)
        {
            tileSpawner.ReturnTile(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void PlayValidTapFeedback()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = validTapColor;
        }
    }

    private void PlayInvalidTapFeedback()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = invalidTapColor;
            flashTimer = flashDuration;
        }
    }

    private IEnumerator PlayValidTapSequence()
    {
        isConsumed = true;
        PlayValidTapFeedback();

        yield return new WaitForSeconds(validTapDelay);

        if (gameManager != null && gameManager.IsGameOver)
        {
            isConsumed = false;
            yield break;
        }

        isConsumed = false;
        ReturnToPool();
    }

    public void SetSpriteColor(Color color)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }
}
