using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;   // SelectList için
using ik.Data;
using ik.Models;

namespace ik.Controllers;


public class PersonelController : Controller
{
    private readonly PersonelRepository _personelRepo;
    private readonly DepartmanRepository _departmanRepo;

    public PersonelController(PersonelRepository personelRepo, DepartmanRepository departmanRepo)
    {
        _personelRepo = personelRepo;
        _departmanRepo = departmanRepo;
    }

  
    private void DepartmanListesiniHazirla(long? secili = null)
    {
        var departmanlar = _departmanRepo.TumunuGetir();

        // (kaynak liste, value alanı, görünen metin, seçili değer)
        ViewBag.Departmanlar = new SelectList(departmanlar, "DepartmanId", "DepartmanAd", secili);
    }

    // ════════════════════════════════════════════════════════
    //  1) LİSTELEME
    //  GET: /Gorev
    // ════════════════════════════════════════════════════════
    public IActionResult Index()
    {
        return View(_personelRepo.TumunuGetir());
    }

    // ════════════════════════════════════════════════════════
    //  2) YENİ KAYIT FORMU
    //  GET: /Gorev/Create
    // ════════════════════════════════════════════════════════
    public IActionResult Create()
    {
        DepartmanListesiniHazirla();
        return View();
    }

    // ════════════════════════════════════════════════════════
    //  3) YENİ KAYDI KAYDET
    //  POST: /Gorev/Create
    // ════════════════════════════════════════════════════════
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Personel personel)
    {
   
        if (!ModelState.IsValid)
        {
       
            DepartmanListesiniHazirla(personel.DepartmanId);
            return View(personel);   // kullanıcının yazdıkları kaybolmasın
        }

        _personelRepo.Ekle(personel);
        TempData["Basarili"] = $"\"{personel.Ad} {personel.Soyad}\" personeli eklendi.";

        // POST-Redirect-GET: yönlendirme yapmazsak F5'te çift kayıt olur
        return RedirectToAction("Index");
    }

    // ════════════════════════════════════════════════════════
    //  4) DÜZENLEME FORMU
    //  GET: /Gorev/Edit/5
    // ════════════════════════════════════════════════════════
    public IActionResult Edit(long id)
    {
        Personel? personel = _personelRepo.IdIleGetir(id);

    
        if (personel == null)
            return NotFound();

        DepartmanListesiniHazirla(personel.DepartmanId);   // mevcut departman seçili gelsin
        return View(personel);
    }

    // ════════════════════════════════════════════════════════
    //  5) DÜZENLEMEYİ KAYDET
    //  POST: /Personel/Edit/5
    // ══════════════════════════════════════════════════════
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(Personel personel)
    {
        if (!ModelState.IsValid)
        {
            DepartmanListesiniHazirla(personel.DepartmanId);
            return View(personel);
        }

        _personelRepo.Guncelle(personel);
        TempData["Basarili"] = "Personel güncellendi.";
        return RedirectToAction("Index");
    }

    // ════════════════════════════════════════════════════════
    //  6) SİLME ONAY SAYFASI
    //  GET: /Personel/Delete/5
    // ══════════════════════════════════════════════════════
    public IActionResult Delete(long id)
    {
        Personel? personel = _personelRepo.IdIleGetir(id);

        if (personel == null)
            return NotFound();

        return View(personel);
    }

    // ════════════════════════════════════════════════════════
    //  7) SİLMEYİ ONAYLA
    //  POST: /Gorev/Delete/5
    //
    //  Metot adı DeleteConfirmed çünkü C#'ta aynı isim + aynı imza ile
    //  iki metot olamaz. ActionName ile adres yine /Delete kalıyor.
    // ════════════════════════════════════════════════════════
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(long id)
    {
        _personelRepo.PasifYap(id);
        TempData["Basarili"] = "Personel silindi.";
        return RedirectToAction("Index");
    }

    // ════════════════════════════════════════════════════════
    //  8) DETAY SAYFASI
    //  GET: /Personel/Details/5
    // ════════════════════════════════════════════════════════
    public IActionResult Details(long id)
    {
        Personel? personel = _personelRepo.IdIleGetir(id);

        if (personel == null)
            return NotFound();

        return View(personel);
    }
}