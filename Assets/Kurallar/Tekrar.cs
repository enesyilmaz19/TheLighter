using System;
using System.Collections.Generic;

namespace Cakmak.Kurallar
{
    // S3 — Tekrar oynatma tipleri (Ana Plan 8. bölüm): cezalar, Kızgın Çakmak, kaos turları,
    // gizli görevler, unvanlar, oyun özeti, grup kaydı. Kurallar Oyun.Tekrar.cs'te.

    public enum CezaSeviyesi { Kapali, Hafif, Cesur }

    public enum CezaTuru
    {
        /// <summary>🗣️ Hemen yapılır, şeref sözü. Metni <see cref="Ayarlar.SesliCezalar"/>'dan.</summary>
        Sesli,
        /// <summary>⚙️ Birkaç tur adının yanında 🤡 durur.</summary>
        Palyaco,
        /// <summary>⚙️ Bir sonraki sorusu havuzdan atanır, kendisi yazamaz.</summary>
        HavuzdanSor,
        /// <summary>⚙️ Bir sonraki mini oyununda beraberlik rakibin sayılır.</summary>
        BeraberlikRakibe,
        /// <summary>⚙️ Bir sonraki sorduğu soru, mini oyun ne olursa olsun ifşa olur.</summary>
        SorunIfsa,
    }

    public sealed class Ceza
    {
        public CezaTuru Tur;
        /// <summary>Sadece Sesli.</summary>
        public string Metin;
        /// <summary>Palyaco: kalan tur. Diğer kısıtlamalar: 1 (bir kez işler, sonra kalkar). Sesli: 0.</summary>
        public int Kalan;

        // Palyaco çekildiği turun bitişinde düşmesin diye.
        internal bool Yeni;

        internal Ceza Kopya() => (Ceza)MemberwiseClone();
    }

    public enum CekimNedeni { Kaybetti, Bedel, Yandi }

    public sealed class CezaCekimi
    {
        public OyuncuId Kim;
        public Ceza Ceza;
        public CekimNedeni Neden;
    }

    public enum IsiSeviyesi { Soguk, Ilik, Sicak, Kizgin }

    public enum KaosKurali
    {
        Yok,
        /// <summary>B kazanırsa soru ifşa olur.</summary>
        TersDunya,
        /// <summary>Mini oyun yok. A ve B hariç herkes oylar.</summary>
        GrupKarari,
        /// <summary>A havuzdan soru çeker ama kendisi de görmez.</summary>
        KorSoru,
        /// <summary>Bu tur A'nın kim olduğu gizli.</summary>
        SoranGizli,
        /// <summary>Beraberlik yok: berabere biterse B kaybeder.</summary>
        AniOlum,
        /// <summary>Kaybeden ceza çeker, ceza seviyesi Kapalı olsa bile.</summary>
        CezaTuru,
        /// <summary>Sıra C'ye değil, rastgele birine geçer.</summary>
        YonDegisti,
        /// <summary>Kaybeden bir el daha isteyebilir. Yine kaybederse cezası ikiye katlanır.</summary>
        IkiyeKatla,
    }

    public enum Zorluk { Kolay, Orta, Zor }

    public enum GorevTuru
    {
        /// <summary>Hedef'i en az Adet kez göster (B iken C olarak seç).</summary>
        KisiyiGoster,
        /// <summary>En az Adet farklı kişiyi göster.</summary>
        FarkliKisiGoster,
        /// <summary>En az Adet mini oyun kazan.</summary>
        MiniOyunKazan,
        /// <summary>En az bir kez B ol, B olduğun hiçbir soru ifşa olmasın.</summary>
        HicIfsaOlma,
        /// <summary>Aynı kişiye en az Adet kez soru sor.</summary>
        AyniKisiyeSor,
        /// <summary>Hedef seni en az Adet kez göstersin.</summary>
        SeniGostersin,
        /// <summary>Hiç gösterilme.</summary>
        HicGosterilme,
        /// <summary>Bir kaos turunda mini oyun kazan.</summary>
        KaosTurundaKazan,
    }

