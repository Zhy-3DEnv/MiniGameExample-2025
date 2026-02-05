using UnityEngine;
using UnityEditor;
using EggRogue;

/// <summary>
/// 商店搭建工具 - 创建 ShopPanel、ShopManager。
/// </summary>
public static class ShopSetup
{
    [MenuItem("EggRogue/一键搭建商店（面板 + 管理器）", false, 117)]
    public static void SetupShopAll()
    {
        AddShopManagerToScene();
        AddPlayerLevelManagerToScene();
        AddItemInventoryManagerToScene();
        AddShopPanel();
        Debug.Log("[ShopSetup] 商店搭建完成。请确保 PersistentScene 为当前打开场景。");
    }

    [MenuItem("EggRogue/添加 ItemInventoryManager 到场景", false, 120)]
    public static void AddItemInventoryManagerToScene()
    {
        var managers = GameObject.Find("Managers");
        var root = managers != null ? managers.transform : null;
        if (root == null)
        {
            var pr = GameObject.Find("PersistentRoot");
            root = pr != null ? pr.transform.Find("Managers") : null;
        }
        if (root == null) return;
        if (root.Find("ItemInventoryManager") != null)
        {
            Debug.Log("[ShopSetup] ItemInventoryManager 已存在。");
            return;
        }
        var go = new GameObject("ItemInventoryManager");
        go.transform.SetParent(root);
        go.AddComponent<ItemInventoryManager>();
        Undo.RegisterCreatedObjectUndo(go, "Add ItemInventoryManager");
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log("[ShopSetup] 已添加 ItemInventoryManager。");
    }

    [MenuItem("EggRogue/添加 PlayerLevelManager 到场景", false, 116)]
    public static void AddPlayerLevelManagerToScene()
    {
        var managers = GameObject.Find("Managers");
        var root = managers != null ? managers.transform : null;
        if (root == null)
        {
            var pr = GameObject.Find("PersistentRoot");
            if (pr != null)
            {
                var m = pr.transform.Find("Managers");
                root = m != null ? m : pr.transform;
            }
        }
        if (root == null)
        {
            var go = new GameObject("Managers");
            root = go.transform;
            var pr = GameObject.Find("PersistentRoot");
            if (pr != null) go.transform.SetParent(pr.transform);
        }
        if (GameObject.Find("PlayerLevelManager") != null)
        {
            Debug.Log("[ShopSetup] PlayerLevelManager 已存在。");
            return;
        }
        var plm = new GameObject("PlayerLevelManager");
        plm.transform.SetParent(root);
        plm.AddComponent<PlayerLevelManager>();
        Undo.RegisterCreatedObjectUndo(plm, "Add PlayerLevelManager");
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log("[ShopSetup] 已添加 PlayerLevelManager。");
    }

    [MenuItem("EggRogue/添加 ShopManager 到场景", false, 118)]
    public static void AddShopManagerToScene()
    {
        var managers = GameObject.Find("Managers");
        var root = managers != null ? managers.transform : null;
        if (root == null)
        {
            var pr = GameObject.Find("PersistentRoot");
            if (pr != null)
            {
                var m = pr.transform.Find("Managers");
                root = m != null ? m : pr.transform;
            }
        }
        if (root == null)
        {
            var go = new GameObject("Managers");
            root = go.transform;
            var pr = GameObject.Find("PersistentRoot");
            if (pr != null) go.transform.SetParent(pr.transform);
        }
        if (GameObject.Find("ShopManager") != null)
        {
            Debug.Log("[ShopSetup] ShopManager 已存在。");
            return;
        }
        var sm = new GameObject("ShopManager");
        sm.transform.SetParent(root);
        var shopMgr = sm.AddComponent<ShopManager>();
        var wdb = AssetDatabase.LoadAssetAtPath<WeaponDatabase>("Assets/EggRogue/Configs/WeaponDatabase.asset");
        if (wdb != null) shopMgr.weaponDatabase = wdb;
        var idb = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/EggRogue/Configs/ItemDatabase.asset");
        if (idb != null) shopMgr.itemDatabase = idb;
        Undo.RegisterCreatedObjectUndo(sm, "Add ShopManager");
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log("[ShopSetup] 已添加 ShopManager。");
    }

