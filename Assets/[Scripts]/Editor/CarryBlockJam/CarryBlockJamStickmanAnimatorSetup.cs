using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace CarryBlockJam.Editor
{
    /// <summary>
    /// Builds Stickman.controller from Mixamo clips (StandartWalk / CarryingIdle / CarryWalking).
    /// </summary>
    public static class CarryBlockJamStickmanAnimatorSetup
    {
        private const string ControllerPath = "Assets/[Animations]/Stickman.controller";
        private const string WalkPath = "Assets/[Animations]/Stickman@StandartWalk.fbx";
        private const string CarryIdlePath = "Assets/[Animations]/Stickman@CarryingIdle.fbx";
        private const string CarryWalkPath = "Assets/[Animations]/Stickman@CarryWalking.fbx";

        [InitializeOnLoadMethod]
        private static void AutoSetup()
        {
            if (Application.isPlaying)
                return;

            if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath) != null)
                return;

            EditorApplication.delayCall += () =>
            {
                if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath) != null)
                    return;
                Setup();
            };
        }

        [MenuItem("CarryBlockJam/Setup Stickman Animator Controller")]
        public static void Setup()
        {
            AnimationClip walk = LoadFirstClip(WalkPath);
            AnimationClip carryIdle = LoadFirstClip(CarryIdlePath);
            AnimationClip carryWalk = LoadFirstClip(CarryWalkPath);

            if (walk == null || carryIdle == null || carryWalk == null)
            {
                Debug.LogError(
                    "[CarryBlockJam] Missing Stickman animation clips. Expected:\n" +
                    WalkPath + "\n" + CarryIdlePath + "\n" + CarryWalkPath);
                return;
            }

            EnsureLoop(walk);
            EnsureLoop(carryIdle);
            EnsureLoop(carryWalk);

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

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            stateMachine.states = System.Array.Empty<ChildAnimatorState>();
            stateMachine.anyStateTransitions = System.Array.Empty<AnimatorStateTransition>();
            stateMachine.entryTransitions = System.Array.Empty<AnimatorTransition>();

            AnimatorState emptyIdle = stateMachine.AddState("EmptyIdle", new Vector3(200f, 0f, 0f));
            AnimatorState walkState = stateMachine.AddState("StandartWalk", new Vector3(450f, -80f, 0f));
            AnimatorState carryIdleState = stateMachine.AddState("CarryingIdle", new Vector3(450f, 80f, 0f));
            AnimatorState carryWalkState = stateMachine.AddState("CarryWalking", new Vector3(700f, 0f, 0f));

            walkState.motion = walk;
            carryIdleState.motion = carryIdle;
            carryWalkState.motion = carryWalk;

            stateMachine.defaultState = emptyIdle;

            AddBoolTransition(emptyIdle, walkState, moving: true, carrying: false);
            AddBoolTransition(emptyIdle, carryIdleState, moving: false, carrying: true);
            AddBoolTransition(emptyIdle, carryWalkState, moving: true, carrying: true);

            AddBoolTransition(walkState, emptyIdle, moving: false, carrying: false);
            AddBoolTransition(walkState, carryWalkState, moving: true, carrying: true);
            AddBoolTransition(walkState, carryIdleState, moving: false, carrying: true);

            AddBoolTransition(carryIdleState, emptyIdle, moving: false, carrying: false);
            AddBoolTransition(carryIdleState, carryWalkState, moving: true, carrying: true);
            AddBoolTransition(carryIdleState, walkState, moving: true, carrying: false);

            AddBoolTransition(carryWalkState, carryIdleState, moving: false, carrying: true);
            AddBoolTransition(carryWalkState, walkState, moving: true, carrying: false);
            AddBoolTransition(carryWalkState, emptyIdle, moving: false, carrying: false);

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

        private static void AddBoolTransition(
            AnimatorState from,
            AnimatorState to,
            bool moving,
            bool carrying)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = 0.12f;
            transition.offset = 0f;
            transition.AddCondition(
                moving ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
                0f,
                CarryBlockJamStickmanAnimator.MovingParam);
            transition.AddCondition(
                carrying ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
                0f,
                CarryBlockJamStickmanAnimator.CarryingParam);
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

        private static void EnsureLoop(AnimationClip clip)
        {
            if (clip == null)
                return;

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (settings.loopTime)
                return;

            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
        }
    }
}
