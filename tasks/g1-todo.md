# G1 (Enes) — İş listesi

Kaynak: `docs/Ana Plan.md` · Branch: `g1/kural-motoru`

## 📥 ENES'İN MASASI

- [ ] **İmza temizliği (sonra):** `main`'deki ilk 2 commit'te hâlâ imza var. Arda'nın ve bu PR'ın merge'ünden sonra, açık branch yokken bir kez yeniden yazılıp force push edilecek. O an ikiniz de `git fetch && git reset --hard origin/main` yaparsınız.
- [ ] **Sözleşme oturumu (Arda ile):** aşağıdaki "Sözleşmeye önerilen değişiklikler" listesini birlikte onaylayın. Onaylananlar `docs/Ana Plan.md` 5.1'e işlenir.
- [ ] **Kural motorunu gözden geçir, sonra commit + push + PR** (`g1/kural-motoru` → `main`).

## S1 — Kural motoru (çekirdek döngü)

Kapsam: tur akışı, seçim kuralları, mini oyunlar, süreler, Curcuna, görünüm filtresi. **Cezalar, görevler, kaos, grup hafızası S3'te. Kopma/dönme S5'te.**

- [x] Adım 1 — İskelet: `Assets/Kurallar/` tipleri + `.asmdef`
- [x] Adım 2 — Test altyapısı: `Assets/Kurallar.Tests/` (Unity) + `Sunucu/KuralTestleri/` (`dotnet test`, Unity açmadan)
- [x] Adım 3 — Tur akışı, seçim kuralları, gizlilik
- [x] Adım 4 — Mini oyunlar: TKM, Tek-Çift, Zar, Reaksiyon, beraberlik, Karışık
- [x] Adım 5 — Süreler ve AFK
- [x] Adım 6 — Curcuna, öneri çek, paket ayrıştırma
- [x] Adım 7 — 1.000 rastgele oyun, aynı tohum = aynı oyun, 7 bilerek bozma (7'si de yakalandı)
- [x] Adım 8 — Unity'de doğrulama (geçici 6000.6.4f1 projesinde EditMode testleri) + `.meta` dosyaları

## S2 — G1 tarafı

- [x] Genel paket taslağı: `Assets/Icerik/Genel.txt`, 100 soru, hepsi "kim" sorusu (branch `g1/genel-paket`)
- [x] İçerik testi: `Icerik/` altındaki **her** paket otomatik kontrol ediliyor (≤140 karakter, `?` ile bitiyor, "kim" geçiyor, tekrar yok). 3 şekilde bozuldu, 3'ü de yakalandı
- [ ] **Enes:** paketi düzenle (sil, değiştir, arkadaşların sorularını ekle). Sonra test + commit
- [ ] Birleştirme: Arda'nın ekranları motora bağlanırken çıkan kural hataları
- [ ] Playtest 1'i ayarla: ≥4 kişi, elden ele, 30 dk

## S4'ün ilk yarısı — sunucu iskeleti (öne çekildi)

Neden şimdi: Arda'yı beklerken yapılabilecek, Playtest 1'in sonucundan etkilenmeyen tek büyük iş. Sunucu kural motorunu ağa taşıyor, kurallar değişse de sunucu değişmiyor.

- [x] Protokol yazıldı: `docs/Protokol.md` (taslak, oturumda onaylanacak)
- [x] Sunucu: `Sunucu/Cakmak.Sunucu` (.NET 10, ASP.NET Core, WebSocket). Oda kodu, katılma, anahtarla geri dönüş, ayarlar, başlat, komutlar, 100 ms saat, herkese kendi görünümü
- [x] Kara kutu testleri: `Sunucu/SunucuTestleri` (alt ajan, sadece protokolden yazdı): 49 test, 5'i bilerek bozulup doğrulandı
- [x] Bağımsız kod incelemesi (alt ajan): gizlilik sızıntısı ve kilit hatası yok. 1 hata (aynı alan iki kez → bağlantı düşüyordu), 3 risk (saat hatası sunucuyu durdurur, sessiz kopan fark edilmez, sınırsız kuyruk), 1 not (eski cihaz açık kalıyordu) düzeltildi. Her biri önce kırmızı testle gösterildi
- [x] Kendi bulduğum: online'da Reaksiyon seçilebiliyordu (planda sadece elden ele). Kapatıldı
- [x] Öz inceleme: online "öneri çek" testi yoktu, eklendi (bozup doğrulandı). Sunucu gerçek süreç olarak da çalıştırıldı: Node WebSocket istemcisiyle 4 kişi, oda, öneri, soru, gizlilik → geçti. **Toplam: 82 kural + 57 sunucu testi**
- [x] Oyunda kurucu koparsa kuruculuk ilk bağlı oyuncuya geçiyor (Ana Plan 6.1). Yoksa sınırsız oyunu kimse bitiremiyordu
- [ ] S5'e kalanlar: sohbet, atma, kopanı 60 sn bekleme, AFK etiketi, geç gelen, Temiz filtresi, boş odayı bir süre tutma, isimlerde görünmez karakter
- [ ] Kurulum (VPS, alan adı, Caddy): Enes hazır olunca

## Sözleşmeye önerilen değişiklikler (oturumda onaylanacak)

| # | Ana Plan 5.1'de | Kodda | Neden |
|---|---|---|---|
| 1 | `Oyun Kur(ayarlar, oyuncular, tohum)` | `static Sonuc Kur(ayarlar, oyuncular, tohum, out Oyun oyun)` | "Hata fırlatma" kuralı. 3 kişi ya da aynı kimlik gelirse `OyuncuSayisiGecersiz` / `GecersizGirdi` döner |
| 2 | — | `Sonuc OneriCek(OyuncuId a)` → `Gorunum.Oneri` | "Öneri çek" butonu planda var, metotu yoktu. Rastgelelik tohumlu kalsın diye kural kodunda |
| 3 | — | `Sonuc OyunuBitir()` | "Sınırsız: kurucu bitirir" planda var, metotu yoktu. Kimin çağırabileceğine sunucu karar verir |
| 4 | — | `SoruSor(a, b, null)` = **fısıltı** | Elden ele "Fısıldadım" butonu. B'nin görünümünde `Fisilti = true`, metin yok. İfşada A sesli söyler |
| 5 | Hata kodları `SIRA_SENDE_DEGIL`… | `Sonuc` enum: `SiraSendeDegil, Secilemez, YanlisAsama, GecersizGirdi, HakKalmadi, HavuzBos, OyuncuSayisiGecersiz` | JSON'a çevirisi sunucuda |
| 6 | — | `Kur` olay yaymaz | Abone olmadan önce oluyor. İlk durum `GorunumAl` ile okunur |
| 7 | — | `Karisik` = TKM / Tek-Çift / Zar | Reaksiyon online'da adil değil, sadece elle seçilir (elden ele) |
| 8 | Süre dolunca "rastgele birine" | A'nın süresi dolarsa A hariç · B'nin süresi dolarsa A ve B hariç | Süresi dolan hemen tekrar seçilmesin |

**Henüz yok (plana göre sonra):** `BedelOde`, `Oy`, `Ozet`, `GrupKaydi.Isle` (S3) · `OyuncuKoptu/Dondu/Cikti` (S5).

## Arda'nın bilmesi gerekenler

- Kural motoru hazır olduğu için **sahte veriye gerek yok.** Elden ele ekranları doğrudan gerçek `Oyun`'a bağlanabilir.
- **Perde sırasında `Ilerle` çağırma.** Saat dışarıda olduğu için telefon el değiştirirken süre akmaz.
- Projede `com.unity.test-framework` (1.8.0) yoksa testler Test Runner'da görünmez.

## İnceleme (2026-10-08)

- **79 test**, iki yerde yeşil: `dotnet test` (net8.0, C# 9, uyarılar hata sayılıyor) ve **Unity 6000.6.4f1 EditMode** (NUnit 3.5).
- **1.000 rastgele oyun:** takılma yok, seçim listesi hiç boş değil, soru B dışına hiç sızmıyor.
- **Bilerek bozma:** 7 kural bozuldu (gizlilik, B'nin A'yı seçmesi, süre, TKM, sıra...). 7'si de en az bir testi kırmızıya çevirdi. Sonradan eklenen "görünüm kopyası" testi de bir kez bozulup doğrulandı.
- `.meta` dosyaları Unity'nin kendisi tarafından üretildi, elle yazılmadı.
- Bilinen sınırlar:
  - `Ilerle` tek çağrıda en fazla bir aşama geçer, artan süre taşınmaz (`ponytail:` notu kodda). Unity karede ve sunucu 100 ms'de çağırdığı için fark etmez.
  - `Oyun` thread-safe değil. Sunucu her odayı tek kilit altında çağırmalı (S4).
  - "Güme giden soru bellekten silinir" kodda var, ama dışarıdan gözlenemediği için testle kanıtlanamıyor. Dışarıya sızmadığı ise test ediliyor.
