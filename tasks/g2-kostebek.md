# G2 (Arda): Köstebek ekranları

Bu dosya Arda'nın yapay zekâsı için bir görev tanımı. Kullanım: "`tasks/g2-kostebek.md`'yi oku ve 1'den başla."
Yazan: G1 (Enes), 2026-10-13.

**Sıra:** önce `tasks/g2-pc.md`'nin 1. ve 2. maddeleri (S5 kapanışı, PC düzeltmeleri), sonra bu dosya. Enes'in kararı: **önce oyun tamamen bitecek, testler sonra.** `g2-pc.md`'deki çok bilgisayarlı ve VPS testleri (3.3, 3.4) oyun bitince yapılacak. Geliştirirken kendi denemelerin (3.1, 3.2) sürsün.

---

## 0. Ne değişti

**Yeni oyunlar geliyor.** Bunlar mevcut oyunun ek modları değil, kendi amacı ve kazananı olan ayrı oyunlar (`docs/Oyunlar.md`). Sıra:
1. Köstebek
2. Rus Ruleti
3. Takım Düellosu
4. Casus
5. Vampir Köylü

**İlki Köstebek, kural motoru ve sunucusu hazır:**
- Kurallar: `Assets/Kurallar/Oyun.Kostebek.cs`.
- Protokol: `docs/Protokol.md`, "Köstebek" bölümü ve komutlar tablosu.
- Tasarım: `docs/Oyunlar.md`, 1. bölüm. Oradaki "Kodlandı" notları kesin kurallar.

**Özetle oyun:**
- Grubun içinde gizli görevli bir köstebek var. Turlar normal çakmak, herkesin gizli görevi var.
- Her 5 turda bir ve herkesin bir kez kullanabileceği erken suçlamayla "Köstebek kim?" oylaması yapılır.
- Köstebek görevini bitirirse ya da yakalanmazsa kazanır.

**Ayarlar iki eksenli oldu:** `Ayarlar.Oyun` (Çakmak / Köstebek) ve yanında `Ayarlar.Mod` (Normal / Temiz / Curcuna), mod aynen duruyor. Bugünkü oyun `OyunTuru.Cakmak`, hiçbir davranışı değişmedi; senin 353 testin yeşil.

## 1. Kural motoru (elden ele)

**`Ayarlar.Oyun = OyunTuru.Kostebek`:**
- Kaos açıksa `Kur` → `GecersizGirdi`.
- Görevler, `GizliGorevler` ne olursa olsun herkeste var.

**Görünümde yeni alanlar (`Gorunum`):**

| Alan | Kime | Ne |
|---|---|---|
| `Oyun` | herkese | Hangi oyun |
| `Rolun` | sadece sana | Köstebekte `Rol.Kostebek`, diğer herkeste `Rol.Yok` |
| `SuclamaHakkin` | sadece sana | Erken suçlama hakkın duruyor mu |
| `Supheliler` | herkese | Yanlış suçlananlar |
| `SonSuclama` | herkese | `Suclama` aşamasında: bu oyunun son oylaması mı |

**Yeni aşama `Asama.Suclama`:** `Oylayanlar`, `OyKullanan` ve `OyVerdin` Grup Kararı'ndaki gibi dolar.

**Komutlar:**
- `Sucla(kim)`: sadece `SoruSorma`'da, herkese oyun boyu 1 kez.
- `SuclamaOyu(kim, hedef)`: kendin ve oyundan çıkan seçilemez.

**Olaylar:**
- `SuclamaBasladi` (`Kim`: erken suçlayan ya da `null`).
- `SuclamaBitti` (`Kim`: suçlanan ya da eşitlikte `null`).

**`OyunuBitir()`:** Köstebek'te **önce son oylamayı başlatır** (aşama `Suclama`, `SonSuclama` true). Oylama sürerken ikinci çağrı `YanlisAsama` döner.

**Oyun sonu:** `Ozet().Kostebek` → köstebek kim, kim kazandı, neden (`Yakalandi`, `GorevBitti`, `Yakalanmadi`, `Kacti`). Oyun bitmeden `null`.

## 2. Ekranlar

