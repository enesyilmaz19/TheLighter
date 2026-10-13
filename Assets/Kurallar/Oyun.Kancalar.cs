namespace Cakmak.Kurallar
{
    // Oyunlara özel kancalar (docs/Oyunlar.md): çekirdek döngü sadece bunları çağırır, bunlar da her oyunun kendi
    // dosyasına dağıtır (Oyun.Kostebek.cs, Oyun.RusRuleti.cs, Oyun.Takim.cs, Oyun.Casus.cs). Her oyunun metodu "bu oyun
    // değilse hiçbir şey yapma" diye başlar. Yeni oyun: kancalarını buraya ekle. Oyuna özgü aşamaların akışı (Rus
    // Ruleti'nin tetiği, Casus'un son şansı) kendi dosyasında, Ilerle ve EldekiIsiBitir'de aşama olarak; gizli kişili
    // oyunların ortak oylaması Oyun.Suclama.cs'te.
    public sealed partial class Oyun
    {
        /// <summary>Kurulumda, görevler dağıtıldıktan sonra.</summary>
        void OyunlariKur()
        {
            KostebekKur();
            RusRuletiKur();
            TakimKur();
            CasusKur();
        }

        /// <summary>Yeni tur başlamadan: oyun kendi kuralıyla bitti ya da başka bir akışa geçti (Rus Ruleti finali). true: tur başlamaz.</summary>
        bool TurBasindaOyunKarari() => KostebekGoreviBitti() || RusRuletiAraKontrol() || TakimKazandi();

        /// <summary>Turun sonucu belli (TuruBitir): soru ifşa mı, düelloyu (ya da oylamayı) kim kaybetti.</summary>
        void OyunTurSonucu(bool ifsa, OyuncuId? kaybeden)
        {
            RusRuletiTurSonu(kaybeden);
            TakimTurSonu(ifsa, kaybeden);
        }

        /// <summary>Oyun bitiyor (Bitir): sonuç kendi kuralıyla yazılmadıysa (süre, kurucu, zorla) şimdi yazılır.</summary>
        void OyunSonuclariniYaz()
        {
            KostebekSonucunuYaz();
            RusRuletiSonucunuYaz();
            TakimSonucunuYaz();
            CasusSonucunuYaz();
        }

        /// <summary>Oyun bitti: özete oyunların sonuçları. Oyun bitmeden çağrılmaz (köstebeğin kim olduğu sır).</summary>
        void OyunOzetine(OyunOzeti o)
        {
            o.Kostebek = kostebekSonucu?.Kopya();
            o.RusRuleti = rusRuletiSonucu?.Kopya();
            o.Takim = takimSonucu?.Kopya();
            o.Casus = casusSonucu?.Kopya();
        }

        void OyunGorunumu(Gorunum g, OyuncuId kim)
        {
            KostebekGorunumu(g, kim);
            RusRuletiGorunumu(g);
            TakimGorunumu(g);
            CasusGorunumu(g, kim);
            SuclamaGorunumu(g, kim);
        }

        // Gizli kişili oyunlar (Köstebek, Casus): oylamanın sonucu oyunun kuralıyla.

        /// <summary>Oylamada en çok oyu gizli kişi aldı.</summary>
        void HainYakalandi()
        {
            KostebekYakalandi();
            CasusYakalandi();
        }

        /// <summary>Son oylamada da yakalanmadı.</summary>
        void HainYakalanmadi()
        {
            KostebekYakalanmadi();
            CasusYakalanmadi();
        }

        /// <summary>Gizli kişi oyundan çıktı.</summary>
        void HainKacti()
        {
            KostebekKacti();
            CasusKacti();
        }
    }
}
