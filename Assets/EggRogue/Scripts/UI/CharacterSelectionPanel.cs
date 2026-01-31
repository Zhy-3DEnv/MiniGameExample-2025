using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using EggRogue;

/// <summary>
/// 角色选择界面 - 显示所有可选角色，玩家选择后进入游戏。
/// 
/// 使用方式：
/// 1. 在 Canvas 下创建 Panel，命名为 CharacterSelectionPanel
/// 2. 添加本脚本
/// 3. 在 Inspector 中绑定 UI 元素（角色按钮、描述文本等）
/// 4. 配置 CharacterDatabase 引用
/// </summary>
public class CharacterSelectionPanel : BaseUIPanel
{
    [Header("UI 引用")]
    [Tooltip("角色按钮容器（用于动态生成角色按钮，需放在 ScrollRect 的 Content 下；脚本会强制 4 列 + 垂直自适应，约 3 行可见需在 Viewport 高度和 Cell Size 里调）")]
    public Transform characterButtonContainer;

    [Tooltip("角色列表的 ScrollRect（可选）；不绑定时会从 characterButtonContainer 的父级查找。用于打开面板时滚动到顶部")]
    public ScrollRect characterListScrollRect;

    [Tooltip("角色按钮预制体（需含 Button；子节点可有 Text 与名为 Icon 的 Image 用于名称和图标）")]
    public GameObject characterButtonPrefab;

    [Tooltip("角色名称文本")]
    public Text characterNameText;

    [Tooltip("角色描述文本")]
    public Text characterDescriptionText;

    [Tooltip("角色图标（右侧详情，可选；不设置则仅按钮上显示图标）")]
    public Image characterIcon;

    [Tooltip("特殊能力文本")]
    public Text passiveAbilitiesText;

    [Tooltip("基础属性文本")]
    public Text baseStatsText;

    [Tooltip("确认按钮")]
    public Button confirmButton;

    [Header("配置")]
    [Tooltip("角色数据库")]
    public CharacterDatabase characterDatabase;

    [Tooltip("滑动停止后吸附到整行的速度阈值（velocity 平方小于此值才吸附）")]
    public float scrollSnapVelocityThreshold = 50f;

    [Tooltip("吸附到整行的动画时长（秒），0 表示瞬间吸附")]
    public float scrollSnapDuration = 0.2f;

    private CharacterData _selectedCharacter;
    private int _selectedIndex = 0;
    private float _scrollSnapTimer;
    private const float ScrollSnapWaitTime = 0.05f;
    private bool _userHasDraggedScroll;

