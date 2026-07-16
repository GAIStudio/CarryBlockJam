using UnityEditor;
using UnityEngine;
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
            DrawTunnelPiecesSection();
            GUILayout.Space(8f);
            DrawCarryBlockJamSection();
            GUILayout.Space(8f);
            DrawTutorialSection();

            EditorGUILayout.EndScrollView();

            EditorGUILayout.HelpBox(
                "Grid cell colors apply after Save. CarryBlockJam settings apply immediately. " +
                "Use Apply To Scene to rebuild the open scene board from the current level data.",
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
            int newRows = EditorGUILayout.IntSlider(rowsLabel, _rows, 1, 10);
            int newColumns = EditorGUILayout.IntSlider(colsLabel, _columns, 1, 10);
            if (EditorGUI.EndChangeCheck())
            {
                if (newRows != _rows || newColumns != _columns)
                {
                    _rows = newRows;
                    _columns = newColumns;
                    ResizeGrids(_rows, _columns);
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
            DrawColorGrid(isSlide);
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
            DrawToolButton(CellTool.Curtain, "Curtain");
            if (!isSlide)
                DrawToolButton(CellTool.Tunnel, "Tunnel");
            GUILayout.EndHorizontal();

            string hint = _activeTool switch
            {
                CellTool.None => "Cell'e tıklayınca üzerindeki tüm flag'ler temizlenir. Renk dropdown'la seçilir.",
                CellTool.Hidden when _levelData != null && _levelData.mechanicType == PuzzleMechanicType.Grid =>
                    "Hidden cell + color spawns a hidden CarryBlockJam table at that cell. " +
                    "It reveals when every plate on surrounding cells (including diagonals) is collected. " +
                    "With No Auto Tables off, other tables may still auto-fill to match exits.",
                CellTool.Ice when _levelData != null && _levelData.mechanicType == PuzzleMechanicType.Grid =>
                    "Ice cell + color spawns a frozen CarryBlockJam table. Set unlock moves below the cell. " +
                    "Each collected plate counts down until the table unlocks.",
                CellTool.Curtain when _levelData != null && _levelData.mechanicType == PuzzleMechanicType.Grid =>
                    "Curtain cell: set Table Color and Collect Color separately. " +
                    "Collect Color is the plate color that unlocks the curtain when delivered to its exit. " +
                    "Curtain look is shared on CarryBlockJamRuntimePieceSpawner.",
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
            GUILayout.BeginVertical(GUILayout.Width(70f));

            LevelCellFlag flags = _cellFlags[row, column];
            bool isTunnel = (flags & LevelCellFlag.Tunnel) == LevelCellFlag.Tunnel;

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

            // Flag overlay etiketleri (bitmask birleşik gösterimi: "H", "HI", "IT", "HIT" vb).
            if (flags != LevelCellFlag.None)
            {
                var labelStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white },
                };
                EditorGUI.LabelField(rect, GetFlagsShortLabel(flags), labelStyle);
            }

            // Tunnel cell renk gerektirmez (orada tunnel objesi spawn olur, piece değil).
            if (!isTunnel)
            {
                bool isCurtain = (flags & LevelCellFlag.Curtain) == LevelCellFlag.Curtain;
                if (isCurtain && IsCarryBlockJamGrid())
                {
                    EditorGUILayout.LabelField("Table", EditorStyles.miniLabel);
                    _cellColors[row, column] = TableColorEditorUtility.DrawPopupNoLabel(
                        _cellColors[row, column],
                        includeNone: true,
                        GUILayout.Width(70f));

                    EditorGUILayout.LabelField("Collect", EditorStyles.miniLabel);
                    _cellSecondaryColors[row, column] = PlateColorEditorUtility.DrawPopupNoLabel(
                        _cellSecondaryColors[row, column],
                        includeNone: true,
                        GUILayout.Width(70f));
                }
                else
                {
                    _cellColors[row, column] = DrawGridCellColorPopup(_cellColors[row, column], flags);
                }
            }
            else
            {
                // Tunnel yönü dropdown'u.
                _cellDirections[row, column] = (CellDirection)EditorGUILayout.EnumPopup(
                    _cellDirections[row, column],
                    GUILayout.Width(70f));
            }

            // Ice cell: unlock moves for frozen CarryBlockJam tables.
            if ((flags & LevelCellFlag.Ice) == LevelCellFlag.Ice)
            {
                bool isCarryBlockJamGrid = _levelData != null &&
                    _levelData.mechanicType == PuzzleMechanicType.Grid;
                if (isCarryBlockJamGrid)
                    EditorGUILayout.LabelField("Unlock Moves", EditorStyles.miniLabel);

                int prevValue = _cellFlagValues[row, column];
                int newValue = EditorGUILayout.IntField(prevValue, GUILayout.Width(70f));
                if (newValue < 1) newValue = 1;
                if (newValue != prevValue)
                    _cellFlagValues[row, column] = newValue;
            }

            GUILayout.EndVertical();
        }

        private bool IsCarryBlockJamGrid()
        {
            return _levelData != null && _levelData.mechanicType == PuzzleMechanicType.Grid;
        }

        private PieceColorType DrawGridCellColorPopup(PieceColorType current, LevelCellFlag flags)
        {
            if (!IsCarryBlockJamGrid())
                return (PieceColorType)EditorGUILayout.EnumPopup(current, GUILayout.Width(70f));

            // Hidden / Ice cell color paints the table material.
            // Curtain uses dedicated Table + Collect pickers in DrawCell.
            bool isTableCell =
                (flags & LevelCellFlag.Hidden) != 0 ||
                (flags & LevelCellFlag.Ice) != 0;

            if (isTableCell)
            {
                return TableColorEditorUtility.DrawPopupNoLabel(
                    current,
                    includeNone: true,
                    GUILayout.Width(70f));
            }

            return PlateColorEditorUtility.DrawPopupNoLabel(
                current,
                includeNone: true,
                GUILayout.Width(70f));
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

                // Curtain: seed collect color from table color when unset (old single-color levels).
                if (bit == LevelCellFlag.Curtain &&
                    _cellSecondaryColors[row, column] == PieceColorType.None &&
                    _cellColors[row, column] != PieceColorType.None)
                {
                    _cellSecondaryColors[row, column] = _cellColors[row, column];
                }
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
                "Frozen ice and curtain look are shared on CarryBlockJamRuntimePieceSpawner (all levels). " +
                "In Level Creator only paint Ice/Curtain cells (and unlock moves / table + collect colors).",
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

            DrawStickmanSpawnSettings(stickmanSpawnModeProperty, fixedStickmanCellProperty);
            EditorGUILayout.Space(6f);

            DrawCarryBlockJamSettingsWithoutFrozenVisual(
                carryBlockJamProperty,
                exitsProperty,
                hasTimerProperty,
                timeLimitProperty,
                disableAutoTablesProperty,
                stickmanSpawnModeProperty,
                fixedStickmanCellProperty);
            bool changed = EditorGUI.EndChangeCheck();
            EditorGUILayout.EndVertical();

            if (changed)
            {
                _levelDataSo.ApplyModifiedProperties();
                EditorUtility.SetDirty(_levelData);
                RequestScenePreviewRefresh();
            }
            else
            {
                _levelDataSo.ApplyModifiedPropertiesWithoutUndo();
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
                        "Only manual / painted tables (Hidden, Ice, Curtain, tablePlacements) appear — or none."));
            }

            bool noAuto = disableAutoTablesProperty != null && disableAutoTablesProperty.boolValue;
            EditorGUILayout.HelpBox(
                noAuto
                    ? "Auto table generation is off. Paint Hidden/Ice/Curtain cells (with color) or leave the grid empty for no tables."
                    : "By default, missing tables are auto-generated to match exits. Paint Hidden/Ice/Curtain cells or use tablePlacements for manual tables. " +
                      "Hidden tables reveal when all surrounding plates are collected (including diagonals). Frozen tables unlock after N collected plates. " +
                      "Curtain tables open when all plates of the Collect Color are delivered to the matching exit.",
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
            SerializedProperty fixedStickmanCellProperty = null)
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

                EditorGUILayout.PropertyField(iterator, true);
            }
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

            // Old curtain cells only stored one color — seed Collect from Table when missing.
            for (int row = 0; row < _rows; row++)
            {
                for (int column = 0; column < _columns; column++)
                {
                    if ((_cellFlags[row, column] & LevelCellFlag.Curtain) == 0)
                        continue;
                    if (_cellSecondaryColors[row, column] != PieceColorType.None)
                        continue;
                    if (_cellColors[row, column] == PieceColorType.None)
                        continue;
                    _cellSecondaryColors[row, column] = _cellColors[row, column];
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
            Repaint();
        }

        private void SaveLevel()
        {
            if (_levelData == null)
                return;

            EnsureGridSizes();
            LevelCreatorUtility.WriteGridToLevel(_levelData, _rows, _columns,
                _cellColors, _cellFlags, _cellFlagValues, _cellDirections, _cellTunnelPieces,
                _cellSecondaryColors);

            if (_levelDataSo != null)
            {
                _levelDataSo.Update();
                _levelDataSo.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(_levelData);
            AssetDatabase.SaveAssets();
            RefreshCarryBlockJamScenePreview();
            Debug.Log($"[Level Creator] Saved \"{_levelData.name}\".", _levelData);
        }

        private void ApplyToScene()
        {
            SyncPreviewGridDimensions();
            if (CarryBlockJamSceneLevelApplicator.TryApply(_levelData, true, out string message))
                Debug.Log($"[Level Creator] {message}", _levelData);
            else
                Debug.LogWarning($"[Level Creator] {message}", _levelData);
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

            SyncPreviewGridDimensions();
            if (CarryBlockJamSceneLevelApplicator.TryApply(_levelData, out _))
                Repaint();
        }
    }
}
