# G2 (Arda): Vampir Köylü ekranları

Bu dosya Arda'nın yapay zekâsı için bir görev tanımı. Kullanım: "`tasks/g2-vampir.md`'yi oku ve 1'den başla."
Yazan: G1 (Enes), 2026-10-14.

**Sıra:**
1. `tasks/g2-pc.md` (1. ve 2. maddeler)
2. `tasks/g2-kostebek.md`
3. `tasks/g2-rus-ruleti.md`
4. `tasks/g2-takim.md`
5. `tasks/g2-casus.md`
6. bu dosya

Önce oyun tamamen bitecek, testler sonra.

---

## 0. Ne değişti

**Vampir Köylü (yeni oyunların beşincisi ve sonuncusu) kural motorunda ve sunucuda hazır:**
- Kurallar: `Assets/Kurallar/Oyun.Vampir.cs`. Sohbet kuralları: `Assets/Kurallar/Oyun.Sohbet.cs`.
- Sorular: `Assets/Icerik/VampirSorulari.csv` (paket değil, paket listesinde gösterme).
- Protokol: `docs/Protokol.md`, "Vampir Köylü" ve "Sohbet" bölümleri.
- Tasarım: `docs/Oyunlar.md`, 5. bölüm. "Kodlandı" notları kesin kurallar.

**Özetle oyun (6–10 kişi):**
- Gizli roller: vampir (6–7 kişide 1, 8–10 kişide 2), doktor, gözcü, köylüler. Vampirler birbirini bilir.
- Oyun gündüzle başlar. Gündüz canlıların yarısı kadar normal çakmak turu, sonra linç: en çok oyu alan elenir.
- Gece herkes bir seçim yapar: vampir kurban, doktor korunacak kişi, gözcü bakılacak kişi, köylü "birini düşünür".
- Sabah kurban ölür (doktor koruduysa kimse). Yeni gün.
- Bütün vampirler ölürse köylüler, vampirler sayıca eşitlenirse vampirler kazanır.

## 1. Kural motoru (elden ele)

**`Ayarlar.Oyun = OyunTuru.Vampir`:**
- En az 6 kişi (`Kur` → `OyuncuSayisiGecersiz`).
- `GizliGorevler` ve `KaosTurlari` olamaz (`GecersizGirdi`).
- `Ayarlar.RolAcilir` (varsayılan `true`): elenenin rolü herkese açılır mı.
- Öneri soruları: `VampirSorulari.csv`'yi `Paket.Ayristir` ile oku, `OneriSorulari`'na ver (seçili paketler yerine). Sesli cezaları `Cezalar.csv`'den nasıl veriyorsan öyle.

**Aşamalar:**
- Gündüz: normal çakmak aşamaları.
- `Asama.Linc`: canlılar `LincOyu(kim, hedef)`.
- `Asama.Gece`: canlılar `GeceSecimi(kim, hedef)`.
- Her ikisinde: `Oylayanlar` (seçecek canlılar), `OyKullanan`, `OyVerdin`, adaylar sana `Secilebilir`'de (rolüne göre).
- Elden elede her seçimden sonra süre baştan başlar.

**Görünüm (sadece sana):**
- `Rolun`: `Vampir`, `Doktor`, `Gozcu`, `Koylu`.
- `Vampirler`: vampirlerde bütün vampirler (sen dahil).
- `VampirSecimleri`: vampirlerde, gece, vampirlerin seçimleri (`Kim`, `Hedef`).
- `GozcuBilgilerin`: gözcüde, baktığı herkes (`Kim`, `Vampir`). Seçer seçmez dolar.
- `KendiniKoruyabilir`: doktorda, kendini koruma hakkı (oyun boyu 1).

**Görünüm (herkese):**
- `Gun`, `KalanGunduzTuru` (bu tur hariç; 0 = sıradaki linç). `ToplamTur` 0.
- `OyuncuDurumu.Elendi`, `OyuncuDurumu.AcikRol` (rol açılması kapalıysa `null`).

**Olaylar:**
- `LincBasladi`, `LincBitti` (`Kim`: elenen ya da `null`).
- `GeceBasladi`, `GeceBitti` (`Kim`: ölen ya da `null` = "kimse ölmedi").

**Oyun sonu:** `Ozet().Vampir` → `Kazanan` (`Koylu`, `Vampir`; kurucu bitirdiyse `null`), `Roller` (herkesin rolü).

