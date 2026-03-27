using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 音频管理器：负责游戏中所有音频的播放控制
/// 包括背景音乐（BGM）和音效（SFX）的管理
/// </summary>
public class AudioManager : MonoBehaviour
{
    /// <summary>
    /// 音频管理器的单例实例
    /// </summary>
    private static AudioManager instance;
    
    /// <summary>
    /// 获取音频管理器实例
    /// </summary>
    public static AudioManager Instance { get { return instance; } }

    /// <summary>
    /// Awake 方法：初始化音频管理器，创建音频源组件
    /// </summary>
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;

            // 确保场景中有 AudioListener
            if (FindObjectOfType<AudioListener>() == null)
            {
                var audioListener = gameObject.AddComponent<AudioListener>();
                Debug.Log("[AudioManager] AudioListener added to AudioManager GameObject");
            }

            // 创建背景音乐播放源
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true; // 背景音乐默认循环播放
            }

            // 创建音效播放源
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
            }
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 背景音乐播放源（在 Inspector 中可配置）
    /// </summary>
    [SerializeField]
    private AudioSource musicSource;

    /// <summary>
    /// 音效播放源（在 Inspector 中可配置）
    /// </summary>
    [SerializeField]
    private AudioSource sfxSource;

    /// <summary>
    /// 音频剪辑字典：存储已加载的音频剪辑
    /// Key: 音频名称
    /// Value: 音频剪辑对象
    /// </summary>
    private Dictionary<string, AudioClip> audioClips = new Dictionary<string, AudioClip>();

    /// <summary>
    /// 背景音乐音量（0-1）
    /// </summary>
    private float musicVolume = 1.0f;
    
    /// <summary>
    /// 音效音量（0-1）
    /// </summary>
    private float sfxVolume = 1.0f;

    /// <summary>
    /// 加载音频剪辑（通过名称和对象）
    /// </summary>
    /// <param name="name">音频名称，用于后续播放时查找</param>
    /// <param name="clip">音频剪辑对象</param>
    public void LoadAudioClip(string name, AudioClip clip)
    {
        if (clip != null && !audioClips.ContainsKey(name))
        {
            audioClips[name] = clip;
        }
    }

    /// <summary>
    /// 从 Resources 目录加载音频剪辑
    /// </summary>
    /// <param name="path">Resources 目录下的音频路径</param>
    public void LoadAudioClip(string path)
    {
        AudioClip clip = Resources.Load<AudioClip>(path);
        if (clip != null)
        {
            audioClips[path] = clip;
        }
    }

    /// <summary>
    /// 播放背景音乐
    /// </summary>
    /// <param name="clipName">音频剪辑名称</param>
    /// <param name="loop">是否循环播放，默认为 true</param>
    public void PlayMusic(string clipName, bool loop = true)
    {
        if (audioClips.TryGetValue(clipName, out AudioClip clip))
        {
            // 如果当前播放的不是该音乐，则切换
            if (musicSource.clip != clip)
            {
                musicSource.clip = clip;
                musicSource.loop = loop;
                musicSource.Play();
            }
        }
        else
        {
            Debug.LogWarning($"Music clip not found: {clipName}");
        }
    }

    /// <summary>
    /// 停止背景音乐
    /// </summary>
    public void StopMusic()
    {
        musicSource.Stop();
    }

    /// <summary>
    /// 暂停背景音乐
    /// </summary>
    public void PauseMusic()
    {
        musicSource.Pause();
    }

    /// <summary>
    /// 恢复背景音乐播放
    /// </summary>
    public void ResumeMusic()
    {
        musicSource.UnPause();
    }

    /// <summary>
    /// 播放音效（一次性播放，不循环）
    /// </summary>
    /// <param name="clipName">音频剪辑名称</param>
    public void PlaySFX(string clipName)
    {
        if (audioClips.TryGetValue(clipName, out AudioClip clip))
        {
            // 使用 PlayOneShot 播放音效，可以同时播放多个音效
            sfxSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning($"SFX clip not found: {clipName}");
        }
    }

    /// <summary>
    /// 设置背景音乐音量
    /// </summary>
    /// <param name="volume">音量值（0-1 之间）</param>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        musicSource.volume = musicVolume;
    }

    /// <summary>
    /// 设置音效音量
    /// </summary>
    /// <param name="volume">音量值（0-1 之间）</param>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        sfxSource.volume = sfxVolume;
    }

    /// <summary>
    /// 获取当前背景音乐音量
    /// </summary>
    public float GetMusicVolume()
    {
        return musicVolume;
    }

    /// <summary>
    /// 获取当前音效音量
    /// </summary>
    public float GetSFXVolume()
    {
        return sfxVolume;
    }

    /// <summary>
    /// 卸载指定的音频剪辑
    /// </summary>
    /// <param name="name">音频名称</param>
    public void UnloadAudioClip(string name)
    {
        if (audioClips.ContainsKey(name))
        {
            audioClips.Remove(name);
        }
    }

    /// <summary>
    /// 卸载所有音频剪辑，释放内存
    /// </summary>
    public void UnloadAllAudioClips()
    {
        audioClips.Clear();
    }
}
