using GAITemplate;
using UnityEngine;
using UnityEngine.Serialization;

namespace CarryBlockJam
{
    [CreateAssetMenu(fileName = "CarryBlockJamPrefabSettings", menuName = "CarryBlockJam/Prefab Settings")]
    public class CarryBlockJamPrefabSettings : ScriptableObject
    {
        [Header("Board")]
        public CarryBlockJamPrimitiveVisualSettings cellVisual = new CarryBlockJamPrimitiveVisualSettings
        {
            primitiveType = PrimitiveType.Cube,
            localScale = new Vector3(1.5f, 0.2f, 1.5f),
            tintWithPieceColor = false,
        };

        public CarryBlockJamPrimitiveVisualSettings wallVisual = new CarryBlockJamPrimitiveVisualSettings
        {
            primitiveType = PrimitiveType.Cube,
            localScale = new Vector3(1f, 1f, 0.2f),
            tintWithPieceColor = false,
        };

        [Header("Gameplay Pieces")]
        public CarryBlockJamPrimitiveVisualSettings stickmanVisual = new CarryBlockJamPrimitiveVisualSettings
        {
            primitiveType = PrimitiveType.Capsule,
            localScale = new Vector3(0.8f, 1.2f, 0.8f),
            tintWithPieceColor = false,
        };

        [Tooltip("Grid offset applied when spawning the stickman. Y lifts the model above the cell.")]
        public Vector3 stickmanOffset = new Vector3(0f, 1.3f, 0f);

        [Tooltip("Visual height compensation for walk/carry clips vs EmptyIdle bind pose. Blended with animator transitions.")]
        public Vector3 stickmanCarryOffset = new Vector3(0f, -0.7f, 0f);

        [Tooltip("Local euler rotation applied to the stickman visual. Y=180 faces the camera.")]
        public Vector3 stickmanRotation = new Vector3(0f, 180f, 0f);

        [Header("CharTable")]
        [Tooltip("Grid offset applied when M_CharTable is used as the movable player.")]
        public Vector3 charTableOffset = new Vector3(0f, 1.3f, 0f);

        [Tooltip("Local euler rotation applied to M_CharTable.")]
        public Vector3 charTableRotation = new Vector3(0f, 180f, 0f);

        [Tooltip("Local scale applied to M_CharTable.")]
        public Vector3 charTableScale = Vector3.one;

        [Tooltip("Local position of the first carried plate on the CharTable tabletop. Y = height on face; X/Z = slide on the face.")]
        public Vector3 charTablePlateOffset = new Vector3(0f, 0.9f, -0.08f);

        [Tooltip("Vertical distance between plates stacked on CharTable.")]
        public float charTablePlateStackStep = 0.18f;

        [Tooltip("Total time for a collected plate to reach CharTable.")]
        public float charTablePickupDuration = 0.34f;

        [Tooltip("Distance from the tabletop edge used by the plate approach animation.")]
        public float charTablePickupOutsideDistance = 0.5f;

        [Tooltip("Height of the plate immediately before it lands on CharTable.")]
        public float charTablePickupLift = 0.18f;

        [Tooltip("Speed multiplier when plates leave CharTable for a normal table.")]
        public float charTableDropAnimationSpeed = 3f;

        [Tooltip("Euler rotation added to CharTable during the failure animation.")]
        public Vector3 charTableFailureRotation = new Vector3(90f, 0f, 0f);

        [Tooltip("Duration of the CharTable failure rotation.")]
        public float charTableFailureDuration = 0.35f;

        [Tooltip("Local-position offset added while CharTable falls.")]
        public Vector3 charTableFailureOffset = new Vector3(0f, 0.25f, 0f);

        [FormerlySerializedAs("boxVisual")]
        public CarryBlockJamPrimitiveVisualSettings tableVisual = new CarryBlockJamPrimitiveVisualSettings
        {
            primitiveType = PrimitiveType.Cube,
            localScale = Vector3.one,
        };

        public CarryBlockJamPrimitiveVisualSettings plateVisual = new CarryBlockJamPrimitiveVisualSettings
        {
            primitiveType = PrimitiveType.Cylinder,
            localScale = Vector3.one,
        };

        [Header("Exits")]
        public CarryBlockJamPrimitiveVisualSettings exitVisual = new CarryBlockJamPrimitiveVisualSettings
        {
            primitiveType = PrimitiveType.Cube,
            localScale = new Vector3(0.2f, 0.5f, 1.2f),
        };

        public CarryBlockJamPrimitiveVisualSettings exitCarVisual = new CarryBlockJamPrimitiveVisualSettings
        {
            primitiveType = PrimitiveType.Cube,
            localScale = new Vector3(1.6f, 0.8f, 0.9f),
            tintWithPieceColor = false,
        };

        [Header("Exit Model Offsets")]
        [FormerlySerializedAs("topGateModelOffset")]
        [Tooltip("Local-position offset for top exit models (M_GateUp).")]
        public Vector3 topExitModelOffset;

        [FormerlySerializedAs("bottomGateModelOffset")]
        [Tooltip("Local-position offset for bottom exit models (M_GateBottom).")]
        public Vector3 bottomExitModelOffset;

        [Tooltip("Local-position offset for exits authored on the left side.")]
        public Vector3 leftExitModelOffset;

        [Tooltip("Local-position offset for exits authored on the right side.")]
        public Vector3 rightExitModelOffset;

        [Header("Exit Model Scales")]
        [Tooltip("Local-scale multiplier for top exit models (M_GateUp).")]
        public Vector3 topExitModelScale = Vector3.one;

        [Tooltip("Local-scale multiplier for bottom exit models (M_GateBottom).")]
        public Vector3 bottomExitModelScale = Vector3.one;

        [Tooltip("Local-scale multiplier for left exit models (M_GateLeft).")]
        public Vector3 leftExitModelScale = Vector3.one;

        [Tooltip("Local-scale multiplier for right exit models (M_GateRight).")]
        public Vector3 rightExitModelScale = Vector3.one;

        public Vector3 GetExitModelOffset(bool isTopExit) =>
            isTopExit ? topExitModelOffset : bottomExitModelOffset;

        public Vector3 GetExitModelOffset(BoardBorderSide side)
        {
            return side switch
            {
                BoardBorderSide.Bottom => bottomExitModelOffset,
                BoardBorderSide.Left => leftExitModelOffset,
                BoardBorderSide.Right => rightExitModelOffset,
                _ => topExitModelOffset,
            };
        }

        public Vector3 GetExitModelScale(BoardBorderSide side)
        {
            Vector3 scale = side switch
            {
                BoardBorderSide.Bottom => bottomExitModelScale,
                BoardBorderSide.Left => leftExitModelScale,
                BoardBorderSide.Right => rightExitModelScale,
                _ => topExitModelScale,
            };
            return scale == Vector3.zero ? Vector3.one : scale;
        }

        public Vector3 GetGateModelOffset(bool isUpGate) => GetExitModelOffset(isUpGate);
        public Vector3 GetGateModelOffset(BoardBorderSide side) => GetExitModelOffset(side);
        public Vector3 GetGateModelScale(BoardBorderSide side) => GetExitModelScale(side);

        [Header("Fallback Colors")]
        public PieceColorType boardCellColor = PieceColorType.Grey;
        public PieceColorType wallColor = PieceColorType.Grey;
        public PieceColorType exitCarColor = PieceColorType.White;
    }
}
