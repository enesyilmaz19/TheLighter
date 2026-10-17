# G2 (Arda): Test işi artık sende

Bu dosya Arda'nın yapay zekâsı için bir görev tanımı. Kullanım: "`tasks/g2-test.md`'yi oku, sıradaki işten devam et."
Yazan: G1 (Enes), 2026-10-15.

**Karar (Enes):** oyunun kuralları ve sunucusu bitti. Bundan sonra test işi G2'de: Unity testleri, botlar, elden ele ve online oyun denemeleri, PC test planı. G1 kendi otomatik testlerini (`dotnet test`) koşmaya devam eder ama oyunu oynayarak denemek ve bulgu toplamak sende. Kural ya da sunucu hatası bulursan G1'e yazarsın, ekran hatasını kendin düzeltirsin.

---

## 0. Sıra (hepsi bu dosyadan)

Her görev dosyasının sonundaki "Testler" ve "Bitti ölçütü" bölümleri o işin test kısmı: ekranı yaparken testini de yaz, bitirince oyna.

1. `tasks/g2-pc.md`, 1. ve 2. maddeler (S5'i kapat, PC düzeltmeleri)
2. `tasks/g2-kostebek.md`
3. `tasks/g2-rus-ruleti.md`
4. `tasks/g2-takim.md`
5. `tasks/g2-casus.md`
6. `tasks/g2-cakmak-gecesi.md` (eski Vampir Köylü; en büyüğü, 41 rol)
7. Bu dosyanın 2. ve 3. bölümleri: bütün oyunların son test turu, sonra `tasks/g2-pc.md` 3.3 (çok bilgisayarlı test)

`tasks/g2-pc.md` 3.4 (VPS) şimdilik yok: VPS'i Enes oyun bitince kuracak.

## 1. Her PR'dan önce (otomatik)

- **Unity EditMode:** hepsi yeşil. Bugün main'de 489 geçen, 2 atlanan (atlananlar senin botların, yerel sunucusuz koşmaz).
- **Botlar:** yerel sunucuyla `SunucuIleOyunTesti`:

  ```bash
  dotnet run --project Sunucu/Cakmak.Sunucu
  ```

  Sonra Unity'de `Sunucu` kategorisi (Test Runner'da ya da `-testCategory "Sunucu"`).
  - Bilinen titreklik: `DortBot_CezaGorevKaosAcik_OyunBaştanSonaBiter`, 71. satır (`tasks/g2-takim.md` 0. bölüm). Düzeltmesi orada yazılı.
- **Kural ve sunucu testleri** main'i bozmadığını görmek için, G1'in koşturduğuyla aynı:

  ```bash
  dotnet test Sunucu/KuralTestleri
  ```

  ```bash
  dotnet test Sunucu/SunucuTestleri
  ```

  Bugün 365 ve 183, hepsi yeşil. Sen kurallara dokunmuyorsun; kırmızı görürsen G1'e yaz.

## 2. Oyun denemeleri (elle)

Her oyun **elden ele ve online** en az bir kez baştan sona. Online'da en az 2 pencere (aynı bilgisayarda iki pencere sorunu `tasks/g2-pc.md` 2.1'de; önce o). Oyun başına ölçüt o oyunun görev dosyasının "Bitti ölçütü"nde.

| Oyun | Neyi mutlaka dene |
|---|---|
| Çakmak | Normal, Temiz, Curcuna; ceza/görev/kaos açık bir oyun; kopma ve geri dönme |
| Köstebek | Köstebek yakalanır; köstebek görevini bitirir; son oylama |
| Rus Ruleti | Can 1 ve 3; tetik devret; 3 kişide final |
| Takım Düellosu | Kurucu dağıtır ve rastgele; 7 puan; takım sohbeti |
| Casus | Mekân ve kelime; "Mekânı biliyorum"; son şans |
| Çakmak Gecesi | Aşağıda |

**Çakmak Gecesi'nde roller:** 41 rolün hepsi en az bir oyunda görülsün. Rolleri tek tek denemek için kurucu ayarlarından ötekileri kapat: kapatılmayan özel roller önce dağıtılır, istediğin rol kesin çıkar.

- 6 kişiyle rol açık, 12+ kişiyle rol kapalı ve gecelik 2 kurban.
- Gündüz yetenekleri: Sorgucu, Sınayıcı ve Kuklacı A iken; Kışkırtıcı B iken. Hakem ve Söndürücü gece hazırlar, ertesi gün hedefin ilk düellosunda işler.
- En az bir oyun katiller, bir oyun masumlar, bir oyun bir tarafsız (Soytarı, Cellat, Seri Katil, Bombacı ya da Kibritçi) kazanarak bitsin. Ek kazananlar (Hayatta Kalan, Gözde, Düellocu, Sır Küpü) özette görünsün.
- 18 kişiyle bir online oyun (pencere ya da bot): liste, lobi ve gece dolaşması sığıyor mu, yavaşlıyor mu.

## 3. Bulgular

Her bulgu tek satır, bir listede (PR açıklamasında ya da Enes'e mesajla):

- **Ne yaptın → ne bekledin → ne oldu.** Oyun, mod, kişi sayısı, online'sa oda kodu ve saat (sunucu günlüğünde bulmak için).
- **Kimin işi:**
  - Ekran, metin, akış: senin.
  - Kural sana yanlış geliyorsa, `docs/Oyunlar.md`'de yazılanla oyunun yaptığı farklıysa ya da sunucu hata kodu beklenmedikse: G1.
- **Kural önerisi** ("bu rol çok güçlü", "Falcı'nın grubu büyük"): ayrı bir başlık altında. Karar Enes'in.

## 4. Bitti ölçütü

- 1. bölüm her PR'da yeşil.
- 2. bölümdeki tablo ve Çakmak Gecesi maddeleri tamam; bulgu listesi Enes'te.
- `tasks/g2-pc.md` 3.1, 3.2, 3.3 geçti.
