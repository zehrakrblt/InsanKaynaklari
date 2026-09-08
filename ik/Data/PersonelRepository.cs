using Microsoft.Data.SqlClient;
using ik.Models;

namespace ik.Data;

public class PersonelRepository
{
    private readonly string _baglantiMetni;

    public PersonelRepository(IConfiguration configuration)
    {
        _baglantiMetni = configuration.GetConnectionString("GorevDb")!;
    }

    // ════════════════════════════════════════════════════════
    //  YARDIMCI: satırı nesneye çevir
    //
    //  ⚠️ Bu tabloda tek NULL olabilen sütun: updated_date
    // ════════════════════════════════════════════════════════
    private Personel SatiriNesneyeCevir(SqlDataReader okuyucu)
    {
        Personel p = new Personel();

        p.PersonelId = okuyucu.GetInt64(okuyucu.GetOrdinal("personel_id"));
        p.DepartmanId = okuyucu.GetInt64(okuyucu.GetOrdinal("departman_id"));
        p.Ad = okuyucu.GetString(okuyucu.GetOrdinal("ad"));
        p.Soyad = okuyucu.GetString(okuyucu.GetOrdinal("soyad"));
        p.Tc = okuyucu.GetString(okuyucu.GetOrdinal("tc"));
        p.DogumTarihi = okuyucu.GetDateTime(okuyucu.GetOrdinal("dogum_tarihi"));
        p.Cinsiyet = okuyucu.GetString(okuyucu.GetOrdinal("cinsiyet"));
        p.Telefon = okuyucu.GetString(okuyucu.GetOrdinal("telefon"));
        p.Eposta = okuyucu.GetString(okuyucu.GetOrdinal("eposta"));
        p.Pozisyon = okuyucu.GetString(okuyucu.GetOrdinal("pozisyon"));
        p.IseGirisTarihi = okuyucu.GetDateTime(okuyucu.GetOrdinal("ise_giris_tarihi"));
        p.YillikIzinHakki = okuyucu.GetInt32(okuyucu.GetOrdinal("yillik_izin_hakki"));
        p.CreatedDate = okuyucu.GetDateTime(okuyucu.GetOrdinal("created_date"));

        // BIT sütunu → GetBoolean
        p.IsActive = okuyucu.GetBoolean(okuyucu.GetOrdinal("is_active"));

        // NULL olabilen tarih — kontrolsüz okursak uygulama çöker
        int guncellemeSutun = okuyucu.GetOrdinal("updated_date");
        p.UpdatedDate = okuyucu.IsDBNull(guncellemeSutun)
            ? null
            : okuyucu.GetDateTime(guncellemeSutun);

        return p;
    }

    // ════════════════════════════════════════════════════════
    //  1) READ — tüm aktif personel (departman adıyla birlikte)
    // ════════════════════════════════════════════════════════
    public List<Personel> TumunuGetir()
    {
        List<Personel> liste = new List<Personel>();

        string sql = @"SELECT p.personel_id, p.departman_id, d.departman_ad, p.ad, p.soyad, p.tc,
                              p.dogum_tarihi, p.cinsiyet, p.telefon, p.eposta, p.pozisyon,
                              p.ise_giris_tarihi, p.yillik_izin_hakki, p.created_date,
                              p.updated_date, p.is_active
                       FROM personel p
                       INNER JOIN departman d ON d.departman_id = p.departman_id
                       WHERE p.is_active = 1
                       ORDER BY p.ad, p.soyad";

        using (SqlConnection baglanti = new SqlConnection(_baglantiMetni))
        using (SqlCommand komut = new SqlCommand(sql, baglanti))
        {
            baglanti.Open();

            using (SqlDataReader okuyucu = komut.ExecuteReader())
            {
                while (okuyucu.Read())
                {
                    Personel p = SatiriNesneyeCevir(okuyucu);

                    // JOIN'den gelen ekstra sütun
                    p.DepartmanAd = okuyucu.GetString(okuyucu.GetOrdinal("departman_ad"));

                    liste.Add(p);
                }
            }
        }

        return liste;
    }

