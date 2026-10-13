# G2 (Arda): Rus Ruleti ekranları

Bu dosya Arda'nın yapay zekâsı için bir görev tanımı. Kullanım: "`tasks/g2-rus-ruleti.md`'yi oku ve 1'den başla."
Yazan: G1 (Enes), 2026-10-13.

**Sıra:** `tasks/g2-pc.md` (1, 2) → `tasks/g2-kostebek.md` → bu dosya. Önce oyun tamamen bitecek, testler sonra.

---

## 0. Ne değişti

**Rus Ruleti (yeni oyunların ikincisi) kural motorunda ve sunucuda hazır:**
- Kurallar: `Assets/Kurallar/Oyun.RusRuleti.cs`.
- Protokol: `docs/Protokol.md`, "Rus Ruleti" bölümü.
- Tasarım: `docs/Oyunlar.md`, 2. bölüm. "Kodlandı" notları kesin kurallar.

**Özetle oyun:**
- Çakmak bir tabanca. Normal çakmak turundan sonra düelloyu kaybeden tetiği çeker ya da oyun boyu bir kez başkasına devreder.
- Patlarsa bir can gider, can biterse elenir. Can ayarı: 1 = Rus Ruleti, 3 = Son Çakmak.
- 3 kişi kalınca final başlar: sorusuz ikili düellolar. Son kalan kazanır.

**Senin tarafını ilgilendiren iki değişiklik daha:**
- **`KararBekleniyor` olayı (S3 hatası, düzeldi).** Mini oyunun ya da Grup Kararı oylamasının süresi dolunca oyun İkiye Katla ya da Bedel kararına geçiyordu ama olay yaymıyordu. Sunucu da durum yollamıyordu: online'da B "Bedel öder misin?" sorusunu hiç görmüyordu. Artık geçişte `kararBekleniyor` (`kim`: karar verecek) geliyor, ardından durum. Senin ekranın durumdan çizdiği için ek iş yok; istersen animasyon için kullan. Elden elede zaten sorun yoktu.
- **`Oyun.AyarlarUyumlu(ayarlar)`.** Hangi ayarın hangisiyle birlikte olamayacağı tek yerde: Temiz + Cesur, Köstebek + kaos, Rus Ruleti + ceza/görev/kaos. Kurulum ekranında seçenekleri buna göre kapatabilirsin, kuralı kopyalamana gerek yok.

## 1. Kural motoru (elden ele)

**`Ayarlar.Oyun = OyunTuru.RusRuleti`, `Ayarlar.Can` (1–3):** ceza `Kapali`, `GizliGorevler` ve `KaosTurlari` `false` olmalı, yoksa `Kur` `GecersizGirdi` döner. `TurSayisi` yok sayılır.

**Görünüm:**

| Alan | Ne |
|---|---|
| `ToplamTur` | Bu oyunda `0` |
| `Isi` | Tabancanın ısısı (Kızgın Çakmak bu oyunda kapalı) |
| `TetikCeken` | `Asama.Tetik`'te tetiği çekecek kişi |
| `Final` | 3 kişi kaldı |
| `SonMiniOyun` | `Tetik` aşamasında da dolu: kim kaybetti görünsün |

`OyuncuDurumu.Can`, `Elendi`, `DevretmeHakki` herkese açık.

**Akış:**
- **Normal tur:** soru → C → mini oyun → `Ifsa`/`Gume` (6 sn) → `Tetik`.
- **Final:** `MiniOyun` (A = B, soru yok) → `Tetik`, sonuç ekranı yok.
- **Süre:** `Tetik`'te karar süresi `HamleSuresiSn`; dolunca tetik kendiliğinden çekilir.

**Komutlar:**
- `TetikCek(kim)`.
- `TetikDevret(kim, hedef)`: oyun boyu 1 kez; hedef aktif ve elenmemiş biri, kendin olamaz.

**Olaylar:**
- `TetikBasladi` (`Kim`): tetiği çekecek.
- `TetikBos` / `TetikPatladi` / `Elendi` (`Kim`).
- `TetikDevredildi` (`Kim`: devreden, yeni alan **`Olay.Hedef`**: devralan).
- `FinalBasladi`.

