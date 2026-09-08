using Microsoft.Data.SqlClient;
using ik.Models;

namespace ik.Data;

public class DepartmanRepository
{
    private readonly string _baglantiMetni;

    public DepartmanRepository(IConfiguration configuration)
    {
        _baglantiMetni = configuration.GetConnectionString("GorevDb")!;
    }

    // ════════════════════════════════════════════════════════
    //  YARDIMCI: satırı nesneye çevir
    // ════════════════════════════════════════════════════════
    private Departman SatiriNesneyeCevir(SqlDataReader okuyucu)
    {
        Departman d = new Departman();

        d.DepartmanId = okuyucu.GetInt64(okuyucu.GetOrdinal("departman_id"));
        d.DepartmanAd = okuyucu.GetString(okuyucu.GetOrdinal("departman_ad"));
        d.CreatedDate = okuyucu.GetDateTime(okuyucu.GetOrdinal("created_date"));

        // ⭐ BIT sütunu → GetBoolean
        d.IsActive = okuyucu.GetBoolean(okuyucu.GetOrdinal("is_active"));

        // ⭐ NULL olabilen METİN — kontrolsüz okursak uygulama çöker
        int aciklamaSutun = okuyucu.GetOrdinal("aciklama");
        d.Aciklama = okuyucu.IsDBNull(aciklamaSutun)
            ? null
            : okuyucu.GetString(aciklamaSutun);

        // NULL olabilen TARİH — aynı mantık
        int guncellemeSutun = okuyucu.GetOrdinal("updated_date");
        d.UpdatedDate = okuyucu.IsDBNull(guncellemeSutun)
            ? null
            : okuyucu.GetDateTime(guncellemeSutun);

        return d;
    }

    // ════════════════════════════════════════════════════════
    //  1) READ — tüm aktif departmanlar + personel sayıları
    // ════════════════════════════════════════════════════════
    public List<Departman> TumunuGetir()
    {
        List<Departman> liste = new List<Departman>();

        // Alt sorgu: her departman için o departmandaki aktif personeli say
        string sql = @"SELECT d.departman_id, d.departman_ad, d.aciklama,
                              d.created_date, d.updated_date, d.is_active,
                              (SELECT COUNT(*) FROM personel p
                               WHERE p.departman_id = d.departman_id
                                 AND p.is_active = 1) AS personel_sayisi
                       FROM departman d
                       WHERE d.is_active = 1
                       ORDER BY d.departman_ad";

        using (SqlConnection baglanti = new SqlConnection(_baglantiMetni))
        using (SqlCommand komut = new SqlCommand(sql, baglanti))
        {
            baglanti.Open();

            using (SqlDataReader okuyucu = komut.ExecuteReader())
            {
                while (okuyucu.Read())
                {
                    Departman d = SatiriNesneyeCevir(okuyucu);
                    d.PersonelSayisi = okuyucu.GetInt32(okuyucu.GetOrdinal("personel_sayisi"));
                    liste.Add(d);
                }
            }
        }

        return liste;
    }

    // ════════════════════════════════════════════════════════
    //  2) READ — tek departman
    // ════════════════════════════════════════════════════════
    public Departman? IdIleGetir(long id)
    {
        Departman? sonuc = null;

        string sql = @"SELECT departman_id, departman_ad, aciklama,
                              created_date, updated_date, is_active
                       FROM departman
                       WHERE departman_id = @id";

        using (SqlConnection baglanti = new SqlConnection(_baglantiMetni))
        using (SqlCommand komut = new SqlCommand(sql, baglanti))
        {
            komut.Parameters.AddWithValue("@id", id);
            baglanti.Open();

            using (SqlDataReader okuyucu = komut.ExecuteReader())
            {
                if (okuyucu.Read())
                    sonuc = SatiriNesneyeCevir(okuyucu);
            }
        }

        return sonuc;
    }

    // ════════════════════════════════════════════════════════
    //  3) CREATE
    // ════════════════════════════════════════════════════════
    public void Ekle(Departman departman)
    {
        string sql = @"INSERT INTO departman
                          (departman_ad, aciklama, created_date, updated_date, is_active)
                       VALUES
                          (@ad, @aciklama, @createdDate, NULL, 1)";

        using (SqlConnection baglanti = new SqlConnection(_baglantiMetni))
        using (SqlCommand komut = new SqlCommand(sql, baglanti))
        {
            komut.Parameters.AddWithValue("@ad", departman.DepartmanAd);

            // ⭐ NULL DEĞER GÖNDERME — çok önemli!
            //    AddWithValue(..., null) yazarsan çalışmaz.
            //    C#'ın null'ı ile SQL'in NULL'ı farklı şeylerdir;
            //    aradaki köprü DBNull.Value'dur.
            komut.Parameters.AddWithValue("@aciklama",
                (object?)departman.Aciklama ?? DBNull.Value);

            komut.Parameters.AddWithValue("@createdDate", DateTime.Now);

            baglanti.Open();
            komut.ExecuteNonQuery();
        }
    }

    // ════════════════════════════════════════════════════════
    //  4) UPDATE
    // ════════════════════════════════════════════════════════
    public void Guncelle(Departman departman)
    {
        // ⚠️ WHERE'i unutursan TÜM departmanlar aynı isme dönüşür
        string sql = @"UPDATE departman
                       SET departman_ad  = @ad,
                           aciklama     = @aciklama,
                           updated_date = @updatedDate
                       WHERE departman_id = @id";

        using (SqlConnection baglanti = new SqlConnection(_baglantiMetni))
        using (SqlCommand komut = new SqlCommand(sql, baglanti))
        {
            komut.Parameters.AddWithValue("@ad", departman.DepartmanAd);
            komut.Parameters.AddWithValue("@aciklama",
                (object?)departman.Aciklama ?? DBNull.Value);
            komut.Parameters.AddWithValue("@updatedDate", DateTime.Now);
            komut.Parameters.AddWithValue("@id", departman.DepartmanId);

            baglanti.Open();
            komut.ExecuteNonQuery();
        }
    }

    // ════════════════════════════════════════════════════════
    //  5) DELETE — soft delete
    // ════════════════════════════════════════════════════════
    public void PasifYap(long id)
    {
        string sql = @"UPDATE departman
                       SET is_active = 0, updated_date = @updatedDate
                       WHERE departman_id = @id";

        using (SqlConnection baglanti = new SqlConnection(_baglantiMetni))
        using (SqlCommand komut = new SqlCommand(sql, baglanti))
        {
            komut.Parameters.AddWithValue("@updatedDate", DateTime.Now);
            komut.Parameters.AddWithValue("@id", id);

            baglanti.Open();
            komut.ExecuteNonQuery();
        }
    }

    // ════════════════════════════════════════════════════════
    //  Bu departmanda kaç aktif personel var?
    //  (Silme kontrolü için kullanılır: personeli olan departman silinemez)
    // ════════════════════════════════════════════════════════
    public int AktifPersonelSayisi(long departmanId)
    {
        string sql = @"SELECT COUNT(*) FROM personel
                       WHERE departman_id = @id AND is_active = 1";

        using (SqlConnection baglanti = new SqlConnection(_baglantiMetni))
        using (SqlCommand komut = new SqlCommand(sql, baglanti))
        {
            komut.Parameters.AddWithValue("@id", departmanId);
            baglanti.Open();
            return Convert.ToInt32(komut.ExecuteScalar());
        }
    }
}