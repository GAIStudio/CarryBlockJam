using CarryBlockJam;
using GAITemplate;
using UnityEngine;

namespace CarryBlockJam.Editor
{
    /// <summary>
    /// Plate color options from Mat_Plate-{Color} under Assets/[Materials]/-Plate Materials.
    /// </summary>
    internal static class PlateColorEditorUtility
    {
        private const string CacheKey = "Plate";
        private const string MaterialPrefix = "Mat_Plate";

        public static PieceColorType DrawPopup(string label, PieceColorType currentValue) =>
            ArtMaterialColorEditorUtility.DrawPopup(
                CacheKey,
                CarryBlockJamArtMaterialUtility.PlateMaterialsFolder,
                MaterialPrefix,
                label,
                currentValue);

        public static PieceColorType DrawPopup(Rect rect, string label, PieceColorType currentValue) =>
            ArtMaterialColorEditorUtility.DrawPopup(
                CacheKey,
                CarryBlockJamArtMaterialUtility.PlateMaterialsFolder,
                MaterialPrefix,
                rect,
                label,
                currentValue);

        public static PieceColorType DrawPopupNoLabel(PieceColorType currentValue, params GUILayoutOption[] options) =>
            ArtMaterialColorEditorUtility.DrawPopupNoLabel(
                CacheKey,
                CarryBlockJamArtMaterialUtility.PlateMaterialsFolder,
                MaterialPrefix,
                currentValue,
                options);

        public static PieceColorType DrawPopupNoLabel(
            PieceColorType currentValue,
            bool includeNone,
            params GUILayoutOption[] options) =>
            ArtMaterialColorEditorUtility.DrawPopupNoLabel(
                CacheKey,
                CarryBlockJamArtMaterialUtility.PlateMaterialsFolder,
                MaterialPrefix,
                currentValue,
                includeNone,
                options);

        public static PieceColorType GetDefaultColor() =>
            ArtMaterialColorEditorUtility.GetDefaultColor(
                CacheKey,
                CarryBlockJamArtMaterialUtility.PlateMaterialsFolder,
                MaterialPrefix);

        public static PieceColorType Sanitize(PieceColorType currentValue) =>
            ArtMaterialColorEditorUtility.Sanitize(
                CacheKey,
                CarryBlockJamArtMaterialUtility.PlateMaterialsFolder,
                MaterialPrefix,
                currentValue);
    }
}
