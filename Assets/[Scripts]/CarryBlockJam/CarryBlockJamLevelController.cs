using GAITemplate;
using TMPro;
using System.Collections.Generic;
using UnityEngine;

namespace CarryBlockJam
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CarryBlockJamSimpleBoard))]
    public class CarryBlockJamLevelController : MonoBehaviour
    {
        [SerializeField] private CarryBlockJamSimpleBoard board;
        [SerializeField] private CarryBlockJamPrefabSettings prefabSettings;
        [SerializeField] private Transform runtimeRoot;
        [SerializeField] private Transform piecesRoot;
        [SerializeField] private Transform exitsVisualRoot;

        public CarryBlockJamSimpleBoard Board => board;
        public CarryBlockJamPrefabSettings PrefabSettings => prefabSettings;
        public Transform RuntimeRoot => runtimeRoot;
        public Transform PiecesRoot => piecesRoot;
        public Transform ExitsVisualRoot => exitsVisualRoot;

        private void Awake()
        {
            if (board == null)
                board = GetComponent<CarryBlockJamSimpleBoard>();

            EnsureRoots();
        }

        public void ApplyLevel(LevelData levelData)
        {
            if (board == null || levelData == null)
                return;

            board.ApplyLevelData(levelData, rebuildVisuals: true);
            EnsureRoots();
            BuildRuntimeExits(levelData);
        }

        public void EnsureGameplayFromLevel(LevelData levelData)
        {
            if (board == null || levelData == null)
                return;

            int targetRows = Mathf.Max(1, levelData.gridRows);
            int targetColumns = Mathf.Max(1, levelData.gridColumns);
            bool sizeMismatch = board.Rows != targetRows || board.Columns != targetColumns;
            board.ApplyLevelData(levelData, rebuildVisuals: sizeMismatch);
            EnsureRoots();
            EnsureExitGameplay(levelData);
        }

        private void EnsureExitGameplay(LevelData levelData)
        {
            if (CarryBlockJamArtGateUtility.HasArtGates(board))
            {
                CarryBlockJamArtGateUtility.BindExitsToArtGates(board, levelData);
                return;
            }

            Transform exitsRoot = board.ExitsRoot != null ? board.ExitsRoot : exitsVisualRoot;
            if (levelData?.carryBlockJam?.exits == null || exitsRoot == null)
                return;

            List<CarryBlockJamExitDefinition> definitions = levelData.carryBlockJam.exits;
            int wiredCount = 0;

            for (int i = 0; i < exitsRoot.childCount && i < definitions.Count; i++)
            {
                Transform exitTransform = exitsRoot.GetChild(i);
                CarryBlockJamExitDefinition definition = definitions[i];
                if (definition == null)
                    continue;

                CarryBlockJamExit exit = exitTransform.GetComponent<CarryBlockJamExit>();
                if (exit == null)
                    exit = exitTransform.gameObject.AddComponent<CarryBlockJamExit>();

                exit.Configure(definition);
                TMP_Text label = CarryBlockJamExitLabelUtility.EnsureGoalLabel(
                    exitTransform,
                    definition.side,
                    board != null ? board.ExitLabel : null);

                exit.BindVisuals(
                    FindExitVisual(exitTransform, "Gate"),
                    null,
                    label,
                    board != null ? board.ExitLabel : null);
                RemoveLegacyCarVisual(exitTransform);
                wiredCount++;
            }

            if (wiredCount > 0 || exitsRoot.childCount > 0)
                return;

            BuildRuntimeExits(levelData);
        }

        private void BuildRuntimeExits(LevelData levelData)
        {
            if (CarryBlockJamArtGateUtility.HasArtGates(board))
            {
                CarryBlockJamArtGateUtility.BindExitsToArtGates(board, levelData);
                return;
            }

            Transform exitsRoot = board.ExitsRoot != null ? board.ExitsRoot : exitsVisualRoot;
            if (levelData?.carryBlockJam?.exits == null || exitsRoot == null)
                return;

            for (int i = exitsRoot.childCount - 1; i >= 0; i--)
                Object.Destroy(exitsRoot.GetChild(i).gameObject);

            PuzzleGrid grid = board.GetComponent<PuzzleGrid>();
            if (grid == null)
                return;

            CarryBlockJamPrefabSettings settings = prefabSettings != null ? prefabSettings : board.PrefabSettings;
            BoardExitLabelSettings exitLabelSettings = board.ExitLabel;
            List<CarryBlockJamExitDefinition> definitions = levelData.carryBlockJam.exits;
            for (int i = 0; i < definitions.Count; i++)
                BuildExitVisual(grid, exitsRoot, settings, exitLabelSettings, definitions[i], i);
        }

        private static GamePiece FindExitVisual(Transform exitTransform, string childName)
        {
            if (exitTransform == null)
                return null;

            Transform child = exitTransform.Find(childName);
            if (child == null)
                return null;

            GamePiece piece = child.GetComponent<GamePiece>();
            if (piece == null)
                piece = child.gameObject.AddComponent<GamePiece>();

            return piece;
        }

        private void EnsureRoots()
        {
            if (runtimeRoot == null)
                runtimeRoot = EnsureChild("Runtime");

            if (piecesRoot == null)
                piecesRoot = EnsureChild("Pieces", runtimeRoot);

            if (exitsVisualRoot == null)
                exitsVisualRoot = EnsureChild("Exits", runtimeRoot);
        }

        private Transform EnsureChild(string name) => EnsureChild(name, transform);

        private static Transform EnsureChild(string name, Transform parent)
        {
            if (parent == null)
                return null;

            string[] parts = name.Split('/');
            Transform current = parent;
            for (int i = 0; i < parts.Length; i++)
            {
                Transform child = current.Find(parts[i]);
                if (child == null)
                {
                    var childObject = new GameObject(parts[i]);
                    child = childObject.transform;
                    child.SetParent(current, false);
                }

                current = child;
            }

            return current;
        }

        private static void BuildExitVisual(
            PuzzleGrid grid,
            Transform exitsRoot,
            CarryBlockJamPrefabSettings settings,
            BoardExitLabelSettings exitLabelSettings,
            CarryBlockJamExitDefinition definition,
            int index)
        {
            if (grid == null || exitsRoot == null || definition == null)
                return;

            var exitObject = new GameObject($"Exit_{definition.side}_{definition.startIndex}_{index}");
            exitObject.transform.SetParent(exitsRoot, false);
            exitObject.transform.localPosition = GetExitLocalPosition(grid, definition) + definition.positionOffset;
            exitObject.transform.localRotation = Quaternion.Euler(definition.rotation);
            exitObject.transform.localScale = definition.modelScale == Vector3.zero
                ? Vector3.one
                : definition.modelScale;

            CarryBlockJamExit exit = exitObject.AddComponent<CarryBlockJamExit>();
            exit.Configure(definition);

            GamePiece gatePiece = CreateVisual(
                "Gate",
                exitObject.transform,
                settings != null ? settings.exitVisual : null,
                exit.CurrentColor,
                GetExitLocalScale(grid, definition.side, definition.length));

            TMP_Text label = CarryBlockJamExitLabelUtility.EnsureGoalLabel(
                exitObject.transform,
                definition.side,
                exitLabelSettings);
            exit.BindVisuals(gatePiece, null, label, exitLabelSettings);
        }

        private static void RemoveLegacyCarVisual(Transform exitTransform)
        {
            if (exitTransform == null)
                return;

            Transform car = exitTransform.Find("Car");
            if (car == null)
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(car.gameObject);
            else
#endif
                Destroy(car.gameObject);
        }

        private static GamePiece CreateVisual(
            string objectName,
            Transform parent,
            CarryBlockJamPrimitiveVisualSettings visualSettings,
            PieceColorType color,
            Vector3 fallbackScale)
        {
            if (parent == null)
                return null;

            GameObject visualObject = null;
            if (visualSettings != null && visualSettings.prefab != null)
            {
                visualObject = Object.Instantiate(visualSettings.prefab, parent, false);
                visualObject.name = objectName;
            }
            else
            {
                PrimitiveType primitiveType = visualSettings != null ? visualSettings.primitiveType : PrimitiveType.Cube;
                visualObject = GameObject.CreatePrimitive(primitiveType);
                visualObject.name = objectName;
                visualObject.transform.SetParent(parent, false);
            }

            if (visualObject == null)
                return null;

            visualObject.transform.localPosition = visualSettings != null ? visualSettings.localPosition : Vector3.zero;
            visualObject.transform.localRotation = Quaternion.Euler(
                visualSettings != null ? visualSettings.localRotation : Vector3.zero);
            visualObject.transform.localScale = visualSettings != null && visualSettings.localScale != Vector3.zero
                ? Vector3.Scale(fallbackScale, visualSettings.localScale)
                : fallbackScale;

            GamePiece piece = visualObject.GetComponent<GamePiece>();
            if (piece == null)
                piece = visualObject.AddComponent<GamePiece>();

            if (visualSettings == null || visualSettings.tintWithPieceColor)
                piece.ApplyColor(PieceColorPalette.IsPaintable(color) ? color : PieceColorType.White);

            Collider collider = visualObject.GetComponent<Collider>();
            if (collider != null)
                Object.Destroy(collider);

            return piece;
        }

        private static Vector3 GetExitLocalPosition(PuzzleGrid grid, CarryBlockJamExitDefinition settings)
        {
            float centerIndex = settings.startIndex + (Mathf.Max(1, settings.length) - 1) * 0.5f;

            switch (settings.side)
            {
                case BoardBorderSide.Left:
                {
                    int row = Mathf.RoundToInt(centerIndex);
                    return grid.GetLocalPosition(row, 0) + new Vector3(-grid.GridSpacingX * 0.58f, 0.375f, 0f);
                }
                case BoardBorderSide.Right:
                {
                    int row = Mathf.RoundToInt(centerIndex);
                    return grid.GetLocalPosition(row, grid.Columns - 1) + new Vector3(grid.GridSpacingX * 0.58f, 0.375f, 0f);
                }
                case BoardBorderSide.Top:
                {
                    int column = Mathf.RoundToInt(centerIndex);
                    return grid.GetLocalPosition(0, column) + new Vector3(0f, 0.375f, grid.GridSpacingZ * 0.58f);
                }
                case BoardBorderSide.Bottom:
                {
                    int column = Mathf.RoundToInt(centerIndex);
                    return grid.GetLocalPosition(grid.Rows - 1, column) + new Vector3(0f, 0.375f, -grid.GridSpacingZ * 0.58f);
                }
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
                    new Vector3(1f, 1f, grid.GridSpacingZ * resolvedLength * 0.94f),
                BoardBorderSide.Top or BoardBorderSide.Bottom =>
                    new Vector3(grid.GridSpacingX * resolvedLength * 0.94f, 1f, 1f),
                _ => Vector3.one,
            };
        }
    }
}
