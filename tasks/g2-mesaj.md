# Arda'ya mesaj (Enes, 2026-10-15)

Selam Arda,

G1 tarafı bitti: altı oyunun hepsinin kuralları ve sunucusu main'de, testleri yeşil. Bundan sonra **ekranlar ve test işi sende**. Giriş noktan `tasks/g2-test.md`: sıra, her PR'dan önce koşacakların, oyun başına neyi deneyeceğin ve bulguları nasıl yazacağın orada.

## Son PR'ından beri neler değişti

**Yeni oyunlar** (her birinin görev dosyası ayrı, sırası `g2-test.md`'de):

| Oyun | Görev | Kısaca |
|---|---|---|
| Köstebek | `g2-kostebek.md` | Gizli görevli hain, "Köstebek kim?" oylaması |
| Rus Ruleti | `g2-rus-ruleti.md` | Düelloyu kaybeden tetiği çeker, can 1–3, 3 kişide final |
| Takım Düellosu | `g2-takim.md` | İki takım, düello puanı + ifşa bonusu, takım sohbeti |
| Casus | `g2-casus.md` | Mekân ya da kelime (Imposter), son şans |
| Çakmak Gecesi | `g2-cakmak-gecesi.md` | Eski Vampir Köylü: 6–18 kişi, 41 rol, gündüz çakmak + linç, gece seçimleri |

**Dikkat edilecekler:**
- **Vampir Köylü yok, yerine Çakmak Gecesi var** (protokolde `oyun: "cakmakGecesi"`). Sana `g2-vampir.md` diye bir dosya gittiyse onu boş ver, yerine `g2-cakmak-gecesi.md`.
- **Oda 18 kişilik.** Çakmak Gecesi 6–18 kişi, diğer oyunlar 10'da kaldı: 10'dan fazlayla başlatırsan `OYUNCU_SAYISI_GECERSIZ`. Elden ele kurulum ekranı Çakmak Gecesi'nde 18'e izin vermeli (`Oyun.GeceEnFazlaOyuncu`).
- **Sohbet kuralları kural motorunda** (`Oyun.SohbeteYazabilir`, `Oyun.KanalAlicilari`). Takım sohbeti dahil. Protokol aynı (`kime: "takim"`), yeni kanallar `"katil"` ve `"oluler"`.
- **Yeni komutlar:** `sucla`, `suclamaOyu`, `tetikCek`, `tetikDevret`, `casusTahmini`, `lincOyu`, `geceSecimi`, `suikast`, `yetenek`. Hepsi `docs/Protokol.md`'de.
- **Tasarım ve kesin kurallar** `docs/Oyunlar.md`'de. Ekran metinlerini rol tablolarından yaz.

## Main'in şu anki durumu

| Test | Sonuç |
|---|---|
| `dotnet test Sunucu/KuralTestleri` | 342/342 |
| `dotnet test Sunucu/SunucuTestleri` | 183/183 |
| Unity EditMode | 466 geçen, 0 kalan, 2 atlanan (atlananlar senin botların; yerel sunucuyla 2/2 geçiyor) |

Senin `SunucuIleOyunTesti`'nde ara sıra kırmızı veren bir satır var (71. satır, yarış). Düzeltmesi `g2-takim.md` 0. bölümde.

## İş bölümü

- **Ekran, metin, akış hatası:** senin.
- **Kural ya da sunucu hatası,** ya da oyun `docs/Oyunlar.md`'de yazandan farklı davranıyorsa: bana yaz, ben düzeltirim.
- **Kural önerilerin** ("bu rol çok güçlü" gibi): ayrı bir başlıkta yaz, kararı ben veririm.
- **Sözleşme oturumu** için konuşacaklarımız `g2-pc.md` 5. bölümde duruyor. Uygun olunca bir saat ayarlayalım.

Kolay gelsin,
Enes