    [MenuItem("EggRogue/添加商店面板到 Canvas", false, 119)]
    public static void AddShopPanel()
    {
        var canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[ShopSetup] 场景中无 Canvas。");
            return;
        }

        var existingPanel = canvas.transform.Find("ShopPanel");
        if (existingPanel != null)
        {
            var sp = existingPanel.GetComponent<ShopPanel>();
            if (sp != null)
            {
                if (sp.itemSlots == null || sp.itemSlots.Length == 0 || sp.itemSlots[0].buyButton == null)
                {
                    AddItemSlotsToExistingShop(existingPanel.gameObject);
                    Debug.Log("[ShopSetup] 已为现有 ShopPanel 添加商品槽。");
                }
                if (sp.weaponSlotsDisplay == null)
                {
                    AddWeaponSlotsAndItemInventory(existingPanel.gameObject);
                    Debug.Log("[ShopSetup] 已添加武器槽和物品栏。");
                }
            }
            return;
        }

        var panel = new GameObject("ShopPanel");
        panel.transform.SetParent(canvas.transform, false);
        var rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = panel.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0, 0, 0, 0.85f);

        var titleGo = new GameObject("TitleText");
        titleGo.transform.SetParent(panel.transform, false);
        var titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.9f);
        titleRt.anchorMax = new Vector2(0.5f, 0.9f);
        titleRt.sizeDelta = new Vector2(200, 40);
        titleRt.anchoredPosition = Vector2.zero;
        var titleText = titleGo.AddComponent<UnityEngine.UI.Text>();
        titleText.text = "商店";
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.fontSize = 28;
        titleText.color = Color.white;
        EggRogue.GameFont.ApplyTo(titleText);

        var goldGo = new GameObject("GoldText");
        goldGo.transform.SetParent(panel.transform, false);
        var goldRt = goldGo.AddComponent<RectTransform>();
        goldRt.anchorMin = new Vector2(0.5f, 0.75f);
        goldRt.anchorMax = new Vector2(0.5f, 0.75f);
        goldRt.sizeDelta = new Vector2(200, 30);
        goldRt.anchoredPosition = Vector2.zero;
        var goldText = goldGo.AddComponent<UnityEngine.UI.Text>();
        goldText.text = "金币: 0";
        goldText.alignment = TextAnchor.MiddleCenter;
        goldText.fontSize = 20;
        goldText.color = Color.yellow;
        EggRogue.GameFont.ApplyTo(goldText);

        var continueBtn = new GameObject("ContinueButton");
        continueBtn.transform.SetParent(panel.transform, false);
        var btnRt = continueBtn.AddComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0.15f);
        btnRt.anchorMax = new Vector2(0.5f, 0.15f);
        btnRt.sizeDelta = new Vector2(160, 40);
        btnRt.anchoredPosition = Vector2.zero;
        continueBtn.AddComponent<UnityEngine.UI.Image>().color = new Color(0.2f, 0.6f, 0.2f);
        continueBtn.AddComponent<UnityEngine.UI.Button>();
        var textGo = new GameObject("Text");
        textGo.transform.SetParent(continueBtn.transform, false);
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        var btnText = textGo.AddComponent<UnityEngine.UI.Text>();
        btnText.text = "继续";
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.color = Color.white;
        EggRogue.GameFont.ApplyTo(btnText);

        var itemSlotsRoot = new GameObject("ItemSlots");
        itemSlotsRoot.transform.SetParent(panel.transform, false);
        var slotsRt = itemSlotsRoot.AddComponent<RectTransform>();
        slotsRt.anchorMin = new Vector2(0.5f, 0.55f);
        slotsRt.anchorMax = new Vector2(0.5f, 0.55f);
        slotsRt.sizeDelta = new Vector2(900, 120);
        slotsRt.anchoredPosition = Vector2.zero;
        var hlg = itemSlotsRoot.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        hlg.spacing = 20f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        var shopPanel = panel.AddComponent<ShopPanel>();
        shopPanel.titleText = titleText;
        shopPanel.goldText = goldText;
        shopPanel.continueButton = continueBtn.GetComponent<UnityEngine.UI.Button>();
        shopPanel.itemSlots = new ShopPanel.ShopItemSlotView[5];

        for (int i = 0; i < 5; i++)
        {
            var (icon, nameText, priceText, buyBtn, lockBtn) = CreateShopItemSlot(panel.transform);
            var slotGo = icon.gameObject.transform.parent.gameObject;
            slotGo.transform.SetParent(itemSlotsRoot.transform, false);
            shopPanel.itemSlots[i] = new ShopPanel.ShopItemSlotView
            {
                icon = icon,
                nameText = nameText,
                priceText = priceText,
                buyButton = buyBtn,
                lockButton = lockBtn
            };
        }

        var rerollGo = new GameObject("RerollButton");
        rerollGo.transform.SetParent(panel.transform, false);
        var rerollRt = rerollGo.AddComponent<RectTransform>();
        rerollRt.anchorMin = new Vector2(0.5f, 0.35f);
        rerollRt.anchorMax = new Vector2(0.5f, 0.35f);
        rerollRt.sizeDelta = new Vector2(140, 36);
        rerollRt.anchoredPosition = Vector2.zero;
        rerollGo.AddComponent<UnityEngine.UI.Image>().color = new Color(0.4f, 0.3f, 0.6f);
        var rerollBtn = rerollGo.AddComponent<UnityEngine.UI.Button>();
        var rerollTextGo = new GameObject("Text");
        rerollTextGo.transform.SetParent(rerollGo.transform, false);
        var rerollTextRt = rerollTextGo.AddComponent<RectTransform>();
        rerollTextRt.anchorMin = Vector2.zero;
        rerollTextRt.anchorMax = Vector2.one;
        rerollTextRt.offsetMin = Vector2.zero;
        rerollTextRt.offsetMax = Vector2.zero;
        var rerollText = rerollTextGo.AddComponent<UnityEngine.UI.Text>();
        rerollText.text = "随机 (10金币)";
        rerollText.alignment = TextAnchor.MiddleCenter;
        EggRogue.GameFont.ApplyTo(rerollText);
        rerollText.color = Color.white;
        shopPanel.rerollButton = rerollBtn;

        AddWeaponSlotsAndItemInventory(panel);

        var um = Object.FindObjectOfType<UIManager>();
        if (um != null)
        {
            um.shopPanel = shopPanel;
        }

        panel.SetActive(false);
        Undo.RegisterCreatedObjectUndo(panel, "Add ShopPanel");
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log("[ShopSetup] 已添加 ShopPanel 并尝试绑定到 UIManager。");
    }

    private static void AddWeaponSlotsAndItemInventory(GameObject panelGo)
    {
        var shopPanel = panelGo.GetComponent<ShopPanel>();
        if (shopPanel == null) return;

        var weaponTitle = new GameObject("WeaponSlotsTitle");
        weaponTitle.transform.SetParent(panelGo.transform, false);
        var wtRt = weaponTitle.AddComponent<RectTransform>();
        wtRt.anchorMin = new Vector2(0.15f, 0.85f);
        wtRt.anchorMax = new Vector2(0.45f, 0.88f);
        wtRt.offsetMin = Vector2.zero;
        wtRt.offsetMax = Vector2.zero;
        var wtText = weaponTitle.AddComponent<UnityEngine.UI.Text>();
        wtText.text = "当前武器（点击出售，拖拽同等级武器合并）";
        wtText.fontSize = 14;
        EggRogue.GameFont.ApplyTo(wtText);
        wtText.color = Color.white;

        var weaponsRoot = new GameObject("WeaponSlotsDisplay");
        weaponsRoot.transform.SetParent(panelGo.transform, false);
        var wRt = weaponsRoot.AddComponent<RectTransform>();
        wRt.anchorMin = new Vector2(0.15f, 0.7f);
        wRt.anchorMax = new Vector2(0.45f, 0.84f);
        wRt.offsetMin = Vector2.zero;
        wRt.offsetMax = Vector2.zero;
        var wGrid = weaponsRoot.AddComponent<UnityEngine.UI.GridLayoutGroup>();
        wGrid.cellSize = new Vector2(70, 90);
        wGrid.spacing = new Vector2(8f, 8f);
        wGrid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
        wGrid.constraintCount = 3;
        wGrid.childAlignment = TextAnchor.MiddleCenter;

        var weaponDisplay = weaponsRoot.AddComponent<ShopWeaponSlotsDisplay>();
        weaponDisplay.slots = new ShopWeaponSlotsDisplay.WeaponSlotView[6];
        for (int i = 0; i < 6; i++)
        {
            var slotGo = CreateWeaponSlotForShop($"WeaponSlot{i}");
            slotGo.transform.SetParent(weaponsRoot.transform, false);
            var icon = slotGo.transform.Find("Icon")?.GetComponent<UnityEngine.UI.Image>();
            var nameT = slotGo.transform.Find("NameText")?.GetComponent<UnityEngine.UI.Text>();
            var levelT = slotGo.transform.Find("LevelText")?.GetComponent<UnityEngine.UI.Text>();
            var sellBtn = slotGo.transform.Find("SellButton")?.GetComponent<UnityEngine.UI.Button>();
            weaponDisplay.slots[i] = new ShopWeaponSlotsDisplay.WeaponSlotView
            {
                root = slotGo.GetComponent<RectTransform>(),
                icon = icon,
                nameText = nameT,
                levelText = levelT,
                sellButton = sellBtn
            };
        }
        shopPanel.weaponSlotsDisplay = weaponDisplay;

        var itemTitle = new GameObject("ItemInventoryTitle");
        itemTitle.transform.SetParent(panelGo.transform, false);
        var itRt = itemTitle.AddComponent<RectTransform>();
        itRt.anchorMin = new Vector2(0.15f, 0.63f);
        itRt.anchorMax = new Vector2(0.45f, 0.66f);
        itRt.offsetMin = Vector2.zero;
        itRt.offsetMax = Vector2.zero;
        var itText = itemTitle.AddComponent<UnityEngine.UI.Text>();
        itText.text = "已购买物品";
        itText.fontSize = 14;
        EggRogue.GameFont.ApplyTo(itText);
        itText.color = Color.white;

        var itemsScroll = new GameObject("ItemInventoryScroll");
        itemsScroll.transform.SetParent(panelGo.transform, false);
        var scrollRt = itemsScroll.AddComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0.15f, 0.55f);
        scrollRt.anchorMax = new Vector2(0.45f, 0.62f);
        scrollRt.offsetMin = Vector2.zero;
        scrollRt.offsetMax = Vector2.zero;
        var scroll = itemsScroll.AddComponent<UnityEngine.UI.ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;

        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(itemsScroll.transform, false);
        var vpRt = viewport.AddComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = Vector2.zero;
        vpRt.offsetMax = Vector2.zero;
        viewport.AddComponent<UnityEngine.UI.RectMask2D>();
        var vpImage = viewport.AddComponent<UnityEngine.UI.Image>();
        vpImage.color = new Color(0f, 0f, 0f, 0f);
        scroll.viewport = vpRt;

        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var contentRt = content.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = Vector2.zero;
        scroll.content = contentRt;

        var itemDisplay = content.AddComponent<ShopItemInventoryDisplay>();
        itemDisplay.container = content.transform;
        itemDisplay.scrollRect = scroll;
        shopPanel.itemInventoryDisplay = itemDisplay;
    }

    private static GameObject CreateWeaponSlotForShop(string name)
    {
        var slot = new GameObject(name);
        slot.AddComponent<RectTransform>();
        var layout = slot.AddComponent<UnityEngine.UI.LayoutElement>();
        layout.preferredWidth = 70;
        layout.preferredHeight = 90;
        var bg = slot.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0.2f, 0.2f, 0.25f, 0.95f);
        bg.raycastTarget = true;

        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(slot.transform, false);
        var iconRt = iconGo.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.65f);
        iconRt.anchorMax = new Vector2(0.5f, 0.65f);
        iconRt.sizeDelta = new Vector2(40, 40);
        iconRt.anchoredPosition = Vector2.zero;
        iconGo.AddComponent<UnityEngine.UI.Image>();

        var nameGo = new GameObject("NameText");
        nameGo.transform.SetParent(slot.transform, false);
        var nameRt = nameGo.AddComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0.5f, 0.4f);
        nameRt.anchorMax = new Vector2(0.5f, 0.4f);
        nameRt.sizeDelta = new Vector2(60, 18);
        nameRt.anchoredPosition = Vector2.zero;
        var nameT = nameGo.AddComponent<UnityEngine.UI.Text>();
        nameT.text = "空";
        nameT.alignment = TextAnchor.MiddleCenter;
        nameT.fontSize = 10;
        EggRogue.GameFont.ApplyTo(nameT);

        var levelGo = new GameObject("LevelText");
        levelGo.transform.SetParent(slot.transform, false);
        var levelRt = levelGo.AddComponent<RectTransform>();
        levelRt.anchorMin = new Vector2(0.5f, 0.25f);
        levelRt.anchorMax = new Vector2(0.5f, 0.25f);
        levelRt.sizeDelta = new Vector2(50, 16);
        levelRt.anchoredPosition = Vector2.zero;
        var levelT = levelGo.AddComponent<UnityEngine.UI.Text>();
        levelT.text = "";
        levelT.alignment = TextAnchor.MiddleCenter;
        levelT.fontSize = 9;
        EggRogue.GameFont.ApplyTo(levelT);

        var sellGo = new GameObject("SellButton");
        sellGo.transform.SetParent(slot.transform, false);
        var sellRt = sellGo.AddComponent<RectTransform>();
        sellRt.anchorMin = new Vector2(0.5f, 0.02f);
        sellRt.anchorMax = new Vector2(0.5f, 0.18f);
        sellRt.sizeDelta = new Vector2(50, 18);
        sellRt.anchoredPosition = Vector2.zero;
        sellGo.AddComponent<UnityEngine.UI.Image>().color = new Color(0.6f, 0.3f, 0.2f);
        var sellBtn = sellGo.AddComponent<UnityEngine.UI.Button>();
        var sellTxt = new GameObject("Text");
        sellTxt.transform.SetParent(sellGo.transform, false);
        var sellTxtRt = sellTxt.AddComponent<RectTransform>();
        sellTxtRt.anchorMin = Vector2.zero;
        sellTxtRt.anchorMax = Vector2.one;
        sellTxtRt.offsetMin = Vector2.zero;
        sellTxtRt.offsetMax = Vector2.zero;
        var st = sellTxt.AddComponent<UnityEngine.UI.Text>();
        st.text = "出售";
        st.alignment = TextAnchor.MiddleCenter;
        st.fontSize = 9;
        st.color = Color.white;
        EggRogue.GameFont.ApplyTo(st);

        return slot;
    }

    private static (UnityEngine.UI.Image icon, UnityEngine.UI.Text nameText, UnityEngine.UI.Text priceText, UnityEngine.UI.Button buyButton, UnityEngine.UI.Button lockButton) CreateShopItemSlot(Transform parent)
    {
        var slot = new GameObject("ItemSlot");
        slot.transform.SetParent(parent, false);
        var layout = slot.AddComponent<UnityEngine.UI.LayoutElement>();
        layout.preferredWidth = 160;
        layout.preferredHeight = 100;

        var bg = slot.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(slot.transform, false);
        var iconRt = iconGo.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.7f);
        iconRt.anchorMax = new Vector2(0.5f, 0.7f);
        iconRt.sizeDelta = new Vector2(48, 48);
        iconRt.anchoredPosition = Vector2.zero;
        var icon = iconGo.AddComponent<UnityEngine.UI.Image>();
        icon.color = Color.white;

        var nameGo = new GameObject("Name");
        nameGo.transform.SetParent(slot.transform, false);
        var nameRt = nameGo.AddComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0.5f, 0.4f);
        nameRt.anchorMax = new Vector2(0.5f, 0.4f);
        nameRt.sizeDelta = new Vector2(140, 24);
        nameRt.anchoredPosition = Vector2.zero;
        var nameText = nameGo.AddComponent<UnityEngine.UI.Text>();
        nameText.text = "-";
        nameText.alignment = TextAnchor.MiddleCenter;
        nameText.fontSize = 14;
        EggRogue.GameFont.ApplyTo(nameText);
        nameText.color = Color.white;

        var priceGo = new GameObject("Price");
        priceGo.transform.SetParent(slot.transform, false);
        var priceRt = priceGo.AddComponent<RectTransform>();
        priceRt.anchorMin = new Vector2(0.5f, 0.2f);
        priceRt.anchorMax = new Vector2(0.5f, 0.2f);
        priceRt.sizeDelta = new Vector2(100, 20);
        priceRt.anchoredPosition = Vector2.zero;
        var priceText = priceGo.AddComponent<UnityEngine.UI.Text>();
        priceText.text = "";
        priceText.alignment = TextAnchor.MiddleCenter;
        priceText.fontSize = 12;
        EggRogue.GameFont.ApplyTo(priceText);
        priceText.color = Color.yellow;

        var buyGo = new GameObject("BuyButton");
        buyGo.transform.SetParent(slot.transform, false);
        var buyRt = buyGo.AddComponent<RectTransform>();
        buyRt.anchorMin = new Vector2(0.5f, 0.02f);
        buyRt.anchorMax = new Vector2(0.5f, 0.12f);
        buyRt.sizeDelta = new Vector2(80, 24);
        buyRt.anchoredPosition = Vector2.zero;
        buyGo.AddComponent<UnityEngine.UI.Image>().color = new Color(0.2f, 0.5f, 0.2f);
        var buyBtn = buyGo.AddComponent<UnityEngine.UI.Button>();
        var buyTextGo = new GameObject("Text");
        buyTextGo.transform.SetParent(buyGo.transform, false);
        var buyTextRt = buyTextGo.AddComponent<RectTransform>();
        buyTextRt.anchorMin = Vector2.zero;
        buyTextRt.anchorMax = Vector2.one;
        buyTextRt.offsetMin = Vector2.zero;
        buyTextRt.offsetMax = Vector2.zero;
        var buyText = buyTextGo.AddComponent<UnityEngine.UI.Text>();
        buyText.text = "购买";
        buyText.alignment = TextAnchor.MiddleCenter;
        buyText.fontSize = 12;
        EggRogue.GameFont.ApplyTo(buyText);
        buyText.color = Color.white;

        var lockGo = new GameObject("LockButton");
        lockGo.transform.SetParent(slot.transform, false);
        var lockRt = lockGo.AddComponent<RectTransform>();
        lockRt.anchorMin = new Vector2(1f, 0.5f);
        lockRt.anchorMax = new Vector2(1f, 0.5f);
        lockRt.pivot = new Vector2(1f, 0.5f);
        lockRt.anchoredPosition = new Vector2(-4f, 0f);
        lockRt.sizeDelta = new Vector2(44, 24);
        lockGo.AddComponent<UnityEngine.UI.Image>().color = new Color(0.3f, 0.3f, 0.4f, 0.9f);
        var lockBtn = lockGo.AddComponent<UnityEngine.UI.Button>();
        var lockTextGo = new GameObject("Text");
        lockTextGo.transform.SetParent(lockGo.transform, false);
        var lockTextRt = lockTextGo.AddComponent<RectTransform>();
        lockTextRt.anchorMin = Vector2.zero;
        lockTextRt.anchorMax = Vector2.one;
        lockTextRt.offsetMin = Vector2.zero;
        lockTextRt.offsetMax = Vector2.zero;
        var lockText = lockTextGo.AddComponent<UnityEngine.UI.Text>();
        lockText.text = "锁定";
        lockText.alignment = TextAnchor.MiddleCenter;
        lockText.fontSize = 10;
        EggRogue.GameFont.ApplyTo(lockText);
        lockText.color = Color.white;

        return (icon, nameText, priceText, buyBtn, lockBtn);
    }

    private static void AddItemSlotsToExistingShop(GameObject panelGo)
    {
        var shopPanel = panelGo.GetComponent<ShopPanel>();
        if (shopPanel == null) return;

        var itemSlotsRoot = panelGo.transform.Find("ItemSlots");
        if (itemSlotsRoot == null)
        {
            var root = new GameObject("ItemSlots");
            root.transform.SetParent(panelGo.transform, false);
            var slotsRt = root.AddComponent<RectTransform>();
            slotsRt.anchorMin = new Vector2(0.5f, 0.55f);
            slotsRt.anchorMax = new Vector2(0.5f, 0.55f);
            slotsRt.sizeDelta = new Vector2(900, 120);
            slotsRt.anchoredPosition = Vector2.zero;
            root.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            itemSlotsRoot = root.transform;
        }

        shopPanel.itemSlots = new ShopPanel.ShopItemSlotView[5];
        for (int i = 0; i < 5; i++)
        {
            var (icon, nameText, priceText, buyBtn, lockBtn) = CreateShopItemSlot(panelGo.transform);
            var slotGo = icon.gameObject.transform.parent.gameObject;
            slotGo.transform.SetParent(itemSlotsRoot, false);
            shopPanel.itemSlots[i] = new ShopPanel.ShopItemSlotView
            {
                icon = icon,
                nameText = nameText,
                priceText = priceText,
                buyButton = buyBtn,
                lockButton = lockBtn
            };
        }

        var reroll = panelGo.transform.Find("RerollButton");
        if (reroll == null)
        {
            var rerollGo = new GameObject("RerollButton");
            rerollGo.transform.SetParent(panelGo.transform, false);
            var rerollRt = rerollGo.AddComponent<RectTransform>();
            rerollRt.anchorMin = new Vector2(0.5f, 0.35f);
            rerollRt.anchorMax = new Vector2(0.5f, 0.35f);
            rerollRt.sizeDelta = new Vector2(140, 36);
            rerollRt.anchoredPosition = Vector2.zero;
            rerollGo.AddComponent<UnityEngine.UI.Image>().color = new Color(0.4f, 0.3f, 0.6f);
            var rerollBtn = rerollGo.AddComponent<UnityEngine.UI.Button>();
            var rerollTextGo = new GameObject("Text");
            rerollTextGo.transform.SetParent(rerollGo.transform, false);
            var rerollTextRt = rerollTextGo.AddComponent<RectTransform>();
            rerollTextRt.anchorMin = Vector2.zero;
            rerollTextRt.anchorMax = Vector2.one;
            rerollTextRt.offsetMin = Vector2.zero;
            rerollTextRt.offsetMax = Vector2.zero;
            var rerollText = rerollTextGo.AddComponent<UnityEngine.UI.Text>();
            rerollText.text = "随机 (10金币)";
            rerollText.alignment = TextAnchor.MiddleCenter;
            rerollText.color = Color.white;
            EggRogue.GameFont.ApplyTo(rerollText);
            shopPanel.rerollButton = rerollBtn;
        }
    }
}
