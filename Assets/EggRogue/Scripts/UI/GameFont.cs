using UnityEngine;
using UnityEngine.UI;

namespace EggRogue
{
    /// <summary>
    /// 项目统一字体工具 - 默认使用 simhei.ttf，从 UIFontConfig 加载。
    /// </summary>
    public static class GameFont
    {
        private static Font _cached;
        private static UIFontConfig _config;

        /// <summary>获取项目默认字体，若未配置则回退到内置字体</summary>
        public static Font GetDefault()
        {
            if (_cached != null) return _cached;

            _config = Resources.Load<UIFontConfig>("UIFontConfig");
            if (_config != null && _config.defaultFont != null)
            {
                _cached = _config.defaultFont;
                return _cached;
            }

            _cached = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            return _cached;
        }

        /// <summary>为 Text 组件设置默认字体</summary>
        public static void ApplyTo(Text text)
        {
            if (text == null) return;
            var font = GetDefault();
            if (font != null) text.font = font;
        }
    }
}
