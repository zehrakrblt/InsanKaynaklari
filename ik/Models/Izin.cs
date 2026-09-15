using System.ComponentModel.DataAnnotations;

namespace ik.Models;

public class Izin
{
    public long IzinId { get; set; }

    [Required(ErrorMessage = "Personel seçmelisiniz.")]
    [Display(Name = "Personel")]
    public long PersonelId { get; set; }          // yabancı anahtar

    [Required(ErrorMessage = "İzin tipi seçmelisiniz.")]
    [Display(Name = "İzin tipi")]
    public string IzinTipi { get; set; } = "Yillik";   // Yıllık / Mazeret / Hastalık / Ücretsiz

    [Required(ErrorMessage = "Başlangıç tarihi zorunludur.")]
    [DataType(DataType.Date)]
    [Display(Name = "Başlangıç tarihi")]
    public DateTime BaslangicTarihi { get; set; }

    [Required(ErrorMessage = "Bitiş tarihi zorunludur.")]
    [DataType(DataType.Date)]
    [Display(Name = "Bitiş tarihi")]
    public DateTime BitisTarihi { get; set; }

    // Veritabanına yazılır ama controller/servis tarafından hesaplanır,
    // formdan elle girilmez.
    [Display(Name = "Gün sayısı")]
    public int GunSayisi { get; set; }

    // İsteğe bağlı uzun metin
    [StringLength(500, ErrorMessage = "En fazla 500 karakter olabilir.")]
    [Display(Name = "Açıklama")]
    public string? Aciklama { get; set; }

    [Required(ErrorMessage = "Durum seçmelisiniz.")]
    [Display(Name = "Durum")]
    public string Durum { get; set; } = "Beklemede";   // Beklemede / Onaylandı / Reddedildi

    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public bool IsActive { get; set; } = true;

    // ── Veritabanında OLMAYAN alanlar ────────────────────────
    // JOIN ile gelir. INSERT/UPDATE'te KULLANILMAZ!
    [Display(Name = "Personel")]
    public string PersonelAdi { get; set; } = "";

    [Display(Name = "Departman")]
    public string DepartmanAd { get; set; } = "";

    // ── HESAPLANAN ÖZELLİKLER ────────────────────────────────
    // Veritabanında yok, her okunduğunda hesaplanır.

    /// <summary>
    /// Durumun Bootstrap rozet rengi.
    /// switch ifadesi: her değere bir sonuç eşler, "_" ise varsayılan.
    /// </summary>
    public string DurumRenk => Durum switch
    {
        "Beklemede" => "warning",
        "Onaylandi" => "success",
        "Reddedildi" => "danger",
        _ => "secondary"
    };
}