using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Image crown1Image;
    [SerializeField] private Image crown2Image;
    [SerializeField] private Image crown3Image;
    [SerializeField] private Image gameOverDimImage;
    [SerializeField] private Canvas tapEffectCanvas;
    [SerializeField] private TapRipple tapRipplePrefab;
    [SerializeField] private ScorePopup scorePopupPrefab;

    [Header("Feedback")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform hitLine;
    [SerializeField] private float invalidShakeDuration = 0.08f;
    [SerializeField] private float invalidShakeStrength = 0.08f;
    [SerializeField] private float missRevealDuration = 1f;
    [SerializeField] private float missRiseSpeed = 1.75f;

    [SerializeField] private float missFlickerSpeed = 24f;
    [SerializeField] private Color missStartColor = Color.white;
    [SerializeField] private Color missEndColor = new Color(1f, 0.15f, 0.15f, 1f);

    [Header("Particle Effects")]
    [SerializeField] private GameObject[] particleVfxPrefabs;
    [SerializeField] private float particleVfxLifetime = 3f;
    private bool hasStarted;
    private static bool skipStartPanel;
    private readonly List<Tile> activeTiles = new();
    private Tile currentBottomTile;
    private AudioManager audioManager;
    private TileSpawner tileSpawner;
    private int score;
    private bool gameOver;
    private bool endingSequence;
    private Vector3 cameraStartPosition;
    private float shakeTimer;
    private readonly HashSet<Tile> consumedTiles = new();
    private UIPopPanel startPanelAnimator;
    private UIPopPanel gameOverPanelAnimator;

    public Tile CurrentBottomTile => currentBottomTile;
    public bool IsGameOver => gameOver;
    public bool HasStarted => hasStarted;
    public bool IsEndingSequence => endingSequence;

    private void Awake()
    {
        audioManager = GetComponent<AudioManager>();
        tileSpawner = GetComponent<TileSpawner>();

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera != null)
        {
            cameraStartPosition = mainCamera.transform.localPosition;
        }

        if (tapEffectCanvas == null)
        {
            tapEffectCanvas = FindObjectOfType<Canvas>();
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
            gameOverPanelAnimator = gameOverPanel.GetComponent<UIPopPanel>();
            gameOverPanelAnimator?.SetInstantState(false);
            if (gameOverDimImage == null)
            {
                Transform dimTransform = gameOverPanel.transform.Find("Dim");
                if (dimTransform != null)
                {
                    gameOverDimImage = dimTransform.GetComponent<Image>();
                }
            }
        }

        if (startPanel != null)
        {
            startPanelAnimator = startPanel.GetComponent<UIPopPanel>();
        }

        if (skipStartPanel)
        {
            hasStarted = true;
            skipStartPanel = false;

            if (startPanel != null)
            {
                startPanel.SetActive(false);
            }

            startPanelAnimator?.SetInstantState(false);

            if (scoreText != null)
            {
                scoreText.enabled = true;
            }
        }
        else
        {
            if (startPanel != null)
            {
                startPanel.SetActive(true);
            }

            startPanelAnimator?.SetInstantState(true);

            if (scoreText != null)
            {
                scoreText.enabled = false;
            }
        }

        UpdateScoreText();
        ApplyCrownDisplay(0);
    }

    private void Update()
    {
        if (!hasStarted || gameOver || endingSequence)
        {
            return;
        }

        UpdateCameraShake();
        HandleEmptySpaceTap();
    }

    public void StartGame()
    {
        if (hasStarted || gameOver)
        {
            return;
        }

        audioManager?.PlaySfx(SfxType.Start);
        hasStarted = true;

        startPanelAnimator?.Hide();
        if (startPanel != null && startPanelAnimator == null)
        {
            startPanel.SetActive(false);
        }

        if (scoreText != null)
        {
            scoreText.enabled = true;
        }

        if (tileSpawner != null)
        {
            tileSpawner.StartSpawning();
        }
    }

    public void RegisterTile(Tile tile)
    {
        if (tile == null || activeTiles.Contains(tile))
        {
            return;
        }

        activeTiles.Add(tile);
        RecalculateCurrentBottomTile();
    }

    public void UnregisterTile(Tile tile)
    {
        if (tile == null)
        {
            return;
        }

        activeTiles.Remove(tile);
        consumedTiles.Remove(tile);
        RecalculateCurrentBottomTile();
    }

    private bool IsTileTappable(Tile tile)
    {
        return tile == currentBottomTile &&
               hitLine != null &&
               tile.transform.position.y <= hitLine.position.y;
    }
    private void SpawnParticleVfx(Vector3 worldPosition)
    {
        if (particleVfxPrefabs == null || particleVfxPrefabs.Length == 0)
            return;

        const int spawnCount = 6;

        for (int i = 0; i < spawnCount; i++)
        {
            GameObject prefab =
                particleVfxPrefabs[Random.Range(0, particleVfxPrefabs.Length)];

            Vector3 randomOffset = new Vector3(
                Random.Range(-0.5f, 0.5f),
                Random.Range(-0.5f, 0.5f),
                0f
            );

            GameObject vfx = Instantiate(
                prefab,
                worldPosition + randomOffset,
                Quaternion.Euler(0f, 0f, Random.Range(0f, 360f))
            );

            Destroy(vfx, particleVfxLifetime);
        }
    }
    public bool TryTapTile(Tile tile)
    {
        if (gameOver || endingSequence || tile == null)
        {
            return false;
        }

        if (IsTileTappable(tile))
        {
            MarkTileConsumed(tile);
            SpawnParticleVfx(tile.transform.position);
            AddScore(tile.transform.position);
            return true;
        }

        ShowInvalidTapFeedback(tile.transform.position);
        return false;
    }
    private void ShowScorePopup(Vector3 tileWorldPosition, int scoreIncrement)
    {
        if (scorePopupPrefab == null || tapEffectCanvas == null)
        {
            return;
        }

        Vector3 popupWorldPosition = tileWorldPosition + Vector3.up * 1.5f;

        Vector2 screenPosition =
            RectTransformUtility.WorldToScreenPoint(mainCamera, popupWorldPosition);

        ScorePopup popup = Instantiate(
            scorePopupPrefab,
            tapEffectCanvas.transform
        );
        popup.Setup(scoreIncrement);
        popup.transform.position = screenPosition;
    }
    public void AddScore(Vector3 tilePosition)
    {
        int scoreIncrement = 100;
        if (gameOver)
        {
            return;
        }

        score += scoreIncrement;
        UpdateScoreText();
        ShowScorePopup(tilePosition, scoreIncrement);

        StartMusicIfNeeded();
    }

    public void StartMusicIfNeeded()
    {
        if (audioManager != null)
        {
            audioManager.Play();
        }
    }

    public void MissTile(Tile tile)
    {
        if (gameOver || endingSequence || tile != currentBottomTile)
        {
            return;
        }
        audioManager?.Stop();
        audioManager?.PlaySfx(SfxType.Miss);
        StartCoroutine(PlayMissSequence(tile));
    }
    public void EndGame()
    {
        audioManager?.PlaySfx(SfxType.GameOver);
        gameOver = true;
        endingSequence = false;

        tileSpawner?.StopSpawning();

        for (int i = activeTiles.Count - 1; i >= 0; i--)
        {
            activeTiles[i].ReturnToPool();
        }

        activeTiles.Clear();
        consumedTiles.Clear();
        currentBottomTile = null;

        if (finalScoreText != null)
        {
            finalScoreText.text = score.ToString();
        }
        ApplyGameOverDim();
        ApplyCrownDisplay(GetCrownCount());

        ShowEndPanel(gameOverPanel);
    }
    public void Restart()
    {
        skipStartPanel = true;
        audioManager?.PlaySfx(SfxType.Start);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ShowInvalidTapFeedback(Vector3 worldPosition)
    {
        shakeTimer = invalidShakeDuration;
        SpawnTapRipple(Input.mousePosition);
    }

    private void RecalculateCurrentBottomTile()
    {
        currentBottomTile = null;

        for (int i = activeTiles.Count - 1; i >= 0; i--)
        {
            Tile tile = activeTiles[i];
            if (tile == null || !tile.gameObject.activeInHierarchy)
            {
                activeTiles.RemoveAt(i);
                continue;
            }

            if (consumedTiles.Contains(tile))
            {
                continue;
            }

            if (currentBottomTile == null || tile.transform.position.y < currentBottomTile.transform.position.y)
            {
                currentBottomTile = tile;
            }
        }
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }
    }

    private void HandleEmptySpaceTap()
    {
        if (gameOver || !Input.GetMouseButtonDown(0))
        {
            return;
        }

        Vector3 pointerWorldPosition = GetPointerWorldPosition();
        Collider2D hit2D = Physics2D.OverlapPoint(pointerWorldPosition);
        if (hit2D != null && hit2D.GetComponentInParent<Tile>() != null)
        {
            return;
        }

        Ray ray = mainCamera != null
            ? mainCamera.ScreenPointToRay(Input.mousePosition)
            : new Ray(pointerWorldPosition + Vector3.back * 10f, Vector3.forward);

        if (Physics.Raycast(ray, out RaycastHit hit3D) && hit3D.collider.GetComponentInParent<Tile>() != null)
        {
            return;
        }

        ShowInvalidTapFeedback(pointerWorldPosition);
    }

    public void SpawnTapRipple(Vector2 screenPosition)
    {
        if (tapRipplePrefab == null || tapEffectCanvas == null)
        {
            return;
        }

        TapRipple ripple = Instantiate(tapRipplePrefab, tapEffectCanvas.transform);
        ripple.Play(screenPosition, tapEffectCanvas);
    }

    private Vector3 GetPointerWorldPosition()
    {
        if (mainCamera == null)
        {
            return Vector3.zero;
        }

        Vector3 screenPosition = Input.mousePosition;
        screenPosition.z = Mathf.Abs(mainCamera.transform.position.z);
        return mainCamera.ScreenToWorldPoint(screenPosition);
    }

    private void UpdateCameraShake()
    {
        if (mainCamera == null)
        {
            return;
        }

        if (shakeTimer <= 0f)
        {
            mainCamera.transform.localPosition = cameraStartPosition;
            return;
        }

        shakeTimer -= Time.deltaTime;
        Vector2 offset = Random.insideUnitCircle * invalidShakeStrength;
        mainCamera.transform.localPosition = cameraStartPosition + new Vector3(offset.x, offset.y, 0f);
    }

    private void ShowEndPanel(GameObject panel)
    {
        if (scoreText != null)
        {
            scoreText.enabled = false;
        }

        if (panel != null)
        {
            UIPopPanel popPanel = panel == gameOverPanel ? gameOverPanelAnimator : panel.GetComponent<UIPopPanel>();
            if (popPanel != null)
            {
                popPanel.Show();
                return;
            }

            panel.SetActive(true);
        }
    }

    private void ApplyGameOverDim()
    {
        if (gameOverDimImage == null)
        {
            return;
        }

        Color color = gameOverDimImage.color;
        color.a = 0.75f;
        gameOverDimImage.color = color;
    }

    private void MarkTileConsumed(Tile tile)
    {
        if (tile == null)
        {
            return;
        }

        consumedTiles.Add(tile);
        RecalculateCurrentBottomTile();
    }

    private IEnumerator PlayMissSequence(Tile missedTile)
    {
        endingSequence = true;

        if (tileSpawner != null)
        {
            tileSpawner.StopSpawning(false);
        }

        float elapsed = 0f;

        while (elapsed < missRevealDuration)
        {
            float deltaTime = Time.deltaTime;
            elapsed += deltaTime;

            float flicker =
                Mathf.Abs(Mathf.Sin(Time.time * missFlickerSpeed));

            Color flickerColor =
                Color.Lerp(missStartColor, missEndColor, flicker);

            if (missedTile != null)
            {
                missedTile.SetSpriteColor(flickerColor);
            }

            for (int i = 0; i < activeTiles.Count; i++)
            {
                Tile tile = activeTiles[i];

                if (tile == null || !tile.gameObject.activeInHierarchy)
                    continue;

                tile.transform.position +=
                    Vector3.up * missRiseSpeed * deltaTime;
            }

            yield return null;
        }

        if (!gameOver)
        {
            EndGame();
        }
    }

    private int GetCrownCount()
    {
        if (audioManager == null || audioManager.TotalNotesCount <= 0)
        {
            return 0;
        }
        float progress = score / (float)audioManager.TotalNotesCount;
        return Mathf.Clamp(Mathf.FloorToInt(progress / 100 * 3f), 0, 3);
    }

    private void ApplyCrownDisplay(int crownCount)
    {
        Color activeColor = Color.white;
        Color inactiveColor = Color.grey;

        Image[] crowns = { crown1Image, crown2Image, crown3Image };

        for (int i = 0; i < crowns.Length; i++)
        {
            if (crowns[i] == null)
            {
                continue;
            }

            crowns[i].color = i < crownCount ? activeColor : inactiveColor;
        }
    }
}
