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
    /// Builds PieceColorType dropdowns from Mat_*-{Color} materials in a folder.
    /// </summary>
    internal static class ArtMaterialColorEditorUtility
    {
        private sealed class ColorCache
        {
            public string[] DisplayNames;
            public PieceColorType[] AvailableTypes;
            public bool Built;
        }

        private static readonly Dictionary<string, ColorCache> Caches =
            new Dictionary<string, ColorCache>(StringComparer.Ordinal);

        public static PieceColorType DrawPopup(
            string cacheKey,
            string materialsFolder,
            string materialPrefix,
            string label,
            PieceColorType currentValue)
        {
            EnsureCache(cacheKey, materialsFolder, materialPrefix);
            ColorCache cache = Caches[cacheKey];
            if (cache.AvailableTypes == null || cache.AvailableTypes.Length == 0)
                return currentValue;

            int currentIndex = IndexOfOrDefault(cache, currentValue);
            int selectedIndex = EditorGUILayout.Popup(label, currentIndex, cache.DisplayNames);
            return cache.AvailableTypes[Mathf.Clamp(selectedIndex, 0, cache.AvailableTypes.Length - 1)];
        }

        public static PieceColorType DrawPopup(
            string cacheKey,
            string materialsFolder,
            string materialPrefix,
            Rect rect,
            string label,
            PieceColorType currentValue)
        {
            EnsureCache(cacheKey, materialsFolder, materialPrefix);
            ColorCache cache = Caches[cacheKey];
            if (cache.AvailableTypes == null || cache.AvailableTypes.Length == 0)
                return currentValue;

            int currentIndex = IndexOfOrDefault(cache, currentValue);
            int selectedIndex = EditorGUI.Popup(rect, label, currentIndex, cache.DisplayNames);
            return cache.AvailableTypes[Mathf.Clamp(selectedIndex, 0, cache.AvailableTypes.Length - 1)];
        }

        public static PieceColorType DrawPopupNoLabel(
            string cacheKey,
            string materialsFolder,
            string materialPrefix,
            PieceColorType currentValue,
            params GUILayoutOption[] options)
        {
            return DrawPopupNoLabel(
                cacheKey,
                materialsFolder,
                materialPrefix,
                currentValue,
                includeNone: false,
                options);
        }

        public static PieceColorType DrawPopupNoLabel(
            string cacheKey,
            string materialsFolder,
            string materialPrefix,
            PieceColorType currentValue,
            bool includeNone,
            params GUILayoutOption[] options)
        {
            EnsureCache(cacheKey, materialsFolder, materialPrefix);
            ColorCache cache = Caches[cacheKey];
            if (cache.AvailableTypes == null || cache.AvailableTypes.Length == 0)
                return includeNone && currentValue == PieceColorType.None ? PieceColorType.None : currentValue;

            if (!includeNone)
            {
                int currentIndex = IndexOfOrDefault(cache, currentValue);
                int selectedIndex = EditorGUILayout.Popup(currentIndex, cache.DisplayNames, options);
                return cache.AvailableTypes[Mathf.Clamp(selectedIndex, 0, cache.AvailableTypes.Length - 1)];
            }

            var names = new string[cache.DisplayNames.Length + 1];
            names[0] = PieceColorType.None.ToString();
            for (int i = 0; i < cache.DisplayNames.Length; i++)
                names[i + 1] = cache.DisplayNames[i];

            int indexWithNone = currentValue == PieceColorType.None
                ? 0
                : IndexOfOrDefault(cache, currentValue) + 1;
            int selected = EditorGUILayout.Popup(indexWithNone, names, options);
            if (selected <= 0)
                return PieceColorType.None;

            return cache.AvailableTypes[Mathf.Clamp(selected - 1, 0, cache.AvailableTypes.Length - 1)];
        }

        public static PieceColorType GetDefaultColor(
            string cacheKey,
            string materialsFolder,
            string materialPrefix)
        {
            EnsureCache(cacheKey, materialsFolder, materialPrefix);
            ColorCache cache = Caches[cacheKey];
            if (cache.AvailableTypes == null || cache.AvailableTypes.Length == 0)
                return PieceColorType.Red;

            return cache.AvailableTypes[0];
        }

        public static PieceColorType Sanitize(
            string cacheKey,
            string materialsFolder,
            string materialPrefix,
            PieceColorType currentValue)
        {
            EnsureCache(cacheKey, materialsFolder, materialPrefix);
            ColorCache cache = Caches[cacheKey];
            if (cache.AvailableTypes == null || cache.AvailableTypes.Length == 0)
                return currentValue;

            int index = Array.IndexOf(cache.AvailableTypes, currentValue);
            return cache.AvailableTypes[index >= 0 ? index : 0];
        }

        public static void InvalidateCache(string cacheKey = null)
        {
            if (string.IsNullOrEmpty(cacheKey))
            {
                Caches.Clear();
                return;
            }

            Caches.Remove(cacheKey);
        }

        private static int IndexOfOrDefault(ColorCache cache, PieceColorType currentValue)
        {
            int index = Array.IndexOf(cache.AvailableTypes, currentValue);
            return index >= 0 ? index : 0;
        }

        private static void EnsureCache(string cacheKey, string materialsFolder, string materialPrefix)
        {
            if (!Caches.TryGetValue(cacheKey, out ColorCache cache))
            {
                cache = new ColorCache();
                Caches[cacheKey] = cache;
            }

            if (cache.Built && cache.AvailableTypes != null && cache.DisplayNames != null)
                return;

            cache.Built = true;
            var foundColors = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { materialsFolder });
            var suffixRegex = new Regex(
                $"^{Regex.Escape(materialPrefix)}-(.+)$",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                string name = Path.GetFileNameWithoutExtension(path);
                Match match = suffixRegex.Match(name);
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

            available.Sort(CompareColors);
            var display = new List<string>(available.Count);
            for (int i = 0; i < available.Count; i++)
                display.Add(available[i].ToString());

            cache.AvailableTypes = available.ToArray();
            cache.DisplayNames = display.ToArray();
        }

        private static int CompareColors(PieceColorType a, PieceColorType b)
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
