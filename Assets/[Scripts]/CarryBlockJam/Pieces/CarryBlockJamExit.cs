using System.Collections.Generic;
using DG.Tweening;
using GAITemplate;
using TMPro;
using UnityEngine;

namespace CarryBlockJam
{
    public class CarryBlockJamExit : MonoBehaviour
    {
        [SerializeField] private BoardBorderSide side;
        [SerializeField] private int startIndex;
        [SerializeField] private int length = 1;
        [SerializeField] private int row = -1;
        [SerializeField] private int column = -1;
        [SerializeField] private List<CarryBlockJamExitGoal> goals = new List<CarryBlockJamExitGoal>();
        [SerializeField] private int currentGoalIndex;
        [SerializeField] private int remainingPlateCount;
        [SerializeField] private GamePiece gateVisual;
        [SerializeField] private GamePiece carVisual;
        [SerializeField] private TMP_Text goalLabel;
        [SerializeField] private bool useArtGateMaterials;
        [SerializeField] private BoardBorderSide artGateSide = BoardBorderSide.Top;
        [SerializeField] private Vector3 modelScale = Vector3.one;
        [SerializeField] private float labelBounceDuration = 0.28f;
        [SerializeField] private float labelBounceScale = 1.35f;
        [SerializeField] private string gateCompleteSound = "SFX_Interact_Fabric";
        [Tooltip("Layer Lab fx_special_particle sparkle burst when a plate lands on the gate.")]
        [SerializeField] private GameObject plateDeliverVfxPrefab;
        [Tooltip("Star splash when the gate finishes its last plate (Particle VFX).")]
        [SerializeField] private GameObject gateCompleteVfxPrefab;
        [SerializeField] private float labelDecreaseVfxScale = 0.5f;
        [SerializeField] private float gateCompleteVfxScale = 0.32f;
        [SerializeField] private float plateDeliverVfxHeight = 0.85f;

        private const string PlateDeliverVfxResourcePath = "particles/GatePlateDeliverVfx";
        private const string GateCompleteVfxResourcePath = "particles/GateCompleteVfx";
        private const string SpecialParticleTextureResourcePath = "particles/fx_special_particle_white";
#if UNITY_EDITOR
        private const string SpecialParticleTextureEditorPath =
            "Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Particles/Texture/fx_special_particle_white.png";
#endif

        private Vector3 _goalLabelRestScale;
        private static GameObject _cachedPlateDeliverVfxPrefab;
        private static GameObject _cachedGateCompleteVfxPrefab;
        private static Texture2D _specialParticleTexture;
        private static Material _specialParticleMaterial;

        public BoardBorderSide Side => side;
        public int StartIndex => startIndex;
        public int Length => length;
        public int Row => row;
        public int Column => column;
        public Vector3 ModelScale => modelScale == Vector3.zero ? Vector3.one : modelScale;
        public PieceColorType CurrentColor =>
            currentGoalIndex >= 0 && currentGoalIndex < goals.Count
                ? goals[currentGoalIndex].color
                : PieceColorType.None;
        public int RemainingPlateCount => remainingPlateCount;
        public bool IsCompleted => currentGoalIndex >= goals.Count;

        public void Configure(CarryBlockJamExitDefinition definition)
        {
            if (definition == null)
                return;

            side = definition.side;
            startIndex = definition.startIndex;
            length = Mathf.Max(1, definition.length);
            row = definition.row;
            column = definition.column;
            modelScale = definition.modelScale == Vector3.zero ? Vector3.one : definition.modelScale;
            goals = new List<CarryBlockJamExitGoal>(definition.goals ?? new List<CarryBlockJamExitGoal>());
            currentGoalIndex = 0;
            remainingPlateCount = goals.Count > 0 ? Mathf.Max(0, goals[0].requiredPlateCount) : 0;
            RefreshVisuals();
        }

