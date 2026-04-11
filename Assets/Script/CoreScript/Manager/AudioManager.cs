using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 音频管理器Simple
/// 2026/1/15 LE
/// </summary>
public class AudioManager : MonoSingleton<AudioManager>
{
    // 路径常量
    private const string BGM_PATH = "Audio/BGM/";
    private const string EFFECT_PATH = "Audio/Effect/";
    private const string MIXER_PATH = "Audio/Mixer/MainMixers";

    // 核心组件
    private AudioSource bgmSource;
    private AudioMixerGroup effectMixerGroup;
    private AudioMixerGroup bgmMixerGroup;

    // 动态对象池
    private readonly List<AudioSource> effectSourcesPool = new();

    // 资源缓存
    private readonly Dictionary<string, AudioClip> clipCache = new();

    // ==================== 音量/静音 → DataManager.Settings ====================

    private SettingsData S => DataManager.Instance.Settings;

    public float GlobalBgmVolume => S.bgmVolume;
    public float GlobalEffectVolume => S.effectVolume;
    public bool IsBgmMuted => S.isBgmMuted;
    public bool IsEffectMuted => S.isEffectMuted;

    protected void Awake()
    {
        InitComponents();
        ApplySettingsToMixer();
    }

    private void InitComponents()
    {
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }

