using System.Collections.Generic;
using UnityEngine;

namespace EggRogue
{
    /// <summary>
    /// EggRogue 专用的音频管理器（音效 + 后续可扩展的背景音乐）。
    /// 全局单例；不使用 DontDestroyOnLoad，切场景后由 Instance 自动查找或创建新实例，避免 Unity 断言。
    /// </summary>
    public class EggRogueAudioManager : MonoBehaviour
    {
        /// <summary> 用于「挥砍音效」等短音限流的分组名，窗口内最多播放次数见 meleeSwingMaxPerWindow。 </summary>
        public const string CategoryMeleeSwing = "MeleeSwing";

        private static EggRogueAudioManager _instance;
        public static EggRogueAudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    // 优先查找场景中已存在的实例，避免重复创建
                    _instance = FindObjectOfType<EggRogueAudioManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("EggRogueAudioManager");
                        _instance = go.AddComponent<EggRogueAudioManager>();
                    }
                }
                return _instance;
            }
        }

        [Header("按钮点击音效")]
        [Tooltip("所有 UI 按钮点击时播放的音效")]
        public AudioClip buttonClickClip;

        [Range(0f, 1f)]
        [Tooltip("按钮点击音效音量")]
        public float buttonClickVolume = 1f;

        [Header("短音限流（挥砍等）")]
        [Tooltip("挥砍类音效：在此时间窗口（秒）内最多播放次数")]
        public float meleeSwingWindowSec = 0.1f;

        [Tooltip("挥砍类音效：窗口内最多播放几次")]
        public int meleeSwingMaxPerWindow = 2;

        [Header("全局音效设置")]
        [Range(0f, 1f)]
        [Tooltip("全局音效音量（乘在各类音效之上）")]
        public float masterSfxVolume = 1f;

        [Tooltip("是否启用音效（关闭后不再播放任何音效）")]
        public bool enableSoundEffects = true;

        private AudioSource _sfxSource;
        private readonly Dictionary<string, List<float>> _categoryPlayTimes = new Dictionary<string, List<float>>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            if (_sfxSource == null)
            {
                _sfxSource = gameObject.AddComponent<AudioSource>();
                _sfxSource.playOnAwake = false;
                _sfxSource.loop = false;
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        /// <summary>
        /// 播放统一的按钮点击音效（供全局 UI 使用）。
        /// </summary>
        public void PlayButtonClick()
        {
            if (!enableSoundEffects) return;
            if (_sfxSource == null || buttonClickClip == null) return;

            float finalVolume = buttonClickVolume * masterSfxVolume;
            _sfxSource.PlayOneShot(buttonClickClip, finalVolume);
        }

        /// <summary>
        /// 播放任意音效（需要自行传入 AudioClip）。
        /// </summary>
        public void PlaySfx(AudioClip clip, float volume = 1f)
        {
            if (!enableSoundEffects) return;
            if (_sfxSource == null || clip == null) return;

            float finalVolume = volume * masterSfxVolume;
            _sfxSource.PlayOneShot(clip, finalVolume);
        }

        /// <summary>
        /// 在指定分类下按「时间窗口 + 最大次数」限流播放。适合挥砍、脚步等短音，多武器同时触发时不会叠成一片。
        /// </summary>
        /// <param name="clip">音效</param>
        /// <param name="volume">音量 0~1</param>
        /// <param name="category">分类名，如 CategoryMeleeSwing</param>
        /// <param name="windowSec">时间窗口（秒）</param>
        /// <param name="maxPerWindow">窗口内最多播放次数，超过则本次不播</param>
        /// <returns>是否实际播放了</returns>
        public bool PlaySfxLimited(AudioClip clip, float volume, string category, float windowSec, int maxPerWindow)
        {
            if (!enableSoundEffects || _sfxSource == null || clip == null) return false;

            float now = Time.time;
            if (!_categoryPlayTimes.TryGetValue(category, out var times))
            {
                times = new List<float>();
                _categoryPlayTimes[category] = times;
            }

            // 去掉窗口外的记录
            while (times.Count > 0 && times[0] < now - windowSec)
                times.RemoveAt(0);

            if (times.Count >= maxPerWindow)
                return false;

            times.Add(now);
            float finalVolume = volume * masterSfxVolume;
            _sfxSource.PlayOneShot(clip, finalVolume);
            return true;
        }

        /// <summary>
        /// 播放挥砍音效（使用内置限流：窗口内最多 meleeSwingMaxPerWindow 次）。
        /// </summary>
        public void PlayMeleeSwing(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            PlaySfxLimited(clip, volume, CategoryMeleeSwing, meleeSwingWindowSec, meleeSwingMaxPerWindow);
        }
    }
}

