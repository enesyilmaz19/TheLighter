# Oyunlar: çakmak kuralları entegre edilmiş beş yeni oyun

Durum: **Enes'in kararları verildi (2026-10-13), Arda'yla sözleşme oturumunda konuşulacak** (ekranlar ve protokol). Ana Plan 7. bölüm buraya bakar.
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
- **Suçlama:** iki yoldan "Köstebek kim?" oylaması açılır (A ve B dahil herkes oy verir):
  - Her 5 turda bir, kendiliğinden.
  - Ayrıca herkesin oyun boyunca **1 erken suçlama hakkı** var: istediği an kullanır, oylama hemen başlar.

  Sonuçlar:
  - Çoğunluk köstebeği gösterirse grup kazanır, oyun biter.
  - Yanlış kişi gösterilirse oyun sürer, o kişi "şüpheli" etiketi taşır.
  - Eşitlikte bir şey olmaz.
- **Kazanma:**
  - Köstebek görevini bitirdiği an kazanır: ekranda "Köstebek kazandı", kim olduğu açılır.
  - Tur sayısı biter ve yakalanmamışsa son bir oylama yapılır, yakalanmazsa köstebek kazanır.
- **Elden ele:** köstebek kartı görev perdesiyle gösterilir. Herkes perdeye bakar, kimse ayrı tutulmaz. Oylama Grup Kararı gibi dolaşır.
- **Az kişi kuralı:** eleme yok, sorun yok.
- **İçerik:** 12–15 köstebek görevi (G1). Mevcut gizli görev türlerinin bir kısmı uyarlanır.
- **Kararlar (2026-10-13):** suçlama her 5 turda bir + herkese 1 erken suçlama hakkı. Görevini bitiren köstebek hemen kazanır.

