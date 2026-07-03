namespace GAITemplate
{
    /// <summary>
    /// Placed on the trigger child that <see cref="PuzzleBoardLayout"/> adds to every floor cell.
    /// <see cref="WallAreaBuilder"/> uses this to identify floor triggers without requiring a
    /// dedicated physics layer.
    /// </summary>
    public sealed class FloorTriggerMarker : UnityEngine.MonoBehaviour { }
}
