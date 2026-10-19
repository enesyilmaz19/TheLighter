# G2 (Arda): Önce Android

Bu dosya Arda'nın yapay zekâsı için bir görev tanımı. Kullanım: "`tasks/g2-android.md`'yi oku ve 0'dan başla."
Yazan: G1 (Enes), 2026-10-19.

**Karar (Enes ve Arda):** oyun önce **Android**'de çıkıyor, Steam sonra. iOS daha sonra. `tasks/g2-pc.md`'deki "önce PC" önceliği bitti: PC build'i çalışır kalsın ama cilası (Steamworks, 3.3 çok bilgisayarlı deneme) Steam'e kalıyor. Elle oyun denemeleri (`tasks/g2-test.md` 2. bölüm) bundan sonra **telefonda** yapılır, o yüzden bu dosya denemelerden önce.

`tasks/g2-s4-s5.md`'deki ilkeler aynen geçerli: sadece senin klasörlerin (`Assets/Scripts`, `Assets/UI`, ProjectSettings); `Assets/Kurallar`, `Sunucu/`, `docs/` değişmez. Sunucu tarafında bir şey gerekirse Enes'e yaz.

---

## 0. Durum: PR'lar açılabilir

Enes `g2/cakmak-gecesi`'de (öbür 5 dalı içeriyor) bütün testleri koştu, 2026-10-19:

