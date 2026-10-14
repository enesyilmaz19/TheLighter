# G2 (Arda): Çakmak Gecesi ekranları

Bu dosya Arda'nın yapay zekâsı için bir görev tanımı. Kullanım: "`tasks/g2-cakmak-gecesi.md`'yi oku ve 1'den başla."
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

**Çakmak Gecesi (eski adı Vampir Köylü; yeni oyunların beşincisi ve en büyüğü) kural motorunda ve sunucuda hazır:**
- Kurallar: `Assets/Kurallar/Oyun.CakmakGecesi.cs`. Sohbet kuralları: `Assets/Kurallar/Oyun.Sohbet.cs`.
- Sorular: `Assets/Icerik/GeceSorulari.csv` (paket değil, paket listesinde gösterme).
- Protokol: `docs/Protokol.md`, "Çakmak Gecesi" ve "Sohbet" bölümleri.
- Tasarım: `docs/Oyunlar.md`, 5. bölüm: rollerin ne yaptığı, gece sırası, kazanma. **Oradaki tablolar kesin kurallar**, ekran metinlerini oradan yaz.

**Özetle oyun (6–18 kişi):**
- Taraflar **Masumlar** ve **Katiller**, bir de sadece kendisi kazanan **tarafsızlar**. 24 rol.
- Roller kişi sayısına göre otomatik dağılır, kurucu istemediği rolleri kapatır.
- Oyun gündüzle başlar: canlıların yarısı kadar çakmak turu, sonra linç.
- Gece herkes bir seçim yapar (yeteneği olmayan da "birini düşünür"), sabah ölenler açıklanır.

**Önemli:** oda artık 18 kişilik (`Oyun.GeceEnFazlaOyuncu`). Diğer oyunlar 10'da kaldı (`Oyun.EnFazlaOyuncu`): 10'dan fazla kişiyle başlatılırsa `OYUNCU_SAYISI_GECERSIZ`. Kurulum ekranı (elden ele) Çakmak Gecesi seçilince 18'e kadar oyuncu eklemeye izin vermeli. Lobi ve oyuncu listesi 18 kişiye sığmalı.

## 1. Kural motoru (elden ele)

**Ayarlar (`OyunTuru.CakmakGecesi`):**
- 6–18 kişi. `GizliGorevler` ve `KaosTurlari` olamaz.
- `RolAcilir` (varsayılan `true`): ölenin rolü herkese açılır mı.
- `GecelikKurban` (1–3, varsayılan 1): katillerin gecede öldürdüğü kişi sayısı.
- `KapaliRoller`: kurucunun kapattığı roller. Bu oyunun rolleri `Oyun.GeceRolleri`, tarafı `Oyun.TarafI(rol)`. Masum ve Katil kapatılamaz.
- Öneri soruları: `GeceSorulari.csv`'yi `Paket.Ayristir` ile oku, `OneriSorulari`'na ver (seçili paketler yerine).

**Aşamalar:**
- Gündüz: normal çakmak aşamaları.
- `Asama.Linc`: `LincOyu(kim, hedef)`. Linç sürerken Suikastçı `Suikast(kim, hedef, rol)` da yapabilir.
- `Asama.Gece`: `GeceSecimi(kim, hedef, ikinci)`.
  - `hedef = null` pas demek.
  - Katil tarafında `hedef` kurban oyu, `ikinci` rolün yeteneği.
  - Bombacı kendini seçerse bombaları patlatır.
- Her ikisinde `Oylayanlar`, `OyKullanan`, `OyVerdin`.
- Adaylar `Secilebilir`'de, katillerin ikinci hedef adayları `IkinciSecilebilir`'de. Ekranda sadece bu listeleri göster, kendin süzme.
- Elden elede her seçimden sonra süre baştan başlar.

**Görünüm (sadece sana):**
- `Rolun`: kendi rolün. Deli'ye sandığı rol görünür, Deli bunu bilmez. Ekran Deli'yi asla ayrı göstermesin.
- `Katiller`: katillerde, bütün katiller rolleriyle.
- `KatilSecimleri`: katillerde, gece, kurban oyları.
- `Bulgularin`: gece bakışlarının sonuçları; Dedektif, Casus, Bekçi ve Falcı'da dolar.
- `KalanHakkin`: sınırlı yeteneğin kalan hakkı.
- `CellatHedefin`, `Bombalilar`, `KulakSorusu` (Kulak, sadece Gume aşamasında).

**Görünüm (herkese):**
- `Gun`, `KalanGunduzTuru`.
- `OyuncuDurumu.Elendi`, `AcikRol` (açılmadıysa `null`), `Susturuldu`.

**Olaylar:**
- `LincBasladi`, `LincBitti` (`Kim`: elenen ya da `null`), `Suikast` (`Kim`: ölen).
- `GeceBasladi`, gece ölenler için birer `Elendi`, sonra `GeceBitti`.

**Oyun sonu:** `Ozet().Gece`:
- `Kazanan`: Masumlar, Katiller, SeriKatil, Bombaci, Soytari, Cellat, Berabere ya da `null` (kurucu bitirdi).
- `Kazananlar`: kazanan oyuncular.
- `Roller`: herkesin gerçek rolü, Deli dahil.

