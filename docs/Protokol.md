# Ağ Protokolü (online mod)

Durum: **taslak (S4 iskeleti + S3 tekrar oynatma + S4 sınırlar + S5 kopma/AFK).** Sözleşme oturumunda onaylanacak. Kaynak: `docs/Ana Plan.md` 5.2 ve 8.
Sunucu: `Sunucu/Cakmak.Sunucu` · İstemci: Unity (G2).

Henüz **yok** (S5'te gelecek): sohbet + DM, atma, geç gelen bekleme odası, Temiz filtresi. Kopma ve AFK var (aşağıda "Bağlantı koparsa").
Online'da **grup paketi yok** (sonra): yüzlerce soru 4096 baytlık mesaja sığmıyor, parça parça yükleme gerekiyor. Grup hafızası cihazda, `ozet`'ten yazılır.

---

## Bağlantı

- Adres: `ws://<sunucu>/ws` (yayında `wss://`).
- Her WebSocket **metin** mesajı tek bir JSON nesnesi. UTF-8. İstemciden gelen mesaj en fazla **4096 bayt**. Daha büyüğü bağlantıyı kapatır (kapanış kodu 1009). Sunucudan gidenin sınırı yok: oyun sonu `durum`'u (özetle) birkaç on KB olabilir.
- İkili (binary) çerçeve ve aynı alanı iki kez yazan JSON → `GECERSIZ_MESAJ`, bağlantı açık kalır.
- Sunucu 10 sn'de bir ping atar. 20 sn cevap gelmezse bağlantı kopmuş sayılır.
- Aynı IP'den aynı anda açık bağlantı sınırlı (aşağıda "Sınırlar"). Aşılırsa WebSocket açılmaz, HTTP **429** döner.
- `Origin` başlığı olan istek (tarayıcı) reddedilir, HTTP **403**. Uygulama bu başlığı yollamamalı.
- Bağlantıyı oda kurarken/katılırken aç: bağlanıp 120 sn içinde odaya girmeyen bağlantı kesilir.
- Her mesajda `t` alanı tipi söyler.
- Oyuncu kimliği: `"p1"` … `"p10"`. Odayı kuran `p1`, sonra katılış sırasıyla.
- Oda kodu: 5 büyük harf, `I` ve `O` yok (`0/O`, `1/I` karışmasın). Örnek: `"KXTPM"`.

## İstemci → sunucu

| `t` | Alanlar | Kim, ne zaman |
|---|---|---|
| `odaKur` | `isim` | Herkes. Yeni oda açar, gönderen `p1` ve kurucu olur |
| `katil` | `oda`, `isim`, `anahtar` (ilk girişte `null`) | Oda lobideyse yeni oyuncu olarak girer. `anahtar` doğruysa oyun başlamış olsa da **aynı kimlikle geri döner** |
| `ayarlar` | `mod`, `miniOyun`, `turSayisi`, `soruSn`, `cevapSn`, `hamleSn`, `ceza`, `gorevler`, `kaos`, `paketler` (hepsi isteğe bağlı) | Kurucu, lobide. Sadece gönderilen alanlar değişir |
| `baslat` | — | Kurucu, lobide, en az 4 oyuncu |
| `komut` | `tur`, `komut`, + komutun alanları (aşağıda) | Oyunda |
| `bitir` | — | Kurucu, oyunda. Oyunu hemen bitirir |
| `buradayim` | — | Oyunda, herkes. Ekrana dokununca gönder: AFK etiketi kalkar. AFK değilse bir şey olmaz. Her `komut` da aynı işi görür |

**Metin temizliği** (isim, soru): görünmez ve yön karakterleri atılır, her türlü boşluk tek boşluk olur, baş/son boşluk silinir (`TemizMetin`). Temizlenince boş kalan isim `ISIM_GECERSIZ`, soru `GECERSIZ_GIRDI`. Ekranda hep temizlenmiş hali görünür.

**İsim:** temizlendikten sonra 1–20 karakter. Aynı odada aynı isim iki kez olamaz: büyük/küçük harf ve I/İ/ı/i farkı gözetilmez ("İrem" = "irem", "ILKER" = "ılker"). Tanım tek yerde: `Oyuncu.IsimAnahtari`. Yeni girişte `isim` yoksa `ISIM_GECERSIZ`. Anahtarla dönüşte `isim` yok sayılır.

**Değerler:**
- `mod`: `"normal"` · `"temiz"` · `"curcuna"`
- `miniOyun`: `"tkm"` · `"tekCift"` · `"zar"` · `"karisik"`. **`"reaksiyon"` online'da yok** (`GECERSIZ_GIRDI`): süre istemciden geliyor ve telefonların dokunma gecikmesi farklı. Sadece elden ele.
- `turSayisi`: `0` = sınırsız, `1–100`
- `soruSn`, `cevapSn`, `hamleSn`: `5–300`
- `ceza`: `"kapali"` (varsayılan) · `"hafif"` · `"cesur"`. Cesur'da Kızgın Çakmak da açık. Temiz modda `"cesur"` olamaz: sonuç Temiz + Cesur olacaksa (hangi sırayla gönderilirse gönderilsin) `GECERSIZ_GIRDI`, önce cezayı düşür
- `gorevler`, `kaos`: `true` / `false` (varsayılan `false`). "Hızlı başla" istemcide, bu alanları kendisi gönderir
- `paketler`: paket adları dizisi, dosya adı uzantısız: `["Genel", "Okul"]`. Varsayılan: sunucudaki bütün paketler. Boş dizi, sunucuda olmayan ya da iki kez yazılan ad `GECERSIZ_GIRDI`. Öneri çek, Curcuna'nın boşlukları ve Kör Soru sadece seçilenlerden çeker

### Komutlar

`tur`: durumdaki `tur` ile aynı olmalı. Farklıysa komut yapılmaz, `ESKI_TUR` döner. Soru toplama aşamasında `tur` = `0`.

| `komut` | Alanlar | Kural motorunda |
|---|---|---|
| `havuzaEkle` | `metin` | `HavuzaSoruEkle` |
| `oneriCek` | — | `OneriCek` |
| `soruyuDegistir` | — | `SoruyuDegistir` |
| `soruSor` | `hedef`, `metin` | `SoruSor`. Curcuna'da `metin` yok sayılır. Diğer modlarda `metin` zorunlu, yoksa `GECERSIZ_GIRDI` (online'da fısıltı yok) |
| `cevapSec` | `hedef` | `CevapSec` |
| `hamle` | `hamle` nesnesi (aşağıda) | `MiniOyunHamlesi` |
| `oy` | `ifsa` (`true` = ifşa, `false` = güme; zorunlu) | `Oy`. Sadece `oylama` aşamasında, `oylayanlar` içindekiler, bir kez |
| `ikiyeKatla` | — | `IkiyeKatla`. Sadece `ikiyeKatla` aşamasında, `kararVeren` |
| `bedelOde` | — | `BedelOde`. Sadece `bedel` aşamasında, `kararVeren` (B) |
| `karariGec` | — | `KarariGec`: "hayır". `ikiyeKatla` ya da `bedel` aşamasında, `kararVeren`. Süre dolması da aynı sonucu verir |

