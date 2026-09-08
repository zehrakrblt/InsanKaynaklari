using Microsoft.Data.SqlClient;
using ik.Models;

namespace ik.Data;

public class IzinRepository
{
    private readonly string _baglantiMetni;

    public IzinRepository(IConfiguration configuration)
    {
        _baglantiMetni = configuration.GetConnectionString("GorevDb")!;
    }

    // ════════════════════════════════════════════════════════
    //  YARDIMCI: satırı nesneye çevir
    // ════════════════════════════════════════════════════════
    private Izin SatiriNesneyeCevir(SqlDataReader okuyucu)
    {
        Izin i = new Izin();

        i.IzinId = okuyucu.GetInt64(okuyucu.GetOrdinal("izin_id"));
        i.PersonelId = okuyucu.GetInt64(okuyucu.GetOrdinal("personel_id"));
        i.IzinTipi = okuyucu.GetString(okuyucu.GetOrdinal("izin_tipi"));
        i.BaslangicTarihi = okuyucu.GetDateTime(okuyucu.GetOrdinal("baslangic_tarihi"));
        i.BitisTarihi = okuyucu.GetDateTime(okuyucu.GetOrdinal("bitis_tarihi"));
        i.GunSayisi = okuyucu.GetInt32(okuyucu.GetOrdinal("gun_sayisi"));
        i.Durum = okuyucu.GetString(okuyucu.GetOrdinal("durum"));
        i.CreatedDate = okuyucu.GetDateTime(okuyucu.GetOrdinal("created_date"));

        // BIT sütunu → GetBoolean
        i.IsActive = okuyucu.GetBoolean(okuyucu.GetOrdinal("is_active"));

        // NULL olabilen METİN — kontrolsüz okursak uygulama çöker
        int aciklamaSutun = okuyucu.GetOrdinal("aciklama");
        i.Aciklama = okuyucu.IsDBNull(aciklamaSutun)
            ? null
            : okuyucu.GetString(aciklamaSutun);

        // NULL olabilen TARİH — aynı mantık
        int guncellemeSutun = okuyucu.GetOrdinal("updated_date");
        i.UpdatedDate = okuyucu.IsDBNull(guncellemeSutun)
            ? null
            : okuyucu.GetDateTime(guncellemeSutun);

        return i;
    }

    // ════════════════════════════════════════════════════════
    //  1) READ — tüm aktif izinler (personel adı ve departmanla birlikte)
    // ════════════════════════════════════════════════════════
    public List<Izin> TumunuGetir()
    {
        List<Izin> liste = new List<Izin>();

        string sql = @"SELECT iz.izin_id, iz.personel_id, p.ad + ' ' + p.soyad AS personel_adi,
                              d.departman_ad, iz.izin_tipi, iz.baslangic_tarihi, iz.bitis_tarihi,
                              iz.gun_sayisi, iz.aciklama, iz.durum, iz.created_date,
                              iz.updated_date, iz.is_active
                       FROM izin iz
                       INNER JOIN personel p ON p.personel_id = iz.personel_id
                       INNER JOIN departman d ON d.departman_id = p.departman_id
                       WHERE iz.is_active = 1
                       ORDER BY iz.baslangic_tarihi DESC";

        using (SqlConnection baglanti = new SqlConnection(_baglantiMetni))
        using (SqlCommand komut = new SqlCommand(sql, baglanti))
        {
            baglanti.Open();

            using (SqlDataReader okuyucu = komut.ExecuteReader())
            {
                while (okuyucu.Read())
                {
                    Izin i = SatiriNesneyeCevir(okuyucu);
                    i.PersonelAdi = okuyucu.GetString(okuyucu.GetOrdinal("personel_adi"));
                    i.DepartmanAd = okuyucu.GetString(okuyucu.GetOrdinal("departman_ad"));
                    liste.Add(i);
                }
            }
        }

        return liste;
    }

