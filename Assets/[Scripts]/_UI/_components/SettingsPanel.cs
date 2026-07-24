using DG.Tweening;
using GAITemplate;
using UnityEngine;
using UnityEngine.UI;

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
        EnsureAudioToggle();

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

    /// <summary>
    /// SampleScene historically had no Audio switch assigned. Clone the Vibration
    /// toggle and place it on the Audio row so both settings always match.
    /// </summary>
    private void EnsureAudioToggle()
    {
        if (soundToggle != null)
            return;
        if (vibrationToggle == null || settingsPanel == null)
            return;

        Transform soundLabel = FindDeepChild(settingsPanel.transform, "SoundText");
        if (soundLabel == null)
            return;

        // TMP label steals clicks over the switch area if left raycastable.
        Graphic soundGraphic = soundLabel.GetComponent<Graphic>();
        if (soundGraphic != null)
            soundGraphic.raycastTarget = false;

        for (int i = soundLabel.childCount - 1; i >= 0; i--)
            Destroy(soundLabel.GetChild(i).gameObject);

        GameObject clone = Instantiate(vibrationToggle.gameObject, soundLabel, false);
        clone.name = "AudioSwitch";
        clone.SetActive(true);

        RectTransform cloneRect = clone.GetComponent<RectTransform>();
        RectTransform vibrationRect = vibrationToggle.GetComponent<RectTransform>();
        if (cloneRect != null)
        {
            if (vibrationRect != null)
            {
                cloneRect.anchorMin = vibrationRect.anchorMin;
                cloneRect.anchorMax = vibrationRect.anchorMax;
                cloneRect.pivot = vibrationRect.pivot;
                cloneRect.sizeDelta = vibrationRect.sizeDelta;
                cloneRect.localRotation = vibrationRect.localRotation;
                cloneRect.localScale = vibrationRect.localScale;
            }

            // Same panel X as Vibration: SoundText(-100) + 278.15 ≈ HapticText(-142.48) + 320.63
            cloneRect.anchoredPosition = new Vector2(278.15f, 0f);
        }

        soundToggle = clone.GetComponent<ToggleSwitch>();
        if (soundToggle == null)
            soundToggle = clone.AddComponent<ToggleSwitch>();

        // Instantiate of a scene PrefabInstance can leave ToggleSwitch refs on the
        // original Vibration objects — rebind to this clone so visuals actually move.
        Transform handle = clone.transform.Find("Handle");
        soundToggle.thumb = handle != null
            ? handle as RectTransform
            : clone.transform.GetChild(0) as RectTransform;
        soundToggle.trackImage = clone.GetComponent<Image>();
        if (vibrationToggle != null)
        {
            soundToggle.offThumbPosition = vibrationToggle.offThumbPosition;
            soundToggle.onThumbPosition = vibrationToggle.onThumbPosition;
            soundToggle.offTrackColor = vibrationToggle.offTrackColor;
            soundToggle.onTrackColor = vibrationToggle.onTrackColor;
            soundToggle.animDuration = vibrationToggle.animDuration;
            soundToggle.animEase = vibrationToggle.animEase;
        }

        BindToggleButton(clone, soundToggle);
        if (handle != null)
            BindToggleButton(handle.gameObject, soundToggle);
    }

    private static void BindToggleButton(GameObject target, ToggleSwitch toggle)
    {
        if (target == null || toggle == null)
            return;

        Button button = target.GetComponent<Button>();
        if (button == null)
        {
            button = target.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            Graphic graphic = target.GetComponent<Graphic>();
            if (graphic != null)
            {
                graphic.raycastTarget = true;
                button.targetGraphic = graphic;
            }
        }

        // Persistent inspector listeners still call Vibration.Toggle after Instantiate;
        // replace the whole event so only this Audio toggle is driven.
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(toggle.Toggle);
    }

    private static Transform FindDeepChild(Transform root, string name)
    {
        if (root == null)
            return null;
        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepChild(root.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
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
