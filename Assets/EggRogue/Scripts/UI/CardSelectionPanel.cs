using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using EggRogue;

/// <summary>
/// 卡片选择界面 - 显示 4 张卡片供玩家选择。
/// 使用方式：
/// 1. 在 Canvas 下创建 Panel，命名为 CardSelectionPanel
/// 2. 添加本脚本
/// 3. 在 Inspector 中设置 cardButtons 数组（4 个按钮）
/// 4. 每个按钮下需要有：Icon（Image）、Name（Text）、Description（Text）
/// </summary>
public class CardSelectionPanel : BaseUIPanel
{
    [Header("卡片按钮")]
    [Tooltip("卡片按钮列表（4 个按钮）")]
    public Button[] cardButtons = new Button[4];

    [Header("卡片数据库")]
    [Tooltip("卡片数据库（ScriptableObject）")]
    public CardDatabase cardDatabase;

    [Header("选卡后进入商店")]
    [Tooltip("选卡后显示的「继续」按钮，点击进入商店")]
    public Button nextLevelButton;

    [Header("选择次数显示")]
    [Tooltip("显示可选次数，如 2/4（已选/总额）")]
    public Text selectionCountText;

    [Header("随机刷新")]
    [Tooltip("随机按钮（花费金币重新随机 4 张卡片，每次价格递增，下关重置）")]
    public Button rerollButton;

    [Tooltip("首次随机价格")]
    public int rerollBasePrice = 5;

    [Tooltip("每次随机价格递增")]
    public int rerollPriceIncrement = 5;

    private CardOffer[] currentCards = new CardOffer[4];
    private int selectedCount = 0;
    private int totalPicks = 1;
    private readonly bool[] selectedIndices = new bool[4];
    private int rerollCount = 0;

    private void Start()
    {
        // 绑定按钮事件
        for (int i = 0; i < cardButtons.Length; i++)
        {
            int index = i;
            if (cardButtons[i] != null)
            {
                cardButtons[i].onClick.RemoveAllListeners();
                cardButtons[i].onClick.AddListener(() => OnCardSelected(index));
            }
        }

        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.RemoveAllListeners();
            nextLevelButton.onClick.AddListener(OnContinueClicked);
            nextLevelButton.gameObject.SetActive(false);
        }

