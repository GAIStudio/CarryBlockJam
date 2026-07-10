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

        [FormerlySerializedAs("boxVisual")]
        public CarryBlockJamPrimitiveVisualSettings tableVisual = new CarryBlockJamPrimitiveVisualSettings
        {
            primitiveType = PrimitiveType.Cube,
            localScale = Vector3.one,
        };

        public CarryBlockJamPrimitiveVisualSettings plateVisual = new CarryBlockJamPrimitiveVisualSettings
        {
            primitiveType = PrimitiveType.Cylinder,
            localScale = new Vector3(0.9f, 0.15f, 0.9f),
            localRotation = new Vector3(90f, 0f, 0f),
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

        [Header("Fallback Colors")]
        public PieceColorType boardCellColor = PieceColorType.Grey;
        public PieceColorType wallColor = PieceColorType.Grey;
        public PieceColorType exitCarColor = PieceColorType.White;
    }
}
