using GAITemplate;
using UnityEngine;

namespace CarryBlockJam
{
    public static class CarryBlockJamSceneLevelRuntime
    {
        public static int EnsureGameplayOnSceneBoards(LevelData levelData)
        {
            if (levelData == null || levelData.mechanicType != PuzzleMechanicType.Grid)
                return 0;

            CarryBlockJamSimpleBoard[] boards = Object.FindObjectsOfType<CarryBlockJamSimpleBoard>();
            int appliedCount = 0;

            for (int i = 0; i < boards.Length; i++)
            {
                CarryBlockJamSimpleBoard board = boards[i];
                if (board == null)
                    continue;

                CarryBlockJamLevelController controller = board.GetComponent<CarryBlockJamLevelController>();
                if (controller != null)
                {
                    controller.EnsureGameplayFromLevel(levelData);
                }
                else
                {
                    int targetRows = Mathf.Max(1, levelData.gridRows);
                    int targetColumns = Mathf.Max(1, levelData.gridColumns);
                    bool sizeMismatch = board.Rows != targetRows || board.Columns != targetColumns;
                    board.ApplyLevelData(levelData, rebuildVisuals: sizeMismatch);
                }

                appliedCount++;
            }

            return appliedCount;
        }

        public static int ApplyToSceneBoards(LevelData levelData)
        {
            if (levelData == null || levelData.mechanicType != PuzzleMechanicType.Grid)
                return 0;

            CarryBlockJamSimpleBoard[] boards = Object.FindObjectsOfType<CarryBlockJamSimpleBoard>();
            int appliedCount = 0;

            for (int i = 0; i < boards.Length; i++)
            {
                CarryBlockJamSimpleBoard board = boards[i];
                if (board == null)
                    continue;

                CarryBlockJamLevelController controller = board.GetComponent<CarryBlockJamLevelController>();
                if (controller != null)
                    controller.ApplyLevel(levelData);
                else
                    board.ApplyLevelData(levelData, rebuildVisuals: true);

                CarryBlockJamRuntimePieceSpawner spawner = board.GetComponent<CarryBlockJamRuntimePieceSpawner>();
                if (spawner != null)
                    spawner.RespawnFromLevel(levelData);

                appliedCount++;
            }

            return appliedCount;
        }
    }
}
