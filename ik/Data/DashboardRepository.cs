using Microsoft.Data.SqlClient;
using ik.Models;

namespace ik.Data;

public class DashboardRepository
{
    private readonly string _baglantiMetni;

    public DashboardRepository(IConfiguration configuration)
    {
        _baglantiMetni = configuration.GetConnectionString("GorevDb")!;
    }

    /// <summary>
    /// Dashboard için gereken üç sayıyı TEK sorguda getirir.
    /// out parametreleriyle birden çok değer dışarı verilir.
    /// </summary>
    public void SayilariGetir(out int toplamPersonel, out int bekleyenIzin, out int buAyIzindeOlan)
    {
        toplamPersonel = 0;
        bekleyenIzin = 0;
        buAyIzindeOlan = 0;

        using (SqlConnection baglanti = new SqlConnection(_baglantiMetni))
        {
            baglanti.Open();

            string sql1 = "SELECT COUNT(*) FROM personel WHERE is_active = 1";
            using (SqlCommand komut1 = new SqlCommand(sql1, baglanti))
            {
                toplamPersonel = Convert.ToInt32(komut1.ExecuteScalar());
            }

            string sql2 = "SELECT COUNT(*) FROM izin WHERE durum = N'Beklemede' AND is_active = 1";
            using (SqlCommand komut2 = new SqlCommand(sql2, baglanti))
            {
                bekleyenIzin = Convert.ToInt32(komut2.ExecuteScalar());
            }

            string sql3 = @"SELECT COUNT(DISTINCT personel_id)
                            FROM izin
                            WHERE durum = N'Onaylandi'
                              AND is_active = 1
                              AND MONTH(baslangic_tarihi) = @ay
                              AND YEAR(baslangic_tarihi) = @yil";
            using (SqlCommand komut3 = new SqlCommand(sql3, baglanti))
            {
                komut3.Parameters.AddWithValue("@ay", DateTime.Now.Month);
                komut3.Parameters.AddWithValue("@yil", DateTime.Now.Year);
                buAyIzindeOlan = Convert.ToInt32(komut3.ExecuteScalar());
            }
        }
    }

    /// <summary>
    /// Departmanlara göre personel dağılımı (çubuk grafik için).
    /// </summary>
    public List<DepartmanDagilim> DepartmanDagilimi()
    {
        var liste = new List<DepartmanDagilim>();

        string sql = @"SELECT d.departman_ad, COUNT(p.personel_id) AS personel_sayisi
                       FROM departman d
                       LEFT JOIN personel p ON p.departman_id = d.departman_id AND p.is_active = 1
                       WHERE d.is_active = 1
                       GROUP BY d.departman_ad
                       ORDER BY personel_sayisi DESC";

        using (SqlConnection baglanti = new SqlConnection(_baglantiMetni))
        using (SqlCommand komut = new SqlCommand(sql, baglanti))
        {
            baglanti.Open();

            using (SqlDataReader okuyucu = komut.ExecuteReader())
            {
                while (okuyucu.Read())
                {
                    liste.Add(new DepartmanDagilim
                    {
                        DepartmanAd = okuyucu.GetString(okuyucu.GetOrdinal("departman_ad")),
                        PersonelSayisi = okuyucu.GetInt32(okuyucu.GetOrdinal("personel_sayisi"))
                    });
                }
            }
        }

        return liste;
    }
}

/// <summary>
/// Dashboard'daki çubuk grafik için küçük yardımcı sınıf.
/// Ayrı bir model dosyası olarak da tutulabilir (Models/DepartmanDagilim.cs).
/// </summary>