        // 尝试加载 Mixer
        var mixer = Resources.Load<AudioMixer>(MIXER_PATH);
        if (mixer != null)
        {
            var bgmGroups = mixer.FindMatchingGroups("BGM");
            var effectGroups = mixer.FindMatchingGroups("Effect");

            if (bgmGroups.Length > 0)
            {
                bgmMixerGroup = bgmGroups[0];
                bgmSource.outputAudioMixerGroup = bgmMixerGroup;
            }

            if (effectGroups.Length > 0)
            {
                effectMixerGroup = effectGroups[0];
            }
        }
        else
        {
            Debug.LogWarning("[AudioManager] 未加载到 Mixer，将使用直接音量控制模式。");
        }
    }

    /// <summary>
    /// 从 Settings 恢复音量到 Mixer / Source
    /// </summary>
    private void ApplySettingsToMixer()
    {
        SetBGMVolume(S.bgmVolume);
        SetEffectVolume(S.effectVolume);
    }

    #region BGM Control

    public void PlayBGM(string name, float fadeDuration = 1.0f)
    {
        if (string.IsNullOrEmpty(name)) return;

        var clip = LoadClip(name, true);
        if (clip == null) return;

        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        StartCoroutine(FadeSwitchBGM(clip, fadeDuration));
    }

    private IEnumerator FadeSwitchBGM(AudioClip newClip, float duration)
    {
        var targetVolume = (bgmMixerGroup != null) ? 1f : S.bgmVolume;

        if (bgmSource.isPlaying && duration > 0)
        {
            var startVol = bgmSource.volume;
            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                bgmSource.volume = Mathf.Lerp(startVol, 0, t / duration);
                yield return null;
            }
        }

        bgmSource.Stop();
        bgmSource.clip = newClip;
        bgmSource.volume = 0;
        bgmSource.Play();

        if (duration > 0)
        {
            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                bgmSource.volume = Mathf.Lerp(0, targetVolume, t / duration);
                yield return null;
            }
        }
        bgmSource.volume = targetVolume;
    }

    public void StopBGM() => bgmSource.Stop();
    public void PauseBGM() => bgmSource.Pause();
    public void ResumeBGM() => bgmSource.UnPause();

    #endregion

    #region Effect Control

    /// <summary>
    /// 播放音效
    /// </summary>
    /// <param name="name">音效文件名</param>
    /// <param name="volumeScale">音量缩放 (0~1)</param>
    /// <param name="pitch">音调 (1为原声，0.8-1.2常用于随机化)</param>
    public void PlayEffect(string name, float volumeScale = 1f, float pitch = 1f)
    {
        if (IsEffectMuted) return;

        var clip = LoadClip(name, false);
        if (clip == null) return;

        var source = GetAvailableSource();
        source.pitch = pitch;
        source.clip = clip;

        if (effectMixerGroup != null)
        {
            source.volume = volumeScale;
        }
        else
        {
            source.volume = S.effectVolume * volumeScale;
        }

        source.Play();
    }

    public void PlayEffectRandom(string name)
    {
        PlayEffect(name, 1f, Random.Range(0.9f, 1.1f));
    }

    private AudioSource GetAvailableSource()
    {
        foreach (var source in effectSourcesPool)
        {
            if (!source.isPlaying) return source;
        }
        var newSource = gameObject.AddComponent<AudioSource>();
        newSource.outputAudioMixerGroup = effectMixerGroup;
        newSource.playOnAwake = false;
        effectSourcesPool.Add(newSource);

        return newSource;
    }

    #endregion

    #region 3D Effect Control (空间音效)

    public void PlayEffectAt(string name, Vector3 position, GameObject prefab = null, float volumeScale = 1f)
    {
        if (IsEffectMuted) return;
        var clip = LoadClip(name, false);
        if (clip == null) return;

        CreateAndPlay3DSource(clip, position, null, prefab, volumeScale);
    }

    public void PlayEffectFollow(string name, Transform target, GameObject prefab = null, float volumeScale = 1f)
    {
        if (IsEffectMuted) return;
        var clip = LoadClip(name, false);
        if (clip == null) return;

        CreateAndPlay3DSource(clip, target.position, target, prefab, volumeScale);
    }

    private void CreateAndPlay3DSource(AudioClip clip, Vector3 pos, Transform parent, GameObject prefab, float volume)
    {
        GameObject audioObj;
        AudioSource source;

        if (prefab != null)
        {
            audioObj = Instantiate(prefab, pos, Quaternion.identity);
            source = audioObj.GetComponent<AudioSource>();
            if (source == null)
            {
                Debug.LogWarning($"[AudioManager] 传入的 Prefab '{prefab.name}' 缺少 AudioSource 组件，已自动添加。");
                source = audioObj.AddComponent<AudioSource>();
            }
        }
        else
        {
            audioObj = new GameObject($"TempSFX_{clip.name}");
            audioObj.transform.position = pos;
            source = audioObj.AddComponent<AudioSource>();

            source.spatialBlend = 1.0f;
            source.minDistance = 2f;
            source.maxDistance = 20f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
        }

        if (parent != null)
        {
            audioObj.transform.SetParent(parent);
            audioObj.transform.localPosition = Vector3.zero;
        }

        source.clip = clip;
        source.outputAudioMixerGroup = effectMixerGroup;

        if (effectMixerGroup != null)
        {
            source.volume = volume;
        }
        else
        {
            source.volume = S.effectVolume * volume;
        }

        source.Play();

        Destroy(audioObj, clip.length + 0.1f);
    }

    #endregion

    #region Resource Management

    private AudioClip LoadClip(string name, bool isBgm)
    {
        if (clipCache.TryGetValue(name, out var cachedClip))
        {
            return cachedClip;
        }

        string path = (isBgm ? BGM_PATH : EFFECT_PATH) + name;
        var clip = Resources.Load<AudioClip>(path);

        if (clip == null)
        {
            Debug.LogError($"[AudioManager] 找不到音频文件: {path}");
            return null;
        }

        clipCache.Add(name, clip);
        return clip;
    }

    public void ClearCache()
    {
        clipCache.Clear();
    }

    #endregion

    #region Volume Control

    public void SetBGMVolume(float volume)
    {
        S.bgmVolume = volume;

        if (bgmMixerGroup != null)
        {
            var db = volume <= 0.0001f ? -80f : Mathf.Log10(volume) * 20;
            bool result = bgmMixerGroup.audioMixer.SetFloat("BGMVolume", db);
            if (!result) Debug.LogWarning("请确保AudioMixer中已Expose名为'BGMVolume'的参数");
        }
        else
        {
            bgmSource.volume = volume;
        }
    }

    public void SetEffectVolume(float volume)
    {
        S.effectVolume = volume;

        if (effectMixerGroup != null)
        {
            var db = volume <= 0.0001f ? -80f : Mathf.Log10(volume) * 20;
            effectMixerGroup.audioMixer.SetFloat("EffectVolume", db);
        }
        else
        {
            foreach (var s in effectSourcesPool)
            {
                s.volume = volume;
            }
        }
    }

    public void SetBgmMuted(bool muted)
    {
        S.isBgmMuted = muted;
        bgmSource.mute = muted;
    }

    public void SetEffectMuted(bool muted)
    {
        S.isEffectMuted = muted;
    }

    #endregion
}
