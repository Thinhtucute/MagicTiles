using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileSpawner : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private Tile tilePrefab;
    [SerializeField] private Transform[] lanes;
    [SerializeField] private Transform spawnLine;
    [SerializeField] private Transform despawnLine;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 0.65f;
    [SerializeField] private float tileMoveSpeed = 5f;
    [SerializeField] private int initialPoolSize = 24;
    [SerializeField] private bool startAutomatically = true;

    private readonly Queue<Tile> tilePool = new();
    private Coroutine spawnRoutine;
    private bool isSpawning;
    private int nextBeatmapNoteIndex;
    private double spawnStartDspTime;
    private int activeSpawnedTiles;
    private bool beatmapExhausted;

    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager = GetComponent<GameManager>();
        }

        if (audioManager == null)
        {
            audioManager = GetComponent<AudioManager>();
        }

        ResolveSceneReferences();
        CreatePool();
    }

    private void Start()
    {
        if (startAutomatically && gameManager != null && gameManager.HasStarted)
        {
            StartSpawning();
        }
    }

    public void StartSpawning()
    {
        if (isSpawning)
        {
            return;
        }

        isSpawning = true;
        nextBeatmapNoteIndex = 0;
        spawnStartDspTime = AudioSettings.dspTime;
        activeSpawnedTiles = 0;
        beatmapExhausted = false;

        if (audioManager != null && audioManager.HasBeatmap)
        {
            spawnRoutine = StartCoroutine(BeatmapSpawnLoop());
            return;
        }

        spawnRoutine = StartCoroutine(FallbackSpawnLoop());
    }

    public void StopSpawning(bool stopAudio = true)
    {
        isSpawning = false;

        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        activeSpawnedTiles = 0;
        beatmapExhausted = false;

        if (stopAudio && audioManager != null)
        {
            audioManager.Stop();
        }
    }

    public void ReturnTile(Tile tile)
    {
        if (tile == null)
        {
            return;
        }

        if (activeSpawnedTiles > 0)
        {
            activeSpawnedTiles--;
        }

        tile.gameObject.SetActive(false);
        tile.transform.SetParent(transform);
        tilePool.Enqueue(tile);

        TryFinishBeatmap();
    }

    private IEnumerator BeatmapSpawnLoop()
    {
        while (isSpawning)
        {
            if (!CanSpawnTile())
            {
                yield return null;
                continue;
            }

            if (nextBeatmapNoteIndex >= audioManager.Notes.Count)
            {
                beatmapExhausted = true;
                TryFinishBeatmap();
                yield break;
            }

            BeatmapNote note = audioManager.Notes[nextBeatmapNoteIndex];
            double noteDspTime = spawnStartDspTime + note.time;

            if (AudioSettings.dspTime < noteDspTime)
            {
                yield return null;
                continue;
            }

            SpawnTileAtLane(note.lane);
            nextBeatmapNoteIndex++;
        }
    }

    private IEnumerator FallbackSpawnLoop()
    {
        while (isSpawning)
        {
            SpawnTileAtLane(Random.Range(0, lanes.Length));
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnTileAtLane(int laneIndex)
    {
        if (!CanSpawnTile())
        {
            return;
        }

        laneIndex = Mathf.Clamp(laneIndex, 0, lanes.Length - 1);
        Transform lane = lanes[laneIndex];
        Tile tile = GetTileFromPool();
        Vector3 spawnPosition = lane.position;

        if (spawnLine != null)
        {
            spawnPosition.y = spawnLine.position.y;
        }

        tile.transform.SetParent(lane);
        tile.transform.position = spawnPosition;
        tile.transform.rotation = Quaternion.identity;
        tile.gameObject.SetActive(true);
        tile.Initialize(gameManager, this, despawnLine, tileMoveSpeed);

        activeSpawnedTiles++;
        gameManager.RegisterTile(tile);
    }

    private bool CanSpawnTile()
    {
        if (gameManager == null || gameManager.IsGameOver)
        {
            return false;
        }

        if (tilePrefab == null)
        {
            return false;
        }

        if (lanes == null || lanes.Length == 0)
        {
            return false;
        }

        return true;
    }

    private Tile GetTileFromPool()
    {
        if (tilePool.Count > 0)
        {
            return tilePool.Dequeue();
        }

        Tile tile = Instantiate(tilePrefab, transform);
        tile.gameObject.SetActive(false);
        return tile;
    }

    private void CreatePool()
    {
        if (tilePrefab == null)
        {
            return;
        }

        for (int i = 0; i < initialPoolSize; i++)
        {
            Tile tile = Instantiate(tilePrefab, transform);
            tile.gameObject.SetActive(false);
            tilePool.Enqueue(tile);
        }
    }

    private void ResolveSceneReferences()
    {
        if ((lanes == null || lanes.Length == 0) && GameObject.Find("LaneContainer") is GameObject laneContainer)
        {
            lanes = new Transform[laneContainer.transform.childCount];
            for (int i = 0; i < laneContainer.transform.childCount; i++)
            {
                lanes[i] = laneContainer.transform.GetChild(i);
            }
        }

        if (spawnLine == null && GameObject.Find("SpawnLine") is GameObject foundSpawnLine)
        {
            spawnLine = foundSpawnLine.transform;
        }

        if (despawnLine == null && GameObject.Find("DespawnLine") is GameObject foundDespawnLine)
        {
            despawnLine = foundDespawnLine.transform;
        }
    }

    private void TryFinishBeatmap()
    {
        if (!isSpawning || !beatmapExhausted || activeSpawnedTiles > 0 || gameManager == null || gameManager.IsGameOver)
        {
            return;
        }

        gameManager.EndGame();
    }
}