    // ════════════════════════════════════════════════════════
    //  2) READ — tek izin
    // ════════════════════════════════════════════════════════
    public Izin? IdIleGetir(long id)
    {
        Izin? sonuc = null;

        string sql = @"SELECT iz.izin_id, iz.personel_id, p.ad + ' ' + p.soyad AS personel_adi,
                              d.departman_ad, iz.izin_tipi, iz.baslangic_tarihi, iz.bitis_tarihi,
                              iz.gun_sayisi, iz.aciklama, iz.durum, iz.created_date,
                              iz.updated_date, iz.is_active
                       FROM izin iz
                       INNER JOIN personel p ON p.personel_id = iz.personel_id
                       INNER JOIN departman d ON d.departman_id = p.departman_id
                       WHERE iz.izin_id = @id";

        using (SqlConnection baglanti = new SqlConnection(_baglantiMetni))
        using (SqlCommand komut = new SqlCommand(sql, baglanti))
        {
            komut.Parameters.AddWithValue("@id", id);
            baglanti.Open();

            using (SqlDataReader okuyucu = komut.ExecuteReader())
            {
                if (okuyucu.Read())
                {
                    sonuc = SatiriNesneyeCevir(okuyucu);
                    sonuc.PersonelAdi = okuyucu.GetString(okuyucu.GetOrdinal("personel_adi"));
                    sonuc.DepartmanAd = okuyucu.GetString(okuyucu.GetOrdinal("departman_ad"));
                }
            }
        }

        return sonuc;
    }

    // ════════════════════════════════════════════════════════
    //  3) CREATE
    // ════════════════════════════════════════════════════════
    public void Ekle(Izin izin)
    {
        string sql = @"INSERT INTO izin
                          (personel_id, izin_tipi, baslangic_tarihi, bitis_tarihi, gun_sayisi,
                           aciklama, durum, created_date, is_active)
                       VALUES
                          (@personelId, @izinTipi, @baslangicTarihi, @bitisTarihi, @gunSayisi,
                           @aciklama, N'Beklemede', @createdDate, 1)";

        using (SqlConnection baglanti = new SqlConnection(_baglantiMetni))
        using (SqlCommand komut = new SqlCommand(sql, baglanti))
        {
            komut.Parameters.AddWithValue("@personelId", izin.PersonelId);
            komut.Parameters.AddWithValue("@izinTipi", izin.IzinTipi);
            komut.Parameters.AddWithValue("@baslangicTarihi", izin.BaslangicTarihi);
            komut.Parameters.AddWithValue("@bitisTarihi", izin.BitisTarihi);
            komut.Parameters.AddWithValue("@gunSayisi", izin.GunSayisi);

            // ⭐ NULL DEĞER GÖNDERME — çok önemli!
            //    AddWithValue(..., null) yazarsan çalışmaz.
            //    C#'ın null'ı ile SQL'in NULL'ı farklı şeylerdir;
            //    aradaki köprü DBNull.Value'dur.
            komut.Parameters.AddWithValue("@aciklama",
                (object?)izin.Aciklama ?? DBNull.Value);

            komut.Parameters.AddWithValue("@createdDate", DateTime.Now);

            baglanti.Open();
            komut.ExecuteNonQuery();
        }
    }

    // ════════════════════════════════════════════════════════
    //  4) UPDATE
    // ════════════════════════════════════════════════════════
    public void Guncelle(Izin izin)
    {
        // ⚠️ WHERE'i unutursan TÜM izinler aynı kayda dönüşür
        string sql = @"UPDATE izin
                       SET personel_id = @personelId,
                           izin_tipi = @izinTipi,
                           baslangic_tarihi = @baslangicTarihi,
                           bitis_tarihi = @bitisTarihi,
                           gun_sayisi = @gunSayisi,
                           aciklama = @aciklama,
                           updated_date = @updatedDate
                       WHERE izin_id = @id";

        using (SqlConnection baglanti = new SqlConnection(_baglantiMetni))
        using (SqlCommand komut = new SqlCommand(sql, baglanti))
        {
            komut.Parameters.AddWithValue("@personelId", izin.PersonelId);
            komut.Parameters.AddWithValue("@izinTipi", izin.IzinTipi);
            komut.Parameters.AddWithValue("@baslangicTarihi", izin.BaslangicTarihi);
            komut.Parameters.AddWithValue("@bitisTarihi", izin.BitisTarihi);
            komut.Parameters.AddWithValue("@gunSayisi", izin.GunSayisi);
            komut.Parameters.AddWithValue("@aciklama",
                (object?)izin.Aciklama ?? DBNull.Value);
            komut.Parameters.AddWithValue("@updatedDate", DateTime.Now);
            komut.Parameters.AddWithValue("@id", izin.IzinId);

            baglanti.Open();
            komut.ExecuteNonQuery();
        }
    }

   

