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
        private static readonly string[] LeftGateNames = { "M_GateLeft", "M_GateLeft (1)" };
        private static readonly string[] RightGateNames = { "M_GateRight", "M_GateRight (1)" };

        private static readonly Dictionary<int, Vector3> GateBaseLocalPositions = new();
        private static readonly Dictionary<int, Quaternion> GateBaseLocalRotations = new();
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

            PuzzleGrid grid = board.GetComponent<PuzzleGrid>();
            int columns = grid != null ? grid.Columns : board.Columns;
            int rows = grid != null ? grid.Rows : board.Rows;
            CarryBlockJamExitLayout.NormalizeExits(levelData.carryBlockJam.exits, rows, columns);

            BoardExitLabelSettings labelSettings = board.ExitLabel;
            CarryBlockJamPrefabSettings prefabSettings = board.PrefabSettings;
            HashSet<Transform> usedGates = new HashSet<Transform>();

            // Start with every art gate hidden; only authored exits turn models back on.
            for (int i = 0; i < gatesRoot.childCount; i++)
            {
                Transform gate = gatesRoot.GetChild(i);
                if (gate == null)
                    continue;

                RemoveGoalLabel(gate);
                gate.gameObject.SetActive(false);
            }

            List<CarryBlockJamExitDefinition> definitions = levelData.carryBlockJam.exits;
            for (int i = 0; i < definitions.Count; i++)
            {
                CarryBlockJamExitDefinition definition = definitions[i];
                if (definition == null)
                    continue;

                Transform gate = ResolveGate(gatesRoot, definition, columns, rows, usedGates);
                if (gate == null)
                {
                    Debug.LogWarning(
                        $"[CarryBlockJam] No art gate for exit at row={definition.row} col={definition.column}.");
                    continue;
                }

                usedGates.Add(gate);
                gate.gameObject.SetActive(true);
                PositionGateForExit(gate, gatesRoot, grid, board, definition, prefabSettings);
                BindExitToGate(gate, definition, labelSettings, prefabSettings);
            }
        }

        /// <summary>
        /// Places an art gate on the authored border cell (one grid), plus Prefab Settings offset.
        /// </summary>
        public static void PositionGateForExit(
            Transform gate,
            Transform gatesRoot,
            PuzzleGrid grid,
            CarryBlockJamSimpleBoard board,
            CarryBlockJamExitDefinition definition,
            CarryBlockJamPrefabSettings settings)
        {
            if (gate == null || definition == null)
                return;

            Vector3 boardLocal = ResolveExitBoardLocalPosition(grid, board, definition);
            Vector3 gateLocal = boardLocal;
            if (gatesRoot != null && board != null)
            {
                // Convert board-local exit pose into Gates-root local space.
                Vector3 world = board.transform.TransformPoint(boardLocal);
                gateLocal = gatesRoot.InverseTransformPoint(world);
            }

            Vector3 offset = settings != null
                ? settings.GetGateModelOffset(definition.side)
                : Vector3.zero;

            // Cache current placement as the new base so later offset tweaks keep cell alignment.
            int id = gate.GetInstanceID();
            GateBaseLocalPositions[id] = gateLocal;
            gate.localPosition = gateLocal + offset;
        }

        private static Vector3 ResolveExitBoardLocalPosition(
            PuzzleGrid grid,
            CarryBlockJamSimpleBoard board,
            CarryBlockJamExitDefinition definition)
        {
            float spacingX = grid != null
                ? grid.GridSpacingX
                : (board != null ? Mathf.Max(0.01f, board.GridSpacingX) : 1.1f);
            float spacingZ = grid != null
                ? grid.GridSpacingZ
                : (board != null ? Mathf.Max(0.01f, board.GridSpacingZ) : 1.1f);
            int rows = grid != null ? grid.Rows : (board != null ? board.Rows : 8);
            int columns = grid != null ? grid.Columns : (board != null ? board.Columns : 6);

            Vector3 cellLocal;
            if (grid != null)
            {
                int row = Mathf.Clamp(definition.row, 0, Mathf.Max(0, rows - 1));
                int column = Mathf.Clamp(definition.column, 0, Mathf.Max(0, columns - 1));
                cellLocal = grid.GetLocalPosition(row, column);
                // PuzzleGrid is often on the board object; if not, convert into board space.
                if (board != null && grid.transform != board.transform)
                    cellLocal = board.transform.InverseTransformPoint(grid.transform.TransformPoint(cellLocal));
            }
            else
            {
                float offsetX = (columns - 1) * spacingX * 0.5f;
                float offsetZ = (rows - 1) * spacingZ * 0.5f;
                cellLocal = new Vector3(
                    definition.column * spacingX - offsetX,
                    0f,
                    offsetZ - definition.row * spacingZ);
            }

            Vector3 outward = definition.side switch
            {
                BoardBorderSide.Left => new Vector3(-spacingX * 0.58f, 0.375f, 0f),
                BoardBorderSide.Right => new Vector3(spacingX * 0.58f, 0.375f, 0f),
                BoardBorderSide.Top => new Vector3(0f, 0.375f, spacingZ * 0.58f),
                BoardBorderSide.Bottom => new Vector3(0f, 0.375f, -spacingZ * 0.58f),
                _ => new Vector3(0f, 0.375f, 0f),
            };

            return cellLocal + outward + definition.positionOffset;
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
                if (gate == null || !gate.gameObject.activeSelf)
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

            CarryBlockJamExit exit = gate.GetComponent<CarryBlockJamExit>();
            BoardBorderSide side = exit != null
                ? exit.Side
                : ResolveGateSideFromName(gate);
            gate.localPosition = baseLocalPosition + ResolveGateModelOffset(
                settings,
                side,
                IsUpGate(gate));
        }

        public static void ApplyGateModelScales(CarryBlockJamSimpleBoard board, Transform gatesRoot = null)
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
                if (gate == null || !gate.gameObject.activeSelf)
                    continue;

                CarryBlockJamExit exit = gate.GetComponent<CarryBlockJamExit>();
                BoardBorderSide side = exit != null
                    ? exit.Side
                    : ResolveGateSideFromName(gate);
                Vector3 exitScale = exit != null ? exit.ModelScale : Vector3.one;
                ApplyGateModelScale(gate, side, exitScale, settings);
            }
        }

        /// <summary>
        /// Left/Right fall back to the legacy top/bottom offset while unset,
        /// so existing setups keep their gate placement.
        /// </summary>
        private static Vector3 ResolveGateModelOffset(
            CarryBlockJamPrefabSettings settings,
            BoardBorderSide side,
            bool legacyIsUpGate)
        {
            if (settings == null)
                return Vector3.zero;

            Vector3 offset = settings.GetGateModelOffset(side);
            bool isSideGate = side == BoardBorderSide.Left || side == BoardBorderSide.Right;
            if (isSideGate && offset == Vector3.zero)
                return settings.GetGateModelOffset(legacyIsUpGate);

            return offset;
        }

        public static void ApplyGateModelScale(
            Transform gate,
            BoardBorderSide side,
            Vector3 exitModelScale,
            CarryBlockJamPrefabSettings settings)
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

            Vector3 sideScale = settings != null ? settings.GetGateModelScale(side) : Vector3.one;
            Vector3 exitScale = exitModelScale == Vector3.zero ? Vector3.one : exitModelScale;
            gate.localScale = Vector3.Scale(baseLocalScale, Vector3.Scale(sideScale, exitScale));
        }

        public static void ApplyGateModelScale(Transform gate, Vector3 modelScale)
        {
            ApplyGateModelScale(gate, ResolveGateSideFromName(gate), modelScale, settings: null);
        }

        public static void ApplyGateModelRotation(Transform gate, Vector3 rotation)
        {
            ApplyGateModelRotation(gate, rotation, ResolveGateSideFromName(gate));
        }

        public static void ApplyGateModelRotation(Transform gate, Vector3 rotation, BoardBorderSide side)
        {
            if (gate == null)
                return;

            int id = gate.GetInstanceID();
            // Side-specific FBXs (M_GateUp / Bottom / Left / Right) are authored facing
            // outward already. Do not inherit a stale scene yaw (e.g. M_GateBottom (1)
            // was left at 180° from an old side placement and sat on the board tiles).
            if (!GateBaseLocalRotations.TryGetValue(id, out Quaternion baseLocalRotation) ||
                !IsExpectedArtGateBaseRotation(baseLocalRotation))
            {
                baseLocalRotation = Quaternion.identity;
                GateBaseLocalRotations[id] = baseLocalRotation;
            }

            gate.localRotation = baseLocalRotation * Quaternion.Euler(rotation);
        }

        private static bool IsExpectedArtGateBaseRotation(Quaternion rotation)
        {
            return Quaternion.Angle(rotation, Quaternion.identity) < 0.5f;
        }

        public static void BindExitToGate(
            Transform gate,
            CarryBlockJamExitDefinition definition,
            BoardExitLabelSettings labelSettings,
            CarryBlockJamPrefabSettings prefabSettings = null)
        {
            if (gate == null || definition == null)
                return;

            CarryBlockJamExit exit = gate.GetComponent<CarryBlockJamExit>();
            if (exit == null)
                exit = gate.gameObject.AddComponent<CarryBlockJamExit>();

            // Apply gate color before creating the goal label so TMP renderers
            // are not mixed into mesh material assignment.
            ApplyGateModelRotation(gate, definition.rotation, definition.side);
            ApplyGateModelScale(gate, definition.side, definition.modelScale, prefabSettings);
            exit.Configure(definition);
            exit.BindArtGate(definition.side, null, labelSettings);

            TMP_Text label = CarryBlockJamExitLabelUtility.EnsureGoalLabel(
                gate,
                definition.side,
                labelSettings);
            CarryBlockJamExitLabelUtility.ApplyArtGateLabelPlacement(
                label != null ? label.transform : null,
                definition.side,
                labelSettings);
            exit.BindArtGate(definition.side, label, labelSettings);
        }

        public static void ApplyGateColor(Transform gate, bool isUpGate, PieceColorType color)
        {
            ApplyGateColor(gate, isUpGate ? BoardBorderSide.Top : BoardBorderSide.Bottom, color);
        }

        public static void ApplyGateColor(Transform gate, BoardBorderSide side, PieceColorType color)
        {
            if (gate == null)
                return;

            List<Renderer> gateRenderers = CollectGateMeshRenderers(gate);
            if (gateRenderers.Count == 0)
                return;

            // Prefer the side encoded in the model name so a mismatched artGateSide
            // still loads the correct Mat_Gate* materials.
            BoardBorderSide resolvedSide = ResolveGateSideFromName(gate);
            if (resolvedSide != side)
                side = resolvedSide;

            string prefix = CarryBlockJamArtMaterialUtility.GetGateMaterialPrefix(side);
            Material baseMaterial = LoadMaterial(prefix);
            Material colorMaterial = LoadColorMaterial(side, color);
            if (colorMaterial == null)
                colorMaterial = baseMaterial;

            bool appliedColorSlot = false;
            for (int i = 0; i < gateRenderers.Count; i++)
            {
                Renderer renderer = gateRenderers[i];
                if (renderer == null)
                    continue;

                bool rendererIsColor =
                    renderer.gameObject.name.IndexOf(
                        "-Color",
                        System.StringComparison.OrdinalIgnoreCase) >= 0;

                Material[] materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    if (rendererIsColor && colorMaterial != null)
                    {
                        renderer.sharedMaterial = colorMaterial;
                        appliedColorSlot = true;
                    }

                    continue;
                }

                bool changed = false;
                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    Material current = materials[materialIndex];
                    string materialName = NormalizeMaterialName(
                        current != null ? current.name : string.Empty);
                    bool materialIsColor = IsColoredGateMaterialName(materialName);
                    bool materialIsBase = IsBaseGateMaterialName(materialName);

                    // Color mesh always receives the active goal material, even when
                    // the previous material came from another side or a palette fallback.
                    if (rendererIsColor && colorMaterial != null)
                    {
                        materials[materialIndex] = colorMaterial;
                        appliedColorSlot = true;
                        changed = true;
                    }
                    else if (materialIsColor && colorMaterial != null)
                    {
                        materials[materialIndex] = colorMaterial;
                        appliedColorSlot = true;
                        changed = true;
                    }
                    else if (materialIsBase && baseMaterial != null)
                    {
                        materials[materialIndex] = baseMaterial;
                        changed = true;
                    }
                }

                if (changed)
                    renderer.sharedMaterials = materials;
            }

            // Legacy gate meshes may not preserve recognizable material names.
            if (!appliedColorSlot)
            {
                ResolveBaseAndColorRenderers(
                    gateRenderers,
                    out Renderer baseRenderer,
                    out Renderer colorRenderer);

                if (baseRenderer != null && baseMaterial != null)
                    baseRenderer.sharedMaterial = baseMaterial;
                if (colorRenderer != null && colorMaterial != null)
                    colorRenderer.sharedMaterial = colorMaterial;
                else if (gateRenderers.Count == 1 && colorMaterial != null)
                    gateRenderers[0].sharedMaterial = colorMaterial;
            }
        }

        private static string NormalizeMaterialName(string materialName)
        {
            if (string.IsNullOrEmpty(materialName))
                return string.Empty;

            const string instanceSuffix = " (Instance)";
            if (materialName.EndsWith(instanceSuffix, System.StringComparison.Ordinal))
                return materialName.Substring(0, materialName.Length - instanceSuffix.Length);

            return materialName;
        }

        private static bool IsColoredGateMaterialName(string materialName)
        {
            if (string.IsNullOrEmpty(materialName) ||
                !materialName.StartsWith("Mat_Gate", System.StringComparison.OrdinalIgnoreCase))
                return false;

            int separator = materialName.IndexOf('-');
            return separator > 0 && separator < materialName.Length - 1;
        }

        private static bool IsBaseGateMaterialName(string materialName)
        {
            if (string.IsNullOrEmpty(materialName))
                return false;

            return materialName.Equals("Mat_GateUp", System.StringComparison.OrdinalIgnoreCase) ||
                   materialName.Equals("Mat_GateBottom", System.StringComparison.OrdinalIgnoreCase) ||
                   materialName.Equals("Mat_GateLeft", System.StringComparison.OrdinalIgnoreCase) ||
                   materialName.Equals("Mat_GateRight", System.StringComparison.OrdinalIgnoreCase);
        }

        public static Color GetGateTintColor(bool isUpGate, PieceColorType color)
        {
            return GetGateTintColor(
                isUpGate ? BoardBorderSide.Top : BoardBorderSide.Bottom,
                color);
        }

        public static Color GetGateTintColor(
            BoardBorderSide side,
            PieceColorType color)
        {
            Material material = LoadColorMaterial(side, color);
            return ExtractMaterialTint(material, color);
        }

        /// <summary>
        /// Exit goal labels always tint from Mat_GateUp-{Color} in -GateUp Materials.
        /// </summary>
        public static Color GetGateUpLabelTintColor(PieceColorType color)
        {
            Material material = LoadColorMaterial(BoardBorderSide.Top, color);
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
            return ResolveGate(gatesRoot, definition, columns, rows, usedGates: null);
        }

        public static Transform ResolveGate(
            Transform gatesRoot,
            CarryBlockJamExitDefinition definition,
            int columns,
            int rows,
            HashSet<Transform> usedGates)
        {
            if (gatesRoot == null || definition == null)
                return null;

            BoardBorderSide side = definition.side;
            string[] preferredNames = GetGateNamesForSide(side);

            Transform best = null;
            float bestScore = float.MaxValue;
            int targetColumn = definition.column >= 0 ? definition.column : definition.startIndex;
            int targetRow = definition.row >= 0 ? definition.row : definition.startIndex;
            bool scoreByRow = side == BoardBorderSide.Left || side == BoardBorderSide.Right;

            // Left/Right exits must use M_GateLeft / M_GateRight only — never top/bottom stand-ins.
            for (int i = 0; i < preferredNames.Length; i++)
            {
                Transform gate = FindGateChild(gatesRoot, preferredNames[i]);
                if (gate == null)
                    continue;
                if (usedGates != null && usedGates.Contains(gate))
                    continue;

                float halfPenalty;
                float proximity;
                if (scoreByRow)
                {
                    halfPenalty = (i == 0) == (targetRow < rows * 0.5f) ? 0f : 10f;
                    proximity = Mathf.Abs(EstimateGateRow(gate, rows) - targetRow);
                }
                else
                {
                    halfPenalty = (i == 0) == (targetColumn < columns * 0.5f) ? 0f : 10f;
                    proximity = Mathf.Abs(EstimateGateColumn(gate, columns) - targetColumn);
                }

                float score = halfPenalty + proximity;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = gate;
                }
            }

            if (best != null)
                return best;

            // Same-side unused children only (name prefix match), never cross sides.
            string sidePrefix = GetGateNamePrefix(side);
            for (int i = 0; i < gatesRoot.childCount; i++)
            {
                Transform gate = gatesRoot.GetChild(i);
                if (gate == null)
                    continue;
                if (usedGates != null && usedGates.Contains(gate))
                    continue;
                if (!gate.name.StartsWith(sidePrefix))
                    continue;
                return gate;
            }

            // More exits on this side than named slots — clone an extra same-side model.
            Transform clone = TryCloneExtraGate(gatesRoot, sidePrefix);
            if (clone != null)
                return clone;

            Debug.LogWarning(
                $"[CarryBlockJam] Missing {sidePrefix} art gate for exit side={side}. " +
                "Apply the level in Level Creator / rebuild the board to spawn Left/Right models.");
            return null;
        }

        private static Transform TryCloneExtraGate(Transform gatesRoot, string sidePrefix)
        {
            if (gatesRoot == null || string.IsNullOrEmpty(sidePrefix))
                return null;

            Transform template = null;
            int sameSideCount = 0;
            for (int i = 0; i < gatesRoot.childCount; i++)
            {
                Transform child = gatesRoot.GetChild(i);
                if (child == null || !child.name.StartsWith(sidePrefix))
                    continue;

                sameSideCount++;
                if (template == null)
                    template = child;
            }

            if (template == null)
                return null;

            GameObject clone = Object.Instantiate(template.gameObject, gatesRoot);
            clone.name = $"{sidePrefix} ({sameSideCount})";
            clone.SetActive(false);

            // Fresh instance ids so base pose caches do not collide with the template.
            RemoveGoalLabel(clone.transform);
            CarryBlockJamExit existing = clone.GetComponent<CarryBlockJamExit>();
            if (existing != null)
                Object.DestroyImmediate(existing);

            return clone.transform;
        }

        private static Transform FindGateChild(Transform gatesRoot, string name)
        {
            if (gatesRoot == null || string.IsNullOrEmpty(name))
                return null;

            // Transform.Find skips inactive children — gates may already be hidden.
            for (int i = 0; i < gatesRoot.childCount; i++)
            {
                Transform child = gatesRoot.GetChild(i);
                if (child != null && child.name == name)
                    return child;
            }

            return null;
        }

        private static float EstimateGateColumn(Transform gate, int columns)
        {
            if (gate == null)
                return columns * 0.5f;

            // Scene art gates: left names → low columns, right names (1) → high columns.
            string name = gate.name;
            if (name.IndexOf("(1)", System.StringComparison.Ordinal) >= 0)
                return columns * 0.75f;
            return columns * 0.25f;
        }

        private static float EstimateGateRow(Transform gate, int rows)
        {
            if (gate == null)
                return rows * 0.5f;

            // Side gates: first name → upper rows, "(1)" → lower rows.
            string name = gate.name;
            if (name.IndexOf("(1)", System.StringComparison.Ordinal) >= 0)
                return rows * 0.75f;
            return rows * 0.25f;
        }

        private static string[] GetGateNamesForSide(BoardBorderSide side)
        {
            return side switch
            {
                BoardBorderSide.Bottom => BottomGateNames,
                BoardBorderSide.Left => LeftGateNames,
                BoardBorderSide.Right => RightGateNames,
                _ => TopGateNames,
            };
        }

        private static string GetGateNamePrefix(BoardBorderSide side)
        {
            return side switch
            {
                BoardBorderSide.Bottom => "M_GateBottom",
                BoardBorderSide.Left => "M_GateLeft",
                BoardBorderSide.Right => "M_GateRight",
                _ => "M_GateUp",
            };
        }

        private static BoardBorderSide ResolveGateSideFromName(Transform gate)
        {
            if (gate == null)
                return BoardBorderSide.Top;

            string name = gate.name;
            if (name.StartsWith("M_GateBottom"))
                return BoardBorderSide.Bottom;
            if (name.StartsWith("M_GateLeft"))
                return BoardBorderSide.Left;
            if (name.StartsWith("M_GateRight"))
                return BoardBorderSide.Right;
            return BoardBorderSide.Top;
        }

        public static BoardBorderSide ResolveGateSide(Transform gate) =>
            ResolveGateSideFromName(gate);

        private static bool IsUpGate(Transform gate)
        {
            return ResolveGateSideFromName(gate) == BoardBorderSide.Top;
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

        private static Material LoadColorMaterial(BoardBorderSide side, PieceColorType color)
        {
            string prefix = CarryBlockJamArtMaterialUtility.GetGateMaterialPrefix(side);
            string folder = CarryBlockJamArtMaterialUtility.GetGateMaterialsFolder(side);

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
            BoardBorderSide side = BoardBorderSide.Top;
            if (materialName != null)
            {
                if (materialName.StartsWith("Mat_GateBottom"))
                    side = BoardBorderSide.Bottom;
                else if (materialName.StartsWith("Mat_GateLeft"))
                    side = BoardBorderSide.Left;
                else if (materialName.StartsWith("Mat_GateRight"))
                    side = BoardBorderSide.Right;
            }

            string folder = CarryBlockJamArtMaterialUtility.GetGateMaterialsFolder(side);
            return CarryBlockJamArtMaterialUtility.LoadMaterial(folder, materialName, "Materials/Gates");
        }
    }
}
