using System.ComponentModel.DataAnnotations;

namespace ik.Models;

public class Personel
{
    public long PersonelId { get; set; }

    [Required(ErrorMessage = "Departman seçmelisiniz.")]
    [Display(Name = "Departman")]
    public long DepartmanId { get; set; }

    // Veritabanında YOK — JOIN ile gelir. Listede departman adını göstermek için.
    public string DepartmanAd { get; set; } = "";

    [Required(ErrorMessage = "Ad zorunludur.")]
    [StringLength(100, ErrorMessage = "En fazla 100 karakter olabilir.")]
    [Display(Name = "Ad")]
    public string Ad { get; set; } = "";

    [Required(ErrorMessage = "Soyad zorunludur.")]
    [StringLength(100, ErrorMessage = "En fazla 100 karakter olabilir.")]
    [Display(Name = "Soyad")]
    public string Soyad { get; set; } = "";

   // ⭐ YENİ: desen (regex) kontrolü
    [Required(ErrorMessage = "TC kimlik numarası zorunludur.")]
    [RegularExpression(@"^[1-9][0-9]{10}$",
        ErrorMessage = "TC kimlik numarası 11 haneli olmalı ve 0 ile başlamamalıdır.")]
    [Display(Name = "TC kimlik no")]
    public string Tc { get; set; } = "";

    [Required(ErrorMessage = "Doğum tarihi zorunludur.")]
    [DataType(DataType.Date)]
    [Display(Name = "Doğum Tarihi")]
    public DateTime DogumTarihi { get; set; }

    [Required(ErrorMessage = "Cinsiyet zorunludur.")]
    [Display(Name = "Cinsiyet")]
    public string Cinsiyet { get; set; } = "";

    [Required(ErrorMessage = "Telefon zorunludur.")]
    [StringLength(20, ErrorMessage = "En fazla 20 karakter olabilir.")]
    [Display(Name = "Telefon")]
    public string Telefon { get; set; } = "";

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta giriniz.")]
    [Display(Name = "E-posta")]
    public string Eposta { get; set; } = "";

    [Required(ErrorMessage = "Pozisyon zorunludur.")]
    [StringLength(100, ErrorMessage = "En fazla 100 karakter olabilir.")]
    [Display(Name = "Pozisyon")]
    public string Pozisyon { get; set; } = "";

    [Required(ErrorMessage = "İşe giriş tarihi zorunludur.")]
    [DataType(DataType.Date)]
    [Display(Name = "İşe Giriş Tarihi")]
    public DateTime IseGirisTarihi { get; set; }

    [Display(Name = "Yıllık İzin Hakkı")]
    public int YillikIzinHakki { get; set; } = 14;

    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public bool IsActive { get; set; } = true;

    // Veritabanında YOK — hesaplanır. Kıdem (kaç yıldır çalışıyor).
    public int Kidem => DateTime.Now.Year - IseGirisTarihi.Year -
        (DateTime.Now.Date < IseGirisTarihi.Date.AddYears(DateTime.Now.Year - IseGirisTarihi.Year) ? 1 : 0);

    // Veritabanında YOK — hesaplanır. Kalan yıllık izin günü (dashboard/detay için doldurulur).
    public int KalanIzinHakki { get; set; }
}