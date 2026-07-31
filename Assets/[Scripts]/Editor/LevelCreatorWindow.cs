using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using CarryBlockJam;
using CarryBlockJam.Editor;

namespace GAITemplate.Editor
{
    public class LevelCreatorWindow : EditorWindow
    {
        private const string LastLevelDataKey = "GAITemplate.LevelCreator.LastLevelData";
        private const string PinLoadKey = "GAITemplate.LevelCreator.PinLoadAtTop";

        private enum CellTool { None, Hidden, Ice, Curtain, Tunnel }

        private const int IceDefaultCount = 3;

        private LevelData _levelData;
        private LevelData _lastLoadedLevelData;
        private SerializedObject _levelDataSo;

        private PieceColorType[,] _cellColors;
        private PieceColorType[,] _cellSecondaryColors;
        private LevelCellFlag[,]  _cellFlags;
        private int[,]            _cellFlagValues;
        private CellDirection[,]  _cellDirections;
        private System.Collections.Generic.List<PieceColorType>[,] _cellTunnelPieces;
        private int _rows = 4;
        private int _columns = 4;
        private const int MinGridSize = 1;
        private const int MaxGridSize = 20;

        private CellTool _activeTool = CellTool.None;

        private Vector2 _scroll;
        private bool _pinLoadAtTop;
        private bool _tutorialFoldout = true;
        private bool _carryBlockJamFoldout = true;
        private Vector2 _tutorialStagesScroll;
        private bool _scenePreviewRefreshQueued;

        [MenuItem("GAITemplate/Level Creator")]
        public static void Open()
        {
            GetWindow<LevelCreatorWindow>("Level Creator");
        }

        private void OnEnable()
        {
            _pinLoadAtTop = EditorPrefs.GetBool(PinLoadKey, true);

            string lastLevelGuid = EditorPrefs.GetString(LastLevelDataKey, string.Empty);
            if (!string.IsNullOrEmpty(lastLevelGuid))
            {
                string path = AssetDatabase.GUIDToAssetPath(lastLevelGuid);
                if (!string.IsNullOrEmpty(path))
                    SetLevelData(AssetDatabase.LoadAssetAtPath<LevelData>(path), false);
            }
        }

        private void OnGUI()
        {
            bool canLoadFromLevelManager = Application.isPlaying &&
                                           LevelManager.instance != null &&
                                           LevelManager.instance.currentLevelData != null;

            if (_pinLoadAtTop)
            {
                DrawLoadFromLevelManagerRow(canLoadFromLevelManager);
                GUILayout.Space(5f);
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            GUILayout.Label("Level Data Settings", EditorStyles.boldLabel);
            DrawLevelDataField();

            if (!_pinLoadAtTop)
            {
                GUILayout.Space(5f);
                DrawLoadFromLevelManagerRow(canLoadFromLevelManager);
            }

            if (_levelData == null)
            {
                EditorGUILayout.EndScrollView();
                return;
            }

            EnsureGridLoaded();

            GUILayout.Space(10f);
            DrawGridSettings();
            GUILayout.Space(8f);
            DrawPlateGateBalanceSummary();
            GUILayout.Space(8f);
            DrawTunnelPiecesSection();
            GUILayout.Space(8f);
            DrawCarryBlockJamSection();
            GUILayout.Space(8f);
            DrawTutorialSection();

            EditorGUILayout.EndScrollView();

            EditorGUILayout.HelpBox(
                "Grid size + cell colors Save to the Level Data asset and the open SampleScene board. " +
                "CarryBlockJam settings apply immediately. Use Apply To Scene to rebuild without leaving Level Creator.",
                MessageType.Info);

            GUILayout.BeginHorizontal();
            GUI.color = Color.green;
            if (GUILayout.Button("Save", GUILayout.Height(30f)))
                SaveLevel();
            GUI.color = Color.white;

            if (GUILayout.Button("Apply To Scene", GUILayout.Height(30f), GUILayout.Width(140f)))
                ApplyToScene();
            GUILayout.EndHorizontal();
        }

        private void DrawLoadFromLevelManagerRow(bool canLoadFromLevelManager)
        {
            GUILayout.BeginHorizontal();
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 58f;
            _pinLoadAtTop = EditorGUILayout.Toggle("Pin at top", _pinLoadAtTop, GUILayout.ExpandWidth(false));
            EditorPrefs.SetBool(PinLoadKey, _pinLoadAtTop);
            EditorGUIUtility.labelWidth = oldLabelWidth;
            GUILayout.Space(4f);
            using (new EditorGUI.DisabledScope(!canLoadFromLevelManager))
            {
                if (GUILayout.Button("Load from LevelManager", GUILayout.Width(160f)))
                    LoadFromLevelManager();
            }

            GUILayout.EndHorizontal();
        }

        private void DrawLevelDataField()
        {
            LevelData previous = _levelData;
            _levelData = (LevelData)EditorGUILayout.ObjectField("Level Data", _levelData, typeof(LevelData), false);
            if (_levelData != previous)
                SetLevelData(_levelData, true);
        }

        private void DrawGridSettings()
        {
            bool isSlide = _levelData != null && _levelData.mechanicType == PuzzleMechanicType.SlideLane;

            string sectionLabel = isSlide ? "Slide Lane Settings" : "Grid Settings";
            string rowsLabel    = isSlide ? "Depth"      : "Row";
            string colsLabel    = isSlide ? "Lane Count" : "Column";

            GUILayout.Label(sectionLabel, EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            int newRows = EditorGUILayout.IntSlider(rowsLabel, _rows, MinGridSize, MaxGridSize);
            int newColumns = EditorGUILayout.IntSlider(colsLabel, _columns, MinGridSize, MaxGridSize);
            if (EditorGUI.EndChangeCheck())
            {
                if (newRows != _rows || newColumns != _columns)
                {
                    _rows = newRows;
                    _columns = newColumns;
                    ResizeGrids(_rows, _columns);
                    SyncPreviewGridDimensions();
                    EditorUtility.SetDirty(_levelData);
                    RequestScenePreviewRefresh();
                }
            }

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Clear All", GUILayout.Width(80f)))
                ClearAllCells();
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);
            DrawToolbar();
            GUILayout.Space(6f);
            EditorGUI.BeginChangeCheck();
            DrawColorGrid(isSlide);
            if (EditorGUI.EndChangeCheck() && IsCarryBlockJamGrid())
            {
                SyncPlatePlacementsFromGrid();
                EditorUtility.SetDirty(_levelData);
                RequestScenePreviewRefresh();
            }
        }

        // ── Toolbar ──────────────────────────────────────────────────────────────────

        private void DrawToolbar()
        {
            bool isSlide = _levelData != null && _levelData.mechanicType == PuzzleMechanicType.SlideLane;

            // SlideLane'de Tunnel yok — aktif toolsa None'a düş.
            if (isSlide && _activeTool == CellTool.Tunnel)
                _activeTool = CellTool.None;

            GUILayout.Label("Tool", EditorStyles.miniBoldLabel);

            GUILayout.BeginHorizontal();
            DrawToolButton(CellTool.None,   "None");
            DrawToolButton(CellTool.Hidden, "Hidden");
            DrawToolButton(CellTool.Ice,    "Ice");
            DrawToolButton(CellTool.Curtain, "Color Table");
            if (!isSlide)
                DrawToolButton(CellTool.Tunnel, "Tunnel");
            GUILayout.EndHorizontal();

            string hint = _activeTool switch
            {
                CellTool.None when IsCarryBlockJamGrid() =>
                    "Normal plates: pick a color under a cell (no Hidden/Ice/Color Table flag). " +
                    "Click the cell with None tool to clear special flags and keep it as a normal plate. " +
                    "Set color to None to remove the plate.",
                CellTool.None => "Cell'e tıklayınca üzerindeki tüm flag'ler temizlenir. Renk dropdown'la seçilir.",
                CellTool.Hidden when _levelData != null && _levelData.mechanicType == PuzzleMechanicType.Grid =>
                    "Hidden cell + color spawns a hidden CarryBlockJam plate at that cell. " +
                    "It reveals when every plate on surrounding cells (including diagonals) is collected.",
                CellTool.Ice when _levelData != null && _levelData.mechanicType == PuzzleMechanicType.Grid =>
                    "Ice cell + color spawns a frozen CarryBlockJam plate. Set unlock moves in the small field next to the color. " +
                    "Each collected plate counts down until the ice melts and the plate can be picked up.",
                CellTool.Curtain when _levelData != null && _levelData.mechanicType == PuzzleMechanicType.Grid =>
                    "Color Table cell: set Accept Color. Spawns a table that only accepts plates of that color. " +
                    "Badge sprite look is shared on CarryBlockJamRuntimePieceSpawner.",
                _ => $"Cell'e tıklayınca {_activeTool} bit'i toggle olur. Birden fazla flag aynı cell'de bulunabilir.",
            };
            EditorGUILayout.HelpBox(hint, MessageType.None);
        }

        private void DrawToolButton(CellTool tool, string label)
        {
            Color prev = GUI.backgroundColor;
            if (_activeTool == tool) GUI.backgroundColor = new Color(0.5f, 0.85f, 0.5f);
            if (GUILayout.Button(label, GUILayout.Height(24f)))
                _activeTool = tool;
            GUI.backgroundColor = prev;
        }

        // ── Cell grid ────────────────────────────────────────────────────────────────

        private void DrawColorGrid(bool isSlide)
        {
            EnsureGridSizes();

            // Editör görünümü: row 0 en üstte (sol-üst köşe = [0,0]).
            for (int row = 0; row < _rows; row++)
            {
                GUILayout.BeginHorizontal();

                if (isSlide && row == 0)
                    GUILayout.Label("Back  →", GUILayout.Width(50f));
                else if (isSlide && row == _rows - 1)
                    GUILayout.Label("Front →", GUILayout.Width(50f));
                else if (isSlide)
                    GUILayout.Space(50f);

                for (int column = 0; column < _columns; column++)
                {
                    DrawCell(row, column);
                }

                GUILayout.EndHorizontal();
            }
        }

        private void DrawCell(int row, int column)
        {
            // Keep each cell column narrow and short so Ice/Color-Table extras
            // do not open large gaps between grid rows.
            GUILayout.BeginVertical(GUILayout.Width(64f));

            LevelCellFlag flags = _cellFlags[row, column];
            bool isTunnel = (flags & LevelCellFlag.Tunnel) == LevelCellFlag.Tunnel;
            bool isIce = (flags & LevelCellFlag.Ice) == LevelCellFlag.Ice;
            bool isColorTable = (flags & LevelCellFlag.Curtain) == LevelCellFlag.Curtain;

            Rect rect = GUILayoutUtility.GetRect(40f, 40f, GUILayout.Width(40f), GUILayout.Height(40f));

            // Tutorial clickable cell ise yeşil border (tüm stage'lerden union).
            if (IsTutorialClickableCell(row, column))
            {
                Rect border = new Rect(rect.x - 2f, rect.y - 2f, rect.width + 4f, rect.height + 4f);
                EditorGUI.DrawRect(border, new Color(0.2f, 0.9f, 0.3f, 0.85f));
            }

            // Tunnel cell: gri arka plan (renk önemsiz). Aksi halde piece rengini göster.
            Color prevBgColor = GUI.color;
            GUI.color = isTunnel
                ? new Color(0.35f, 0.35f, 0.45f)
                : PieceColorPalette.GetColor(_cellColors[row, column]);
            if (GUI.Button(rect, GUIContent.none))
                ApplyToolToCell(row, column);
            GUI.color = prevBgColor;

            // Sol-üst köşede [row, col] etiketi.
            var coordStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = new Color(1f, 1f, 1f, 0.75f) },
                fontSize = 9,
            };
            EditorGUI.LabelField(
                new Rect(rect.x + 2f, rect.y, rect.width, 12f),
                $"[{row},{column}]",
                coordStyle);

