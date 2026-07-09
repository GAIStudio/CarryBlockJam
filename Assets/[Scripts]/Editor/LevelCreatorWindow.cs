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

        private enum CellTool { None, Hidden, Ice, Tunnel }

        private const int IceDefaultCount = 1;

        private LevelData _levelData;
        private LevelData _lastLoadedLevelData;
        private SerializedObject _levelDataSo;

        private PieceColorType[,] _cellColors;
        private LevelCellFlag[,]  _cellFlags;
        private int[,]            _cellFlagValues;
        private CellDirection[,]  _cellDirections;
        private System.Collections.Generic.List<PieceColorType>[,] _cellTunnelPieces;
        private int _rows = 4;
        private int _columns = 4;

        private CellTool _activeTool = CellTool.None;

        private Vector2 _scroll;
        private bool _pinLoadAtTop;
        private bool _cameraFoldout = true;
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
            DrawCameraSection();
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
            if (!isSlide)
                DrawToolButton(CellTool.Tunnel, "Tunnel");
            GUILayout.EndHorizontal();

            string hint = _activeTool switch
            {
                CellTool.None => "Cell'e tıklayınca üzerindeki tüm flag'ler temizlenir. Renk dropdown'la seçilir.",
                CellTool.Hidden when _levelData != null && _levelData.mechanicType == PuzzleMechanicType.Grid =>
                    "Hidden cell + color spawns a hidden CarryBlockJam box at that cell. " +
                    "Other boxes still auto-fill to match exit count.",
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
                _cellColors[row, column] = (PieceColorType)EditorGUILayout.EnumPopup(
                    _cellColors[row, column],
                    GUILayout.Width(70f));
            }
            else
            {
                // Tunnel yönü dropdown'u.
                _cellDirections[row, column] = (CellDirection)EditorGUILayout.EnumPopup(
                    _cellDirections[row, column],
                    GUILayout.Width(70f));
            }

            // Ice cell ise count alanı.
            if ((flags & LevelCellFlag.Ice) == LevelCellFlag.Ice)
            {
                int prevValue = _cellFlagValues[row, column];
                int newValue  = EditorGUILayout.IntField(prevValue, GUILayout.Width(70f));
                if (newValue < 1) newValue = 1;
                if (newValue != prevValue)
                    _cellFlagValues[row, column] = newValue;
            }

            GUILayout.EndVertical();
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

                list[i] = (PieceColorType)EditorGUILayout.EnumPopup(list[i], GUILayout.Width(120f));
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
                    _cellColors[row, column] = PieceColorType.None;
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
                    _levelData.tutorialStages.Add(new TutorialStage());
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
            DrawCarryBlockJamBoxPlacementHelp(carryBlockJamProperty);
            EditorGUILayout.PropertyField(carryBlockJamProperty, true);
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

        private static void DrawCarryBlockJamBoxPlacementHelp(SerializedProperty carryBlockJamProperty)
        {
            if (carryBlockJamProperty == null)
                return;

            EditorGUILayout.HelpBox(
                "Box count follows exit count. Paint Hidden cells on the grid (set color below the cell) " +
                "or use boxPlacements for manual/hidden boxes. Missing boxes are auto-generated. " +
                "Hidden boxes stay grey until adjacent plates are collected.",
                MessageType.Info);
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

            stage.targetPos    = EditorGUILayout.Vector3Field("Target Pos",    stage.targetPos);
            stage.handRotation = EditorGUILayout.Vector3Field("Hand Rotation", stage.handRotation);

            GUILayout.Space(4f);
            EditorGUILayout.LabelField("Clickable Cells (row, col)", EditorStyles.miniBoldLabel);
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

        // ── Camera ───────────────────────────────────────────────────────────────────

        private void DrawCameraSection()
        {
            if (_levelDataSo == null || _levelDataSo.targetObject != _levelData)
                _levelDataSo = new SerializedObject(_levelData);

            _cameraFoldout = EditorGUILayout.Foldout(_cameraFoldout, "Camera", true, EditorStyles.foldoutHeader);
            if (!_cameraFoldout)
                return;

            _levelDataSo.Update();
            EditorGUILayout.PropertyField(_levelDataSo.FindProperty("cameraPosition"));
            EditorGUILayout.PropertyField(_levelDataSo.FindProperty("cameraRotation"));
            EditorGUILayout.PropertyField(_levelDataSo.FindProperty("cameraOrthographic"), new GUIContent("Orthographic"));
            if (_levelData.cameraOrthographic)
            {
                EditorGUILayout.PropertyField(_levelDataSo.FindProperty("cameraOrthographicSize"),
                    new GUIContent("Orthographic Size"));
            }
            else
            {
                EditorGUILayout.PropertyField(_levelDataSo.FindProperty("cameraFieldOfView"),
                    new GUIContent("Field Of View"));
            }

            _levelDataSo.ApplyModifiedProperties();
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
                _cellColors == null || _cellFlags == null ||
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
            LevelCreatorUtility.ReadFlagsIntoGrid(_levelData, _cellFlags);
            LevelCreatorUtility.ReadFlagValuesIntoGrid(_levelData, _cellFlagValues);
            LevelCreatorUtility.ReadDirectionsIntoGrid(_levelData, _cellDirections);
            LevelCreatorUtility.ReadTunnelPiecesIntoGrid(_levelData, _cellTunnelPieces);
        }

        private void EnsureGridSizes()
        {
            bool colorBad = _cellColors == null ||
                            _cellColors.GetLength(0) != _rows ||
                            _cellColors.GetLength(1) != _columns;

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

            if (colorBad || flagBad || valueBad || dirBad || tunnelBad)
                ResizeGrids(_rows, _columns);
        }

        private void ResizeGrids(int rows, int columns)
        {
            var newColors  = new PieceColorType[rows, columns];
            var newFlags   = new LevelCellFlag[rows, columns];
            var newValues  = new int[rows, columns];
            var newDirs    = new CellDirection[rows, columns];
            var newTunnels = new System.Collections.Generic.List<PieceColorType>[rows, columns];

            CopyGrid(_cellColors,       newColors);
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
                _cellColors, _cellFlags, _cellFlagValues, _cellDirections, _cellTunnelPieces);

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
