using UnityEngine;
using UnityEngine.Rendering;

namespace EggRogue
{
    /// <summary>
    /// 解决编辑器中从 PersistentScene 进入 GameScene 后环境光不生效的问题。
    /// 原因：通过 LoadScene 动态加载场景时，编辑器有时不会正确应用新场景的 RenderSettings（环境光）。
    /// 打包后正常，仅编辑器 Play 时会出现。修复逻辑仅在 UNITY_EDITOR 下执行，打包（含微信小游戏）后不会运行，无额外开销。
    /// </summary>
    public class SceneEnvironmentLightingFix : MonoBehaviour
    {
        [Header("环境光（与 Lighting 窗口 / 场景中设置保持一致）")]
        [Tooltip("与环境光模式一致：0=Skybox, 1=Gradient, 2=Color, 3=Flat")]
        [Range(0, 3)]
        public int ambientMode = 0;

        [Tooltip("Skybox 模式下作为备用；Gradient/Color/Flat 下直接使用")]
        public Color ambientSkyColor = new Color(0.212f, 0.227f, 0.259f, 1f);

        public Color ambientEquatorColor = new Color(0.114f, 0.125f, 0.133f, 1f);
        public Color ambientGroundColor = new Color(0.047f, 0.043f, 0.035f, 1f);

        [Range(0f, 2f)]
        public float ambientIntensity = 1f;

        [Tooltip("勾选则在加载后延迟一帧再应用并刷新环境，用于编辑器下覆盖首帧错误状态")]
        public bool delayOneFrame = true;

        private void Start()
        {
#if UNITY_EDITOR
            if (delayOneFrame)
                StartCoroutine(ApplyNextFrame());
            else
                Apply();
#endif
        }

#if UNITY_EDITOR
        private System.Collections.IEnumerator ApplyNextFrame()
        {
            yield return null;
            Apply();
        }

        private void Apply()
        {
            RenderSettings.ambientMode = (AmbientMode)Mathf.Clamp(ambientMode, 0, 3);
            RenderSettings.ambientSkyColor = ambientSkyColor;
            RenderSettings.ambientEquatorColor = ambientEquatorColor;
            RenderSettings.ambientGroundColor = ambientGroundColor;
            RenderSettings.ambientIntensity = ambientIntensity;

            if (RenderSettings.ambientMode == AmbientMode.Skybox && RenderSettings.skybox != null)
            {
                DynamicGI.UpdateEnvironment();
            }
        }
#endif
    }
}
