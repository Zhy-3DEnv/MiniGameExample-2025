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

    [Header("确认进入下一关")]
    [Tooltip("确认进入下一关的按钮（PC 按 Enter，手机点击此按钮）")]
    public Button nextLevelButton;

    private CardData[] currentCards = new CardData[4];
    private bool hasSelected = false;

    // 保存暂停前的状态
    private EnemySpawner enemySpawner;
    private CharacterController characterController;
    private PlayerCombatController playerCombatController;
    private bool wasSpawning = false;
    private bool wasCharacterEnabled = false;
    private bool wasCombatEnabled = false;

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
            nextLevelButton.onClick.AddListener(OnConfirmNextLevel);
            // 初始不可见 / 不可用，等待玩家选完卡再显示
            nextLevelButton.gameObject.SetActive(false);
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

        // 随机选择 4 张卡片
        currentCards = cardDatabase.GetRandomCards(4);
        hasSelected = false;

        // 更新按钮显示
        for (int i = 0; i < cardButtons.Length && i < currentCards.Length; i++)
        {
            UpdateCardButton(i, currentCards[i]);
        }

        Show();
    }

    private void UpdateCardButton(int index, CardData card)
    {
        if (cardButtons[index] == null || card == null)
            return;

        Transform btnTransform = cardButtons[index].transform;

        // 查找子元素（Icon、Name、Description）
        Image icon = btnTransform.Find("Icon")?.GetComponent<Image>();
        Text nameText = btnTransform.Find("Name")?.GetComponent<Text>();
        Text descText = btnTransform.Find("Description")?.GetComponent<Text>();

        if (icon != null)
            icon.sprite = card.icon;
        if (nameText != null)
            nameText.text = card.cardName;
        if (descText != null)
        {
            // 如果卡片没有填写描述，则根据属性加成自动生成一段说明
            if (!string.IsNullOrEmpty(card.description))
            {
                descText.text = card.description;
            }
            else
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                if (card.damageBonus != 0f) sb.AppendLine($"伤害 +{card.damageBonus}");
                if (card.fireRateBonus != 0f) sb.AppendLine($"攻击速度 +{card.fireRateBonus}");
                if (card.maxHealthBonus != 0f) sb.AppendLine($"最大生命值 +{card.maxHealthBonus}");
                if (card.moveSpeedBonus != 0f) sb.AppendLine($"移动速度 +{card.moveSpeedBonus}");
                if (card.bulletSpeedBonus != 0f) sb.AppendLine($"子弹速度 +{card.bulletSpeedBonus}");
                if (card.attackRangeBonus != 0f) sb.AppendLine($"攻击范围 +{card.attackRangeBonus}");

                descText.text = sb.Length > 0 ? sb.ToString() : "无属性加成";
            }
        }
    }

    private void OnCardSelected(int index)
    {
        if (hasSelected || index < 0 || index >= currentCards.Length)
            return;

        CardData selectedCard = currentCards[index];
        if (selectedCard == null)
            return;

        hasSelected = true;

        // 应用卡片加成
        if (CardManager.Instance != null)
        {
            CardManager.Instance.ApplyCard(selectedCard);
        }

        Debug.Log($"CardSelectionPanel: 玩家选择了卡片 - {selectedCard.cardName}");
        
        // 隐藏所有卡片按钮（玩家只看到结果与“进入下一关”按钮）
        for (int i = 0; i < cardButtons.Length; i++)
        {
            if (cardButtons[i] != null)
            {
                cardButtons[i].gameObject.SetActive(false);
            }
        }

        // 显示“进入下一关”按钮，让玩家自行确认
        if (nextLevelButton != null)
        {
            nextLevelButton.gameObject.SetActive(true);
            nextLevelButton.interactable = true;
        }
    }

    /// <summary>
    /// 玩家确认进入下一关（按钮点击或键盘 Enter 调用）
    /// </summary>
    private void OnConfirmNextLevel()
    {
        if (!hasSelected)
            return;

        // 隐藏界面，进入下一关
        Hide();
        if (EggRogue.LevelManager.Instance != null)
        {
            EggRogue.LevelManager.Instance.NextLevel();
        }
    }

    private void Update()
    {
        // 允许玩家在选完卡后按 Enter 进入下一关
        if (IsVisible() && hasSelected)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
            {
                OnConfirmNextLevel();
            }
        }
    }

    protected override void OnShow()
    {
        base.OnShow();
        hasSelected = false;

        // 每次显示选卡界面前，确保卡片按钮重新激活、可交互
        for (int i = 0; i < cardButtons.Length; i++)
        {
            if (cardButtons[i] != null)
            {
                cardButtons[i].gameObject.SetActive(true);
                cardButtons[i].interactable = true;
            }
        }

        // 隐藏“进入下一关”按钮，等待玩家重新选择
        if (nextLevelButton != null)
        {
            nextLevelButton.gameObject.SetActive(false);
            nextLevelButton.interactable = false;
        }

        // 暂停所有游戏玩法系统（通过统一的 GameplayPauseManager）
        if (EggRogue.GameplayPauseManager.Instance != null)
        {
            EggRogue.GameplayPauseManager.Instance.RequestPause("CardSelectionPanel");
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
