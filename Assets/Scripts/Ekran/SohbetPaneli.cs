using System;
using System.Collections.Generic;
using Cakmak.Kurallar;
using TheLighter.Ag;
using UnityEngine;
using UnityEngine.UIElements;
using static TheLighter.Istemci.Arayuz;

namespace TheLighter.Istemci
{
    /// <summary>
    /// Sohbet (Ana Plan 4, "Sohbet arayüzü"; protokol: docs/Protokol.md "Sohbet"): Genel ve DM sekmeleri,
    /// mesaj listesi, yazı kutusu. Telefonda "Sohbet" düğmesiyle açılan katman; PC'de geniş pencerede sağ sütunda
    /// hep açık (<see cref="YanaAl"/>). Enter gönderir, odak kutuda kalır.
    /// Mobilde klavye açılınca alt boşluk klavye kadar büyür, yazı kutusu klavyenin altında kalmaz.
    /// </summary>
    public sealed class SohbetPaneli
    {
        readonly SohbetDefteri defter;
        readonly Func<OyuncuId?, string, bool> gonder;
        readonly Func<IReadOnlyList<OdaOyuncusu>> oyuncular;
        readonly Func<OyuncuId?> ben;
        readonly VisualElement sekmeler;
        readonly ScrollView liste;
        readonly VisualElement dmSecici;
        readonly TextField kutu;
        readonly Button yolla;
        readonly Label sayac;
        readonly Label hata;
        readonly Button kapat;
        bool yan;
        OyuncuId? kanal;
        int cizilenMesaj = -1;
        string cizilenKanal;

        public VisualElement Kok { get; }

        public bool Acik => !Kok.ClassListContains("gizli");

        public SohbetPaneli(SohbetDefteri defter, Func<OyuncuId?, string, bool> gonder,
            Func<IReadOnlyList<OdaOyuncusu>> oyuncular, Func<OyuncuId?> ben)
        {
            this.defter = defter;
            this.gonder = gonder;
            this.oyuncular = oyuncular;
            this.ben = ben;

            Kok = Kutu("menu-perde", "sohbet", "gizli");
            var kutuKap = Kutu("menu-kutu", "sohbet-kutu");

            var ust = Kutu("satir");
            ust.Add(Yazi(Metin.Al("sohbet.baslik"), "baslik"));
            ust.Add(Esnek());
            kapat = Buton(Metin.Al("genel.kapat"), Kapat, "buton-kucuk");
            ust.Add(kapat);
            kutuKap.Add(ust);

            sekmeler = Kutu("secenekler", "sohbet-sekmeler");
            kutuKap.Add(sekmeler);

            dmSecici = Kutu("cipler", "gizli");
            kutuKap.Add(dmSecici);

            liste = new ScrollView(ScrollViewMode.Vertical);
            liste.AddToClassList("kaydirma");
            liste.AddToClassList("sohbet-liste");
            kutuKap.Add(liste);

            hata = Yazi("", "hata");
            kutuKap.Add(hata);

            var alt = Kutu("satir", "sohbet-alt");
            kutu = new TextField { maxLength = SohbetDefteri.EnFazlaKarakter };
            kutu.AddToClassList("isim-kutusu");
            kutu.RegisterValueChangedCallback(_ => Durum());
            // Enter gönderir; odak kutuda kalır, arka arkaya yazılabilir.
            kutu.RegisterCallback<KeyDownEvent>(e =>
            {
                bool enter = e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter;
                if (!enter && e.character != '\n' && e.character != '\r') return;
                e.StopImmediatePropagation();
                if (enter) Yolla();
                kutu.schedule.Execute(() => kutu.Focus());
            }, TrickleDown.TrickleDown);
            alt.Add(kutu);
            yolla = Buton(Metin.Al("sohbet.gonder"), Yolla, "buton-birincil");
            alt.Add(yolla);
            kutuKap.Add(alt);
            sayac = Yazi("", "sayac");
            kutuKap.Add(sayac);

            Kok.Add(kutuKap);
            Durum();
        }

        public void Ac()
        {
            Kok.RemoveFromClassList("gizli");
            cizilenMesaj = -1;
            Ciz();
        }

        public void Kapat()
        {
            if (!yan) Kok.AddToClassList("gizli");
        }

        /// <summary>
        /// true: PC'de sağ sütunda hep açık (kapat düğmesi yok). false: telefondaki gibi açılıp kapanan katman (kapalı başlar).
        /// </summary>
        public void YanaAl(bool yanda)
        {
            yan = yanda;
            Kok.EnableInClassList("menu-perde", !yanda);
            Kok.EnableInClassList("sohbet-yan", yanda);
            Kok.EnableInClassList("gizli", !yanda);
            kapat.EnableInClassList("gizli", yanda);
            cizilenMesaj = -1;
        }

