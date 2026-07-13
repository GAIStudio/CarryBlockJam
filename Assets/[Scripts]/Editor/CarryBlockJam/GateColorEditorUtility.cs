using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using CarryBlockJam;
using GAITemplate;
using UnityEditor;
using UnityEngine;

namespace CarryBlockJam.Editor
{
    /// <summary>
    /// Exit color options come from Mat_Gate* tint materials under Assets/[Materials]/-Gate* folders.
    /// </summary>
    internal static class GateColorEditorUtility
    {
        private static readonly string[] MaterialsFolders =
        {
            CarryBlockJamArtMaterialUtility.GateUpMaterialsFolder,
            CarryBlockJamArtMaterialUtility.GateBottomMaterialsFolder,
        };

        private static readonly Regex ColorSuffixRegex = new Regex(
            @"^Mat_Gate(?:Up|Bottom)-(.+)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static string[] _displayNames;
        private static PieceColorType[] _availableTypes;
        private static bool _cacheBuilt;

        public static PieceColorType DrawPopupNoLabel(PieceColorType currentValue, params GUILayoutOption[] options)
        {
            EnsureCache();
            if (_availableTypes == null || _availableTypes.Length == 0)
                return currentValue;

            int currentIndex = IndexOfOrDefault(currentValue);
            int selectedIndex = EditorGUILayout.Popup(currentIndex, _displayNames, options);
            return _availableTypes[Mathf.Clamp(selectedIndex, 0, _availableTypes.Length - 1)];
        }

        public static PieceColorType DrawPopup(Rect rect, string label, PieceColorType currentValue)
        {
            EnsureCache();
            if (_availableTypes == null || _availableTypes.Length == 0)
                return currentValue;

            int currentIndex = IndexOfOrDefault(currentValue);
            int selectedIndex = EditorGUI.Popup(rect, label, currentIndex, _displayNames);
            return _availableTypes[Mathf.Clamp(selectedIndex, 0, _availableTypes.Length - 1)];
        }

        public static PieceColorType GetDefaultColor()
        {
            EnsureCache();
            if (_availableTypes == null || _availableTypes.Length == 0)
                return PieceColorType.Red;

            return _availableTypes[0];
        }

        public static PieceColorType Sanitize(PieceColorType currentValue)
        {
            EnsureCache();
            if (_availableTypes == null || _availableTypes.Length == 0)
                return currentValue;

            int index = Array.IndexOf(_availableTypes, currentValue);
            return _availableTypes[index >= 0 ? index : 0];
        }

        public static void InvalidateCache()
        {
            _cacheBuilt = false;
            _displayNames = null;
            _availableTypes = null;
        }

        private static int IndexOfOrDefault(PieceColorType currentValue)
        {
            int index = Array.IndexOf(_availableTypes, currentValue);
            return index >= 0 ? index : 0;
        }

        private static void EnsureCache()
        {
            if (_cacheBuilt && _availableTypes != null && _displayNames != null)
                return;

            _cacheBuilt = true;
            var foundColors = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            string[] guids = AssetDatabase.FindAssets("t:Material", MaterialsFolders);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                string name = Path.GetFileNameWithoutExtension(path);
                Match match = ColorSuffixRegex.Match(name);
                if (!match.Success)
                    continue;

                foundColors.Add(match.Groups[1].Value);
            }

            var available = new List<PieceColorType>();
            foreach (string colorName in foundColors)
            {
                if (!CarryBlockJamArtMaterialUtility.TryParseMaterialSuffix(colorName, out PieceColorType colorType))
                    continue;

                available.Add(colorType);
            }

            available.Sort(CompareGateColors);
            var display = new List<string>(available.Count);
            for (int i = 0; i < available.Count; i++)
                display.Add(available[i].ToString());

            _availableTypes = available.ToArray();
            _displayNames = display.ToArray();
        }

        private static int CompareGateColors(PieceColorType a, PieceColorType b)
        {
            return GetSortRank(a).CompareTo(GetSortRank(b));
        }

        private static int GetSortRank(PieceColorType color)
        {
            return color switch
            {
                PieceColorType.Red => 0,
                PieceColorType.Orange => 1,
                PieceColorType.Yellow => 2,
                PieceColorType.Green => 3,
                PieceColorType.GreenDark => 4,
                PieceColorType.Blue => 5,
                PieceColorType.Lightblue => 6,
                PieceColorType.Purple => 7,
                PieceColorType.Pink => 8,
                PieceColorType.Cherry => 9,
                _ => 100 + (int)color,
            };
        }
    }
}
