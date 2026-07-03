using System.ComponentModel;
using GAITemplate;

/// <summary>
/// SRDebugger Options tab'ına level navigation kontrollerini ekler.
/// Not: SRDebugger her option'ı kendi satırında çizer — gerçek yan-yana yerleşim
/// için "Pin" özelliğini kullanın (sağ taraftaki pin ikonu).
/// </summary>
public partial class SROptions
{
    // ─── Jump To Level ────────────────────────────────────────────────────────────

    // -1 = henüz set edilmemiş → SR açıldığında getter current level'ı döner.
    // Reload sonrası yeni SROptions instance'ı oluşturulduğu için her açılışta sıfırlanır.
    private int _jumpTarget = -1;

    [Category("Jump To Level")]
    [DisplayName("Target")]
    public int JumpTarget
    {
        get
        {
            if (_jumpTarget < 0)
                return GameManager.instance != null ? GameManager.instance.level : 0;
            return _jumpTarget;
        }
        set
        {
            _jumpTarget = System.Math.Max(0, value);
            OnPropertyChanged(nameof(JumpTarget));
        }
    }

    [Category("Jump To Level")]
    [DisplayName("→ Go")]
    public void JumpGo() => LevelNavigator.GoTo(JumpTarget);

    // ─── Navigation ───────────────────────────────────────────────────────────────

    // Sırayla: Previous (sol), Next (sağ).
    [Category("Levels")]
    [DisplayName("◀ Previous Level")]
    public void PreviousLevel() => LevelNavigator.Previous();

    [Category("Levels")]
    [DisplayName("▶ Next Level")]
    public void NextLevel() => LevelNavigator.Next();

    [Category("Levels")]
    [DisplayName("⟲ Reload Current")]
    public void Reload() => LevelNavigator.Reload();

    // Sırayla: Success, Fail.
    [Category("Levels")]
    [DisplayName("✓ Force Success")]
    public void ForceSuccess() => LevelNavigator.Success();

    [Category("Levels")]
    [DisplayName("✗ Force Fail")]
    public void ForceFail() => LevelNavigator.Fail();

    // ─── Status ───────────────────────────────────────────────────────────────────

    [Category("Levels")]
    [DisplayName("Total Levels")]
    public int TotalLevels
    {
        get
        {
            if (GameManager.instance == null || GameManager.instance.LevelConfig == null) return 0;
            return GameManager.instance.LevelConfig.LevelCount;
        }
    }
}
