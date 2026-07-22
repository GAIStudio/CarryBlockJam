using UnityEngine;

namespace CarryBlockJam
{
    /// <summary>
    /// Drives Stickman idle, movement, carrying, and failure animation states.
    /// </summary>
    [DisallowMultipleComponent]
    public class CarryBlockJamStickmanAnimator : MonoBehaviour
    {
        public const string MovingParam = "IsMoving";
        public const string CarryingParam = "IsCarrying";
        public const string FailedParam = "IsFailed";
        public const string ControllerAssetPath = "Assets/[Animations]/Stickman.controller";
        public const string EmptyIdleStateName = "Idle";
        public const string FailedStateName = "FallingDown";

        [SerializeField] private Animator animator;
        [SerializeField] private Vector3 animatedHeightOffset = new Vector3(0f, -0.7f, 0f);

        private static readonly int MovingId = Animator.StringToHash(MovingParam);
        private static readonly int CarryingId = Animator.StringToHash(CarryingParam);
        private static readonly int FailedId = Animator.StringToHash(FailedParam);

        private bool _isMoving;
        private bool _isCarrying;
        private bool _isFailed;

        public bool IsMoving => _isMoving;
        public bool IsCarrying => _isCarrying;
        public bool IsFailed => _isFailed;

        private void Awake()
        {
            EnsureAnimator();
        }

        private void LateUpdate()
        {
            if (!EnsureAnimator())
                return;

            // Every requested state uses a Mixamo clip. Apply the configured
            // feet-height compensation consistently while transitions blend.
            transform.localPosition = animatedHeightOffset * ResolveAnimatedHeightFactor();
        }

        public void SetAnimatedHeightOffset(Vector3 offset)
        {
            animatedHeightOffset = offset;
        }

        public void SetMoving(bool moving)
        {
            if (_isFailed)
                return;

            if (_isMoving == moving)
                return;

            _isMoving = moving;
            if (EnsureAnimator())
                animator.SetBool(MovingId, moving);
        }

        public void SetCarrying(bool carrying)
        {
            if (_isFailed)
                return;

            if (_isCarrying == carrying)
                return;

            _isCarrying = carrying;
            if (EnsureAnimator())
                animator.SetBool(CarryingId, carrying);
        }

        public void SetState(bool moving, bool carrying)
        {
            if (_isFailed)
                return;

            SetMoving(moving);
            SetCarrying(carrying);
        }

        public void PlayFailure()
        {
            if (_isFailed)
                return;

            _isFailed = true;
            _isMoving = false;
            if (!EnsureAnimator())
                return;

            animator.SetBool(MovingId, false);
            animator.SetBool(FailedId, true);
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
            bool created = driver == null;
            if (driver == null)
                driver = visual.gameObject.AddComponent<CarryBlockJamStickmanAnimator>();

            driver.EnsureAnimator(controller);
            if (created)
            {
                driver._isFailed = false;
                if (driver.animator != null)
                    driver.animator.SetBool(FailedId, false);
                driver.SetState(false, false);
            }
            return driver;
        }

        private float ResolveAnimatedHeightFactor()
        {
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
