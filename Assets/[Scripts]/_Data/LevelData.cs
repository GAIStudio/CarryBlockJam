using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace GAITemplate
{
    [Serializable]
    public struct LevelColorCell
    {
        public int row;
        public int column;
        public PieceColorType color;
        public LevelCellFlag flag;

        // Flag'e bağlı sayısal değer (Ice count vs.).
        public int flagValue;

        // Tunnel flag için yön (Front/Back/Left/Right). Diğer flag'lerde anlamı yok.
        public CellDirection direction;

        // Tunnel'dan ��ıkacak piece'lerin renk sırası. Yalnızca Tunnel flag'li cell'de anlamlı.
        public PieceColorType[] tunnelPieces;
    }

    /// <summary>Hücre yönü — şu an sadece Tunnel için kullanılır.</summary>
    public enum CellDirection
    {
        Front = 0,
        Back  = 1,
        Left  = 2,
        Right = 3,
    }

    [Serializable]
    public class TutorialStage
    {
        public string stageName = "New Stage";

        [TextArea(2, 4)]
        public string instruction = "";

        public Vector3 targetPos    = Vector3.zero;
        public Vector3 handRotation = Vector3.zero;

        public System.Collections.Generic.List<Vector2Int> clickableCells =
            new System.Collections.Generic.List<Vector2Int>();
    }

    /// <summary>
    /// Bir cell'in renkle ek olarak taşıdığı özel durumlar. Bitmask — birden fazla
    /// flag aynı cell'de bulunabilir (örn. Hidden + Ice). Mekanikleri sonra eklenecek.
    /// </summary>
    [Flags]
    public enum LevelCellFlag
    {
        None    = 0,
        Hidden  = 1 << 0,
        Ice     = 1 << 1,
        Tunnel  = 1 << 2,
    }

    [CreateAssetMenu(fileName = "LevelData", menuName = "GAITemplate/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Header("Mechanic")]
        [Tooltip("Bu level hangi puzzle mekaniği için tasarlanmış. " +
                 "LevelCreatorWindow buna göre uygun editörü gösterir.")]
        public PuzzleMechanicType mechanicType = PuzzleMechanicType.Grid;

        [Header("Grid / Slide Lane")]
        [Tooltip("Grid: satır sayısı. SlideLane: depth (her lane'deki slot sayısı).")]
        [Min(1)] public int gridRows = 4;

        [Tooltip("Grid: sütun sayısı. SlideLane: lane sayısı.")]
        [Min(1)] public int gridColumns = 4;

        [Tooltip("Per-cell colors. Empty cells use None.")]
        public LevelColorCell[] colorCells;

        [Header("Tutorial")]
        public bool hasTutorial;

        [Tooltip("Instruction text'inin world position'u — Camera ile screen-space'e dönüştürülür.")]
        public Vector3 tutorialTextWorldPosition = Vector3.zero;

        public System.Collections.Generic.List<TutorialStage> tutorialStages =
            new System.Collections.Generic.List<TutorialStage>();

        [Header("Camera")]
        public Vector3 cameraPosition = new Vector3(0f, 10f, -8f);
        public Vector3 cameraRotation = new Vector3(45f, 0f, 0f);
        public bool cameraOrthographic = true;
        public float cameraOrthographicSize = 6f;
        public float cameraFieldOfView = 60f;

        [Header("Deprecated")]
        [FormerlySerializedAs("levelPrefab")]
        [HideInInspector]
        public GameObject levelPrefab;

        [HideInInspector] public string gridKey;
        [HideInInspector] public GameObject defaultCellPrefab;
        [HideInInspector] public GridSpawnProfile spawnProfile;

        public PieceColorType GetCellColor(int row, int column)
        {
            if (colorCells == null)
                return PieceColorType.None;

            for (int i = 0; i < colorCells.Length; i++)
            {
                LevelColorCell cell = colorCells[i];
                if (cell.row == row && cell.column == column)
                    return cell.color;
            }

            return PieceColorType.None;
        }

        public LevelCellFlag GetCellFlag(int row, int column)
        {
            if (colorCells == null)
                return LevelCellFlag.None;

            for (int i = 0; i < colorCells.Length; i++)
            {
                LevelColorCell cell = colorCells[i];
                if (cell.row == row && cell.column == column)
                    return cell.flag;
            }

            return LevelCellFlag.None;
        }

        public bool HasCellFlag(int row, int column, LevelCellFlag flag) =>
            (GetCellFlag(row, column) & flag) == flag;

        public int GetCellFlagValue(int row, int column)
        {
            if (colorCells == null)
                return 0;

            for (int i = 0; i < colorCells.Length; i++)
            {
                LevelColorCell cell = colorCells[i];
                if (cell.row == row && cell.column == column)
                    return cell.flagValue;
            }

            return 0;
        }

        public CellDirection GetCellDirection(int row, int column)
        {
            if (colorCells == null)
                return CellDirection.Front;

            for (int i = 0; i < colorCells.Length; i++)
            {
                LevelColorCell cell = colorCells[i];
                if (cell.row == row && cell.column == column)
                    return cell.direction;
            }

            return CellDirection.Front;
        }

        public PieceColorType[] GetCellTunnelPieces(int row, int column)
        {
            if (colorCells == null)
                return System.Array.Empty<PieceColorType>();

            for (int i = 0; i < colorCells.Length; i++)
            {
                LevelColorCell cell = colorCells[i];
                if (cell.row == row && cell.column == column)
                    return cell.tunnelPieces ?? System.Array.Empty<PieceColorType>();
            }

            return System.Array.Empty<PieceColorType>();
        }
    }
}
