# Ağ Protokolü (online mod)

Durum: **taslak (S4 iskeleti + S3 tekrar oynatma + S4 sınırlar + S5 kopma/AFK, bekleme odası, atma, sohbet + DM, Temiz filtresi, grup paketi; yeni oyunlardan Köstebek, Rus Ruleti, Takım Düellosu, Casus, Çakmak Gecesi).** Sözleşme oturumunda onaylanacak. Kaynak: `docs/Ana Plan.md` 5.2, 6.4 ve 8.
Sunucu: `Sunucu/Cakmak.Sunucu` · İstemci: Unity (G2).

Grup hafızası cihazda, `ozet`'ten yazılır. Grubun kendi paketini kurucu lobide parça parça yükler (aşağıda "Grup paketi").

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
| `odaKur` | `isim`, `surum` | Herkes. Yeni oda açar, gönderen `p1` ve kurucu olur |
| `katil` | `oda`, `isim`, `anahtar` (ilk girişte `null`), `surum` | Oda lobideyse yeni oyuncu olarak girer. Oyun sürüyorsa **bekleme odasına** girer, sonraki oyunda oynar. `anahtar` doğruysa oyun başlamış olsa da **aynı kimlikle geri döner**. Oda kapasitesi (10) bekleyenler dahil |
| `ayarlar` | `oyun`, `can`, `takimlar`, `mod`, `miniOyun`, `turSayisi`, `soruSn`, `cevapSn`, `hamleSn`, `ceza`, `gorevler`, `kaos`, `paketler` (hepsi isteğe bağlı) | Kurucu, lobide. Sadece gönderilen alanlar değişir |
| `baslat` | — | Kurucu, lobide, en az 4 oyuncu |
| `komut` | `tur`, `komut`, + komutun alanları (aşağıda) | Oyunda |
| `bitir` | — | Kurucu, oyunda. Oyunu hemen bitirir. **Köstebek ve Casus'ta** önce son oylama başlar (kurucu köstebek ya da casussa oyunu bitirip kazanamasın), oylama sürerken ikinci `bitir` `YANLIS_ASAMA`. Çakmak Gecesi'nde oyun hemen biter, kazanan yok |
| `buradayim` | — | Oyunda, herkes. Ekrana dokununca gönder: AFK etiketi kalkar. AFK değilse bir şey olmaz. Her `komut` da aynı işi görür |
| `at` | `kim` | Kurucu; lobide, oyunda ya da bekleme odasında. Kendini atamaz. Atılan `ATILDIN` (`neden: "kurucu"`) alır, bağlantısı kapanır; bu odaya ne anahtarıyla ne aynı isimle dönebilir (hesap yok: başka bir isimle girebilir, rapor + engelleme S6). Oyundaysa oyundan çıkar, sayaçları özette kalır. Herkese `olay: "atildi"` |
| `tekrarOyna` | — | Kurucu, `oyunSonu`'nda. Oda lobiye döner: bağlı olmayanlar düşer, bekleme odasındakiler oyuncu olur, ayarlar ve atılanların yasağı kalır |
| `sohbet` | `kime` (`null` ya da yok = genel, `"p3"` = DM, `"takim"` / `"katil"` / `"oluler"` = kanal), `metin` | Odadaki herkes, her an (lobi, oyun, bekleme odası, oyun sonu); Çakmak Gecesi'nde kısıtlı. Aşağıda "Sohbet" |
| `grupPaketi` | `sorular` (metin dizisi), `ilk` (isteğe bağlı, varsayılan `false`) | Kurucu, lobide. Grubun kendi paketini parça parça yükler. Aşağıda "Grup paketi" |

**Sürüm:** `odaKur` ve `katil`'de istemcinin protokol sürümü, `surum` (bugün `1`). Sunucunun kabul ettiği en eskiden küçükse `GUNCELLE` (oda açılmaz, bağlantı açık kalır): "Oyunu güncelle" ekranı göster. Uyumsuz bir protokol değişikliğinde artar. Şimdilik yollamayan da kabul ediliyor; mağaza build'i mutlaka yollasın.

**Metin temizliği** (isim, soru, sohbet): görünmez ve yön karakterleri atılır, her türlü boşluk tek boşluk olur, baş/son boşluk silinir (`TemizMetin`). Temizlenince boş kalan isim `ISIM_GECERSIZ`, soru ve sohbet `GECERSIZ_GIRDI`. Ekranda hep temizlenmiş hali görünür.

**İsim:** temizlendikten sonra 1–20 karakter. Aynı odada aynı isim iki kez olamaz: büyük/küçük harf ve I/İ/ı/i farkı gözetilmez ("İrem" = "irem", "ILKER" = "ılker"). Tanım tek yerde: `Oyuncu.IsimAnahtari`. Yeni girişte `isim` yoksa `ISIM_GECERSIZ`. Anahtar tanınırsa `isim` yok sayılır.

**Yeniden bağlanırken `anahtar` ile birlikte aynı `isim`'i de yolla.** Oyunda yerin tutulur, anahtar tanınır. Lobide ya da bekleme odasında kopan yerini kaybeder ve anahtarı unutulur: o zaman aynı isimle yeni giriş olur (isim yoksa `ISIM_GECERSIZ` alırdın). Atıldıysan iki durumda da `ATILDIN`.

**Değerler:**
- `oyun`: `"cakmak"` (varsayılan, bugünkü oyun) · `"kostebek"` · `"rusRuleti"` · `"takim"` · `"casus"` · `"cakmakGecesi"`. Yeni oyunlar geldikçe eklenir (`docs/Oyunlar.md`). Ayarlar birbirine uymalı, sonuç uymayacaksa (hangi sırayla gönderilirse gönderilsin) `GECERSIZ_GIRDI`; kural tek yerde, `Oyun.AyarlarUyumlu`:
  - Köstebek'te kaos olamaz. Gizli görevler `gorevler` ayarı ne olursa olsun herkeste vardır.
  - Rus Ruleti'nde ceza `"kapali"`, `gorevler` ve `kaos` `false` olmalı (can sistemi zaten ceza). `turSayisi` yok sayılır: son kalana kadar sürer.
  - Takım Düellosu'nda `gorevler` `false` olmalı.
  - Casus'ta `mod` `"curcuna"` olamaz (sorular mekâna göre yazılmalı), `gorevler` ve `kaos` `false` olmalı.
  - Çakmak Gecesi'nde `gorevler` ve `kaos` `false` olmalı. 6–18 kişi (diğer oyunlar 4–10; oda 18 kişilik, sınır dışında `baslat` → `OYUNCU_SAYISI_GECERSIZ`). `turSayisi` yok sayılır: bir taraf kazanana kadar sürer.
