using UnityEngine;

namespace CarryBlockJam
{
    /// <summary>
    /// Drives Stickman Animator states: empty walk, carrying idle, carry walk.
    /// Also keeps Visual Y synced to clip/bind height so empty stop does not hop.
    /// </summary>
    [DisallowMultipleComponent]
    public class CarryBlockJamStickmanAnimator : MonoBehaviour
    {
        public const string MovingParam = "IsMoving";
        public const string CarryingParam = "IsCarrying";
        public const string ControllerAssetPath = "Assets/[Animations]/Stickman.controller";
        public const string EmptyIdleStateName = "EmptyIdle";

        [SerializeField] private Animator animator;
        [SerializeField] private Vector3 animatedHeightOffset = new Vector3(0f, -0.7f, 0f);

        private static readonly int MovingId = Animator.StringToHash(MovingParam);
        private static readonly int CarryingId = Animator.StringToHash(CarryingParam);

        private bool _isMoving;
        private bool _isCarrying;

        public bool IsMoving => _isMoving;
        public bool IsCarrying => _isCarrying;

        private void Awake()
        {
            EnsureAnimator();
        }

        private void LateUpdate()
        {
            if (!EnsureAnimator())
                return;

            // EmptyIdle uses bind pose (taller). Walk/carry clips sit lower.
            // Blend Visual offset with the same weights as the state transition
            // so stopping does not hop when pose + offset fight each other.
            transform.localPosition = animatedHeightOffset * ResolveAnimatedHeightFactor();
        }

        public void SetAnimatedHeightOffset(Vector3 offset)
        {
            animatedHeightOffset = offset;
        }

        public void SetMoving(bool moving)
        {
            if (_isMoving == moving)
                return;

            _isMoving = moving;
            if (EnsureAnimator())
                animator.SetBool(MovingId, moving);
        }

        public void SetCarrying(bool carrying)
        {
            if (_isCarrying == carrying)
                return;

            _isCarrying = carrying;
            if (EnsureAnimator())
                animator.SetBool(CarryingId, carrying);
        }

        public void SetState(bool moving, bool carrying)
        {
            SetMoving(moving);
            SetCarrying(carrying);
        }

        public static CarryBlockJamStickmanAnimator EnsureOnCylinder(
            Transform cylinderRoot,
            RuntimeAnimatorController controller = null)
        {
            if (cylinderRoot == null)
                return null;

            Transform visual = cylinderRoot.Find("Visual");
            if (visual == null)
                visual = cylinderRoot;

            CarryBlockJamStickmanAnimator driver = visual.GetComponent<CarryBlockJamStickmanAnimator>();
            if (driver == null)
                driver = visual.gameObject.AddComponent<CarryBlockJamStickmanAnimator>();

            driver.EnsureAnimator(controller);
            driver.SetState(false, false);
            return driver;
        }

        private float ResolveAnimatedHeightFactor()
        {
            // EmptyIdle has no motion → clip weight 0 (bind pose).
            // Walk/carry clips contribute weight; during transitions those weights
            // already blend, so Visual Y tracks hips instead of hopping.
            float weight = SumClipWeights(animator.GetCurrentAnimatorClipInfo(0));
            if (animator.IsInTransition(0))
                weight += SumClipWeights(animator.GetNextAnimatorClipInfo(0));

            return Mathf.Clamp01(weight);
        }

        private static float SumClipWeights(AnimatorClipInfo[] clipInfos)
        {
            if (clipInfos == null || clipInfos.Length == 0)
                return 0f;

            float sum = 0f;
            for (int i = 0; i < clipInfos.Length; i++)
                sum += clipInfos[i].weight;

            return sum;
        }

        private bool EnsureAnimator(RuntimeAnimatorController controller = null)
        {
            if (animator == null)
                animator = GetComponent<Animator>();

            if (animator == null)
                animator = GetComponentInChildren<Animator>(true);

            if (animator == null)
                return false;

            animator.applyRootMotion = false;

            if (controller != null)
                animator.runtimeAnimatorController = controller;
            else if (animator.runtimeAnimatorController == null)
            {
#if UNITY_EDITOR
                animator.runtimeAnimatorController =
                    UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerAssetPath);
#endif
                if (animator.runtimeAnimatorController == null)
                    animator.runtimeAnimatorController =
                        Resources.Load<RuntimeAnimatorController>("Stickman");
            }

            return animator.runtimeAnimatorController != null;
        }
    }
}
