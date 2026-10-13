# Oyunlar: çakmak kuralları entegre edilmiş beş yeni oyun

Durum: **taslak, Enes'in kararlarını bekliyor** (2026-10-13). Onaylananlar `docs/Ana Plan.md`'ye işlenir, ekranlar için Arda'yla konuşulur.
Karar (Enes): bunlar mevcut oyunun ek modları değil, **kendi amacı ve kazananı olan ayrı oyunlar**. Çıkıştan önce gelecekler, online ve elden elede oynanacaklar. Imposter ile Casus tek oyunda, Son Çakmak ile Rus Ruleti tek oyunda birleşti.

---

## 0. Ortak zemin

**Çakmak döngüsü** her oyunda aynı kalır, değişen ona yüklenen anlam:

1. A, B'ye gizli bir soru sorar.
2. B cevabı olan C'ye çakmağı verir.
3. B ile C mini oyun oynar.
4. C kazanırsa soru ifşa olur, B kazanırsa güme gider.
5. Sıra C'ye geçer.

**Ayarlar iki eksenli olur:**

- **Oyun:** Çakmak (bugünkü oyun) · Vampir Köylü · Casus · Köstebek · Rus Ruleti · Takım Düellosu.
- **Mod:** Normal · Temiz · Curcuna. Mod soruların nereden geldiğini ve filtreyi belirler; her oyunda geçerli, bazı birleşimler hariç (her oyunda yazılı).

**Hep geçerli:**

- 4–10 kişi; bazı oyunlarda en az sayı daha yüksek.
- Temiz filtresi, sohbet, kopma ve AFK kuralları, atma.

**Ortak yeni parçalar (kural motorunda bir kez yazılır, her oyun kullanır):**

| Parça | Ne | Bugün ne var |
|---|---|---|
| **Gizli rol** | Her oyuncuya sadece kendisinin gördüğü rol (vampir, casus, köstebek, takım). Elden elede perdeyle gösterilir | Gizli görev aynı yoldan gidiyor (`sana.gorevin`, görev perdesi) |
| **Eleme** | Elenen izleyici olur: seçilemez, sırası gelmez | Oyundan çıkan oyuncu tam olarak bu (`cikti`) |
| **Oylama** | Herkes ya da bir grup oy verir, sonuç ilan edilir | Grup Kararı oylaması (kimin ne dediği gizli) |
| **Gizli sayaç** | Oyunculara görünmeyen bir eşik | Kızgın Çakmak'ın eşiği |
| **Az kişi kuralı** | Aktif oyuncu 4'ün altına düşünce çakmak turu olamaz (B, A'yı gösteremez, C ne A ne B). Her oyunda ne olacağı yazılı | Yok |

**Ceza, görev ve kaos (tekrar oynatma) hangi oyunda açık:**

| | Ceza | Gizli görev | Kaos |
|---|---|---|---|
| Vampir Köylü | Açık olabilir | Kapalı (rollerle karışır) | Kapalı |
| Casus | Açık olabilir | Kapalı | Kapalı |
| Köstebek | Açık olabilir | **Herkese açık** (köstebeğin hareketi gizlensin) | Kapalı |
| Rus Ruleti | Kapalı (can sistemi zaten ceza) | Kapalı | Kapalı |
| Takım Düellosu | Açık olabilir | Kapalı | Açık olabilir |

---

## 1. Köstebek

**Fikir:** grubun içinde bir hain var. Gizli bir amacı var ve kimse kim olduğunu bilmiyor.

