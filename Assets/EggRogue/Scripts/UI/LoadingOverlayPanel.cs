using UnityEngine;
using UnityEngine.UI;

namespace EggRogue
{
    /// <summary>
    /// 加载过渡界面 - 黑屏或简单 Loading 提示，用于商店点继续后进入下一关前的等待。
    /// 挂在 UIManager 同层级或 Canvas 下，或由 UIManager 在首次使用时自动创建。
    /// </summary>
    public class LoadingOverlayPanel : MonoBehaviour
    {
        [Header("可选：预先配置的 UI")]
        [Tooltip("留空则首次 Show 时自动创建全屏黑底 + 文本")]
        public RectTransform overlayRoot;

        [Tooltip("显示文本（如：准备中...、Loading...）")]
        public string loadingText = "准备中...";

        [Tooltip("等待时长（秒），由调用方控制，此处仅作提示")]
        public float defaultDuration = 1f;

        private Canvas _canvas;
        private GameObject _runtimeOverlay;

        /// <summary>
        /// 显示加载界面。若 overlayRoot 未配置则自动创建。
        /// </summary>
        public void Show()
        {
            if (overlayRoot != null)
            {
                overlayRoot.gameObject.SetActive(true);
                return;
            }

            if (_runtimeOverlay != null)
            {
                _runtimeOverlay.SetActive(true);
                return;
            }

            EnsureCanvas();
            if (_canvas == null) return;

            _runtimeOverlay = CreateRuntimeOverlay();
            if (_runtimeOverlay != null)
                _runtimeOverlay.transform.SetParent(_canvas.transform, false);
        }

        /// <summary>
        /// 隐藏加载界面。
        /// </summary>
        public void Hide()
        {
            if (overlayRoot != null)
            {
                overlayRoot.gameObject.SetActive(false);
                return;
            }

            if (_runtimeOverlay != null)
                _runtimeOverlay.SetActive(false);
        }

        private void EnsureCanvas()
        {
            if (_canvas != null) return;
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas == null)
                _canvas = Object.FindObjectOfType<Canvas>();
        }

        private GameObject CreateRuntimeOverlay()
        {
            var go = new GameObject("LoadingOverlay");
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 1f);
            img.raycastTarget = true;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(rect, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(400, 80);
            textRect.anchoredPosition = Vector2.zero;

            var text = textGo.AddComponent<Text>();
            text.text = string.IsNullOrEmpty(loadingText) ? "准备中..." : loadingText;
            text.fontSize = 36;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            var font = GameFont.GetDefault();
            if (font != null) text.font = font;

            return go;
        }
    }
}