- `can`: Rus Ruleti'nde herkesin canı, `1`–`3` (varsayılan `1`; `3` = Son Çakmak). Diğer oyunlarda yok sayılır
- `takimlar`: Takım Düellosu'nda kurucunun dağılımı, `{ "p1": 0, "p2": 1, ... }`. Boş nesne `{}` = rastgele ve dengeli (varsayılan). Takım numarası `0`/`1` değilse ya da odada olmayan biri varsa `GECERSIZ_GIRDI`; sayı değilse `GECERSIZ_MESAJ`. Lobiden çıkan dağılımdan düşer, `tekrarOyna`'da dağılım sıfırlanır. Herkesin yer alıp almadığı ve denge (takımlar arasında en fazla 1 kişi fark) `baslat`'ta denetlenir: tutmuyorsa `baslat` → `GECERSIZ_GIRDI`. Lobi `durum`'unda `ayarlar.takimlar`
- `casus`: Casus'ta casusun bilmediği şey: `"mekan"` (varsayılan; herkesin bir rolü var) · `"kelime"` (Imposter, rol yok). Diğer oyunlarda yok sayılır. Lobi `durum`'unda `ayarlar.casus`
- `rolAcilir`: Çakmak Gecesi'nde ölenin rolü herkese açılır mı (`true`, varsayılan) ya da oyun sonuna kadar gizli mi (`false`). Lobi `durum`'unda `ayarlar.rolAcilir`
- `gecelikKurban`: Çakmak Gecesi'nde katillerin gecede öldürdüğü kişi sayısı, `1`–`3` (varsayılan `1`; en fazla canlı katil sayısı kadar)
- `kapaliRoller`: Çakmak Gecesi'nde kurucunun kapattığı roller, ör. `["palyaco", "deli"]` (varsayılan `[]`). Rol adı değilse `GECERSIZ_MESAJ`; `"masum"`, `"katil"` ya da bu oyunun rolü değilse `GECERSIZ_GIRDI`. Roller: aşağıda "Çakmak Gecesi"
- `mod`: `"normal"` · `"temiz"` · `"curcuna"`
- `miniOyun`: `"tkm"` · `"tekCift"` · `"zar"` · `"karisik"`. **`"reaksiyon"` online'da yok** (`GECERSIZ_GIRDI`): süre istemciden geliyor ve telefonların dokunma gecikmesi farklı. Sadece elden ele.
- `turSayisi`: `0` = sınırsız, `1–100`
- `soruSn`, `cevapSn`, `hamleSn`: `5–300`
- `ceza`: `"kapali"` (varsayılan) · `"hafif"` · `"cesur"`. Cesur'da Kızgın Çakmak da açık. Temiz modda `"cesur"` olamaz: sonuç Temiz + Cesur olacaksa (hangi sırayla gönderilirse gönderilsin) `GECERSIZ_GIRDI`, önce cezayı düşür
- `gorevler`, `kaos`: `true` / `false` (varsayılan `false`). "Hızlı başla" istemcide, bu alanları kendisi gönderir
- `paketler`: paket adları dizisi, dosya adı uzantısız: `["Genel", "Okul"]`. Varsayılan: sunucudaki bütün paketler. Grup paketi yüklendiyse `"@bizim"` de seçilebilir (yüklenmeden `GECERSIZ_GIRDI`; Temiz modda seçili olsa da kullanılmaz). Boş dizi, sunucuda olmayan ya da iki kez yazılan ad `GECERSIZ_GIRDI`. Öneri çek, Curcuna'nın boşlukları ve Kör Soru sadece seçilenlerden çeker. **Çakmak Gecesi'nde paket seçimi kullanılmaz:** öneriler sunucudaki suçlama sorularından (`Assets/Icerik/GeceSorulari.csv`, 48 soru)

### Komutlar

`tur`: durumdaki `tur` ile aynı olmalı. Farklıysa komut yapılmaz, `ESKI_TUR` döner. Soru toplama aşamasında `tur` = `0`.

| `komut` | Alanlar | Kural motorunda |
|---|---|---|
| `havuzaEkle` | `metin` | `HavuzaSoruEkle` |
| `oneriCek` | — | `OneriCek` |
| `soruyuDegistir` | — | `SoruyuDegistir` |
| `soruSor` | `hedef`, `metin` | `SoruSor`. Curcuna'da `metin` yok sayılır. Diğer modlarda `metin` zorunlu, yoksa `GECERSIZ_GIRDI` (online'da fısıltı yok). Temiz modda filtreye takılırsa `UYGUNSUZ` (aşağıda "Temiz mod") |
| `cevapSec` | `hedef` | `CevapSec` |
| `hamle` | `hamle` nesnesi (aşağıda) | `MiniOyunHamlesi` |
| `oy` | `ifsa` (`true` = ifşa, `false` = güme; zorunlu) | `Oy`. Sadece `oylama` aşamasında, `oylayanlar` içindekiler, bir kez |
| `ikiyeKatla` | — | `IkiyeKatla`. Sadece `ikiyeKatla` aşamasında, `kararVeren` |
| `bedelOde` | — | `BedelOde`. Sadece `bedel` aşamasında, `kararVeren` (B) |
| `karariGec` | — | `KarariGec`: "hayır". `ikiyeKatla` ya da `bedel` aşamasında, `kararVeren`. Süre dolması da aynı sonucu verir |
| `sucla` | — | `Sucla`. Köstebek, erken suçlama: herkesin oyun boyu 1 hakkı, sadece `soruSorma` aşamasında (düello ortası kesilmez). Başka aşamada ya da Çakmak'ta `YANLIS_ASAMA`, hak bittiyse `HAK_KALMADI` |
| `suclamaOyu` | `hedef` | `SuclamaOyu`. Sadece `suclama` aşamasında, `oylayanlar` içindekiler, bir kez. Kendin ya da oyundan çıkan `SECILEMEZ` |
| `lincOyu` | `hedef` | `LincOyu`. Çakmak Gecesi, `linc` aşamasında, `oylayanlar` içindekiler (susturulan değil), bir kez. Kendin, ölü ya da oyundan çıkan `SECILEMEZ` |
| `geceSecimi` | `hedef` (yoksa pas), `ikinci` (isteğe bağlı), `ifsa` (Hakem: yarınki düellonun kararı, varsayılan `true`) | `GeceSecimi`. Çakmak Gecesi, `gece` aşamasında, her canlı bir kez. Geçerli hedefler sende `secilebilir`'de, katillerin yeteneği için `ikinciSecilebilir`'de; dışındakiler `SECILEMEZ` |
| `yetenek` | `hedef` (sadece Kuklacı) | `GunduzYetenegi`. Çakmak Gecesi, gündüz turu sürerken (`soruSorma`, `cevapSecme`, `miniOyun`): Sorgucu ve Sınayıcı (A iken), Kuklacı (A iken, `hedef` = C), Kışkırtıcı (B iken). Rolü uymayana `SIRA_SENDE_DEGIL` (sadece ona döner), hak bittiyse `HAK_KALMADI`, başka aşamada `YANLIS_ASAMA` |
| `suikast` | `hedef`, `rol` (zorunlu) | `Suikast`. Suikastçı, `linc` aşamasında, oyunda 1 kez (`HAK_KALMADI`). Başkası `SIRA_SENDE_DEGIL`, bu oyunun rolü olmayan `rol` `GECERSIZ_GIRDI` |
| `casusTahmini` | `tahmin` (zorunlu) | `CasusTahmini`. Casus, `casusListesi`'nden bir mekân (ya da kelime). Oyunun her aşamasında bir kez ("Mekânı biliyorum"), `casusTahmini` aşamasında son şans olarak. Oyun hemen biter. Casus değilsen `SIRA_SENDE_DEGIL`, listede yoksa `GECERSIZ_GIRDI`, Casus oyunu değilse `YANLIS_ASAMA` |
| `tetikCek` | — | `TetikCek`. Rus Ruleti, `tetik` aşamasında, sadece `tetikCeken` (başkası `SIRA_SENDE_DEGIL`) |
| `tetikDevret` | `hedef` | `TetikDevret`. `tetikCeken`, oyun boyu 1 kez (`HAK_KALMADI`); tetik hedefe geçer, onun karar süresi baştan. Kendin, elenen, kopuk ya da AFK `SECILEMEZ` |

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
| `sohbet` | Genel mesaj odadaki herkese (yazana da), DM sadece alana ve yazana |
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
               "ceza": "kapali", "gorevler": false, "kaos": false, "paketler": ["AileDostu", "Genel", "Okul", "TatilYolculuk"] },
  "grupPaketi": 0 }
