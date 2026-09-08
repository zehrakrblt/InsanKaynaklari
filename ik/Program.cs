using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using ik.Data;

var builder = WebApplication.CreateBuilder(args);

// ── 1. "Giriş zorunlu" filtresi ─────────────────────────────
//
// ⭐ Neden global filtre, her controller'a [Authorize] değil?
//    Yarın yeni controller yazıp [Authorize] koymayı unutursan
//    o sayfa herkese açık kalır. Global filtreyle varsayılan KAPALI olur.
//    GÜVENLİK İLKESİ: varsayılan hep en kısıtlayıcı seçenek olmalı.
builder.Services.AddControllersWithViews(secenekler =>
{
    var politika = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    secenekler.Filters.Add(new AuthorizeFilter(politika));
});

// ── 2. Çerez ayarı ──────────────────────────────────────────
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(secenekler =>
    {
        secenekler.LoginPath = "/Hesap/Giris";
        secenekler.ReturnUrlParameter = "donusUrl";   // controller parametresiyle aynı olmalı!
        secenekler.ExpireTimeSpan = TimeSpan.FromHours(8);
        secenekler.SlidingExpiration = true;
        secenekler.Cookie.HttpOnly = true;            // JS çereze erişemez → XSS koruması
        secenekler.Cookie.SameSite = SameSiteMode.Lax; // CSRF koruması
        secenekler.Cookie.Name = "GorevTakip.Oturum";
    });

// ── 3. Repository ───────────────────────────────────────────
builder.Services.AddScoped<KullaniciRepository>();
builder.Services.AddScoped<DepartmanRepository>();
builder.Services.AddScoped<PersonelRepository>();
builder.Services.AddScoped<IzinRepository>();
builder.Services.AddScoped<DashboardRepository>();
var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// ⭐⭐ SIRA KRİTİK
app.UseAuthentication();   // ÖNCE: "sen kimsin?" (çerezi okur)
app.UseAuthorization();    // SONRA: "girebilir mi?" (filtreyi uygular)

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();