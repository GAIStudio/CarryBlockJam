using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace CarryBlockJam.Editor
{
    /// <summary>
    /// Imports the requested Mixamo clips and builds the Stickman animator controller.
    /// </summary>
    public static class CarryBlockJamStickmanAnimatorSetup
    {
        private const string ControllerPath = "Assets/[Animations]/Stickman.controller";
        private const string StickmanModelPath = "Assets/[Models]/Stickman.fbx";
        private const string IdlePath = "Assets/[Animations]/Idle.fbx";
        private const string SlowRunPath = "Assets/[Animations]/Slow Run.fbx";
        private const string BoxIdlePath = "Assets/[Animations]/Box Idle.fbx";
        private const string BoxWalkPath = "Assets/[Animations]/Box Walk Arc.fbx";
        private const string FallingDownPath = "Assets/[Animations]/Falling Down.fbx";

        [InitializeOnLoadMethod]
        private static void AutoSetup()
        {
            if (Application.isPlaying)
            {
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                return;
            }

            if (IsControllerCurrent())
                return;

            EditorApplication.delayCall += () =>
            {
                if (IsControllerCurrent())
                    return;
                Setup();
            };
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode)
                return;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            AutoSetup();
        }

        [MenuItem("CarryBlockJam/Setup Stickman Animator Controller")]
        public static void Setup()
        {
            ConfigureHumanoidClip(IdlePath, loop: true);
            ConfigureHumanoidClip(SlowRunPath, loop: true);
            ConfigureHumanoidClip(BoxIdlePath, loop: true);
            ConfigureHumanoidClip(BoxWalkPath, loop: true);
            ConfigureHumanoidClip(FallingDownPath, loop: false);

            AnimationClip idle = LoadFirstClip(IdlePath);
            AnimationClip slowRun = LoadFirstClip(SlowRunPath);
            AnimationClip boxIdle = LoadFirstClip(BoxIdlePath);
            AnimationClip boxWalk = LoadFirstClip(BoxWalkPath);
            AnimationClip fallingDown = LoadFirstClip(FallingDownPath);

            if (idle == null || slowRun == null || boxIdle == null ||
                boxWalk == null || fallingDown == null)
            {
                Debug.LogError(
                    "[CarryBlockJam] Missing Stickman animation clips. Expected:\n" +
                    IdlePath + "\n" + SlowRunPath + "\n" + BoxIdlePath + "\n" +
                    BoxWalkPath + "\n" + FallingDownPath);
                return;
            }

            EnsureLoopAndFeetBake(idle, loop: true);
            EnsureLoopAndFeetBake(slowRun, loop: true);
            EnsureLoopAndFeetBake(boxIdle, loop: true);
            EnsureLoopAndFeetBake(boxWalk, loop: true);
            EnsureLoopAndFeetBake(fallingDown, loop: false);

            string folder = Path.GetDirectoryName(ControllerPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder(folder))
                Directory.CreateDirectory(folder);

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }
            else
            {
                while (controller.layers.Length > 0)
                    controller.RemoveLayer(0);
                controller.AddLayer("Base Layer");
            }

            controller.parameters = System.Array.Empty<AnimatorControllerParameter>();
            controller.AddParameter(CarryBlockJamStickmanAnimator.MovingParam, AnimatorControllerParameterType.Bool);
            controller.AddParameter(CarryBlockJamStickmanAnimator.CarryingParam, AnimatorControllerParameterType.Bool);
            controller.AddParameter(CarryBlockJamStickmanAnimator.FailedParam, AnimatorControllerParameterType.Bool);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            stateMachine.states = System.Array.Empty<ChildAnimatorState>();
            stateMachine.anyStateTransitions = System.Array.Empty<AnimatorStateTransition>();
            stateMachine.entryTransitions = System.Array.Empty<AnimatorTransition>();

            AnimatorState idleState = stateMachine.AddState(
                CarryBlockJamStickmanAnimator.EmptyIdleStateName,
                new Vector3(200f, 0f, 0f));
            AnimatorState slowRunState = stateMachine.AddState("SlowRun", new Vector3(450f, -80f, 0f));
            AnimatorState boxIdleState = stateMachine.AddState("BoxIdle", new Vector3(450f, 80f, 0f));
            AnimatorState boxWalkState = stateMachine.AddState("BoxWalkArc", new Vector3(700f, 0f, 0f));
            AnimatorState fallingState = stateMachine.AddState(
                CarryBlockJamStickmanAnimator.FailedStateName,
                new Vector3(450f, 220f, 0f));

            idleState.motion = idle;
            slowRunState.motion = slowRun;
            boxIdleState.motion = boxIdle;
            boxWalkState.motion = boxWalk;
            fallingState.motion = fallingDown;

            stateMachine.defaultState = idleState;

            AddBoolTransition(idleState, slowRunState, moving: true, carrying: false);
            AddBoolTransition(idleState, boxIdleState, moving: false, carrying: true);
            AddBoolTransition(idleState, boxWalkState, moving: true, carrying: true);

            AddBoolTransition(slowRunState, idleState, moving: false, carrying: false);
            AddBoolTransition(slowRunState, boxWalkState, moving: true, carrying: true);
            AddBoolTransition(slowRunState, boxIdleState, moving: false, carrying: true);

            AddBoolTransition(boxIdleState, idleState, moving: false, carrying: false);
            AddBoolTransition(boxIdleState, boxWalkState, moving: true, carrying: true);
            AddBoolTransition(boxIdleState, slowRunState, moving: true, carrying: false);

            AddBoolTransition(boxWalkState, boxIdleState, moving: false, carrying: true);
            AddBoolTransition(boxWalkState, slowRunState, moving: true, carrying: false);
            AddBoolTransition(boxWalkState, idleState, moving: false, carrying: false);

            AnimatorStateTransition failTransition = stateMachine.AddAnyStateTransition(fallingState);
            failTransition.hasExitTime = false;
            failTransition.hasFixedDuration = true;
            failTransition.duration = 0.1f;
            failTransition.canTransitionToSelf = false;
            failTransition.AddCondition(
                AnimatorConditionMode.If,
                0f,
                CarryBlockJamStickmanAnimator.FailedParam);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            const string resourcesFolder = "Assets/Resources";
            const string resourcesCopy = "Assets/Resources/Stickman.controller";
            if (!AssetDatabase.IsValidFolder(resourcesFolder))
                AssetDatabase.CreateFolder("Assets", "Resources");

            if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(resourcesCopy) != null)
                AssetDatabase.DeleteAsset(resourcesCopy);

            AssetDatabase.CopyAsset(ControllerPath, resourcesCopy);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CarryBlockJam] Stickman animator controller ready at " + ControllerPath);
        }

        public static void SetupBatch()
        {
            Setup();
        }

        [MenuItem("CarryBlockJam/Bake Stickman Clip Feet Height")]
        public static void BakeFeetHeightMenu()
        {
            ConfigureHumanoidClip(IdlePath, loop: true);
            ConfigureHumanoidClip(SlowRunPath, loop: true);
            ConfigureHumanoidClip(BoxIdlePath, loop: true);
            ConfigureHumanoidClip(BoxWalkPath, loop: true);
            ConfigureHumanoidClip(FallingDownPath, loop: false);
            AssetDatabase.SaveAssets();
            Debug.Log("[CarryBlockJam] Baked Root Transform Position Y (Feet) on Stickman clips.");
        }

        public static void BakeFeetHeightBatch()
        {
            BakeFeetHeightMenu();
        }

        private static void AddBoolTransition(
            AnimatorState from,
            AnimatorState to,
            bool moving,
            bool carrying)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = 0.25f;
            transition.offset = 0f;
            transition.AddCondition(
                moving ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
                0f,
                CarryBlockJamStickmanAnimator.MovingParam);
            transition.AddCondition(
                carrying ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
                0f,
                CarryBlockJamStickmanAnimator.CarryingParam);
            transition.AddCondition(
                AnimatorConditionMode.IfNot,
                0f,
                CarryBlockJamStickmanAnimator.FailedParam);
        }

        private static bool IsControllerCurrent()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null || controller.layers.Length == 0)
                return false;

            ChildAnimatorState[] states = controller.layers[0].stateMachine.states;
            bool hasCurrentIdle = false;
            bool hasFailure = false;
            for (int i = 0; i < states.Length; i++)
            {
                AnimatorState state = states[i].state;
                if (state == null)
                    continue;

                if (state.name == CarryBlockJamStickmanAnimator.EmptyIdleStateName)
                {
                    string motionPath = state.motion != null
                        ? AssetDatabase.GetAssetPath(state.motion)
                        : string.Empty;
                    hasCurrentIdle = motionPath == IdlePath;
                }

                if (state.name == CarryBlockJamStickmanAnimator.FailedStateName)
                    hasFailure = true;
            }

            return hasCurrentIdle && hasFailure;
        }

        private static AnimationClip LoadFirstClip(string assetPath)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (assets == null)
                return null;

            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    return clip;
            }

            return null;
        }

        private static void EnsureLoopAndFeetBake(AnimationClip clip, bool loop)
        {
            if (clip == null)
                return;

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            // Match avatar foot height across clips (prevents stand↔walk hop).
            settings.keepOriginalPositionY = false;
            settings.heightFromFeet = true;
            settings.keepOriginalPositionXZ = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
        }

        private static void ConfigureHumanoidClip(string assetPath, bool loop)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogWarning("[CarryBlockJam] Missing ModelImporter for " + assetPath);
                return;
            }

            Avatar sourceAvatar = LoadStickmanAvatar();
            importer.animationType = ModelImporterAnimationType.Human;
            if (sourceAvatar != null)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                importer.sourceAvatar = sourceAvatar;
            }

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            BakeFeetHeightOnClip(assetPath, loop);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            EnsureLoopAndFeetBake(LoadFirstClip(assetPath), loop);
        }

        private static Avatar LoadStickmanAvatar()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(StickmanModelPath);
            if (assets == null)
                return null;

            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Avatar avatar)
                    return avatar;
            }

            return null;
        }

        private static void BakeFeetHeightOnClip(string assetPath, bool loop)
        {
            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogWarning("[CarryBlockJam] Missing ModelImporter for " + assetPath);
                return;
            }

            // Prefer defaultClipAnimations so Unity fills take/frame data correctly.
            // Hand-written clipAnimations in .meta break the ModelImporter inspector
            // (MaskFromClip / array out of bounds).
            ModelImporterClipAnimation[] defaults = importer.defaultClipAnimations;
            if (defaults == null || defaults.Length == 0)
            {
                Debug.LogWarning("[CarryBlockJam] No default clip animations on " + assetPath);
                return;
            }

            ModelImporterClipAnimation[] clips = new ModelImporterClipAnimation[defaults.Length];
            for (int i = 0; i < defaults.Length; i++)
            {
                clips[i] = defaults[i];
                clips[i].loopTime = loop;
                clips[i].lockRootHeightY = true;
                clips[i].keepOriginalPositionY = false;
                clips[i].heightFromFeet = true;
                clips[i].keepOriginalPositionXZ = true;
                clips[i].lockRootPositionXZ = false;
                clips[i].lockRootRotation = true;
                clips[i].keepOriginalOrientation = false;
            }

            importer.clipAnimations = clips;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }
    }
}