- **Kişi:** 4–10. 8 ve üstünde istenirse 2 köstebek (birbirini bilmez).
- **Kurulum:** köstebek gizli bir **köstebek görevi** çeker ("3 soruyu güme gönder", "X'i 2 kez ifşa ettir", "çakmağı 3 kez aynı kişiye ver"). Diğer herkes normal gizli görev çeker. Herkesin bir amacı olunca köstebeğin garip hamleleri göze batmaz.
- **Tur:** normal çakmak.
- **Suçlama:** her 5 turda bir "Köstebek kim?" oylaması (A ve B dahil herkes oy verir).
  - Çoğunluk köstebeği gösterirse grup kazanır, oyun biter.
  - Yanlış kişi gösterilirse oyun sürer, o kişi "şüpheli" etiketi taşır.
  - Eşitlikte bir şey olmaz.
- **Kazanma:**
  - Köstebek görevini bitirirse hemen kazanır: ekranda "Köstebek kazandı", kim olduğu açılır.
  - Tur sayısı biter ve yakalanmamışsa son bir oylama yapılır, yakalanmazsa köstebek kazanır.
- **Elden ele:** köstebek kartı görev perdesiyle gösterilir. Herkes perdeye bakar, kimse ayrı tutulmaz. Oylama Grup Kararı gibi dolaşır.
- **Az kişi kuralı:** eleme yok, sorun yok.
- **İçerik:** 12–15 köstebek görevi (G1). Mevcut gizli görev türlerinin bir kısmı uyarlanır.
- **Açık sorular:**
  1. Suçlama her 5 turda bir mi, yoksa herkesin oyun boyunca 1 kez kullanabileceği bir "suçla" düğmesi mi?
  2. Köstebek görevi tamamlanınca hemen mi kazansın, oyun sonunda mı?

## 2. Rus Ruleti (can ayarıyla Son Çakmak da bu)

**Fikir:** çakmak bir tabanca. Düelloyu kaybeden tetiği çeker.

- **Kişi:** 4–10. Can: 1 (Rus Ruleti) ya da 3 (Son Çakmak).
- **Tabanca:** 6 hazne, 1 mermi, yeri gizli. Her tetik bir hazneyi boşaltır. Kaç haznenin boşaldığı herkese açık: risk her tetikte artar, herkes görür. Mermi patlayınca tabanca yeniden doldurulur (yeni gizli yer).
- **Tur:** normal çakmak. Mini oyunu kaybeden tetiği çeker. Patlarsa bir can gider, can biterse elenir ve izleyici olur. Soru ifşa ya da güme kuralı aynen işler; eğlencesi orada kalır.
- **Gerilim:** B çakmağı sorunun dürüst cevabına mı versin, yoksa düelloda yenebileceğini düşündüğü kişiye mi? Bir de tabanca neredeyse dolmuşken kime verdiği önemli.
- **Az kişi kuralı:** 3 kişi kalınca çakmak turu biter, **final** başlar. Kalanlar sırayla ikili düello yapar, kaybeden tetiği çeker. Tek kişi kalınca o kazanır.
- **Kazanma:** son kalan.
- **Elden ele:** sorunsuz; tetik ekranı herkesin önünde (zaten açık bilgi).
- **İçerik:** yok.
- **Açık sorular:**
  1. Kalan hazne sayısı açık mı olsun (bilinen risk), gizli mi (Kızgın Çakmak gibi ısı ipucu)?
  2. Düelloyu kazanan tetiği rakibine "devredebilsin" mi (bir kez kullanılabilen bir hak)?

## 3. Takım Düellosu

**Fikir:** iki takım. Düellolar takım puanına dönüşür.

- **Kişi:** 4–10. Takımlar rastgele ya da kurucu dağıtır. Tek sayıda kişide küçük takım 1 puanla başlar.
- **Tur:** normal çakmak.
  - B ile C **farklı takımdansa**: düelloyu kazananın takımı 1 puan alır.
  - **Aynı takımdansa**: puan yok, ama çakmak takımda kalır, yani sıradaki soruyu yine o takım sorar.
