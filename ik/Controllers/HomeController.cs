using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ik.Data;
using ik.Models;

namespace ik.Controllers;

public class HomeController : Controller
{
    private readonly DashboardRepository _dashboardRepo;
    private readonly IzinRepository _izinRepo;

    public HomeController(DashboardRepository dashboardRepo, IzinRepository izinRepo)
    {
        _dashboardRepo = dashboardRepo;
        _izinRepo = izinRepo;
    }

    // GET: /  (Dashboard)
    public IActionResult Index()
    {
        var model = new DashboardViewModel();

        // ⭐ out parametreleriyle üç sayıyı tek seferde al
        _dashboardRepo.SayilariGetir(
            out int toplamPersonel,
            out int bekleyenIzin,
            out int buAyIzindeOlan);

        model.ToplamPersonel = toplamPersonel;
        model.BekleyenIzinSayisi = bekleyenIzin;
        model.BuAyIzindeOlanPersonel = buAyIzindeOlan;

        // Çubuk grafik verisi
        model.DepartmanDagilimlari = _dashboardRepo.DepartmanDagilimi();

        // Son 5 bekleyen izin talebi
        model.SonBekleyenIzinler = _izinRepo.SonBekleyenler(5);

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}