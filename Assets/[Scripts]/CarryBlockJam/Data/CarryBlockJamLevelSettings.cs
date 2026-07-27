using System;
using System.Collections.Generic;
using GAITemplate;
using UnityEngine;
using UnityEngine.Serialization;

namespace CarryBlockJam
{
    [Serializable]
    public class CarryBlockJamLevelSettings
    {
        [Header("Timer")]
        [Tooltip("When enabled, a countdown timer is shown under the level UI.")]
        public bool hasTimer = false;
        [Tooltip("Level time limit in seconds. When it reaches zero the level fails.")]
        [Min(1f)] public float timeLimitSeconds = 60f;

        [Header("Board")]
        public Vector3 gridCellScale = CarryBlockJamSimpleBoard.DefaultCellScale;
        [Min(0.1f)] public float gridSpacingX = 1.1f;
        [Min(0.1f)] public float gridSpacingZ = 1.1f;

        [Header("Colors")]
        public List<CarryBlockJamColorSetup> colorSetups = new List<CarryBlockJamColorSetup>
        {
            new CarryBlockJamColorSetup(),
        };

        [Header("Exits")]
        [Tooltip("Optional exits. Add as many as needed — each covers one 0-based grid cell (row/column).")]
        public List<CarryBlockJamExitDefinition> exits = new List<CarryBlockJamExitDefinition>();

        [Header("Tables")]
        [Tooltip("When enabled, tables are not auto-generated from exits. Only painted Color Table cells and tablePlacements are used.")]
        public bool disableAutoTables = false;

        [FormerlySerializedAs("boxPlacements")]
        public List<CarryBlockJamBoxPlacement> tablePlacements = new List<CarryBlockJamBoxPlacement>();

        [Header("Plates")]
        public List<CarryBlockJamPlatePlacement> platePlacements = new List<CarryBlockJamPlatePlacement>();

        [Header("Stickman Spawn")]
        [Tooltip("How the stickman is placed on the grid.")]
        public CarryBlockJamStickmanSpawnMode stickmanSpawnMode = CarryBlockJamStickmanSpawnMode.Center;
        [Tooltip("0-based stickman cell (row, column). Used when Spawn Mode is Fixed Cell.")]
        public CarryBlockJamGridCoordinate fixedStickmanCell = new CarryBlockJamGridCoordinate(2, 3);
    }
}