        if (rerollButton != null)
        {
            rerollButton.onClick.RemoveAllListeners();
            rerollButton.onClick.AddListener(OnRerollClicked);
        }
    }

    /// <summary>
    /// 显示卡片选择界面（外部调用，例如 ResultPanel 倒计时结束）。
    /// </summary>
    public void ShowCardSelection()
    {
        if (cardDatabase == null)
        {
            Debug.LogError("CardSelectionPanel: cardDatabase 未设置！");
            return;
        }

        // 按当前关卡的卡牌等级权重随机选择 4 张卡片（返回 CardOffer[]）
        var levelData = GetCurrentLevelData();
        currentCards = cardDatabase.GetRandomCards(4, levelData);
        selectedCount = 0;
        rerollCount = 0;
        for (int i = 0; i < selectedIndices.Length; i++)
            selectedIndices[i] = false;

        totalPicks = GetCardPickCount();

        RefreshCardDisplay();
        RefreshSelectedVisibility();
        RefreshRerollButton();
        RefreshContinueButton();
        RefreshSelectionCountText();
        Show();
    }

    private int GetCardPickCount()
    {
        if (PlayerLevelManager.Instance != null)
            return PlayerLevelManager.Instance.GetCardPickCount();
        return 1;
    }

    private LevelData GetCurrentLevelData()
    {
        var lm = LevelManager.Instance;
        if (lm == null || lm.levelDatabase == null) return null;
        return lm.levelDatabase.GetLevel(lm.CurrentLevel);
    }

    private void RefreshCardDisplay()
    {
        for (int i = 0; i < cardButtons.Length && i < currentCards.Length; i++)
        {
            UpdateCardButton(i, ref currentCards[i]);
        }
    }

    /// <summary>
    /// 刷新已选卡片的可见性。已选中的用 CanvasGroup 隐藏显示但保留占位，避免 HorizontalLayoutGroup 重新排布。
    /// </summary>
    private void RefreshSelectedVisibility()
    {
        for (int i = 0; i < cardButtons.Length && i < selectedIndices.Length; i++)
        {
            if (cardButtons[i] == null) continue;

            bool selected = selectedIndices[i];
            var go = cardButtons[i].gameObject;

            // 保持 GameObject 激活，用 CanvasGroup 控制可见性，以保留布局占位
            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = go.AddComponent<CanvasGroup>();

            if (selected)
            {
                cg.alpha = 0f;
                cg.blocksRaycasts = true;  // 阻挡点击
                cg.interactable = false;
                cardButtons[i].interactable = false;
            }
            else
            {
                cg.alpha = 1f;
                cg.blocksRaycasts = true;
                cg.interactable = true;
                cardButtons[i].interactable = true;
            }
        }
    }

    private void RefreshContinueButton()
    {
        if (nextLevelButton == null) return;
        bool canContinue = selectedCount >= totalPicks;
        nextLevelButton.gameObject.SetActive(canContinue);
        if (canContinue)
        {
            nextLevelButton.interactable = true;
            var btnText = nextLevelButton.GetComponentInChildren<Text>();
            if (btnText != null)
                btnText.text = selectedCount > 0 ? "继续" : "继续";
        }
    }

    private void RefreshSelectionCountText()
    {
        if (selectionCountText != null)
        {
            int remaining = Mathf.Max(0, totalPicks - selectedCount);
            selectionCountText.text = $"{remaining}/{totalPicks}";
        }
    }

    private void RefreshRerollButton()
    {
        if (rerollButton == null) return;
        int price = GetCurrentRerollPrice();
        var text = rerollButton.GetComponentInChildren<Text>();
        if (text != null)
            text.text = $"随机 ({price}金币)";
        rerollButton.interactable = selectedCount == 0 && GoldManager.Instance != null && GoldManager.Instance.Gold >= price;
    }

    private int GetCurrentRerollPrice()
    {
        return rerollBasePrice + rerollCount * rerollPriceIncrement;
    }

    private void UpdateCardButton(int index, ref CardOffer offer)
    {
        if (cardButtons[index] == null || offer.card == null)
            return;

        CardData card = offer.card;
        int level = offer.level;
        Transform btnTransform = cardButtons[index].transform;

        Image icon = btnTransform.Find("Icon")?.GetComponent<Image>();
        Text nameText = btnTransform.Find("Name")?.GetComponent<Text>();
        Text descText = btnTransform.Find("Description")?.GetComponent<Text>();

        if (icon != null)
            icon.sprite = card.icon;
        if (nameText != null)
            nameText.text = $"{card.cardName} Lv{level}";
        if (descText != null)
            descText.text = card.GetDescriptionForLevel(level);
    }

    private void OnCardSelected(int index)
    {
        if (index < 0 || index >= currentCards.Length || selectedIndices[index])
            return;
        if (selectedCount >= totalPicks)
            return;

        CardOffer selectedOffer = currentCards[index];
        if (selectedOffer.card == null)
            return;

        selectedIndices[index] = true;
        selectedCount++;

        if (CardManager.Instance != null)
        {
            CardManager.Instance.ApplyCard(selectedOffer.card, selectedOffer.level);
        }
        else
        {
            Debug.LogWarning("CardSelectionPanel: 未找到 CardManager.Instance，卡片加成未能应用到角色。");
        }

        Debug.Log($"CardSelectionPanel: 玩家选择了卡片 - {selectedOffer.card.cardName} Lv{selectedOffer.level} ({selectedCount}/{totalPicks})");

        // 隐藏已选中的卡片
        RefreshSelectedVisibility();
        RefreshRerollButton();
        RefreshContinueButton();
        RefreshSelectionCountText();

        // 当卡池空了但还有选择次数时，自动补充新卡片
        if (selectedCount < totalPicks && AreAllSlotsSelected())
            RefillCards();
    }

    private bool AreAllSlotsSelected()
    {
        for (int i = 0; i < selectedIndices.Length && i < cardButtons.Length; i++)
        {
            if (!selectedIndices[i]) return false;
        }
        return true;
    }

    private void RefillCards()
    {
        if (cardDatabase == null) return;
        var levelData = GetCurrentLevelData();
        currentCards = cardDatabase.GetRandomCards(4, levelData);
        for (int i = 0; i < selectedIndices.Length; i++)
            selectedIndices[i] = false;
        RefreshCardDisplay();
        RefreshSelectedVisibility();
        RefreshRerollButton();
        RefreshContinueButton();
        RefreshSelectionCountText();
    }

    private void OnRerollClicked()
    {
        if (selectedCount > 0) return;
        int price = GetCurrentRerollPrice();
        if (GoldManager.Instance == null || !GoldManager.Instance.SpendGold(price))
            return;

        rerollCount++;
        var levelData = GetCurrentLevelData();
        currentCards = cardDatabase.GetRandomCards(4, levelData);
        RefreshCardDisplay();
        RefreshRerollButton();
    }

    /// <summary>
    /// 玩家点击「继续」后进入商店，再在商店中点击「继续」进入下一关。
    /// </summary>
    private void OnContinueClicked()
    {
        if (selectedCount < totalPicks)
            return;

        Hide();
        if (UIManager.Instance != null)
            UIManager.Instance.ShowShop();
        else if (LevelManager.Instance != null)
            LevelManager.Instance.NextLevel();
    }

    private void Update()
    {
        RefreshRerollButton();
        // 选够卡后按 Enter 进入商店
        if (IsVisible() && selectedCount >= totalPicks)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
            {
                OnContinueClicked();
            }
        }
    }

    protected override void OnShow()
    {
        base.OnShow();
        
        // 行业标准做法：进入选卡界面时清理所有伤害飘字，避免它们显示在UI上并阻挡交互
        ClearAllDamagePopups();
        
        selectedCount = 0;
        for (int i = 0; i < selectedIndices.Length; i++)
            selectedIndices[i] = false;

        // 确保卡片按钮可见、可交互（RefreshSelectedVisibility 会在 Refresh 时根据 selectedIndices 更新）
        for (int i = 0; i < cardButtons.Length; i++)
        {
            if (cardButtons[i] != null)
            {
                cardButtons[i].gameObject.SetActive(true);
                cardButtons[i].interactable = true;
            }
        }

        if (nextLevelButton != null)
        {
            nextLevelButton.gameObject.SetActive(false);
            nextLevelButton.interactable = false;
            var btnText = nextLevelButton.GetComponentInChildren<Text>();
            if (btnText != null)
                btnText.text = "继续";
        }

        RefreshSelectionCountText();

        // 暂停所有游戏玩法系统（通过统一的 GameplayPauseManager）
        if (EggRogue.GameplayPauseManager.Instance != null)
        {
            EggRogue.GameplayPauseManager.Instance.RequestPause("CardSelectionPanel");
        }
    }
    
    /// <summary>
    /// 清理场景中所有活跃的伤害飘字（ScorePopup），避免它们显示在UI上并阻挡交互。
    /// </summary>
    private void ClearAllDamagePopups()
    {
        FlappyBird.ScorePopup[] popups = Object.FindObjectsOfType<FlappyBird.ScorePopup>();
        foreach (var popup in popups)
        {
            if (popup != null && popup.gameObject != null)
            {
                Object.Destroy(popup.gameObject);
            }
        }
    }

    protected override void OnHide()
    {
        base.OnHide();
        
        // 结束选卡界面的暂停请求
        if (EggRogue.GameplayPauseManager.Instance != null)
        {
            EggRogue.GameplayPauseManager.Instance.RequestResume("CardSelectionPanel");
        }
    }
}
