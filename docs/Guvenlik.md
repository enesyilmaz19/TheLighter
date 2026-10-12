# Güvenlik

Çevrim içi bir oyunda olması gereken güvenlik katmanları, bu oyun için hangilerinin gerektiği ve nerede oldukları. 2026-10-12.

## Neyi, kime karşı koruyoruz

**Korunan:**
1. **Gizli bilgi:** soru metni (sadece B görür; güme gidince hiç kimse), gizli görevler, Soran Gizli'nin A'sı, Kızgın Çakmak eşiği, oylamada kimin ne dediği. Oyunun kendisi bu.
2. **Adalet:** mini oyun sonuçları, zarlar, kaos ve ceza kartları.
3. **Sunucunun ayakta kalması:** bir kişi herkesin oyununu bozamasın.
4. **Oyuncular arası:** isim taklidi, metinle başkasının ekranını bozma.
5. **Kişisel veri:** az (isim, IP, oyun içi metin), saklanmıyor.

**Kime karşı:** hile yapan arkadaş (değiştirilmiş istemci, kendi ağ trafiğini okuyan), oda kodu tahmin eden yabancı, bot ve yük saldırısı.

## Katmanlar

| # | Katman | Bu oyunda | Durum, yer |
|---|---|---|---|
| 1 | **Aktarım şifrelemesi** (TLS, `wss://`) | Gerekli: sorular ve anahtar ağda açık gitmesin | Caddy, kendiliğinden sertifika (`Sunucu/Kurulum/Caddyfile`) |
| 2 | **Yetkili sunucu:** kurallar sunucuda, istemci sadece niyet yollar | Gerekli: istemciye güvenilmez | Var (S1/S4). Kural motoru sunucuda çalışır, istemcinin hesabı yok sayılır |
| 3 | **Gizli bilgi sadece sahibine** (görünüm filtresi) | Gerekli: oyunun özü | Var, testli: her oyuncuya kendi `durum`'u; özet sadece oyun sonunda; olaylarda soru sadece ifşada |
| 4 | **Tahmin edilemeyen rastgelelik** | Gerekli: tohum bulunursa bütün görevler, eşik ve gelecek zarlar bilinir | **Bu adımda:** online'da kural motoru işletim sisteminin kriptografik kaynağını kullanır (`GuvenliRastgele`) |
| 5 | **Girdi doğrulama** | Gerekli | Var: 4 KB mesaj, alan tipleri, ad listeleri, 140/20 karakter. **Bu adımda:** isim ve sorudan görünmez/yön karakterleri atılır, boşluklar tekleşir (`TemizMetin`); görünmez karakterle başkasının kılığına girilemez, boş görünen isim/soru yazılamaz |
| 6 | **Oturum** | Gerekli: hesap yok, yeniden bağlanma anahtarı var | Var: 128 bit rastgele anahtar, sadece sahibine. **Bu adımda:** sabit sürede karşılaştırma |
| 7 | **Hız ve kaynak sınırları** | Gerekli | S4: IP başına bağlantı/giriş, bağlantı başına mesaj, başlatılmayan lobi. **Bu adımda:** toplam oda (`SUNUCU_DOLU`), toplam bağlantı, odaya girmeyen bağlantı 120 sn'de kesilir |
| 8 | **Tarayıcıdan gelen bağlantı** | Gerekli: bir web sayfası ziyaretçilerini sunucuya saldırtamasın | **Bu adımda:** `Origin` başlığı olan (tarayıcı) WebSocket reddedilir (403). Uygulama bu başlığı yollamaz |
| 9 | **Bilgi sızıntısı** | Gerekli | **Bu adımda:** `Server` başlığı yok. Production'da hata sayfası yığın izi göstermez (`cakmak.service`) |
| 10 | **Gizli bilgiler** (anahtarlar) | İleride (Vivox) | Repoda yok, `.gitignore` korur. Sunucuda `/etc/cakmak/ortam` (root:cakmak, 0640) |
| 11 | **Sunucu sıkılaştırma** | Gerekli | **Bu adımda dosyalar hazır** (`Sunucu/Kurulum/`): SSH sadece anahtar, güvenlik duvarı 22/80/443, otomatik güncelleme, fail2ban, root olmayan kullanıcı, systemd kum havuzu, uygulama sadece localhost'u dinler |
| 12 | **Bağımlılık taraması** | Gerekli | `dotnet list package --vulnerable --include-transitive`: temiz (2026-10-12). Her sürümden önce tekrar |
| 13 | **Günlük kaydı** | Gerekli | Var: oyuncu metni, soru ve IP günlüğe yazılmaz (AGENTS.md), hatada sadece hata tipi |
| 14 | **Kişisel veri (KVKK)** | Gerekli | Sunucuda kalıcı veri yok; grup kaydı oyuncunun cihazında. Aydınlatma metni S6 |
| 15 | **Sohbet ve moderasyon** | Gerekli: herkese açık oda yok ama oyuncular birbirine yazıyor | Var (S5): sohbet `TemizMetin`'den geçer, 200 karakter, saniyede 1; DM sadece iki kişiye; sunucu mesaj saklamaz. Temiz modda filtre (`TemizFiltre`), 3. ihlalde atma; her modda kurucu atabilir, atılan o odaya dönemez. Rapor + engelleme S6 |

## Gerekmeyenler (şimdilik)

| Katman | Neden |
|---|---|
| Hesap, şifre, iki adımlı doğrulama | Hesap yok: oda kodu + isim + yeniden bağlanma anahtarı |
| İstemci bütünlüğü, hile tespiti, kod karartma | Sunucu yetkili: değiştirilmiş istemci avantaj kazanamaz (hamle bir seçim, zar sunucuda, süre ölçen Reaksiyon online'da kapalı) |
| Veritabanı güvenliği, SQL enjeksiyonu | Veritabanı yok |
| Şifreli depolama | Sunucuda saklanan veri yok |
| Ödeme güvenliği | Satış modeli belli değil; mağaza üzerinden olursa mağaza yapar |
| Ayrı DDoS hizmeti (Cloudflare vb.) | Küçük ölçek. Gerekirse eklenir, o zaman `KnownNetworks` ayarı şart (`Sunucu/Kurulum/BENIOKU.md`) |

## Sonra gelecekler

- **Rapor ve engelleme (S6):** Google Play şartı.
- **Vivox ses (S6):** erişim anahtarı sunucuda imzalanır; gizli anahtar istemciye ve repoya girmez.
- Yeni bağlantı açma hızı sınırı; oyun sonunda açık kalan odaya zaman aşımı.

## İstemci (G2) için

1. **Zengin metin kapalı:** oyuncunun yazdığı her şeyi gösteren etikette (isim, soru, ileride sohbet) `enableRichText = false`. Unity'de varsayılan açık: `<size=500>` ya da `<color>` yazan biri başkalarının ekranını bozar.
2. **Bağlantıyı oda kur/katıl anında aç:** odaya girmeyen bağlantı 120 sn'de kesilir.
3. **`Origin` başlığı ekleme:** sunucu tarayıcı sanıp reddeder.
4. **Yayın sürümünde sadece `wss://`.** `ws://localhost` sadece yerel deneme.
5. **Yeniden bağlanma anahtarını günlüğe yazma**, başka yere gönderme.
6. Yeni hata kodları: `COK_HIZLI` (biraz bekle), `SUNUCU_DOLU` (sonra dene).
