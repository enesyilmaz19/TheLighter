using System.Collections.Generic;

namespace Cakmak.Kurallar
{
    // Casus (docs/Oyunlar.md, 4; Imposter da bu): herkes gizli bir mekânı (ya da kelimeyi) bilir, casus bilmez. Turlar
    // normal çakmak, sorular mekâna göre. Her 4 turda bir ve oyun biterken "Casus kim?" oylaması (Oyun.Suclama.cs).
    // Casus istediği an bir kez tahmin eder; yakalanınca son şansı olarak tahmin eder.

    public enum CasusKazanan { Grup, Casus }

    public enum CasusNedeni
    {
        /// <summary>Grup kazandı: casus yakalandı, son şansında bilemedi (ya da süresi doldu).</summary>
        Yakalandi,
        /// <summary>Grup kazandı: casus yakalanmadan tahmin etti, bilemedi.</summary>
        YanlisTahmin,
        /// <summary>Grup kazandı: casus oyundan çıktı.</summary>
        Kacti,
        /// <summary>Casus kazandı: yakalanmadan doğru tahmin etti.</summary>
        Bildi,
        /// <summary>Casus kazandı: yakalandı ama son şansında bildi.</summary>
        SonSanstaBildi,
        /// <summary>Casus kazandı: son oylamada da yakalanmadı (ya da oyun yarıda kesildi).</summary>
        Yakalanmadi,
    }

    /// <summary>Oyun sonunda herkese. Oyun bitmeden hiçbir yerde yok.</summary>
    public sealed class CasusSonucu
    {
        public OyuncuId Casus;
        public CasusKazanan Kazanan;
        public CasusNedeni Neden;
        /// <summary>Mekân ya da kelime.</summary>
        public string Sir;
        /// <summary>Casusun tahmini; tahmin etmediyse null.</summary>
        public string Tahmin;

        internal CasusSonucu Kopya() => (CasusSonucu)MemberwiseClone();
    }

    public sealed partial class Oyun
    {
        /// <summary>Bu kadar turda bir "Casus kim?" oylaması (5., 9., 13. turun başında).</summary>
        public const int CasusSuclamaAraligi = 4;
        /// <summary>Yakalanan casusun son şansı.</summary>
        public const int CasusTahminiSn = 30;

        string casusSirri;
        string casusKategori;
        List<string> casusListesi;
        readonly Dictionary<OyuncuId, string> mekanRolleri = new Dictionary<OyuncuId, string>();
        CasusSonucu casusSonucu;

        bool Casuslu => ayar.Oyun == OyunTuru.Casus;

        /// <summary>
        /// Casusun tahmini, listeden. Yakalanmadan istediği an bir kez ("Mekânı biliyorum"), yakalanınca son şans olarak.
        /// İki durumda da oyun biter: bildiyse casus, bilemediyse grup kazanır.
        /// </summary>
        public Sonuc CasusTahmini(OyuncuId kim, string tahmin)
        {
            if (!Casuslu || asama == Asama.OyunSonu) return Sonuc.YanlisAsama;
            if (kim != hain) return Sonuc.SiraSendeDegil;
            if (!casusListesi.Contains(tahmin)) return Sonuc.GecersizGirdi;

            bool bildi = tahmin == casusSirri;
            bool sonSans = asama == Asama.CasusTahmini;
            Yay(new Olay { Tur = OlayTuru.CasusTahminEtti, Kim = kim });
            if (bildi) CasusBitir(CasusKazanan.Casus, sonSans ? CasusNedeni.SonSanstaBildi : CasusNedeni.Bildi, tahmin);
            else CasusBitir(CasusKazanan.Grup, sonSans ? CasusNedeni.Yakalandi : CasusNedeni.YanlisTahmin, tahmin);
            return Sonuc.Tamam;
        }

        // ------------------------------------------------------------ Akış

        /// <summary>
        /// Kurulumda: casus, mekân (ya da kategori ve kelime) seçilir. Mekânda casus dışındaki herkese ayrı rol.
        /// Mekân listesi bütün mekânlar, kelime listesi kategorinin kelimeleri.
        /// </summary>
        void CasusKur()
        {
            if (!Casuslu) return;
            hain = oyuncular[rastgele.Next(oyuncular.Count)].Id;
            if (ayar.Casus == CasusTuru.Mekan)
            {
                var mekan = CasusIcerik.Mekanlar[rastgele.Next(CasusIcerik.Mekanlar.Length)];
                casusListesi = new List<string>();
                foreach (var m in CasusIcerik.Mekanlar) casusListesi.Add(m.Ad);
                casusSirri = mekan.Ad;
                var roller = Karistir(new List<string>(mekan.Ogeler));
                int i = 0;
                foreach (var o in oyuncular)
                    if (o.Id != hain) mekanRolleri[o.Id] = roller[i++];
            }
            else
            {
                var grup = CasusIcerik.Kelimeler[rastgele.Next(CasusIcerik.Kelimeler.Length)];
                casusKategori = grup.Ad;
                casusListesi = new List<string>(grup.Ogeler);
                casusSirri = grup.Ogeler[rastgele.Next(grup.Ogeler.Count)];
            }
        }

        /// <summary>Oylamada yakalandı: son şans. Casus herkese açılır, listeden tahmin eder.</summary>
        void CasusYakalandi()
        {
            if (!Casuslu) return;
            AsamayaGec(Asama.CasusTahmini, CasusTahminiSn);
            Yay(new Olay { Tur = OlayTuru.CasusYakalandi, Kim = hain });
        }

        void CasusYakalanmadi()
        {
            if (Casuslu) CasusBitir(CasusKazanan.Casus, CasusNedeni.Yakalanmadi, null);
        }

        void CasusKacti()
        {
            if (Casuslu) CasusBitir(CasusKazanan.Grup, CasusNedeni.Kacti, null);
        }

        /// <summary>Oyun bitiyor ve sonuç önceden yazılmadı (oyun zorla bitti): casus yakalanmadı.</summary>
        void CasusSonucunuYaz()
        {
            if (Casuslu && casusSonucu == null) casusSonucu = YeniCasusSonucu(CasusKazanan.Casus, CasusNedeni.Yakalanmadi, null);
        }

        void CasusBitir(CasusKazanan kazanan, CasusNedeni neden, string tahmin)
        {
            casusSonucu = YeniCasusSonucu(kazanan, neden, tahmin);
            Bitir();
        }

        CasusSonucu YeniCasusSonucu(CasusKazanan kazanan, CasusNedeni neden, string tahmin) =>
            new CasusSonucu { Casus = hain, Kazanan = kazanan, Neden = neden, Sir = casusSirri, Tahmin = tahmin };

        // ------------------------------------------------------------ Görünüm

        void CasusGorunumu(Gorunum g, OyuncuId kim)
        {
            if (!Casuslu) return;
            if (kim == hain) g.Rolun = Rol.Casus;
            else
            {
                g.Sirrin = casusSirri;
                g.MekanRolun = mekanRolleri.TryGetValue(kim, out var rol) ? rol : null;
            }
            g.CasusListesi = new List<string>(casusListesi);
            g.CasusKategori = casusKategori;
            if (asama == Asama.CasusTahmini) g.Yakalanan = hain;
        }
    }
}