**Hamle nesnesi:**

```json
{ "oyun": "tkm", "secim": "tas" }                 // "tas" | "kagit" | "makas"
{ "oyun": "tekCift", "parmak": 3, "tek": true }   // "tek": sadece B'nin tahmini, yoksa false (çift)
{ "oyun": "zar" }
```

## Sunucu → istemci

| `t` | Ne zaman |
|---|---|
| `hosgeldin` | `odaKur` / `katil` başarılı olunca, sadece gönderene |
| `durum` | Her değişiklikte, **odadaki herkese, herkese kendine özel** |
| `olay` | Kural motoru bir olay yaydığında, herkese |
| `hata` | Bir istek yapılamadığında, sadece gönderene |

### `hosgeldin`

```json
{ "t": "hosgeldin", "sen": "p2", "anahtar": "32-karakterlik-rastgele-dize", "oda": "KXTPM" }
```

`anahtar`ı sakla. Bağlantı koparsa `katil` ile aynı `anahtar`ı gönder, aynı kimlikle ve **aynı anahtarla** dönersin. Kimlik hâlâ başka bir cihazda açıksa o eski bağlantı kesilir.

### `durum` — lobide

```json
{ "t": "durum", "surum": 3, "oda": "KXTPM", "asama": "lobi", "kurucu": "p1",
  "oyuncular": [ { "id": "p1", "isim": "Enes", "renk": 0, "bagli": true } ],
  "ayarlar": { "mod": "normal", "miniOyun": "tkm", "turSayisi": 15, "soruSn": 60, "cevapSn": 20, "hamleSn": 10,
               "ceza": "kapali", "gorevler": false, "kaos": false, "paketler": ["AileDostu", "Genel", "Okul", "TatilYolculuk"] } }
```

