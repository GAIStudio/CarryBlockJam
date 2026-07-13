using System.Collections.Generic;
using GAITemplate;
using TMPro;
using UnityEngine;

namespace CarryBlockJam
{
    /// <summary>
    /// Binds level exit settings onto the fixed art gate models under the board.
    /// Gate base poses come from the scene; Prefab Settings can add a model offset.
    /// </summary>
    public static class CarryBlockJamArtGateUtility
    {
        private const string GatesRootName = "Gates";

        private static readonly string[] TopGateNames = { "M_GateUp", "M_GateUp (1)" };
        private static readonly string[] BottomGateNames = { "M_GateBottom", "M_GateBottom (1)" };

        private static readonly Dictionary<int, Vector3> GateBaseLocalPositions = new();
        private static readonly Dictionary<int, Vector3> GateBaseLocalScales = new();

        public static Transform FindGatesRoot(CarryBlockJamSimpleBoard board)
        {
            if (board == null)
                return null;

            Transform gates = board.transform.Find(GatesRootName);
            return gates;
        }

        public static bool HasArtGates(CarryBlockJamSimpleBoard board)
        {
            Transform gatesRoot = FindGatesRoot(board);
            return gatesRoot != null && gatesRoot.childCount > 0;
        }

        public static void BindExitsToArtGates(CarryBlockJamSimpleBoard board, LevelData levelData)
        {
            if (board == null || levelData?.carryBlockJam?.exits == null)
                return;

            Transform gatesRoot = FindGatesRoot(board);
            if (gatesRoot == null)
                return;

            ClearLegacyCubeExits(board.ExitsRoot);
            ClearExitComponents(gatesRoot);
            ApplyGateModelOffsets(board, gatesRoot);

            PuzzleGrid grid = board.GetComponent<PuzzleGrid>();
            int columns = grid != null ? grid.Columns : board.Columns;
            int rows = grid != null ? grid.Rows : board.Rows;
            BoardExitLabelSettings labelSettings = board.ExitLabel;
            HashSet<Transform> usedGates = new HashSet<Transform>();

            List<CarryBlockJamExitDefinition> definitions = levelData.carryBlockJam.exits;
            for (int i = 0; i < definitions.Count; i++)
            {
                CarryBlockJamExitDefinition definition = definitions[i];
                if (definition == null)
                    continue;

                Transform gate = ResolveGate(gatesRoot, definition, columns, rows);
                if (gate == null)
                {
                    Debug.LogWarning(
                        $"[CarryBlockJam] No art gate for exit {definition.side} startIndex={definition.startIndex}.");
                    continue;
                }

                if (!usedGates.Add(gate))
                {
                    Debug.LogWarning(
                        $"[CarryBlockJam] Art gate '{gate.name}' already used by another exit; skipping duplicate.");
                    continue;
                }

                BindExitToGate(gate, definition, labelSettings);
            }

            for (int i = 0; i < gatesRoot.childCount; i++)
            {
                Transform gate = gatesRoot.GetChild(i);
                if (gate == null || usedGates.Contains(gate))
                    continue;

                RemoveGoalLabel(gate);
            }
        }

        public static void ApplyGateModelOffsets(CarryBlockJamSimpleBoard board, Transform gatesRoot = null)
        {
            if (board == null)
                return;

            gatesRoot ??= FindGatesRoot(board);
            if (gatesRoot == null)
                return;

            CarryBlockJamPrefabSettings settings = board.PrefabSettings;
            for (int i = 0; i < gatesRoot.childCount; i++)
            {
                Transform gate = gatesRoot.GetChild(i);
                if (gate == null)
                    continue;

                ApplyGateModelOffset(gate, settings);
            }
        }

        public static void ApplyGateModelOffset(Transform gate, CarryBlockJamPrefabSettings settings)
        {
            if (gate == null)
                return;

            int id = gate.GetInstanceID();
            if (!GateBaseLocalPositions.TryGetValue(id, out Vector3 baseLocalPosition))
            {
                baseLocalPosition = gate.localPosition;
                GateBaseLocalPositions[id] = baseLocalPosition;
            }

            Vector3 offset = settings != null
                ? settings.GetGateModelOffset(IsUpGate(gate))
                : Vector3.zero;
            gate.localPosition = baseLocalPosition + offset;
        }

        public static void ApplyGateModelScale(Transform gate, Vector3 modelScale)
        {
            if (gate == null)
                return;

            int id = gate.GetInstanceID();
            if (!GateBaseLocalScales.TryGetValue(id, out Vector3 baseLocalScale))
            {
                baseLocalScale = gate.localScale;
                if (baseLocalScale == Vector3.zero)
                    baseLocalScale = Vector3.one;
                GateBaseLocalScales[id] = baseLocalScale;
            }

            Vector3 scale = modelScale == Vector3.zero ? Vector3.one : modelScale;
            gate.localScale = Vector3.Scale(baseLocalScale, scale);
        }

