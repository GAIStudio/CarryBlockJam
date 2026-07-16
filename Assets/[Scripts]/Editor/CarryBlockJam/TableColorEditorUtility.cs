using CarryBlockJam;
using GAITemplate;
using UnityEngine;

namespace CarryBlockJam.Editor
{
    /// <summary>
    /// Table color options from Mat_Table-{Color} under Assets/[Materials]/-Table Materials.
    /// </summary>
    internal static class TableColorEditorUtility
    {
        private const string CacheKey = "Table";
        private const string MaterialPrefix = "Mat_Table";

        public static PieceColorType DrawPopup(string label, PieceColorType currentValue) =>
            ArtMaterialColorEditorUtility.DrawPopup(
                CacheKey,
                CarryBlockJamArtMaterialUtility.TableMaterialsFolder,
                MaterialPrefix,
                label,
                currentValue);

        public static PieceColorType DrawPopup(Rect rect, string label, PieceColorType currentValue) =>
            ArtMaterialColorEditorUtility.DrawPopup(
                CacheKey,
                CarryBlockJamArtMaterialUtility.TableMaterialsFolder,
                MaterialPrefix,
                rect,
                label,
                currentValue);

        public static PieceColorType DrawPopupNoLabel(PieceColorType currentValue, params GUILayoutOption[] options) =>
            ArtMaterialColorEditorUtility.DrawPopupNoLabel(
                CacheKey,
                CarryBlockJamArtMaterialUtility.TableMaterialsFolder,
                MaterialPrefix,
                currentValue,
                options);

        public static PieceColorType DrawPopupNoLabel(
            PieceColorType currentValue,
            bool includeNone,
            params GUILayoutOption[] options) =>
            ArtMaterialColorEditorUtility.DrawPopupNoLabel(
                CacheKey,
                CarryBlockJamArtMaterialUtility.TableMaterialsFolder,
                MaterialPrefix,
                currentValue,
                includeNone,
                options);

        public static PieceColorType GetDefaultColor() =>
            ArtMaterialColorEditorUtility.GetDefaultColor(
                CacheKey,
                CarryBlockJamArtMaterialUtility.TableMaterialsFolder,
                MaterialPrefix);

        public static PieceColorType Sanitize(PieceColorType currentValue) =>
            ArtMaterialColorEditorUtility.Sanitize(
                CacheKey,
                CarryBlockJamArtMaterialUtility.TableMaterialsFolder,
                MaterialPrefix,
                currentValue);
    }
}
