using UnityEngine;
using UnityEngine.UI;
using EggRogue;

/// <summary>
/// 设置面板 - 游戏内调节操控手感等，支持 PlayerPrefs 持久化。
/// 从 GameHUD 的设置按钮打开，可扩展音量等更多选项。
/// </summary>
public class SettingsPanel : BaseUIPanel
{
    private const string PrefsDeadZone = "Settings_DeadZone";
    private const string PrefsAcceleration = "Settings_AccelerationFactor";
    private const string PrefsRigidbodyDrag = "Settings_RigidbodyDrag";
    private const string PrefsJoystickReturnSpeed = "Settings_JoystickReturnSpeed";

    [Header("操控手感 - Slider")]
    [Tooltip("摇杆死区（0.01-0.25）")]
    public Slider deadZoneSlider;

    [Tooltip("移动响应系数（5-30，越大越跟手）")]
    public Slider accelerationSlider;

    [Tooltip("停止惯性/阻力（2-25，越大停得越快）")]
    public Slider rigidbodyDragSlider;

    [Tooltip("摇杆回中速度（5-25）")]
    public Slider joystickReturnSpeedSlider;

    [Header("按钮")]
    [Tooltip("关闭按钮")]
    public Button closeButton;

    [Header("数值显示（可选）")]
    public Text deadZoneValueText;
    public Text accelerationValueText;
    public Text rigidbodyDragValueText;
    public Text joystickReturnSpeedValueText;

    private void Start()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(OnCloseClicked);
        }

        if (deadZoneSlider != null)
            deadZoneSlider.onValueChanged.AddListener(_ => OnSliderChanged());
        if (accelerationSlider != null)
            accelerationSlider.onValueChanged.AddListener(_ => OnSliderChanged());
        if (rigidbodyDragSlider != null)
            rigidbodyDragSlider.onValueChanged.AddListener(_ => OnSliderChanged());
        if (joystickReturnSpeedSlider != null)
            joystickReturnSpeedSlider.onValueChanged.AddListener(_ => OnSliderChanged());
    }

    protected override void OnShow()
    {
        base.OnShow();
        if (GameplayPauseManager.Instance != null)
            GameplayPauseManager.Instance.RequestPause("SettingsPanel");
        LoadFromPrefs();
        RefreshValueTexts();
        ApplyToComponents();
    }

    protected override void OnHide()
    {
        SaveToPrefs();
        if (GameplayPauseManager.Instance != null)
            GameplayPauseManager.Instance.RequestResume("SettingsPanel");
        base.OnHide();
    }

    private void OnCloseClicked()
    {
        SaveToPrefs();
        Hide();
    }

    private void OnSliderChanged()
    {
        ApplyToComponents();
        RefreshValueTexts();
    }

    private void RefreshValueTexts()
    {
        if (deadZoneValueText != null)
            deadZoneValueText.text = GetDeadZone().ToString("F2");
        if (accelerationValueText != null)
            accelerationValueText.text = GetAcceleration().ToString("F0");
        if (rigidbodyDragValueText != null)
            rigidbodyDragValueText.text = GetRigidbodyDrag().ToString("F0");
        if (joystickReturnSpeedValueText != null)
            joystickReturnSpeedValueText.text = GetJoystickReturnSpeed().ToString("F0");
    }

    private void ApplyToComponents()
    {
        float dz = GetDeadZone();
        float acc = GetAcceleration();
        float drag = GetRigidbodyDrag();
        float ret = GetJoystickReturnSpeed();

        var touchHandler = FindObjectOfType<TouchInputHandler>();
        if (touchHandler != null)
            touchHandler.deadZone = dz;

        var characterController = FindObjectOfType<CharacterController>();
        if (characterController != null)
        {
            characterController.accelerationFactor = acc;
            characterController.SetRigidbodyDrag(drag);
        }

        var joystick = FindObjectOfType<VirtualJoystick>();
        if (joystick != null)
            joystick.returnSpeed = ret;
    }

    private float GetDeadZone()
    {
        if (deadZoneSlider != null)
            return Mathf.Lerp(0.01f, 0.25f, deadZoneSlider.value);
        return PlayerPrefs.GetFloat(PrefsDeadZone, 0.1f);
    }

    private float GetAcceleration()
    {
        if (accelerationSlider != null)
            return Mathf.Lerp(5f, 30f, accelerationSlider.value);
        return PlayerPrefs.GetFloat(PrefsAcceleration, 15f);
    }

    private float GetRigidbodyDrag()
    {
        if (rigidbodyDragSlider != null)
            return Mathf.Lerp(2f, 25f, rigidbodyDragSlider.value);
        return PlayerPrefs.GetFloat(PrefsRigidbodyDrag, 10f);
    }

    private float GetJoystickReturnSpeed()
    {
        if (joystickReturnSpeedSlider != null)
            return Mathf.Lerp(5f, 25f, joystickReturnSpeedSlider.value);
        return PlayerPrefs.GetFloat(PrefsJoystickReturnSpeed, 10f);
    }

    private void LoadFromPrefs()
    {
        float dz = PlayerPrefs.GetFloat(PrefsDeadZone, 0.1f);
        float acc = PlayerPrefs.GetFloat(PrefsAcceleration, 15f);
        float drag = PlayerPrefs.GetFloat(PrefsRigidbodyDrag, 10f);
        float ret = PlayerPrefs.GetFloat(PrefsJoystickReturnSpeed, 10f);

        if (deadZoneSlider != null)
            deadZoneSlider.value = Mathf.InverseLerp(0.01f, 0.25f, dz);
        if (accelerationSlider != null)
            accelerationSlider.value = Mathf.InverseLerp(5f, 30f, acc);
        if (rigidbodyDragSlider != null)
            rigidbodyDragSlider.value = Mathf.InverseLerp(2f, 25f, drag);
        if (joystickReturnSpeedSlider != null)
            joystickReturnSpeedSlider.value = Mathf.InverseLerp(5f, 25f, ret);
    }

    private void SaveToPrefs()
    {
        PlayerPrefs.SetFloat(PrefsDeadZone, GetDeadZone());
        PlayerPrefs.SetFloat(PrefsAcceleration, GetAcceleration());
        PlayerPrefs.SetFloat(PrefsRigidbodyDrag, GetRigidbodyDrag());
        PlayerPrefs.SetFloat(PrefsJoystickReturnSpeed, GetJoystickReturnSpeed());
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 将已保存的设置应用到当前场景的操控组件（进入游戏/下一关时调用）
    /// </summary>
    public static void ApplySavedToScene()
    {
        float dz = PlayerPrefs.GetFloat(PrefsDeadZone, -1f);
        float acc = PlayerPrefs.GetFloat(PrefsAcceleration, -1f);
        float drag = PlayerPrefs.GetFloat(PrefsRigidbodyDrag, -1f);
        float ret = PlayerPrefs.GetFloat(PrefsJoystickReturnSpeed, -1f);

        if (dz >= 0)
        {
            var th = FindObjectOfType<TouchInputHandler>();
            if (th != null) th.deadZone = dz;
        }
        if (acc >= 0)
        {
            var cc = FindObjectOfType<CharacterController>();
            if (cc != null) cc.accelerationFactor = acc;
        }
        if (drag >= 0)
        {
            var cc = FindObjectOfType<CharacterController>();
            if (cc != null) cc.SetRigidbodyDrag(drag);
        }
        if (ret >= 0)
        {
            var j = FindObjectOfType<VirtualJoystick>();
            if (j != null) j.returnSpeed = ret;
        }
    }
}
