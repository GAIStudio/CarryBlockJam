using CarryBlockJam;
using GAITemplate;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CarryBlockJam.Editor
{
    public static class CarryBlockJamSimpleBoardBuilder
    {
        private const string ScenePath = "Assets/[Scenes]/SampleScene.unity";
        private const string CellPrefabPath = "Assets/[Prefabs]/GamePiece.prefab";
        private const string LevelConfigPath = "Assets/[LevelDatas]/LevelConfig.asset";

        [MenuItem("CarryBlockJam/Build 6x6 Board In SampleScene")]
        public static void BuildInSampleSceneMenu()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[CarryBlockJam] Could not open {ScenePath}");
                return;
            }

            CarryBlockJamSimpleBoard board = Object.FindObjectOfType<CarryBlockJamSimpleBoard>();
            if (board == null)
                board = CreateBoardRoot();

            ApplyDefaultSettings(board);
            BuildBoard(board);
            EnsureMainCamera();
            DisableRuntimeLevelSpawn();

            if (!Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        public static void BuildBoard(CarryBlockJamSimpleBoard board)
        {
            BuildBoard(board, null);
        }

        public static void BuildBoard(CarryBlockJamSimpleBoard board, LevelData levelData)
        {
            if (board == null)
                return;

            if (!Application.isPlaying)
                Undo.RegisterFullObjectHierarchyUndo(board.gameObject, "Build CarryBlockJam Board");
            ApplyLevelPreview(board, levelData);

            GameObject cellPrefab = board.CellPrefab;
            if (cellPrefab == null)
                cellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CellPrefabPath);

            if (cellPrefab == null)
            {
                Debug.LogError($"[CarryBlockJam] Missing cell prefab at {CellPrefabPath}");
                return;
            }

            EnsureRoots(board);

            PuzzleGrid grid = board.GetComponent<PuzzleGrid>();
            if (grid == null)
                grid = board.gameObject.AddComponent<PuzzleGrid>();

            grid.BeginLayout(board.Rows, board.Columns, board.GridSpacingX, board.GridSpacingZ);
            BuildCells(board, grid, board.CellsRoot, cellPrefab, board.CellScaleXYZ, board.CellColor);
            BuildExits(board, grid, board.ExitsRoot, levelData);
            grid.EndLayout();

            EditorUtility.SetDirty(board);
            if (!Application.isPlaying && board.gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(board.gameObject.scene);
        }

        public static void ClearBoard(CarryBlockJamSimpleBoard board)
        {
            if (board == null)
                return;

            if (board.CellsRoot != null)
                ClearChildren(board.CellsRoot);

            if (board.ExitsRoot != null)
                ClearChildren(board.ExitsRoot);

            EditorUtility.SetDirty(board);
            if (!Application.isPlaying && board.gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(board.gameObject.scene);
        }

        private static CarryBlockJamSimpleBoard CreateBoardRoot()
        {
            var boardObject = new GameObject("CarryBlockJamBoard");
            boardObject.AddComponent<PuzzleGrid>();
            CarryBlockJamSimpleBoard board = boardObject.AddComponent<CarryBlockJamSimpleBoard>();
            ApplyDefaultSettings(board);
            EnsureRoots(board);
            return board;
        }

        private static void ApplyDefaultSettings(CarryBlockJamSimpleBoard board)
        {
            SerializedObject serializedBoard = new SerializedObject(board);
            serializedBoard.FindProperty("rows").intValue = CarryBlockJamSimpleBoard.DefaultRows;
            serializedBoard.FindProperty("columns").intValue = CarryBlockJamSimpleBoard.DefaultColumns;
            serializedBoard.FindProperty("gridSpacingX").floatValue = CarryBlockJamSimpleBoard.DefaultSpacing;
            serializedBoard.FindProperty("gridSpacingZ").floatValue = CarryBlockJamSimpleBoard.DefaultSpacing;
            serializedBoard.FindProperty("cellScale").vector3Value = CarryBlockJamSimpleBoard.DefaultCellScale;
            serializedBoard.FindProperty("cellColor").enumValueIndex = (int)PieceColorType.Grey;
            SerializedProperty exitsProperty = serializedBoard.FindProperty("exits");
            if (exitsProperty != null)
                exitsProperty.arraySize = 0;

            if (serializedBoard.FindProperty("cellPrefab").objectReferenceValue == null)
            {
                serializedBoard.FindProperty("cellPrefab").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>(CellPrefabPath);
            }

            serializedBoard.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ApplyLevelPreview(CarryBlockJamSimpleBoard board, LevelData levelData)
        {
            if (board == null || levelData == null || levelData.carryBlockJam == null)
                return;

            SerializedObject serializedBoard = new SerializedObject(board);
            serializedBoard.FindProperty("rows").intValue = Mathf.Max(1, levelData.gridRows);
            serializedBoard.FindProperty("columns").intValue = Mathf.Max(1, levelData.gridColumns);
            serializedBoard.FindProperty("gridSpacingX").floatValue = Mathf.Max(0.01f, levelData.carryBlockJam.gridSpacingX);
            serializedBoard.FindProperty("gridSpacingZ").floatValue = Mathf.Max(0.01f, levelData.carryBlockJam.gridSpacingZ);
            serializedBoard.FindProperty("cellScale").vector3Value = levelData.carryBlockJam.gridCellScale;
            serializedBoard.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureRoots(CarryBlockJamSimpleBoard board)
        {
            SerializedObject serializedBoard = new SerializedObject(board);

            Transform cellsRoot = board.CellsRoot != null ? board.CellsRoot : board.transform.Find("Cells");
            if (cellsRoot == null)
                cellsRoot = CreateChild(board.transform, "Cells");

            Transform exitsRoot = board.ExitsRoot != null ? board.ExitsRoot : board.transform.Find("Exits");
            if (exitsRoot == null)
                exitsRoot = CreateChild(board.transform, "Exits");

            serializedBoard.FindProperty("cellsRoot").objectReferenceValue = cellsRoot;
            serializedBoard.FindProperty("exitsRoot").objectReferenceValue = exitsRoot;
            serializedBoard.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildCells(
            CarryBlockJamSimpleBoard board,
            PuzzleGrid grid,
            Transform cellsRoot,
            GameObject cellPrefab,
            Vector3 cellScale,
            PieceColorType cellColor)
        {
            ClearChildren(cellsRoot);

            for (int row = 0; row < grid.Rows; row++)
            {
                for (int col = 0; col < grid.Columns; col++)
                {
                    Vector3 localPos = grid.GetLocalPosition(row, col);

                    var slot = new GameObject($"Cell_{row}_{col}");
                    slot.transform.SetParent(cellsRoot, false);
                    slot.transform.localPosition = localPos;
                    slot.transform.localRotation = Quaternion.identity;
                    slot.transform.localScale = Vector3.one;

                    GameObject visual = PrefabUtility.InstantiatePrefab(cellPrefab, slot.transform) as GameObject;
                    if (visual == null)
                        visual = Object.Instantiate(cellPrefab, slot.transform);

                    visual.name = cellPrefab.name;
                    Transform visualTransform = visual.transform;
                    visualTransform.localPosition = Vector3.zero;
                    visualTransform.localRotation = Quaternion.identity;
                    visualTransform.localScale = cellScale;

                    GamePiece piece = visual.GetComponent<GamePiece>();
                    if (piece != null)
                        piece.ApplyColor(cellColor);

                    grid.RegisterCell(row, col, slot.transform, false);
                }
            }

            EditorUtility.SetDirty(board);
        }

        private static void BuildExits(
            CarryBlockJamSimpleBoard board,
            PuzzleGrid grid,
            Transform exitsRoot,
            LevelData levelData)
        {
            ClearChildren(exitsRoot);

            if (levelData?.carryBlockJam?.exits == null)
                return;

            for (int i = 0; i < levelData.carryBlockJam.exits.Count; i++)
                BuildExit(board, grid, exitsRoot, levelData.carryBlockJam.exits[i], i);
        }

        private static void BuildExit(
            CarryBlockJamSimpleBoard board,
            PuzzleGrid grid,
            Transform exitsRoot,
            CarryBlockJamExitDefinition definition,
            int index)
        {
            if (grid == null || exitsRoot == null || definition == null)
                return;

            PieceColorType color = PieceColorType.None;
            if (definition.goals != null && definition.goals.Count > 0 && definition.goals[0] != null)
                color = definition.goals[0].color;

            Vector3 localPos = GetExitLocalPosition(grid, definition) + definition.positionOffset;
            Quaternion localRot = Quaternion.Euler(definition.rotation);

            var exitObject = new GameObject($"Exit_{definition.side}_{definition.startIndex}_{index}");
            exitObject.transform.SetParent(exitsRoot, false);
            exitObject.transform.localPosition = localPos;
            exitObject.transform.localRotation = localRot;
            exitObject.transform.localScale = Vector3.one;

            GameObject gate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gate.name = "Gate";
            gate.transform.SetParent(exitObject.transform, false);
            gate.transform.localPosition = Vector3.zero;
            gate.transform.localRotation = Quaternion.identity;
            gate.transform.localScale = GetExitLocalScale(grid, definition.side, definition.length);
            ApplyColor(gate, color);
            Object.DestroyImmediate(gate.GetComponent<Collider>());

            CarryBlockJamExit exit = exitObject.AddComponent<CarryBlockJamExit>();
            exit.Configure(definition);

            TMP_Text label = CarryBlockJamExitLabelUtility.EnsureGoalLabel(
                exitObject.transform,
                definition.side,
                board != null ? board.ExitLabel : null);

            exit.BindVisuals(
                EnsureGamePiece(gate),
                null,
                label,
                board != null ? board.ExitLabel : null);
        }

        private static GamePiece EnsureGamePiece(GameObject target)
        {
            if (target == null)
                return null;

            GamePiece piece = target.GetComponent<GamePiece>();
            if (piece == null)
                piece = target.AddComponent<GamePiece>();

            return piece;
        }

        private static void ApplyColor(GameObject target, PieceColorType color)
        {
            if (target == null || !PieceColorPalette.IsPaintable(color))
                return;

            Renderer renderer = target.GetComponent<Renderer>();
            Material material = PieceColorPalette.GetMaterial(color);
            if (renderer != null && material != null)
                renderer.sharedMaterial = material;
        }

        private static Vector3 GetExitLocalPosition(PuzzleGrid grid, CarryBlockJamExitDefinition definition)
        {
            float centerIndex = definition.startIndex + (Mathf.Max(1, definition.length) - 1) * 0.5f;

            switch (definition.side)
            {
                case BoardBorderSide.Left:
                    return grid.GetLocalPosition(Mathf.RoundToInt(centerIndex), 0) +
                           new Vector3(-grid.GridSpacingX * 0.58f, 0.375f, 0f);
                case BoardBorderSide.Right:
                    return grid.GetLocalPosition(Mathf.RoundToInt(centerIndex), grid.Columns - 1) +
                           new Vector3(grid.GridSpacingX * 0.58f, 0.375f, 0f);
                case BoardBorderSide.Top:
                    return grid.GetLocalPosition(0, Mathf.RoundToInt(centerIndex)) +
                           new Vector3(0f, 0.375f, grid.GridSpacingZ * 0.58f);
                case BoardBorderSide.Bottom:
                    return grid.GetLocalPosition(grid.Rows - 1, Mathf.RoundToInt(centerIndex)) +
                           new Vector3(0f, 0.375f, -grid.GridSpacingZ * 0.58f);
                default:
                    return Vector3.zero;
            }
        }

        private static Vector3 GetExitLocalScale(PuzzleGrid grid, BoardBorderSide side, int length)
        {
            int resolvedLength = Mathf.Max(1, length);
            return side switch
            {
                BoardBorderSide.Left or BoardBorderSide.Right =>
                    new Vector3(0.16f, 0.5f, grid.GridSpacingZ * resolvedLength * 0.94f),
                BoardBorderSide.Top or BoardBorderSide.Bottom =>
                    new Vector3(grid.GridSpacingX * resolvedLength * 0.94f, 0.5f, 0.16f),
                _ => Vector3.one,
            };
        }

        private static void EnsureMainCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.transform.SetPositionAndRotation(
                new Vector3(0f, 16f, -8f),
                Quaternion.Euler(52f, 0f, 0f));
            camera.orthographic = true;
            camera.orthographicSize = 8.5f;
        }

        private static void DisableRuntimeLevelSpawn()
        {
            var levelConfig = AssetDatabase.LoadAssetAtPath<LevelConfig>(LevelConfigPath);
            if (levelConfig == null)
                return;

            levelConfig.autoSpawnLevel = false;
            EditorUtility.SetDirty(levelConfig);
        }

        private static Transform CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }
    }
}