- **Gerilim:** dürüst cevap rakip takımdan biri mi? Yoksa çakmağı takımda tutmak için arkadaşını mı göstereceksin?
- **Kazanma:** önce 7 puana ulaşan takım ya da tur sayısı bitince önde olan.
- **Elden ele:** sorunsuz. Takım renkleri ekranda.
- **Online:** istenirse takım sohbet kanalı (DM altyapısıyla).
- **İçerik:** yok.
- **Açık sorular:**
  1. İfşa takıma ayrıca puan versin mi?
  2. Takım içi sohbet çıkışta olsun mu?

## 4. Casus (mekân ya da kelime: Imposter da bu)

**Fikir:** herkes gizli bir mekânı bilir, casus bilmez. Casus, sorulara bilmeden cevap vermek zorunda.

- **Kişi:** 4–10, 1 casus. Ayar: **Mekân** (Uçak, Hastane, Düğün…) ya da **Kelime** (Imposter: tek bir kelime).
- **Kurulum:** herkes perdede mekânı görür, casus "Casussun" görür.
- **Tur:** normal çakmak. A, B'ye mekânla ilgili bir soru yazar ("Burada en çok kim terler?").
  - B mekânı biliyorsa mantıklı birini gösterir. Casus bilmeden gösterir, seçimi onu ele verebilir.
  - Soru ifşa olursa herkes görür, **casus da**: soru mekânı fazla belli ederse casusa ipucu olur. A'nın da dikkatli sorması lazım.
- **Suçlama:** her 4 turda bir "Casus kim?" oylaması. Çoğunluk casusu gösterirse **casusun son şansı** var: mekânı tahmin eder, bilirse yine o kazanır. Yanlış kişi gösterilirse oyun sürer.
- **Casusun tahmini:** casus istediği an bir kez "Mekânı biliyorum" diyebilir. Mekân listesinden seçer: doğruysa kazanır, yanlışsa kaybeder.
- **Kazanma:**
  - Grup kazanır: casus yakalanır ve son şansta bilemez.
  - Casus kazanır: mekânı doğru tahmin eder ya da tur sayısı biter ve yakalanmaz.
- **Mod:** Normal ve Temiz. **Curcuna yok** (sorular mekâna göre yazılmalı, havuzdaki genel sorular işe yaramaz).
- **Elden ele:** mekân perdeyle gösterilir. Casusun "biliyorum" düğmesi her perdede herkeste görünür (sadece casusunkinde çalışır); kimse düğmeye bakarak casusu anlamasın.
- **İçerik (G1):** 30 mekân, 100 kelime.
- **Açık sorular:**
  1. Mekânda herkese bir de rol verilsin mi ("Uçak: pilot, hostes, yolcu")? Spyfall'da var; soruları zenginleştiriyor ama içerik yükü 3 kat.
  2. Casus mekân listesini oyun boyunca görsün mü?

## 5. Vampir Köylü

**Fikir:** gizli roller, gece ve gündüz. Gündüzleri çakmak, suçlamanın ta kendisi.

- **Kişi:** **6–10.** 6–7 kişide 1 vampir, 8–10 kişide 2 vampir. Vampirler birbirini bilir.
- **Roller:**
  - **Köylü.**
  - **Doktor:** her gece birini korur, kendini oyunda 1 kez.
  - **Gözcü:** her gece bir kişinin vampir olup olmadığını öğrenir.
- **Gündüz:** canlı oyuncu sayısının yarısı kadar çakmak turu.
  - Sorular suçlama aracı ("Sence vampir kim?", "Bu gece kim korunmalı?"). Paketten ya da serbest.
  - B çakmağı şüphelendiğine verir. İfşa olursa suçlama herkese açılır, güme giderse gizli kalır.
- **Linç:** gündüz sonunda bütün canlılar oy verir. Çoğunluğu alan elenir ve rolü açılır. Eşitlikte kimse elenmez.
- **Gece:**
  - Vampirler bir kurban seçer (2 vampirde ortak karar ya da çoğunluk).
  - Doktor birini korur, gözcü birine bakar.
  - Sabah kurban açıklanır (korunduysa "kimse ölmedi").
