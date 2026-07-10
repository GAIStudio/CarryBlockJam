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
        private const string CellPrefabPath = "Assets/[Models]/M_GridCell.fbx";
        private const string CellMaterialPath = "Assets/[Materials]/Mat_Gridcell.mat";
        private const string GridWallPrefabPath = "Assets/[Models]/M_Gridwall.fbx";
        private const string GridPrefabPath = "Assets/[Models]/M_Grid.fbx";
        private const string GridBottomPrefabPath = "Assets/[Models]/M_GridBottom.fbx";
        private const string GridWallMaterialPath = "Assets/[Materials]/Mat_GridWall.mat";
        private const string GateUpPrefabPath = "Assets/[Models]/M_GateUp.fbx";
        private const string GateBottomPrefabPath = "Assets/[Models]/M_GateBottom.fbx";
        private const string LevelConfigPath = "Assets/[LevelDatas]/LevelConfig.asset";

        /// <summary>
        /// World position that centers the board on ArtScene's dark BG pit
        /// (Art-Environment at (-2.5, 0.15, -9) + grid center local (2.5, 0, 4.5)).
        /// </summary>
        private static readonly Vector3 ArtGridBoardPosition = new Vector3(0f, 0.15f, -4.5f);

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
            AlignBoardToArtGrid(board);
            EnsureBoardGridFrame(board);
            EnsureBoardGates(board);

            GameObject cellPrefab = board.CellPrefab;
            if (cellPrefab == null)
                cellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CellPrefabPath);

            if (cellPrefab == null)
            {
                Debug.LogError($"[CarryBlockJam] Missing cell prefab at {CellPrefabPath}");
                return;
            }

            Material cellMaterial = board.CellMaterial;
            if (cellMaterial == null)
                cellMaterial = AssetDatabase.LoadAssetAtPath<Material>(CellMaterialPath);

            EnsureRoots(board);

            PuzzleGrid grid = board.GetComponent<PuzzleGrid>();
            if (grid == null)
                grid = board.gameObject.AddComponent<PuzzleGrid>();

            grid.BeginLayout(board.Rows, board.Columns, board.GridSpacingX, board.GridSpacingZ);
            BuildCells(board, grid, board.CellsRoot, cellPrefab, board.CellScaleXYZ, cellMaterial);
            BuildExits(board, grid, board.ExitsRoot, levelData);
            grid.EndLayout();

            EditorUtility.SetDirty(board);
            if (!Application.isPlaying && board.gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(board.gameObject.scene);
        }

        private static void AlignBoardToArtGrid(CarryBlockJamSimpleBoard board)
        {
            if (board == null)
                return;

            board.transform.position = ArtGridBoardPosition;
            board.transform.rotation = Quaternion.identity;
            board.transform.localScale = Vector3.one;
            EditorUtility.SetDirty(board);
        }

        private static void EnsureBoardGridFrame(CarryBlockJamSimpleBoard board)
        {
            if (board == null)
                return;

            Transform gridRoot = board.transform.Find("Grid");
            if (gridRoot == null)
                gridRoot = CreateChild(board.transform, "Grid");

            // Local positions keep the same world placement as ArtScene's dark pit
            // when the board is at ArtGridBoardPosition.
            EnsureArtModel(
                gridRoot,
                "M_Gridwall",
                GridWallPrefabPath,
                GridWallMaterialPath,
                new Vector3(-0.5f, -0.15f, 4.5f));
            EnsureArtModel(
                gridRoot,
                "M_Grid",
                GridPrefabPath,
                GridWallMaterialPath,
                new Vector3(2.5f, -0.15f, 4.5f));
            EnsureArtModel(
                gridRoot,
                "M_GridBottom",
                GridBottomPrefabPath,
                CellMaterialPath,
                new Vector3(0f, 0.24f, 4.5f));
        }

        private static void EnsureBoardGates(CarryBlockJamSimpleBoard board)
        {
            if (board == null)
                return;

            Transform gatesRoot = board.transform.Find("Gates");
            if (gatesRoot == null)
                gatesRoot = CreateChild(board.transform, "Gates");

            // Base (uncolored upper) first, then accent color — same for top and bottom gates.
            EnsureGateModel(
                gatesRoot,
                "M_GateUp",
                GateUpPrefabPath,
                new Vector3(-1.5f, 0.35f, 4.5f),
                "Assets/[Materials]/Mat_GateUp.mat",
                "Assets/[Materials]/Mat_GateUp-Green.mat");
            EnsureGateModel(
                gatesRoot,
                "M_GateUp (1)",
                GateUpPrefabPath,
                new Vector3(1.5f, 0.35f, 4.5f),
                "Assets/[Materials]/Mat_GateUp.mat",
                "Assets/[Materials]/Mat_GateUp-Purple.mat");
            EnsureGateModel(
                gatesRoot,
                "M_GateBottom",
                GateBottomPrefabPath,
                new Vector3(-1.5f, 0.35f, -4.5f),
                "Assets/[Materials]/Mat_GateBottom.mat",
                "Assets/[Materials]/Mat_GateBottom-Blue.mat");
            EnsureGateModel(
                gatesRoot,
                "M_GateBottom (1)",
                GateBottomPrefabPath,
                new Vector3(1.5f, 0.35f, -4.5f),
                "Assets/[Materials]/Mat_GateBottom.mat",
                "Assets/[Materials]/Mat_GateBottom-Red.mat");
        }

        private static void EnsureGateModel(
            Transform parent,
            string name,
            string prefabPath,
            Vector3 localPosition,
            params string[] materialPaths)
        {
            if (parent == null)
                return;

            Transform existing = parent.Find(name);
            GameObject instance;
            if (existing != null)
            {
                instance = existing.gameObject;
            }
            else
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    Debug.LogWarning($"[CarryBlockJam] Missing art model at {prefabPath}");
                    return;
                }

                instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
                if (instance == null)
                    instance = Object.Instantiate(prefab, parent);
                instance.name = name;

                instance.transform.localPosition = localPosition;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
            }

            // Always keep mesh materials on the correct parts (Gate_* vs Gate_*-Color).
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            Material baseMaterial = materialPaths.Length > 0
                ? AssetDatabase.LoadAssetAtPath<Material>(materialPaths[0])
                : null;
            Material colorMaterial = materialPaths.Length > 1
                ? AssetDatabase.LoadAssetAtPath<Material>(materialPaths[1])
                : null;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                bool isColorMesh = renderer.gameObject.name.IndexOf("-Color", System.StringComparison.OrdinalIgnoreCase) >= 0;
                Material material = isColorMesh ? colorMaterial : baseMaterial;
                if (material != null)
                    renderer.sharedMaterial = material;
            }
        }

        private static void EnsureArtModel(
            Transform parent,
            string name,
            string prefabPath,
            string materialPath,
            Vector3 localPosition)
        {
            if (parent == null)
                return;

            Transform existing = parent.Find(name);
            if (existing != null)
            {
                existing.localPosition = localPosition;
                existing.localRotation = Quaternion.identity;
                existing.localScale = Vector3.one;
                ApplySharedMaterial(existing.gameObject, AssetDatabase.LoadAssetAtPath<Material>(materialPath));
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[CarryBlockJam] Missing art model at {prefabPath}");
                return;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (instance == null)
                instance = Object.Instantiate(prefab, parent);

            instance.name = name;
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            ApplySharedMaterial(instance, AssetDatabase.LoadAssetAtPath<Material>(materialPath));
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

            if (serializedBoard.FindProperty("cellMaterial").objectReferenceValue == null)
            {
                serializedBoard.FindProperty("cellMaterial").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Material>(CellMaterialPath);
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
            Material cellMaterial)
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

                    if (cellMaterial != null)
                        ApplySharedMaterial(visual, cellMaterial);

                    grid.RegisterCell(row, col, slot.transform, false);
                }
            }

            EditorUtility.SetDirty(board);
        }

        private static void ApplySharedMaterial(GameObject visual, Material material)
        {
            if (visual == null || material == null)
                return;

            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].sharedMaterial = material;
            }
        }

        private static void BuildExits(
            CarryBlockJamSimpleBoard board,
            PuzzleGrid grid,
            Transform exitsRoot,
            LevelData levelData)
        {
            if (CarryBlockJamArtGateUtility.HasArtGates(board))
            {
                CarryBlockJamArtGateUtility.BindExitsToArtGates(board, levelData);
                return;
            }

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
            if (Camera.main != null)
                return;

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
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