```

### `durum` — bekleme odasında

Oyun sürerken katılan, oyunda değil. Sadece herkese açık bilgiyi görür: `sana`, soru, görev, seçim listesi hiç gelmez.

```json
{ "t": "durum", "surum": 58, "oda": "KXTPM", "asama": "beklemeOdasi", "kurucu": "p1",
  "oyuncular": [ { "id": "p1", "isim": "Enes", "renk": 0, "bagli": true } ],
  "bekleyenler": [ { "id": "p6", "isim": "Ayşe", "renk": 5, "bagli": true } ],
  "tur": 7, "toplamTur": 15, "oyunBitti": false }
```

`komut` yollarsa `OYUNDA_DEGILSIN`. Oyun bitince (`oyunBitti: true`) kurucunun `tekrarOyna`'sını bekler.

### `durum` — oyunda

```json
{ "t": "durum", "surum": 57, "oda": "KXTPM", "asama": "miniOyun", "tur": 12, "toplamTur": 15, "kalanMs": 8000,
  "kurucu": "p1", "a": "p1", "b": "p2", "c": "p4",
  "oyuncular": [ { "id": "p1", "isim": "Enes", "renk": 0, "bagli": true,
                   "havuzaEkledigi": 0, "gosterilme": 1, "soruAlma": 2, "soruSorma": 3,
                   "ifsaEttirme": 0, "saklama": 1, "miniOyunKazanma": 1,
                   "oynadigiMiniOyun": 2, "cezaCekme": 1, "yanma": 0, "bedelOdeme": 0, "bedelHakki": 2, "kaosKazanma": 0,
                   "kisitlamalar": [ { "tur": "palyaco", "metin": null, "kalan": 2 } ],
                   "afk": false, "cikti": false, "can": 0, "elendi": false, "devretmeHakki": false, "takim": null, "acikRol": null,
                   "susturuldu": false } ],
  "secilebilir": [], "ikinciSecilebilir": [],
  "miniOyun": "tkm", "bHamleYapti": true, "cHamleYapti": false, "beraberlik": 0,
  "sonMiniOyun": null,
  "oyun": "cakmak", "kaos": "yok", "isi": "ilik", "kararVeren": null,
  "oylayanlar": [], "oyKullanan": 0, "sonOylama": null, "bedelOdendi": false, "turCezalari": [],
  "supheliler": [], "sonSuclama": false, "tetikCeken": null, "final": false, "takimPuanlari": [],
  "casusListesi": [], "casusKategori": null, "yakalanan": null, "gun": 0, "kalanGunduzTuru": 0,
  "sana": { "soru": "Grupta sırrını en kolay kim açık eder?", "fisilti": false,
            "atananSoru": null, "degistirmeHakki": false, "oneri": null,
            "hamlen": { "oyun": "tkm", "secim": "tas" },
            "gorevin": { "tur": "kisiyiGoster", "hedef": "p3", "adet": 2, "zorluk": "orta" }, "oyVerdin": false,
            "rol": null, "suclamaHakki": false, "sir": null, "mekanRolu": null,
            "katiller": [], "katilSecimleri": [], "bulgularin": [], "kalanHakkin": null, "cellatHedefin": null,
            "bombalilar": [], "kulakSorusu": null },
  "ifsa": null,
  "ozet": null,
  "bekleme": null,
  "bekleyenler": [] }
