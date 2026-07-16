using CarryBlockJam;
using GAITemplate;
using UnityEngine;

namespace CarryBlockJam.Editor
{
    /// <summary>
    /// Exit color options from Mat_GateUp-{Color} under Assets/[Materials]/-GateUp Materials.
    /// </summary>
    internal static class GateColorEditorUtility
    {
        private const string CacheKey = "GateUp";
        private const string MaterialPrefix = "Mat_GateUp";

        public static PieceColorType DrawPopupNoLabel(PieceColorType currentValue, params GUILayoutOption[] options) =>
            ArtMaterialColorEditorUtility.DrawPopupNoLabel(
                CacheKey,
                CarryBlockJamArtMaterialUtility.GateUpMaterialsFolder,
                MaterialPrefix,
                currentValue,
                options);

        public static PieceColorType DrawPopup(Rect rect, string label, PieceColorType currentValue) =>
            ArtMaterialColorEditorUtility.DrawPopup(
                CacheKey,
                CarryBlockJamArtMaterialUtility.GateUpMaterialsFolder,
                MaterialPrefix,
                rect,
                label,
                currentValue);

        public static PieceColorType GetDefaultColor() =>
            ArtMaterialColorEditorUtility.GetDefaultColor(
                CacheKey,
                CarryBlockJamArtMaterialUtility.GateUpMaterialsFolder,
                MaterialPrefix);

        public static PieceColorType Sanitize(PieceColorType currentValue) =>
            ArtMaterialColorEditorUtility.Sanitize(
                CacheKey,
                CarryBlockJamArtMaterialUtility.GateUpMaterialsFolder,
                MaterialPrefix,
                currentValue);

        public static void InvalidateCache() =>
            ArtMaterialColorEditorUtility.InvalidateCache(CacheKey);
    }
}
