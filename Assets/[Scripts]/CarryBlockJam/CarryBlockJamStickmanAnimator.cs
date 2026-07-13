using UnityEngine;

namespace CarryBlockJam
{
    /// <summary>
    /// Drives Stickman Animator states: empty walk, carrying idle, carry walk.
    /// </summary>
    [DisallowMultipleComponent]
    public class CarryBlockJamStickmanAnimator : MonoBehaviour
    {
        public const string MovingParam = "IsMoving";
        public const string CarryingParam = "IsCarrying";
        public const string ControllerAssetPath = "Assets/[Animations]/Stickman.controller";

        [SerializeField] private Animator animator;

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