    /// <summary>Gizli görev. Metni istemci yazar (Türü + Hedef + Adet). Sadece sahibinin görünümünde.</summary>
    public sealed class Gorev
    {
        public GorevTuru Tur;
        public OyuncuId? Hedef;
        public int Adet;
        public Zorluk Zorluk;

        internal Gorev Kopya() => (Gorev)MemberwiseClone();
    }

    public sealed class GorevSonucu
    {
        public OyuncuId Kim;
        public Gorev Gorev;
        public bool Basarili;
    }

    public enum UnvanTuru
    {
        Miknatis,       // en çok gösterilen
        Gammaz,         // en çok ifşa ettiren (C olarak)
        SirKupu,        // en çok soruyu güme gönderen (B olarak)
        HedefTahtasi,   // en çok soru alan
        SoruMakinesi,   // en çok soru soran
        Sansli,         // en çok mini oyun kazanan
        Takintili,      // aynı kişiye en çok soru soran
        Intikamci,      // kendisini gösterene hemen soru soran
        AtesleOynayan,  // Kızgın Çakmak'ta en çok yanan
        Bedelci,        // en çok bedel ödeyen
        Cezakes,        // en çok ceza çeken
        KaosCanavari,   // kaos turlarında en çok kazanan
        Kale,           // B iken soruları hiç ifşa olmayan
        Gorunmez,       // hiç gösterilmeyen
        Talihsiz,       // hiç mini oyun kazanamayan
    }

    public sealed class Unvan
    {
        public UnvanTuru Tur;
        public OyuncuId Kim;
        /// <summary>Unvanı getiren sayı (kaç kez gösterildi vb.).</summary>
        public int Deger;
    }

    public sealed class IfsaKaydi
    {
        public string Soru;
        public OyuncuId A;
        public OyuncuId B;
        public OyuncuId C;
    }

    /// <summary>
    /// Oyunun özeti: sayaçlar, en uç 3 unvan, görevler, ifşa olan sorular.
    /// <b>Güme giden soru burada yoktur.</b> Fısıltıyla sorulan (metni olmayan) soru da yoktur.
    /// </summary>
    public sealed class OyunOzeti
    {
        public int OynananTur;
        public IReadOnlyList<OyuncuDurumu> Oyuncular = Array.Empty<OyuncuDurumu>();
        public IReadOnlyList<Unvan> Unvanlar = Array.Empty<Unvan>();
        public IReadOnlyList<GorevSonucu> Gorevler = Array.Empty<GorevSonucu>();
        public IReadOnlyList<IfsaKaydi> IfsaOlanlar = Array.Empty<IfsaKaydi>();
        /// <summary>Curcuna'da oyuncuların yazdığı sorular (öneri havuzundan tamamlananlar hariç).</summary>
        public IReadOnlyList<string> CurcunaSorulari = Array.Empty<string>();
    }

    // ------------------------------------------------------------ Grup kaydı (cihazda saklanır)
    // Unity JsonUtility ile yazılabilsin diye: [Serializable], public alanlar, List<T>, sözlük yok.

    [Serializable]
    public sealed class UnvanSerisi
    {
        public UnvanTuru Tur;
        public int Seri;
    }

    [Serializable]
    public sealed class GrupUyesi
    {
        public string Isim;
        public int Oyun;
        public int ToplamGosterilme;
        /// <summary>Tek bir oyunda en çok gösterilme.</summary>
        public int RekorGosterilme;
        public int TamamlananGorev;
        /// <summary>Son oyunda aldığı unvanlar ve kaç oyundur üst üste aldığı.</summary>
        public List<UnvanSerisi> Seriler = new List<UnvanSerisi>();
    }

    [Serializable]
    public sealed class GrupKaydi
    {
        public const int EfsaneSiniri = 200;
        public const int GrupPaketiSiniri = 500;

        public string Ad = "";
        public int OyunSayisi;
        public List<GrupUyesi> Uyeler = new List<GrupUyesi>();
        /// <summary>İfşa olan soruların arşivi, en yenisi sonda.</summary>
        public List<string> Efsaneler = new List<string>();
        /// <summary>"Bizim paket": Curcuna soruları ve ifşa olan sorular, tekrarsız.</summary>
        public List<string> GrupPaketi = new List<string>();

