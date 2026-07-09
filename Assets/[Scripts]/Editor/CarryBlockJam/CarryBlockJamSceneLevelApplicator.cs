using System.Collections.Generic;
using GAITemplate;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CarryBlockJam.Editor
{
    public static class CarryBlockJamSceneLevelApplicator
    {
        private const string SampleScenePath = "Assets/[Scenes]/SampleScene.unity";

        public static bool TryApply(LevelData levelData, out string message) =>
            TryApply(levelData, false, out message);

        public static bool TryApply(LevelData levelData, bool openSampleSceneIfMissing, out string message)
        {
            message = string.Empty;

            if (levelData == null)
            {
                message = "Assign a Level Data asset first.";
                return false;
            }

            if (levelData.mechanicType != PuzzleMechanicType.Grid)
            {
                message = $"\"{levelData.name}\" uses {levelData.mechanicType}. Scene preview only supports Grid levels.";
                return false;
            }

            if (Application.isPlaying)
            {
                int appliedCount = CarryBlockJamSceneLevelRuntime.ApplyToSceneBoards(levelData);
                if (appliedCount == 0)
                {
                    message = "No CarryBlockJam board found in the loaded scene.";
                    return false;
                }

                message = $"Applied \"{levelData.name}\" to {appliedCount} board(s) in Play Mode.";
                return true;
            }

            CarryBlockJamSimpleBoard[] boards = FindBoardsInOpenScenes();
            if (boards.Length == 0 && openSampleSceneIfMissing)
            {
                Scene scene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
                if (!scene.IsValid())
                {
                    message = $"Could not open {SampleScenePath}. Open a scene with CarryBlockJamBoard and try again.";
                    return false;
                }

                boards = FindBoardsInOpenScenes();
            }

            if (boards.Length == 0)
            {
                message = "No CarryBlockJam board in open scenes. Open SampleScene or click Apply To Scene.";
                return false;
            }

            for (int i = 0; i < boards.Length; i++)
            {
                CarryBlockJamSimpleBoard board = boards[i];
                CarryBlockJamSimpleBoardBuilder.BuildBoard(board, levelData);
                PreviewRuntimePieces(board, levelData);
                EditorSceneManager.MarkSceneDirty(board.gameObject.scene);
            }

            SceneView.RepaintAll();
            message = $"Applied \"{levelData.name}\" to {boards.Length} board(s).";
            return true;
        }

        private static CarryBlockJamSimpleBoard[] FindBoardsInOpenScenes()
        {
            CarryBlockJamSimpleBoard[] boards = Resources.FindObjectsOfTypeAll<CarryBlockJamSimpleBoard>();
            var sceneBoards = new List<CarryBlockJamSimpleBoard>();

            for (int i = 0; i < boards.Length; i++)
            {
                CarryBlockJamSimpleBoard board = boards[i];
                if (board == null || EditorUtility.IsPersistent(board) || !board.gameObject.scene.IsValid())
                    continue;

                sceneBoards.Add(board);
            }

            return sceneBoards.ToArray();
        }

        private static void PreviewRuntimePieces(CarryBlockJamSimpleBoard board, LevelData levelData)
        {
            if (board == null || levelData == null)
                return;

            CarryBlockJamLevelController controller = board.GetComponent<CarryBlockJamLevelController>();
            if (controller != null)
                controller.EnsureGameplayFromLevel(levelData);

            CarryBlockJamRuntimePieceSpawner spawner = board.GetComponent<CarryBlockJamRuntimePieceSpawner>();
            if (spawner == null)
                spawner = board.gameObject.AddComponent<CarryBlockJamRuntimePieceSpawner>();

            spawner.RespawnFromLevel(levelData);
            EditorUtility.SetDirty(spawner);
        }
    }
}
