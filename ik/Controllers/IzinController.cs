
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using ik.Data;
using ik.Models;

namespace ik.Controllers;

// [Authorize] yazmaya gerek YOK — Program.cs'teki global filtre
// zaten tüm controller'ları koruyor.
public class IzinController : Controller
{
    private readonly IzinRepository _izinrepo;
    private readonly PersonelRepository _personelRepo;

    public IzinController(IzinRepository repo, PersonelRepository personelRepo)
    {
        _izinrepo = repo;
        _personelRepo = personelRepo;
    }

    private void PersonelListesiniHazirla(long? secili = null)
    {
        var personeller = _personelRepo.TumunuGetir();

        // personel adı+soyadını tek metinde göstermek için anonim liste
        var liste = personeller.Select(p => new { p.PersonelId, AdSoyad = $"{p.Ad} {p.Soyad}" });
        ViewBag.Personeller = new SelectList(liste, "PersonelId", "AdSoyad", secili);
    }



    // GET: /Izin/Create yeni kayit formu
    public IActionResult Create()
    {
        PersonelListesiniHazirla();
        return View();
    }

    // POST: /Izin/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Izin izin)
    {
        // Bitiş tarihi başlangıçtan önce olamaz
        if (izin.BitisTarihi < izin.BaslangicTarihi)
        {
            ModelState.AddModelError("", "Bitiş tarihi başlangıç tarihinden önce olamaz.");
        }

        if (!ModelState.IsValid)
        {
            PersonelListesiniHazirla(izin.PersonelId);
            return View(izin);
        }

        // Gün sayısı: basit takvim günü hesabı
        izin.GunSayisi = (izin.BitisTarihi - izin.BaslangicTarihi).Days + 1;

        _izinrepo.Ekle(izin);
        TempData["Basarili"] = "İzin talebi eklendi.";

        // POST-Redirect-GET: F5'te çift kayıt olmasın
        return RedirectToAction("Index");
    }

    // GET: /Izin/Edit/5
    public IActionResult Edit(long id)
    {
        var izin = _izinrepo.IdIleGetir(id);
        if (izin == null) return NotFound();

        PersonelListesiniHazirla(izin.PersonelId);
        return View(izin);
    }

    // POST: /Izin/Edit/5/düzenlemeyi kaydet
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(Izin izin)
    {
        if (izin.BitisTarihi < izin.BaslangicTarihi)
        {
            ModelState.AddModelError("", "Bitiş tarihi başlangıç tarihinden önce olamaz.");
        }

        if (!ModelState.IsValid)
        {
            PersonelListesiniHazirla(izin.PersonelId);
            return View(izin);
        }

        izin.GunSayisi = (izin.BitisTarihi - izin.BaslangicTarihi).Days + 1;

        _izinrepo.Guncelle(izin);
        TempData["Basarili"] = "İzin güncellendi.";
        return RedirectToAction("Index");
    }

    
    // ════════════════════════════════════════════════════════
    //  8) DETAY SAYFASI
    //  GET: /Izin/Details/5
    // ════════════════════════════════════════════════════════
    public IActionResult Details(long id)
    {
        Izin? izin = _izinrepo.IdIleGetir(id);

        if (izin == null)
            return NotFound();

        return View(izin);
    }

    // GET: /Izin/Delete/5/silme onay sayfasi
     public IActionResult Delete(long id)
    {
        Izin? izin = _izinrepo.IdIleGetir(id);

        if (izin == null)
            return NotFound();

        return View(izin);
    }

    // POST: /Izin/Delete/5/Silmeyi onayla
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(long id)
    {
        _izinrepo.PasifYap(id);
        TempData["Basarili"] = "İzin silindi.";
        return RedirectToAction("Index");
    }

    // POST: /Izin/Onayla/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Onayla(long id)
    {
        _izinrepo.DurumGuncelle(id, "Onaylandi");
        TempData["Basarili"] = "İzin onaylandi.";
        return RedirectToAction("Index");
    }

    // POST: /Izin/Reddet/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Reddet(long id)
    {
        _izinrepo.DurumGuncelle(id, "Reddedildi");
        TempData["Basarili"] = "İzin reddedildi.";
        return RedirectToAction("Index");
    }

    // GET: /Izin?arama=rapor&personelId=2&durum=Beklemede
public IActionResult Index(string? arama, long? personelId, string? durum,
                           bool sadeceReddedilenler = false)
{
    var liste = _izinrepo.Filtrele(arama, personelId, durum, sadeceReddedilenler);

    // ⭐ Filtre değerlerini View'a geri gönder — form dolu kalsın
    ViewBag.Arama          = arama;
    ViewBag.SeciliPersonel = personelId;
    ViewBag.SeciliDurum    = durum;
    ViewBag.SadeceReddedilenler = sadeceReddedilenler;

    PersonelListesiniHazirla(personelId);

    return View(liste);
}
}