        public static void BindExitToGate(
            Transform gate,
            CarryBlockJamExitDefinition definition,
            BoardExitLabelSettings labelSettings)
        {
            if (gate == null || definition == null)
                return;

            CarryBlockJamExit exit = gate.GetComponent<CarryBlockJamExit>();
            if (exit == null)
                exit = gate.gameObject.AddComponent<CarryBlockJamExit>();

            // Apply gate color before creating the goal label so TMP renderers
            // are not mixed into mesh material assignment.
            bool isUpGate = IsUpGate(gate);
            ApplyGateModelScale(gate, definition.modelScale);
            exit.Configure(definition);
            exit.BindArtGate(isUpGate, null, labelSettings);

            TMP_Text label = CarryBlockJamExitLabelUtility.EnsureGoalLabel(
                gate,
                definition.side,
                labelSettings);
            CarryBlockJamExitLabelUtility.ApplyArtGateLabelPlacement(
                label != null ? label.transform : null,
                definition.side,
                labelSettings);
            exit.BindArtGate(isUpGate, label, labelSettings);
        }

        public static void ApplyGateColor(Transform gate, bool isUpGate, PieceColorType color)
        {
            if (gate == null)
                return;

            List<Renderer> gateRenderers = CollectGateMeshRenderers(gate);
            if (gateRenderers.Count == 0)
                return;

            Material baseMaterial = LoadMaterial(isUpGate ? "Mat_GateUp" : "Mat_GateBottom");
            Material colorMaterial = LoadColorMaterial(isUpGate, color);
            if (colorMaterial == null)
                colorMaterial = baseMaterial;

            ResolveBaseAndColorRenderers(gateRenderers, out Renderer baseRenderer, out Renderer colorRenderer);

            if (baseRenderer != null && baseMaterial != null)
                baseRenderer.sharedMaterial = baseMaterial;
            if (colorRenderer != null && colorMaterial != null)
                colorRenderer.sharedMaterial = colorMaterial;
            else if (gateRenderers.Count == 1 && colorMaterial != null)
                gateRenderers[0].sharedMaterial = colorMaterial;
        }

        public static Color GetGateTintColor(bool isUpGate, PieceColorType color)
        {
            Material material = LoadColorMaterial(isUpGate, color);
            return ExtractMaterialTint(material, color);
        }

        /// <summary>
        /// Exit goal labels always tint from Mat_GateUp-{Color} in -GateUp Materials.
        /// </summary>
        public static Color GetGateUpLabelTintColor(PieceColorType color)
        {
            Material material = LoadColorMaterial(isUpGate: true, color);
            return ExtractMaterialTint(material, color);
        }

        private static Color ExtractMaterialTint(Material material, PieceColorType color)
        {
            if (material != null)
            {
                if (material.HasProperty("_BaseColor"))
                    return material.GetColor("_BaseColor");
                if (material.HasProperty("_Color"))
                {
                    Color tint = material.GetColor("_Color");
                    if (tint.maxColorComponent > 0.01f && tint != Color.white)
                        return tint;
                }
            }

            return PieceColorPalette.GetColor(
                PieceColorPalette.IsPaintable(color) ? color : PieceColorType.White);
        }

