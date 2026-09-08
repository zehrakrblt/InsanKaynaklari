using ik.Models;

namespace ik.Models;

public class DashboardViewModel
{
    // ── Üst kartlar ──────────────────────────────────────────
    public int ToplamPersonel { get; set; }
    public int BekleyenIzinSayisi { get; set; }
    public int BuAyIzindeOlanPersonel { get; set; }

    // ── Listeler ─────────────────────────────────────────────
    // = new()  →  boş başlasın, null olmasın (view'da çökmesin)
    public List<DepartmanDagilim> DepartmanDagilimlari { get; set; } = new();
    public List<Izin> SonBekleyenIzinler { get; set; } = new();
}

/// <summary>
/// Departman başına personel dağılımı.
/// Sadece dashboard'da kullanılır, bir tabloya karşılık gelmez.
/// </summary>
public class DepartmanDagilim
{
    public string DepartmanAd { get; set; } = "";
    public int PersonelSayisi { get; set; }
}