- **Kazanma:** bütün vampirler elenirse köylüler kazanır. Vampirler sayıca köylülere eşit ya da fazla olursa vampirler kazanır.
- **Az kişi kuralı:** canlılar 4'ün altına düşünce çakmak turları atlanır, sadece linç ve gece kalır.
- **Elden ele (en zor kısım):** gece telefon herkese tek tek gider ve **herkes bir seçim yapar.** Köylüye "birini düşün" ekranı gelir, seçimi bir işe yaramaz. Böylece kimin uzun düşündüğünden rol anlaşılmaz. Linç, Grup Kararı gibi dolaşır.
- **Online:** gece herkes kendi telefonunda seçer. Genel sohbet gece kapalı; vampirlere gece özel kanal.
- **İçerik (G1):** "Vampir" soru paketi (~40 soru), rol metinleri (G2).
- **Açık sorular:**
  1. Elenen kişinin rolü açılsın mı (klasik, kolay) yoksa gizli kalsın mı (zor, daha çok tartışma)?
  2. Gündüz turu sayısı sabit mi, canlı sayısına göre mi?
  3. Ölüler sohbette yazabilsin mi? Klasikte yazamaz; online'da "ölüler kanalı" olabilir.

---

## 6. Kural motoru ve iş bölümü

- **Yeni ayar:** `Ayarlar.Oyun` (enum, sona eklenir). `Mod` aynen kalır. Geçersiz birleşimler `Kur`'da `GecersizGirdi` döner: Casus + Curcuna, Vampir < 6 kişi.
- **Yeni aşamalar** (enum sonuna): `Suclama` (oylama), `Gece`, `Linc`, `Tetik`, `Final`, `CasusTahmini`.
- **Her oyun ayrı dosya:** `Oyun.Kostebek.cs`, `Oyun.RusRuleti.cs`, `Oyun.Takim.cs`, `Oyun.Casus.cs`, `Oyun.Vampir.cs`. Oyuna bağlandıkları yerler:
  - tur sonu,
  - düello sonucu,
  - ifşa ve güme,
  - kazanma kontrolü.

  Bugünkü Çakmak bunların hiçbirinden etkilenmez, testleri aynen kalır.
- **Gizlilik testleri her oyunda:** rol, mekân, vampir arkadaşları, gözcünün öğrendiği sadece sahibinin görünümünde. Bugünkü soru gizliliği testleri gibi.
- **Protokol:**
  - `ayarlar.oyun`;
  - `durum`'da `sana.rol` / `sana.mekan`, `elenenler`, `takimlar`, `puanlar`, `tabanca`;
  - yeni komutlar `suclamaOyu`, `geceSecimi`, `casusTahmini`, `tetik`.
- **G1 (Enes):** kurallar, testler, sunucu, protokol, içerik (köstebek görevleri, mekânlar, kelimeler, Vampir soruları).
- **G2 (Arda):** oyun seçimi ekranı, rol perdeleri, gece ekranları, oylama, tabanca, takım skoru, kazanan ekranı; elden ele ve online.

## 7. Önerilen sıra

En ucuzdan en pahalıya, her biri bitip test edilince sıradaki:

1. **Köstebek:** gizli görev ve oylama hazır. Yeni olan: rol, suçlama, köstebek görevleri.
2. **Rus Ruleti:** eleme hazır. Yeni olan: tabanca, can, final.
3. **Takım Düellosu:** yeni olan sadece takım ve puan.
4. **Casus:** yeni içerik ister (mekân ve kelime).
5. **Vampir Köylü:** en büyüğü. Gece, roller, linç, elden elede gece dolaşması.

Bugünkü yol haritasına etkisi: bu beş oyun S5 ile S6 arasına yeni bir blok olarak girer. Çıkış o kadar kayar. VPS ve testler (Enes'in kararı) oyun tamamen bitince.