```

- `asama`: `"soruToplama"` · `"soruSorma"` · `"cevapSecme"` · `"miniOyun"` · `"oylama"` · `"ikiyeKatla"` · `"bedel"` · `"ifsa"` · `"gume"` · `"oyunSonu"` · `"suclama"` (Köstebek, Casus) · `"tetik"` (Rus Ruleti) · `"casusTahmini"` (Casus) · `"linc"` · `"gece"` (Çakmak Gecesi), aşağıda.
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
- `bekleyenler`: bekleme odasındakiler (`{ id, isim, renk, bagli }`), sonraki oyunda oynayacak.
- `bekleme`: bağlı oyuncu 4'ün altına düştü, oyun durdu: `{ "kalanMs": 95000 }` (bu sürede kimse dönmezse oyun biter). Yoksa `null`.
- `secilebilir`: kopuk, AFK ve çıkmış oyuncular listede yok (aktif kimse kalmadıysa yine de dolu gelir, oyun takılmaz).

**S3 alanları** (kurallar: Ana Plan 8, kararlar: `tasks/g1-todo.md`):
- `kaos`: bu turun kaos kuralı, yoksa `"yok"`. `"tersDunya"` · `"grupKarari"` · `"korSoru"` · `"soranGizli"` · `"aniOlum"` · `"cezaTuru"` · `"yonDegisti"` · `"ikiyeKatla"`. Kaos açıksa her 5 turda bir.
- `isi`: Kızgın Çakmak (sadece `ceza: "cesur"`), yoksa `null`. `"soguk"` · `"ilik"` · `"sicak"` · `"kizgin"`. Eşik hiçbir mesajda yok. Rus Ruleti'nde tabancanın ısısı (aşağıda).
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
    "curcunaSorulari": [ "…" ],
    "kostebek": null,
    "rusRuleti": null,
    "takim": null,
    "casus": null,
    "gece": null }
  ```
  `kostebek`: sadece Köstebek oyununda dolu (aşağıda "Köstebek"), diğer oyunlarda `null`.
  Unvan `tur`: `"miknatis"` · `"gammaz"` · `"sirKupu"` · `"hedefTahtasi"` · `"soruMakinesi"` · `"sansli"` · `"takintili"` · `"intikamci"` · `"atesleOynayan"` · `"bedelci"` · `"cezakes"` · `"kaosCanavari"` · `"kale"` · `"gorunmez"` · `"talihsiz"` · `"sosyalKelebek"` · `"sadik"` · `"dedikoducu"` · `"gundem"` · `"yenilmez"` · `"zorba"` · `"av"` · `"ayna"` · `"uykucu"` · `"kacak"` · `"acikKitap"` · `"eliBos"` · `"kumarbaz"` · `"bumerang"` · `"sogukkanli"` (anlamları `UnvanTuru`'nda). En uç 3 unvan, eşitlikte verilmez.
  **Güme giden soru özette yoktur.** Sayaçlar `oyuncular`'da. Grup hafızası (`GrupKaydi.Isle`) cihazda bu bilgilerden yazılır.

### `olay`

```json
{ "t": "olay", "olay": "ifsa", "a": "p1", "b": "p2", "c": "p4", "kim": null, "soru": "…", "ceza": null, "kaos": null, "hedef": null, "takim": null }
```

`olay`: `"turBasladi"` · `"soruSoruldu"` · `"cevapSecildi"` · `"berabere"` · `"ifsa"` · `"gume"` · `"sureDoldu"` · `"oyunBitti"` ·
`"kaosBasladi"` · `"yandi"` · `"cezaCekildi"` · `"ikiyeKatlandi"` · `"bedelOdendi"` · `"oyVerildi"` ·
`"siraAtlandi"` · `"afk"` · `"afkBitti"` · `"oyuncuCikti"` · `"kararBekleniyor"` · `"suclamaBasladi"` · `"suclamaBitti"` ·
`"tetikBasladi"` · `"tetikBos"` · `"tetikPatladi"` · `"elendi"` · `"tetikDevredildi"` · `"finalBasladi"` · `"duelloPuani"` · `"ifsaBonusu"` · `"casusYakalandi"` · `"casusTahminEtti"` · `"lincBasladi"` · `"lincBitti"` · `"geceBasladi"` · `"geceBitti"` · `"suikast"` · `"hakemKarari"` · `"duelloSonduruldu"` · `"atildi"` (sunucunun olayı: biri atıldı; sadece bu olayda `neden` alanı var: `"kurucu"` ya da `"temiz"`).
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
| `OYUN_BASLADI` | Oyun başlamış: `ayarlar` ve `baslat` yapılamaz (katılan artık bekleme odasına girer) |
| `ATILDIN` | Atıldın (bağlantı kapanır), `neden`: `"kurucu"` ya da `"temiz"` (Temiz modda son ihlal). Ya da bu odadan atılmıştın (anahtar ya da isim yasaklı, `neden` yok) |
| `UYGUNSUZ` | Temiz mod: soru ya da sohbet mesajı filtreye takıldı, **kimseye gitmedi**. `ihlal`: bu odada kaçıncı, `sinir`: kaçıncıda atılırsın (3). Örnek: `{ "t": "hata", "kod": "UYGUNSUZ", "ihlal": 1, "sinir": 3 }` |
| `OYUNDA_DEGILSIN` | Bekleme odasındasın, oyun komutu yollayamazsın |
| `ISIM_GECERSIZ` | İsim boş, 20 karakterden uzun ya da odada aynısı var |
| `ODADA_DEGILSIN` | Odaya girmeden oda komutu gönderdin |
| `ZATEN_ODADASIN` | Bu bağlantı zaten bir odada |
| `KURUCU_DEGILSIN` | Sadece kurucu yapabilir |
| `OYUN_YOK` | Oyun başlamadan oyun komutu |
| `ESKI_TUR` | Komuttaki `tur` güncel değil |
| `COK_HIZLI` | Mesaj sınırı, `odaKur`/`katil` sınırı ya da sohbet sınırı aşıldı. Mesaj **hiç işlenmedi**, biraz bekleyip tekrar gönder |
| `GUNCELLE` | `odaKur`/`katil`'deki `surum` eski: oyunu güncellemen lazım |
| `SUNUCU_DOLU` | Sunucudaki oda sayısı sınırda, yeni oda açılamadı. Sonra tekrar dene |
| `SIRA_SENDE_DEGIL` · `SECILEMEZ` · `YANLIS_ASAMA` · `GECERSIZ_GIRDI` · `HAK_KALMADI` · `HAVUZ_BOS` · `OYUNCU_SAYISI_GECERSIZ` | Kural motorunun `Sonuc` değerleri (`UYGUNSUZ` da öyle, yukarıda) |

### Alanların anlamı

- `surum`: odanın sayacı, her `durum`'da bir artar. Eski sürümlü mesajı at (Ana Plan 5.2, kural 3). Geri dönen oyuncu da kaldığı sayıdan devam eder.
- `renk`: 0–9, oyuncu numarasından (`p1` → 0). Arayüz kendi renk listesinde bu sıraya bakar.
- `olay.kim`: `sureDoldu` (süresi dolan), `yandi` (çakmak elinde yanan), `cezaCekildi` (çeken), `ikiyeKatlandi` (bir el daha isteyen), `bedelOdendi` (ödeyen B), `oyVerildi` (oy veren; oyun ne olduğu yok), `siraAtlandi` (sırası gelen ama kopuk/AFK/çıkmış olan; sıra rastgele birine geçti), `afk` / `afkBitti` (AFK olan / dönen), `oyuncuCikti` (oyundan çıkan), `atildi` (atılan), `suclamaBasladi` (erken suçlama hakkını kullanan; aralıklı oylamada ve son oylamada `null`), `suclamaBitti` (en çok oyu alan; eşitlik ya da hiç oy yoksa `null`), `kararBekleniyor` (İkiye Katla ya da Bedel kararını verecek; mini oyunun ya da oylamanın süresi dolunca da gelir), `tetikBasladi` / `tetikBos` / `tetikPatladi` / `elendi` (tetiği çekecek / çeken / canı biten), `tetikDevredildi` (devreden; devralan `hedef`'te, `hedef` sadece bu olayda dolu), `duelloPuani` (düelloyu kazanan) / `ifsaBonusu` (A); bu ikisinde `takim`: puan alan takım (sadece bunlarda dolu), `casusYakalandi` / `casusTahminEtti` (casus), `lincBitti` (elenen; eşitlik ya da hiç oy yoksa `null`), `suikast` (Suikastçı'nın tahminiyle ölen), `hakemKarari` / `duelloSonduruldu` (`b`, `c`: düello oynanmadı; kimin yaptığı yok), `elendi` (Çakmak Gecesi'nde gece ölen; ölenler `geceBitti`'den önce birer birer gelir). Diğerlerinde `null`.
- Paketler ve sesli cezalar: sunucu `Assets/Icerik` altındaki `.txt` paketlerini ve `Cezalar.csv`'yi açılışta okur.
- Sabit süreler (şimdilik ayarlanamaz): ifşa/güme ekranı 6 sn, Curcuna soru toplama 90 sn, İkiye Katla/Bedel kararı 5 sn. Oylama `hamleSn` kadar sürer.

### Hata sırası

Bir mesaj birden çok kuralı çiğniyorsa ilk tutan döner:
`COK_HIZLI` (mesaj sınırı, mesaj okunmadan) → JSON bozuk ya da `t` yok/bilinmiyor (`GECERSIZ_MESAJ`) → `COK_HIZLI` (`odaKur`/`katil` sınırı) → `ODADA_DEGILSIN` → `KURUCU_DEGILSIN` → `OYUN_BASLADI` / `OYUN_YOK` → eksik ya da yanlış tipte alan (`GECERSIZ_MESAJ`) → `ESKI_TUR` → `GECERSIZ_GIRDI` ve kural motoru hataları.
`komut` mesajında `ESKI_TUR`'dan önce sadece `tur` ve `komut` alanlarına bakılır. Komutun kendi alanları (`hedef`, `metin`, `hamle`, `ifsa`) ve bilinmeyen `komut` adı `ESKI_TUR`'dan sonra denetlenir (`GECERSIZ_MESAJ`).

- `ayarlar`: bilinmeyen `mod`/`miniOyun`/`ceza` adı ya da yanlış tip (`paketler` dizi değil ya da içinde metin olmayan var) `GECERSIZ_MESAJ`. Aralık dışı sayı, Temiz + Cesur, geçersiz paket listesi `GECERSIZ_GIRDI`. Oyun başladıysa `OYUN_BASLADI`.
- `oy` komutunda `ifsa` yoksa ya da `true`/`false` değilse `GECERSIZ_MESAJ`.
- `baslat` oyun başladıktan sonra: `OYUN_BASLADI`.
- `at`: `kim` yoksa ya da `"p…"` değilse `GECERSIZ_MESAJ`. Odada olmayan ya da kurucunun kendisi `GECERSIZ_GIRDI`. Kurucu değilse `KURUCU_DEGILSIN`.
- `tekrarOyna`: oyun yoksa `OYUN_YOK`, bitmediyse `YANLIS_ASAMA`.
- `grupPaketi`: `KURUCU_DEGILSIN` → `OYUN_BASLADI` → `sorular` yok ya da metin dizisi değil, `ilk` `true`/`false` değil (`GECERSIZ_MESAJ`) → boş dizi, temizlenince boş ya da 140 karakterden uzun soru, toplam 500'ü aşıyor (`GECERSIZ_GIRDI`; parçanın hiçbiri eklenmez, `ilk` olsa da eski paket silinmez).
- `sohbet`: `ODADA_DEGILSIN` → `COK_HIZLI` (sohbet sınırı) → `metin` yok ya da metin değil, `kime` metin değil ya da `"p…"` / kanal adı değil (`GECERSIZ_MESAJ`) → temizlenince boş, 200 karakterden uzun, `kime` odada yok ya da kendin, kanala yazamıyorsun, Çakmak Gecesi'nde genele/DM'e yazamıyorsun (`GECERSIZ_GIRDI`) → `UYGUNSUZ`.
- Bilinmeyen `komut` ya da hamle `oyun` adı: `GECERSIZ_MESAJ`.

## Sınırlar

Sunucuyu kötüye kullanmaya karşı. Normal oyunda hiçbirine takılınmaz.

| Sınır | Varsayılan | Aşılınca | Ayar adı |
|---|---|---|---|
| IP başına aynı anda açık bağlantı | 32 | HTTP 429, WebSocket açılmaz | `IpBasinaBaglanti` |
| IP başına `odaKur` + `katil` | dakikada 30 (her 2 sn'de bir hak dolar, en fazla 30 birikir) | `COK_HIZLI` | `IpBasinaGirisDakikada` |
| Bağlantı başına mesaj | saniyede 10, arka arkaya en fazla 20 | `COK_HIZLI` | `MesajSaniyede`, `MesajPatlama` |
| Bağlantı başına `sohbet` | saniyede 1 (Ana Plan 6.4), arka arkaya en fazla 2 (ağ gecikmesi iki mesajı yaklaştırabilir) | `COK_HIZLI` | `SohbetPatlama` |
| Başlatılmayan lobi | 30 dk | Oda kapanır, bağlantılar kesilir. Anahtarla dönüş `ODA_YOK` | `LobiSn` |
| Odaya girmeyen bağlantı | 120 sn | Bağlantı kesilir | `OdasizSn` |
| Sunucudaki toplam oda | 2000 | `odaKur` → `SUNUCU_DOLU` | `EnFazlaOda` |
| Sunucudaki toplam WebSocket | 5000 | Yeni bağlantı açılmaz | `EnFazlaBaglanti` |

- **IP:** sunucunun önündeki Caddy'nin `X-Forwarded-For` başlığından. Bu başlığa sadece aynı makineden (localhost) gelen istekte güvenilir. IPv6'da bir /64 bloğu tek IP sayılır (bir ev ya da telefon bütün bloğu alır).
- Aynı Wi-Fi'daki herkes aynı IP'dir: 10 kişilik grup ve yeniden bağlananlar sınırlara sığar.
- `COK_HIZLI` alan istemci durmazsa cevaplar gönderim kuyruğunu doldurur ve bağlantı kesilir.
- Ayarlar sunucuda: `appsettings.json` içinde `"Sinirlar"` bölümü ya da ortam değişkeni (`Sinirlar__IpBasinaBaglanti=50`).

## Köstebek (docs/Oyunlar.md, 1)

`ayarlar` → `oyun: "kostebek"`. Turlar normal çakmak; grubun içinde gizli görevli bir köstebek var.

- **Kurulum:** biri köstebek olur, sadece kendi `durum`'unda `sana.rol: "kostebek"` (diğerlerinde ve Çakmak'ta `null`). Herkesin gizli görevi var (`sana.gorevin`); köstebeğinki köstebek görevi (`zorluk: "zor"`, oyun ortasında bitebilen türlerden).
- **Suçlama oylaması** (`asama: "suclama"`): her 5 turda bir kendiliğinden (6., 11., 16. turun başında, A sormadan önce) ya da biri `sucla` deyince. `oylayanlar`: oyundan çıkmamış herkes (A ve B dahil). Her biri `suclamaOyu` ile kendisi dışında birini gösterir. Herkes oy verince ya da süre (60 sn) dolunca biter. Kimin kime oy verdiği hiçbir mesajda yok; `oyKullanan` sayısı ve `sana.oyVerdin` var.
- **Sonuç:** en çok oyu alan suçlanır (`olay: "suclamaBitti"`, `kim`). Eşitlikte ya da hiç oy yoksa kimse (`kim: null`).
  - Suçlanan köstebekse grup kazanır, oyun biter.
  - Değilse `supheliler`'e eklenir (herkese açık) ve oyun sürer: A'nın turu baştan, tam süreyle. A bu arada oyundan çıktıysa tur yanar, sıra başkasına geçer.
- **Köstebek görevini bitirirse** o turun sonucu (ifşa/güme) gösterildikten sonra oyun biter, köstebek kazanır.
- **Son oylama:** tur sayısı bitince ya da kurucu `bitir` deyince bir oylama daha (`sonSuclama: true`). Yakalanmazsa köstebek kazanır. Köstebek görevini o turda bitirdiyse oylama olmaz, kazanır.
- **Köstebek oyundan çıkarsa** grup kazanır. Bağlı oyuncu 4'ün altında kalıp oyun biterse (2 dk) oylama yapılmaz, köstebek yakalanmamış sayılır (görevini bitirdiyse `neden: "gorevBitti"`).
- `sana.suclamaHakki`: erken suçlama hakkın duruyor mu.
- **Oyun sonunda** `ozet.kostebek`: `{ "kim": "p3", "kazanan": "grup" | "kostebek", "neden": "yakalandi" | "gorevBitti" | "yakalanmadi" | "kacti" }`. Herkesin görevi (köstebeğinki dahil) `ozet.gorevler`'de. Oyun bitmeden köstebeğin kim olduğu hiçbir mesajda yok.

## Rus Ruleti (docs/Oyunlar.md, 2)

`ayarlar` → `oyun: "rusRuleti"`, `can: 1`–`3`. Çakmak bir tabanca; düelloyu kaybeden tetiği çeker.

- **Tur:** normal çakmak. Sonuç ekranından (`ifsa` / `gume`) sonra `asama: "tetik"`, `tetikCeken`: düelloyu kaybeden (`olay: "tetikBasladi"`). O `tetikCek` der ya da (oyun boyu bir kez) `tetikDevret` ile başkasına verir. Karar süresi `hamleSn`; dolarsa tetik kendiliğinden çekilir.
- **Tabanca:** 6 hazne, 1 mermi, yeri ve kaç haznenin boşaldığı hiçbir mesajda yok. `isi`: tabancanın ısısı, gürültülü. Sonraki tetik patlayacaksa her zaman `"kizgin"` görünür, ama `"kizgin"` her zaman patlar demek değil.
- **Patlarsa** (`tetikPatladi`) bir can gider, tabanca yeniden dolar. Can biterse `elendi`: izler, seçilmez, sırası gelmez.
- **Final:** 3 kişi kalınca çakmak turu biter (`finalBasladi`, `final: true`). Her turda kalanlardan rastgele ikisi sorusuz düello yapar (`b`, `c`; `a` = `b`), kaybeden doğrudan `tetik`'e geçer (sonuç ekranı yok).
- **Kazanma:** son kalan. Kurucu `bitir` derse en çok canı olan kazanır, eşitlikte kimse. Tetiği çekecek kişi oyundan çıkarsa çekilmez.
- `toplamTur` bu oyunda `0` (son kalana kadar sürer). Oyuncu nesnesinde `can`, `elendi`, `devretmeHakki` (herkese açık; diğer oyunlarda `0` / `false`).
- **Oyun sonunda** `ozet.rusRuleti`: `{ "kazanan": "p3" | null, "elenmeSirasi": ["p2", "p4"] }`.

## Takım Düellosu (docs/Oyunlar.md, 3)

`ayarlar` → `oyun: "takim"`, isteğe bağlı `takimlar`. İki takım (`0` ve `1`), turlar normal çakmak.

- **Takımlar:** kurucunun dağılımı ya da rastgele ve dengeli. Oyuncu nesnesinde `takim` (herkese açık; diğer oyunlarda `null`). Tek sayıda kişide küçük takım 1 puanla başlar.
- **Puan** (`takimPuanlari: [0. takım, 1. takım]`):
  - **Düello puanı** (`duelloPuani`): B ile C farklı takımdansa düelloyu kazananın takımı +1. Grup Kararı turunda oylamanın kazananı sayılır (eşitlikte puan yok).
  - **İfşa bonusu** (`ifsaBonusu`): A rakip takımdan birine sorduysa ve soru ifşa olursa A'nın takımı +1. Soran Gizli turunda yok (A'nın takımını ele verirdi).
  - B ile C aynı takımdansa puan yok; çakmak takımda kalır (sıradaki A, C).