**Sohbet kuralları motorda** (online'da sunucu kullanıyor, elden elede sohbet yok):
- `SohbeteYazabilir(kim)`: Vampir'de gece ve ölüler genele/DM'e yazamaz.
- `KanalAlicilari(kim, SohbetKanali.Takim | Vampir | Oluler)`: kanala kimler okur; yazamıyorsan `null`.

## 2. Ekranlar

1. **Oyun seçimi:** Vampir Köylü seçilince görevler ve kaos kapanıp kilitlenir. 6 kişiden azsa "En az 6 kişi". Ayar: "Ölenin rolü açılsın" (açık / kapalı).
2. **Rol perdesi** (elden elede herkese sırayla, online'da başta): rol, kısa açıklama, vampirse diğer vampir(ler).
   - Perdenin boyu ve süresi herkeste aynı olsun.
3. **Gündüz:** normal çakmak ekranları. Üstte "Gün 2 · linçe 1 tur". Ölüler listede soluk (rol açıksa rolüyle).
4. **Linç:** "Linç: kim gitsin?" Canlılar listesi, oy sayısı (`OyKullanan` / `Oylayanlar`). Elden elede telefon sırayla dolaşır (Grup Kararı gibi). Sonuç: "Ali linç edildi (köylüydü)" ya da "Kimse linç edilmedi".
5. **Gece (elden ele, en zor kısım):** telefon herkese tek tek gider, **herkes bir seçim yapar**:
   - Vampir: "Kurbanını seç" (diğer vampirin seçimi görünür).
   - Doktor: "Bu gece kimi koruyorsun?" (kendini koruma hakkı varsa kendisi de listede).
   - Gözcü: "Kime bakıyorsun?" → hemen "Vampir!" / "Vampir değil".
   - Köylü: "Birini düşün" (seçimi işe yaramaz).
   - Bütün ekranlar aynı görünsün ve benzer sürede bitsin. Kimin uzun düşündüğünden rol anlaşılmasın. Gözcünün cevabı da köylünün ekranındaki gibi bir "Tamam" düğmesiyle kapansın.
   - Her seçimden sonra "Telefonu sıradakine ver" perdesi.
6. **Sabah:** "Gece Ayşe öldü (gözcüydü)" ya da "Bu gece kimse ölmedi".
7. **Ölüler:** izleyici. Seçim ve oy ekranları gelmez. Online'da genel sohbete yazamaz, okur; "Ölüler" sekmesinde kendi aralarında yazar.
8. **Oyun sonu:** "Köylüler kazandı" / "Vampirler kazandı" / "Oyun yarıda bitti", herkesin rolü. Sonra her zamanki unvanlar.

## 3. Online (protokol: `docs/Protokol.md`)

- **`ayarlar`:** `oyun: "vampir"`, `rolAcilir: true | false`. 6'dan azla `baslat` → `OYUNCU_SAYISI_GECERSIZ`.
- **Paket seçimi Vampir'de kullanılmaz,** öneriler sunucunun Vampir sorularından. Lobide paket seçimini gizle ya da "Bu oyunda kullanılmaz" yaz.
- **Komutlar:** `{ "komut": "lincOyu", "hedef": "p3" }`, `{ "komut": "geceSecimi", "hedef": "p3" }`.
- **`durum`:**
  - `gun`, `kalanGunduzTuru`, `oyuncular[].acikRol`.
  - `sana.rol`, `sana.vampirler`, `sana.vampirSecimleri`, `sana.gozcuBilgilerin`, `sana.kendiniKoruyabilir`.
- **Aşamalar:** `"linc"`, `"gece"`.
- **Olaylar:** `lincBasladi`, `lincBitti`, `geceBasladi`, `geceBitti` (`kim`).
- **Sohbet:**
  - Kanallar: `kime: "vampir"` (gece, vampirler) ve `kime: "oluler"` (ölüler). Gelen mesajda `kime` kanalın adı.
  - Gece genel sohbet kapalı: yazma kutusunu kapat, "Gece: herkes uyuyor" yaz.
  - Ölü genele yazamaz: kutuyu kapat, "Ölüler sekmesinde yaz" yaz.
  - Yazılamayan kanala yollarsan `GECERSIZ_GIRDI` döner. Kutuyu baştan kapatmak daha iyi.
  - Takım sohbeti aynı yoldan gidiyor, değişen bir şey yok.
- **Oyun sonu:** `ozet.vampir`: `{ "kazanan": "koylu" | "vampir" | null, "roller": [ { "kim", "rol" } ] }`.

## 4. Testler

- `Cevirici`: yukarıdaki alanların hepsi, aşamalar `linc` / `gece`, olaylar, `ozet.vampir`, lobide `ayarlar.rolAcilir`.
- Rol perdesi: rol sadece sahibinin perdesinde. Vampir olmayanda vampir listesi yok.
- Gece dolaşması (elden ele): herkes bir seçim yapıyor, ölüler atlanıyor, gözcünün cevabı sadece gözcünün perdesinde.
- Online: gece genel sohbet kapalı, ölü genele yazamıyor, vampir ve ölüler sekmeleri sadece ilgililerde.

## 5. Bitti ölçütü

- EditMode'un hepsi yeşil, `SunucuIleOyunTesti` yerel sunucuyla geçiyor.
- Elden elede ve online'da birer Vampir Köylü oyunu baştan sona, biri rol açık, biri kapalı. Biri köylüler, biri vampirler kazanarak bitsin.
- PR: sadece senin klasörlerin, `.meta`'lar tam. Kural ya da protokolde eksik görürsen Enes'e yaz.