            // Overlay: special flags (H/I/C/T) or "P" for a normal painted plate.
            string overlayLabel = GetFlagsShortLabel(flags);
            if (string.IsNullOrEmpty(overlayLabel) &&
                IsCarryBlockJamGrid() &&
                PieceColorPalette.IsPaintable(_cellColors[row, column]))
            {
                overlayLabel = "P";
            }

            if (!string.IsNullOrEmpty(overlayLabel))
            {
                var labelStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white },
                };
                EditorGUI.LabelField(rect, overlayLabel, labelStyle);
            }

            // Ice unlock count on the cell (bottom-right) so it does not need a
            // separate tall "Unlock Moves" label under the grid.
            if (isIce && IsCarryBlockJamGrid())
            {
                var unlockStyle = new GUIStyle(EditorStyles.miniBoldLabel)
                {
                    alignment = TextAnchor.LowerRight,
                    normal = { textColor = Color.white },
                    fontSize = 10,
                };
                EditorGUI.LabelField(
                    new Rect(rect.x, rect.yMax - 14f, rect.width - 2f, 14f),
                    _cellFlagValues[row, column].ToString(),
                    unlockStyle);
            }

            GUILayout.Space(1f);

            // Tunnel cell renk gerektirmez (orada tunnel objesi spawn olur, piece değil).
            if (isTunnel)
            {
                _cellDirections[row, column] = (CellDirection)EditorGUILayout.EnumPopup(
                    _cellDirections[row, column],
                    GUILayout.Width(64f),
                    GUILayout.Height(16f));
            }
            else if (isIce && IsCarryBlockJamGrid())
            {
                // One compact row: color + unlock moves (no extra label line).
                EditorGUILayout.BeginHorizontal(GUILayout.Width(64f), GUILayout.Height(16f));
                _cellColors[row, column] = PlateColorEditorUtility.DrawPopupNoLabel(
                    _cellColors[row, column],
                    includeNone: true,
                    GUILayout.Width(40f),
                    GUILayout.Height(16f));

                int prevValue = _cellFlagValues[row, column];
                int newValue = EditorGUILayout.IntField(
                    new GUIContent(string.Empty, "Unlock moves"),
                    prevValue,
                    GUILayout.Width(22f),
                    GUILayout.Height(16f));
                if (newValue < 1)
                    newValue = 1;
                if (newValue != prevValue)
                    _cellFlagValues[row, column] = newValue;
                EditorGUILayout.EndHorizontal();
            }
            else if (isColorTable && IsCarryBlockJamGrid())
            {
                _cellColors[row, column] = PlateColorEditorUtility.DrawPopupNoLabel(
                    _cellColors[row, column],
                    includeNone: true,
                    GUILayout.Width(64f),
                    GUILayout.Height(16f));
            }
            else
            {
                    _cellColors[row, column] = DrawGridCellColorPopup(
                    _cellColors[row, column]);
            }

            GUILayout.EndVertical();
        }

        private bool IsCarryBlockJamGrid()
        {
            return _levelData != null && _levelData.mechanicType == PuzzleMechanicType.Grid;
        }

        private PieceColorType DrawGridCellColorPopup(PieceColorType current)
        {
            if (!IsCarryBlockJamGrid())
            {
                return (PieceColorType)EditorGUILayout.EnumPopup(
                    current,
                    GUILayout.Width(64f),
                    GUILayout.Height(16f));
            }

            return PlateColorEditorUtility.DrawPopupNoLabel(
                current,
                includeNone: true,
                GUILayout.Width(64f),
                GUILayout.Height(16f));
        }

        private void DrawTunnelPiecesSection()
        {
            // Sadece Grid mode'da göster (SlideLane'de tunnel yok).
            bool isSlide = _levelData != null && _levelData.mechanicType == PuzzleMechanicType.SlideLane;
            if (isSlide) return;

            // Tunnel cell'leri topla.
            var tunnels = new System.Collections.Generic.List<(int row, int col)>();
            for (int r = 0; r < _rows; r++)
                for (int c = 0; c < _columns; c++)
                    if ((_cellFlags[r, c] & LevelCellFlag.Tunnel) == LevelCellFlag.Tunnel)
                        tunnels.Add((r, c));

            GUILayout.Label("Tunnel Pieces", EditorStyles.boldLabel);

            if (tunnels.Count == 0)
            {
                EditorGUILayout.HelpBox("Henüz Tunnel flag'i set edilmiş cell yok.", MessageType.None);
                return;
            }

            foreach (var (row, col) in tunnels)
            {
                DrawTunnelEntry(row, col);
                GUILayout.Space(4f);
            }
        }

        private void DrawTunnelEntry(int row, int column)
        {
            var list = _cellTunnelPieces[row, column];
            if (list == null)
            {
                list = new System.Collections.Generic.List<PieceColorType>();
                _cellTunnelPieces[row, column] = list;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            CellDirection dir = _cellDirections[row, column];
            EditorGUILayout.LabelField(
                $"Tunnel [{row}, {column}] — {dir}",
                EditorStyles.miniBoldLabel);

            // Pieces listesi.
            for (int i = 0; i < list.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"#{i + 1}", GUILayout.Width(28f));

                Rect colorRect = GUILayoutUtility.GetRect(20f, 16f, GUILayout.Width(20f));
                EditorGUI.DrawRect(colorRect, PieceColorPalette.GetColor(list[i]));

                list[i] = IsCarryBlockJamGrid()
                    ? PlateColorEditorUtility.DrawPopupNoLabel(list[i], includeNone: true, GUILayout.Width(120f))
                    : (PieceColorType)EditorGUILayout.EnumPopup(list[i], GUILayout.Width(120f));
                if (GUILayout.Button("×", GUILayout.Width(22f)))
                {
                    list.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }

            // "+" yeni piece ekle.
            if (GUILayout.Button("+ Add Piece", GUILayout.Height(20f)))
                list.Add(PieceColorType.None);

            EditorGUILayout.EndVertical();
        }

        private void ApplyToolToCell(int row, int column)
        {
            if (_activeTool == CellTool.None)
            {
                _cellFlags[row, column]      = LevelCellFlag.None;
                _cellFlagValues[row, column] = 0;
                if (IsCarryBlockJamGrid())
                {
                    SyncPlatePlacementsFromGrid();
                    EditorUtility.SetDirty(_levelData);
                    RequestScenePreviewRefresh();
                }
                return;
            }

            LevelCellFlag bit = _activeTool switch
            {
                CellTool.Hidden => LevelCellFlag.Hidden,
                CellTool.Ice    => LevelCellFlag.Ice,
                CellTool.Curtain => LevelCellFlag.Curtain,
                CellTool.Tunnel => LevelCellFlag.Tunnel,
                _               => LevelCellFlag.None,
            };

            bool wasSet = (_cellFlags[row, column] & bit) == bit;
            if (wasSet)
            {
                // Bit'i kaldır.
                _cellFlags[row, column] &= ~bit;
                if (bit == LevelCellFlag.Ice)
                    _cellFlagValues[row, column] = 0;
            }
            else
            {
                // Bit'i ekle.
                _cellFlags[row, column] |= bit;
                if (bit == LevelCellFlag.Ice && _cellFlagValues[row, column] < 1)
                    _cellFlagValues[row, column] = IceDefaultCount;

                // Tunnel set edildiğinde renk temizlenir (default direction zaten Front).
                if (bit == LevelCellFlag.Tunnel)
                {
                    _cellColors[row, column] = PieceColorType.None;
                    _cellSecondaryColors[row, column] = PieceColorType.None;
                }

                // Color Table is a table mechanic — clear plate-only flags on the same cell.
                if (bit == LevelCellFlag.Curtain)
                {
                    _cellFlags[row, column] &= ~(LevelCellFlag.Hidden | LevelCellFlag.Ice);
                    _cellFlagValues[row, column] = 0;
                }

                // Plate flags clear Color Table on the same cell.
                if (bit == LevelCellFlag.Hidden || bit == LevelCellFlag.Ice)
                    _cellFlags[row, column] &= ~LevelCellFlag.Curtain;
            }

            if (IsCarryBlockJamGrid())
            {
                SyncPlatePlacementsFromGrid();
                EditorUtility.SetDirty(_levelData);
                RequestScenePreviewRefresh();
            }
        }

        private bool IsTutorialClickableCell(int row, int column)
        {
            if (_levelData == null || !_levelData.hasTutorial) return false;
            if (_levelData.tutorialStages == null) return false;

            foreach (var stage in _levelData.tutorialStages)
            {
                if (stage?.clickableCells == null) continue;
                foreach (var c in stage.clickableCells)
                    if (c.x == row && c.y == column) return true;
            }
            return false;
        }

        private static string GetFlagsShortLabel(LevelCellFlag flags)
        {
            if (flags == LevelCellFlag.None) return "";
            var sb = new System.Text.StringBuilder(3);
            if ((flags & LevelCellFlag.Hidden) != 0) sb.Append('H');
            if ((flags & LevelCellFlag.Ice)    != 0) sb.Append('I');
            if ((flags & LevelCellFlag.Curtain) != 0) sb.Append('C');
            if ((flags & LevelCellFlag.Tunnel) != 0) sb.Append('T');
            return sb.ToString();
        }

        // ── Tutorial ─────────────────────────────────────────────────────────────────

        private void DrawTutorialSection()
        {
            GUILayout.Label("Tutorial", EditorStyles.boldLabel);

            _tutorialFoldout = EditorGUILayout.Foldout(_tutorialFoldout, "Tutorial Configuration",
                true, EditorStyles.foldoutHeader);
            if (!_tutorialFoldout) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            Undo.RecordObject(_levelData, "Edit Tutorial");

            _levelData.hasTutorial = EditorGUILayout.Toggle("Has Tutorial", _levelData.hasTutorial);

            if (_levelData.hasTutorial)
            {
                if (_levelData.tutorialStages == null)
                    _levelData.tutorialStages = new System.Collections.Generic.List<TutorialStage>();

                if (_levelData.tutorialStages.Count == 0)
                {
                    EditorGUILayout.HelpBox(
                        "Has Tutorial is on — add at least one stage so the hand can appear.",
                        MessageType.Warning);
                }

                if (GUILayout.Button("Clear Tutorial Completed Pref (force show again)", GUILayout.Height(20f)))
                {
                    TutorialManager.ResetCompletion(_levelData);
                    Debug.Log($"[Level Creator] Cleared tutorial completion for '{_levelData.name}'.");
                }

                GUILayout.Space(6f);
                _levelData.tutorialTextWorldPosition = EditorGUILayout.Vector3Field(
                    "Text Position", _levelData.tutorialTextWorldPosition);

                GUILayout.Space(6f);
                EditorGUILayout.LabelField(
                    $"Tutorial Stages ({_levelData.tutorialStages.Count})",
                    EditorStyles.miniBoldLabel);

                for (int i = 0; i < _levelData.tutorialStages.Count; i++)
                {
                    if (DrawTutorialStageEntry(i))
                    {
                        _levelData.tutorialStages.RemoveAt(i);
                        i--;
                    }
                }

                if (GUILayout.Button("+ Add Tutorial Stage", GUILayout.Height(22f)))
                {
                    _levelData.tutorialStages.Add(new TutorialStage
                    {
                        stageName = $"Stage {_levelData.tutorialStages.Count + 1}",
                        hideHand = false,
                        useGridHandPath = true,
                        startCell = new Vector2Int(Mathf.Max(0, _rows / 2), Mathf.Max(0, _columns / 2 - 1)),
                        targetCell = new Vector2Int(Mathf.Max(0, _rows / 2), Mathf.Min(_columns - 1, _columns / 2 + 1)),
                    });
                }
            }

            if (GUI.changed)
                EditorUtility.SetDirty(_levelData);

            EditorGUILayout.EndVertical();
        }

        private void DrawCarryBlockJamSection()
        {
            if (_levelDataSo == null || _levelDataSo.targetObject != _levelData)
                _levelDataSo = new SerializedObject(_levelData);

            GUILayout.Label("CarryBlockJam", EditorStyles.boldLabel);
            _carryBlockJamFoldout = EditorGUILayout.Foldout(
                _carryBlockJamFoldout,
                "CarryBlockJam Level Settings",
                true,
                EditorStyles.foldoutHeader);
            if (!_carryBlockJamFoldout)
                return;

            _levelDataSo.Update();

            SerializedProperty carryBlockJamProperty = _levelDataSo.FindProperty("carryBlockJam");
            if (carryBlockJamProperty == null)
            {
                EditorGUILayout.HelpBox(
                    "CarryBlockJam level settings could not be found on LevelData.",
                    MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.BeginChangeCheck();
            DrawCarryBlockJamTimerSettings(carryBlockJamProperty);
            EditorGUILayout.Space(6f);
            DrawCarryBlockJamTablePlacementHelp(carryBlockJamProperty);
            EditorGUILayout.HelpBox(
                "Normal plates: paint None + color on the grid, or use Plate Placements (row/column) below. " +
                "Frozen ice and Color Table badge look are shared on CarryBlockJamRuntimePieceSpawner (all levels). " +
                "Paint Ice cells (unlock moves) or Color Table cells (Accept Color).",
                MessageType.None);
            EditorGUILayout.Space(6f);

            SerializedProperty exitsProperty = carryBlockJamProperty.FindPropertyRelative("exits");
            CarryBlockJamFixedExitsEditorUtility.DrawExits(exitsProperty, _rows, _columns);
            EditorGUILayout.Space(6f);

            SerializedProperty hasTimerProperty = carryBlockJamProperty.FindPropertyRelative("hasTimer");
            SerializedProperty timeLimitProperty = carryBlockJamProperty.FindPropertyRelative("timeLimitSeconds");
            SerializedProperty disableAutoTablesProperty =
                carryBlockJamProperty.FindPropertyRelative("disableAutoTables");
            SerializedProperty stickmanSpawnModeProperty =
                carryBlockJamProperty.FindPropertyRelative("stickmanSpawnMode");
            SerializedProperty fixedStickmanCellProperty =
                carryBlockJamProperty.FindPropertyRelative("fixedStickmanCell");
            SerializedProperty platePlacementsProperty =
                carryBlockJamProperty.FindPropertyRelative("platePlacements");

            DrawStickmanSpawnSettings(stickmanSpawnModeProperty, fixedStickmanCellProperty);
            EditorGUILayout.Space(6f);

            if (platePlacementsProperty != null)
            {
                EditorGUILayout.LabelField("Plate Placements", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "Existing levels use this list (row / column / color / count). " +
                    "Edits here paint onto the grid; grid None+color cells sync back into this list on Save.",
                    MessageType.None);
                EditorGUILayout.PropertyField(platePlacementsProperty, true);
                EditorGUILayout.Space(6f);
            }

            DrawCarryBlockJamSettingsWithoutFrozenVisual(
                carryBlockJamProperty,
                exitsProperty,
                hasTimerProperty,
                timeLimitProperty,
                disableAutoTablesProperty,
                stickmanSpawnModeProperty,
                fixedStickmanCellProperty,
                platePlacementsProperty);
            bool changed = EditorGUI.EndChangeCheck();
            EditorGUILayout.EndVertical();

            if (changed)
            {
                _levelDataSo.ApplyModifiedProperties();
                // List edits (row/column) drive the grid — do not rebuild the list from the grid here.
                if (IsCarryBlockJamGrid())
                    ApplyPlatePlacementsToGrid();
                EditorUtility.SetDirty(_levelData);
                RequestScenePreviewRefresh();
            }
        }

        private static void DrawCarryBlockJamTimerSettings(SerializedProperty carryBlockJamProperty)
        {
            if (carryBlockJamProperty == null)
                return;

            SerializedProperty hasTimerProperty = carryBlockJamProperty.FindPropertyRelative("hasTimer");
            SerializedProperty timeLimitProperty = carryBlockJamProperty.FindPropertyRelative("timeLimitSeconds");
            if (hasTimerProperty == null || timeLimitProperty == null)
                return;

            EditorGUILayout.LabelField("Timer", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(hasTimerProperty, new GUIContent("Has Timer"));
            using (new EditorGUI.DisabledScope(!hasTimerProperty.boolValue))
            {
                EditorGUILayout.PropertyField(timeLimitProperty, new GUIContent("Time Limit (Seconds)"));
            }

            EditorGUILayout.HelpBox(
                "When enabled, a countdown is shown under the level UI. Time reaching zero fails the level.",
                MessageType.None);
            EditorGUILayout.EndVertical();
        }

        private static void DrawCarryBlockJamTablePlacementHelp(SerializedProperty carryBlockJamProperty)
        {
            if (carryBlockJamProperty == null)
                return;

            EditorGUILayout.LabelField("Tables", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            SerializedProperty disableAutoTablesProperty =
                carryBlockJamProperty.FindPropertyRelative("disableAutoTables");
            if (disableAutoTablesProperty != null)
            {
                EditorGUILayout.PropertyField(
                    disableAutoTablesProperty,
                    new GUIContent(
                        "No Auto Tables",
                        "When ticked, tables are not auto-generated from exits. " +
                        "Only manual tablePlacements and Color Table cells appear — or none. " +
                        "Hidden and Ice paint plates. Color Table paints a single-color accept table."));
            }

            bool noAuto = disableAutoTablesProperty != null && disableAutoTablesProperty.boolValue;
            EditorGUILayout.HelpBox(
                noAuto
                    ? "Auto table generation is off. Use tablePlacements or Color Table cells for tables. " +
                      "Paint normal plates on the grid with None + color. Hidden / Ice cells spawn special plates."
                    : "By default, missing tables are auto-generated to match exits. " +
                      "Paint normal plates on the grid with None + color. " +
                      "Hidden plates reveal when surrounding plates are collected. " +
                      "Frozen plates unlock after N collected plates. " +
                      "Color Table cells spawn a table that only accepts plates of the Accept Color.",
                MessageType.Info);

            EditorGUILayout.EndVertical();
        }

        private void DrawStickmanSpawnSettings(
            SerializedProperty stickmanSpawnModeProperty,
            SerializedProperty fixedStickmanCellProperty)
        {
            EditorGUILayout.LabelField("Stickman Spawn", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Row / Col are 0-based (0 = first cell).",
                MessageType.None);

            if (stickmanSpawnModeProperty != null)
                EditorGUILayout.PropertyField(stickmanSpawnModeProperty, new GUIContent("Spawn Mode"));

            if (fixedStickmanCellProperty == null)
                return;

            bool showFixed =
                stickmanSpawnModeProperty == null ||
                stickmanSpawnModeProperty.enumValueIndex == (int)CarryBlockJamStickmanSpawnMode.FixedCell;

            EditorGUI.BeginDisabledGroup(!showFixed);
            SerializedProperty rowProperty = fixedStickmanCellProperty.FindPropertyRelative("row");
            SerializedProperty columnProperty = fixedStickmanCellProperty.FindPropertyRelative("column");
            if (rowProperty != null && columnProperty != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Fixed Cell", GUILayout.Width(70f));
                EditorGUILayout.LabelField("Row", GUILayout.Width(28f));
                rowProperty.intValue = Mathf.Clamp(
                    EditorGUILayout.IntField(rowProperty.intValue, GUILayout.Width(48f)),
                    0,
                    Mathf.Max(0, _rows - 1));
                EditorGUILayout.LabelField("Col", GUILayout.Width(24f));
                columnProperty.intValue = Mathf.Clamp(
                    EditorGUILayout.IntField(columnProperty.intValue, GUILayout.Width(48f)),
                    0,
                    Mathf.Max(0, _columns - 1));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.EndDisabledGroup();
        }

        private static void DrawCarryBlockJamSettingsWithoutFrozenVisual(
            SerializedProperty carryBlockJamProperty,
            SerializedProperty exitsProperty = null,
            SerializedProperty hasTimerProperty = null,
            SerializedProperty timeLimitProperty = null,
            SerializedProperty disableAutoTablesProperty = null,
            SerializedProperty stickmanSpawnModeProperty = null,
            SerializedProperty fixedStickmanCellProperty = null,
            SerializedProperty platePlacementsProperty = null)
        {
            if (carryBlockJamProperty == null)
                return;

            SerializedProperty iterator = carryBlockJamProperty.Copy();
            SerializedProperty endProperty = iterator.GetEndProperty();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, endProperty))
            {
                enterChildren = false;
                if (exitsProperty != null && iterator.propertyPath == exitsProperty.propertyPath)
                    continue;
                if (hasTimerProperty != null && iterator.propertyPath == hasTimerProperty.propertyPath)
                    continue;
                if (timeLimitProperty != null && iterator.propertyPath == timeLimitProperty.propertyPath)
                    continue;
                if (disableAutoTablesProperty != null &&
                    iterator.propertyPath == disableAutoTablesProperty.propertyPath)
                    continue;
                if (stickmanSpawnModeProperty != null &&
                    iterator.propertyPath == stickmanSpawnModeProperty.propertyPath)
                    continue;
                if (fixedStickmanCellProperty != null &&
                    iterator.propertyPath == fixedStickmanCellProperty.propertyPath)
                    continue;
                // Normal plates are also drawn above via Plate Placements (row/column).
                // Skip the auto iterator copy so we do not show the list twice.
                if (platePlacementsProperty != null &&
                    iterator.propertyPath == platePlacementsProperty.propertyPath)
                    continue;

                EditorGUILayout.PropertyField(iterator, true);
            }
        }

        private void DrawPlateGateBalanceSummary()
        {
            if (!IsCarryBlockJamGrid() || _levelData?.carryBlockJam == null)
                return;

            // Flush pending CarryBlockJam inspector edits so gate totals stay current.
            if (_levelDataSo != null && _levelDataSo.targetObject == _levelData)
                _levelDataSo.ApplyModifiedProperties();

            EnsureGridSizes();

            var manualByColor = new Dictionary<PieceColorType, int>();
            var hiddenByColor = new Dictionary<PieceColorType, int>();
            var frozenByColor = new Dictionary<PieceColorType, int>();
            var curtainByColor = new Dictionary<PieceColorType, int>();
            var gridPlateByColor = new Dictionary<PieceColorType, int>();
            var authoredPlateByColor = new Dictionary<PieceColorType, int>();
            var autoByColor = new Dictionary<PieceColorType, int>();
            var gateByColor = new Dictionary<PieceColorType, int>();
            var manualCells = new Dictionary<Vector2Int, PieceColorType>();

            // Live grid None+color cells are normal plates (platePlacements is synced from these).
            CountNormalGridPlates(manualByColor, manualCells);

            int invalidHidden = 0;
            int invalidFrozen = 0;
            int invalidCurtain = 0;
            CountLiveGridMechanics(
                manualByColor,
                manualCells,
                hiddenByColor,
                frozenByColor,
                curtainByColor,
                gridPlateByColor,
                authoredPlateByColor,
                ref invalidHidden,
                ref invalidFrozen,
                ref invalidCurtain);
            CountGateRequirements(_levelData.carryBlockJam.exits, gateByColor);

            // Runtime only auto-fills when no normal/grid plates are authored yet.
            bool usesAutoGeneration = SumCounts(authoredPlateByColor) == 0;
            if (usesAutoGeneration)
            {
                foreach (KeyValuePair<PieceColorType, int> entry in gateByColor)
                {
                    authoredPlateByColor.TryGetValue(entry.Key, out int authored);
                    int autoCount = Mathf.Max(0, entry.Value - authored);
                    if (autoCount > 0)
                        autoByColor[entry.Key] = autoCount;
                }
            }

            var expectedPlateByColor = new Dictionary<PieceColorType, int>(authoredPlateByColor);
            foreach (KeyValuePair<PieceColorType, int> entry in autoByColor)
                AddColorCount(expectedPlateByColor, entry.Key, entry.Value);

            int manualTotal = SumCounts(manualByColor);
            int hiddenTotal = SumCounts(hiddenByColor);
            int frozenTotal = SumCounts(frozenByColor);
            int curtainTotal = SumCounts(curtainByColor);
            int gridPlateTotal = SumCounts(gridPlateByColor);
            int autoTotal = SumCounts(autoByColor);
            int authoredPlateTotal = SumCounts(authoredPlateByColor);
            int expectedPlateTotal = SumCounts(expectedPlateByColor);
            int gateTotal = SumCounts(gateByColor);

            List<PieceColorType> colors = CollectSortedColors(
                manualByColor,
                hiddenByColor,
                frozenByColor,
                curtainByColor,
                autoByColor,
                gateByColor);

            EditorGUILayout.LabelField("Plate / Gate Balance", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Sources", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField(
                $"Normal plates: {manualTotal}   |   Grid plates (Hidden/Frozen): {gridPlateTotal}");
            EditorGUILayout.LabelField(
                $"Hidden cells: {hiddenTotal}   |   Frozen cells: {frozenTotal}   |   Curtain tables: {curtainTotal}");
            EditorGUILayout.LabelField(
                usesAutoGeneration
                    ? $"Auto-generated plates (fill remaining gate labels): {autoTotal}"
                    : "Auto-generated plates: 0 (disabled while grid plates are authored)");

            MessageType totalMessageType = expectedPlateTotal == gateTotal
                ? MessageType.Info
                : MessageType.Error;
            EditorGUILayout.HelpBox(
                $"Expected plates: {expectedPlateTotal} " +
                $"(authored {authoredPlateTotal} + auto {autoTotal})   |   " +
                $"Gate labels: {gateTotal}   |   {FormatMatchStatus(expectedPlateTotal, gateTotal)}",
                totalMessageType);

            if (colors.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No authored plates, grid mechanics, or gate goals were found.",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Per Color", EditorStyles.miniBoldLabel);
                for (int i = 0; i < colors.Count; i++)
                {
                    PieceColorType color = colors[i];
                    manualByColor.TryGetValue(color, out int manual);
                    hiddenByColor.TryGetValue(color, out int hidden);
                    frozenByColor.TryGetValue(color, out int frozen);
                    curtainByColor.TryGetValue(color, out int curtain);
                    gridPlateByColor.TryGetValue(color, out int gridPlates);
                    autoByColor.TryGetValue(color, out int auto);
                    expectedPlateByColor.TryGetValue(color, out int expected);
                    gateByColor.TryGetValue(color, out int gates);

                    int difference = expected - gates;
                    MessageType messageType = difference == 0
                        ? MessageType.Info
                        : difference < 0
                            ? MessageType.Error
                            : MessageType.Warning;

                    EditorGUILayout.HelpBox(
                        $"{color}: plates {expected} = manual {manual} + grid {gridPlates} " +
                        $"(H{hidden}/F{frozen}) + auto {auto}   |   " +
                        $"gate labels {gates}   |   curtain tables {curtain}   |   " +
                        $"{FormatMatchStatus(expected, gates)}",
                        messageType);
                }
            }

            if (invalidHidden > 0 || invalidFrozen > 0 || invalidCurtain > 0)
            {
                EditorGUILayout.HelpBox(
                    $"Cells missing a color (will not spawn): " +
                    $"Hidden {invalidHidden}, Frozen {invalidFrozen}, Curtain {invalidCurtain}.",
                    MessageType.Warning);
            }

            if (usesAutoGeneration)
            {
                EditorGUILayout.HelpBox(
                    "No normal/Hidden/Frozen plates are painted yet, so runtime may fill remaining gate labels with auto plates.",
                    MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Paint normal plates with None + color on the grid. Hidden/Ice still add their own plates.",
                    MessageType.None);
            }

            EditorGUILayout.EndVertical();
        }

        private void CountNormalGridPlates(
            Dictionary<PieceColorType, int> counts,
            Dictionary<Vector2Int, PieceColorType> occupiedCells)
        {
            if (_cellColors == null || _cellFlags == null)
                return;

            int rows = Mathf.Min(_rows, _cellColors.GetLength(0));
            int columns = Mathf.Min(_columns, _cellColors.GetLength(1));
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    LevelCellFlag flags = _cellFlags[row, column];
                    if ((flags & (LevelCellFlag.Hidden |
                                  LevelCellFlag.Ice |
                                  LevelCellFlag.Curtain |
                                  LevelCellFlag.Tunnel)) != 0)
                        continue;

                    PieceColorType color = _cellColors[row, column];
                    if (!PieceColorPalette.IsPaintable(color))
                        continue;

                    AddColorCount(counts, color, 1);
                    if (occupiedCells != null)
                        occupiedCells[new Vector2Int(row, column)] = color;
                }
            }
        }

        private void CountLiveGridMechanics(
            Dictionary<PieceColorType, int> manualByColor,
            Dictionary<Vector2Int, PieceColorType> manualCells,
            Dictionary<PieceColorType, int> hiddenByColor,
            Dictionary<PieceColorType, int> frozenByColor,
            Dictionary<PieceColorType, int> curtainByColor,
            Dictionary<PieceColorType, int> gridPlateByColor,
            Dictionary<PieceColorType, int> authoredPlateByColor,
            ref int invalidHidden,
            ref int invalidFrozen,
            ref int invalidCurtain)
        {
            if (manualByColor != null)
            {
                foreach (KeyValuePair<PieceColorType, int> entry in manualByColor)
                    AddColorCount(authoredPlateByColor, entry.Key, entry.Value);
            }

            if (_cellColors == null || _cellFlags == null)
                return;

            int rows = Mathf.Min(_rows, _cellColors.GetLength(0));
            int columns = Mathf.Min(_columns, _cellColors.GetLength(1));

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    LevelCellFlag flags = _cellFlags[row, column];
                    PieceColorType color = _cellColors[row, column];
                    var cell = new Vector2Int(row, column);
                    PieceColorType manualColor = PieceColorType.None;
                    bool hasManualPlate = manualCells != null &&
                                          manualCells.TryGetValue(cell, out manualColor);
                    bool isPaintable = PieceColorPalette.IsPaintable(color);

                    if ((flags & LevelCellFlag.Hidden) != 0)
                    {
                        if (isPaintable)
                            AddColorCount(hiddenByColor, color, 1);
                        else if (hasManualPlate && PieceColorPalette.IsPaintable(manualColor))
                            AddColorCount(hiddenByColor, manualColor, 1);
                        else
                            invalidHidden++;
                    }

                    if ((flags & LevelCellFlag.Ice) != 0)
                    {
                        if (isPaintable)
                            AddColorCount(frozenByColor, color, 1);
                        else if (hasManualPlate && PieceColorPalette.IsPaintable(manualColor))
                            AddColorCount(frozenByColor, manualColor, 1);
                        else
                            invalidFrozen++;
                    }

                    if ((flags & LevelCellFlag.Curtain) != 0)
                    {
                        PieceColorType acceptColor = isPaintable
                            ? color
                            : (_cellSecondaryColors != null
                                ? _cellSecondaryColors[row, column]
                                : PieceColorType.None);
                        if (PieceColorPalette.IsPaintable(acceptColor))
                            AddColorCount(curtainByColor, acceptColor, 1);
                        else
                            invalidCurtain++;
                    }

                    // Hidden/Ice spawn an extra plate only when the cell has no manual placement.
                    bool spawnsFlagPlate =
                        !hasManualPlate &&
                        isPaintable &&
                        ((flags & (LevelCellFlag.Hidden | LevelCellFlag.Ice)) != 0);
                    if (spawnsFlagPlate)
                    {
                        AddColorCount(gridPlateByColor, color, 1);
                        AddColorCount(authoredPlateByColor, color, 1);
                    }
                }
            }
        }

        private static void CountGateRequirements(
            List<CarryBlockJamExitDefinition> exits,
            Dictionary<PieceColorType, int> counts)
        {
            if (exits == null)
                return;

            for (int exitIndex = 0; exitIndex < exits.Count; exitIndex++)
            {
                CarryBlockJamExitDefinition exit = exits[exitIndex];
                if (exit?.goals == null)
                    continue;

                for (int goalIndex = 0; goalIndex < exit.goals.Count; goalIndex++)
                {
                    CarryBlockJamExitGoal goal = exit.goals[goalIndex];
                    if (goal == null)
                        continue;

                    AddColorCount(
                        counts,
                        goal.color,
                        Mathf.Max(1, goal.requiredPlateCount));
                }
            }
        }

        private static List<PieceColorType> CollectSortedColors(
            params Dictionary<PieceColorType, int>[] sources)
        {
            var colors = new List<PieceColorType>();
            if (sources == null)
                return colors;

            for (int sourceIndex = 0; sourceIndex < sources.Length; sourceIndex++)
            {
                Dictionary<PieceColorType, int> source = sources[sourceIndex];
                if (source == null)
                    continue;

                foreach (PieceColorType color in source.Keys)
                {
                    if (!colors.Contains(color))
                        colors.Add(color);
                }
            }

            colors.Sort((left, right) => ((int)left).CompareTo((int)right));
            return colors;
        }

        private static string FormatMatchStatus(int plates, int gateLabels)
        {
            if (plates == gateLabels)
                return "MATCH";
            return plates < gateLabels
                ? $"SHORT BY {gateLabels - plates}"
                : $"EXTRA {plates - gateLabels}";
        }

        private static void AddColorCount(
            Dictionary<PieceColorType, int> counts,
            PieceColorType color,
            int amount)
        {
            if (counts == null ||
                amount <= 0 ||
                !PieceColorPalette.IsPaintable(color))
                return;

            counts.TryGetValue(color, out int current);
            counts[color] = current + amount;
        }

        private static int SumCounts(Dictionary<PieceColorType, int> counts)
        {
            int total = 0;
            if (counts == null)
                return total;

            foreach (int count in counts.Values)
                total += count;
            return total;
        }

        // Return true → caller bu stage'i listeden silmeli.
        private bool DrawTutorialStageEntry(int index)
        {
            var stage = _levelData.tutorialStages[index];

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Stage {index + 1}", EditorStyles.boldLabel, GUILayout.Width(60f));
            stage.stageName = EditorGUILayout.TextField("Name", stage.stageName);
            bool remove = GUILayout.Button("Remove", GUILayout.Width(60f));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Instruction");
            stage.instruction = EditorGUILayout.TextArea(stage.instruction, GUILayout.Height(40f));

            GUILayout.Space(4f);
            bool showHand = !stage.hideHand;
            showHand = EditorGUILayout.Toggle(
                new GUIContent(
                    "Show Hand",
                    "When off, this stage shows instruction text only (no hand / click point)."),
                showHand);
            stage.hideHand = !showHand;

            if (!showHand)
            {
                EditorGUILayout.HelpBox(
                    "Text-only stage: instruction stays on screen until the first click/swipe, " +
                    "then disappears. Stickman keeps free movement (no hand / path lock).",
                    MessageType.None);
            }
            else
            {
                stage.useGridHandPath = EditorGUILayout.Toggle(
                    new GUIContent("Use Grid Hand Path", "Loop hand between Start and Target grid cells."),
                    stage.useGridHandPath);

                if (stage.useGridHandPath)
                {
                    EditorGUILayout.LabelField("Start Cell (0-based Row / Col)", EditorStyles.miniBoldLabel);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Row", GUILayout.Width(28f));
                    int startRow = Mathf.Clamp(
                        EditorGUILayout.IntField(stage.startCell.x, GUILayout.Width(48f)),
                        0,
                        Mathf.Max(0, _rows - 1));
                    EditorGUILayout.LabelField("Col", GUILayout.Width(24f));
                    int startCol = Mathf.Clamp(
                        EditorGUILayout.IntField(stage.startCell.y, GUILayout.Width(48f)),
                        0,
                        Mathf.Max(0, _columns - 1));
                    EditorGUILayout.EndHorizontal();
                    stage.startCell = new Vector2Int(startRow, startCol);
                    stage.startPositionOffset = EditorGUILayout.Vector3Field(
                        "Start Position Offset",
                        stage.startPositionOffset);

                    GUILayout.Space(4f);
                    EditorGUILayout.LabelField("Target Cell (0-based Row / Col)", EditorStyles.miniBoldLabel);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Row", GUILayout.Width(28f));
                    int targetRow = Mathf.Clamp(
                        EditorGUILayout.IntField(stage.targetCell.x, GUILayout.Width(48f)),
                        0,
                        Mathf.Max(0, _rows - 1));
                    EditorGUILayout.LabelField("Col", GUILayout.Width(24f));
                    int targetCol = Mathf.Clamp(
                        EditorGUILayout.IntField(stage.targetCell.y, GUILayout.Width(48f)),
                        0,
                        Mathf.Max(0, _columns - 1));
                    EditorGUILayout.EndHorizontal();
                    stage.targetCell = new Vector2Int(targetRow, targetCol);
                    stage.targetPositionOffset = EditorGUILayout.Vector3Field(
                        "Target Position Offset",
                        stage.targetPositionOffset);

                    GUILayout.Space(4f);
                    stage.handMoveDuration = Mathf.Max(
                        0.05f,
                        EditorGUILayout.FloatField("Hand Move Duration", stage.handMoveDuration));
                    stage.handPauseAtEnds = Mathf.Max(
                        0f,
                        EditorGUILayout.FloatField("Pause At Ends", stage.handPauseAtEnds));
                }
                else
                {
                    stage.targetPos = EditorGUILayout.Vector3Field("Target Pos (World)", stage.targetPos);
                    stage.targetPositionOffset = EditorGUILayout.Vector3Field(
                        "Target Position Offset",
                        stage.targetPositionOffset);
                }

                stage.handRotation = EditorGUILayout.Vector3Field("Hand Rotation", stage.handRotation);
            }

            GUILayout.Space(4f);
            EditorGUILayout.LabelField("Clickable Cells (0-based row, col)", EditorStyles.miniBoldLabel);
            DrawClickableCellsList(stage.clickableCells);

            EditorGUILayout.EndVertical();
            GUILayout.Space(4f);

            return remove;
        }

        private void DrawClickableCellsList(System.Collections.Generic.List<Vector2Int> cells)
        {
            for (int j = 0; j < cells.Count; j++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"#{j + 1}", GUILayout.Width(28f));

                EditorGUILayout.LabelField("Row", GUILayout.Width(30f));
                int row = Mathf.Clamp(EditorGUILayout.IntField(cells[j].x, GUILayout.Width(40f)),
                    0, Mathf.Max(0, _rows - 1));

                EditorGUILayout.LabelField("Col", GUILayout.Width(30f));
                int col = Mathf.Clamp(EditorGUILayout.IntField(cells[j].y, GUILayout.Width(40f)),
                    0, Mathf.Max(0, _columns - 1));

                cells[j] = new Vector2Int(row, col);

                if (GUILayout.Button("×", GUILayout.Width(22f)))
                {
                    cells.RemoveAt(j);
                    j--;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Cell", GUILayout.Width(70f)))
                cells.Add(new Vector2Int(0, 0));
            if (GUILayout.Button("Front Mid", GUILayout.Width(80f)))
                cells.Add(new Vector2Int(Mathf.Max(0, _rows - 1), _columns / 2));
            if (GUILayout.Button("Back Mid", GUILayout.Width(80f)))
                cells.Add(new Vector2Int(0, _columns / 2));
            EditorGUILayout.EndHorizontal();
        }

        // ── Load / Save ──────────────────────────────────────────────────────────────

        private void LoadFromLevelManager()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Level Creator", "Enter Play Mode first.", "OK");
                return;
            }

            if (LevelManager.instance == null || LevelManager.instance.currentLevelData == null)
            {
                EditorUtility.DisplayDialog("Level Creator", "LevelManager has no current level.", "OK");
                return;
            }

            SetLevelData(LevelManager.instance.currentLevelData, true);
        }

        private void SetLevelData(LevelData levelData, bool rememberSelection)
        {
            _levelData = levelData;
            _levelDataSo = levelData != null ? new SerializedObject(levelData) : null;

            if (rememberSelection && _levelData != null)
            {
                string path = AssetDatabase.GetAssetPath(_levelData);
                if (!string.IsNullOrEmpty(path))
                    EditorPrefs.SetString(LastLevelDataKey, AssetDatabase.AssetPathToGUID(path));
            }

            if (_levelData != _lastLoadedLevelData)
            {
                if (_levelData != null)
                {
                    LoadGridFromLevel();
                }
                else
                {
                    _cellColors       = null;
                    _cellSecondaryColors = null;
                    _cellFlags        = null;
                    _cellFlagValues   = null;
                    _cellDirections   = null;
                    _cellTunnelPieces = null;
                }

                _lastLoadedLevelData = _levelData;
            }

            RequestScenePreviewRefresh();
            Repaint();
        }

        private void OnDisable()
        {
            _scenePreviewRefreshQueued = false;
        }

        private void RequestScenePreviewRefresh()
        {
            if (_scenePreviewRefreshQueued)
                return;

            _scenePreviewRefreshQueued = true;
            EditorApplication.delayCall += RunQueuedScenePreviewRefresh;
        }

        private void RunQueuedScenePreviewRefresh()
        {
            _scenePreviewRefreshQueued = false;
            if (this == null)
                return;

            RefreshCarryBlockJamScenePreview();
        }

        private void EnsureGridLoaded()
        {
            if (_levelData == null)
                return;

            if (_lastLoadedLevelData != _levelData ||
                _cellColors == null || _cellSecondaryColors == null || _cellFlags == null ||
                _cellFlagValues == null || _cellDirections == null ||
                _cellTunnelPieces == null)
            {
                LoadGridFromLevel();
                _lastLoadedLevelData = _levelData;
            }
        }

        private void LoadGridFromLevel()
        {
            _rows = Mathf.Max(1, _levelData.gridRows);
            _columns = Mathf.Max(1, _levelData.gridColumns);
            EnsureGridSizes();
            LevelCreatorUtility.ReadColorsIntoGrid(_levelData, _cellColors);
            LevelCreatorUtility.ReadSecondaryColorsIntoGrid(_levelData, _cellSecondaryColors);
            LevelCreatorUtility.ReadFlagsIntoGrid(_levelData, _cellFlags);
            LevelCreatorUtility.ReadFlagValuesIntoGrid(_levelData, _cellFlagValues);
            LevelCreatorUtility.ReadDirectionsIntoGrid(_levelData, _cellDirections);
            LevelCreatorUtility.ReadTunnelPiecesIntoGrid(_levelData, _cellTunnelPieces);

            // Legacy curtain cells may only have Collect in secondaryColor — promote to Accept Color.
            for (int row = 0; row < _rows; row++)
            {
                for (int column = 0; column < _columns; column++)
                {
                    if ((_cellFlags[row, column] & LevelCellFlag.Curtain) == 0)
                        continue;
                    if (_cellColors[row, column] != PieceColorType.None)
                        continue;
                    if (_cellSecondaryColors[row, column] == PieceColorType.None)
                        continue;
                    _cellColors[row, column] = _cellSecondaryColors[row, column];
                }
            }

            if (_levelData.carryBlockJam?.exits != null)
            {
                CarryBlockJamExitLayout.NormalizeExits(
                    _levelData.carryBlockJam.exits,
                    _rows,
                    _columns);
                EditorUtility.SetDirty(_levelData);
            }

            // Legacy / list-authored plates appear as normal grid colors (None tool).
            ApplyPlatePlacementsToGrid();
        }

        /// <summary>
        /// Paints platePlacements onto the color grid so designers edit them visually.
        /// Does not overwrite Hidden / Ice / Color Table / Tunnel cell colors.
        /// </summary>
        private void ApplyPlatePlacementsToGrid()
        {
            if (!IsCarryBlockJamGrid() ||
                _levelData?.carryBlockJam?.platePlacements == null ||
                _cellColors == null ||
                _cellFlags == null)
                return;

            List<CarryBlockJamPlatePlacement> placements = _levelData.carryBlockJam.platePlacements;
            for (int i = 0; i < placements.Count; i++)
            {
                CarryBlockJamPlatePlacement placement = placements[i];
                if (placement == null ||
                    !PieceColorPalette.IsPaintable(placement.color) ||
                    placement.row < 0 ||
                    placement.row >= _rows ||
                    placement.column < 0 ||
                    placement.column >= _columns)
                    continue;

                LevelCellFlag flags = _cellFlags[placement.row, placement.column];
                if ((flags & (LevelCellFlag.Hidden |
                              LevelCellFlag.Ice |
                              LevelCellFlag.Curtain |
                              LevelCellFlag.Tunnel)) != 0)
                    continue;

                _cellColors[placement.row, placement.column] = placement.color;
            }
        }

        /// <summary>
        /// Rebuilds platePlacements from None-flag grid cells that have a plate color.
        /// Hidden / Ice / Color Table / Tunnel cells are not written here.
        /// </summary>
        private void SyncPlatePlacementsFromGrid()
        {
            if (!IsCarryBlockJamGrid() || _levelData?.carryBlockJam == null)
                return;

            EnsureGridSizes();

            var previousCounts = new Dictionary<Vector2Int, int>();
            List<CarryBlockJamPlatePlacement> existing = _levelData.carryBlockJam.platePlacements;
            if (existing != null)
            {
                for (int i = 0; i < existing.Count; i++)
                {
                    CarryBlockJamPlatePlacement placement = existing[i];
                    if (placement == null)
                        continue;

                    previousCounts[new Vector2Int(placement.row, placement.column)] =
                        Mathf.Max(1, placement.count);
                }
            }

            var synced = new List<CarryBlockJamPlatePlacement>();
            for (int row = 0; row < _rows; row++)
            {
                for (int column = 0; column < _columns; column++)
                {
                    LevelCellFlag flags = _cellFlags[row, column];
                    if ((flags & (LevelCellFlag.Hidden |
                                  LevelCellFlag.Ice |
                                  LevelCellFlag.Curtain |
                                  LevelCellFlag.Tunnel)) != 0)
                        continue;

                    PieceColorType color = _cellColors[row, column];
                    if (!PieceColorPalette.IsPaintable(color))
                        continue;

                    var cell = new Vector2Int(row, column);
                    int count = previousCounts.TryGetValue(cell, out int previousCount)
                        ? previousCount
                        : 1;

                    synced.Add(new CarryBlockJamPlatePlacement
                    {
                        color = color,
                        row = row,
                        column = column,
                        count = count,
                    });
                }
            }

            _levelData.carryBlockJam.platePlacements = synced;
            EditorUtility.SetDirty(_levelData);
            if (_levelDataSo != null && _levelDataSo.targetObject == _levelData)
                _levelDataSo.Update();
        }

        private void EnsureGridSizes()
        {
            bool colorBad = _cellColors == null ||
                            _cellColors.GetLength(0) != _rows ||
                            _cellColors.GetLength(1) != _columns;

            bool secondaryBad = _cellSecondaryColors == null ||
                                _cellSecondaryColors.GetLength(0) != _rows ||
                                _cellSecondaryColors.GetLength(1) != _columns;

            bool flagBad = _cellFlags == null ||
                           _cellFlags.GetLength(0) != _rows ||
                           _cellFlags.GetLength(1) != _columns;

            bool valueBad = _cellFlagValues == null ||
                            _cellFlagValues.GetLength(0) != _rows ||
                            _cellFlagValues.GetLength(1) != _columns;

            bool dirBad = _cellDirections == null ||
                          _cellDirections.GetLength(0) != _rows ||
                          _cellDirections.GetLength(1) != _columns;

            bool tunnelBad = _cellTunnelPieces == null ||
                             _cellTunnelPieces.GetLength(0) != _rows ||
                             _cellTunnelPieces.GetLength(1) != _columns;

            if (colorBad || secondaryBad || flagBad || valueBad || dirBad || tunnelBad)
                ResizeGrids(_rows, _columns);
        }

        private void ResizeGrids(int rows, int columns)
        {
            var newColors  = new PieceColorType[rows, columns];
            var newSecondary = new PieceColorType[rows, columns];
            var newFlags   = new LevelCellFlag[rows, columns];
            var newValues  = new int[rows, columns];
            var newDirs    = new CellDirection[rows, columns];
            var newTunnels = new System.Collections.Generic.List<PieceColorType>[rows, columns];

            CopyGrid(_cellColors,       newColors);
            CopyGrid(_cellSecondaryColors, newSecondary);
            CopyGrid(_cellFlags,        newFlags);
            CopyGrid(_cellFlagValues,   newValues);
            CopyGrid(_cellDirections,   newDirs);
            CopyGrid(_cellTunnelPieces, newTunnels);

            // Init empty lists for any new cells.
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < columns; c++)
                    if (newTunnels[r, c] == null)
                        newTunnels[r, c] = new System.Collections.Generic.List<PieceColorType>();

            _cellColors       = newColors;
            _cellSecondaryColors = newSecondary;
            _cellFlags        = newFlags;
            _cellFlagValues   = newValues;
            _cellDirections   = newDirs;
            _cellTunnelPieces = newTunnels;
        }

        private static void CopyGrid<T>(T[,] from, T[,] to)
        {
            if (from == null) return;
            int copyRows = Mathf.Min(to.GetLength(0), from.GetLength(0));
            int copyColumns = Mathf.Min(to.GetLength(1), from.GetLength(1));
            for (int row = 0; row < copyRows; row++)
                for (int column = 0; column < copyColumns; column++)
                    to[row, column] = from[row, column];
        }

        private void ClearAllCells()
        {
            EnsureGridSizes();
            for (int row = 0; row < _rows; row++)
            {
                for (int column = 0; column < _columns; column++)
                {
                    _cellColors[row, column]     = PieceColorType.None;
                    _cellSecondaryColors[row, column] = PieceColorType.None;
                    _cellFlags[row, column]      = LevelCellFlag.None;
                    _cellFlagValues[row, column] = 0;
                    _cellDirections[row, column] = CellDirection.Front;
                    _cellTunnelPieces[row, column]?.Clear();
                }
            }
            if (IsCarryBlockJamGrid())
                SyncPlatePlacementsFromGrid();
            Repaint();
        }

        private void SaveLevel()
        {
            if (_levelData == null)
                return;

            EnsureGridSizes();

            // Flush pending SerializedObject edits BEFORE writing grid/plates.
            // Calling Update() first would discard those edits and could restore a
            // stale platePlacements list over the grid sync.
            if (_levelDataSo == null || _levelDataSo.targetObject != _levelData)
                _levelDataSo = new SerializedObject(_levelData);
            _levelDataSo.ApplyModifiedProperties();

            if (IsCarryBlockJamGrid())
                SyncPlatePlacementsFromGrid();

            LevelCreatorUtility.WriteGridToLevel(_levelData, _rows, _columns,
                _cellColors, _cellFlags, _cellFlagValues, _cellDirections, _cellTunnelPieces,
                _cellSecondaryColors);

            EditorUtility.SetDirty(_levelData);
            AssetDatabase.SaveAssets();

            // Refresh the SerializedObject from the saved asset; do not Apply afterward.
            _levelDataSo.Update();

            RefreshCarryBlockJamScenePreview();
            SaveDirtyGameplayScenes();
            Debug.Log(
                $"[Level Creator] Saved \"{_levelData.name}\" ({_rows}x{_columns}) and scene board preview.",
                _levelData);
        }

        private void ApplyToScene()
        {
            if (IsCarryBlockJamGrid())
                SyncPlatePlacementsFromGrid();
            SyncPreviewGridDimensions();
            EditorUtility.SetDirty(_levelData);
            if (CarryBlockJamSceneLevelApplicator.TryApply(_levelData, true, out string message))
            {
                SaveDirtyGameplayScenes();
                Debug.Log($"[Level Creator] {message}", _levelData);
            }
            else
                Debug.LogWarning($"[Level Creator] {message}", _levelData);
        }

        private static void SaveDirtyGameplayScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded || !scene.isDirty)
                    continue;

                EditorSceneManager.SaveScene(scene);
            }
        }

        private void SyncPreviewGridDimensions()
        {
            if (_levelData == null)
                return;

            _levelData.gridRows = _rows;
            _levelData.gridColumns = _columns;
        }

        private void RefreshCarryBlockJamScenePreview()
        {
            if (_levelData == null)
                return;

            if (IsCarryBlockJamGrid())
                SyncPlatePlacementsFromGrid();
            SyncPreviewGridDimensions();
            if (CarryBlockJamSceneLevelApplicator.TryApply(_levelData, out _))
                Repaint();
        }
    }
}
