using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;   // SelectList için
using ik.Data;
using ik.Models;
using Microsoft.Data.SqlClient;

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

    try
    {
        _personelRepo.Ekle(personel);
        TempData["Basarili"] = $"\"{personel.Ad} {personel.Soyad}\" personeli eklendi.";

        // POST-Redirect-GET: yönlendirme yapmazsak F5'te çift kayıt olur
        return RedirectToAction("Index");
    }
    catch (SqlException ex)
    {
        BenzersizlikHatasiniIsle(ex);
        DepartmanListesiniHazirla(personel.DepartmanId);
        return View(personel);
    }
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
        try
        {
               _personelRepo.Guncelle(personel);
        TempData["Basarili"] = "Personel güncellendi.";
        return RedirectToAction("Index");
        }

        catch(SqlException ex)
        {
            //Not ögtenci kendi e postasini degistirmeden kaydederse hata almaz.
           BenzersizlikHatasiniIsle(ex);
           DepartmanListesiniHazirla(personel.DepartmanId);
           return View(personel);
        }


    }


    //  YARDIMCI sqlexceptioni kullanici dostu mesaja cevirmek icin

    private void BenzersizlikHatasiniIsle(SqlException ex)
    {
         if (ex.Number == 2627 || ex.Number == 2601)
        {
            if (ex.Message.Contains("eposta"))
            {
                ModelState.AddModelError("Eposta",
                    "Bu e-posta adresi başka bir Personele kayıtlı.");
            }
            else if (ex.Message.Contains("telefon"))
            {
                ModelState.AddModelError("Telefon",
                    "Bu telefon numarası başka bir Personele kayıtlı.");
            }
            else if (ex.Message.Contains("tc"))
            {
                ModelState.AddModelError("Tc",
                    "Bu TC kimlik numarası başka bir Personele kayıtlı.");
            }
            else
            {
                ModelState.AddModelError("", "Bu kayıt zaten mevcut.");
            }
        }
        else
        {
            // Beklenmedik veritabanı hatası — detay verme
            ModelState.AddModelError("",
                "Kayıt sırasında bir sorun oluştu. Lütfen tekrar deneyin.");
        }
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

            personel.KalanIzinHakki = _personelRepo.KalanIzinHakkiHesapla(personel.PersonelId, personel.YillikIzinHakki);

        return View(personel);
    }
}