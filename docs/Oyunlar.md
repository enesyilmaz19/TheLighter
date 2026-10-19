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

- **Oyun:** Çakmak (bugünkü oyun) · Çakmak Gecesi (eski Vampir Köylü) · Casus · Köstebek · Rus Ruleti · Takım Düellosu.
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
| Çakmak Gecesi | Açık olabilir | Kapalı (rollerle karışır) | Kapalı |
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

**Kodlandı (2026-10-13, kural motoru + sunucu; ekranlar Arda'da).** Kodlarken netleşenler:

- **Tetik, sonuç ekranından sonra:** önce sorunun sonucu (ifşa/güme, 6 sn), sonra kaybeden tetik ekranında. Karar süresi `hamleSn`; dolarsa tetik kendiliğinden çekilir. Devredilince devralanın süresi baştan.
- **Devret hedefi:** oyunda olan, elenmemiş, kopuk ya da AFK olmayan biri; kendine olmaz.
- **Isı:** sonraki tetik patlayacaksa her zaman "kızgın"; ama "kızgın" her zaman patlar demek değil (gürültü). Patlayınca tabanca yeniden dolar.
- **Tur sayısı yok:** oyun son kalana kadar sürer. Kurucu erken bitirirse en çok canı olan kazanır, eşitlikte kazanan yok.
- **Final:** 3 kişi kalınca her turda kalanlardan **rastgele iki** kişi sorusuz düello yapar; kaybeden doğrudan tetiğe (sonuç ekranı yok).
- **Oyundan çıkan** (online) elenmiş gibi sayılır; tetiği çekecekken çıkarsa tetik çekilmez.
- **Ayar uyumu tek yerde:** Rus Ruleti'nde ceza, görev ve kaos kapalı olmalı (aksi `GECERSIZ_GIRDI`); bu ve diğer "birlikte olamaz" kuralları `Oyun.AyarlarUyumlu`'da, sunucunun lobisi de onu kullanıyor.

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

**Kodlandı (2026-10-13, kural motoru + sunucu; ekranlar Arda'da).** Kodlarken netleşenler:

- **Kurucunun dağılımı:** herkes tam bir kez, takımlar arasında en fazla 1 kişi fark (4+ kişide her takımda en az 2). Dağıtmazsa rastgele ve dengeli. Online'da lobiden çıkan dağılımdan düşer, tekrar oynayınca dağılım sıfırlanır.
- **Kazanma anı:** 7 puana ulaşan takım, o turun sonucu gösterildikten sonra kazanır. Aynı turda iki takım da 7'yi geçerse yüksek olan; tur sayısı ya da kurucu bitirirse önde olan, eşitlikte berabere.
- **Kaos turları:** Grup Kararı'nda düello yok, oylamanın kazananı sayılır (oylama berabereyse puan yok). Soran Gizli turunda ifşa bonusu yok: puan A'nın takımını ele verirdi.
- **Görevler kapalı;** ceza ve kaos açılabilir.
- **Takım sohbeti:** `kime: "takim"`, sadece oyun sürerken (lobide ve bekleme odasında takım yok).

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

**Kodlandı (2026-10-14, kural motoru + sunucu; ekranlar Arda'da).** Kodlarken netleşenler:

- **Oylama Köstebek'inkiyle ortak** (`Oyun.Suclama.cs`): en çok oyu alan suçlanır, eşitlikte kimse; yanlış suçlanan şüpheli olur. Erken suçlama yok, sadece her 4 turda bir.
- **Son oylama** (G1 kararı): tur sayısı bitince ya da kurucu bitirince Köstebek'teki gibi bir oylama daha. Spyfall'da da tur sonunda oylanır; kurucu casussa oyunu kendisi bitirip kazanamaz. Burada da yakalanan casusun son şansı var.
- **Tahmin:** "Mekânı biliyorum" de son şans da aynı komut. Oyunun her aşamasında (düellonun ortasında da) yapılabilir, oyun hemen biter. Son şans 30 sn; süre dolarsa grup kazanır.
- **Mekân listesi** bütün mekânlar (30). **Kelimede** oyun bir kategori seçer, kategori herkese açık, liste o kategorinin kelimeleri (20).
- **Roller herkese ayrı:** her mekânda en az 9 rol (10 kişide casus dışında 9 kişi).
- **Casus oyundan çıkarsa** grup kazanır; oyun zorla biterse (az kişi) casus yakalanmamış sayılır.
- **Ceza açılabilir;** görev, kaos ve Curcuna kapalı. Temiz'de oynanır: bütün içerik Temiz filtresinden geçiyor (test var).
- **İçerik taslağı** `Assets/Kurallar/CasusIcerik.cs`: 30 mekân × 9 rol, 8 kategori × 20 kelime. Gözden geçirildi (2026-10-17): Şehirler, Ülkeler, Taşıtlar eklendi.

## 5. Çakmak Gecesi (eski adı Vampir Köylü)

**Fikir:** gizli roller, gece ve gündüz. Gündüzleri çakmak, suçlamanın ta kendisi. Taraflar **Masumlar** ve **Katiller**; bir de sadece kendisi kazanan **tarafsızlar**. Roller ToS, Goose Goose Duck, Feign ve Among Us'tan uyarlandı, bazıları çakmağa özel.

**Kararlar (Enes, 2026-10-14):** ad Çakmak Gecesi. 6–18 kişi (diğer oyunlar 10'da kalır, odalar 18 kişilik). Gecelik kurban sayısı ayarı. Roller kişi sayısına göre otomatik dağılır, kurucu istemediklerini kapatır. Seri Katil'in oyunda 1 kaçınma hakkı. Soytarı ya da Cellat kazanınca oyun biter. Falcı oyuncu sayısıyla orantılı ve sınırlı. Deli kendini rastgele bir masum rolü sanır.

### Akış

- **Oyun gündüzle başlar.** Gündüz canlıların yarısı kadar (aşağı yuvarlanır, en az 1) çakmak turu. Canlılar 4'ün altındaysa (gün ortasında düşülse de) çakmak turu yok.
- **Linç:** canlılar oy verir (susturulan veremez), en çok oyu alan elenir, eşitlikte ya da hiç oy yoksa kimse.
- **Gece:** her canlı bir seçim yapar, yeteneği olmayan da "birini düşünür" (kimin uzun düşündüğünden rol anlaşılmasın). Seçim yapmamak (pas) da olur. Katil tarafındakiler hem kurban oyu verir hem (varsa) kendi yeteneğini kullanır: tek komut, iki hedef.
- **Sabah:** ölenler açıklanır. Sonra yeni gün.
- **Rolün açılması (ayar):** elenenin rolü herkese açılır ya da oyun sonuna kadar gizli kalır. Temizlikçi'nin temizlediği ve Seri Katil'in öldürdüğü kişinin rolü her durumda gizli kalır.
- **Gecelik kurban (ayar):** 1, 2 ya da 3; en fazla canlı katil sayısı kadar. Her katil bir kişiye oy verir, en çok oy alanlar ölür (eşitlikte rastgele).

### Roller

**Masumlar** (bütün katiller ve öldüren tarafsızlar ölünce kazanırlar):

| Rol | Ne yapar | İlham |
|---|---|---|
| Masum | Yeteneği yok | |
| Koruyucu | Gece birini korur (bütün saldırılara karşı), kendini oyunda 1 kez | ToS Doctor |
| Dedektif | Gece birine bakar: şüpheli mi? Katil tarafı, Seri Katil ve Bombacı şüpheli; Patron temiz görünür, Tuzakçı'nın işaretlediği şüpheli | ToS Sheriff |
| Şerif | Gece birini vurabilir. Vurduğu masum tarafındansa kendisi de ölür | GGD Sheriff |
| Kanunsuz | Oyunda 1 kurşun, cezasız | GGD Vigilante |
| Bekçi | Gece birini izler, sabah o gece ona kimlerin uğradığını öğrenir | ToS Lookout |
| Kulak | Çakmağa özel: güme giden soruyu güme ekranında görür (kimin sorduğunu görmez) | |
| Falcı | Oyunda 2 kez (12+ kişide 3): gece birini seçer, sabah onunla rastgele başkalarından oluşan bir grup görür ve "grupta kötü var/yok" öğrenir. Grup canlıların üçte biri (en az 3) | ToS Psychic |
| Deli | Kendini rastgele bir masum rolü sanır (Koruyucu, Dedektif, Bekçi ya da Falcı). Ekranında o rol yazar, sonuçları rastgele, koruması işlemez. Masum tarafında kazanır | Feign Crazy |

**Katiller** (birbirini ve rollerini bilir; gece kendi kanalları var; masumlardan az değillerse ve öldüren tarafsız kalmadıysa kazanırlar):

| Rol | Ne yapar | İlham |
|---|---|---|
| Katil | Düz katil | |
| Patron | Dedektif'e temiz görünür | ToS Godfather |
| Casus | Gece birinin tam rolünü öğrenir (Deli'yi Deli görür) | ToS Consigliere |
| Susturucu | Gece birini susturur: ertesi gün yazamaz, linçte oy veremez (herkes görür) | GGD Silencer |
| Tuzakçı | Gece birini işaretler: o gece Dedektif ona bakarsa şüpheli görür | ToS Framer |
| Temizlikçi | Oyunda 2 kez: gece seçtiği kişi o gece ölürse rolü açılmaz | ToS Janitor |
| Suikastçı | Linçte oyunda 1 kez birinin rolünü tahmin eder. Bilirse o kişi hemen ölür, bilemezse kendisi ölür | GGD Assassin |
| Palyaço | Gece birini kandırır: o kişinin ertesi günkü linç oyu Palyaço'nun oy verdiği kişiye sayılır | ToS Witch |
| Vampir | Oyunda 1 kez ısırır: ısırılan masum tarafındaysa ve o gece ölmezse ertesi sabah Katil olur | |

**Tarafsızlar** (sadece kendileri kazanır):

| Rol | Ne yapar |
|---|---|
| Soytarı | Linç edilirse kazanır, oyun biter |
| Seri Katil | Her gece birini öldürür, öldürdüğünün rolü açılmaz. Katillerin ilk saldırısından kurtulur (oyunda 1 kez). Son kalırsa kazanır |
| Cellat | Başta gizli bir hedefi var (masum tarafından). Hedef linç edilirse kazanır, oyun biter. Hedef başka yoldan ölürse Soytarı olur |
| Hayatta Kalan | Oyun bitince hayattaysa, kim kazanırsa kazansın o da kazanır |
| Hırsız | Oyunda 1 kez gece birini öldürür, sabah onun rolü (ve tarafı) Hırsız'ın olur: kalan hakları, Cellat'ın hedefi, Deli'nin sandığı dahil. Korunan ölmez, hak yine gider; o gece kendisi ölürse rolü alamaz. Rol almadan oyun biterse kaybeder (Enes, 2026-10-18) |
| Bombacı | Gece birine gizlice bomba koyar ya da (kendini seçerek) patlatır: bombalı herkes ölür. Son kalırsa kazanır (ToS Arsonist) |

### Çakmağa özel roller (Enes, 2026-10-15)

Yetenekleri gündüz turuna bağlı: kim kime sordu (A → B), kim kimi gösterdi (B → C), düelloyu kim kazandı, soru ifşa mı güme mi. Bu modda kaos kapalı, A, B ve C herkesin önünde: gizli olan soru. Elden elede gizli yeteneği telefonu tutan kullanır (A ya da B); başkasının düellosuna karışanlar (Hakem, Söndürücü) gece hazırlar. Gündüzün kayıtları her sabah silinir.

**Masumlar:**

| Rol | Ne yapar |
|---|---|
| Sorgucu | Oyunda 1 kez (12+ kişide 2), A iken (soruyu sormadan önce de olur): soruyu mühürler, düello ne olursa olsun soru ifşa olur, bedel yok |
| Hakem | Oyunda 2 kez: gece birini ve bir karar (ifşa / güme) seçer. Ertesi gün o kişinin B ya da C olduğu ilk düello oynanmaz, Hakem'in kararıyla biter. Herkes "Hakem kararı" görür, Hakem'in kim olduğunu değil |
| Kollayıcı | O gün A iken soru sorduğu herkes (B) o gece korunur |
| Yüzleşmeci | C olarak gösterildiği düelloyu kazanırsa, onu gösteren B'nin şüpheli olup olmadığını hemen öğrenir |
| Sezgici | B iken gösterdiği C'nin şüpheli olup olmadığını sabah öğrenir |
| Sınayıcı | Oyunda 2 kez, A iken: sorduğu B'nin şüpheli olup olmadığını sabah öğrenir |
| Siper | Oyunda 2 kez, kendiliğinden: o gün C olarak gösterildiyse o gecenin bütün saldırılarından kurtulur |
| Söndürücü | Oyunda 1 kez: gece birini seçer. Ertesi gün o kişinin karıştığı ilk düello söner: oynanmaz, soru güme gider, kimse ceza çekmez; Söndürücü o turun A'sının şüpheli olup olmadığını sabah öğrenir. Herkes "düello söndü" görür |

**Katiller:**

| Rol | Ne yapar |
|---|---|
| Kundakçı | Oyunda 1 kez: gece, o gün onu C olarak gösteren B'lerden birini yakar (ikinci hedef; korunan kurtulur) |
| Dinleyici | Katillerin Kulak'ı: güme giden soruyu güme ekranında duyar |
| Fitneci | A olduğu turlarda B'nin gösterdiği C, o gece Dedektif'e (ve şüphe bakışlarına) şüpheli görünür |
| Kuklacı | Oyunda 1 kez, A iken: C'yi B'nin yerine o seçer (soruyu sormadan önce seçerse soru gidince işler; B'nin kendisi olamaz) |
| Kışkırtıcı | Oyunda 1 kez, B iken: gösterdiği C o günün linçine 2 oyla başlar (C'yi seçmeden önce de olur) |

**Tarafsızlar:**

| Rol | Ne yapar |
|---|---|
| Kibritçi | Öldüren tarafsız: gece, o gün A iken soru sorduğu birini yakar. Son kalırsa kazanır |
| Gözde | 4 kez (12+ kişide 5) C olarak gösterilirse kazananlara eklenir, ölse bile; oyun sürer |
| Düellocu | 3 düello kazanırsa kazananlara eklenir, ölse bile |
| Sır Küpü | En az 3 kez B olup bunların hiçbiri ifşa olmazsa kazananlara eklenir, ölse bile |

Şüphe bakışlarının kuralı Dedektif'inki: katil tarafı ve öldüren tarafsızlar şüpheli, Patron temiz, o gecenin işaretlenenleri (Tuzakçı, Fitneci) şüpheli. Yüzleşmeci gündüz baktığı için işaretleri görmez.

### Dağılım

| Kişi | Katil | Tarafsız | Masum |
|---|---|---|---|
| 6–7 | 1 | 0 | geri kalanı |
| 8–10 | 2 | 1 | geri kalanı |
| 11–12 | 3 | 1 | geri kalanı |
| 13–14 | 3 | 2 | geri kalanı |
| 15–16 | 4 | 2 | geri kalanı |
| 17–18 | 4 | 3 | geri kalanı |

- Katil tarafı: her yer önce kapatılmamış özel katil rollerinden (rastgele, her biri en fazla 1), biterse düz Katil.
- Masum tarafı: Koruyucu ve Dedektif (kapatılmadıysa), sonra özel masum rolleri; masumların üçte biri (aşağı yuvarlanır) düz Masum kalır.
- Tarafsızlar: kapatılmamış tarafsız rollerden rastgele; hepsi kapalıysa yeri Masum'a kalır.
- Masum ve Katil kapatılamaz.

### Gece sırası

1. Seçimler toplanır. Tuzakçı işaretler, Susturucu susturur, Palyaço kandırır (ertesi gün için).
2. Koruyucu korur, Kollayıcı gündüz sorduklarını korur. Fitneci'nin turlarında gösterilenler işaretlenir.
3. Saldırılar: katillerin kurbanları, Seri Katil, Şerif, Kanunsuz, Hırsız, Bombacı'nın patlatması, Kibritçi, Kundakçı. Korunan ölmez. Seri Katil katillerin ilk saldırısından kurtulur. Siper o gün C olduysa bütün saldırılardan kurtulur. Şerif vurduğu masumsa kendisi de ölür (vurduğu korunsa bile).
4. Bakışlar gecenin başındaki rollere göre: Dedektif, Casus, Bekçi (o gece hedefe yeteneğini kullanan herkes; katillerden sadece kurbanına oy verenler), Falcı, Deli'nin sahte sonuçları; gündüzden bekleyen şüphe bakışları (Sezgici, Sınayıcı, Söndürücü).
5. Ölümler: Temizlikçi'nin ve Seri Katil'in kurbanının rolü gizli. Cellat'ın hedefi öldüyse Cellat Soytarı olur.
6. Vampir'in ısırdığı (yaşıyorsa ve masumsa) Katil olur, Hırsız, öldürdüğünün rolünü alır (hedefi bu gece öldüyse; aldığı Cellat'ın hedefi de öldüyse Soytarı olur).
7. Yeni gün: dünün kayıtları silinir, bu gecenin Hakem ve Söndürücü seçimleri yarına hazırlanır (kullanılmazsa o gün biter).

### Kazanma (her ölümde ve oyundan çıkışta bakılır)

- Soytarı linç edildi ya da Cellat'ın hedefi linç edildi: o kazanır, oyun biter.
- Masumlar: canlı katil, Seri Katil ve Bombacı kalmadı.
- Katiller: öldüren tarafsız kalmadı ve canlı katiller diğer canlılardan az değil.
- Seri Katil / Bombacı / Kibritçi: canlı tek öldüren o ve en fazla 2 kişi kaldı.
- 2 ya da daha az canlı kaldıysa ve kimse kazanamıyorsa (ör. Katil ile Seri Katil) berabere.
- Ek kazananlar, kim kazanırsa kazansın (beraberlikte de): hayattaki Hayatta Kalan; hedefini tutturan Gözde, Düellocu, Sır Küpü (ölü de olsa).
- Kurucu bitirirse kazanan yok.

### Sohbet (online)

- Katil kanalı (`kime: "katil"`): gece, canlı katiller arasında.
- Ölüler kanalı (`kime: "oluler"`): ölüler arasında, her an.
- Genel ve DM: gece kimse, ölüler ve o gün susturulan hiç yazamaz; bekleme odası da yazamaz. Herkes okur.

### Kulak ve güme kuralı (bilinçli istisna)

Ana Plan 8.5: güme giden soru hiçbir yerde kalmaz. Kulak ve Dinleyici bunun tek istisnası: soruyu **sadece güme ekranı süresince ve sadece kendi görünümlerinde** görürler. Soru kaydedilmez (özet, grup kaydı, sunucu), güme ekranı bitince bellekten de silinir.

### Elden ele

Gece telefon herkese tek tek gider, herkes bir seçim yapar; linç Grup Kararı gibi dolaşır. Her seçimden sonra süre baştan başlar. 18 kişide gece uzun sürer: istemci "Telefonu sıradakine ver" perdesini kısa tutsun.

### İçerik

`Assets/Icerik/GeceSorulari.csv` (48 suçlama sorusu): paket değil; bu oyunda öneriler sadece buradan.

---

## 6. Kural motoru ve iş bölümü

- **Yeni ayar:** `Ayarlar.Oyun` (enum, sona eklenir). `Mod` aynen kalır. "Birlikte olamaz" kuralları tek yerde, `Oyun.AyarlarUyumlu`: `Kur` ve sunucunun lobisi onu kullanır (yapıldı: Temiz + Cesur, Köstebek + kaos, Rus Ruleti + ceza/görev/kaos, Takım + görev, Casus + Curcuna/görev/kaos, Çakmak Gecesi + görev/kaos/gecelik kurban/kapalı roller). Kişi sayısı sınırı (Çakmak Gecesi 6–18, diğerleri 4–10) `Kur`'da.
- **Yeni aşamalar** (enum sonuna): `Suclama` (Köstebek ve Casus, yapıldı), `Tetik` (Rus Ruleti, yapıldı), `CasusTahmini`, `Linc`, `Gece` (yapıldı). Rus Ruleti'nin finali ayrı aşama değil, `Gorunum.Final` bayrağı (final düellosu normal `MiniOyun` aşaması).
- **Olay kuralı:** motor her aşama değişikliğinde bir olay yayar. Sunucu yeni durumu olaydan sonra yollar; olaysız değişen aşama online'da ekrana gitmez. Rastgele oyun testi bunu her oyunda denetler.
- **Her oyun ayrı dosya:** `Oyun.Kostebek.cs`, `Oyun.RusRuleti.cs`, `Oyun.Takim.cs`, `Oyun.Casus.cs`, `Oyun.CakmakGecesi.cs` (hepsi yapıldı). Gizli kişili oyunların (Köstebek, Casus) "kim?" oylaması ortak: `Oyun.Suclama.cs`; sonucu (yakalandı, yakalanmadı, kaçtı) yine kancalarla oyuna döner. Çekirdek döngü oyunlara tek bir yerden, `Oyun.Kancalar.cs` üzerinden bağlanır:
  - kurulum (`OyunlariKur`),
  - yeni tur başlamadan oyunun kararı: bitti mi, başka akışa mı geçiyor (`TurBasindaOyunKarari`),
  - turun sonucu: ifşa mı, düelloyu kim kaybetti (`OyunTurSonucu`),
  - oyun biterken sonucun yazılması (`OyunSonuclariniYaz`) ve özete konması (`OyunOzetine`),
  - görünüm (`OyunGorunumu`),
  - biri oyundan çıkınca oyun bitti mi (`OyuncuCiktiOyunKarari`: gizli kişi kaçtı, Çakmak Gecesi'nde bir taraf kazandı),
  - gizli kişili oyunlarda oylamanın sonucu (`HainYakalandi`, `HainYakalanmadi`, `HainKacti`).

  Her oyunun metodu "bu oyun değilse hiçbir şey yapma" diye başlar; yeni oyun bu kancalara eklenir. Oyuna özgü aşamalar (Rus Ruleti'nin tetiği, Casus'un son şansı, Çakmak Gecesi'nin linci ve gecesi) kendi dosyasında. Sohbet kanallarının kuralları (takım, katil, ölüler) `Oyun.Sohbet.cs`'te; sunucu sadece dağıtır. Bugünkü Çakmak bunların hiçbirinden etkilenmez, testleri ve rastgeleliği aynen kalır.
- **Gizlilik testleri her oyunda:** rol, mekân, vampir arkadaşları, gözcünün öğrendiği sadece sahibinin görünümünde. Bugünkü soru gizliliği testleri gibi.
- **Protokol:**
  - `ayarlar.oyun`;
  - `durum`'da `sana.rol` / `sana.mekan`, `elenenler`, `takimlar`, `puanlar`, `tabanca`;
  - yeni komutlar `suclamaOyu`, `geceSecimi`, `casusTahmini`, `tetik`.
- **G1 (Enes):** kurallar, testler, sunucu, protokol, içerik (köstebek görevleri, mekânlar, kelimeler, Çakmak Gecesi soruları).
- **G2 (Arda):** oyun seçimi ekranı, rol perdeleri, gece ekranları, oylama, tabanca, takım skoru, kazanan ekranı; elden ele ve online.

## 7. Önerilen sıra

En ucuzdan en pahalıya, her biri bitip test edilince sıradaki:

1. **Köstebek:** gizli görev ve oylama hazır. Yeni olan: rol, suçlama, köstebek görevleri.
2. **Rus Ruleti:** eleme hazır. Yeni olan: tabanca, can, final.
3. **Takım Düellosu:** yeni olan sadece takım ve puan.
4. **Casus:** yeni içerik ister (mekân ve kelime).
5. **Çakmak Gecesi (eski Vampir Köylü):** en büyüğü. Gece, 41 rol (17si çakmağa özel), linç, elden elede gece dolaşması.

Bugünkü yol haritasına etkisi: bu beş oyun S5 ile S6 arasına yeni bir blok olarak girer. Çıkış o kadar kayar. VPS ve testler (Enes'in kararı) oyun tamamen bitince.