        /// <summary>
        /// Biten oyunu grubun kaydına işler, <b>yeni</b> bir kayıt döner (eskisi değişmez).
        /// Kişiler isimle eşleşir: aynı isim (büyük/küçük harf fark etmez) = aynı kişi.
        /// Güme giden soru <see cref="OyunOzeti"/>'nde olmadığı için buraya da hiç yazılmaz.
        /// </summary>
        public static GrupKaydi Isle(GrupKaydi eski, OyunOzeti ozet)
        {
            // ponytail: aynı isim = aynı kişi. "Ali" ile "Ali K." iki kişi sayılır; kurucu birleştirebilir (istemci).
            var yeni = new GrupKaydi
            {
                Ad = eski?.Ad ?? "",
                OyunSayisi = (eski?.OyunSayisi ?? 0) + 1,
            };
            if (eski != null)
            {
                foreach (var u in eski.Uyeler) yeni.Uyeler.Add(UyeKopyasi(u));
                yeni.Efsaneler.AddRange(eski.Efsaneler);
                yeni.GrupPaketi.AddRange(eski.GrupPaketi);
            }
            if (ozet == null) return yeni;

            foreach (var o in ozet.Oyuncular)
            {
                var uye = yeni.Uyeler.Find(x => string.Equals(x.Isim, o.Isim, StringComparison.OrdinalIgnoreCase));
                if (uye == null)
                {
                    uye = new GrupUyesi { Isim = o.Isim };
                    yeni.Uyeler.Add(uye);
                }
                uye.Oyun++;
                uye.ToplamGosterilme += o.Gosterilme;
                if (o.Gosterilme > uye.RekorGosterilme) uye.RekorGosterilme = o.Gosterilme;

                var seriler = new List<UnvanSerisi>();
                foreach (var unvan in ozet.Unvanlar)
                {
                    if (unvan.Kim != o.Id) continue;
                    var once = uye.Seriler.Find(s => s.Tur == unvan.Tur);
                    seriler.Add(new UnvanSerisi { Tur = unvan.Tur, Seri = (once?.Seri ?? 0) + 1 });
                }
                uye.Seriler = seriler;

                foreach (var g in ozet.Gorevler)
                    if (g.Kim == o.Id && g.Basarili) uye.TamamlananGorev++;
            }

            foreach (var ifsa in ozet.IfsaOlanlar)
            {
                yeni.Efsaneler.Add(ifsa.Soru);
                PaketeEkle(yeni.GrupPaketi, ifsa.Soru);
            }
            foreach (var soru in ozet.CurcunaSorulari) PaketeEkle(yeni.GrupPaketi, soru);

            if (yeni.Efsaneler.Count > EfsaneSiniri) yeni.Efsaneler.RemoveRange(0, yeni.Efsaneler.Count - EfsaneSiniri);
            if (yeni.GrupPaketi.Count > GrupPaketiSiniri) yeni.GrupPaketi.RemoveRange(0, yeni.GrupPaketi.Count - GrupPaketiSiniri);
            return yeni;
        }

        static void PaketeEkle(List<string> paket, string soru)
        {
            if (string.IsNullOrWhiteSpace(soru)) return;
            if (paket.Exists(x => string.Equals(x, soru, StringComparison.OrdinalIgnoreCase))) return;
            paket.Add(soru);
        }

        static GrupUyesi UyeKopyasi(GrupUyesi u)
        {
            var k = new GrupUyesi
            {
                Isim = u.Isim,
                Oyun = u.Oyun,
                ToplamGosterilme = u.ToplamGosterilme,
                RekorGosterilme = u.RekorGosterilme,
                TamamlananGorev = u.TamamlananGorev,
            };
            foreach (var s in u.Seriler) k.Seriler.Add(new UnvanSerisi { Tur = s.Tur, Seri = s.Seri });
            return k;
        }
    }
}