        public void BindVisuals(
            GamePiece gatePiece,
            GamePiece carPiece,
            TMP_Text label,
            BoardExitLabelSettings labelSettings = null)
        {
            useArtGateMaterials = false;
            gateVisual = gatePiece;
            carVisual = carPiece;
            goalLabel = label;

            if (goalLabel != null && labelSettings != null)
                CarryBlockJamExitLabelUtility.ApplyLabelSettings(goalLabel, labelSettings);
            else
                CarryBlockJamExitLabelUtility.ApplyRuntimeOutline(goalLabel, labelSettings);

            if (goalLabel != null && useArtGateMaterials)
                CarryBlockJamExitLabelUtility.ApplyArtGateLabelPlacement(goalLabel.transform, side, labelSettings);

            CaptureGoalLabelRestScale();
            RefreshVisuals();
        }

        public void BindArtGate(
            BoardBorderSide gateSide,
            TMP_Text label,
            BoardExitLabelSettings labelSettings = null)
        {
            useArtGateMaterials = true;
            artGateSide = gateSide;
            gateVisual = null;
            carVisual = null;
            goalLabel = label;

            if (goalLabel != null && labelSettings != null)
                CarryBlockJamExitLabelUtility.ApplyLabelSettings(goalLabel, labelSettings);
            else
                CarryBlockJamExitLabelUtility.ApplyRuntimeOutline(goalLabel, labelSettings);

            if (goalLabel != null)
                CarryBlockJamExitLabelUtility.ApplyArtGateLabelPlacement(goalLabel.transform, side, labelSettings);

            CaptureGoalLabelRestScale();
            RefreshVisuals();
        }

        // Legacy overload for older callers.
        public void BindArtGate(
            bool isUpGate,
            TMP_Text label,
            BoardExitLabelSettings labelSettings = null)
        {
            BindArtGate(isUpGate ? BoardBorderSide.Top : BoardBorderSide.Bottom, label, labelSettings);
        }

        public bool CanAccept(PieceColorType color) =>
            !IsCompleted && color != PieceColorType.None && color == CurrentColor;

        public int GetAcceptablePlateCount(PieceColorType color)
        {
            if (!CanAccept(color))
                return 0;

            return Mathf.Max(0, remainingPlateCount);
        }

        /// <summary>
        /// Consumes up to <paramref name="plateCount"/> plates at once (updates label once).
        /// Prefer <see cref="ConsumeOne"/> when animating deliveries one-by-one.
        /// </summary>
        public int Consume(PieceColorType color, int plateCount)
        {
            if (!CanAccept(color) || plateCount <= 0)
                return 0;

            int consumed = 0;
            while (consumed < plateCount && CanAccept(color))
            {
                if (ConsumeOne(color) <= 0)
                    break;
                consumed++;
            }

            return consumed;
        }

        /// <summary>
        /// Consumes a single matching plate and refreshes the goal label (x3 → x2 → x1 → hide).
        /// </summary>
        public int ConsumeOne(PieceColorType color)
        {
            if (!CanAccept(color) || remainingPlateCount <= 0)
                return 0;

            PieceColorType fxColor = color;
            remainingPlateCount--;
            bool labelFinished = false;
            if (remainingPlateCount == 0)
            {
                AdvanceGoal();
                labelFinished = IsCompleted;
            }
            else
            {
                RefreshVisuals();
            }

            AnimateGoalLabelChange();
            PlayPlateDeliveryVfx(fxColor);
            if (labelFinished)
            {
                PlayGateCompleteSfx();
                PlayGateCompleteVfx();
            }

            return 1;
        }

        private void PlayGateCompleteSfx()
        {
            if (string.IsNullOrEmpty(gateCompleteSound) || SoundManager.instance == null)
                return;
            SoundManager.instance.PlayOneShot(gateCompleteSound);
        }

        /// <summary>
        /// Plays the per-plate gate delivery burst at the gate mesh (not CharTable).
        /// Uses Layer Lab <c>fx_special_particle</c> via a runtime one-shot system.
        /// </summary>
        public void PlayPlateDeliveryVfx(PieceColorType color)
        {
            float scale = Mathf.Clamp(labelDecreaseVfxScale, 0.35f, 0.7f);
            SpawnSpecialParticleBurst(
                GetGateVfxOrigin(plateDeliverVfxHeight),
                scale,
                ResolveLabelParticleColor(color));
        }