        private static void ResolveBaseAndColorRenderers(
            List<Renderer> gateRenderers,
            out Renderer baseRenderer,
            out Renderer colorRenderer)
        {
            baseRenderer = null;
            colorRenderer = null;

            for (int i = 0; i < gateRenderers.Count; i++)
            {
                Renderer renderer = gateRenderers[i];
                if (renderer == null)
                    continue;

                string objectName = renderer.gameObject.name;
                if (objectName.IndexOf("-Color", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    colorRenderer = renderer;
                else
                    baseRenderer = renderer;
            }

            // Fallback if mesh names are unexpected.
            if (gateRenderers.Count >= 2)
            {
                baseRenderer ??= gateRenderers[0];
                if (colorRenderer == null)
                {
                    for (int i = 0; i < gateRenderers.Count; i++)
                    {
                        if (gateRenderers[i] != baseRenderer)
                        {
                            colorRenderer = gateRenderers[i];
                            break;
                        }
                    }
                }
            }
        }

        private static List<Renderer> CollectGateMeshRenderers(Transform gate)
        {
            var result = new List<Renderer>();
            if (gate == null)
                return result;

            MeshRenderer[] renderers = gate.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                MeshRenderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                // Skip goal number text (TMP) under GoalLabel.
                Transform label = gate.Find("GoalLabel");
                if (label != null && (renderer.transform == label || renderer.transform.IsChildOf(label)))
                    continue;

                result.Add(renderer);
            }

            return result;
        }

        public static Transform ResolveGate(
            Transform gatesRoot,
            CarryBlockJamExitDefinition definition,
            int columns,
            int rows)
        {
            if (gatesRoot == null || definition == null)
                return null;

            int slotIndex = ResolveFixedSlotIndex(definition, columns);
            if (slotIndex >= 0)
            {
                string[] allNames =
                {
                    TopGateNames[0],
                    TopGateNames[1],
                    BottomGateNames[0],
                    BottomGateNames[1],
                };
                return gatesRoot.Find(allNames[slotIndex]);
            }

            BoardBorderSide visualSide = ResolveVisualSide(definition.side, definition.startIndex, rows);
            string[] names = visualSide == BoardBorderSide.Bottom ? BottomGateNames : TopGateNames;
            int slot = ResolveHorizontalSlot(definition, columns);
            string gateName = names[Mathf.Clamp(slot, 0, names.Length - 1)];
            return gatesRoot.Find(gateName);
        }

        private static int ResolveFixedSlotIndex(CarryBlockJamExitDefinition definition, int columns)
        {
            if (definition == null)
                return -1;

            if (definition.side != BoardBorderSide.Top && definition.side != BoardBorderSide.Bottom)
                return -1;

            int left = CarryBlockJamFixedExitSlots.GetLeftColumnIndex(columns);
            int right = CarryBlockJamFixedExitSlots.GetRightColumnIndex(columns);
            int horizontal = Mathf.Abs(definition.startIndex - left) <= Mathf.Abs(definition.startIndex - right)
                ? 0
                : 1;
            return definition.side == BoardBorderSide.Bottom ? 2 + horizontal : horizontal;
        }

        private static BoardBorderSide ResolveVisualSide(BoardBorderSide side, int startIndex, int rows)
        {
            switch (side)
            {
                case BoardBorderSide.Top:
                case BoardBorderSide.Bottom:
                    return side;
                case BoardBorderSide.Left:
                case BoardBorderSide.Right:
                    // Side exits map onto the nearer top/bottom art gate row.
                    int mid = Mathf.Max(1, rows) / 2;
                    return startIndex < mid ? BoardBorderSide.Top : BoardBorderSide.Bottom;
                default:
                    return BoardBorderSide.Top;
            }
        }

        private static int ResolveHorizontalSlot(CarryBlockJamExitDefinition definition, int columns)
        {
            int safeColumns = Mathf.Max(1, columns);
            switch (definition.side)
            {
                case BoardBorderSide.Left:
                    return 0;
                case BoardBorderSide.Right:
                    return 1;
                default:
                {
                    float centerIndex = definition.startIndex + (Mathf.Max(1, definition.length) - 1) * 0.5f;
                    return centerIndex < safeColumns * 0.5f ? 0 : 1;
                }
            }
        }

        private static bool IsUpGate(Transform gate)
        {
            if (gate == null)
                return true;

            string name = gate.name;
            return name.StartsWith("M_GateUp");
        }

        private static void ClearExitComponents(Transform gatesRoot)
        {
            if (gatesRoot == null)
                return;

            CarryBlockJamExit[] exits = gatesRoot.GetComponentsInChildren<CarryBlockJamExit>(true);
            for (int i = 0; i < exits.Length; i++)
            {
                if (exits[i] == null)
                    continue;

                Object.DestroyImmediate(exits[i]);
            }
        }

        private static void RemoveGoalLabel(Transform gate)
        {
            if (gate == null)
                return;

            Transform label = gate.Find("GoalLabel");
            if (label == null)
                return;

            Object.DestroyImmediate(label.gameObject);
        }

        private static void ClearLegacyCubeExits(Transform exitsRoot)
        {
            if (exitsRoot == null)
                return;

            for (int i = exitsRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = exitsRoot.GetChild(i);
                if (child == null)
                    continue;

                Object.DestroyImmediate(child.gameObject);
            }
        }

        private static Material LoadColorMaterial(bool isUpGate, PieceColorType color)
        {
            string prefix = isUpGate ? "Mat_GateUp" : "Mat_GateBottom";
            string folder = isUpGate
                ? CarryBlockJamArtMaterialUtility.GateUpMaterialsFolder
                : CarryBlockJamArtMaterialUtility.GateBottomMaterialsFolder;

            if (!PieceColorPalette.IsPaintable(color) || color == PieceColorType.Grey)
                return CarryBlockJamArtMaterialUtility.LoadMaterial(folder, prefix, "Materials/Gates");

            return CarryBlockJamArtMaterialUtility.LoadColoredMaterial(
                folder,
                prefix,
                color,
                "Materials/Gates");
        }

        private static Material LoadMaterial(string materialName)
        {
            bool isUpGate = materialName != null && materialName.StartsWith("Mat_GateUp");
            string folder = isUpGate
                ? CarryBlockJamArtMaterialUtility.GateUpMaterialsFolder
                : CarryBlockJamArtMaterialUtility.GateBottomMaterialsFolder;
            return CarryBlockJamArtMaterialUtility.LoadMaterial(folder, materialName, "Materials/Gates");
        }
    }
}