**Sohbet kuralları motorda** (online'da sunucu kullanıyor):
- `SohbeteYazabilir(kim)`: gece, ölüler ve o gün susturulan genele yazamaz.
- `KanalAlicilari(kim, SohbetKanali.Takim | Katil | Oluler)`.

## 2. Ekranlar

1. **Oyun seçimi ve ayarlar:**
   - Görevler ve kaos kapanıp kilitlenir.
   - "En az 6 kişi" uyarısı.
   - Ayarlar: "Ölenin rolü açılsın", "Gecelik kurban: 1 / 2 / 3", "Roller" (taraf taraf liste, her rolün yanında aç/kapat; Masum ve Katil kilitli).
2. **Rol perdesi:** rolün adı, tarafı, ne yaptığı (`docs/Oyunlar.md` 5'teki tablolar). Katilse diğer katiller ve rolleri. Cellat'sa hedefi. Perdenin boyu ve süresi herkeste aynı.
3. **Gündüz:** normal çakmak ekranları.
   - Üstte "Gün 2 · linçe 1 tur".
   - Ölüler listede soluk, rolü açıldıysa rolüyle. Susturulanın yanında 🤐.
   - Kulak, güme ekranında soruyu küçük bir "Duydun:" kutusunda görür, başkası görmez.
4. **Linç:**
   - "Linç: kim gitsin?" Canlılar listesi, oy sayısı. Elden elede sırayla dolaşır.
   - Susturulan oy veremez.
   - Suikastçı'nın ekranında bir "Suikast" düğmesi: kişi ve rol seçer, onaylar.
5. **Gece (elden ele, en zor kısım):** telefon herkese tek tek gider, herkes bir seçim yapar.
   - Ekranlar rolüne göre: "Kurbanını seç", "Kimi koruyorsun?", "Kime bakıyorsun?", "Bomba koy / Patlat", "Birini düşün"...
   - Katillerde ikinci bir seçim: yetenekleri.
   - **Bütün ekranlar aynı düzende olsun ve benzer sürede bitsin.** Kimin uzun düşündüğünden rol anlaşılmasın.
   - Pas düğmesi herkeste.
   - **Önceki gecenin sonucu (Bulgularin) kişinin bir sonraki perdesinde gösterilir**, sadece ona: "Dün gece: Ayşe şüpheli". Yeni bulgu yoksa da aynı uzunlukta bir ekran gelsin.
   - Her seçimden sonra kısa bir "Telefonu sıradakine ver" perdesi. 18 kişide gece uzun sürer, geçişleri kısa tut.
6. **Sabah:** "Gece Ayşe ve Ali öldü (rolleri…)" ya da "Bu gece kimse ölmedi". Rolü açılmayan için "rolü bilinmiyor".
7. **Ölüler:** izleyici. Seçim ve oy ekranları gelmez. Online'da genel sohbete yazamaz, okur; "Ölüler" sekmesinde kendi aralarında yazar.
8. **Oyun sonu:** kazanan ("Masumlar kazandı", "Soytarı kazandı", "Berabere"…), kazananlar, herkesin rolü (Deli dahil: "Ali Deli'ydi, kendini Dedektif sanıyordu"). Sonra her zamanki unvanlar.

## 3. Online (protokol: `docs/Protokol.md`)

- **`ayarlar`:** `oyun: "cakmakGecesi"`, `rolAcilir`, `gecelikKurban`, `kapaliRoller: ["palyaco", ...]`.
  - Lobide `ayarlar` içinde aynı alanlar.
  - Paket seçimi bu oyunda kullanılmaz; lobide gizle ya da "Bu oyunda kullanılmaz" yaz.
- **Komutlar:**
  - `lincOyu {hedef}`.
  - `geceSecimi {hedef?, ikinci?}`: `hedef` yoksa pas.
  - `suikast {hedef, rol}`.
- **`durum`:**
  - Herkese: `gun`, `kalanGunduzTuru`, `oyuncular[].acikRol`, `oyuncular[].susturuldu`, `ikinciSecilebilir`.
  - `sana`: `rol`, `katiller`, `katilSecimleri`, `bulgularin`, `kalanHakkin`, `cellatHedefin`, `bombalilar`, `kulakSorusu`.
- **Aşamalar:** `"linc"`, `"gece"`.
- **Olaylar:** `lincBasladi`, `lincBitti`, `suikast`, `geceBasladi`, `elendi`, `geceBitti`.
- **Sohbet:**
  - Kanallar: `kime: "katil"` (gece, katiller) ve `kime: "oluler"` (ölüler).
  - Gece genel sohbet kapalı: kutuyu kapat, "Gece: herkes uyuyor" yaz.
  - Ölü ya da susturulan genele yazamaz; kutuyu kapat ve sebebini yaz.
  - Takım sohbeti aynı.
- **Oyun sonu:** `ozet.gece`: `{ "kazanan", "kazananlar", "roller" }`.

## 4. Testler

- `Cevirici`: yukarıdaki bütün alanlar, `bulgularin` nesneleri, aşamalar, olaylar, `ozet.gece`, lobide yeni ayarlar.
- Rol perdesi: rol sadece sahibinin perdesinde. Katil olmayanda katil listesi yok. Deli'nin perdesinde sandığı rol.
- Gece dolaşması (elden ele): herkes bir seçim yapıyor, ölüler atlanıyor, bulgular sadece sahibinin perdesinde.
- Kurulum: 18 kişi eklenebiliyor (Çakmak Gecesi), diğer oyunlarda 10.
- Online: gece genel sohbet kapalı, ölü ve susturulan genele yazamıyor, katil ve ölüler sekmeleri sadece ilgililerde.

## 5. Bitti ölçütü

- EditMode'un hepsi yeşil, `SunucuIleOyunTesti` yerel sunucuyla geçiyor.
- Elden elede ve online'da birer Çakmak Gecesi oyunu baştan sona.
  - Biri 6 kişiyle rol açık, biri 12+ kişiyle rol kapalı ve gecelik 2 kurban.
  - Biri katiller, biri masumlar ya da bir tarafsız kazanarak bitsin.
- PR: sadece senin klasörlerin, `.meta`'lar tam. Kural ya da protokolde eksik görürsen Enes'e yaz.
