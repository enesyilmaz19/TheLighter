# G2 (Arda): Casus ekranları

Bu dosya Arda'nın yapay zekâsı için bir görev tanımı. Kullanım: "`tasks/g2-casus.md`'yi oku ve 1'den başla."
Yazan: G1 (Enes), 2026-10-14.

**Sıra:**
1. `tasks/g2-pc.md` (1. ve 2. maddeler)
2. `tasks/g2-kostebek.md`
3. `tasks/g2-rus-ruleti.md`
4. `tasks/g2-takim.md`
5. bu dosya

Önce oyun tamamen bitecek, testler sonra.

---

## 0. Ne değişti

**Casus (yeni oyunların dördüncüsü, Imposter da bu) kural motorunda ve sunucuda hazır:**
- Kurallar: `Assets/Kurallar/Oyun.Casus.cs`. İçerik: `Assets/Kurallar/CasusIcerik.cs`.
- Protokol: `docs/Protokol.md`, "Casus" bölümü.
- Tasarım: `docs/Oyunlar.md`, 4. bölüm. "Kodlandı" notları kesin kurallar.

**Özetle oyun:**
- Herkes gizli bir mekânı bilir ("Uçak") ve o mekândaki rolünü ("Hostes"). Casus bilmez.
- Kelime ayarında (Imposter) mekân yerine tek bir kelime, rol yok; kategori herkese açık ("Hayvanlar").
- Turlar normal çakmak, sorular mekâna göre.
- Her 4 turda "Casus kim?" oylaması. Yakalanan casusun son şansı var: listeden mekânı tahmin eder.
- Casus istediği an bir kez "Mekânı biliyorum" diyebilir. Bilirse kazanır, bilemezse kaybeder.

**Köstebek'le ortak:** "kim?" oylaması aynı kod. `Suclama` aşaması, `SuclamaOyu`, `Supheliler`, `SonSuclama` iki oyunda da aynı çalışıyor. Köstebek için yaptığın oylama ekranı Casus'ta da kullanılabilir. Sadece başlık farklı ("Köstebek kim?" / "Casus kim?"), Casus'ta erken suçlama düğmesi yok.

## 1. Kural motoru (elden ele)

**`Ayarlar.Oyun = OyunTuru.Casus`, `Ayarlar.Casus = CasusTuru.Mekan | CasusTuru.Kelime`:**
- Curcuna, `GizliGorevler` ve `KaosTurlari` olamaz (`Kur` → `GecersizGirdi`). Ceza açılabilir.

**Görünüm:**
- `Rolun == Rol.Casus`: sadece casusta.
- `Sirrin`: casus olmayana mekân ya da kelime; casusa `null`.
- `MekanRolun`: casus olmayana rolü (herkese ayrı); kelimede ve casusa `null`.
- `CasusListesi`: herkese, oyun boyunca. Mekânda bütün mekânlar, kelimede kategorinin kelimeleri.
- `CasusKategori`: kelimede kategori, mekânda `null`.
- `Yakalanan`: `CasusTahmini` aşamasında yakalanan casus (herkese açık).

**Komut:** `CasusTahmini(kim, tahmin)`, `tahmin` listeden bir öğe.
- Yakalanmadan: her aşamada (düellonun ortasında da), bir kez. Oyun hemen biter.
- `CasusTahmini` aşamasında son şans. Süre (`Oyun.CasusTahminiSn`, 30 sn) dolarsa grup kazanır.
- Casus değilse `SiraSendeDegil`, listede yoksa `GecersizGirdi`.

**Olaylar:**
- `CasusYakalandi` (`Kim`: casus): son şans başladı.
- `CasusTahminEtti` (`Kim`: casus): oyun bitiyor, sonuç özette.

**Oyun sonu:** `Ozet().Casus` → `Casus`, `Kazanan` (`Grup` / `Casus`), `Neden`, `Sir`, `Tahmin` (tahmin etmediyse `null`).
- `Neden`: `Yakalandi`, `YanlisTahmin`, `Kacti`, `Bildi`, `SonSanstaBildi`, `Yakalanmadi`.

