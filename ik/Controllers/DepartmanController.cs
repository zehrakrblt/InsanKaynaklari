using Microsoft.AspNetCore.Mvc;
using ik.Data;
using ik.Models;

namespace ik.Controllers;

// [Authorize] yazmaya gerek YOK — Program.cs'teki global filtre
// zaten tüm controller'ları koruyor.
public class DepartmanController : Controller
{
    private readonly DepartmanRepository _repo;

    public DepartmanController(DepartmanRepository repo)
    {
        _repo = repo;
    }

    // ════════════════════════════════════════════════════════
    //  1) LİSTELEME
    //  GET: /Departman
    // ════════════════════════════════════════════════════════
    public IActionResult Index()
    {
        return View(_repo.TumunuGetir());
    }

    // ════════════════════════════════════════════════════════
    //  2) YENİ KAYIT FORMU
    //  GET: /Departman/Create
    // ════════════════════════════════════════════════════════
    public IActionResult Create()
    {
        return View();
    }

    // ════════════════════════════════════════════════════════
    //  3) YENİ KAYDI KAYDET
    //  POST: /Departman/Create
    // ════════════════════════════════════════════════════════
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Departman departman)
    {
        // Tarayıcı doğrulaması F12 ile kandırılabilir.
        // Bu yüzden sunucuda TEKRAR kontrol ediyoruz.
        if (!ModelState.IsValid)
            return View(departman);   // kullanıcının yazdıkları kaybolmasın

        _repo.Ekle(departman);
        TempData["Basarili"] = $"\"{departman.DepartmanAd}\" departmanı eklendi.";

        // POST-Redirect-GET: yönlendirme yapmazsak F5'te çift kayıt olur
        return RedirectToAction("Index");
    }

    // ════════════════════════════════════════════════════════
    //  4) DÜZENLEME FORMU
    //  GET: /Departman/Edit/5
    // ════════════════════════════════════════════════════════
    public IActionResult Edit(long id)
    {
        Departman? departman = _repo.IdIleGetir(id);

        // Kullanıcı adres çubuğuna /Departman/Edit/99999 yazabilir.
        // null kontrolü ZORUNLU.
        if (departman == null)
            return NotFound();

        return View(departman);
    }

    // ════════════════════════════════════════════════════════
    //  5) DÜZENLEMEYİ KAYDET
    //  POST: /Departman/Edit/5
    // ════════════════════════════════════════════════════════
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(Departman departman)
    {
        if (!ModelState.IsValid)
            return View(departman);

        _repo.Guncelle(departman);
        TempData["Basarili"] = "Departman güncellendi.";
        return RedirectToAction("Index");
    }

    // ════════════════════════════════════════════════════════
    //  6) SİLME ONAY SAYFASI
    //  GET: /Departman/Delete/5
    // ════════════════════════════════════════════════════════
    public IActionResult Delete(long id)
    {
        Departman? departman = _repo.IdIleGetir(id);

        if (departman == null)
            return NotFound();

        // Silinemeyecekse kullanıcıya ÖNCEDEN söyle
        ViewBag.PersonelSayisi = _repo.AktifPersonelSayisi(id);
        return View(departman);
    }

    // ════════════════════════════════════════════════════════
    //  7) SİLMEYİ ONAYLA
    //  POST: /Departman/Delete/5
    //
    //  Metot adı DeleteConfirmed çünkü C#'ta aynı isim + aynı imza ile
    //  iki metot olamaz. ActionName ile adres yine /Delete kalıyor.
    // ════════════════════════════════════════════════════════
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(long id)
    {
        // ⭐ İlişkili kayıt kontrolü
        int personelSayisi = _repo.AktifPersonelSayisi(id);

        if (personelSayisi > 0)
        {
            TempData["Uyari"] = $"Bu departmanda {personelSayisi} personel var. " +
                                 "Önce personeli silmeli veya başka departmana taşımalısınız.";
            return RedirectToAction("Index");
        }

        _repo.PasifYap(id);
        TempData["Basarili"] = "Departman silindi.";
        return RedirectToAction("Index");
    }

    // ════════════════════════════════════════════════════════
    //  8) DETAY SAYFASI
    //  GET: /Departman/Details/5
    // ════════════════════════════════════════════════════════
    public IActionResult Details(long id)
    {
        Departman? departman = _repo.IdIleGetir(id);

        if (departman == null)
            return NotFound();

        return View(departman);
    }
}