    // ════════════════════════════════════════════════════════
    //  2) READ — tek personel (detay sayfası için)
    // ════════════════════════════════════════════════════════
    public Personel? IdIleGetir(long id)
    {
        Personel? sonuc = null;

        string sql = @"SELECT p.personel_id, p.departman_id, d.departman_ad, p.ad, p.soyad, p.tc,
                              p.dogum_tarihi, p.cinsiyet, p.telefon, p.eposta, p.pozisyon,
                              p.ise_giris_tarihi, p.yillik_izin_hakki, p.created_date,
                              p.updated_date, p.is_active
                       FROM personel p
                       INNER JOIN departman d ON d.departman_id = p.departman_id
                       WHERE p.personel_id = @id";

        using (SqlConnection baglanti = new SqlConnection(_baglantiMetni))
        using (SqlCommand komut = new SqlCommand(sql, baglanti))
        {
            komut.Parameters.AddWithValue("@id", id);
            baglanti.Open();

            using (SqlDataReader okuyucu = komut.ExecuteReader())
            {
                // while değil if — tek satır bekliyoruz
                if (okuyucu.Read())
                {
                    sonuc = SatiriNesneyeCevir(okuyucu);
                    sonuc.DepartmanAd = okuyucu.GetString(okuyucu.GetOrdinal("departman_ad"));
                }
            }
        }

        return sonuc;   // bulunamazsa null — controller kontrol etmeli
    }

    // ════════════════════════════════════════════════════════
    //  Aynı TC veya e-posta ikinci kez girilirse true döner.
    //  excludeId: düzenleme sırasında kişinin kendisini hariç tutmak için.
    // ════════════════════════════════════════════════════════
    public bool TcVeyaEpostaKullanimda(string tc, string eposta, long? excludeId = null)
    {
        string sql = @"SELECT COUNT(*) FROM personel
                       WHERE (tc = @tc OR eposta = @eposta) AND is_active = 1";

        if (excludeId.HasValue)
        {
            sql += " AND personel_id <> @excludeId";
        }

        using (SqlConnection baglanti = new SqlConnection(_baglantiMetni))
        using (SqlCommand komut = new SqlCommand(sql, baglanti))
        {
            komut.Parameters.AddWithValue("@tc", tc);
            komut.Parameters.AddWithValue("@eposta", eposta);
            if (excludeId.HasValue)
            {
                komut.Parameters.AddWithValue("@excludeId", excludeId.Value);
            }

            baglanti.Open();
            int sayi = Convert.ToInt32(komut.ExecuteScalar());
            return sayi > 0;
        }
    }

    // ════════════════════════════════════════════════════════
    //  3) CREATE
    //     personel_id yazılmaz (IDENTITY)
    //     departman_ad hiç yazılmaz (bu tabloda yok, JOIN'den gelir)
    // ════════════════════════════════════════════════════════
    public void Ekle(Personel personel)
    {
        string sql = @"INSERT INTO personel
                          (departman_id, ad, soyad, tc, dogum_tarihi, cinsiyet, telefon, eposta,
                           pozisyon, ise_giris_tarihi, yillik_izin_hakki, created_date, is_active)
                       VALUES
                          (@departmanId, @ad, @soyad, @tc, @dogumTarihi, @cinsiyet, @telefon, @eposta,
                           @pozisyon, @iseGirisTarihi, @yillikIzinHakki, @createdDate, 1)";

        using (SqlConnection baglanti = new SqlConnection(_baglantiMetni))
        using (SqlCommand komut = new SqlCommand(sql, baglanti))
        {
            komut.Parameters.AddWithValue("@departmanId", personel.DepartmanId);
            komut.Parameters.AddWithValue("@ad", personel.Ad);
            komut.Parameters.AddWithValue("@soyad", personel.Soyad);
            komut.Parameters.AddWithValue("@tc", personel.Tc);
            komut.Parameters.AddWithValue("@dogumTarihi", personel.DogumTarihi);
            komut.Parameters.AddWithValue("@cinsiyet", personel.Cinsiyet);
            komut.Parameters.AddWithValue("@telefon", personel.Telefon);
            komut.Parameters.AddWithValue("@eposta", personel.Eposta);
            komut.Parameters.AddWithValue("@pozisyon", personel.Pozisyon);
            komut.Parameters.AddWithValue("@iseGirisTarihi", personel.IseGirisTarihi);
            komut.Parameters.AddWithValue("@yillikIzinHakki", personel.YillikIzinHakki);
            komut.Parameters.AddWithValue("@createdDate", DateTime.Now);

            baglanti.Open();
            komut.ExecuteNonQuery();
        }
    }

