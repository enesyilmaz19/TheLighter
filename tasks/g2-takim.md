# G2 (Arda): Takım Düellosu ekranları

Bu dosya Arda'nın yapay zekâsı için bir görev tanımı. Kullanım: "`tasks/g2-takim.md`'yi oku ve 1'den başla."
Yazan: G1 (Enes), 2026-10-13.

**Sıra:**
1. `tasks/g2-pc.md` (1. ve 2. maddeler)
2. `tasks/g2-kostebek.md`
3. `tasks/g2-rus-ruleti.md`
4. bu dosya

Önce oyun tamamen bitecek, testler sonra.

---

## 0. Ne değişti

**Takım Düellosu (yeni oyunların üçüncüsü) kural motorunda ve sunucuda hazır:**
- Kurallar: `Assets/Kurallar/Oyun.Takim.cs`.
- Protokol: `docs/Protokol.md`, "Takım Düellosu" bölümü.
- Tasarım: `docs/Oyunlar.md`, 3. bölüm. "Kodlandı" notları kesin kurallar.

**Özetle oyun:**
- İki takım, turlar normal çakmak.
- **Düello puanı:** farklı takımdan B ile C'nin düellosunu kazananın takımı +1.
- **İfşa bonusu:** A rakip takımdan birine sorduysa ve soru ifşa olursa A'nın takımı +1.
- Aynı takımdan B ile C: puan yok, ama çakmak takımda kalır.
- Önce 7 puana ulaşan takım kazanır.

## 1. Kural motoru (elden ele)

**`Ayarlar.Oyun = OyunTuru.Takim`:**
- `Ayarlar.TakimDagilimi`: kurucunun dağılımı, oyuncu → 0 ya da 1. `null` olursa rastgele ve dengeli dağıtılır.
- Kurucu dağıttıysa herkes tam bir kez olmalı ve takımlar arasında en fazla 1 kişi fark olmalı; yoksa `Kur` → `GecersizGirdi`.
- `GizliGorevler` `false` olmalı.

**Görünüm:**
- `OyuncuDurumu.Takim` (0 ya da 1; diğer oyunlarda `null`), herkese açık.
- `Gorunum.TakimPuanlari` (iki sayı). Tek sayıda kişide küçük takım 1 puanla başlar.

**Olaylar:**
- `DuelloPuani` (`Kim`: düelloyu kazanan, yeni alan **`Olay.Takim`**: puan alan takım).
- `IfsaBonusu` (`Kim`: A, `Takim`).

**Kazanma:**
- 7 puana ulaşan takım, o turun sonucu (ifşa ya da güme ekranı) gösterildikten sonra kazanır.
- Tur sayısı biterse ya da `OyunuBitir` çağrılırsa önde olan kazanır, eşitlikte berabere.

**Oyun sonu:** `Ozet().Takim` → `Kazanan` (0, 1 ya da berabere `null`), `Puanlar`.

## 2. Ekranlar

1. **Oyun seçimi ve takımlar:** Takım Düellosu seçilince görevler kapanıp kilitlenir.
   - Kurulum ve lobide takım dağıtma: iki sütun, oyuncuyu sürükle ya da dokun → takım değiştir. "Rastgele" düğmesi dağılımı siler.
   - Denge uyarısı: takımlar arasında en fazla 1 kişi fark olabilir.
   - Takımların renk ve adı senin kararın (ör. Kırmızı / Mavi). Motor sadece 0 ve 1 bilir.
2. **Skor tabelası:** her ekranda iki takımın puanı ve hedef (7). Tek sayıda kişide küçük takımın 1 puanla başladığı görünsün ("+1 avans").
3. **Oyuncu listesi ve seçim ekranları:** herkesin takım rengi. A, B'yi seçerken; B, C'yi seçerken renkler görünsün. Gerilim burada: rakibi mi göstereceksin, takımı mı koruyacaksın?
4. **Puan anı:** `DuelloPuani` ve `IfsaBonusu` olaylarında kısa bir animasyon ("+1 Mavi", "İfşa bonusu +1"). Sonra durumdan çiz.
5. **Oyun sonu:** kazanan takım (ya da "Berabere"), puanlar, takımların üyeleri. Sonra her zamanki unvanlar.
6. **Online takım sohbeti:** sohbet panelinde "Takım" sekmesi, sadece Takım Düellosu sürerken. Takım mesajı kendi takımına (kendin dahil) gider.

## 3. Online (protokol: `docs/Protokol.md`)

- **`ayarlar`:** `oyun: "takim"`, `takimlar: { "p1": 0, "p2": 1, ... }`.
  - `{}` gönderirsen rastgele dağıtılır.
  - Lobi `durum`'unda `ayarlar.takimlar`.
  - Takım numarası 0 ya da 1 değilse ya da odada olmayan biri varsa `GECERSIZ_GIRDI`.
  - Lobiden çıkan dağılımdan düşer.
  - Herkes dağılımda değilse ya da denge bozuksa `baslat` → `GECERSIZ_GIRDI`. Yeni gelen biri varsa önce onu da bir takıma koy.
  - `tekrarOyna`'da dağılım sıfırlanır.
- **Oyunda `durum`:** `oyuncular[].takim`, `takimPuanlari`.
- **Olaylar:** `duelloPuani`, `ifsaBonusu`; ikisinde de yeni alan `takim`.
- **Sohbet:** `{ "t": "sohbet", "kime": "takim", "metin": "…" }`. Gelen mesajda `"kime": "takim"`. Takım oyunu yoksa ya da lobi/bekleme odasındaysan `GECERSIZ_GIRDI`.
- **Oyun sonu:** `ozet.takim`: `{ "kazanan": 0 | 1 | null, "puanlar": [7, 4] }`.

## 4. Testler

- `Cevirici`: `takim`, `takimPuanlari`, olaylardaki `takim`, `ozet.takim`, lobide `takimlar`.
- Dağıtma ekranı:
  - Dengesiz dağılımda "Başlat" kapalı ya da uyarı veriyor.
  - Lobiden çıkan listeden düşüyor.
- Elden ele akış: puanlar ve 7'de oyun sonu.
- Online: takım sohbeti sadece takıma düşüyor (iki pencere).

## 5. Bitti ölçütü

- EditMode'un hepsi yeşil, `SunucuIleOyunTesti` yerel sunucuyla geçiyor.
- Elden elede ve online'da bir Takım Düellosu oyunu baştan sona, bir kez kurucu dağıtarak, bir kez rastgele. Online'da takım sohbeti.
- PR: sadece senin klasörlerin, `.meta`'lar tam. Kural ya da protokolde eksik görürsen Enes'e yaz.
