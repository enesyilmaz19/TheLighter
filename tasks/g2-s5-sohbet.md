# G2 (Arda): S5 sohbet, Temiz filtresi ve kalanlar

Bu dosya Arda'nın yapay zekâsı için bir görev tanımı. Kullanım: "`tasks/g2-s5-sohbet.md`'yi oku ve 1'den başla."
Yazan: G1 (Enes), 2026-10-12. `tasks/g2-s4-s5.md`'nin devamı; oradaki ilkeler ve çalışma şekli aynen geçerli.

**Sıra ve öncelik için önce `tasks/g2-pc.md`'yi oku** (2026-10-13: önce PC, Android sonra). Bu dosya orada adı geçen maddelerin ayrıntısı.

---

## 0. Durum

- **PR #8 ve #9 main'de.** Birleşik sonuç Enes'in makinesinde: EditMode 317/317, `SunucuIleOyunTesti` yerel sunucuyla 2/2. Botlar çok işe yaradı, sunucu değişikliğinden sonra da koşuldu.
- **Sunucuya yeni gelenler:** sohbet + DM ve Temiz filtresi (bu dosyayla aynı commit). Tek kaynak `docs/Protokol.md`: "Sohbet" ve "Temiz mod" bölümleri, `hata` tablosu (`UYGUNSUZ`, `ATILDIN`'in `neden`'i), hata sırası (`sohbet` satırı), "Sınırlar" (sohbet satırı).
- **Kural motorunda yeni:** `Sonuc.Uygunsuz` (enum'un sonunda) ve `TemizFiltre.Uygunsuz(metin)`. Temiz modda `SoruSor` filtreye takılan soruyu göndermez, `Sonuc.Uygunsuz` döner. Elden elede de böyle.
- **Senin dosyana tek satır eklendi:** `Metinler.csv`'ye `hata.Uygunsuz` (MetinTestleri yeni enum değeri için metin istiyordu, main kırmızı kalmasın diye). Metni istediğin gibi değiştir.
- Şu an EditMode: 353 test (Kurallar 229 + seninkiler), hepsi yeşil.

## 1. Sohbeti bağla (`OnlineYonetici.SohbetVar` → true)

- **Gönder:** `{ "t": "sohbet", "kime": null, "metin": "…" }` (genel) ya da `"kime": "p3"` (DM). `SohbetDefteri.Hazirla` ve `GonderebilirMi` aynen kalsın.
- **Yerelde ekleme, sunucudan döneni ekle.** Sunucu genel mesajı yazana da, DM'i alana ve yazana döndürür. `OnlineYonetici`'deki "gönder, sonra Ekle" notu yerine sadece gelen `sohbet` mesajını ekle. Böylece filtreye takılan ya da reddedilen mesaj ekranda hiç görünmez, çift de olmaz.
- **Gelen mesaj:** `{ "t": "sohbet", "id": "m41", "kimden": "p1", "kime": null, "metin": "…" }`.
  - `Benim = kimden == sen`.
  - Kanal: `kime == null` → genel; değilse `kimden == sen ? kime : kimden`.
  - `metin` sunucuda temizlenmiş gelir.
- **Sunucu sohbeti saklamaz.** Kopup dönen eski mesajları görmez; odadan çıkınca/atılınca `SohbetDefteri.Temizle` yeterli.
- **Bekleme odasındakiler** de genel sohbete yazar ve okur. DM listesine bekleyenler de girsin (`bekleyenler`).
- **Hatalar:**
  - `GECERSIZ_GIRDI`: boş, 200'den uzun, kendine DM, odada olmayana DM.
  - `COK_HIZLI`: saniyede 1, arka arkaya en fazla 2. İstemci zaten 1 sn bekletiyor, normalde gelmez.
- Etiketlerde zengin metin kapalı (oyuncu metni).
- **Test:** `Cevirici` (genel/DM `sohbet` okuma), `OnlineIstemci`/`OnlineAkis` (gelen sohbet deftere doğru kanala düşüyor, `Benim` doğru). İstersen `DenemeBotu`'na sohbet adımı.

## 2. Temiz mod geri bildirimi

**Online:**
- `UYGUNSUZ` gelince: `{ "t": "hata", "kod": "UYGUNSUZ", "ihlal": 1, "sinir": 3 }`. `HataMesaji`'na `Ihlal` ve `Sinir` alanları lazım.
  - Yazana göster: "Gönderilmedi (kimse görmedi). 3. seferde odadan atılırsın (1/3)". `sinir`'i koddan değil mesajdan al.
  - **Hem sohbette hem soru yazarken gelebilir:** `soruSor` da `UYGUNSUZ` dönebilir. O zaman tur aynı kalır, A başka soru yazar.
  - Hangi ekranda gösterileceğine son gönderdiğin şeye bakarak sen karar ver.
- `hata` `ATILDIN` artık `neden` taşıyor: `"kurucu"` ya da `"temiz"`. İkisi için ayrı metin olsun ("Kurucu seni attı" / "Temiz mod: 3. uygunsuz mesajda atıldın"). Yasaklı odaya yeniden girmeye çalışınca gelen `ATILDIN`'de `neden` yok.
- `olay: "atildi"` artık `neden` taşıyor (`OlayMesaji`'na alan).
  - Herkese "X atıldı (Temiz mod)" ile "Kurucu X'i attı" ayrı görünsün.
  - Kurucu Temiz yüzünden atılırsa kuruculuk sıradakine geçer; `durum.kurucu`'dan zaten okunuyor.
- Metin tablosu: `hata.UYGUNSUZ` (online, sunucu kodu) ve atılma nedenleri.

**Elden ele:** Temiz modda `SoruSor` → `Sonuc.Uygunsuz`. Soru yazma ekranında `hata.Uygunsuz` metni çıksın. Sıra ve perde değişmez, sayma ve atma yok.

**Test:** `Cevirici` (`UYGUNSUZ` alanları, `ATILDIN` ve `atildi` `neden`'i), elden ele Temiz'de uygunsuz soru reddediliyor, akış aynı aşamada kalıyor.

Filtrenin kendisine dokunma (kural motorunda, `Assets/Kurallar/TemizFiltre.cs`). Masum bir kelime takılırsa ya da bariz bir küfür geçerse Enes'e örnekle yaz, listeye o ekler.

## 3. Düşük öncelik: `PaketListesiGuncelleyici` sağlamlaştırma

**Ne gördü:** Enes'in makinesinde Unity komut satırından test koşarken log'da `No script asset for PaketListesi` çıktı.
- `Guncelle`, dosya varken `LoadAssetAtPath<PaketListesi>`'den null aldı ve `CreateAsset` ile üzerine yazdı.
- `PaketListesi.asset` içinde `m_Script: {fileID: 0}` oldu; git'te değişiklik olarak görünüyor.

**Senin PR'ların getirmedi:** PR'lardan önceki main'de de aynısı oluyor. Editörü normal açınca olmuyor gibi; repodaki dosya sağlam.

**Risk:** bir gün editörde de olursa build'e boş/bozuk paket listesi gider.

**Öneri:**
- Asset dosyası diskte var ama `PaketListesi` olarak yüklenmiyorsa üzerine yazma.
- Uyarı yaz, sonraki bir karede yeniden dene.
- `CreateAsset` sadece dosya hiç yoksa çalışsın.

**Tekrarlamak için:** projenin bir kopyasında (ör. `git worktree add ../deneme main`) şunu çalıştır, sonra `git diff Assets/UI/Resources/TheLighter/PaketListesi.asset`'e bak:

```
Unity.exe -batchmode -projectPath ../deneme -runTests -testPlatform EditMode -testResults s.xml -logFile l.txt
```

## 4. Sunucu adresi (karar bekliyor)

- **Öneri** (`tasks/g1-todo.md`, karar 29): yayın adresi (`wss://<alan-adı>/ws`) istemcide tek bir yerde dursun (sabit ya da Resources'ta bir ayar dosyası). "Sunucu" alanı sadece editörde ve Development build'de görünsün. Oyuncu adres girmez, sen yine yerel sunucuyla deneyebilirsin. Adres gizli değil, build'de durması sorun değil.
- Sözleşme oturumunda konuşulacak. Adres Enes VPS'i kurunca gelecek; o zaman 4 cihaz, farklı ağlar (S4 bitti ölçütünün son maddesi).

## 5. Grup paketi online (protokol geldi, 2026-10-13)

Kaynak: `docs/Protokol.md`, "Grup paketi". Kısaca: kurucu lobide `{ "t": "grupPaketi", "ilk": true, "sorular": [...] }` ile ilk parçayı, sonra `ilk`'siz parçaları yollar; lobi `durum`'unda `grupPaketi` (yüklenen soru sayısı) gelir; `ayarlar` → `paketler`'e `"@bizim"` (`PaketBilgisi.Bizim`, senin adın) eklenince oyunda kullanılır. Temiz modda sunucu kullanmaz.

- **Ne zaman:** kurucu lobide "Bizim paket" seçeneğini açınca (elden ele kurulumdaki `PaketButonu(PaketBilgisi.Bizim, ...)` gibi). O an lobideki isimlerle `GrupDeposu.Eslesen` (karar 22). Eşleşen yoksa ya da paketi boşsa seçenek görünmesin. Temiz'de görünmesin.
- **Parçalara bölme:** her mesaj UTF-8 olarak 4096 baytın altında kalsın (JSON kaçışlarıyla birlikte; payı bırak, ör. 3800). Türkçe karakter 2 bayt. Soruların hepsi sırayla gitsin, ilk parçada `ilk: true`.
- **Hız:** sonraki parçayı, `durum`'da `grupPaketi` arttıktan sonra yolla (saniyede 10 mesaj sınırı; 500 soru ~15–25 parça). `GECERSIZ_GIRDI` gelirse yüklemeyi durdur, uyarı göster (normalde gelmez: kayıttaki sorular zaten temiz ve ≤140 karakter).
- **Seçim:** yükleme bitince `ayarlar` ile `paketler`'e `"@bizim"` ekle. Lobideki herkes `grupPaketi` sayısını görebilir ("Bizim paket: 87 soru").
- **Tekrar oyna:** paket ve seçim sunucuda kalır. Oyun sonunda kurucunun kaydı `GrupKaydi.Isle` ile büyüdüyse istersen `ilk: true` ile yeniden yükle.
- **Test:** parçalara bölen fonksiyon (her parça < sınır, hepsi sırayla, ilk parçada `ilk`), `Cevirici`'de lobi `grupPaketi`. İstersen `SunucuIleOyunTesti`'ne bir yükleme adımı.

## 6. Sözleşme oturumu gündemi (seninle ilgili)

Hepsi `tasks/g1-todo.md`'de:
- Kararlar 20–23 (PR #7'deki 3 madde + NativeShare).
- Kararlar 25–30 (filtre, ihlal sayımı, sohbet hızı, saklamama, sunucu adresi, grup paketi).
- Sözleşme önerisi 14 (`Sonuc.Uygunsuz`).
- `docs/Guvenlik.md` ve `.gitignore` onayı.

## 7. Bitti ölçütü

- EditMode'un hepsi yeşil (353 + yenilerin). `SunucuIleOyunTesti` yerel sunucuyla geçiyor.
- **Sohbet:** iki pencerede genel ve DM. DM üçüncü pencereye düşmüyor.
- **Grup paketi:** bir grup kaydı olan kurucu "Bizim paket"i açıyor, lobide sayı görünüyor, oyunda "Öneri çek" ondan da geliyor.
- **Temiz:** Temiz modda bir oyuncu (bot ya da sen) 3 kez küfürlü mesaj yazınca atılıyor. Diğerleri "atıldı (Temiz mod)" görüyor, o isimle geri giremiyor.
- **PR:** sadece senin klasörlerin, `.meta`'lar tam, `Assets/Kurallar`, `Sunucu/`, `docs/` değişmemiş.