### `durum` — oyunda

```json
{ "t": "durum", "surum": 57, "oda": "KXTPM", "asama": "miniOyun", "tur": 12, "toplamTur": 15, "kalanMs": 8000,
  "kurucu": "p1", "a": "p1", "b": "p2", "c": "p4",
  "oyuncular": [ { "id": "p1", "isim": "Enes", "renk": 0, "bagli": true,
                   "havuzaEkledigi": 0, "gosterilme": 1, "soruAlma": 2, "soruSorma": 3,
                   "ifsaEttirme": 0, "saklama": 1, "miniOyunKazanma": 1,
                   "oynadigiMiniOyun": 2, "cezaCekme": 1, "yanma": 0, "bedelOdeme": 0, "bedelHakki": 2, "kaosKazanma": 0,
                   "kisitlamalar": [ { "tur": "palyaco", "metin": null, "kalan": 2 } ],
                   "afk": false, "cikti": false } ],
  "secilebilir": [],
  "miniOyun": "tkm", "bHamleYapti": true, "cHamleYapti": false, "beraberlik": 0,
  "sonMiniOyun": null,
  "kaos": "yok", "isi": "ilik", "kararVeren": null,
  "oylayanlar": [], "oyKullanan": 0, "sonOylama": null, "bedelOdendi": false, "turCezalari": [],
  "sana": { "soru": "Grupta sırrını en kolay kim açık eder?", "fisilti": false,
            "atananSoru": null, "degistirmeHakki": false, "oneri": null,
            "hamlen": { "oyun": "tkm", "secim": "tas" },
            "gorevin": { "tur": "kisiyiGoster", "hedef": "p3", "adet": 2, "zorluk": "orta" }, "oyVerdin": false },
  "ifsa": null,
  "ozet": null,
  "bekleme": null }
```

