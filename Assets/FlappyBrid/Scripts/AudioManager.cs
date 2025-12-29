using UnityEngine;

/// <summary>
/// 音频管理器
/// 统一管理游戏中的所有音频播放（包括音效和背景音乐）
/// </summary>
public class AudioManager : MonoBehaviour
{
    private static AudioManager instance;
    public static AudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("AudioManager");
                instance = go.AddComponent<AudioManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }
    
    [Header("按钮音效")]
    [Tooltip("按钮点击音效")]
    public AudioClip buttonClickSound;
    [Tooltip("按钮点击音效音量（0-1）")]
    [Range(0f, 1f)]
    public float buttonClickVolume = 1f;
    
    [Header("游戏音效")]
    [Tooltip("小鸟飞行音效（点击屏幕时播放）")]
    public AudioClip birdFlapSound;
    [Tooltip("小鸟飞行音效音量（0-1）")]
    [Range(0f, 1f)]
    public float birdFlapVolume = 1f;
    
    [Tooltip("碰到道具音效")]
    public AudioClip itemCollectSound;
    [Tooltip("碰到道具音效音量（0-1）")]
    [Range(0f, 1f)]
    public float itemCollectVolume = 1f;
    
    [Tooltip("死亡音效")]
    public AudioClip deathSound;
    [Tooltip("死亡音效音量（0-1）")]
    [Range(0f, 1f)]
    public float deathVolume = 1f;
    
    [Tooltip("通过关卡音效")]
    public AudioClip levelCompleteSound;
    [Tooltip("通过关卡音效音量（0-1）")]
    [Range(0f, 1f)]
    public float levelCompleteVolume = 1f;
    
    [Header("音效设置")]
    [Tooltip("全局音效音量（0-1，影响所有音效）")]
    [Range(0f, 1f)]
    public float soundEffectVolume = 1f;
    
    [Tooltip("是否启用音效")]
    public bool enableSoundEffects = true;
    
    [Header("背景音乐设置")]
    [Tooltip("背景音乐音量（0-1）")]
    [Range(0f, 1f)]
    public float backgroundMusicVolume = 0.5f;
    
    [Tooltip("是否启用背景音乐")]
    public bool enableBackgroundMusic = true;
    
    [Tooltip("背景音乐淡入淡出时间（秒）")]
    [Range(0f, 2f)]
    public float musicFadeTime = 0.5f;
    
    private AudioSource soundEffectSource;  // 音效AudioSource
    private AudioSource backgroundMusicSource;  // 背景音乐AudioSource
    private AudioClip currentBackgroundMusic;  // 当前背景音乐
    private Coroutine musicFadeCoroutine;  // 音乐淡入淡出协程
    
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        // 创建音效AudioSource组件
        soundEffectSource = gameObject.AddComponent<AudioSource>();
        soundEffectSource.playOnAwake = false;
        soundEffectSource.loop = false;
        soundEffectSource.volume = soundEffectVolume;
        
        // 创建背景音乐AudioSource组件
        backgroundMusicSource = gameObject.AddComponent<AudioSource>();
        backgroundMusicSource.playOnAwake = false;
        backgroundMusicSource.loop = true;  // 背景音乐循环播放
        backgroundMusicSource.volume = backgroundMusicVolume;
    }
    
    /// <summary>
    /// 播放按钮点击音效
    /// </summary>
    public void PlayButtonClickSound()
    {
        if (!enableSoundEffects) return;
        if (buttonClickSound != null && soundEffectSource != null)
        {
            float finalVolume = buttonClickVolume * soundEffectVolume;
            soundEffectSource.PlayOneShot(buttonClickSound, finalVolume);
        }
    }
    
    /// <summary>
    /// 播放小鸟飞行音效
    /// </summary>
    public void PlayBirdFlapSound()
    {
        if (!enableSoundEffects) return;
        if (birdFlapSound != null && soundEffectSource != null)
        {
            float finalVolume = birdFlapVolume * soundEffectVolume;
            soundEffectSource.PlayOneShot(birdFlapSound, finalVolume);
        }
    }
    
    /// <summary>
    /// 播放道具收集音效
    /// </summary>
    public void PlayItemCollectSound()
    {
        if (!enableSoundEffects) return;
        if (itemCollectSound != null && soundEffectSource != null)
        {
            float finalVolume = itemCollectVolume * soundEffectVolume;
            soundEffectSource.PlayOneShot(itemCollectSound, finalVolume);
        }
    }
    
    /// <summary>
    /// 播放死亡音效
    /// </summary>
    public void PlayDeathSound()
    {
        if (!enableSoundEffects) return;
        if (deathSound != null && soundEffectSource != null)
        {
            float finalVolume = deathVolume * soundEffectVolume;
            soundEffectSource.PlayOneShot(deathSound, finalVolume);
        }
    }
    
    /// <summary>
    /// 播放通过关卡音效
    /// </summary>
    public void PlayLevelCompleteSound()
    {
        if (!enableSoundEffects) return;
        if (levelCompleteSound != null && soundEffectSource != null)
        {
            float finalVolume = levelCompleteVolume * soundEffectVolume;
            soundEffectSource.PlayOneShot(levelCompleteSound, finalVolume);
        }
    }
    
    /// <summary>
    /// 播放自定义音效
    /// </summary>
    public void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (!enableSoundEffects) return;
        if (clip != null && soundEffectSource != null)
        {
            soundEffectSource.PlayOneShot(clip, volume * soundEffectVolume);
        }
    }
    
    /// <summary>
    /// 设置音效音量
    /// </summary>
    public void SetSoundEffectVolume(float volume)
    {
        soundEffectVolume = Mathf.Clamp01(volume);
        if (soundEffectSource != null)
        {
            soundEffectSource.volume = soundEffectVolume;
        }
    }
    
    /// <summary>
    /// 启用/禁用音效
    /// </summary>
    public void SetSoundEffectsEnabled(bool enabled)
    {
        enableSoundEffects = enabled;
    }
    
    /// <summary>
    /// 播放背景音乐
    /// </summary>
    public void PlayBackgroundMusic(AudioClip musicClip, bool fadeIn = true)
    {
        if (!enableBackgroundMusic) return;
        if (musicClip == null) return;
        
        // 如果正在播放相同的音乐，不重复播放
        if (currentBackgroundMusic == musicClip && backgroundMusicSource.isPlaying)
        {
            return;
        }
        
        currentBackgroundMusic = musicClip;
        
        if (backgroundMusicSource != null)
        {
            // 停止之前的淡入淡出协程
            if (musicFadeCoroutine != null)
            {
                StopCoroutine(musicFadeCoroutine);
            }
            
            if (fadeIn && backgroundMusicSource.isPlaying)
            {
                // 淡出旧音乐，然后淡入新音乐
                musicFadeCoroutine = StartCoroutine(FadeOutAndPlayNewMusic(musicClip));
            }
            else
            {
                // 直接播放新音乐
                backgroundMusicSource.clip = musicClip;
                backgroundMusicSource.volume = backgroundMusicVolume;
                backgroundMusicSource.Play();
            }
        }
    }
    
    /// <summary>
    /// 停止背景音乐
    /// </summary>
    public void StopBackgroundMusic(bool fadeOut = true)
    {
        if (backgroundMusicSource == null || !backgroundMusicSource.isPlaying) return;
        
        if (musicFadeCoroutine != null)
        {
            StopCoroutine(musicFadeCoroutine);
        }
        
        if (fadeOut)
        {
            musicFadeCoroutine = StartCoroutine(FadeOutMusic());
        }
        else
        {
            backgroundMusicSource.Stop();
            currentBackgroundMusic = null;
        }
    }
    
    /// <summary>
    /// 暂停背景音乐
    /// </summary>
    public void PauseBackgroundMusic()
    {
        if (backgroundMusicSource != null && backgroundMusicSource.isPlaying)
        {
            backgroundMusicSource.Pause();
        }
    }
    
    /// <summary>
    /// 恢复背景音乐
    /// </summary>
    public void ResumeBackgroundMusic()
    {
        if (backgroundMusicSource != null && !backgroundMusicSource.isPlaying)
        {
            backgroundMusicSource.UnPause();
        }
    }
    
    /// <summary>
    /// 设置背景音乐音量
    /// </summary>
    public void SetBackgroundMusicVolume(float volume)
    {
        backgroundMusicVolume = Mathf.Clamp01(volume);
        if (backgroundMusicSource != null)
        {
            backgroundMusicSource.volume = backgroundMusicVolume;
        }
    }
    
    /// <summary>
    /// 启用/禁用背景音乐
    /// </summary>
    public void SetBackgroundMusicEnabled(bool enabled)
    {
        enableBackgroundMusic = enabled;
        if (!enabled)
        {
            StopBackgroundMusic(false);
        }
        else if (currentBackgroundMusic != null)
        {
            PlayBackgroundMusic(currentBackgroundMusic, false);
        }
    }
    
    /// <summary>
    /// 淡出旧音乐并播放新音乐
    /// </summary>
    private System.Collections.IEnumerator FadeOutAndPlayNewMusic(AudioClip newMusic)
    {
        // 淡出当前音乐
        float startVolume = backgroundMusicSource.volume;
        float elapsed = 0f;
        
        while (elapsed < musicFadeTime)
        {
            elapsed += Time.deltaTime;
            backgroundMusicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / musicFadeTime);
            yield return null;
        }
        
        // 切换音乐
        backgroundMusicSource.clip = newMusic;
        backgroundMusicSource.volume = 0f;
        backgroundMusicSource.Play();
        
        // 淡入新音乐
        elapsed = 0f;
        while (elapsed < musicFadeTime)
        {
            elapsed += Time.deltaTime;
            backgroundMusicSource.volume = Mathf.Lerp(0f, backgroundMusicVolume, elapsed / musicFadeTime);
            yield return null;
        }
        
        backgroundMusicSource.volume = backgroundMusicVolume;
        musicFadeCoroutine = null;
    }
    
    /// <summary>
    /// 淡出音乐
    /// </summary>
    private System.Collections.IEnumerator FadeOutMusic()
    {
        float startVolume = backgroundMusicSource.volume;
        float elapsed = 0f;
        
        while (elapsed < musicFadeTime)
        {
            elapsed += Time.deltaTime;
            backgroundMusicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / musicFadeTime);
            yield return null;
        }
        
        backgroundMusicSource.Stop();
        currentBackgroundMusic = null;
        musicFadeCoroutine = null;
    }
}

