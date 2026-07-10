using System;
using System.Collections.Generic;
using System.IO;
using GAITemplate;
using UnityEditor;
using UnityEngine;

namespace CarryBlockJam.Editor
{
    internal static class PieceColorTypeEditorUtility
    {
        private const string MaterialsFolder = "Assets/Resources/Materials/GamePieceColors";

        private static string[] _displayNames;
        private static PieceColorType[] _availableTypes;

        public static PieceColorType DrawPopup(string label, PieceColorType currentValue)
        {
            EnsureCache();
            if (_availableTypes == null || _availableTypes.Length == 0)
                return currentValue;

            int currentIndex = Array.IndexOf(_availableTypes, currentValue);
            if (currentIndex < 0)
                currentIndex = 0;

            int selectedIndex = EditorGUILayout.Popup(label, currentIndex, _displayNames);
            return _availableTypes[Mathf.Clamp(selectedIndex, 0, _availableTypes.Length - 1)];
        }

        public static PieceColorType DrawPopup(Rect rect, string label, PieceColorType currentValue)
        {
            EnsureCache();
            if (_availableTypes == null || _availableTypes.Length == 0)
                return currentValue;

            int currentIndex = Array.IndexOf(_availableTypes, currentValue);
            if (currentIndex < 0)
                currentIndex = 0;

            int selectedIndex = EditorGUI.Popup(rect, label, currentIndex, _displayNames);
            return _availableTypes[Mathf.Clamp(selectedIndex, 0, _availableTypes.Length - 1)];
        }

        public static PieceColorType DrawPopupNoLabel(PieceColorType currentValue, params GUILayoutOption[] options)
        {
            EnsureCache();
            if (_availableTypes == null || _availableTypes.Length == 0)
                return currentValue;

            int currentIndex = Array.IndexOf(_availableTypes, currentValue);
            if (currentIndex < 0)
                currentIndex = 0;

            int selectedIndex = EditorGUILayout.Popup(currentIndex, _displayNames, options);
            return _availableTypes[Mathf.Clamp(selectedIndex, 0, _availableTypes.Length - 1)];
        }

        private static void EnsureCache()
        {
            if (_availableTypes != null && _displayNames != null)
                return;

            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { MaterialsFolder });
            var foundNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                foundNames.Add(Path.GetFileNameWithoutExtension(path));
            }

            var available = new List<PieceColorType>();
            foreach (PieceColorType type in Enum.GetValues(typeof(PieceColorType)))
            {
                if (type == PieceColorType.None)
                    continue;

                if (foundNames.Contains(type.ToString()))
                    available.Add(type);
            }

            _availableTypes = available.ToArray();
            _displayNames = new string[_availableTypes.Length];
            for (int i = 0; i < _availableTypes.Length; i++)
                _displayNames[i] = _availableTypes[i].ToString();
        }
    }
}