    // ════════════════════════════════════════════════════════
    //  4) UPDATE
    // ════════════════════════════════════════════════════════
    public void Guncelle(Personel personel)
    {
        // ⚠️ WHERE'i unutursan TÜM personel aynı kayda dönüşür
        string sql = @"UPDATE personel
                       SET departman_id = @departmanId,
                           ad = @ad,
                           soyad = @soyad,
                           tc = @tc,
                           dogum_tarihi = @dogumTarihi,
                           cinsiyet = @cinsiyet,
                           telefon = @telefon,
                           eposta = @eposta,
                           pozisyon = @pozisyon,
                           ise_giris_tarihi = @iseGirisTarihi,
                           yillik_izin_hakki = @yillikIzinHakki,
                           updated_date = @updatedDate
                       WHERE personel_id = @id";

        using (SqlConnection baglanti = new SqlConnection(_baglantiMetni))
        using (SqlCommand komut = new SqlCommand(sql, baglanti))
        {
            komut.Parameters.AddWithValue("@departmanId", personel.DepartmanId);
            komut.Parameters.AddWithValue("@ad", personel.Ad);
            komut.Parameters.AddWithValue("@soyad", personel.Soyad);
            komut.Parameters.AddWithValue("@tc", personel.Tc);
            komut.Parameters.AddWithValue("@dogumTarihi", personel.DogumTarihi);
            komut.Parameters.AddWithValue("@cinsiyet", personel.Cinsiyet);
            komut.Parameters.AddWithValue("@telefon", personel.Telefon);
            komut.Parameters.AddWithValue("@eposta", personel.Eposta);
            komut.Parameters.AddWithValue("@pozisyon", personel.Pozisyon);
            komut.Parameters.AddWithValue("@iseGirisTarihi", personel.IseGirisTarihi);
            komut.Parameters.AddWithValue("@yillikIzinHakki", personel.YillikIzinHakki);
            komut.Parameters.AddWithValue("@updatedDate", DateTime.Now);
            komut.Parameters.AddWithValue("@id", personel.PersonelId);

            baglanti.Open();
            komut.ExecuteNonQuery();
        }

        // created_date'e dokunmuyoruz — kayıt tarihi değişmemeli
    }

    // ════════════════════════════════════════════════════════
    //  5) DELETE — soft delete
    // ════════════════════════════════════════════════════════
    public void PasifYap(long id)
    {
        // Kayıt SİLİNMİYOR, pasif işaretleniyor.
        // TumunuGetir() içindeki WHERE is_active = 1 onu listeden gizler.
        string sql = @"UPDATE personel
                       SET is_active = 0, updated_date = @updatedDate
                       WHERE personel_id = @id";

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
    //  Kalan izin hakkı = yıllık izin hakkı − (bu yıl ONAYLANMIŞ YILLIK izinlerin gün toplamı)
    //  Sadece izin_tipi = 'Yıllık', durum = 'Onaylandı', is_active = 1, içinde bulunulan yıl.
    // ════════════════════════════════════════════════════════
    public int KalanIzinHakkiHesapla(long personelId, int yillikIzinHakki)
    {
        string sql = @"SELECT ISNULL(SUM(gun_sayisi), 0)
                       FROM izin
                       WHERE personel_id = @personelId
                         AND izin_tipi = N'Yıllık'
                         AND durum = N'Onaylandı'
                         AND is_active = 1
                         AND YEAR(baslangic_tarihi) = @yil";

        using (SqlConnection baglanti = new SqlConnection(_baglantiMetni))
        using (SqlCommand komut = new SqlCommand(sql, baglanti))
        {
            komut.Parameters.AddWithValue("@personelId", personelId);
            komut.Parameters.AddWithValue("@yil", DateTime.Now.Year);

            baglanti.Open();
            int kullanilanGun = Convert.ToInt32(komut.ExecuteScalar());
            return yillikIzinHakki - kullanilanGun;
        }
    }

    // ════════════════════════════════════════════════════════
    //  Dashboard: toplam aktif personel sayısı
    // ════════════════════════════════════════════════════════
    public int ToplamPersonelSayisi()
    {
        string sql = "SELECT COUNT(*) FROM personel WHERE is_active = 1";

        using (SqlConnection baglanti = new SqlConnection(_baglantiMetni))
        using (SqlCommand komut = new SqlCommand(sql, baglanti))
        {
            baglanti.Open();
            return Convert.ToInt32(komut.ExecuteScalar());
        }
    }

    // ════════════════════════════════════════════════════════
    //  Dashboard: departmanlara göre personel dağılımı (çubuk grafik için)
    // ════════════════════════════════════════════════════════
    public Dictionary<string, int> DepartmanaGorePersonelDagilimi()
    {
        var sonuc = new Dictionary<string, int>();

        string sql = @"SELECT d.departman_ad, COUNT(p.personel_id) AS sayi
                       FROM departman d
                       LEFT JOIN personel p ON p.departman_id = d.departman_id AND p.is_active = 1
                       WHERE d.is_active = 1
                       GROUP BY d.departman_ad
                       ORDER BY d.departman_ad";

        using (SqlConnection baglanti = new SqlConnection(_baglantiMetni))
        using (SqlCommand komut = new SqlCommand(sql, baglanti))
        {
            baglanti.Open();
            using (SqlDataReader okuyucu = komut.ExecuteReader())
            {
                while (okuyucu.Read())
                {
                    sonuc[okuyucu.GetString(0)] = okuyucu.GetInt32(1);
                }
            }
        }

        return sonuc;
    }
}