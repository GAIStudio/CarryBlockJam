using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace GAITemplate
{
    /// <summary>
    /// Success durumunda gösterilen panel.
    /// Reveal: Title fade + Continue button text güncelle + Confetti.
    /// Click → Coin spawn/uçma → Tüm coin'ler iniş yapınca sahne reload.
    /// </summary>
    public class SuccessPanel : EndPanelBase
    {
        [Header("Reward Timing")]
        public float rewardDelay = 0.30f;

        [Header("Confetti")]
        [Tooltip("Sahnede hazır ParticleSystem. Play On Awake kapalı olmalı.")]
        public ParticleSystem confetti;

        [Header("Coin Flight")]
        [Tooltip("Spawn edilecek coin UI prefab'ı (RectTransform içeren küçük Image).")]
        public GameObject coinPrefab;

        [Tooltip("Son coin iniş yaptıktan sonra sahne reload'undan önce beklenecek süre.")]
        [Min(0f)] public float postLandingDelay = 0.5f;

        [Tooltip("Coin'lerin spawn olduğu nokta.")]
        public RectTransform coinSpawnPoint;

        [Tooltip("Coin'lerin uçacağı hedef (GamePanel'deki coin panel rect'i).")]
        public RectTransform coinTarget;

        [Tooltip("Hedefteki para sayacı text'i. Her coin iniş yapınca direkt buraya yazılır. " +
                 "(GamePanel'in moneyText'i — Update'i durmuş olsa bile güncellenmesi için lazım.)")]
        public TextMeshProUGUI coinTargetMoneyText;

        [Min(1)] public int coinCount = 10;
        public float coinSpawnInterval = 0.04f;
        public float coinFlyDuration   = 0.55f;
        public float coinJumpPower     = 200f;
        public Vector2 coinSpawnSpread = new Vector2(40f, 40f);

        [Tooltip("Her coin iniş yaptığında çalınacak ses adı. Boş bırakılırsa ses çalmaz.")]
        public string coinLandSound = "Coin";

        [Header("Camera Stack (URP)")]
        [Tooltip("Show() çağrılınca Camera.main'in URP stack'ine Overlay olarak eklenir, " +
                 "panel kapanınca otomatik çıkarılır.")]
        public Camera successCamera;

        [Header("Feature Progression")]
        public FeatureProgressionUI featureProgression;

        [Header("Success Visuals")]
        [Tooltip("Optional direct reference to the main success emoji/graphic on the panel.")]
        public RectTransform mainEmoji;

        [Header("Continue Button Reward")]
        [Tooltip("Continue butonunda gösterilen reward sayısı (örn. \"40\"). " +
                 "Boş bırakılırsa hiç güncellenmez.")]
        public TextMeshProUGUI continueRewardText;

        [Tooltip("Continue button text format'ı. {0} reward miktarıyla değiştirilir. " +
                 "TMP sprite/ikon kullanmak için inline yazabilirsiniz: \"Next <sprite=0> {0}\".")]
        public string continueRewardFormat = "{0}";

        [Tooltip("Açık ise SuccessPanel kendi reward miktarını buradan kullanır. " +
                 "Kapalıysa GameData.levelCompleteReward kullanılır.")]
        public bool overrideRewardAmount;

        [Min(0)]
        [Tooltip("overrideRewardAmount açıkken kullanılacak reward miktarı.")]
        public int rewardAmount = 40;

        [Header("Emoji Shine")]
        [Min(0f)] public float emojiPulseDuration = 0.7f;
        [Min(0f)] public float emojiShineDuration = 1.15f;
        public Vector3 emojiPunchScale = new Vector3(0.16f, 0.16f, 0.16f);
        public Vector3 emojiShineRotation = new Vector3(0f, 0f, 7f);

        // ── State ─────────────────────────────────────────────────────────────────────

        private bool _isFinishing;
        private int  _coinsInFlight;
        private Tween _emojiLoopTween;

        // ── Lifecycle ─────────────────────────────────────────────────────────────────

        public override void Show()
        {
            AddCameraToStack();
            ResolveSceneReferences();
            UpdateContinueRewardText();
            base.Show(); // _shown guard'ı base'de
            featureProgression?.Refresh();
        }

        protected override void OnDisable()
        {
            StopEmojiLoop();
            RemoveCameraFromStack();
            base.OnDisable();
        }

        /// <summary>Continue button tıklanınca çağrılır → coin spawn + uçma → restart.</summary>
        public override void OnPressRestart()
        {
            if (_isFinishing) return;
            _isFinishing = true;

            SetContinueInteractable(false);

            int amount = ResolveRewardAmount();

            bool willFly = amount > 0 && coinPrefab != null && coinTarget != null && coinSpawnPoint != null;
            if (willFly)
            {
                SpawnAndFlyCoins(amount);
                // Tüm coin'ler iniş yaptığında OnCoinLanded base.OnPressRestart çağıracak.
            }
            else
            {
                if (amount > 0 && GameManager.instance != null)
                    GameManager.instance.AddMoney(amount);
                base.OnPressRestart();
            }
        }

        // ── Reset / Build ─────────────────────────────────────────────────────────────

        protected override void ResetVisuals()
        {
            base.ResetVisuals();

            ResolveSceneReferences();
            StopEmojiLoop();

            if (confetti != null)
                confetti.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            SetContinueInteractable(false);
            _isFinishing   = false;
            _coinsInFlight = 0;

            featureProgression?.ResetState();
        }

        protected override Sequence BuildSequence()
        {
            Sequence seq = base.BuildSequence();

            // Reveal sırasında sadece button text + confetti.
            seq.InsertCallback(rewardDelay, UpdateContinueRewardText);
            seq.InsertCallback(rewardDelay, StartEmojiLoop);
            if (confetti != null)
                seq.InsertCallback(rewardDelay, () => confetti.Play(true));

            // Button reveal animasyonu bitince button'ı interactive yap.
            seq.InsertCallback(buttonDelay + buttonDuration, () => SetContinueInteractable(true));

            return seq;
        }

        private void UpdateContinueRewardText()
        {
            ResolveSceneReferences();
            if (continueRewardText == null) return;

            int amount = ResolveRewardAmount();

            continueRewardText.text = string.Format(continueRewardFormat, amount);
        }

        // ── Coin uçurma ──────────────────────────────────────────────────────────────

        private void SpawnAndFlyCoins(int totalAmount)
        {
            int perCoin   = Mathf.Max(1, totalAmount / coinCount);
            int remainder = totalAmount - (perCoin * coinCount);

            _coinsInFlight = coinCount;

            for (int i = 0; i < coinCount; i++)
            {
                int   indexCopy   = i;
                int   delta       = perCoin + (i == coinCount - 1 ? remainder : 0);
                float spawnDelay  = i * coinSpawnInterval;

                DOVirtual.DelayedCall(spawnDelay, () =>
                    SpawnOneCoin(indexCopy, delta), ignoreTimeScale: false);
            }
        }

        private void SpawnOneCoin(int index, int rewardDelta)
        {
            GameObject coin = Instantiate(coinPrefab, coinSpawnPoint.parent);
            RectTransform coinRT = coin.GetComponent<RectTransform>();
            if (coinRT == null) { Destroy(coin); return; }

            coinRT.position = coinSpawnPoint.position;
            coinRT.anchoredPosition += new Vector2(
                Random.Range(-coinSpawnSpread.x, coinSpawnSpread.x),
                Random.Range(-coinSpawnSpread.y, coinSpawnSpread.y));

            float duration = coinFlyDuration + Random.Range(-0.05f, 0.05f);

            coinRT
                .DOJump(coinTarget.position, coinJumpPower, 1, duration)
                .SetEase(Ease.InQuad)
                .OnComplete(() => OnCoinLanded(coin, rewardDelta));
        }

        private void OnCoinLanded(GameObject coin, int rewardDelta)
        {
            Destroy(coin);

            if (GameManager.instance != null)
                GameManager.instance.AddMoney(rewardDelta);

            // Coin iniş sesi (üst üste çalınabildiği için PlayOneShot).
            if (!string.IsNullOrEmpty(coinLandSound) && SoundManager.instance != null)
                SoundManager.instance.PlayOneShot(coinLandSound);

            // Para sayacını direkt güncelle (GamePanel inaktif olduğu için Update'i çalışmaz).
            UpdateMoneyText();

            if (coinTarget != null)
            {
                coinTarget.DOKill(complete: true);
                coinTarget.DOPunchScale(Vector3.one * 0.12f, 0.2f, vibrato: 1, elasticity: 0.5f);
            }

            // Son coin iniş yapınca kısa bir bekleme sonrası sahneyi reload et.
            _coinsInFlight--;
            if (_coinsInFlight <= 0)
                DOVirtual.DelayedCall(postLandingDelay, () => base.OnPressRestart(),
                    ignoreTimeScale: false);
        }

        // ── Money text update ────────────────────────────────────────────────────────

        private TextMeshProUGUI _resolvedMoneyText;

        private void UpdateMoneyText()
        {
            if (GameManager.instance == null) return;

            // Inspector'dan atanmış text varsa onu kullan; yoksa coinTarget hiyerarşisinden bul.
            if (_resolvedMoneyText == null)
            {
                _resolvedMoneyText = coinTargetMoneyText;
                if (_resolvedMoneyText == null && coinTarget != null)
                    _resolvedMoneyText = coinTarget.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (_resolvedMoneyText != null)
                _resolvedMoneyText.text = GameManager.instance.money.ToString();
        }

        private void ResolveSceneReferences()
        {
            if (continueRewardText == null && continueButton != null)
                continueRewardText = continueButton.GetComponentInChildren<TextMeshProUGUI>(true);

            if (mainEmoji == null)
            {
                Transform emojiTransform = transform.Find("main-emoji");
                if (emojiTransform == null)
                    emojiTransform = transform.Find("MainEmoji");

                if (emojiTransform != null)
                    mainEmoji = emojiTransform as RectTransform;
            }
        }

        private void StartEmojiLoop()
        {
            if (mainEmoji == null)
                return;

            StopEmojiLoop();

            mainEmoji.localScale = Vector3.one;
            mainEmoji.localRotation = Quaternion.identity;

            Sequence sequence = DOTween.Sequence().SetUpdate(true);
            sequence.Append(mainEmoji.DOPunchScale(emojiPunchScale, emojiPulseDuration, vibrato: 1, elasticity: 0.6f));
            sequence.Join(mainEmoji.DOLocalRotate(emojiShineRotation, emojiShineDuration).SetEase(Ease.InOutSine));
            sequence.Append(mainEmoji.DOLocalRotate(Vector3.zero, emojiShineDuration).SetEase(Ease.InOutSine));
            sequence.SetLoops(-1, LoopType.Restart);
            _emojiLoopTween = sequence;
        }

        private void StopEmojiLoop()
        {
            if (_emojiLoopTween != null)
            {
                _emojiLoopTween.Kill();
                _emojiLoopTween = null;
            }

            if (mainEmoji != null)
            {
                mainEmoji.localScale = Vector3.one;
                mainEmoji.localRotation = Quaternion.identity;
            }
        }

        // ── Button helper ────────────────────────────────────────────────────────────

        private void SetContinueInteractable(bool value)
        {
            if (continueButton == null) return;
            Button btn = continueButton.GetComponent<Button>();
            if (btn == null) btn = continueButton.GetComponentInChildren<Button>(true);
            if (btn != null) btn.interactable = value;
        }

        // ── Camera stack (URP) ───────────────────────────────────────────────────────

        private void AddCameraToStack()
        {
            if (successCamera == null) return;
            Camera main = Camera.main;
            if (main == null) return;

            var overlayData = successCamera.GetUniversalAdditionalCameraData();
            overlayData.renderType = CameraRenderType.Overlay;

            var mainData = main.GetUniversalAdditionalCameraData();
            if (!mainData.cameraStack.Contains(successCamera))
                mainData.cameraStack.Add(successCamera);
        }

        private void RemoveCameraFromStack()
        {
            if (successCamera == null) return;
            Camera main = Camera.main;
            if (main == null) return;

            var mainData = main.GetUniversalAdditionalCameraData();
            mainData.cameraStack.Remove(successCamera);
        }

        private int ResolveRewardAmount()
        {
            if (overrideRewardAmount)
                return Mathf.Max(0, rewardAmount);

            int configuredReward = GameManager.instance != null && GameManager.instance.Data != null
                ? GameManager.instance.Data.levelCompleteReward
                : 0;

            if (configuredReward > 0)
                return configuredReward;

            return Mathf.Max(0, rewardAmount);
        }
    }
}
