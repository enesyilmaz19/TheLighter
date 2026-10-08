# Ağ Protokolü (online mod)

Durum: **taslak (S4 iskeleti).** Sözleşme oturumunda onaylanacak. Kaynak: `docs/Ana Plan.md` 5.2.
Sunucu: `Sunucu/Cakmak.Sunucu` · İstemci: Unity (G2).

Bu iskelette **yok** (S5): sohbet, atma, kopanın 60 sn beklenmesi, AFK etiketi, geç gelen bekleme odası, Temiz filtresi.

---

## Bağlantı

- Adres: `ws://<sunucu>/ws` (yayında `wss://`).
- Her WebSocket **metin** mesajı tek bir JSON nesnesi. UTF-8. En fazla **4096 bayt**. Daha büyüğü bağlantıyı kapatır (kapanış kodu 1009).
- İkili (binary) çerçeve ve aynı alanı iki kez yazan JSON → `GECERSIZ_MESAJ`, bağlantı açık kalır.
- Sunucu 10 sn'de bir ping atar. 20 sn cevap gelmezse bağlantı kopmuş sayılır.
- Her mesajda `t` alanı tipi söyler.
- Oyuncu kimliği: `"p1"` … `"p10"`. Odayı kuran `p1`, sonra katılış sırasıyla.
- Oda kodu: 5 büyük harf, `I` ve `O` yok (`0/O`, `1/I` karışmasın). Örnek: `"KXTPM"`.

## İstemci → sunucu

| `t` | Alanlar | Kim, ne zaman |
|---|---|---|
| `odaKur` | `isim` | Herkes. Yeni oda açar, gönderen `p1` ve kurucu olur |
| `katil` | `oda`, `isim`, `anahtar` (ilk girişte `null`) | Oda lobideyse yeni oyuncu olarak girer. `anahtar` doğruysa oyun başlamış olsa da **aynı kimlikle geri döner** |
| `ayarlar` | `mod`, `miniOyun`, `turSayisi`, `soruSn`, `cevapSn`, `hamleSn` (hepsi isteğe bağlı) | Kurucu, lobide. Sadece gönderilen alanlar değişir |
| `baslat` | — | Kurucu, lobide, en az 4 oyuncu |
| `komut` | `tur`, `komut`, + komutun alanları (aşağıda) | Oyunda |
| `bitir` | — | Kurucu, oyunda. Oyunu hemen bitirir |

**İsim:** baştaki/sondaki boşluk silinir, 1–20 karakter. Aynı odada aynı isim iki kez olamaz (büyük/küçük harf fark etmez; Türkçe İ/ı özel olarak eşlenmez). Yeni girişte `isim` yoksa `ISIM_GECERSIZ`. Anahtarla dönüşte `isim` yok sayılır.

**Değerler:**
- `mod`: `"normal"` · `"temiz"` · `"curcuna"`
- `miniOyun`: `"tkm"` · `"tekCift"` · `"zar"` · `"karisik"`. **`"reaksiyon"` online'da yok** (`GECERSIZ_GIRDI`): süre istemciden geliyor ve telefonların dokunma gecikmesi farklı. Sadece elden ele.
- `turSayisi`: `0` = sınırsız, `1–100`
- `soruSn`, `cevapSn`, `hamleSn`: `5–300`

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
  "ayarlar": { "mod": "normal", "miniOyun": "tkm", "turSayisi": 15, "soruSn": 60, "cevapSn": 20, "hamleSn": 10 } }
```

### `durum` — oyunda

```json
{ "t": "durum", "surum": 57, "oda": "KXTPM", "asama": "miniOyun", "tur": 12, "toplamTur": 15, "kalanMs": 8000,
  "kurucu": "p1", "a": "p1", "b": "p2", "c": "p4",
  "oyuncular": [ { "id": "p1", "isim": "Enes", "renk": 0, "bagli": true,
                   "havuzaEkledigi": 0, "gosterilme": 1, "soruAlma": 2, "soruSorma": 3,
                   "ifsaEttirme": 0, "saklama": 1, "miniOyunKazanma": 1 } ],
  "secilebilir": [],
  "miniOyun": "tkm", "bHamleYapti": true, "cHamleYapti": false, "beraberlik": 0,
  "sonMiniOyun": null,
  "sana": { "soru": "Grupta sırrını en kolay kim açık eder?", "fisilti": false,
            "atananSoru": null, "degistirmeHakki": false, "oneri": null,
            "hamlen": { "oyun": "tkm", "secim": "tas" } },
  "ifsa": null }