1. **Oyun seçimi** (elden ele kurulum ve online lobi): Çakmak / Köstebek. Köstebek seçilince kaos seçeneği kapanır, "gizli görevler" açık görünür ve kilitlenir. Lobide kısa bir tarif.
2. **Rol perdesi:** oyun başında herkese sırayla görev perdesi.
   - Köstebeğe "Köstebeksin!" ve köstebek görevi, diğerlerine normal görevi.
   - **Perde herkese aynı uzunlukta ve aynı dokunuşla:** kimin uzun baktığından köstebek anlaşılmasın.
   - Online'da herkesin kendi telefonunda.
3. **Erken suçlama düğmesi:** `SoruSorma` aşamasında, hakkı olanlara.
   - Online: düğme herkesin kendi telefonunda.
   - Elden ele: telefon A'da; düğme "Kim suçluyor?" diye sorar, hakkı olan seçilir, `Sucla(o)` çağrılır.
4. **Suçlama oylaması:** "Köstebek kim?". Son oylamada "Son oylama!" diye yazsın.
   - Online: herkes kendi telefonunda seçer.
   - Elden ele: Grup Kararı gibi telefon herkese dolaşır, her oy perde arkasında verilir. Kimin kime oy verdiği hiçbir yerde görünmesin.
   - Süre 60 sn (`Ayarlar.SuclamaSuresiSn`). Elden elede saatin nerede durduğu senin kuralın; tartışma süresi dolaşmadan önce işleyebilir.
5. **Oylama sonucu** (`SuclamaBitti`):
   - "X suçlandı… köstebek değildi" → X oyuncu listesinde "şüpheli" etiketi taşır.
   - Eşitlikte "Kimse suçlanmadı".
   - Köstebek yakalanırsa oyun sonuna geçilir.
6. **Oyun sonu:** önce **köstebek açılışı.** Köstebek kimdi, kim kazandı ve neden ("Görevini bitirdi", "Yakalandı", "Yakalanmadı", "Kaçtı"), köstebeğin görevi ve başarılı mıydı. Sonra her zamanki unvanlar ve görevler.
7. **"Oyunu bitir" düğmesi:** Köstebek'te son oylamayı açar; ekran buna göre değişsin.

## 3. Online (protokol: `docs/Protokol.md`)

- **`ayarlar`:** `oyun: "cakmak" | "kostebek"`. Lobide `ayarlar.oyun`. Köstebek + kaos `GECERSIZ_GIRDI`; sırası fark etmez, önce kaosu kapat.
- **Oyunda `durum`:** `oyun`, `supheliler`, `sonSuclama`, `sana.rol` (`"kostebek"` ya da `null`), `sana.suclamaHakki`. Aşama `"suclama"`.
- **Komutlar:** `{ "t": "komut", "tur": n, "komut": "sucla" }`, `{ ..., "komut": "suclamaOyu", "hedef": "p3" }`.
- **Olaylar:** `suclamaBasladi`, `suclamaBitti` (`kim`).
- **`bitir`:** Köstebek'te son oylamayı başlatır. Oylama sürerken ikinci `bitir` `YANLIS_ASAMA`.
- **Oyun sonu:** `ozet.kostebek`: `{ "kim", "kazanan": "grup" | "kostebek", "neden": "yakalandi" | "gorevBitti" | "yakalanmadi" | "kacti" }`.

## 4. Metinler

- Yeni enum değeri için `Metinler.csv` gerekirse ekle: `OyunTuru`, `Rol`, `KostebekKazanan`, `KostebekNedeni`. Testine eklemek senin kararın.
- Köstebek görevleri mevcut `GorevTuru`'ları kullanıyor: `gorev.tur.*` metinleri yeter, yeni görev türü yok.

## 5. Testler

- `Cevirici`: yeni alanlar, `suclama` aşaması, olaylar, `ozet.kostebek`.
- Elden ele akış:
  - Rol perdesinde rol sadece köstebeğe görünüyor.
  - Suçlama oylaması dolaşıyor.
  - "Oyunu bitir" son oylamayı açıyor.
- İstersen `DenemeBotu`'na Köstebek: botlar oy versin.

## 6. Bitti ölçütü

- EditMode'un hepsi yeşil, `SunucuIleOyunTesti` yerel sunucuyla geçiyor.
- Elden elede ve online'da bir Köstebek oyunu baştan sona: erken suçlama, 5. turdan sonraki oylama, yanlış suçlama ("şüpheli"), köstebeğin yakalanması ya da kazanması, köstebek açılışı.
- PR: sadece senin klasörlerin, `.meta`'lar tam. Kural ya da protokolde eksik görürsen Enes'e yaz.
