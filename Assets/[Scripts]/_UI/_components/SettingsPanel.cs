using DG.Tweening;
using GAITemplate;
using UnityEngine;

public class SettingsPanel : MonoBehaviour
{
    [Header("Panel")]
    public GameObject settingsPanel;
    public RectTransform settingsIcon;
    public GameObject settingsBG;

    [Header("Toggles")]
    public ToggleSwitch soundToggle;
    public ToggleSwitch vibrationToggle;

    private Vector3 _settingsPanelStartScale;
    private bool _isOpen;

    private void Start()
    {
        _settingsPanelStartScale = settingsPanel.transform.localScale;

        if (soundToggle != null)
        {
            soundToggle.SetValueWithoutEvent(DataManager.instance.sound);
            soundToggle.onValueChanged.AddListener(OnSoundChanged);
        }

        if (vibrationToggle != null)
        {
            vibrationToggle.SetValueWithoutEvent(DataManager.instance.vibration);
            vibrationToggle.onValueChanged.AddListener(OnVibrationChanged);
        }

        SoundManager.instance.AllSound(DataManager.instance.sound);
    }

    // ── Panel open / close ───────────────────────────────────────────────────────────

    public void OnPressSettingsButton()
    {
        Haptic.MediumTaptic();
        if (!_isOpen) AppearSettings();
        else          DisappearSettings();
        _isOpen = !_isOpen;
    }

    public void SettingsPanelClose() => DisappearSettings();

    public void AppearSettings(float duration = 0.3f)
    {
        if (settingsIcon != null)
            settingsIcon.DORotate(new Vector3(0, 0, -360), duration, RotateMode.FastBeyond360);

        DOTween.Kill(settingsPanel.transform);
        settingsPanel.transform.localScale = Vector3.zero;
        settingsPanel.SetActive(true);
        if (settingsBG != null) settingsBG.SetActive(true);
        settingsPanel.transform.DOScale(_settingsPanelStartScale, duration).SetEase(Ease.OutBack);
    }

    private void DisappearSettings(float duration = 0.3f)
    {
        if (settingsBG != null) settingsBG.SetActive(false);
        DOTween.Kill(settingsPanel.transform);
        settingsPanel.transform.DOScale(Vector3.zero, duration).SetEase(Ease.InBack)
            .OnComplete(() => settingsPanel.SetActive(false));
        _isOpen = false;
    }

    // ── Toggle handlers ──────────────────────────────────────────────────────────────

    private void OnSoundChanged(bool value)
    {
        DataManager.instance.SetSound(value);
        SoundManager.instance.AllSound(value);
    }

    private void OnVibrationChanged(bool value)
    {
        DataManager.instance.SetVibration(value);
    }
}
