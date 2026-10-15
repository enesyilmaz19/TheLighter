using System;
using Cakmak.Kurallar;
using TheLighter.Ag;

namespace TheLighter.Istemci
{
    /// <summary>
    /// Oyun seçimi (elden ele kurulum ve online lobi ortak): hangi oyunlar seçilebilir, ayarlar oyuna uymuyorsa ne kapanır.
    /// Uyum kuralı kural motorunda (<see cref="Oyun.AyarlarUyumlu"/>): burada sadece sorulur, kural yazılmaz.
    /// </summary>
    public static class OyunSecimi
    {
        /// <summary>Ekranı hazır olan oyunlar (tasks/g2-test.md sırasıyla eklenir).</summary>
        public static readonly OyunTuru[] Secenekler = { OyunTuru.Cakmak, OyunTuru.Kostebek, OyunTuru.RusRuleti, OyunTuru.Takim };

        /// <summary>Rus Ruleti can seçenekleri (1 = Rus Ruleti, 3 = Son Çakmak).</summary>
        public static readonly int[] CanSecenekleri = { 1, 2, 3 };

        /// <summary>Bu oyunda tur sayısı yok sayılır (son kalana / bir taraf kazanana kadar sürer).</summary>
        public static bool TurSayisiYok(OyunTuru oyun) => oyun == OyunTuru.RusRuleti || oyun == OyunTuru.CakmakGecesi;

        /// <summary>
        /// Bu oyunda gizli görev ayardan bağımsız herkeste (Köstebek: köstebeğin hamleleri göze batmasın; docs/Oyunlar.md 1).
        /// Ekranda "görevler" düğmesi açık ve kilitli görünür.
        /// </summary>
        public static bool GorevlerHerkeste(OyunTuru oyun) => oyun == OyunTuru.Kostebek;

        /// <summary><paramref name="degistir"/> uygulanınca ayarlar hâlâ uyumlu mu (kural motoruna sorulur).</summary>
        public static bool Uyumlu(Ayarlar a, Action<Ayarlar> degistir)
        {
            var k = Kopya(a);
            degistir?.Invoke(k);
            return Oyun.AyarlarUyumlu(k);
        }

        /// <summary>
        /// Ayarlar oyuna uymuyorsa sırayla kapatır: kaos, görevler, ceza, mod (Curcuna → Normal). Ne kadar az şey değişirse o kadar iyi:
        /// her adımdan sonra kural motoruna yeniden sorulur.
        /// </summary>
        public static void Uyumla(Ayarlar a)
        {
            if (Oyun.AyarlarUyumlu(a)) return;
            a.KaosTurlari = false;
            if (Oyun.AyarlarUyumlu(a)) return;
            a.GizliGorevler = false;
            if (Oyun.AyarlarUyumlu(a)) return;
            if (a.Mod == OyunModu.Temiz && a.CezaSeviyesi == CezaSeviyesi.Cesur) a.CezaSeviyesi = CezaSeviyesi.Hafif;
            if (Oyun.AyarlarUyumlu(a)) return;
            a.CezaSeviyesi = CezaSeviyesi.Kapali;
            if (Oyun.AyarlarUyumlu(a)) return;
            if (a.Mod == OyunModu.Curcuna) a.Mod = OyunModu.Normal;
        }

        /// <summary>Lobideki ayarların kural motoru karşılığı (uyum sorusu için; sayılar önemli değil).</summary>
        public static Ayarlar OdadanAyarlar(OdaAyarlari o) => new Ayarlar
        {
            Oyun = o.Oyun,
            Can = o.Can,
            Mod = o.Mod,
            MiniOyun = o.MiniOyun,
            TurSayisi = o.TurSayisi,
            CezaSeviyesi = o.Ceza,
            GizliGorevler = o.Gorevler,
            KaosTurlari = o.Kaos,
        };

        static Ayarlar Kopya(Ayarlar a) => new Ayarlar
        {
            Oyun = a.Oyun,
            Mod = a.Mod,
            MiniOyun = a.MiniOyun,
            TurSayisi = a.TurSayisi,
            CezaSeviyesi = a.CezaSeviyesi,
            GizliGorevler = a.GizliGorevler,
            KaosTurlari = a.KaosTurlari,
            Can = a.Can,
            Casus = a.Casus,
            GecelikKurban = a.GecelikKurban,
            KapaliRoller = a.KapaliRoller,
            TakimDagilimi = a.TakimDagilimi,
            RolAcilir = a.RolAcilir,
        };
    }
}
