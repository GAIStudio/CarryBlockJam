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
        [SerializeField] private float labelDecreaseVfxScale = 1f;
        [SerializeField] private float gateCompleteVfxScale = 1f;

        private Vector3 _goalLabelRestScale;
        private ParticleSystem _labelDecreaseParticles;
        private ParticleSystem _gateCompleteParticles;

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
            PlayLabelDecreaseVfx(fxColor);
            if (labelFinished)
            {
                PlayGateCompleteSfx();
                PlayGateCompleteVfx(fxColor);
            }

            return 1;
        }

        private void PlayGateCompleteSfx()
        {
            if (string.IsNullOrEmpty(gateCompleteSound) || SoundManager.instance == null)
                return;
            SoundManager.instance.PlayOneShot(gateCompleteSound);
        }

        private void PlayLabelDecreaseVfx(PieceColorType color)
        {
            EnsureLabelDecreaseParticles();
            if (_labelDecreaseParticles == null)
                return;

            ConfigureLabelDecreaseParticles(_labelDecreaseParticles);
            _labelDecreaseParticles.transform.position = GetGateVfxOrigin();
            ApplyParticleTint(_labelDecreaseParticles, ResolveVfxColor(color), brighten: 0.55f);
            _labelDecreaseParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _labelDecreaseParticles.Play(true);
        }

        private void PlayGateCompleteVfx(PieceColorType color)
        {
            EnsureGateCompleteParticles();
            if (_gateCompleteParticles == null)
                return;

            _gateCompleteParticles.transform.position = GetGateVfxOrigin();
            ApplyParticleTint(_gateCompleteParticles, ResolveVfxColor(color), brighten: 0.35f);
            _gateCompleteParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _gateCompleteParticles.Play(true);
        }

        /// <summary>
        /// World center of the gate mesh — never the CharTable or floating label.
        /// </summary>
        private Vector3 GetGateVfxOrigin()
        {
            Renderer bestRenderer = null;
            float bestVolume = -1f;
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

                Bounds bounds = renderer.bounds;
                float volume = bounds.size.x * bounds.size.y * bounds.size.z;
                if (volume <= bestVolume)
                    continue;

                bestVolume = volume;
                bestRenderer = renderer;
            }

            if (bestRenderer != null)
                return bestRenderer.bounds.center;

            return transform.position + Vector3.up * 0.4f;
        }

        private static Color ResolveVfxColor(PieceColorType color)
        {
            if (!PieceColorPalette.IsPaintable(color))
                return new Color(1f, 0.95f, 0.75f, 1f);
            Color tint = PieceColorPalette.GetColor(color);
            tint.a = 1f;
            return tint;
        }

        private void EnsureLabelDecreaseParticles()
        {
            if (_labelDecreaseParticles != null)
                return;

            var go = new GameObject("GateLabelDecreaseVfx");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            _labelDecreaseParticles = go.AddComponent<ParticleSystem>();
            ConfigureLabelDecreaseParticles(_labelDecreaseParticles);
        }

        private void EnsureGateCompleteParticles()
        {
            if (_gateCompleteParticles != null)
                return;

            var go = new GameObject("GateCompleteVfx");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            _gateCompleteParticles = go.AddComponent<ParticleSystem>();
            ConfigureGateCompleteParticles(_gateCompleteParticles);
        }

        private void ConfigureLabelDecreaseParticles(ParticleSystem particles)
        {
            if (particles == null)
                return;

            float scale = Mathf.Max(1f, labelDecreaseVfxScale);
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.65f;
            main.startLifetime = 0.85f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.9f * scale, 1.8f * scale);
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f * scale, 0.36f * scale);
            main.startColor = Color.white;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 90;
            main.gravityModifier = -0.06f;

            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, 40),
                new ParticleSystem.Burst(0.1f, 18),
            });

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.65f * scale;

            ConfigureSharedParticleStyle(particles);
        }

        private void ConfigureGateCompleteParticles(ParticleSystem particles)
        {
            if (particles == null)
                return;

            float scale = Mathf.Max(0.85f, gateCompleteVfxScale);
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.95f;
            main.startLifetime = 1.05f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.1f * scale, 2.3f * scale);
            main.startSize = new ParticleSystem.MinMaxCurve(0.14f * scale, 0.3f * scale);
            main.startColor = Color.white;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 120;
            main.gravityModifier = -0.04f;

            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, 52),
                new ParticleSystem.Burst(0.14f, 28),
            });

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.85f * scale;

            ConfigureSharedParticleStyle(particles);
        }

        private static void ConfigureSharedParticleStyle(ParticleSystem particles)
        {
            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f),
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.12f),
                    new GradientAlphaKey(0.75f, 0.5f),
                    new GradientAlphaKey(0f, 1f),
                });
            colorOverLifetime.color = gradient;

            var sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
                1f,
                new AnimationCurve(
                    new Keyframe(0f, 0.35f),
                    new Keyframe(0.2f, 1f),
                    new Keyframe(1f, 0.15f)));

            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            if (renderer == null)
                return;

            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            Shader particleShader =
                Shader.Find("Particles/Standard Unlit") ??
                Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                Shader.Find("Sprites/Default");
            if (particleShader != null)
                renderer.material = new Material(particleShader);
        }

        private static void ApplyParticleTint(
            ParticleSystem particles,
            Color tint,
            float brighten = 0.35f)
        {
            if (particles == null)
                return;

            Color soft = Color.Lerp(tint, Color.white, Mathf.Clamp01(brighten));
            soft.a = 1f;
            tint.a = 1f;
            var main = particles.main;
            main.startColor = new ParticleSystem.MinMaxGradient(tint, soft);
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
