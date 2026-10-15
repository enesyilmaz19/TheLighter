# G1 (Enes) — İş listesi

Kaynak: `docs/Ana Plan.md` · Branch'ler: `g1/<iş>` → `main`

## 📥 ENES'İN MASASI

- [x] ~~İmza temizliği~~ → **vazgeçildi (2026-10-10).** Contributors listesi zaten temiz (sadece enesyilmaz19 ve ardatelli0). İmza sadece ilk 2 commit'in sayfasında görünüyor. Geçmiş yeniden yazılmayacak.
- [ ] **Sözleşme oturumu (Arda ile):** aşağıdaki "Sözleşmeye önerilen değişiklikler" (14 madde), "Kararlar" (30 madde) ve `docs/Protokol.md`'nin sonundaki farklar tablosu birlikte onaylanır. Onaylananlar `docs/Ana Plan.md` 5.1/5.2'ye işlenir. Arda'nın onay bekleyenleri: kararlar 20–23, 25–30, `docs/Guvenlik.md`, `.gitignore` eklemeleri, sunucu adresinin build'e konması (karar 29).
- [ ] **Temiz kara listesi ve izin listesi:** `Assets/Kurallar/TemizFiltre.cs` başındaki üç liste içerik (Ana Plan 4: "Temiz kara listesi ve izin listesi" G1'de). Başlangıç listesini Claude yazdı, gözden geçir, ekle/çıkar. Eklediğin her kelime için `TemizFiltreTestleri`'ne bir satır, paket soruları zaten deneniyor.
- [x] **Öncelik (2026-10-13, Enes):** önce PC. İlk testler bilgisayarda; Android PC bitince. S6 şimdilik bekliyor. Arda'nın görevi `tasks/g2-pc.md` (S5 kapanışı, PC bulguları: aynı bilgisayarda iki pencerenin kimlik çakışması, `runInBackground` kapalı, pencere modu; PC test planı).
- [ ] **VPS + alan adı** (aşağıda "S4 sonu"): hazır olunca `wss://<alan-adı>/ws` adresini Arda'ya ilet, 4 cihazla farklı ağlardan dene (S4 bitti ölçütünün son maddesi).

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
- [x] **S5 kopma ve AFK (2026-10-12):** kopanın yeri 60 sn tutulur, dönmezse oyundan çıkar (elindeki iş hemen biter), üst üste 2 süre dolduran AFK (atlanır, seçilemez, bir mesajla kalkar), bağlı oyuncu 4'ün altında oyun durur ve 2 dk bekler, herkes koparsa oda 2 dk tutulur. Kural motoru `Oyun.Baglanti.cs` (sözleşmedeki `OyuncuKoptu/Dondu/Cikti` + `Buradayim`), sunucu `Oda.cs`, protokol "Bağlantı koparsa". 16 kural + 7 sunucu testi, 20 bilerek bozma yakalandı, Unity 221/221. Görünmez karakterli isim: güvenlik adımında yapıldı
- [x] **S5 bekleme odası, tekrar oyna, atma (2026-10-12):** oyun sürerken katılan bekleme odasına girer (sadece herkese açık bilgi; komutta `OYUNDA_DEGILSIN`), kapasite bekleyenler dahil 10. Kurucu `oyunSonu`'nda `tekrarOyna` ile lobiye döner (kopanlar düşer, bekleyenler oyuncu olur, ayarlar kalır). Kurucu `at`: atılan `ATILDIN` alır, bağlantısı kapanır, odaya anahtarıyla ve aynı isimle dönemez (hesap yok: yeni isimle girebilir, Ana Plan 6.4'te kabul edilmiş). 12 yeni sunucu testi, 14 bilerek bozma yakalandı
- [x] **S5 sohbet + DM, Temiz filtresi (2026-10-12):** `sohbet` mesajı (genel herkese ve yazana, DM alana ve yazana; bekleme odası da yazar; 200 karakter; saniyede 1, patlama 2; sunucu saklamaz). Temiz filtresi kural motorunda (`TemizFiltre`, Ana Plan 6.4'ün adımları), `SoruSor` Temiz'de `Sonuc.Uygunsuz` döner (elden elede de). Sunucuda soru + sohbet ihlali aynı sayaçta, isme göre; `UYGUNSUZ` (`ihlal`, `sinir`), 3.'de `at` ile aynı yoldan atılır (`neden: "temiz"`), kurucuysa kuruculuk devredilir. 36 kural + 17 sunucu testi, 29 bilerek bozma (28 yakalandı; kaçan, gereksiz bir `Replace`'ti, silindi). Arda'nın uçtan uca bot testi yeni sunucuyla geçti. Arda'nın metin tablosuna `hata.Uygunsuz` yer tutucu eklendi (MetinTestleri enum için istiyor)
- [x] **S5 online grup paketi (2026-10-13):** kurucu lobide `grupPaketi` ile parça parça yükler (`ilk` öncekini siler; temizlik, 140 karakter, tekrarsız, en fazla 500; bozuk parça hiç eklenmez), lobi `durum`'unda `grupPaketi` sayısı, `paketler`'de `"@bizim"` (yüklenmeden seçilemez), Temiz'de kullanılmaz, sadece bellekte, `tekrarOyna`'da kalır. 14 sunucu testi, 14 bilerek bozma yakalandı. İstemci tarafı Arda'da (`tasks/g2-s5-sohbet.md`, 5. madde). **G1'in S5 işleri bitti**
- [x] **Köstebek (2026-10-13), yeni oyunların ilki:** kural motoru `Oyun.Kostebek.cs` (rol, köstebek görevi, her 5 turda + erken suçlama, son oylama, kazanma), `Ayarlar.Oyun` yeni eksen (Çakmak davranışı aynı, rastgelelik dahil). Sunucu: `oyun` ayarı, `sucla` / `suclamaOyu`, durum ve özet alanları; 4'ün altında oyun oylamasız biter. 17 kural + 9 sunucu testi, rastgele 300 Köstebek oyunu; 38 bilerek bozmanın hepsi yakalandı. Arda'nın botları yeni sunucuyla geçti. Ekranlar: `tasks/g2-kostebek.md`. Sırada: Rus Ruleti
- [x] **Rus Ruleti (2026-10-13), yeni oyunların ikincisi:** `Oyun.RusRuleti.cs` (tabanca 6 hazne, gizli mermi + gürültülü ısı, can 1–3, tetik devret, eleme, 3 kişide sorusuz final, son kalan kazanır; tur sayısı yok). `Oyun.AyarlarUyumlu`: "birlikte olamaz" kuralları tek yerde, sunucunun lobisi de kullanıyor. Sunucu: `can`, `tetikCek` / `tetikDevret`, durum ve özet alanları, olayda `hedef`. 16 kural + 6 sunucu testi, rastgele 300 Rus Ruleti oyunu, 39 bilerek bozmanın hepsi yakalandı. Ekranlar: `tasks/g2-rus-ruleti.md`. Sırada: Takım Düellosu
- [x] **Takım Düellosu (2026-10-13), yeni oyunların üçüncüsü:** `Oyun.Takim.cs` (rastgele dengeli ya da kurucunun dağılımı, tek sayıda küçük takıma +1, düello puanı + ifşa bonusu, Soran Gizli'de bonus yok, 7 puanda biter). Oyun kancaları tek yerde: `Oyun.Kancalar.cs` (kurulum, tur başı kararı, tur sonucu, sonuç yazma, görünüm). Sunucu: `takimlar` ayarı (lobiden çıkan düşer, tekrar oynayınca sıfırlanır), takım sohbeti (`kime: "takim"`), durum/olay/özet alanları. 10 kural + 5 sunucu testi, rastgele 300 Takım oyunu (puan = başlangıç + puan olayları denetimi), 36 bilerek bozmanın hepsi yakalandı. Ekranlar: `tasks/g2-takim.md`. Sırada: Casus
- [x] **Casus (2026-10-14), yeni oyunların dördüncüsü (Imposter dahil):** `Oyun.Casus.cs` (mekân ya da kelime, mekânda herkese ayrı rol, liste herkese açık, her 4 turda oylama, yakalanınca son şans, istediği an bir kez tahmin, son oylama). Köstebek'in oylaması iki oyunun ortağı oldu: `Oyun.Suclama.cs` (Köstebek davranışı aynı, testleri değişmedi). İçerik taslağı `CasusIcerik.cs`: 30 mekân × 9 rol, 5 kategori × 20 kelime, hepsi Temiz filtresinden geçiyor. Sunucu: `casus` ayarı, `casusTahmini`, durum/olay/özet alanları, sır casusa hiç gitmiyor. 12 kural + 7 sunucu testi, rastgele 300 Casus oyunu, 37 bilerek bozmanın hepsi yakalandı. Ekranlar: `tasks/g2-casus.md`. Sırada: Vampir Köylü
- [x] **Vampir Köylü (2026-10-14), yeni oyunların beşincisi ve sonuncusu:** `Oyun.Vampir.cs` (roller: 1–2 vampir, doktor, gözcü, köylü; gündüz canlıların yarısı kadar tur, linç, gece herkes seçer, sabah, kazanma, rol açılması ayarı, az kişide sadece linç ve gece). Sohbet kanal kuralları kural motoruna taşındı (`Oyun.Sohbet.cs`: takım, vampir, ölüler; Vampir'de gece ve ölüler genele yazamaz). Sorular `Assets/Icerik/VampirSorulari.csv` (40). Sunucu: `rolAcilir`, `lincOyu`, `geceSecimi`, kanallar, durum/olay/özet alanları. 20 kural + 3 sunucu testi, rastgele 300 Vampir oyunu (köylü 93, vampir 207 kazandı), 52 bilerek bozmanın hepsi yakalandı. Ekranlar: `tasks/g2-vampir.md`. **Beş yeni oyun bitti.**
- [x] **Çakmak Gecesi (2026-10-14/15), Vampir Köylü'nün yerine (Enes'in kararları: ad, 6–18 kişi, 24 rol, gecelik kurban, rol kapatma):** `Oyun.CakmakGecesi.cs`. Taraflar Masum/Katil + tarafsızlar; 9 masum, 9 katil, 6 tarafsız rol (ToS, GGD, Feign, Among Us'tan; Kulak çakmağa özel). Kişi sayısına göre otomatik dağılım, kurucu kapatır. Gece sırası (işaret/susturma/kandırma → koruma → saldırılar → bakışlar → ölümler → ısırma/çalma), Suikastçı linçte, kazanma (Soytarı/Cellat oyunu bitirir, berabere, Hayatta Kalan). Oda 18 kişilik, diğer oyunlar 10'da. Kulak güme sorusunu sadece güme ekranında görür: Ana Plan 8.5'e bilinçli istisna (docs/Oyunlar.md 5). 34 kural + 4 sunucu testi, 300 rastgele oyun 6–18 kişi (yedi sonucun hepsi çıktı), 68 + 18 bilerek bozmanın hepsi yakalandı. Rastgele test 4 gerçek hata buldu (Cellat kazanınca Soytarı'ya dönüyordu, beraberlikte Seri Katil kazanan sayılıyordu, gece ölen Hırsız/Vampir dönüşüm yapabiliyordu, ölü Cellat'ın rolü değişiyordu). Ekranlar: `tasks/g2-cakmak-gecesi.md`. **Beş yeni oyun bitti.**
- [x] **Çakmak Gecesi'ne çakmağa özel 17 rol (2026-10-15, Enes'in seçimi):** `Oyun.CakmakRolleri.cs`. Masum: Sorgucu, Hakem, Kollayıcı, Yüzleşmeci, Sezgici, Sınayıcı, Siper, Söndürücü. Katil: Kundakçı, Dinleyici, Fitneci, Kuklacı, Kışkırtıcı. Tarafsız: Kibritçi (öldüren), Gözde, Düellocu, Sır Küpü (ek kazanan). Yetenekler gündüz turuna bağlı (A → B, B → C, düello sonucu); gündüz yeteneği komutu `yetenek`; Hakem ve Söndürücü elden elede gizli kalsın diye gece hazırlar (Enes'in kararı). Toplam 41 rol. 17 kural + 1 sunucu testi; rastgele 300 oyunda yeni rollerin hepsi devreye giriyor, 43 bilerek bozmanın hepsi yakalandı. Dinleyici ile Kulak istisnası iki role çıktı
- [ ] **Enes: Çakmak Gecesi'ni gözden geçir:** sorular (`Assets/Icerik/GeceSorulari.csv`), rol tanımları ve dağılım tablosu (`docs/Oyunlar.md` 5). Benim verdiğim kararlar: Falcı grubu canlıların üçte biri (en az 3), 2/3 hak; Deli Koruyucu/Dedektif/Bekçi/Falcı sanır; Şerif masum vurursa korunsa bile ölür; Seri Katil'in kaçınması sadece katillere karşı; Hırsız tarafsızdan çalarsa çalınan Hayatta Kalan olur
- [ ] **Enes: Casus içeriğini gözden geçir** (`Assets/Kurallar/CasusIcerik.cs`): mekân, rol ve kelime ekle/çıkar. Kural: her mekânda en az 9 farklı rol, her kategoride en az 10 kelime; test denetliyor
- [x] **S3 online hatası (2026-10-13, Rus Ruleti sırasında bulundu):** mini oyunun ya da Grup Kararı oylamasının süresi dolup İkiye Katla / Bedel kararına geçilince olay yayılmıyordu, sunucu durum yollamıyordu: online'da B bedel sorusunu hiç görmüyor, 5 sn sonra "hayır" sayılıyordu. `KararBekleniyor` olayı eklendi. Rastgele oyun testine genel kural: zamanla aşama değiştiyse olay yayılmış olmalı (1600 oyunda denetleniyor). Sunucu regresyon testi düzeltmesiz kırmızı
- [ ] Oturumda karar: "Temiz modda sadece Aile Dostu paketi" (Ana Plan 8.4) şu an sadece istemcide (Arda'nın lobisi Temiz'de `paketler: ["AileDostu"]` yolluyor). Sunucu Temiz + Cesur'u reddediyor ama Temiz + başka paketi reddetmiyor. Reddetsin mi (Cesur gibi `GECERSIZ_GIRDI`), yoksa Temiz seçilince paketleri kendisi mi düşürsün? Değiştirilmiş istemci sadece kendi odasını etkiler, acil değil
- [x] **Reaksiyon hatası (Arda'nın editör testi, 2026-10-10):** süre dolunca erken basan kazanabiliyordu (iki yönde de). Önce 3 kırmızı test, sonra `Erken()` yardımcısı iki yolda da kullanılıyor, bilerek bozuldu. 88 kural + 57 sunucu, Unity'de 107/107. Kural Ana Plan 6.5'e yazıldı

## S4 — online'a açılmadan önce (güvenlik)

Arda'nın Claude'unun listesinden (2026-10-10). Elden ele (Playtest 1) için hiçbiri gerekmiyor.

- [x] **(2026-10-12)** IP başına sınırlar: aynı anda 32 bağlantı (aşılınca HTTP 429), dakikada 30 `odaKur`+`katil` (oda kodu denemeyi yavaşlatır), bağlantı başına saniyede 10 mesaj (20'lik patlama); aşılınca `COK_HIZLI`. .NET'in hazır sınırlayıcıları (`System.Threading.RateLimiting`), değerler ayardan (`Sinirlar__...`). IPv6'da /64 bloğu tek IP
- [x] **(2026-10-12)** Gerçek IP: forwarded headers, sadece localhost'tan gelen `X-Forwarded-For`'a güvenir (Caddy aynı makinede olmalı)
- [x] **(2026-10-12)** 30 dk'da başlatılmayan lobi kapanır. Biçim `docs/Protokol.md`, "Sınırlar". 6 yeni test (`SinirTestleri`), her biri kendi küçük sınırlı sunucusunda
- [ ] Sonra (gerekirse): yeni bağlantı açma hızı (şimdi sadece aynı anda açık sayısı sınırlı), oyun sonunda açık kalan odaya zaman aşımı
- [x] Zaten var: 4 KB mesaj sınırı, sınırlı gönderim kuyruğu, 20 sn'de sessiz kopanı fark etme, boş odayı silme, bozuk JSON'da bağlantıyı düşürmeme
- [x] **Güvenlik katmanları (2026-10-12, Enes'in isteğiyle):** hangi katmanın gerekli olduğu ve neden `docs/Guvenlik.md`'de. Bu adımda eklenenler: kriptografik rastgelelik (online'da tohum tahmin edilemez: görevler, eşik, zarlar), `TemizMetin` (isim ve soruda görünmez/yön karakterleri; kılık değiştirme ve ekran bozma), tarayıcı WebSocket'i reddi (403), toplam oda (`SUNUCU_DOLU`) ve bağlantı sınırı, odaya girmeyen bağlantı 120 sn'de kesilir, anahtar sabit sürede karşılaştırılır, `Server` başlığı yok, kurulum dosyaları (`Sunucu/Kurulum/`: systemd kum havuzu, Caddyfile, adımlar). 14 yeni test (7 kural + 7 sunucu), 15 bilerek bozma yakalandı
- [x] Arda (PR #8/#9, 2026-10-12): `enableRichText = false` (oyuncu metni), bağlantı oda kur/katıl anında açılıyor, `Origin` yollanmıyor (uçtan uca test sunucuya bağlandı: Origin olsaydı 403 alırdı)
- [ ] Yayında `wss://`: adres VPS + alan adını bekliyor, build'e nasıl konacağı karar 29

## S4 sonu — sunucu kurulumu (Enes'in hesabı ve ödemesi gerekiyor). Adımlar ve dosyalar: `Sunucu/Kurulum/`

- [ ] VPS: Hetzner CX23 (Arda'nın notu: Haziran 2026 zammından sonra ~5,49 €/ay, plandaki "5 $" eski), Ubuntu LTS
- [ ] SSH sadece anahtarla (şifre kapalı) · güvenlik duvarında sadece 22, 80, 443 · otomatik güvenlik güncellemeleri · fail2ban
- [ ] Uygulama systemd servisi, root olmayan kullanıcı, sadece localhost'u dinler · önünde Caddy (alan adı + HTTPS kendiliğinden, wss:// ek ayarsız). **Caddy aynı makinede olmalı:** sunucu `X-Forwarded-For`'a sadece localhost'tan güveniyor. Önüne Cloudflare gibi bir şey daha konursa `ForwardedHeadersOptions.KnownNetworks` ayarlanmalı, yoksa herkes aynı IP görünür
- [ ] Gizli bilgiler (Vivox anahtarı vb.) sunucuda ortam değişkeninde. Repoya ve Unity build'ine asla girmez
- [ ] Alan adı
- [x] **Kurulum denendi (2026-10-13):** `Sunucu/Kurulum/BENIOKU.md`'nin 2. adımı WSL Ubuntu 24.04'te (systemd) birebir uygulandı: servis kum havuzuyla açıldı, oyun, gizlilik ve Temiz filtresi geçti. Rehbere `libicu74` ve `ufw` notu eklendi
- [ ] Mağazadan önce (S6): istemci–sunucu protokol sürümü kontrolü. Sunucu güncellenince eski build'le bağlanan anlaşılır bir "güncelle" mesajı alsın. Arkadaşlar arası testte gerek yok

## S6 — mağaza ve yasal (mağazadan önce şart)

- [ ] Google Play kullanıcı içeriği: içerik yazmadan önce kullanım şartları kabulü · şartlarda yasak içerik tanımı · uygulama içi rapor + engelleme · sürekli moderasyon (Temiz filtresi + rapor + engelleme planda var)
- [ ] Play formları: gizlilik politikası linki · Veri Güvenliği formu · içerik derecelendirme anketi (kullanıcı içeriği: evet). 13+ olduğu için Families programına girmiyor
- [ ] KVKK aydınlatma metni (VERBİS'ten muaf olsa da zorunlu). Toplanan veri az: isim, IP, rapor gelirse son 50 mesaj. Kesin hüküm için bir hukukçuya danışılmalı

## S3 — Tekrar oynatma (kurallar G1'de bitti, 2026-10-10)

Playtest (Kapı 1) atlandı, Enes'in kararı. Spec: Ana Plan 8. bölüm.

- [x] Cezalar: seviye (Kapalı/Hafif/Cesur), kaybeden çeker, 4 ⚙️ kısıtlama (🤡 2 tur, havuzdan sor, beraberlik rakibin, sorun ifşa) + 🗣️ sesli kartlar
- [x] Bedel (kişi başı 2), Kızgın Çakmak (gizli eşik 8–15 devir)
- [x] Kaos turları (her 5 turda bir, 8 kural), Grup Kararı oylaması, İkiye Katla
- [x] Gizli görevler (8 tür, herkese aynı zorluk), unvanlar (15 tür, en uç 3), oyun özeti, grup kaydı
- [x] İçerik: Aile Dostu 60, Okul 50, Tatil & Yolculuk 50 soru + 25 sesli ceza (alt ajan yazdı, testler geçti; **Enes düzenleyecek**)
- [x] Testler: 154 kural (62'si yeni) + 1.000 rastgele oyunun yarısı S3 açık. 22 S3 kuralı bilerek bozuldu, 22'si de yakalandı. Unity'de 173/173 (Arda'nın 19'u dahil)
- [x] **Bağımsız inceleme (alt ajan):** soru metni sızıntısı yok (600 oyun, 49.000 adım), determinizm temiz. Bulunan 5 hata + 2 risk düzeltildi, her biri önce kırmızı testle gösterildi:
  - Kızgın Çakmak eşiği ısı değişiminden kesin hesaplanıyordu → göstergeye her devirde ±1 gizli gürültü
  - Curcuna'da güme giden soru grup paketine giriyordu → güme olunca listeden de siliniyor
  - Soran Gizli'de A 5 yoldan anlaşılıyordu (önceki C, sayaç, harcanan kart, süre olayı, özet) → A rastgele, sayaç ve kart tur sonuna erteleniyor
  - Özet oyun ortasında herkesin görevini veriyordu → oyun bitmeden gizli kısımlar boş
  - Ters Dünya + Bedel'de asıl kaybeden cezadan kaçıyordu · "beraberlik rakibin" kartı Zar/Tek-Çift'te hiç kalkmıyordu · İkiye Katla iki el sayıyordu · AFK oylayıcılar C'yi cezalandırabiliyordu
- [x] **Online S3 (2026-10-11):** `ayarlar`'a `ceza`/`gorevler`/`kaos`/`paketler` (paket seçimi, dosya adıyla), `durum`'a S3 alanları + oyun sonunda `ozet`, olaylara `ceza`/`kaos`, `oy`/`ikiyeKatla`/`bedelOde`/`karariGec` komutları. Sunucu `Cezalar.csv`'yi okuyor. Biçim `docs/Protokol.md`'de. 27 yeni kara kutu testi (toplam 84), 17 bilerek bozma
- [ ] Online grup paketi: kurucunun cihazındaki "Bizim paket" sunucuya parça parça yüklenmeli (4096 bayt sınırı). Şimdilik online'da yok
- [x] **İçerik hedefi (2026-10-11): 20 görev, 30 unvan** (Ana Plan 8.2 ve 8.9). Liste ve anlamları `Assets/Kurallar/Tekrar.cs`'te (`GorevTuru`, `UnvanTuru`), aşağıdaki kararlar 16–19. Yeni türler enum'ların sonunda: grup kaydı unvanı sayı olarak saklıyor. `GorevUnvanTestleri`: 600 rastgele oyunda görev sonuçları ve unvanlar, testin dışarıdan tuttuğu kayıtla birebir (eski iki kısmi test bunun içine alındı); her görev hem başarıldı hem kaçırıldı, her unvan en az bir kez verildi. 20 bilerek bozma yakalandı
  - Görev: Kolay (6) Kişiyi göster ×2 · 3 farklı kişiyi göster · Kişiye soru sor · 2 soru sor · C olarak ifşa ettir · B olarak güme gönder. Orta (7) 3 mini oyun kazan · B iken hiç ifşa olma · Aynı kişiye 2 soru · 3 farklı kişiye soru · İntikam al · Kişinin sırrını aç · Bedel öde. Zor (7) Kişi seni göstersin · Hiç gösterilme · Kaos turunda kazan · Üst üste 3 kazan · Kişiyi yen · 2 mini oyun oyna hiç ceza çekme · 4 kez gösteril
  - Yeni unvanlar: Sosyal Kelebek · Sadık · Dedikoducu · Gündem · Yenilmez · Zorba · Av · Ayna · Uykucu · Kaçak · Açık Kitap · Eli Boş · Kumarbaz · Bumerang · Soğukkanlı
- [ ] Arda: 12 görev ve 15 unvan için `Metinler.csv` metinleri (türü + hedef + adet geliyor)
- [ ] Playtest 2'de unvan dağılımına bak: ağırlıklar (değer/ortalama, "hep" unvanlarında n/2) rastgele oyunla ayarlandı, gerçek oyunla değil. Testler değerlerin ve sahiplerin doğruluğunu kanıtlıyor, sıralama ağırlığını değil

### S3 sözleşme eklemeleri (Arda'nın ekranları için)

| Ne | Kodda |
|---|---|
| Ayarlar | `CezaSeviyesi` (varsayılan Kapali), `GizliGorevler`, `KaosTurlari` (varsayılan kapalı; "Hızlı başla" açar), `SesliCezalar` (`Assets/Icerik/Cezalar.csv` → `Paket.Ayristir`), `KararSuresiSn` (5), **`TekCihaz` (elden elede true yapın)** |
| Yeni aşamalar | `Oylama` (Grup Kararı), `IkiyeKatla` (kaybeden karar verir, 5 sn), `Bedel` (B karar verir, 5 sn). Sıra: mini oyun → İkiye Katla? → Bedel? → Ifsa/Gume |
| Yeni komutlar | `Oy(kim, ifsa)`, `IkiyeKatla(kim)`, `BedelOde(kim)`, `KarariGec(kim)` ("hayır"), `Ozet()`, `GrupKaydi.Isle(eski, ozet)` |
| Görünüm | `Kaos`, `Isi` (Cesur'da; Soğuk/Ilık/Sıcak/Kızgın), `Gorevin` (sadece sahibine), `KararVeren`, `Oylayanlar`/`OyVerdin`/`OyKullanan`, `SonOylama`, `BedelOdendi`, `TurCezalari` |
| Oyuncu durumu | `BedelHakki`, `Kisitlamalar` (herkese açık: 🤡 vb.), `CezaCekme`, `Yanma`, `BedelOdeme`, `KaosKazanma`, `OynadigiMiniOyun` |
| Olaylar | `KaosBasladi`, `Yandi`, `CezaCekildi`, `IkiyeKatlandi`, `BedelOdendi`, `OyVerildi` |
| Metinler (G2) | Görev, unvan, kısıtlama ve kaos metinleri koda gömülü değil: türü + hedef + adet geliyor, metin `Metinler.csv`'de (G2). Sesli ceza metni kartla birlikte geliyor |

### S3 kararları (Ana Plan'da açık kalan, oturumda onaylanacak)

| # | Karar | Neden |
|---|---|---|
| 1 | Bedel'in 2 kartı kaybedenin normal kartının **yerine** geçer | Plan "B iki ceza çeker" diyor, üstüne mi yerine mi yazmıyor |
| 2 | Hafif'te Bedel 2 ⚙️ kart (sesli kart Cesur'da) | Hafif'te sesli kart yok, plan Bedel'i Hafif'e de koyuyor |
| 3 | Unvan **tekrar edebilir**, grup hafızası seriyi tutar ("üst üste 3. kez Mıknatıs") | Plan 8.5 ile 8.6 çelişiyordu |
| 4 | Eşitlikte unvan verilmez | Tek sahibi olmalı |
| 5 | Oylamada eşitlik ya da hiç oy → güme | Süre dolma kuralı gibi B'nin lehine |
| 6 | Kızgın: ilk 3 devir hep soğuk; ≤2 kaldı kızgın, ≤5 sıcak, yoksa ılık | Yanmadan önce hep "kızgın" görünsün, ama kaçıncı devirde yakacağı belli olmasın |
| 7 | Yanan: 2 kart (büyük ceza) | Plan "büyük ceza" diyor |
| 8 | Soran Gizli'de B, A'yı da seçebilir | A listeden çıkarsa kim olduğu anlaşılır |
| 9 | Ters Dünya'da mini oyunu kaybeden yine ceza çeker (soru ifşa olsa da) | Ceza mini oyuna bağlı, sonuca değil |
| 10 | "Sorun ifşa" cezasında bedel fırsatı yok | Ceza zaten "ifşa olur" diyor |
| 11 | Görev "B olduğun hiçbir soru ifşa olmasın": en az bir kez B olmak şart | Yoksa hiç oynamayan kazanır |
| 12 | Ceza Turu kaosu seviye Kapalı'yken: Temiz modda Hafif, değilse Cesur kartlar | Plan "Temiz modda Hafif" diyor, dışarıda tam ceza |
| 13 | Oylamada çoğunluk yoksa (eşitlik ya da hiç oy) kimse ceza çekmez | AFK oylayıcılar birini cezalandıramasın |
| 14 | Aynı isim iki kez kurulamaz ("aynı" = `Oyuncu.IsimAnahtari`, bkz. 24) | Grup kaydı kişileri isimle eşliyor |
| 15 | Unvan serisi kişinin oynadığı oyunlar üzerinden: bir oyunu kaçıran seriyi kaybetmez | "Üst üste" o kişinin oyunları için |
| 16 | "Bedel öde" ve "hiç ceza çekme" görevleri, "Kaçak" unvanı sadece ceza açıkken | Kapalıyken bedel yok, ceza çekmemek de bedava |
| 17 | Süre dolması (Uykucu): soru sormayan A, C seçmeyen B, hamle yapmayan B/C. Oylama ve karar süreleri sayılmaz | Karar vermemek "hayır" demek, oy vermemek de çekimser kalmak |
| 18 | Görünmez'in puanı ortalama gösterilme / 0,5 (eskiden sabit 1,5) | 30 unvan arasında sabit puan hiç ilk 3'e giremiyordu (600 oyunda 0) |
| 19 | "Hep" unvanları (Kale, Talihsiz, Kaçak) puanı n / 2: n denemenin hepsinde tutmak yazı-tura gibi 2⁻ⁿ | Ortalamayla puanlanınca koşulu tutmayanların 0'ı yüzünden her oyunda bunlar çıkıyordu |
| 20 | Elden elede (`TekCihaz`) mini oyun hamlesi sırayla: ilk hamleden sonra ikincinin süresi baştan başlar. Online'da süre ortak | Arda'nın bulgusu (PR #7): bölünmüş ekranda önce B seçiyor, B 7 sn düşününce C'ye 3 sn kalıyordu |
| 21 | Elden elede saat, telefon dolaşırken (perde, görev, Curcuna yazma, oylama, ceza kartı) **istemcide** durur, kural motoru bilmez | Arda sordu. Motorun saati yok, `Ilerle`'yi kim çağırıyorsa zaman onun. Online'da sunucu hiç durdurmaz |
| 22 | Grup eşleşmesi: bugünkülerin en az 3'ü ve en az yarısı kayıtlı grupta varsa aynı grup | Arda önerdi (istemcide, `GrupDeposu`) |
| 23 | Android paylaşma menüsü: NativeShare eklentisi (`Packages/` değişir) | Arda sordu, **Enes karar verecek**. Şimdilik kart dosyaya kaydediliyor |
| 24 | İsim karşılaştırmasının tek tanımı `Oyuncu.IsimAnahtari`: boşluk, büyük/küçük harf ve I/İ/ı/i farkı gözetilmez ("İrem" = "irem", "ILKER" = "ılker"). `Kur`, `GrupKaydi.Isle`, sunucu ve istemci (`GrupDeposu`, kurulum) hep bunu kullanır. **Yapıldı (2026-10-11, Enes'in isteğiyle)** | 3 yerde 3 farklı karşılaştırma vardı: istemci aynı kişi sayarken grup kaydı iki üye açabiliyordu |
| 25 | Temiz filtresi kelimeye başından bakar: kara listedeki kökle başlıyorsa ya da kısaltmanın kendisiyse ("am", "aq", "mk") yakalar, izin listesi kurtarır. İzin listesi ı'yı korur ("sık", "şık" masum; "sik" değil), Türkçe klavyesiz yazılan masumlar da listede ("sikildim") | Plan 6.4'ün adımlarında ı → i sadeleşince "sıkıldım", "şık", "sıkıcı" yakalanıyordu. Kelime ortası ve bitişik yazılan cümle yakalanmaz (plan zaten "hiçbir filtre yakalamaz" diyor) |
| 26 | Temiz ihlali: soru ve sohbet aynı sayaçta, odada isme göre (lobide çıkıp aynı isimle giren sıfırlanmaz), 3.'de atılır. Kurucu da atılabilir, kuruculuk devredilir. İsim filtrelenmez | Plan "3. ihlalde at" diyor, neyin sayıldığını yazmıyor |
| 27 | Sohbet saniyede 1, arka arkaya en fazla 2 | İstemci saniyede bir yollasa da ağ gecikmesi iki mesajı yaklaştırabilir |
| 28 | Sunucu sohbeti saklamaz, kopup dönen eskileri görmez. Rapor gelince son 50 mesaj (S6) ayrı iş | Gizlilik metni "mesaj saklanmıyor" diyor |
| 29 | **Öneri:** yayın adresi (`wss://<alan-adı>/ws`) istemcide tek bir sabitte (ya da Resources'ta bir ayar dosyasında), "Sunucu" alanı sadece editörde ve geliştirme build'inde görünür | Arda sordu. Oyuncu adres girmesin; geliştirirken yerel sunucu yine denenebilsin. Adres gizli değil, build'de durması sorun değil |
| 30 | Grup paketi online'da kurucunun cihazındaki kayıttan yüklenir, `"@bizim"` adıyla seçilir, Temiz'de kullanılmaz, sunucuda sadece oda açıkken bellekte | Kayıt cihazda (Ana Plan 8.5, hesap yok). Elden eledeki kuralın aynısı |

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
| 9 | — | `Hamle.Erken` (bool): Reaksiyonda erken basış | Kuralın tek tanımı. İstemci "Erken bastın" yazısı için eşiği kendisi karşılaştırmasın (2026-10-10, istemci de buna geçirildi) |

| 10 | `void OyuncuKoptu/Dondu/Cikti(kim)` | `Sonuc` döner (bilinmeyen oyuncu `GecersizGirdi`) + yeni `Sonuc Buradayim(kim)`: AFK'yı kaldırır | Hata fırlatma kuralı; "ekrana dokununca geri gelir" (Ana Plan 6.1) |
| 11 | — | `OyuncuDurumu.Afk`, `OyuncuDurumu.Cikti`; olaylar `SiraAtlandi`, `Afk`, `AfkBitti`, `OyuncuCikti` | Ekranda "AFK", "çıktı" etiketleri, "sıra atlandı" duyurusu |
| 12 | — | `SoruSor`/`CevapSec` görünümdeki `Secilebilir` listesine göre denetler: kopuk/AFK/çıkmış seçilemez (aktif kimse yoksa seçilebilir) | Görünüm ile kural tek yerde. Elden elede kimse pasif olmadığı için değişen bir şey yok |
| 13 | — | `Kur(ayarlar, oyuncular, Random rastgele, out oyun)` | Online'da kriptografik rastgelelik (`docs/Guvenlik.md`). Tohumlu `Kur` aynen duruyor |
| 14 | — | `Sonuc.Uygunsuz` (enum sonuna) + `TemizFiltre.Uygunsuz(metin)`: Temiz modda `SoruSor` filtreye takılan soruyu göndermez | Ana Plan 6.4. Filtre tek yerde: elden ele ve sunucu aynısını kullanır |

Sohbet, atma ve geç gelen (S5) sunucuda, kural motoruna girmedi. S3'ün eklemeleri yukarıdaki "S3 sözleşme eklemeleri" tablosunda.

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