        private void PlayGateCompleteVfx()
        {
            GameObject prefab = ResolveGateCompleteVfxPrefab();
            if (prefab == null)
                return;

            float scale = Mathf.Clamp(gateCompleteVfxScale, 0.12f, 0.4f);
            SpawnGateVfx(
                prefab,
                GetGateVfxOrigin(0.55f),
                scale,
                Color.white,
                particleCountMultiplier: 2.8f,
                particleSizeMultiplier: 0.7f,
                preserveAuthoring: false,
                configureSpecialParticleBurst: false,
                stripSplashBubbles: true);
        }

        private GameObject ResolveGateCompleteVfxPrefab()
        {
            if (gateCompleteVfxPrefab != null)
                return gateCompleteVfxPrefab;

            if (_cachedGateCompleteVfxPrefab == null)
                _cachedGateCompleteVfxPrefab = Resources.Load<GameObject>(GateCompleteVfxResourcePath);

            return _cachedGateCompleteVfxPrefab;
        }

        private void SpawnSpecialParticleBurst(Vector3 worldPosition, float scale, Color tint)
        {
            Material shared = ResolveSpecialParticleMaterial();
            if (shared == null)
                return;

            tint.a = 1f;
            // Keep material white so particle startColor carries the label tint cleanly.
            Material material = new Material(shared)
            {
                name = "GateFxSpecialParticleMat_Tinted",
                hideFlags = HideFlags.HideAndDontSave,
                color = Color.white
            };
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", Color.white);
            if (material.HasProperty("_TintColor"))
                material.SetColor("_TintColor", new Color(1f, 1f, 1f, 1f));
            // Multiply color mode: vertex/particle color * texture.
            if (material.HasProperty("_ColorMode"))
                material.SetFloat("_ColorMode", 0f);

            var go = new GameObject("GatePlateDeliverVfx");
            go.transform.position = worldPosition;
            go.transform.rotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            ParticleSystem particles = go.AddComponent<ParticleSystem>();
            // AddComponent starts Play On Awake — stop before editing duration/modules.
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.Clear(true);
            ConfigureSpecialParticleBurst(particles, tint, Mathf.Max(0.35f, scale));

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.alignment = ParticleSystemRenderSpace.View;
                renderer.sharedMaterial = material;
                renderer.sortingOrder = 120;
                renderer.maxParticleSize = 5f;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            particles.Play(true);
            // Destroy GO first; material is cleaned with a short delay after particles end.
            Object.Destroy(go, 2.1f);
            Object.Destroy(material, 2.3f);
        }

