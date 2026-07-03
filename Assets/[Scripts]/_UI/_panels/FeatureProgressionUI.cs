using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GAITemplate
{
    /// <summary>
    /// Win-screen milestone bar: progress toward the next feature unlock level.
    /// Mirrors Beads_Out EndPanel feature progression without remote config.
    /// </summary>
    public class FeatureProgressionUI : MonoBehaviour
    {
        [Header("Root")]
        [Tooltip("Hidden when progression is disabled or all milestones are passed.")]
        public GameObject progressionRoot;

        [Header("Slots")]
        public List<GameObject> darkOverlays = new List<GameObject>();
        public List<Image> fillImages = new List<Image>();

        [Header("Text")]
        public TextMeshProUGUI newFeatureText;

        [Header("All Milestones Complete")]
        [Tooltip("Shown when every feature milestone is unlocked. Feature slots are hidden.")]
        public GameObject coinRewardImage;

        [Tooltip("Optional parent for milestone slots (dark overlays + fill images). Hidden in coin mode.")]
        public GameObject featureSlotsRoot;

        [Header("Animation")]
        [Min(0f)] public float fillAnimDuration = 0.5f;

        private int _lastFeaturePercent;

        public void Refresh()
        {
            FeatureProgressionConfig config = ResolveConfig();
            int progressLevel = GetProgressLevel();

            if (config == null || !config.HasMilestones || progressLevel <= 0)
            {
                SetRootActive(false);
                return;
            }

            if (config.IsMilestoneLevel(progressLevel))
                FeatureProgressionStore.MarkMilestoneReached(progressLevel);

            SetRootActive(true);

            bool justUnlockedMilestone = config.IsMilestoneLevel(progressLevel);
            bool hasRemainingMilestones = config.HasRemainingMilestones(progressLevel);
            SetCoinRewardVisible(!hasRemainingMilestones && !justUnlockedMilestone);

            if (!hasRemainingMilestones && !justUnlockedMilestone)
            {
                SetFeatureSlotsVisible(false);

                if (newFeatureText != null)
                    newFeatureText.gameObject.SetActive(false);

                _lastFeaturePercent = 100;
                return;
            }

            SetFeatureSlotsVisible(true);
            UpdateFillImages(progressLevel, config.featureLevels);
            UpdateStatusText(progressLevel, config);
            UpdateActiveSlot(progressLevel, config.featureLevels);
        }

        /// <summary>
        /// Win screen runs after LevelUp, so use the level just completed (not the next level to play).
        /// </summary>
        private static int GetProgressLevel()
        {
            if (GameManager.instance == null)
                return 0;

            return Mathf.Max(0, GameManager.instance.level - 1);
        }

        public void ResetState()
        {
            _lastFeaturePercent = 0;

            foreach (Image fill in fillImages)
            {
                if (fill == null)
                    continue;

                fill.DOKill();
                fill.fillAmount = 0f;
            }

            if (newFeatureText != null)
                newFeatureText.gameObject.SetActive(false);

            SetCoinRewardVisible(false);
            SetFeatureSlotsVisible(true);
        }

        private static FeatureProgressionConfig ResolveConfig()
        {
            if (GameManager.instance == null)
                return null;

            return GameManager.instance.FeatureProgressionConfig;
        }

        private void SetRootActive(bool active)
        {
            if (progressionRoot != null)
            {
                progressionRoot.SetActive(active);
            }
            else
            {
                if (!active)
                {
                    if (featureSlotsRoot != null) featureSlotsRoot.SetActive(false);
                    if (newFeatureText != null) newFeatureText.gameObject.SetActive(false);
                    if (coinRewardImage != null) coinRewardImage.SetActive(false);
                }
            }
        }

        private void SetCoinRewardVisible(bool visible)
        {
            if (coinRewardImage != null)
                coinRewardImage.SetActive(visible);
        }

        private void SetFeatureSlotsVisible(bool visible)
        {
            if (featureSlotsRoot != null)
                featureSlotsRoot.SetActive(visible);

            if (visible)
                return;

            HideAllFeatureSlots();
        }

        private void HideAllFeatureSlots()
        {
            for (int i = 0; i < fillImages.Count; i++)
            {
                if (fillImages[i] != null)
                    fillImages[i].gameObject.SetActive(false);
            }

            for (int i = 0; i < darkOverlays.Count; i++)
            {
                if (darkOverlays[i] != null)
                    darkOverlays[i].SetActive(false);
            }
        }

        private void UpdateFillImages(int progressLevel, IReadOnlyList<int> featureLevels)
        {
            for (int i = 0; i < featureLevels.Count; i++)
            {
                if (i >= fillImages.Count)
                    break;

                Image fill = fillImages[i];
                if (fill == null)
                    continue;

                int featureLevel = featureLevels[i];
                fill.DOKill();

                if (progressLevel >= featureLevel)
                {
                    fill.DOFillAmount(1f, fillAnimDuration).SetEase(Ease.OutQuad);
                    continue;
                }

                int previousFeatureLevel = 0;
                for (int j = 0; j < i; j++)
                {
                    if (featureLevels[j] < progressLevel)
                        previousFeatureLevel = featureLevels[j];
                }

                float total = featureLevel - previousFeatureLevel;
                float progress = total > 0f
                    ? Mathf.Clamp01((progressLevel - previousFeatureLevel) / total)
                    : 0f;

                fill.DOFillAmount(progress, fillAnimDuration).SetEase(Ease.OutQuad);
            }
        }

        private void UpdateStatusText(int progressLevel, FeatureProgressionConfig config)
        {
            if (newFeatureText == null)
                return;

            if (config.IsMilestoneLevel(progressLevel))
            {
                newFeatureText.gameObject.SetActive(true);
                newFeatureText.text = config.unlockedMessage;
                _lastFeaturePercent = 100;
                return;
            }

            newFeatureText.gameObject.SetActive(true);

            float progress = CalculateProgressToNextFeature(
                progressLevel,
                config.featureLevels,
                out _,
                out _);
            int percentage = Mathf.RoundToInt(progress * 100f);
            AnimatePercentText(config, _lastFeaturePercent, percentage);
            _lastFeaturePercent = percentage;
        }

        private void AnimatePercentText(FeatureProgressionConfig config, int startValue, int endValue)
        {
            if (newFeatureText == null)
                return;

            DOVirtual.Float(startValue, endValue, fillAnimDuration, value =>
            {
                if (newFeatureText != null)
                    newFeatureText.text = string.Format(config.progressFormat, Mathf.RoundToInt(value));
            }).SetTarget(this);
        }

        private void UpdateActiveSlot(int progressLevel, IReadOnlyList<int> featureLevels)
        {
            for (int i = 0; i < fillImages.Count; i++)
            {
                if (fillImages[i] != null)
                    fillImages[i].gameObject.SetActive(false);
            }

            for (int i = 0; i < darkOverlays.Count; i++)
            {
                if (darkOverlays[i] != null)
                    darkOverlays[i].SetActive(false);
            }

            int showIndex = -1;
            for (int i = 0; i < featureLevels.Count; i++)
            {
                if (progressLevel < featureLevels[i])
                {
                    showIndex = i;
                    break;
                }
            }

            if (showIndex == -1)
                showIndex = featureLevels.Count;

            int activeIndex = showIndex > 0 && progressLevel == featureLevels[showIndex - 1]
                ? showIndex - 1
                : showIndex;

            for (int i = 0; i < fillImages.Count; i++)
            {
                bool isActive = i == activeIndex;
                if (fillImages[i] != null)
                    fillImages[i].gameObject.SetActive(isActive);
                if (i < darkOverlays.Count && darkOverlays[i] != null)
                    darkOverlays[i].SetActive(isActive);
            }
        }

        public static float CalculateProgressToNextFeature(
            int progressLevel,
            IReadOnlyList<int> featureLevels,
            out int previousFeatureLevel,
            out int nextFeatureLevel)
        {
            previousFeatureLevel = 0;
            nextFeatureLevel = int.MaxValue;

            if (featureLevels == null || featureLevels.Count == 0)
                return 1f;

            for (int i = 0; i < featureLevels.Count; i++)
            {
                if (progressLevel < featureLevels[i])
                {
                    nextFeatureLevel = featureLevels[i];
                    break;
                }

                previousFeatureLevel = featureLevels[i];
            }

            float total = nextFeatureLevel - previousFeatureLevel;
            if (total <= 0f)
                return 1f;

            return Mathf.Clamp01((progressLevel - previousFeatureLevel) / total);
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
        }
    }
}