| Test | Sonuç |
|---|---|
| `dotnet test Sunucu/KuralTestleri` | 365/365 |
| `dotnet test Sunucu/SunucuTestleri` | 184/184 |
| Unity EditMode | 594 geçen, 0 kalan, 8 atlanan |
| Unity `-testCategory "Sunucu"`, yerel sunucu açık | 8/8 (Çakmak Gecesi'nin 8 botu 147 sn) |

**Dikkat:** sunucu açık olsa da tam koşuda 8 bot testi atlanıyor; sadece `Sunucu` kategorisi seçilince koşuyorlar. 1. bölümdeki ön koşul listesine bu yüzden ikisi ayrı yazılı.

Altı dalın PR'larını aç. Dallar üst üste kurulu: her PR bir öncekinin üstüne; sıra `s5-kapanis` → `ayarlar-dil` → `kostebek` → `rus-ruleti` → `takim` → `surum` → `casus` → `cakmak-gecesi`. Hangisini nasıl birleştireceğine sen karar ver (tek PR da olur), yeter ki main'e sırayla girsin.

## 1. Build ayarları

Bugünkü ProjectSettings'te (senin dalında) olanlar ve eksikler:

| Ayar | Bugün | Olması gereken |
|---|---|---|
| Scripting backend | IL2CPP ✓ | — |
| Mimari | ARM64 ✓ | Play 64 bit istiyor |
| Min SDK | 26 ✓ | — |
| Target SDK | Otomatik (yüklü en yeni) | Yüklemeden önce Play'in o yıl istediği seviyeye bak (her ağustos yükseliyor), Android SDK'sı güncel olsun |
| Paket adı | Android için yok (`com.DefaultCompany.2D-URP` sadece Standalone'da) | **Enes ile birlikte karar:** Play'e bir kez yüklenince değişmez. Karar gelene kadar `com.thelighter.cakmak` gibi bir yer tutucu, Play'e yüklemeden önce kesinleşir |
| Ürün adı | `TheLighter` | Telefonda ikonun altında görünen ad. Enes ile karar (Çakmak?) |
| Yön | Otomatik döndürme, dört yön açık; kod mobilde dikeye zorluyor | Player Settings'te sadece dikey: açılışta yan dönüp düzelmesin |
| Çıktı | APK | Play için **AAB** (Build App Bundle). Deneme için APK da olur |
| Sürüm | `bundleVersion 1.0`, `AndroidBundleVersionCode 1` | Play'e her yüklemede kod +1 |
| İmza anahtarı (keystore) | yok | Bir anahtar oluştur. **Dosya ve şifresi repoya girmez** (`docs/Guvenlik.md`); ikinizin de ulaşabileceği bir yerde yedekli. Play App Signing açılacak: senin anahtarın sadece yükleme anahtarı |
| Sunucu adresi | Online menüsünde "Sunucu" alanı, varsayılan `ws://localhost:5000/ws` | Karar 29 (`tasks/g1-todo.md`): yayın adresi koddaki tek sabitte, alan sadece editörde ve geliştirme build'inde. VPS gelene kadar sabit boş kalabilir |

## 2. Telefonda sunucuya bağlanmak (VPS gelene kadar)

Sunucu varsayılan olarak sadece `localhost`'u dinliyor: telefon bağlanamaz. Bilgisayar ve telefon aynı Wi-Fi'deyken:

```bash
dotnet run --project Sunucu/Cakmak.Sunucu -- --urls http://0.0.0.0:5000
```

- Telefonda adres: `ws://<bilgisayarın yerel IP'si>:5000/ws` (`ipconfig` → IPv4, ör. `192.168.1.34`). Windows güvenlik duvarı ilk açılışta sorar: özel ağa izin ver.
- Android düz `ws://`'yi engellerse (bağlanmıyor, sunucu günlüğüne hiçbir şey düşmüyor): Player Settings → "Allow downloads over HTTP" sadece **geliştirme build'inde** açık. Yayın build'i hep `wss://` ile gider, VPS'te HTTPS'i Caddy veriyor.
- Farklı ağlardan deneme (mobil veri) VPS'i bekliyor. VPS'i Enes kuracak, hazır olunca adresi sana yollar.

## 3. Telefona özel ekran işleri

Bir kısmı zaten var; telefonda dene, eksikleri tamamla:

| Konu | Bugün | Yapılacak |
|---|---|---|
| Ekran kararmasın | ✓ `Screen.sleepTimeout` | Telefonda dene |
| Çentik (safe area) | ✓ `OlcekVeGuvenliAlan` | Çentikli bir telefonda bak: düğmeler ve üst bilgi kesilmiyor |
| Arka plandan dönünce yeniden bağlanma | ✓ `OnApplicationPause` | Online oyunda WhatsApp'a geçip 30 sn sonra dön: oyuna aynı isimle dönülüyor |
| Klavye | Sohbet paneli kayıyor ✓ | **Soru yazma** kutusu da klavyenin altında kalmasın (Ana Plan 6.2) |
| Geri tuşu | yok | Android geri tuşu (Input System'da `escapeKey`): açık panel/perdeyi kapatır, menüde bir önceki ekrana döner; oyunda "Oyundan çıkılsın mı?" sorar. Hiçbir yerde uygulamayı habersiz kapatmaz |
| Paylaşım kartı | PNG `persistentDataPath/Kartlar`'a kaydediliyor | Ana Plan 8.4: "Paylaş" telefonun paylaşma menüsünü açar (Instagram, WhatsApp). Android `ACTION_SEND` intent'i; hazır eklenti NativeShare (yasirkula, MIT) ya da kendi küçük Java köprün. Kartta Steam linki yerine Play linki (link Play sayfası açılınca gelir, şimdilik yer tutucu) |
| Dokunma alanları | ? | Düğmeler en az ~48 dp; bölünmüş ekran mini oyunlarında iki kişinin parmağı birbirine değmesin |
| Kare hızı | Unity'nin mobil varsayılanı 30 | Oyunda 60 (`Application.targetFrameRate`), pil derdi çıkarsa sonra düşür |
| İkon ve açılış ekranı | Unity varsayılanı | Görsel kararlar (`docs/Gorsel.md`) gelene kadar yer tutucu. Android uyarlanabilir ikon iki katman ister (ön + arka plan) |

## 4. Testler

- **Otomatik:** 0. bölümdeki dört koşu her PR'dan önce (`tasks/g2-test.md` 1. bölüm). Yeni mantık (geri tuşunun ekran ekran ne yaptığı, adres sabitinin geliştirme build'inde değişebilmesi) için EditMode testi.
- **Elle, telefonda:**
  - En az iki Android telefon; biri elinizdeki en zayıf telefon olsun (3D masa kararı da ona göre verilecek, `docs/Gorsel.md` 19).
  - Elden ele: her oyun bir kez baştan sona tek telefonda.
  - Online: iki telefon + bilgisayar, 2. bölümdeki yerel ağ kurulumuyla. 18 kişilik Çakmak Gecesi: telefonlar + bilgisayar pencereleri ya da botlar.
  - `tasks/g2-test.md` 2. bölümündeki tablo ve Çakmak Gecesi maddeleri artık telefonda yapılıyor. `tasks/g2-pc.md` 3.3'ün (çok bilgisayar) yerine: çok telefon aynı Wi-Fi'de.

## 5. Bu dosyada olmayanlar

- **Play Console hesabı, kapalı test (12 kişi × 14 gün), mağaza sayfası, gizlilik metni:** Enes.
- **Rapor ve engelleme:** Play kullanıcı içeriği olan uygulamada şart. Önce sunucu tarafı (Enes), sonra sana ayrı görev gelecek.
- **Sesli sohbet (Vivox):** ilk sürümde olup olmayacağına Enes ile karar verin. Olmazsa mikrofon izni ve onun gizlilik yükü de kalkar.
- **Satış modeli** (uygulama içi satın alma gerekirse Google Play Billing): karar bekliyor.
- **3D masa ve görseller:** `docs/Gorsel.md`, birlikte.

## 6. Bitti ölçütü

- Altı dal main'de.
- AAB çıkıyor, iki telefona kuruluyor, dikey açılıyor.
- İki telefonda elden ele bir oyun baştan sona; iki telefon + bilgisayarla online bir oyun baştan sona.
- 3. bölümün her satırı telefonda denendi: geri tuşu, iki klavye, çentik, arka plandan dönüş, paylaşma menüsü.
- 0. bölümdeki dört koşu yeşil.