        private static Material ResolveSpecialParticleMaterial()
        {
            if (_specialParticleMaterial != null)
                return _specialParticleMaterial;

            Texture2D texture = ResolveSpecialParticleTexture();
            if (texture == null)
                return null;

            Shader particleShader =
                Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                Shader.Find("Particles/Standard Unlit") ??
                Shader.Find("Legacy Shaders/Particles/Additive") ??
                Shader.Find("Mobile/Particles/Additive") ??
                Shader.Find("Sprites/Default");
            if (particleShader == null)
                return null;

            _specialParticleMaterial = new Material(particleShader)
            {
                name = "GateFxSpecialParticleMat",
                hideFlags = HideFlags.HideAndDontSave,
                mainTexture = texture,
                color = Color.white
            };

            if (_specialParticleMaterial.HasProperty("_BaseMap"))
                _specialParticleMaterial.SetTexture("_BaseMap", texture);
            if (_specialParticleMaterial.HasProperty("_MainTex"))
                _specialParticleMaterial.SetTexture("_MainTex", texture);

            Color bright = Color.white;
            if (_specialParticleMaterial.HasProperty("_BaseColor"))
                _specialParticleMaterial.SetColor("_BaseColor", bright);
            if (_specialParticleMaterial.HasProperty("_Color"))
                _specialParticleMaterial.SetColor("_Color", bright);

            if (_specialParticleMaterial.HasProperty("_Surface"))
                _specialParticleMaterial.SetFloat("_Surface", 1f);
            if (_specialParticleMaterial.HasProperty("_Blend"))
                _specialParticleMaterial.SetFloat("_Blend", 1f);
            if (_specialParticleMaterial.HasProperty("_SrcBlend"))
                _specialParticleMaterial.SetFloat(
                    "_SrcBlend",
                    (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (_specialParticleMaterial.HasProperty("_DstBlend"))
                _specialParticleMaterial.SetFloat(
                    "_DstBlend",
                    (float)UnityEngine.Rendering.BlendMode.One);
            if (_specialParticleMaterial.HasProperty("_ZWrite"))
                _specialParticleMaterial.SetFloat("_ZWrite", 0f);

            _specialParticleMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            _specialParticleMaterial.EnableKeyword("_BLENDMODE_ADD");
            _specialParticleMaterial.SetOverrideTag("RenderType", "Transparent");
            _specialParticleMaterial.renderQueue = 3000;
            return _specialParticleMaterial;
        }

        private static Texture2D ResolveSpecialParticleTexture()
        {
            if (_specialParticleTexture != null)
                return _specialParticleTexture;

            _specialParticleTexture = Resources.Load<Texture2D>(SpecialParticleTextureResourcePath);
#if UNITY_EDITOR
            if (_specialParticleTexture == null)
            {
                _specialParticleTexture =
                    UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(SpecialParticleTextureEditorPath);
            }
#endif
            return _specialParticleTexture;
        }

        private static void SpawnGateVfx(
            GameObject prefab,
            Vector3 worldPosition,
            float scale,
            Color tint,
            float particleCountMultiplier = 1f,
            float particleSizeMultiplier = 1f,
            bool preserveAuthoring = false,
            bool configureSpecialParticleBurst = false,
            bool stripSplashBubbles = false)
        {
            if (prefab == null)
                return;

            // Inactive first so package AudioSources cannot Play On Awake.
            GameObject instance = Object.Instantiate(prefab);
            instance.name = prefab.name;
            instance.SetActive(false);
            instance.transform.position = worldPosition;
            instance.transform.rotation = Quaternion.identity;
            float clampedScale = Mathf.Max(0.05f, scale);
            instance.transform.localScale = Vector3.one * clampedScale;

            StripParticleAudio(instance);
            if (stripSplashBubbles)
                StripSplashBubbleEmitters(instance);

            ParticleSystem[] systems = instance.GetComponentsInChildren<ParticleSystem>(true);
            float destroyAfter = 2.5f;
            float countMul = Mathf.Max(1f, particleCountMultiplier);
            float sizeMul = Mathf.Clamp(particleSizeMultiplier, 0.2f, 2f);

            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem particles = systems[i];
                if (particles == null)
                    continue;
                if (stripSplashBubbles && IsSplashBubbleEmitter(particles.transform))
                    continue;

                if (configureSpecialParticleBurst)
                {
                    ConfigureSpecialParticleBurst(particles, tint);
                }
                else if (!preserveAuthoring)
                {
                    ApplyParticleTint(particles, tint, brighten: 0.2f);
                    BoostParticleDensity(particles, countMul, sizeMul);

                    var main = particles.main;
                    main.playOnAwake = false;
                    main.simulationSpace = ParticleSystemSimulationSpace.World;
                    if (main.loop)
                        main.loop = false;

                    ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
                    if (renderer != null)
                    {
                        renderer.sortingOrder = 80;
                        renderer.maxParticleSize = 5f;
                    }
                }
                else
                {
                    ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
                    if (renderer != null)
                    {
                        renderer.sortingOrder = 80;
                        renderer.maxParticleSize = 5f;
                    }
                }

                var lifetimeMain = particles.main;
                float lifetime = lifetimeMain.duration;
                if (lifetimeMain.startLifetime.mode == ParticleSystemCurveMode.Constant)
                    lifetime += lifetimeMain.startLifetime.constantMax;
                else if (lifetimeMain.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
                    lifetime += lifetimeMain.startLifetime.constantMax;
                else
                    lifetime += 1.25f;

                destroyAfter = Mathf.Max(destroyAfter, lifetime + 0.35f);
            }

            instance.SetActive(true);

            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] == null)
                    continue;
                if (stripSplashBubbles && IsSplashBubbleEmitter(systems[i].transform))
                    continue;
                systems[i].Clear(true);
                systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] == null)
                    continue;
                if (stripSplashBubbles && IsSplashBubbleEmitter(systems[i].transform))
                    continue;
                systems[i].Play(true);
            }

            Object.Destroy(instance, destroyAfter);
        }

        /// <summary>
        /// One-shot burst using Layer Lab <c>fx_special_particle</c> sparkle materials.
        /// </summary>
        private static void ConfigureSpecialParticleBurst(ParticleSystem particles, Color tint, float scale = 0.5f)
        {
            if (particles == null)
                return;

            // Duration/loop can only be edited while fully stopped.
            if (particles.isPlaying || particles.particleCount > 0)
            {
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                particles.Clear(true);
            }

            float s = Mathf.Clamp(scale, 0.35f, 0.85f);
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.7f;
            main.startDelay = 0f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 1.05f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.4f * s, 5.2f * s);
            main.startSize = new ParticleSystem.MinMaxCurve(0.32f * s, 0.7f * s);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 120;
            main.gravityModifier = -0.04f;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            // Match gate label color exactly (no white wash).
            tint.a = 1f;
            Color highlight = Color.Lerp(tint, Color.white, 0.18f);
            highlight.a = 1f;
            main.startColor = new ParticleSystem.MinMaxGradient(tint, highlight);

            var emission = particles.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, 42),
                new ParticleSystem.Burst(0.05f, 28),
            });

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.55f * s;

            var velocityOverLifetime = particles.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
            velocityOverLifetime.speedModifier = new ParticleSystem.MinMaxCurve(
                1f,
                new AnimationCurve(
                    new Keyframe(0f, 1.15f),
                    new Keyframe(0.35f, 1f),
                    new Keyframe(1f, 0.35f)));

            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(tint, 0f),
                    new GradientColorKey(highlight, 0.35f),
                    new GradientColorKey(tint, 1f),
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.08f),
                    new GradientAlphaKey(1f, 0.45f),
                    new GradientAlphaKey(0f, 1f),
                });
            colorOverLifetime.color = gradient;

            var sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
                1f,
                new AnimationCurve(
                    new Keyframe(0f, 0.4f),
                    new Keyframe(0.2f, 1f),
                    new Keyframe(1f, 0.15f)));
        }

        private static void StripParticleAudio(GameObject root)
        {
            if (root == null)
                return;

            AudioSource[] audioSources = root.GetComponentsInChildren<AudioSource>(true);
            for (int i = 0; i < audioSources.Length; i++)
            {
                AudioSource source = audioSources[i];
                if (source == null)
                    continue;

                source.Stop();
                source.playOnAwake = false;
                source.enabled = false;
                Object.Destroy(source);
            }
        }

        /// <summary>
        /// Removes soft bubble/splash emitters from star finish FX, keeping Stars.
        /// </summary>
        private static void StripSplashBubbleEmitters(GameObject root)
        {
            if (root == null)
                return;

            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform child = transforms[i];
                if (child == null || child == root.transform)
                    continue;
                if (!IsSplashBubbleEmitter(child))
                    continue;

                ParticleSystem particles = child.GetComponent<ParticleSystem>();
                if (particles != null)
                {
                    particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    particles.Clear(true);
                }

                child.gameObject.SetActive(false);
            }
        }

        private static bool IsSplashBubbleEmitter(Transform target)
        {
            if (target == null)
                return false;

            string name = target.name;
            // Keep star emitters and the Star_Splash root; only remove soft Splash/bubble children.
            if (name.IndexOf("Star", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return false;

            return name.IndexOf("Splash", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("Bubble", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.Equals("Circle", System.StringComparison.OrdinalIgnoreCase) ||
                   name.IndexOf("Ring", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("Orb", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void BoostParticleDensity(
            ParticleSystem particles,
            float countMultiplier,
            float sizeMultiplier)
        {
            if (particles == null)
                return;

            var main = particles.main;
            main.maxParticles = Mathf.Clamp(
                Mathf.CeilToInt(main.maxParticles * countMultiplier),
                1,
                5000);

            if (!Mathf.Approximately(sizeMultiplier, 1f))
            {
                main.startSize = ScaleMinMaxCurve(main.startSize, sizeMultiplier);
                main.startSizeX = ScaleMinMaxCurve(main.startSizeX, sizeMultiplier);
                main.startSizeY = ScaleMinMaxCurve(main.startSizeY, sizeMultiplier);
                main.startSizeZ = ScaleMinMaxCurve(main.startSizeZ, sizeMultiplier);
            }

            var emission = particles.emission;
            if (!emission.enabled)
                return;

            if (emission.rateOverTime.mode == ParticleSystemCurveMode.Constant ||
                emission.rateOverTime.mode == ParticleSystemCurveMode.TwoConstants)
            {
                emission.rateOverTime = ScaleMinMaxCurve(emission.rateOverTime, countMultiplier);
            }

            int burstCount = emission.burstCount;
            if (burstCount <= 0)
                return;

            var bursts = new ParticleSystem.Burst[burstCount];
            emission.GetBursts(bursts);
            for (int i = 0; i < bursts.Length; i++)
            {
                ParticleSystem.Burst burst = bursts[i];
                burst.count = ScaleMinMaxCurve(burst.count, countMultiplier);
                bursts[i] = burst;
            }

            emission.SetBursts(bursts);
        }

        private static ParticleSystem.MinMaxCurve ScaleMinMaxCurve(
            ParticleSystem.MinMaxCurve curve,
            float multiplier)
        {
            switch (curve.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    curve.constant *= multiplier;
                    break;
                case ParticleSystemCurveMode.TwoConstants:
                    curve.constantMin *= multiplier;
                    curve.constantMax *= multiplier;
                    break;
                case ParticleSystemCurveMode.Curve:
                case ParticleSystemCurveMode.TwoCurves:
                    curve.curveMultiplier *= multiplier;
                    break;
            }

            return curve;
        }

        /// <summary>
        /// World point above the gate mesh — never CharTable, stickman, plates, or the floating label.
        /// </summary>
        private Vector3 GetGateVfxOrigin(float upwardOffset = 0.55f)
        {
            Renderer bestRenderer = null;
            float bestVolume = -1f;
            Renderer preferredGateRenderer = null;
            float preferredGateVolume = -1f;
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null ||
                    !renderer.enabled ||
                    renderer is ParticleSystemRenderer ||
                    renderer.GetComponent<TMP_Text>() != null)
                    continue;

                if (goalLabel != null &&
                    (renderer.transform == goalLabel.transform ||
                     renderer.transform.IsChildOf(goalLabel.transform)))
                    continue;

                if (IsExcludedFromGateVfxOrigin(renderer.transform))
                    continue;

                Bounds bounds = renderer.bounds;
                float volume = bounds.size.x * bounds.size.y * bounds.size.z;
                if (volume > bestVolume)
                {
                    bestVolume = volume;
                    bestRenderer = renderer;
                }

                if (IsLikelyGateMesh(renderer.transform) && volume > preferredGateVolume)
                {
                    preferredGateVolume = volume;
                    preferredGateRenderer = renderer;
                }
            }

            Renderer chosen = preferredGateRenderer != null ? preferredGateRenderer : bestRenderer;
            if (chosen != null)
            {
                Bounds bounds = chosen.bounds;
                return new Vector3(
                    bounds.center.x,
                    bounds.max.y + Mathf.Max(0.15f, upwardOffset),
                    bounds.center.z);
            }

            return transform.position + Vector3.up * Mathf.Max(0.4f, upwardOffset);
        }

        private static bool IsExcludedFromGateVfxOrigin(Transform target)
        {
            Transform current = target;
            while (current != null)
            {
                string name = current.name;
                if (name.IndexOf("CharTable", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Stickman", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Cylinder", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Plate", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Trail", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

                current = current.parent;
            }

            return false;
        }

        private static bool IsLikelyGateMesh(Transform target)
        {
            Transform current = target;
            while (current != null)
            {
                string name = current.name;
                if (name.IndexOf("Gate", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("M_Gate", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Exit", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

                current = current.parent;
            }

            return false;
        }

        /// <summary>
        /// Same tint path as the goal label text in <see cref="RefreshVisuals"/>.
        /// </summary>
        private static Color ResolveLabelParticleColor(PieceColorType color)
        {
            PieceColorType labelColor = PieceColorPalette.IsPaintable(color)
                ? color
                : PieceColorType.White;
            Color tint = PieceColorPalette.GetColor(labelColor);
            tint.a = 1f;
            return tint;
        }

        private static void ApplyParticleTint(
            ParticleSystem particles,
            Color tint,
            float brighten = 0.35f)
        {
            if (particles == null)
                return;

            tint.a = 1f;
            float wash = Mathf.Clamp01(brighten);
            Color soft = wash <= 0f ? tint : Color.Lerp(tint, Color.white, wash);
            soft.a = 1f;
            var main = particles.main;
            main.startColor = wash <= 0f
                ? new ParticleSystem.MinMaxGradient(tint)
                : new ParticleSystem.MinMaxGradient(tint, soft);
        }

        private void AdvanceGoal()
        {
            currentGoalIndex++;
            if (currentGoalIndex >= goals.Count)
            {
                remainingPlateCount = 0;
                RefreshVisuals();
                return;
            }

            remainingPlateCount = Mathf.Max(0, goals[currentGoalIndex].requiredPlateCount);
            RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            if (useArtGateMaterials)
            {
                BoardBorderSide gateSide = CarryBlockJamArtGateUtility.ResolveGateSide(transform);
                artGateSide = gateSide;
                CarryBlockJamArtGateUtility.ApplyGateColor(
                    transform,
                    gateSide,
                    PieceColorPalette.IsPaintable(CurrentColor) ? CurrentColor : PieceColorType.Grey);
            }
            else if (gateVisual != null)
            {
                gateVisual.ApplyColor(
                    PieceColorPalette.IsPaintable(CurrentColor) ? CurrentColor : PieceColorType.Grey);
            }

            if (carVisual != null && PieceColorPalette.IsPaintable(CurrentColor))
                carVisual.ApplyColor(CurrentColor);

            if (goalLabel != null)
            {
                goalLabel.text = IsCompleted ? string.Empty : $"x{remainingPlateCount}";
                goalLabel.gameObject.SetActive(!IsCompleted);

                PieceColorType labelColor = PieceColorPalette.IsPaintable(CurrentColor)
                    ? CurrentColor
                    : PieceColorType.White;
                // Match the logical goal color. Sampling gate materials can diverge
                // because toon gate mats store tint in textures / non-_Color props.
                Color goalTint = PieceColorPalette.GetColor(labelColor);
                goalLabel.color = goalTint;

                // TMP face color multiplies vertex color; keep face white so a shared
                // font material cannot leave a stale blue/green tint on the label.
                Material fontMaterial = goalLabel.fontMaterial;
                if (fontMaterial != null && fontMaterial.HasProperty(ShaderUtilities.ID_FaceColor))
                    fontMaterial.SetColor(ShaderUtilities.ID_FaceColor, Color.white);

                if (goalLabel.font != null && goalLabel.font.material != null)
                    goalLabel.ForceMeshUpdate(true);
            }
        }

        public void ApplyLabelPresentation(BoardExitLabelSettings labelSettings)
        {
            if (goalLabel == null)
                return;

            if (useArtGateMaterials)
            {
                CarryBlockJamExitLabelUtility.ApplyArtGateLabelPlacement(
                    goalLabel.transform,
                    side,
                    labelSettings);
            }
            else if (labelSettings != null)
            {
                CarryBlockJamExitLabelUtility.ApplyLabelTransform(
                    goalLabel.transform,
                    side,
                    labelSettings);
            }
            else
            {
                CarryBlockJamExitLabelUtility.ApplyLabelSettings(goalLabel, labelSettings);
            }

            CaptureGoalLabelRestScale();
            RefreshVisuals();
        }

        private void CaptureGoalLabelRestScale()
        {
            if (goalLabel != null)
                _goalLabelRestScale = goalLabel.transform.localScale;
        }

        private void AnimateGoalLabelChange()
        {
            if (goalLabel == null || !goalLabel.gameObject.activeInHierarchy)
                return;

            Transform labelTransform = goalLabel.transform;
            if (_goalLabelRestScale == Vector3.zero)
                _goalLabelRestScale = labelTransform.localScale;

            labelTransform.DOKill();
            labelTransform.localScale = _goalLabelRestScale;

            float duration = Mathf.Max(0.01f, labelBounceDuration);
            Vector3 enlargedScale = _goalLabelRestScale * Mathf.Max(1f, labelBounceScale);
            Sequence bounce = DOTween.Sequence().SetTarget(labelTransform);
            bounce.Append(labelTransform.DOScale(enlargedScale, duration * 0.4f).SetEase(Ease.OutBack));
            bounce.Append(labelTransform.DOScale(_goalLabelRestScale, duration * 0.6f).SetEase(Ease.OutBounce));
        }
    }
}