    protected override void OnShow()
    {
        base.OnShow();
        _userHasDraggedScroll = false;
        RefreshCharacterList();
        EnsureViewportMask();
        SelectCharacterByIndex(0);
        RefreshSelectionHighlight();
        EnsureScrollToTop();

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        }
    }

    /// <summary>
    /// 确保 ScrollRect 的 Viewport 有裁剪组件（Mask 或 RectMask2D），否则运行时会看到整块内容而不被裁剪。
    /// </summary>
    private void EnsureViewportMask()
    {
        ScrollRect scroll = characterListScrollRect != null
            ? characterListScrollRect
            : (characterButtonContainer != null ? characterButtonContainer.GetComponentInParent<ScrollRect>() : null);
        if (scroll == null || scroll.viewport == null) return;

        RectTransform viewport = scroll.viewport;
        if (viewport.GetComponent<RectMask2D>() == null && viewport.GetComponent<Mask>() == null)
            viewport.gameObject.AddComponent<RectMask2D>();

        // 允许顶部/底部弹性 overscroll，拉到最上面也能再往下拉显示空白
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.elasticity = 0.2f;
    }

    /// <summary>
    /// 确保角色列表滚动到顶部（便于浏览）
    /// </summary>
    private void EnsureScrollToTop()
    {
        ScrollRect scroll = characterListScrollRect != null
            ? characterListScrollRect
            : (characterButtonContainer != null ? characterButtonContainer.GetComponentInParent<ScrollRect>() : null);
        if (scroll != null)
        {
            scroll.verticalNormalizedPosition = 1f;
        }
    }

    private void Update()
    {
        if (!IsVisible())
            return;

        // 使用 Enter 键确认选择
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
        {
            OnConfirmButtonClicked();
        }

        UpdateScrollSnap();
    }

    /// <summary>
    /// 滑动停止后吸附到整行
    /// </summary>
    private void UpdateScrollSnap()
    {
        ScrollRect scroll = characterListScrollRect != null
            ? characterListScrollRect
            : (characterButtonContainer != null ? characterButtonContainer.GetComponentInParent<ScrollRect>() : null);
        if (scroll == null || scroll.content == null || characterButtonContainer == null) return;

        bool dragging = (Mouse.current != null && Mouse.current.leftButton.isPressed)
            || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed);
        float velSq = scroll.velocity.sqrMagnitude;
        bool velocityLow = velSq < scrollSnapVelocityThreshold;

        if (dragging)
        {
            _userHasDraggedScroll = true;
            _scrollSnapTimer = 0f;
            return;
        }

        if (!velocityLow)
        {
            _scrollSnapTimer = 0f;
            return;
        }

        // 仅在实际拖拽过列表后才吸附，避免点击角色按钮后列表回弹
        if (!_userHasDraggedScroll)
            return;

        _scrollSnapTimer += Time.deltaTime;
        if (_scrollSnapTimer < ScrollSnapWaitTime)
            return;

        GridLayoutGroup grid = characterButtonContainer.GetComponent<GridLayoutGroup>();
        if (grid == null) return;

        float rowHeight = grid.cellSize.y + grid.spacing.y;
        if (rowHeight <= 0f) return;

        RectTransform content = scroll.content;
        RectTransform viewport = scroll.viewport != null ? scroll.viewport : scroll.transform as RectTransform;
        float contentHeight = content.rect.height;
        float viewportHeight = viewport != null ? viewport.rect.height : contentHeight;
        float minY = viewportHeight - contentHeight;

        float y = content.anchoredPosition.y;
        float snappedY = Mathf.Round(y / rowHeight) * rowHeight;
        snappedY = Mathf.Clamp(snappedY, minY, 0f);

        if (Mathf.Abs(snappedY - y) < 0.5f)
        {
            scroll.velocity = Vector2.zero;
            _userHasDraggedScroll = false;
            return;
        }

        if (scrollSnapDuration <= 0f)
        {
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, snappedY);
            scroll.velocity = Vector2.zero;
        }
        else
        {
            float t = Mathf.Clamp01(Time.deltaTime / scrollSnapDuration);
            float newY = Mathf.Lerp(y, snappedY, t);
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, newY);
            if (Mathf.Abs(scroll.velocity.y) > 1f)
                scroll.velocity = Vector2.Lerp(scroll.velocity, Vector2.zero, t);
        }
        _userHasDraggedScroll = false;
    }

    /// <summary>
    /// 刷新角色列表（动态生成按钮）
    /// </summary>
    private void RefreshCharacterList()
    {
        if (characterDatabase == null || characterDatabase.characters == null)
        {
            Debug.LogWarning("CharacterSelectionPanel: characterDatabase 未设置或角色列表为空");
            return;
        }

        if (characterButtonContainer == null || characterButtonPrefab == null)
        {
            Debug.LogWarning("CharacterSelectionPanel: characterButtonContainer 或 characterButtonPrefab 未设置");
            return;
        }

        EnsureGridLayoutAndScrollContent(characterButtonContainer);

        // 清空现有按钮
        foreach (Transform child in characterButtonContainer)
        {
            Destroy(child.gameObject);
        }

        // 为每个角色创建按钮
        for (int i = 0; i < characterDatabase.characters.Length; i++)
        {
            CharacterData character = characterDatabase.characters[i];
            if (character == null)
                continue;

            GameObject buttonObj = Instantiate(characterButtonPrefab, characterButtonContainer);
            Button button = buttonObj.GetComponent<Button>();
            Text buttonText = buttonObj.GetComponentInChildren<Text>();

            if (buttonText != null)
            {
                buttonText.text = character.characterName;
            }

            // 在按钮上显示角色图标（优先使用名为 Icon 的子节点 Image，否则用第一个非 Button.targetGraphic 的 Image）
            Image buttonIcon = GetButtonIconImage(buttonObj, button);
            if (buttonIcon != null)
            {
                buttonIcon.sprite = character.icon;
                buttonIcon.enabled = character.icon != null;
            }

            if (button != null)
            {
                int index = i;
                button.onClick.AddListener(() => SelectCharacterByIndex(index));
            }
        }
    }

    /// <summary>
    /// 确保角色按钮容器为 4 列网格（每行 4 个），内容高度自适应以便垂直滚动；约 3 行可见由 Viewport 高度与 Cell Size 决定。
    /// </summary>
    private static void EnsureGridLayoutAndScrollContent(Transform container)
    {
        if (container == null) return;

        GridLayoutGroup grid = container.GetComponent<GridLayoutGroup>();
        if (grid == null)
            grid = container.gameObject.AddComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;
        grid.childAlignment = TextAnchor.UpperCenter;

        ContentSizeFitter fitter = container.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = container.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
    }

    /// <summary>
    /// 从按钮或其子节点获取用于显示角色图标的 Image（优先名为 Icon 的子节点，否则取第一个非按钮背景的 Image）
    /// </summary>
    private static Image GetButtonIconImage(GameObject buttonObj, Button button)
    {
        if (buttonObj == null)
            return null;

        Transform iconTransform = buttonObj.transform.Find("Icon");
        if (iconTransform != null)
        {
            Image img = iconTransform.GetComponent<Image>();
            if (img != null)
                return img;
        }

        Image[] images = buttonObj.GetComponentsInChildren<Image>(true);
        Image targetGraphic = button != null ? button.targetGraphic as Image : null;
        foreach (Image img in images)
        {
            if (img != targetGraphic)
                return img;
        }
        return null;
    }

    /// <summary>
    /// 选择角色（通过索引）
    /// </summary>
    private void SelectCharacterByIndex(int index)
    {
        if (characterDatabase == null || characterDatabase.characters == null)
            return;

        if (index < 0 || index >= characterDatabase.characters.Length)
            return;

        _selectedIndex = index;
        _selectedCharacter = characterDatabase.characters[index];

        RefreshSelectionHighlight();
        RefreshCharacterInfo();
    }

    /// <summary>
    /// 刷新选中高亮（互斥：仅当前选中项高亮）
    /// </summary>
    private void RefreshSelectionHighlight()
    {
        if (characterButtonContainer == null) return;

        for (int i = 0; i < characterButtonContainer.childCount; i++)
        {
            SetButtonSelectedState(characterButtonContainer.GetChild(i).gameObject, i == _selectedIndex);
        }
    }

    /// <summary>
    /// 设置单个按钮的选中/未选中视觉状态（优先使用名为 Highlight 的子节点 Image）
    /// </summary>
    private static void SetButtonSelectedState(GameObject buttonObj, bool selected)
    {
        if (buttonObj == null) return;

        Transform highlightTransform = buttonObj.transform.Find("Highlight");
        if (highlightTransform != null)
        {
            Image img = highlightTransform.GetComponent<Image>();
            if (img != null)
            {
                img.enabled = selected;
                return;
            }
        }

        Button btn = buttonObj.GetComponent<Button>();
        if (btn != null && btn.targetGraphic is Image targetImage)
        {
            targetImage.color = selected ? Color.white : new Color(0.85f, 0.85f, 0.85f, 1f);
        }
    }

    /// <summary>
    /// 刷新右侧角色信息显示
    /// </summary>
    private void RefreshCharacterInfo()
    {
        if (_selectedCharacter == null)
            return;

        // 显示角色名称
        if (characterNameText != null)
        {
            characterNameText.text = _selectedCharacter.characterName;
        }

        // 显示角色描述
        if (characterDescriptionText != null)
        {
            characterDescriptionText.text = _selectedCharacter.description;
        }

        // 显示角色图标
        if (characterIcon != null)
        {
            characterIcon.sprite = _selectedCharacter.icon;
            characterIcon.enabled = _selectedCharacter.icon != null;
        }

        // 显示特殊能力
        if (passiveAbilitiesText != null)
        {
            if (_selectedCharacter.passiveAbilities != null && _selectedCharacter.passiveAbilities.Length > 0)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine("特殊能力：");
                foreach (var passive in _selectedCharacter.passiveAbilities)
                {
                    if (passive != null)
                    {
                        sb.AppendLine($"• {passive.abilityName}");
                        if (!string.IsNullOrEmpty(passive.description))
                        {
                            sb.AppendLine($"  {passive.description}");
                        }
                    }
                }
                passiveAbilitiesText.text = sb.ToString();
            }
            else
            {
                passiveAbilitiesText.text = "特殊能力：无";
            }
        }

        // 显示基础属性
        if (baseStatsText != null)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("基础属性：");
            sb.AppendLine($"伤害: {_selectedCharacter.baseDamage}");
            sb.AppendLine($"射速: {_selectedCharacter.baseFireRate} 发/秒");
            sb.AppendLine($"生命: {_selectedCharacter.baseMaxHealth}");
            sb.AppendLine($"移速: {_selectedCharacter.baseMoveSpeed}");
            sb.AppendLine($"子弹速度: {_selectedCharacter.baseBulletSpeed}");
            sb.AppendLine($"攻击范围: {_selectedCharacter.baseAttackRange}");
            baseStatsText.text = sb.ToString();
        }
    }

    /// <summary>
    /// 确认按钮点击（进入游戏）
    /// </summary>
    private void OnConfirmButtonClicked()
    {
        if (_selectedCharacter == null)
        {
            Debug.LogWarning("CharacterSelectionPanel: 未选择角色");
            return;
        }

        // 保存选择到 CharacterSelectionManager
        if (CharacterSelectionManager.Instance != null)
        {
            CharacterSelectionManager.Instance.SelectCharacter(_selectedCharacter);
        }

        // 隐藏角色选择界面，进入游戏
        Hide();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadGameScene(1);
        }
    }

    protected override void OnHide()
    {
        base.OnHide();

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
        }
    }
}