        /// <summary>Sohbete ait hata (UYGUNSUZ, COK_HIZLI...): panelin altında. Katmansa açılır ki görülsün.</summary>
        public void HataGoster(string metin)
        {
            if (!Acik) Ac();
            hata.text = metin ?? "";
        }

        /// <summary>Her karede: yeni mesaj geldiyse listeyi çizer, klavye açıksa alt boşluğu ayarlar.</summary>
        public void Guncelle(float gecenSn)
        {
            defter.Ilerle(TimeSpan.FromSeconds(gecenSn));
            if (!Acik) return;
            if (defter.Mesajlar(kanal).Count != cizilenMesaj || Anahtar(kanal) != cizilenKanal) Ciz();
            KlavyeBoslugu();
            yolla.SetEnabled(SohbetDefteri.Hazirla(kutu.value) != null && defter.GonderebilirMi);
        }

        static string Anahtar(OyuncuId? k) => k.HasValue ? k.Value.ToString() : "genel";

        /// <summary>
        /// Mobilde klavye ekranın altını kaplar: panelin altına klavye yüksekliği kadar boşluk (panel biriminde).
        /// </summary>
        void KlavyeBoslugu()
        {
            float bosluk = 0;
            if (TouchScreenKeyboard.isSupported && TouchScreenKeyboard.visible && Screen.height > 0)
            {
                float panelYuksekligi = Kok.resolvedStyle.height;
                if (!float.IsNaN(panelYuksekligi) && panelYuksekligi > 0)
                    bosluk = TouchScreenKeyboard.area.height / Screen.height * panelYuksekligi;
            }
            Kok.style.paddingBottom = bosluk;
        }

        void Ciz()
        {
            cizilenKanal = Anahtar(kanal);
            defter.Okundu(kanal);

            sekmeler.Clear();
            sekmeler.Add(Sekme(null, Metin.Al("sohbet.genel")));
            foreach (var dm in defter.DmKanallari) sekmeler.Add(Sekme(dm, Isim(dm)));
            sekmeler.Add(Buton(Metin.Al("sohbet.dm_ac"), DmSeciciAcKapa, "secenek"));

            liste.Clear();
            var mesajlar = defter.Mesajlar(kanal);
            cizilenMesaj = mesajlar.Count;
            foreach (var m in mesajlar)
            {
                var satir = Kutu("satir", "sohbet-satir");
                satir.Add(RenkNoktasi(Renk(m.Kim)));
                var yazi = Yazi(Isim(m.Kim) + ": " + m.Metin, "sohbet-yazi");
                satir.Add(yazi);
                liste.Add(satir);
            }
            liste.schedule.Execute(() => liste.scrollOffset = new Vector2(0, float.MaxValue));
        }

        Button Sekme(OyuncuId? k, string ad)
        {
            int n = defter.Okunmamis(k);
            var b = Buton(n > 0 ? Metin.Al("sohbet.sekme_okunmamis", ad, n) : ad, () => { kanal = k; dmSecici.AddToClassList("gizli"); Ciz(); }, "secenek");
            b.EnableInClassList("secili", Anahtar(k) == Anahtar(kanal));
            return b;
        }

        void DmSeciciAcKapa()
        {
            dmSecici.Clear();
            foreach (var o in oyuncular())
            {
                if (o.Id == ben()) continue;
                var kim = o.Id;
                dmSecici.Add(OyuncuCipi(o.Isim, o.Renk, () =>
                {
                    defter.DmAc(kim);
                    kanal = kim;
                    dmSecici.AddToClassList("gizli");
                    Ciz();
                }));
            }
            dmSecici.ToggleInClassList("gizli");
        }

        void Durum()
        {
            int n = (kutu.value ?? "").Length;
            sayac.text = n + "/" + SohbetDefteri.EnFazlaKarakter;
            hata.text = "";
        }

        void Yolla()
        {
            string metin = SohbetDefteri.Hazirla(kutu.value);
            if (metin == null) return;
            if (!defter.GonderebilirMi)
            {
                hata.text = Metin.Al("sohbet.cok_hizli");
                return;
            }
            if (!gonder(kanal, metin))
            {
                hata.text = Metin.Al("sohbet.gonderilemedi");
                return;
            }
            defter.Gonderildi();
            kutu.value = "";
            kutu.Focus();
        }

        string Isim(OyuncuId id)
        {
            foreach (var o in oyuncular()) if (o.Id == id) return o.Isim;
            return id.ToString();
        }

        int Renk(OyuncuId id)
        {
            foreach (var o in oyuncular()) if (o.Id == id) return o.Renk;
            return id.Deger - 1;
        }
    }
}