## 2. Ekranlar

1. **Oyun seçimi:** Casus seçilince Curcuna, görevler ve kaos kapanıp kilitlenir. Alt seçenek: "Mekân" / "Kelime".
2. **Rol perdesi** (elden elede herkese sırayla, online'da başta):
   - Casus olmayan: "Uçak, sen: Hostes" (kelimede: "Kelime: Penguen").
   - Casus: "Casussun". Kelimede kategori de görünsün ("Kategori: Hayvanlar").
   - Perdenin boyu ve süresi herkeste aynı olsun. Casusun perdesi kısa kalırsa yanındaki anlar.
3. **Liste paneli:** oyun boyunca açılıp kapanan bir panel, `CasusListesi` (kelimede başlığı kategori). Herkeste aynı.
   - Oyuncular listede işaret koyabilsin ("bu olamaz" üstünü çiz) iyi olur. Bu sadece istemcide, motora gitmez.
4. **"Mekânı biliyorum" düğmesi:** her ekranda, **herkeste** görünür. Elden elede de online'da da. Kimse düğmeye bakarak casusu anlamasın.
   - Casus olmayan basarsa: listeyi açıp seçtir, sonra hiçbir şey olmasın ("Sen casus değilsin" deme, yanındaki görür). En kolayı: seçimden sonra listeyi kapat.
   - Casus basarsa: listeden seç → onay ("Emin misin? Yanlışsa kaybedersin") → `CasusTahmini`.
5. **Oylama:** Köstebek'inkiyle aynı ekran, başlık "Casus kim?". Erken suçlama düğmesi yok.
6. **Son şans:** `CasusYakalandi` olayında herkese "Casus: Ali! Son şansı…". Casus listeden seçer, diğerleri bekler, 30 sn sayaç.
7. **Oyun sonu:** casus kimdi, mekân/kelime neydi, tahmini neydi, kim kazandı (`Neden`'e göre bir cümle). Sonra her zamanki unvanlar.

## 3. Online (protokol: `docs/Protokol.md`)

- **`ayarlar`:** `oyun: "casus"`, `casus: "mekan" | "kelime"`. Lobi `durum`'unda `ayarlar.casus`.
- **Oyunda `durum`:** `sana.rol: "casus"` (sadece casusta), `sana.sir`, `sana.mekanRolu`, `casusListesi`, `casusKategori`, `yakalanan`.
- **Komut:** `{ "t": "komut", "tur": …, "komut": "casusTahmini", "tahmin": "Uçak" }`.
  - Casus olmayana `SIRA_SENDE_DEGIL` döner. Hata mesajını gösterme, yukarıdaki gibi sessizce kapat.
- **Olaylar:** `casusYakalandi`, `casusTahminEtti` (`kim`: casus).
- **Aşama:** `"casusTahmini"`.
- **Oyun sonu:** `ozet.casus`: `{ "kim", "kazanan": "grup" | "casus", "neden", "sir", "tahmin" }`.

## 4. Testler

- `Cevirici`: `casusListesi`, `casusKategori`, `yakalanan`, `sana.sir`, `sana.mekanRolu`, `ozet.casus`, lobide `ayarlar.casus`, aşama `casusTahmini`.
- Rol perdesi: casusa sır hiç yazılmıyor (görünüm `null`; ekranda da boş kalmalı).
- "Mekânı biliyorum" düğmesi casus olmayanda da görünüyor, basınca hiçbir şey değişmiyor.
- Elden ele akış: oylamada casus yakalanınca son şans ekranı, tahmin, oyun sonu.

## 5. Bitti ölçütü

- EditMode'un hepsi yeşil, `SunucuIleOyunTesti` yerel sunucuyla geçiyor.
- Elden elede ve online'da birer Casus oyunu baştan sona, bir kez mekânla, bir kez kelimeyle. Biri "Mekânı biliyorum" ile, biri son şansla bitsin.
- PR: sadece senin klasörlerin, `.meta`'lar tam. Kural ya da protokolde eksik görürsen Enes'e yaz.
