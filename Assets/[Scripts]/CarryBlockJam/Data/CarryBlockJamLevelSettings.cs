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
        public List<CarryBlockJamExitDefinition> exits = new List<CarryBlockJamExitDefinition>
        {
            CarryBlockJamFixedExitSlots.CreateDefault(0, 6),
            CarryBlockJamFixedExitSlots.CreateDefault(1, 6),
            CarryBlockJamFixedExitSlots.CreateDefault(2, 6),
            CarryBlockJamFixedExitSlots.CreateDefault(3, 6),
        };

        [Header("Tables")]
        [FormerlySerializedAs("boxPlacements")]
        public List<CarryBlockJamBoxPlacement> tablePlacements = new List<CarryBlockJamBoxPlacement>();

        [Header("Plates")]
        public List<CarryBlockJamPlatePlacement> platePlacements = new List<CarryBlockJamPlatePlacement>();

        [Header("Stickman Spawn")]
        public CarryBlockJamStickmanSpawnMode stickmanSpawnMode = CarryBlockJamStickmanSpawnMode.Center;
        public CarryBlockJamGridCoordinate fixedStickmanCell = new CarryBlockJamGridCoordinate(2, 3);

        [Header("Frozen Table Visual")]
        [FormerlySerializedAs("frozenBoxVisual")]
        public BoardFrozenBoxVisualSettings frozenTableVisual = BoardFrozenBoxVisualSettings.CreateDefault();

        [Header("Curtain Table Visual")]
        [FormerlySerializedAs("curtainBoxVisual")]
        public BoardCurtainBoxVisualSettings curtainTableVisual = BoardCurtainBoxVisualSettings.CreateDefault();
    }
}
