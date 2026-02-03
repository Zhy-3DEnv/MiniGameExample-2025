using UnityEngine;
using UnityEngine.UI;
using EggRogue;

/// <summary>
/// 武器选择界面 - 首次进入游戏时选择 1 把起始武器。
/// 显示武器池（默认 1 枪 + 1 刀），选定后进入游戏。
/// </summary>
public class WeaponSelectionPanel : BaseUIPanel
{
    [Header("UI 引用")]
    [Tooltip("武器按钮容器")]
    public Transform weaponButtonContainer;

    [Tooltip("武器按钮预制体")]
    public GameObject weaponButtonPrefab;

    [Tooltip("标题文本")]
    public Text titleText;

    [Tooltip("提示文本")]
    public Text promptText;

    [Tooltip("确认按钮")]
    public Button confirmButton;

    [Header("配置")]
    [Tooltip("武器数据库（用于获取起始武器池）")]
    public WeaponDatabase weaponDatabase;

    private WeaponData _selectedWeapon;
    private int _selectedIndex = -1;

    protected override void OnShow()
    {
        base.OnShow();

        if (titleText != null)
            titleText.text = "选择起始武器";

        if (promptText != null)
            promptText.text = "选择 1 把武器开始冒险";

        _selectedWeapon = null;
        _selectedIndex = -1;
        RefreshWeaponList();

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirmClicked);
            confirmButton.interactable = false;
        }

        if (GameplayPauseManager.Instance != null)
            GameplayPauseManager.Instance.RequestPause("WeaponSelectionPanel");
    }

    protected override void OnHide()
    {
        base.OnHide();
        if (GameplayPauseManager.Instance != null)
            GameplayPauseManager.Instance.RequestResume("WeaponSelectionPanel");
    }

    private void RefreshWeaponList()
    {
        if (weaponButtonContainer == null || weaponDatabase == null) return;

        for (int i = weaponButtonContainer.childCount - 1; i >= 0; i--)
            Destroy(weaponButtonContainer.GetChild(i).gameObject);

        var pool = weaponDatabase.GetStarterWeaponPool();
        if (pool == null || pool.Length == 0) return;

        for (int i = 0; i < pool.Length; i++)
        {
            var weapon = pool[i];
            if (weapon == null) continue;

            GameObject btnObj = weaponButtonPrefab != null
                ? Instantiate(weaponButtonPrefab, weaponButtonContainer)
                : CreateDefaultButton(weaponButtonContainer);

            btnObj.name = $"WeaponBtn_{weapon.weaponName}";

            var btn = btnObj.GetComponent<Button>();
            if (btn == null) btn = btnObj.AddComponent<Button>();

            var t = btnObj.GetComponentInChildren<Text>(true);
            if (t != null) t.text = weapon.weaponName;

            // 查找名为 Icon 的子节点（含未激活），预制体中 Icon 可能默认未激活
            Image icon = null;
            for (int j = 0; j < btnObj.transform.childCount; j++)
            {
                var child = btnObj.transform.GetChild(j);
                if (child.name == "Icon")
                {
                    icon = child.GetComponent<Image>();
                    break;
                }
            }
            if (icon == null)
                icon = btnObj.GetComponent<Image>();
            if (icon != null)
            {
                if (weapon.icon != null)
                {
                    icon.sprite = weapon.icon;
                    icon.enabled = true;
                    icon.gameObject.SetActive(true);
                }
                else
                {
                    icon.enabled = false;
                    icon.gameObject.SetActive(false);
                }
            }

            int idx = i;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnWeaponSelected(idx, weapon));

            SetWeaponButtonSelected(btnObj, false);
        }

        UpdateAllWeaponButtonBorders();
    }

    /// <summary>
    /// 设置单个武器按钮的选中边框显示状态。
    /// 预制体下需有名为 SelectedBorder 的子节点（Image），用于选中时显示边框。
    /// </summary>
    private void SetWeaponButtonSelected(GameObject btnObj, bool selected)
    {
        if (btnObj == null) return;
        var border = btnObj.transform.Find("SelectedBorder");
        if (border == null)
            border = btnObj.transform.Find("Border");
        if (border != null)
            border.gameObject.SetActive(selected);
    }

    /// <summary>
    /// 根据当前选中的索引，刷新所有武器按钮的边框显示（互斥：仅一个显示边框）。
    /// </summary>
    private void UpdateAllWeaponButtonBorders()
    {
        if (weaponButtonContainer == null) return;
        for (int i = 0; i < weaponButtonContainer.childCount; i++)
        {
            var child = weaponButtonContainer.GetChild(i).gameObject;
            SetWeaponButtonSelected(child, i == _selectedIndex);
        }
    }

    private GameObject CreateDefaultButton(Transform parent)
    {
        var go = new GameObject("WeaponButton");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(160, 60);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.3f, 0.5f, 0.8f);
        var btn = go.AddComponent<Button>();
        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        var text = textGo.AddComponent<Text>();
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 18;
        text.color = Color.white;
        return go;
    }

    private void OnWeaponSelected(int index, WeaponData weapon)
    {
        _selectedWeapon = weapon;
        _selectedIndex = index;
        UpdateAllWeaponButtonBorders();
        if (confirmButton != null)
            confirmButton.interactable = true;
    }

    private void OnConfirmClicked()
    {
        if (_selectedWeapon == null)
        {
            Debug.LogWarning("WeaponSelectionPanel: 未选择武器");
            return;
        }

        if (WeaponSelectionManager.Instance != null)
            WeaponSelectionManager.Instance.SetStarterWeapon(_selectedWeapon);

        if (WeaponInventoryManager.Instance != null)
            WeaponInventoryManager.Instance.InitializeFromStarterWeapon();

        Hide();

        if (GameManager.Instance != null)
            GameManager.Instance.LoadGameScene(1);
    }
}
