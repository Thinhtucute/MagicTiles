using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BeatmapNote
{
    public float time;
    public int lane;
}

[Serializable]
public class Beatmap
{
    public BeatmapNote[] notes;
}

public enum SfxType
{
    Start,
    Miss,
    GameOver
}

public class AudioManager : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip musicClip;
    [SerializeField] private TextAsset beatmapJson;

    [Header("SFX")]
    private AudioSource sfxSource;

    [SerializeField] private AudioClip startSfx;
    [SerializeField] private AudioClip missSfx;
    [SerializeField] private AudioClip gameOverSfx;
    private readonly List<BeatmapNote> notes = new();
    private double scheduledStartDspTime = -1d;
    private bool isPlaying;

    public IReadOnlyList<BeatmapNote> Notes => notes;
    public int TotalNotesCount => notes.Count;
    public bool HasBeatmap => notes.Count > 0;
    public bool IsPlaying => isPlaying;
    public double ScheduledStartDspTime => scheduledStartDspTime;
    public bool HasScheduledStart => scheduledStartDspTime >= 0d;
    public float SongTime => isPlaying && scheduledStartDspTime >= 0d
        ? Mathf.Max(0f, (float)(AudioSettings.dspTime - scheduledStartDspTime))
        : 0f;

    private void Awake()
    {
        if (musicSource == null)
        {
            musicSource = GetComponent<AudioSource>();
        }

        if (musicSource != null)
        {
            musicSource.playOnAwake = false;
            musicSource.Stop();
        }

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;

        LoadBeatmap();
    }

    public void LoadBeatmap()
    {
        notes.Clear();

        if (beatmapJson == null)
        {
            return;
        }

        Beatmap beatmap = ParseBeatmap(beatmapJson.text);
        if (beatmap?.notes == null || beatmap.notes.Length == 0)
        {
            return;
        }

        notes.AddRange(beatmap.notes);
        notes.Sort((left, right) => left.time.CompareTo(right.time));
    }

    public void Play()
    {
        if (isPlaying || musicSource == null || musicClip == null)
        {
            return;
        }

        scheduledStartDspTime = AudioSettings.dspTime;
        musicSource.clip = musicClip;
        musicSource.Play();
        isPlaying = true;
    }

    public void Stop()
    {
        isPlaying = false;
        scheduledStartDspTime = -1d;

        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    public void PlaySfx(SfxType sfxType)
    {
        if (sfxSource == null)
        {
            return;
        }

        AudioClip clip = sfxType switch
        {
            SfxType.Start => startSfx,
            SfxType.Miss => missSfx,
            SfxType.GameOver => gameOverSfx,
            _ => null
        };

        if (clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    private Beatmap ParseBeatmap(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        string trimmed = json.Trim();
        string wrappedJson = trimmed.StartsWith("{", StringComparison.Ordinal)
            ? trimmed
            : "{\"notes\":" + trimmed + "}";

        return JsonUtility.FromJson<Beatmap>(wrappedJson);
    }
}