    // ════════════════════════════════════════════════════════
    //  5) DELETE — soft delete
    // ════════════════════════════════════════════════════════
    public void PasifYap(long id)
    {
        string sql = @"UPDATE izin
                       SET is_active = 0, updated_date = @updatedDate
                       WHERE izin_id = @id";

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
    //  6) Listeden tek tıkla onayla/reddet
    // ════════════════════════════════════════════════════════
    public void DurumGuncelle(long id, string yeniDurum)
    {
        string sql = @"UPDATE izin
                       SET durum = @durum, updated_date = @updatedDate
                       WHERE izin_id = @id";

        using (SqlConnection baglanti = new SqlConnection(_baglantiMetni))
        using (SqlCommand komut = new SqlCommand(sql, baglanti))
        {
            komut.Parameters.AddWithValue("@durum", yeniDurum);
            komut.Parameters.AddWithValue("@updatedDate", DateTime.Now);
            komut.Parameters.AddWithValue("@id", id);

            baglanti.Open();
            komut.ExecuteNonQuery();
        }
    }

    // ════════════════════════════════════════════════════════
    //  Dashboard: bekleyen izin talebi sayısı
    // ════════════════════════════════════════════════════════
    public int BekleyenIzinSayisi()
    {
        string sql = "SELECT COUNT(*) FROM izin WHERE durum = N'Beklemede' AND is_active = 1";

        using (SqlConnection baglanti = new SqlConnection(_baglantiMetni))
        using (SqlCommand komut = new SqlCommand(sql, baglanti))
        {
            baglanti.Open();
            return Convert.ToInt32(komut.ExecuteScalar());
        }
    }

    // ════════════════════════════════════════════════════════
    //  Dashboard: bu ay izinde olan personel sayısı
    // ════════════════════════════════════════════════════════
    public int BuAyIzindeOlanPersonelSayisi()
    {
        string sql = @"SELECT COUNT(DISTINCT personel_id)
                       FROM izin
                       WHERE durum = N'Onaylandı'
                         AND is_active = 1
                         AND MONTH(baslangic_tarihi) = @ay
                         AND YEAR(baslangic_tarihi) = @yil";

        using (SqlConnection baglanti = new SqlConnection(_baglantiMetni))
        using (SqlCommand komut = new SqlCommand(sql, baglanti))
        {
            komut.Parameters.AddWithValue("@ay", DateTime.Now.Month);
            komut.Parameters.AddWithValue("@yil", DateTime.Now.Year);
            baglanti.Open();
            return Convert.ToInt32(komut.ExecuteScalar());
        }
    }

    // ════════════════════════════════════════════════════════
    //  Dashboard: son 5 bekleyen izin talebi
    // ════════════════════════════════════════════════════════
    public List<Izin> SonBekleyenler(int adet = 5)
    {
        var liste = new List<Izin>();

        string sql = @"SELECT TOP (@adet) iz.izin_id, iz.personel_id, p.ad + ' ' + p.soyad AS personel_adi,
                              d.departman_ad, iz.izin_tipi, iz.baslangic_tarihi, iz.bitis_tarihi,
                              iz.gun_sayisi, iz.aciklama, iz.durum, iz.created_date,
                              iz.updated_date, iz.is_active
                       FROM izin iz
                       INNER JOIN personel p ON p.personel_id = iz.personel_id
                       INNER JOIN departman d ON d.departman_id = p.departman_id
                       WHERE iz.durum = N'Beklemede' AND iz.is_active = 1
                       ORDER BY iz.created_date DESC";

        using (SqlConnection baglanti = new SqlConnection(_baglantiMetni))
        using (SqlCommand komut = new SqlCommand(sql, baglanti))
        {
            komut.Parameters.AddWithValue("@adet", adet);
            baglanti.Open();

            using (SqlDataReader okuyucu = komut.ExecuteReader())
            {
                while (okuyucu.Read())
                {
                    Izin i = SatiriNesneyeCevir(okuyucu);
                    i.PersonelAdi = okuyucu.GetString(okuyucu.GetOrdinal("personel_adi"));
                    i.DepartmanAd = okuyucu.GetString(okuyucu.GetOrdinal("departman_ad"));
                    liste.Add(i);
                }
            }
        }



        return liste;
    }
}