```

- `asama`: `"soruToplama"` · `"soruSorma"` · `"cevapSecme"` · `"miniOyun"` · `"ifsa"` · `"gume"` · `"oyunSonu"`
- `a`, `b`, `c`: o an boşsa `null`.
- `secilebilir`: sadece seçim yapacak kişiye dolu (soru sormada A'ya, cevap seçmede B'ye). Diğerlerine `[]`.
- `sana`: **sadece o oyuncunun mesajında** dolu alanlar. Soru metni sadece B'nin mesajında.
- `sonMiniOyun`: `ifsa` ve `gume` aşamasında dolu:
  `{ "oyun": "tkm", "bHamle": {…}, "cHamle": {…}, "bZar": 0, "cZar": 0, "zarlaKarar": false, "sureDoldu": false, "cKazandi": true }`
- `ifsa`: `ifsa` aşamasında `{ "soru": "…", "fisilti": false }`, yoksa `null`.
- `kalanMs`: geri sayımı istemci kendisi yürütür. Sunucu sadece değişiklikte yeni durum yollar.

### `olay`

```json
{ "t": "olay", "olay": "ifsa", "a": "p1", "b": "p2", "c": "p4", "kim": null, "soru": "…" }
```

`olay`: `"turBasladi"` · `"soruSoruldu"` · `"cevapSecildi"` · `"berabere"` · `"ifsa"` · `"gume"` · `"sureDoldu"` · `"oyunBitti"`.
`soru` sadece `ifsa` olayında dolu. Animasyon ve ses için. Ekran her zaman `durum`'a göre çizilir.

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
| `SIRA_SENDE_DEGIL` · `SECILEMEZ` · `YANLIS_ASAMA` · `GECERSIZ_GIRDI` · `HAK_KALMADI` · `HAVUZ_BOS` · `OYUNCU_SAYISI_GECERSIZ` | Kural motorunun `Sonuc` değerleri |

### Alanların anlamı

- `surum`: odanın sayacı, her `durum`'da bir artar. Eski sürümlü mesajı at (Ana Plan 5.2, kural 3). Geri dönen oyuncu da kaldığı sayıdan devam eder.
- `renk`: 0–9, oyuncu numarasından (`p1` → 0). Arayüz kendi renk listesinde bu sıraya bakar.
- `olay.kim`: sadece `sureDoldu`'da dolu, süresi dolan oyuncu.
- Online'da öneri havuzu: sunucu `Assets/Icerik` altındaki **bütün** paketleri kullanır. Paket seçimi S3'te.
- Sabit süreler (şimdilik ayarlanamaz): ifşa/güme ekranı 6 sn, Curcuna soru toplama 90 sn.

### Hata sırası

Bir mesaj birden çok kuralı çiğniyorsa ilk tutan döner:
JSON bozuk ya da `t` yok/bilinmiyor (`GECERSIZ_MESAJ`) → `ODADA_DEGILSIN` → `KURUCU_DEGILSIN` → `OYUN_BASLADI` / `OYUN_YOK` → eksik ya da yanlış tipte alan (`GECERSIZ_MESAJ`) → `ESKI_TUR` → `GECERSIZ_GIRDI` ve kural motoru hataları.

- `ayarlar`: bilinmeyen `mod`/`miniOyun` adı ya da yanlış tip `GECERSIZ_MESAJ`, aralık dışı sayı `GECERSIZ_GIRDI`. Oyun başladıysa `OYUN_BASLADI`.
- `baslat` oyun başladıktan sonra: `OYUN_BASLADI`.
- Bilinmeyen `komut` ya da hamle `oyun` adı: `GECERSIZ_MESAJ`.

## Bağlantı koparsa (iskelette)

- **Lobide:** oyuncu odadan çıkar. Kurucu çıktıysa kuruculuk sıradaki oyuncuya geçer. Oda boşalırsa silinir.
- **Oyunda:** oyuncu `bagli: false` olur ama oyunda kalır. Süreler işlemeye devam eder (kural motoru beklemez). `anahtar` ile geri dönebilir. Kurucu koptuysa kuruculuk ilk bağlı oyuncuya geçer, geri dönünce geri almaz.
- **Odada bağlı kimse kalmazsa** oda hemen silinir. Sonra anahtarla dönüş `ODA_YOK` alır. *(S5'te oda bir süre tutulacak.)*

## Ana Plan 5.2'den farklar (oturumda onaylanacak)

| 5.2'de | Burada | Neden |
|---|---|---|
| `ayarlar` içinde `"tur": 20` | `"turSayisi": 20` | `komut` içindeki `tur` (şu anki tur) ile karışmasın |
| — | `bitir` mesajı | "Sınırsız oyunu kurucu bitirir" |
| — | `hamle` nesnesinin biçimi | 5.2'de yoktu |
| — | Lobi `durum`'u | 5.2 sadece oyun içini gösteriyordu |
