# TheLighter (Çakmak) — Yapay zekâ talimatları

Bu dosyayı **her oturumun başında oku.** Bu repoda iki kişi, iki ayrı yapay zekâyla çalışıyor. Buradaki kurallar ikisini de bağlar.

## Proje

Gizli sorulu sosyal parti oyunu. A, B'ye gizlice soru sorar. B çakmağı cevabı olan C'ye verir. B ile C mini oyun oynar. C kazanırsa soru herkese ifşa olur, B kazanırsa güme gider. Sıra C'ye geçer.
İki mod: **elden ele** (tek cihaz, ağ yok) ve **online** (oda kodu, kendi C# sunucumuz).

- Ne yapılacağı: `docs/Brief.md`
- Nasıl yapılacağı, kararlar, iş bölümü: `docs/Ana Plan.md`
- **Ekran ↔ kural sözleşmesi:** `docs/Ana Plan.md` → 5. bölüm. Kod yazmadan önce oku.

## Sürüm

- **Unity 6000.6.4f1**, URP. Başka sürümde açma, kaydetme, yükseltme önerme.
- Platformlar: Windows + Android.

## Klasör sahipliği

| Klasör | Sahibi |
|---|---|
| `Assets/Kurallar/` · `Assets/Kurallar.Tests/` · `Assets/Icerik/` · `Sunucu/` | **G1 — Enes** (kurallar, sunucu, içerik) |
| `Assets/` altındaki geri kalan her şey · `Packages/` · `ProjectSettings/` | **G2 — arkadaş** (Unity istemcisi) |
| `docs/` · `AGENTS.md` · `.gitignore` | Ortak. Değiştirmeden önce iki kişi de onaylar |

**Kendi sahibinin klasörü dışında dosya değiştirme.** Başka klasörde bir hata ya da eksik görürsen düzeltme. Dur ve kullanıcına, diğer kişiye iletebileceği kısa bir mesaj yaz: hangi dosya, ne oluyor, nasıl tekrarlanır, ne öneriyorsun.

## Sözleşme

- Kural kodunun dışa açık tipleri ve metotları (`Kur`, `SoruSor`, `CevapSec`, `GorunumAl`, `Sonuc`, `Gorunum`...) ve ağ mesajları sözleşmedir.
- **Sözleşme tek taraflı değişmez.** Değişmesi gerekiyorsa kodu değiştirme. Kullanıcına neyin neden değişmesi gerektiğini yaz. İki kişi anlaşınca önce `docs/Ana Plan.md` 5. bölüm, sonra kod değişir.

## Kural kodu (`Assets/Kurallar/`)

- **UnityEngine yok.** `.asmdef`'te "No Engine References" işaretli. `Debug.Log`, `UnityEngine.Random`, `Time` yok.
- **C# 9'u geçme.** Aynı dosyalar hem Unity'de hem .NET sunucusunda derleniyor.
- **Saat tutma.** Zaman `Ilerle(TimeSpan gecen)` ile dışarıdan gelir.
- **Rastgelelik tohumla:** `System.Random(tohum)`. Aynı tohum = aynı oyun.
- **Hata fırlatma.** Her komut `Sonuc` döner (`Tamam` ya da hata kodu).
- Her kural değişikliğine test. Yeni testi yazınca **bir kere bilerek boz**, kırmızıya döndüğünü gör, sonra geri al.

## İstemci (Unity tarafı)

- **İstemci kural hesaplamaz.** "Seçilebilir mi", "ceza var mı", "sıra kimde" gibi her cevap `Gorunum`'dan gelir. Ekranda ikinci bir kural kopyası yazma.
- Elden ele modda her karede `Ilerle(Time.deltaTime)` çağrılır.
- İçerik dosyaları (`Assets/Icerik/*.txt`) `TextAsset` olarak yüklenir, metni kural koduna verilir.
- Arayüz önce **telefon dikey** için tasarlanır, PC'ye genişletilir.

## Gizlilik (pazarlık yok)

- Soru metni **sadece B'nin** görünümünde.
- **Güme giden soru hiçbir yere yazılmaz:** ne sunucuya, ne cihaza, ne loga, ne grup paketine.
- Gizli görev metni sadece sahibinin görünümünde.
- Kızgın Çakmak eşiği hiçbir görünümde yok.

## Gizli bilgiler (repo herkese açık olabilir)

- **Anahtar, şifre, API anahtarı, keystore, sunucu IP'si ya da bağlantı dizesi repoya asla girmez.** Koda gömülmez, örnek dosyaya da yazılmaz.
- Yerel ayarlar git'in yok saydığı dosyalarda durur: `.env`, `*.local.json`, `appsettings.Development.json`, `*.keystore`.
- Bir gizli bilgi yanlışlıkla commit'lendiyse **silmek yetmez**, geçmişte kalır. Dur, kullanıcına haber ver: o anahtar hemen iptal edilip yenisi alınmalı.
- Commit'ten önce `git diff --staged`'e bak. Uzun rastgele dizeler ya da `key`, `secret`, `password` içeren satırlar varsa commit'leme, sor.

## İsimlendirme

- Kod isimleri **Türkçe ama ASCII:** `SoruSor`, `GorunumAl`, `Oyuncu`. `ş ğ ı ö ü ç` sadece metinlerde ve yorumlarda.
- Oyuncuya görünen metinler koda gömülmez. Unity Localization tablosuna girer.

## Git

- `main`'e doğrudan commit yok. Branch: `g1/kisa-aciklama` ya da `g2/kisa-aciklama`. `main`'e PR ile girilir.
- Küçük commit, açıklayıcı mesaj. **Force push yok.**
- `Library/`, `Temp/`, `Logs/`, `UserSettings/`, build çıktıları commit'lenmez (`.gitignore` hallediyor). `.meta` dosyaları **her zaman** commit'lenir.
- Unity sahnesini (`.unity`) ve prefab'ı aynı anda iki kişi düzenlemez.

## Emin değilsen

Dur ve sor. Tahminle sözleşmeyi, başkasının klasörünü ya da proje ayarlarını değiştirme.