**Kodlandı (2026-10-13, kural motoru + sunucu; ekranlar Arda'da).** Kodlarken netleşenler:

- **İlk sürümde 1 köstebek.** "8+ kişide 2 köstebek" sonraya: ikisinin ayrı ayrı yakalanması ve son oylama kuralı ayrıca düşünülmeli.
- **Erken suçlama sadece A soru sormadan önce** (`soruSorma` aşaması). Düellonun ortası kesilmez. Oylama bitince A'nın turu tam süreyle baştan.
- **Her 5 turda bir:** 6., 11., 16. turun başında, A sormadan önce.
- **Oylama:** oyundan çıkmamış herkes (A, B ve köstebek dahil), kendisi dışında birine, 60 sn. **En çok oyu alan** suçlanır, eşitlikte kimse. Kimin kime oy verdiği gizli.
- **"Görevini bitirdiği an"** = o turun sonucu (ifşa/güme) gösterildikten sonra: turun ortası kesilmesin.
- **Kurucu "bitir" derse** önce son oylama yapılır, oylama sürerken ikinci "bitir" reddedilir. Kurucu köstebekse oyunu kendisi bitirip kazanamasın. Köstebek görevini o turda bitirdiyse oylama olmaz, kazanır (bitmiş görev oylamayla elinden alınmasın).
- **Köstebek oyundan çıkarsa** grup kazanır. Online'da bağlı oyuncu 4'ün altında kalıp oyun biterse oylama yapılamaz, köstebek yakalanmamış sayılır.
- **Köstebek görevleri (12):** 2 soruyu güme gönder · X'i 2 kez göster · aynı kişiye 2 kez sor · 3 farklı kişiyi göster · 3 farklı kişiye sor · 3 mini oyun kazan · üst üste 2 kazan · 2 kez ifşa ettir · X'i mini oyunda yen · X'e 2 kez sor · 3 soru sor · 3 kez gösteril. Mevcut görev türleri, köstebeğe göre adetle; çoğu başkalarının seçimine bağlı (grup şüphelendiği kişiyi göstermezse görevi zorlaşır). **Adetler tahmin, Playtest'te ayarlanacak.**

## 2. Rus Ruleti (can ayarıyla Son Çakmak da bu)

**Fikir:** çakmak bir tabanca. Düelloyu kaybeden tetiği çeker.

- **Kişi:** 4–10. Can: 1 (Rus Ruleti) ya da 3 (Son Çakmak).
- **Tabanca:** 6 hazne, 1 mermi, yeri gizli. Her tetik bir hazneyi boşaltır. Kaç haznenin boşaldığı **gizli**; sadece Kızgın Çakmak'taki gibi bir ısı ipucu görünür (soğuk, ılık, sıcak, kızgın; gürültülü, sayı hesaplanamaz). Mermi patlayınca tabanca yeniden doldurulur (yeni gizli yer).
- **Tur:** normal çakmak. Mini oyunu kaybeden tetiği çeker. Patlarsa bir can gider, can biterse elenir ve izleyici olur. Soru ifşa ya da güme kuralı aynen işler; eğlencesi orada kalır.
- **Tetik devret:** her oyuncunun oyun boyunca **1 devretme hakkı** var. Kendi çekeceği tetiği istediği aktif birine verir, o kişi çeker. Devralanın kendi hakkı duruyorsa o da devredebilir; herkesin tek hakkı olduğu için zincir sonsuza gitmez.
- **Gerilim:** B çakmağı sorunun dürüst cevabına mı versin, yoksa düelloda yenebileceğini düşündüğü kişiye mi? Bir de tabanca neredeyse dolmuşken kime verdiği önemli.
- **Az kişi kuralı:** 3 kişi kalınca çakmak turu biter, **final** başlar. Kalanlar sırayla ikili düello yapar, kaybeden tetiği çeker. Tek kişi kalınca o kazanır.
- **Kazanma:** son kalan.
- **Elden ele:** sorunsuz; tetik ekranı herkesin önünde (zaten açık bilgi).
- **İçerik:** yok.
- **Kararlar (2026-10-13):** hazneler gizli, ısı ipucuyla. Herkese oyun boyu 1 "tetik devret" hakkı.

## 3. Takım Düellosu

**Fikir:** iki takım. Düellolar takım puanına dönüşür.

- **Kişi:** 4–10. Takımlar rastgele ya da kurucu dağıtır. Tek sayıda kişide küçük takım 1 puanla başlar.
- **Tur:** normal çakmak.
  - B ile C **farklı takımdansa**: düelloyu kazananın takımı 1 puan alır.
  - **Aynı takımdansa**: puan yok, ama çakmak takımda kalır, yani sıradaki soruyu yine o takım sorar.
  - **İfşa bonusu:** A rakip takımdan birine sorduysa ve soru ifşa olursa A'nın takımı ayrıca 1 puan alır.
- **Gerilim:** dürüst cevap rakip takımdan biri mi? Yoksa çakmağı takımda tutmak için arkadaşını mı göstereceksin?
- **Kazanma:** önce 7 puana ulaşan takım ya da tur sayısı bitince önde olan.
- **Elden ele:** sorunsuz. Takım renkleri ekranda.
- **Online:** takım sohbet kanalı ("Takım" sekmesi, DM altyapısıyla).
- **İçerik:** yok.
- **Kararlar (2026-10-13):** düello puanı + ifşa bonusu. Online'da takım sohbeti var.

## 4. Casus (mekân ya da kelime: Imposter da bu)

**Fikir:** herkes gizli bir mekânı bilir, casus bilmez. Casus, sorulara bilmeden cevap vermek zorunda.

- **Kişi:** 4–10, 1 casus. Ayar: **Mekân** (Uçak, Hastane, Düğün…) ya da **Kelime** (Imposter: tek bir kelime).
- **Kurulum:** herkes perdede mekânı ve o mekândaki **rolünü** görür ("Uçak: hostes"), casus "Casussun" görür. Kelime ayarında rol yok.
- **Mekân listesi:** bu oyundaki bütün mekânlar herkesin ekranında, oyun boyunca. Herkes gördüğü için liste casusu ele vermez; casus ipuçlarını listeyle eşleştirir.
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
- **İçerik (G1):** 30 mekân, her birinde 8–10 rol (~270 rol metni), 100 kelime. En büyük içerik işi bu.
- **Kararlar (2026-10-13):** mekânda roller var. Mekân listesi oyun boyunca herkeste görünür.

## 5. Vampir Köylü

**Fikir:** gizli roller, gece ve gündüz. Gündüzleri çakmak, suçlamanın ta kendisi.

- **Kişi:** **6–10.** 6–7 kişide 1 vampir, 8–10 kişide 2 vampir. Vampirler birbirini bilir.
- **Roller:**
  - **Köylü.**
  - **Doktor:** her gece birini korur, kendini oyunda 1 kez.
  - **Gözcü:** her gece bir kişinin vampir olup olmadığını öğrenir.
- **Gündüz:** canlı oyuncu sayısının yarısı kadar çakmak turu (aşağı yuvarlanır, en az 1).
  - Sorular suçlama aracı ("Sence vampir kim?", "Bu gece kim korunmalı?"). Paketten ya da serbest.
  - B çakmağı şüphelendiğine verir. İfşa olursa suçlama herkese açılır, güme giderse gizli kalır.
- **Linç:** gündüz sonunda bütün canlılar oy verir. Çoğunluğu alan elenir. Eşitlikte kimse elenmez.
- **Rol açılması (ayar, kurucu seçer):** elenenin rolü açılır (klasik, kolay) ya da oyun sonuna kadar gizli kalır (zor, daha çok tartışma). Gece ölenler için de aynı.
- **Gece:**
  - Vampirler bir kurban seçer (2 vampirde ortak karar ya da çoğunluk).
  - Doktor birini korur, gözcü birine bakar.
  - Sabah kurban açıklanır (korunduysa "kimse ölmedi").
- **Kazanma:** bütün vampirler elenirse köylüler kazanır. Vampirler sayıca köylülere eşit ya da fazla olursa vampirler kazanır.
- **Az kişi kuralı:** canlılar 4'ün altına düşünce çakmak turları atlanır, sadece linç ve gece kalır.
- **Elden ele (en zor kısım):** gece telefon herkese tek tek gider ve **herkes bir seçim yapar.** Köylüye "birini düşün" ekranı gelir, seçimi bir işe yaramaz. Böylece kimin uzun düşündüğünden rol anlaşılmaz. Linç, Grup Kararı gibi dolaşır.
- **Online:** gece herkes kendi telefonunda seçer. Genel sohbet gece kapalı; vampirlere gece özel kanal. **Ölüler** genel sohbete yazamaz (canlılara ipucu veremez) ama okur; kendi aralarında "Ölüler" kanalında yazar.
- **İçerik (G1):** "Vampir" soru paketi (~40 soru), rol metinleri (G2).
- **Kararlar (2026-10-13):** rolün açılması kurucunun ayarı. Gündüz tur sayısı canlıların yarısı. Ölüler kanalı var.

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
