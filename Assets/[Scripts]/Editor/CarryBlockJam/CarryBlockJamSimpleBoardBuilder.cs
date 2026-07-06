using CarryBlockJam;
using GAITemplate;
using System.Collections.Generic;
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

        private const float ExitInset = 0.58f;
        private const float ExitHeight = 0.5f;
        private const float ExitThickness = 0.16f;

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

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public static void BuildBoard(CarryBlockJamSimpleBoard board)
        {
            if (board == null)
                return;

            Undo.RegisterFullObjectHierarchyUndo(board.gameObject, "Build CarryBlockJam Board");

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
            BuildExits(board, grid, board.ExitsRoot);
            grid.EndLayout();

            EditorUtility.SetDirty(board);
            if (board.gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(board.gameObject.scene);

            Debug.Log($"[CarryBlockJam] Built {board.Rows}x{board.Columns} board.");
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
            if (board.gameObject.scene.IsValid())
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

            SerializedProperty exits = serializedBoard.FindProperty("exits");
            exits.arraySize = 0;
            AddExit(exits, BoardExitSettings.CreateLeftDefault());
            AddExit(exits, BoardExitSettings.CreateRightDefault());

            if (serializedBoard.FindProperty("cellPrefab").objectReferenceValue == null)
            {
                serializedBoard.FindProperty("cellPrefab").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>(CellPrefabPath);
            }

            serializedBoard.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ApplyExitDefaults(SerializedProperty exitProperty, BoardExitSettings defaults)
        {
            if (exitProperty == null || defaults == null)
                return;

            exitProperty.FindPropertyRelative("side").enumValueIndex = (int)defaults.side;
            exitProperty.FindPropertyRelative("startIndex").intValue = defaults.startIndex;
            exitProperty.FindPropertyRelative("length").intValue = defaults.length;
            exitProperty.FindPropertyRelative("color").enumValueIndex = (int)defaults.color;
            exitProperty.FindPropertyRelative("positionOffset").vector3Value = defaults.positionOffset;
            exitProperty.FindPropertyRelative("rotation").vector3Value = defaults.rotation;
        }

        private static void AddExit(SerializedProperty exitsProperty, BoardExitSettings defaults)
        {
            int index = exitsProperty.arraySize;
            exitsProperty.InsertArrayElementAtIndex(index);
            ApplyExitDefaults(exitsProperty.GetArrayElementAtIndex(index), defaults);
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

        private static void BuildExits(CarryBlockJamSimpleBoard board, PuzzleGrid grid, Transform exitsRoot)
        {
            ClearChildren(exitsRoot);

            IReadOnlyList<BoardExitSettings> exits = board.Exits;
            if (exits == null)
                return;

            for (int i = 0; i < exits.Count; i++)
                BuildExit(grid, exitsRoot, exits[i]);
        }

        private static void BuildExit(
            PuzzleGrid grid,
            Transform exitsRoot,
            BoardExitSettings settings)
        {
            if (settings == null)
                return;

            Vector3 localPos = GetExitLocalPosition(grid, settings.side, settings) + settings.positionOffset;
            Quaternion localRot = Quaternion.Euler(settings.rotation);

            var exitObject = new GameObject($"Exit_{settings.side}_{settings.startIndex}_{settings.color}");
            exitObject.transform.SetParent(exitsRoot, false);
            exitObject.transform.localPosition = localPos;
            exitObject.transform.localRotation = localRot;
            exitObject.transform.localScale = Vector3.one;

            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Visual";
            visual.transform.SetParent(exitObject.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = GetExitLocalScale(grid, settings.side, settings.length);

            Material material = PieceColorPalette.GetMaterial(settings.color);
            if (material != null)
                visual.GetComponent<Renderer>().sharedMaterial = material;

            Object.DestroyImmediate(visual.GetComponent<Collider>());
            AddChevrons(exitObject.transform, settings.rotation);
        }

        private static Vector3 GetExitLocalPosition(PuzzleGrid grid, BoardBorderSide side, BoardExitSettings settings)
        {
            float sx = grid.GridSpacingX;
            float centerIndex = settings.startIndex + (settings.length - 1) * 0.5f;

            switch (side)
            {
                case BoardBorderSide.Left:
                {
                    int row = Mathf.RoundToInt(centerIndex);
                    return grid.GetLocalPosition(row, 0) + new Vector3(-sx * ExitInset, ExitHeight * 0.75f, 0f);
                }
                case BoardBorderSide.Right:
                {
                    int row = Mathf.RoundToInt(centerIndex);
                    return grid.GetLocalPosition(row, grid.Columns - 1) + new Vector3(sx * ExitInset, ExitHeight * 0.75f, 0f);
                }
                case BoardBorderSide.Top:
                {
                    int col = Mathf.RoundToInt(centerIndex);
                    return grid.GetLocalPosition(0, col) + new Vector3(0f, ExitHeight * 0.75f, grid.GridSpacingZ * ExitInset);
                }
                case BoardBorderSide.Bottom:
                {
                    int col = Mathf.RoundToInt(centerIndex);
                    return grid.GetLocalPosition(grid.Rows - 1, col) + new Vector3(0f, ExitHeight * 0.75f, -grid.GridSpacingZ * ExitInset);
                }
                default:
                    return Vector3.zero;
            }
        }

        private static Vector3 GetExitLocalScale(PuzzleGrid grid, BoardBorderSide side, int length)
        {
            float sx = grid.GridSpacingX;
            float sz = grid.GridSpacingZ;

            return side switch
            {
                BoardBorderSide.Left or BoardBorderSide.Right =>
                    new Vector3(ExitThickness, ExitHeight, sz * length * 0.94f),
                BoardBorderSide.Top or BoardBorderSide.Bottom =>
                    new Vector3(sx * length * 0.94f, ExitHeight, ExitThickness),
                _ => Vector3.one,
            };
        }

        private static void AddChevrons(Transform exitRoot, Vector3 exitRotation)
        {
            Material chevronMat = PieceColorPalette.GetMaterial(PieceColorType.White);
            if (chevronMat == null)
            {
                chevronMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                chevronMat.color = Color.white;
            }

            float spacing = 0.1f;
            float size = 0.065f;
            float yaw = exitRotation.y;

            for (int i = 0; i < 2; i++)
            {
                var chevron = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chevron.name = $"Chevron_{i}";
                chevron.transform.SetParent(exitRoot, false);
                Object.DestroyImmediate(chevron.GetComponent<Collider>());
                chevron.GetComponent<Renderer>().sharedMaterial = chevronMat;

                float lane = (i - 0.5f) * spacing;
                float forwardSign = Mathf.Abs(Mathf.DeltaAngle(yaw, 90f)) < Mathf.Abs(Mathf.DeltaAngle(yaw, 270f)) ? 1f : -1f;
                chevron.transform.localPosition = new Vector3(0.1f * forwardSign, 0.02f, lane);
                chevron.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                chevron.transform.localScale = new Vector3(size, size * 1.4f, size * 0.35f);
            }
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