- **Kazanma:** 7 puana ulaşan takım, o turun sonucu gösterildikten sonra kazanır. Tur sayısı biterse ya da kurucu `bitir` derse önde olan; eşitlikte berabere (aynı turda ikisi de 7'yi geçerse yüksek olan).
- **Takım sohbeti:** `{ "t": "sohbet", "kime": "takim", "metin": "…" }`, oyun sürerken, sadece yazanın takımına (kendisi dahil) gider; gelen mesajda `"kime": "takim"`. Takım oyunu yoksa, lobide ya da bekleme odasında `GECERSIZ_GIRDI`.
- **Oyun sonunda** `ozet.takim`: `{ "kazanan": 0 | 1 | null, "puanlar": [7, 4] }`.

## Casus (docs/Oyunlar.md, 4)

`ayarlar` → `oyun: "casus"`, isteğe bağlı `casus: "mekan" | "kelime"`. Herkes gizli bir mekânı (ya da kelimeyi) bilir, casus bilmez. Turlar normal çakmak, sorular mekâna göre.

- **Kurulum:** biri casus olur, sadece kendi `durum`'unda `sana.rol: "casus"`. Diğer herkese `sana.sir` (mekân ya da kelime) ve mekânda `sana.mekanRolu` (herkese ayrı, "Hostes"); casusa ikisi de `null`.
- **Herkese, oyun boyunca:** `casusListesi` (mekânda bütün mekânlar, kelimede kategorinin kelimeleri) ve kelimede `casusKategori` ("Hayvanlar"; mekânda `null`). Liste casusu ele vermez: herkeste aynı.
- **"Casus kim?" oylaması:** Köstebek'teki oylamanın aynısı (`suclama` aşaması, `suclamaOyu`, `supheliler`, `sonSuclama`), her 4 turda bir (5., 9., 13. turun başında). Erken suçlama (`sucla`) yok.
- **Son şans:** oylamada en çok oyu casus alırsa `olay: "casusYakalandi"` (`kim`: casus), `asama: "casusTahmini"`, herkese `yakalanan` (casus). Casus 30 sn içinde `casusTahmini` ile listeden seçer. Bilirse casus, bilemez ya da süre dolarsa grup kazanır.
- **"Mekânı biliyorum":** casus yakalanmadan da, istediği an bir kez `casusTahmini` yollayabilir (`olay: "casusTahminEtti"`). Bilirse casus, bilemezse grup kazanır. Elden elede düğme herkeste görünür; online'da da herkese göster, casus olmayana `SIRA_SENDE_DEGIL` döner, kimse düğmeye bakarak casusu anlamasın.
- **Son oylama:** tur sayısı bitince ya da kurucu `bitir` deyince (`sonSuclama: true`). Casus yakalanırsa yine son şansı var; yakalanmazsa casus kazanır.
- **Casus oyundan çıkarsa** grup kazanır. Bağlı oyuncu 4'ün altında kalıp oyun biterse casus yakalanmamış sayılır.
- **Oyun sonunda** `ozet.casus`: `{ "kim": "p3", "kazanan": "grup" | "casus", "neden": "…", "sir": "Uçak", "tahmin": "Tren" | null }`. `neden`: `"yakalandi"` (son şansta bilemedi ya da süresi doldu) · `"yanlisTahmin"` · `"kacti"` · `"bildi"` · `"sonSanstaBildi"` · `"yakalanmadi"`. Oyun bitmeden casusun kim olduğu ve sır casusa hiçbir mesajda yok.

## Çakmak Gecesi (docs/Oyunlar.md, 5)

`ayarlar` → `oyun: "cakmakGecesi"`; isteğe bağlı `rolAcilir`, `gecelikKurban`, `kapaliRoller`. 6–18 kişi. Taraflar Masumlar ve Katiller, bir de tek başına kazanan tarafsızlar. Rollerin ne yaptığı ve gece sırası: `docs/Oyunlar.md` 5 (kesin kurallar orada).

- **Roller** (`sana.rol`, sadece sahibine; Deli'ye sandığı rol):
  - Masumlar: `"masum"` · `"koruyucu"` · `"dedektif"` · `"serif"` · `"kanunsuz"` · `"bekci"` · `"kulak"` · `"falci"` · `"deli"`
  - Katiller: `"katil"` · `"patron"` · `"casus"` · `"susturucu"` · `"tuzakci"` · `"temizlikci"` · `"suikastci"` · `"palyaco"` · `"vampir"`
  - Tarafsızlar: `"soytari"` · `"seriKatil"` · `"cellat"` · `"hayattaKalan"` · `"hirsiz"` · `"bombaci"`
  - Çakmağa özel: masum `"sorgucu"` · `"hakem"` · `"kollayici"` · `"yuzlesmeci"` · `"sezgici"` · `"sinayici"` · `"siper"` · `"sondurucu"`; katil `"kundakci"` · `"dinleyici"` · `"fitneci"` · `"kuklaci"` · `"kiskirtici"`; tarafsız `"kibritci"` · `"gozde"` · `"duellocu"` · `"sirKupu"`
- **Katiller** birbirini rolleriyle bilir: `sana.katiller` `[ { "kim", "rol" } ]` (sadece katillerde dolu, ölüler dahil). Gece birbirinin kurban oyunu görür: `sana.katilSecimleri` `[ { "kim", "hedef" } ]`.
- **Gündüz:** canlıların yarısı kadar (en az 1) çakmak turu. `gun` (1'den), `kalanGunduzTuru` (bu tur hariç; 0 = sıradaki linç). `toplamTur` hep `0`. Canlılar 4'ün altındaysa çakmak turu yok.
- **Linç** (`asama: "linc"`, `olay: "lincBasladi"`): `oylayanlar` (canlılar, susturulan hariç) `lincOyu` ile kendisi dışında bir canlıyı gösterir. Herkes oy verince ya da süre (60 sn) dolunca en çok oyu alan elenir (`olay: "lincBitti"`, `kim`), eşitlikte kimse. Palyaço'nun kandırdığının oyu Palyaço'nun oyuna sayılır (kimse görmez). Kimin kime oy verdiği hiçbir mesajda yok.
  - **Suikast:** linç sürerken Suikastçı `suikast` (`hedef`, `rol`) yollar: bilirse hedef, bilemezse kendisi hemen ölür (`olay: "suikast"`, `kim`: ölen). Ölenin oyu ve ona verilen oylar düşer; verenler yeniden oy verir (`sana.oyVerdin` yeniden `false`).
- **Gece** (`asama: "gece"`, `olay: "geceBasladi"`): **her canlı** `geceSecimi` yollar; `hedef`siz = pas. Adaylar `secilebilir`'de (rolüne göre: katiller katil olmayanları, Koruyucu hakkı varsa kendini de, Bombacı bomba koyduysa kendini = patlat). Katil tarafının yeteneği (Casus, Susturucu, Tuzakçı, Temizlikçi, Palyaço, Vampir) `ikinci` ile, adayları `ikinciSecilebilir`'de. Yeteneği olmayan da birini seçebilir (etkisi yok); kimin uzun düşündüğünden rol anlaşılmasın.
- **Sabah:** ölenler birer `olay: "elendi"` (`kim`), sonra `olay: "geceBitti"`; hiç `elendi` yoksa kimse ölmedi. Yeni gün (`gun` +1), A rastgele bir canlı.
- **Bakışların sonuçları** sabah `sana.bulgularin`'de birikir: `[ { "gun": 2, "kim": "p3", "supheli": true | null, "rol": "katil" | null, "ziyaretciler": [], "grup": [], "kotuVar": null } ]`. Dedektif `supheli`, Casus `rol`, Bekçi `ziyaretciler`, Falcı `grup` ve `kotuVar` doldurur. Deli'ninkiler rastgele (kendisi bilmez).
- **Haklar:** `sana.kalanHakkin`: sınırlı yeteneğin kalan hakkı (Koruyucu kendini koruma, Kanunsuz kurşun, Falcı, Temizlikçi, Suikastçı, Vampir, Hırsız, Seri Katil'in kaçınması); yoksa `null`.
- **Sadece sahibine:** `sana.cellatHedefin` (Cellat), `sana.bombalilar` (Bombacı), `sana.kulakSorusu` (Kulak ve Dinleyici, sadece `gume` aşamasında: güme giden soru; başka hiçbir yerde yok).
- **Herkese:** `oyuncular[].elendi`, `oyuncular[].acikRol` (rol açılması açıksa ve temizlenmediyse; Seri Katil'in kurbanı da gizli), `oyuncular[].susturuldu` (o gün yazamaz, oy veremez).
- **Kazanma** (her ölümde ve oyundan çıkışta): Soytarı linç edilirse ya da Cellat'ın hedefi linç edilirse o, oyun biter. Masumlar: katil, Seri Katil ve Bombacı kalmadı. Katiller: öldüren tarafsız yok ve katiller diğer canlılardan az değil. Seri Katil / Bombacı / Kibritçi: tek öldüren o ve en fazla 2 canlı. 2 canlı kalıp kimse kazanamıyorsa berabere. Hayattaki Hayatta Kalan ve hedefini tutturan Gözde, Düellocu, Sır Küpü kazananlara eklenir. Kurucu `bitir` derse kazanan yok.
- **Sohbet:** aşağıda "Sohbet" (katil ve ölüler kanalı; gece, ölüler ve susturulan genele yazamaz).
- **Oyun sonunda** `ozet.gece`: `{ "kazanan": "masumlar" | "katiller" | "seriKatil" | "bombaci" | "kibritci" | "soytari" | "cellat" | "berabere" | null, "kazananlar": ["p2", …], "roller": [ { "kim": "p1", "rol": "deli" } ] }`.

## Grup paketi (Ana Plan 8.5)

"Bizim paket": grubun Curcuna soruları ve ifşa olan soruları, kurucunun cihazındaki grup kaydında (`GrupKaydi.GrupPaketi`, en fazla 500). Online'da kurucu lobide yükler:

```json
{ "t": "grupPaketi", "ilk": true, "sorular": ["Kim en çok uyur?", "…"] }   // ilk parça: öncekini siler
{ "t": "grupPaketi", "sorular": ["…"] }                                  // sonrakiler eklenir
```

- Bir mesaj 4096 baytı geçemez: soruları sığdığı kadar parçalara böl (kısa sorularla ~40, en uzunlarla ~20 soru). Sonraki parçayı, öncekinden sonra gelen `durum`'u görünce yolla: mesaj sınırına (saniyede 10) takılmazsın.
- Her soru isim/soru gibi temizlenir (`TemizMetin`), 1–140 karakter. Tekrarlar (büyük/küçük harf farkıyla) bir kez sayılır. Toplam en fazla 500.
- Lobi `durum`'unda `grupPaketi`: yüklenen soru sayısı (herkes görür, sorular gitmez). 0 = yok.
- Seçmek için `ayarlar` → `paketler`'e `"@bizim"` ekle (istemcideki `PaketBilgisi.Bizim` ile aynı ad). Öneri çek, Curcuna boşlukları ve Kör Soru ondan da çeker.
- **Temiz modda kullanılmaz** (sorular filtreden geçmedi), elden elede de öyle.
- Sorular sadece odanın belleğinde, oda kapanınca gider. `tekrarOyna`'da paket ve seçim kalır; oyun sonunda kayıt güncellendiyse kurucu `ilk: true` ile yeniden yükleyebilir.
- Hangi grup kaydının yükleneceği istemcinin işi (elden ele kurulumdaki eşleşme kuralı, karar 22).

## Sohbet (Ana Plan 6.4)

```json
{ "t": "sohbet", "kime": null, "metin": "selam millet" }                              // istemci → sunucu, genel
{ "t": "sohbet", "kime": "p3", "metin": "psst" }                                     // DM
{ "t": "sohbet", "id": "m41", "kimden": "p1", "kime": null, "metin": "selam millet" } // sunucu → istemci
```

- **Genel** (`kime` `null` ya da yok): odadaki herkese gider, **yazana da** (yazan kendi mesajını bu dönüşle ekler; dönmezse gitmemiştir: `hata`'ya bak). Bekleme odasındakiler de yazar ve görür.
- **Takım kanalı** (`kime: "takim"`): sadece Takım Düellosu sürerken, yazanın takımına (yukarıda "Takım Düellosu").
- **Katil kanalı** (`kime: "katil"`): Çakmak Gecesi'nde, gece, canlı katiller arasında.
- **Ölüler kanalı** (`kime: "oluler"`): Çakmak Gecesi'nde, ölüler arasında, her an.
- **Çakmak Gecesi'nde genel ve DM:** gece kimse, ölüler, o gün susturulan ve oyunda olmayan bekleme odası yazamaz (canlılara ipucu vermesinler); herkes okur. Oyun bitince herkes yazar.
- Kim hangi kanala yazar ve kim okur: kural motoru (`Oyun.SohbeteYazabilir`, `Oyun.KanalAlicilari`). Gelen mesajda `kime` kanalın adı.
- **DM** (`kime: "p3"`): sadece alana ve yazana gider. Alan odada olmalı (oyuncu ya da bekleme odasında; bağlı değilse mesaj ona hiç ulaşmaz), kendine DM `GECERSIZ_GIRDI`. İstemcide DM kanalı = karşıdaki kişi: `kimden` sensen `kime`, değilse `kimden`.
- `metin` isim ve soruyla aynı temizlikten geçer (`TemizMetin`), temizlenince 1–200 karakter. Gelen `metin` hep temizlenmiş haldir.
- `id`: odanın sohbet sayacı (`"m1"`, `"m2"`…). Sıra ve ileride rapor için.
- Sunucu mesaj **saklamaz**: kopup dönen ya da sonradan gelen eski mesajları görmez.
- Saniyede 1 mesaj, arka arkaya en fazla 2 (`COK_HIZLI`).

## Temiz mod (Ana Plan 6.4)

`mod: "temiz"` iken soru (`soruSor` komutunun `metin`'i) ve sohbet (genel + DM) filtreden geçer.

- Yakalanan **gönderilmez**, kimse görmez. Sadece yazana `hata` gider: `{ "kod": "UYGUNSUZ", "ihlal": 1, "sinir": 3 }`. Soru yakalanırsa tur aynen kalır, A başka soru yazabilir.
- Soru ve sohbet ihlalleri **aynı sayaçta**, odada isme göre sayılır (lobide çıkıp aynı isimle giren sıfırlanmaz). **3. ihlalde atılır:** `ATILDIN` (`neden: "temiz"`), herkese `olay: "atildi"` (`neden: "temiz"`). Kurucuyu atma ile aynı: bu odaya ne anahtarıyla ne aynı isimle dönebilir, oyundaysa oyundan çıkar. Kurucu atıldıysa kuruculuk sıradakine geçer.
- Filtre kural motorunda, tek tanım: `Assets/Kurallar/TemizFiltre.cs`. Elden ele Temiz modda da `SoruSor` aynı filtreyle `Sonuc.Uygunsuz` döner (sayma ve atma yok). Adımlar Ana Plan 6.4: küçük harf, görünmez karakter, NFKD, rakam → harf (`s1k`, `$ik`), tekrarlar (`sıııık`), tek harfler (`a.m.k`), kara liste kelime başında + izin listesi (`götürmek`, `sikke`, `şikayet`, `sıkıldım`).
- Paket soruları filtreye takılmaz (`IcerikTestleri` her soruyu dener).
- İsim filtrelenmez (Ana Plan'da sadece soru ve sohbet).

## Bağlantı koparsa (Ana Plan 6.1)

- **Lobide ve bekleme odasında:** kopan odadan çıkar (yeri tutulmaz). Kurucu çıktıysa kuruculuk sıradaki oyuncuya geçer. Lobi boşalırsa oda silinir.
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
| "`Kur` grup paketinin sorularını da alır" | Kurucu `grupPaketi` ile parça parça yükler, `paketler`'de `"@bizim"` | 4096 bayt mesaj sınırı |
