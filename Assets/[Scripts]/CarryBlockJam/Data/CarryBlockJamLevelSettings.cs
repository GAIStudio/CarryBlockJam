using System;
using System.Collections.Generic;
using GAITemplate;
using UnityEngine;

namespace CarryBlockJam
{
    [Serializable]
    public class CarryBlockJamLevelSettings
    {
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
            new CarryBlockJamExitDefinition(),
            new CarryBlockJamExitDefinition
            {
                side = BoardBorderSide.Right,
                startIndex = 0,
                length = 1,
                rotation = new Vector3(0f, 270f, 0f),
            },
        };

        [Header("Boxes")]
        public List<CarryBlockJamBoxPlacement> boxPlacements = new List<CarryBlockJamBoxPlacement>();

        [Header("Plates")]
        public List<CarryBlockJamPlatePlacement> platePlacements = new List<CarryBlockJamPlatePlacement>();

        [Header("Stickman Spawn")]
        public CarryBlockJamStickmanSpawnMode stickmanSpawnMode = CarryBlockJamStickmanSpawnMode.Center;
        public CarryBlockJamGridCoordinate fixedStickmanCell = new CarryBlockJamGridCoordinate(2, 3);

        [Header("Frozen Box Visual")]
        public BoardFrozenBoxVisualSettings frozenBoxVisual = BoardFrozenBoxVisualSettings.CreateDefault();
    }
}
