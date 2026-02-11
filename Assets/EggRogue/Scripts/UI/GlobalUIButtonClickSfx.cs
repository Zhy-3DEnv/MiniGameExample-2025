using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace EggRogue
{
    /// <summary>
    /// 全局 UI 按钮点击音效绑定器。
    /// 挂在 Canvas 或 UIRoot 下，会为扫描范围内的所有 Button 绑定点击音效。
    /// 注意：各 Panel 在设置按钮时常用 RemoveAllListeners()，会顺带移除本组件绑定的音效，
    /// 因此需在每次显示面板后调用 RefreshBindings()（UIManager 已统一在显示面板后延迟一帧刷新）。
    /// </summary>
    public class GlobalUIButtonClickSfx : MonoBehaviour
    {
        public static GlobalUIButtonClickSfx Instance { get; private set; }

        [Tooltip("为 true 时在绑定本根节点后，再扫描所有已加载场景的根节点；为 false 时只绑定本根节点。若主菜单/选角在另一场景可勾选。")]
        public bool bindButtonsInAllLoadedScenes = false;

        private readonly System.Collections.Generic.HashSet<Button> _boundButtons =
            new System.Collections.Generic.HashSet<Button>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
                return;
            Instance = this;
        }

        private void OnEnable()
        {
            BindAllButtons();
        }

        private void OnDisable()
        {
            UnbindAllButtons();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            UnbindAllButtons();
        }

        private void OnTransformChildrenChanged()
        {
            BindAllButtons();
        }

        /// <summary>
        /// 重新绑定所有按钮的点击音效。在 Panel 执行 RemoveAllListeners 后调用可恢复音效（建议延迟一帧以便 Panel 先完成 Setup）。
        /// </summary>
        public void RefreshBindings()
        {
            UnbindAllButtons();
            BindAllButtons();
        }

        private void BindAllButtons()
        {
            // 始终先绑定本组件所在根节点下的所有按钮（保证挂在 Canvas 下的 UI 一定被绑定，即使用户在 DontDestroyOnLoad 或场景根未包含时）
            Transform root = transform.root != null ? transform.root : transform;
            BindButtonsUnder(root);

            if (bindButtonsInAllLoadedScenes)
                BindAllButtonsInAllScenes();
        }

        private void BindButtonsUnder(Transform root)
        {
            var buttons = root.GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                if (btn == null || _boundButtons.Contains(btn)) continue;
                btn.onClick.AddListener(OnAnyButtonClicked);
                _boundButtons.Add(btn);
            }
        }

        private void BindAllButtonsInAllScenes()
        {
            for (int s = 0; s < SceneManager.sceneCount; s++)
            {
                var scene = SceneManager.GetSceneAt(s);
                if (!scene.isLoaded) continue;
                var roots = scene.GetRootGameObjects();
                for (int r = 0; r < roots.Length; r++)
                {
                    var root = roots[r];
                    if (root == null) continue;
                    BindButtonsUnder(root.transform);
                }
            }
        }

        private void UnbindAllButtons()
        {
            foreach (var btn in _boundButtons)
            {
                if (btn != null)
                    btn.onClick.RemoveListener(OnAnyButtonClicked);
            }
            _boundButtons.Clear();
        }

        private void OnAnyButtonClicked()
        {
            if (EggRogueAudioManager.Instance != null)
                EggRogueAudioManager.Instance.PlayButtonClick();
        }
    }
}