- `asama`: `"soruToplama"` · `"soruSorma"` · `"cevapSecme"` · `"miniOyun"` · `"oylama"` · `"ikiyeKatla"` · `"bedel"` · `"ifsa"` · `"gume"` · `"oyunSonu"`.
  Sıra: mini oyun (Grup Kararı'nda onun yerine oylama) → İkiye Katla? → Bedel? → ifşa/güme.
- `a`, `b`, `c`: o an boşsa `null`. **Soran Gizli** kaos turunda `a` sadece A'nın kendi mesajında dolu, olaylarda da `null`.
- `secilebilir`: sadece seçim yapacak kişiye dolu (soru sormada A'ya, cevap seçmede B'ye). Diğerlerine `[]`.
- `sana`: **sadece o oyuncunun mesajında** dolu alanlar. Soru metni sadece B'nin mesajında.
- `sonMiniOyun`: `ifsa` ve `gume` aşamasında dolu:
  `{ "oyun": "tkm", "bHamle": {…}, "cHamle": {…}, "bZar": 0, "cZar": 0, "zarlaKarar": false, "sureDoldu": false, "cKazandi": true }`
- `ifsa`: `ifsa` aşamasında `{ "soru": "…", "fisilti": false }`, yoksa `null`.
- `kalanMs`: geri sayımı istemci kendisi yürütür. Sunucu sadece değişiklikte yeni durum yollar. `bekleme` doluyken oyun durur, geri sayımı durdur.
- `oyuncular[].bagli`: bağlantısı açık mı. `false` = "Bağlanıyor…" (yeri 60 sn tutuluyor).
- `oyuncular[].afk`: üst üste 2 kez süresini doldurdu. Sırası gelince atlanır, seçilemez. Kendisi `buradayim` (ya da herhangi bir komut) yollayınca kalkar. Ekranda kendi AFK'nı görünce "Buradayım" düğmesi göster.
- `oyuncular[].cikti`: oyundan çıktı (60 sn'de dönmedi). Bir daha seçilmez, sırası gelmez, sayaçları özette kalır. Anahtarla dönerse izler.
- `bekleme`: bağlı oyuncu 4'ün altına düştü, oyun durdu: `{ "kalanMs": 95000 }` (bu sürede kimse dönmezse oyun biter). Yoksa `null`.
- `secilebilir`: kopuk, AFK ve çıkmış oyuncular listede yok (aktif kimse kalmadıysa yine de dolu gelir, oyun takılmaz).

**S3 alanları** (kurallar: Ana Plan 8, kararlar: `tasks/g1-todo.md`):
- `kaos`: bu turun kaos kuralı, yoksa `"yok"`. `"tersDunya"` · `"grupKarari"` · `"korSoru"` · `"soranGizli"` · `"aniOlum"` · `"cezaTuru"` · `"yonDegisti"` · `"ikiyeKatla"`. Kaos açıksa her 5 turda bir.
- `isi`: Kızgın Çakmak (sadece `ceza: "cesur"`), yoksa `null`. `"soguk"` · `"ilik"` · `"sicak"` · `"kizgin"`. Eşik hiçbir mesajda yok.
- `kararVeren`: `ikiyeKatla` ve `bedel` aşamasında karar verecek oyuncu, yoksa `null`.
- `oylayanlar`, `oyKullanan`: `oylama` aşamasında oy verebilenler (A ve B hariç herkes) ve kaçının verdiği. Kimin ne oy verdiği hiçbir mesajda yok.
- `sonOylama`: oylamalı turun `ifsa`/`gume` aşamasında `{ "ifsa": 2, "gume": 1 }`, yoksa `null`. Eşitlik ya da hiç oy → güme.
- `bedelOdendi`: `gume` aşamasında, soru bedelle gizli kaldıysa `true`.
- `turCezalari`: bu turda çekilen cezalar, tur başında boşalır: `[ { "kim": "p2", "ceza": {…}, "neden": "kaybetti" } ]`. `neden`: `"kaybetti"` · `"bedel"` · `"yandi"`.
- **Ceza nesnesi:** `{ "tur": "sesli", "metin": "…", "kalan": 0 }`. `tur`: `"sesli"` (metin dolu, hemen yapılır) · `"palyaco"` (`kalan` tur 🤡) · `"havuzdanSor"` · `"beraberlikRakibe"` · `"sorunIfsa"` (bir kez işler, `kalan` 1). Metinler (sesli hariç) istemcide.
- `oyuncular[].kisitlamalar`: oyuncunun üstündeki ⚙️ cezalar (herkese açık). `bedelHakki`: kalan bedel (başta 2).
- `sonMiniOyun.katlandi`: İkiye Katla kabul edildi, bu ikinci el.
- `sana.gorevin`: gizli görev, sadece sahibine, görevler kapalıysa `null`. `{ "tur", "hedef" (oyuncu ya da null), "adet", "zorluk" }`. `tur`: `"kisiyiGoster"` · `"farkliKisiGoster"` · `"miniOyunKazan"` · `"hicIfsaOlma"` · `"ayniKisiyeSor"` · `"seniGostersin"` · `"hicGosterilme"` · `"kaosTurundaKazan"` · `"kisiyeSor"` · `"soruSor"` · `"ifsaEttir"` · `"gumeyeGonder"` · `"farkliKisiyeSor"` · `"intikamAl"` · `"sirriniAc"` · `"bedelOde"` · `"ustUsteKazan"` · `"hedefiYen"` · `"hicCezaCekme"` · `"cokGosterilme"` (anlamları ve zorlukları `Assets/Kurallar/Tekrar.cs`, `GorevTuru`). `zorluk`: `"kolay"` · `"orta"` · `"zor"`. Metni istemci yazar.
- `sana.oyVerdin`: `oylama` aşamasında sen oy verdin mi.
- `ozet`: sadece `oyunSonu`'nda, yoksa `null`. Oyun ortasında gelmez (görevler ve Soran Gizli'nin A'sı sır):
  ```json
  { "oynananTur": 15,
    "unvanlar": [ { "tur": "miknatis", "kim": "p3", "deger": 6 } ],
    "gorevler": [ { "kim": "p1", "gorev": { "tur": "kisiyiGoster", "hedef": "p3", "adet": 2, "zorluk": "orta" }, "basarili": true } ],
    "ifsaOlanlar": [ { "soru": "…", "a": "p1", "b": "p2", "c": "p4" } ],
    "curcunaSorulari": [ "…" ] }
  ```
  Unvan `tur`: `"miknatis"` · `"gammaz"` · `"sirKupu"` · `"hedefTahtasi"` · `"soruMakinesi"` · `"sansli"` · `"takintili"` · `"intikamci"` · `"atesleOynayan"` · `"bedelci"` · `"cezakes"` · `"kaosCanavari"` · `"kale"` · `"gorunmez"` · `"talihsiz"` · `"sosyalKelebek"` · `"sadik"` · `"dedikoducu"` · `"gundem"` · `"yenilmez"` · `"zorba"` · `"av"` · `"ayna"` · `"uykucu"` · `"kacak"` · `"acikKitap"` · `"eliBos"` · `"kumarbaz"` · `"bumerang"` · `"sogukkanli"` (anlamları `UnvanTuru`'nda). En uç 3 unvan, eşitlikte verilmez.
  **Güme giden soru özette yoktur.** Sayaçlar `oyuncular`'da. Grup hafızası (`GrupKaydi.Isle`) cihazda bu bilgilerden yazılır.

### `olay`

```json
{ "t": "olay", "olay": "ifsa", "a": "p1", "b": "p2", "c": "p4", "kim": null, "soru": "…", "ceza": null, "kaos": null }
```

`olay`: `"turBasladi"` · `"soruSoruldu"` · `"cevapSecildi"` · `"berabere"` · `"ifsa"` · `"gume"` · `"sureDoldu"` · `"oyunBitti"` ·
`"kaosBasladi"` · `"yandi"` · `"cezaCekildi"` · `"ikiyeKatlandi"` · `"bedelOdendi"` · `"oyVerildi"` ·
`"siraAtlandi"` · `"afk"` · `"afkBitti"` · `"oyuncuCikti"`.
`soru` sadece `ifsa` olayında dolu. `ceza` sadece `cezaCekildi`'de (ceza nesnesi), `kaos` sadece `kaosBasladi`'da (kuralın adı). Animasyon ve ses için. Ekran her zaman `durum`'a göre çizilir.

### `hata`

```json
{ "t": "hata", "kod": "SIRA_SENDE_DEGIL" }
```

| Kod | Ne |
|---|---|
| `GECERSIZ_MESAJ` | JSON bozuk, `t` yok ya da bilinmiyor, alan eksik/yanlış tipte |
| `ODA_YOK` | Bu kodla oda yok |
| `ODA_DOLU` | Odada 10 kişi var |
| `OYUN_BASLADI` | Oyun başlamış, yeni oyuncu giremez (anahtarla dönmek serbest) |
| `ISIM_GECERSIZ` | İsim boş, 20 karakterden uzun ya da odada aynısı var |
| `ODADA_DEGILSIN` | Odaya girmeden oda komutu gönderdin |
| `ZATEN_ODADASIN` | Bu bağlantı zaten bir odada |
| `KURUCU_DEGILSIN` | Sadece kurucu yapabilir |
| `OYUN_YOK` | Oyun başlamadan oyun komutu |
| `ESKI_TUR` | Komuttaki `tur` güncel değil |
| `COK_HIZLI` | Mesaj sınırı ya da `odaKur`/`katil` sınırı aşıldı. Mesaj **hiç işlenmedi**, biraz bekleyip tekrar gönder |
| `SUNUCU_DOLU` | Sunucudaki oda sayısı sınırda, yeni oda açılamadı. Sonra tekrar dene |
| `SIRA_SENDE_DEGIL` · `SECILEMEZ` · `YANLIS_ASAMA` · `GECERSIZ_GIRDI` · `HAK_KALMADI` · `HAVUZ_BOS` · `OYUNCU_SAYISI_GECERSIZ` | Kural motorunun `Sonuc` değerleri |

### Alanların anlamı

- `surum`: odanın sayacı, her `durum`'da bir artar. Eski sürümlü mesajı at (Ana Plan 5.2, kural 3). Geri dönen oyuncu da kaldığı sayıdan devam eder.
- `renk`: 0–9, oyuncu numarasından (`p1` → 0). Arayüz kendi renk listesinde bu sıraya bakar.
- `olay.kim`: `sureDoldu` (süresi dolan), `yandi` (çakmak elinde yanan), `cezaCekildi` (çeken), `ikiyeKatlandi` (bir el daha isteyen), `bedelOdendi` (ödeyen B), `oyVerildi` (oy veren; oyun ne olduğu yok), `siraAtlandi` (sırası gelen ama kopuk/AFK/çıkmış olan; sıra rastgele birine geçti), `afk` / `afkBitti` (AFK olan / dönen), `oyuncuCikti` (oyundan çıkan). Diğerlerinde `null`.
- Paketler ve sesli cezalar: sunucu `Assets/Icerik` altındaki `.txt` paketlerini ve `Cezalar.csv`'yi açılışta okur.
- Sabit süreler (şimdilik ayarlanamaz): ifşa/güme ekranı 6 sn, Curcuna soru toplama 90 sn, İkiye Katla/Bedel kararı 5 sn. Oylama `hamleSn` kadar sürer.

### Hata sırası

Bir mesaj birden çok kuralı çiğniyorsa ilk tutan döner:
`COK_HIZLI` (mesaj sınırı, mesaj okunmadan) → JSON bozuk ya da `t` yok/bilinmiyor (`GECERSIZ_MESAJ`) → `COK_HIZLI` (`odaKur`/`katil` sınırı) → `ODADA_DEGILSIN` → `KURUCU_DEGILSIN` → `OYUN_BASLADI` / `OYUN_YOK` → eksik ya da yanlış tipte alan (`GECERSIZ_MESAJ`) → `ESKI_TUR` → `GECERSIZ_GIRDI` ve kural motoru hataları.
`komut` mesajında `ESKI_TUR`'dan önce sadece `tur` ve `komut` alanlarına bakılır. Komutun kendi alanları (`hedef`, `metin`, `hamle`, `ifsa`) ve bilinmeyen `komut` adı `ESKI_TUR`'dan sonra denetlenir (`GECERSIZ_MESAJ`).

- `ayarlar`: bilinmeyen `mod`/`miniOyun`/`ceza` adı ya da yanlış tip (`paketler` dizi değil ya da içinde metin olmayan var) `GECERSIZ_MESAJ`. Aralık dışı sayı, Temiz + Cesur, geçersiz paket listesi `GECERSIZ_GIRDI`. Oyun başladıysa `OYUN_BASLADI`.
- `oy` komutunda `ifsa` yoksa ya da `true`/`false` değilse `GECERSIZ_MESAJ`.
- `baslat` oyun başladıktan sonra: `OYUN_BASLADI`.
- Bilinmeyen `komut` ya da hamle `oyun` adı: `GECERSIZ_MESAJ`.

## Sınırlar

Sunucuyu kötüye kullanmaya karşı. Normal oyunda hiçbirine takılınmaz.

| Sınır | Varsayılan | Aşılınca | Ayar adı |
|---|---|---|---|
| IP başına aynı anda açık bağlantı | 32 | HTTP 429, WebSocket açılmaz | `IpBasinaBaglanti` |
| IP başına `odaKur` + `katil` | dakikada 30 (her 2 sn'de bir hak dolar, en fazla 30 birikir) | `COK_HIZLI` | `IpBasinaGirisDakikada` |
| Bağlantı başına mesaj | saniyede 10, arka arkaya en fazla 20 | `COK_HIZLI` | `MesajSaniyede`, `MesajPatlama` |
| Başlatılmayan lobi | 30 dk | Oda kapanır, bağlantılar kesilir. Anahtarla dönüş `ODA_YOK` | `LobiSn` |
| Odaya girmeyen bağlantı | 120 sn | Bağlantı kesilir | `OdasizSn` |
| Sunucudaki toplam oda | 2000 | `odaKur` → `SUNUCU_DOLU` | `EnFazlaOda` |
| Sunucudaki toplam WebSocket | 5000 | Yeni bağlantı açılmaz | `EnFazlaBaglanti` |

- **IP:** sunucunun önündeki Caddy'nin `X-Forwarded-For` başlığından. Bu başlığa sadece aynı makineden (localhost) gelen istekte güvenilir. IPv6'da bir /64 bloğu tek IP sayılır (bir ev ya da telefon bütün bloğu alır).
- Aynı Wi-Fi'daki herkes aynı IP'dir: 10 kişilik grup ve yeniden bağlananlar sınırlara sığar.
- `COK_HIZLI` alan istemci durmazsa cevaplar gönderim kuyruğunu doldurur ve bağlantı kesilir.
- Ayarlar sunucuda: `appsettings.json` içinde `"Sinirlar"` bölümü ya da ortam değişkeni (`Sinirlar__IpBasinaBaglanti=50`).

## Bağlantı koparsa (Ana Plan 6.1)

- **Lobide:** oyuncu odadan çıkar. Kurucu çıktıysa kuruculuk sıradaki oyuncuya geçer. Oda boşalırsa silinir.
- **Oyunda kopan:** `bagli: false`, yeri **60 sn** tutulur. Süreler işler (kimse beklemez); bu sürede B/C seçilemez, sırası gelirse atlanır (`siraAtlandi`). `anahtar` ile dönünce kaldığı yerden devam eder.
- **60 sn'de dönmeyen:** oyundan çıkar (`oyuncuCikti`, `cikti: true`). Elindeki iş hemen biter: sormadan çıkan A'nın turu yanar, seçmeden çıkan B'nin sorusu güme gider, mini oyundan kaçan kaybeder, oy vermeden çıkan oylamadan düşer, kararı bekleyen çıkarsa "hayır" sayılır. (Soran Gizli turunda gizli A çıkarsa süresi dolar: hemen bitseydi kim olduğu anlaşılırdı.) Anahtarla dönerse izler, oynamaz.
- **AFK:** üst üste 2 kez süresini dolduran (soru sormadı, C seçmedi, hamle yapmadı) `afk: true` olur: sırası gelince atlanır, seçilemez. `buradayim` ya da herhangi bir `komut` yollayınca kalkar. (Soran Gizli turunda A'nın süresi sayılmaz: AFK olması gizli A'yı ele verirdi.)
- **Bağlı oyuncu 4'ün altına düşerse:** oyun durur (süreler ve kopukların 60 sn'si işlemez), `bekleme` dolu gelir. **2 dk** içinde biri dönüp 4'e çıkılırsa sürer, dönen olmazsa oyun biter (`oyunSonu`).
- **Kurucu koparsa** kuruculuk ilk bağlı oyuncuya geçer, geri dönünce geri almaz.
- **Herkes koparsa** oda hemen silinmez: 2 dk içinde dönen anahtarla girer. Kimse dönmezse silinir, sonra anahtarla dönüş `ODA_YOK`.
- Süreler sunucu ayarı: `KopukSn` (60), `AzOyuncuSn` (120).
- **Lobi 30 dk içinde başlatılmazsa** oda kapanır, bağlantılar kesilir. Anahtarla dönüş `ODA_YOK` alır. Başlamış oyunu bu süre etkilemez.

## Ana Plan 5.2'den farklar (oturumda onaylanacak)

| 5.2'de | Burada | Neden |
|---|---|---|
| `ayarlar` içinde `"tur": 20` | `"turSayisi": 20` | `komut` içindeki `tur` (şu anki tur) ile karışmasın |
| — | `bitir` mesajı | "Sınırsız oyunu kurucu bitirir" |
| — | `hamle` nesnesinin biçimi | 5.2'de yoktu |
| — | Lobi `durum`'u | 5.2 sadece oyun içini gösteriyordu |
| "Ayarlara paketler eklenir" | `paketler`: dosya adları (`"Genel"`) | İstemcinin paket listesi de dosya adını kullanıyor (`TextAsset.name`) |
| "`Kur` grup paketinin sorularını da alır" | Online'da henüz yok | 4096 bayt sınırı, parça parça yükleme ayrı iş |
