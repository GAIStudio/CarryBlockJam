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
            },
        };

        [Header("Stickman Spawn")]
        public CarryBlockJamStickmanSpawnMode stickmanSpawnMode = CarryBlockJamStickmanSpawnMode.Center;
        public CarryBlockJamGridCoordinate fixedStickmanCell = new CarryBlockJamGridCoordinate(2, 3);

        [Header("Layout Constraints")]
        public List<CarryBlockJamGridCoordinate> blockedCells = new List<CarryBlockJamGridCoordinate>();
        public bool useLevelSeed;
        public int generationSeed = 1;

        [Header("Generation")]
        [Min(1)] public int maxPlacementAttempts = 128;
        public bool ensureReachableLayout = true;
        public bool avoidBlockedExitCells = true;
        public bool avoidSameColorExitFrontCells = true;

        [Header("Plates")]
        [Min(1)] public int maxPlateCountPerCell = 3;
        public bool allowPlateStacksOnSameCell = true;

        [Header("Stickman Rules")]
        public bool preferCenteredSpawn = true;
        public bool requirePathToAtLeastOnePlate = true;
        public bool requirePathFromBoxToMatchingExit = true;
    }
}
