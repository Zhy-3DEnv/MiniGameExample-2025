using UnityEngine;

namespace EggRogue
{
    /// <summary>
    /// UI 字体配置 - 统一管理项目默认字体（如 simhei.ttf）。
    /// 资源需放在 Resources/UIFontConfig.asset。
    /// </summary>
    [CreateAssetMenu(fileName = "UIFontConfig", menuName = "EggRogue/UI Font Config", order = 0)]
    public class UIFontConfig : ScriptableObject
    {
        [Tooltip("默认 UI 字体（如 simhei.ttf）")]
        public Font defaultFont;
    }
}