**`OyunuBitir()`:** hemen bitirir. Kazanan en çok canı olan, eşitlikte yok.

**Oyun sonu:** `Ozet().RusRuleti` → `Kazanan` (son kalan ya da `null`), `ElenmeSirasi`.

## 2. Ekranlar

1. **Oyun seçimi:** Rus Ruleti + can seçimi (1 / 2 / 3; 3'ün yanında "Son Çakmak").
   - Seçilince ceza, görevler ve kaos kapanıp kilitlenir.
   - Tur sayısı seçeneği gizlenir ("Son kalan kazanır").
2. **Oyuncu listesi:** her oyuncunun canı (kalpler ya da mermiler). Elenenler soluk ya da "elendi" etiketiyle, seçilemez. Devretme hakkı olanlarda küçük bir işaret.
3. **Tabanca göstergesi:** her ekranda tabancanın ısısı (`Isi`): soğuk / ılık / sıcak / kızgın. Çakmak ısısı gibi ama tabanca görseliyle.
4. **Tetik ekranı** (`Asama.Tetik`):
   - **Tetiği çekecek kişide:** "Tetiği çek" ve (hakkı varsa) "Devret →" ile oyuncu seçimi. Geri sayım: süre dolunca kendisi çeker.
   - **Elden ele:** telefon kaybedende, ekran herkesin önünde (açık bilgi, perde yok).
   - **Online:** herkes izler, sadece çekecek kişide düğmeler.
   - **Sonuç:** `TetikBos` → "Tık…", `TetikPatladi` → "BAM! −1 can", `Elendi` → "Elendi". Kısa animasyon, sonra sıradaki ekran durumdan.
5. **Final:** `FinalBasladi` gelince "FİNAL! Son 3 kişi" duyurusu. Final düellosunda soru ekranı yok, doğrudan mini oyun, kaybeden tetiğe.
6. **Oyun sonu:** kazanan (ya da "Kazanan yok": kurucu erken bitirdi ve canlar eşit), elenme sırası (ilk elenen en altta), sonra her zamanki unvanlar.

## 3. Online (protokol: `docs/Protokol.md`)

- **`ayarlar`:** `oyun: "rusRuleti"`, `can: 1-3`. Uyumsuz ayar `GECERSIZ_GIRDI`.
- **Oyunda `durum`:** `tetikCeken`, `final`, `isi` (tabanca), `toplamTur: 0`. `oyuncular[]` içinde `can`, `elendi`, `devretmeHakki`. Aşama `"tetik"`.
- **Komutlar:** `{ "t": "komut", "tur": n, "komut": "tetikCek" }`, `{ ..., "komut": "tetikDevret", "hedef": "p3" }`.
- **Olaylar:** `tetikBasladi`, `tetikBos`, `tetikPatladi`, `elendi`, `tetikDevredildi` (yeni alan `hedef`), `finalBasladi`, `kararBekleniyor`.
- **Oyun sonu:** `ozet.rusRuleti`: `{ "kazanan": "p3" | null, "elenmeSirasi": [...] }`.

## 4. Testler

- `Cevirici`: yeni alanlar, `tetik` aşaması, olaylar ve `hedef`, `ozet.rusRuleti`.
- Elden ele akış:
  - Tetik ekranı kaybedende açılıyor.
  - Devret başkasına geçiyor.
  - Elenen hiçbir seçimde görünmüyor.
  - Final duyurusu bir kez geliyor.
- İstersen `DenemeBotu`'na tetik çekme.

## 5. Bitti ölçütü

- EditMode'un hepsi yeşil, `SunucuIleOyunTesti` yerel sunucuyla geçiyor.
- Elden elede ve online'da bir Rus Ruleti oyunu baştan sona: devret, patlama, eleme, final, kazanan ekranı. Can 1 ve can 3 ile birer kez.
- PR: sadece senin klasörlerin, `.meta`'lar tam. Kural ya da protokolde eksik görürsen Enes'e yaz.
