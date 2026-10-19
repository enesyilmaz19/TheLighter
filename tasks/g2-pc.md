# G2 (Arda): Önce PC

Bu dosya Arda'nın yapay zekâsı için bir görev tanımı. Kullanım: "`tasks/g2-pc.md`'yi oku ve 1'den başla."
Yazan: G1 (Enes), 2026-10-13.

**Güncelleme (2026-10-19):** önce Android çıkıyor, Steam sonra. Bu dosyanın kalanı (3.3, 3.4) Steam'e kaldı; sıradaki iş `tasks/g2-android.md`.

**Enes'in kararı:** ilk testler bilgisayarda. Öncelik PC; PC bitince Android'e geçilecek (o zaman ayrı görev gelecek). S6 (Vivox, Steam, mağaza, rapor/engelleme) şimdilik başlamıyor, ikimiz de PC'yi bitiriyoruz.

**Güncelleme (2026-10-13):** önce oyun tamamen bitecek, testler sonra. Sıra: bu dosyanın 1 ve 2. maddeleri → `tasks/g2-kostebek.md` (yeni oyunların ilki) → sonraki oyunlar. 3.3 ve 3.4 (çok bilgisayarlı ve VPS testleri) oyun bitince; 3.1 ve 3.2 geliştirirken kendi denemen olarak sürsün.

`tasks/g2-s4-s5.md`'deki ilkeler ve çalışma şekli aynen geçerli (sadece senin klasörlerin; `Assets/Kurallar`, `Sunucu/`, `docs/` değişmez; sunucu tarafında sorun görürsen Enes'e kısa mesaj).

---

## 0. Durum

- PR #8 ve #9 main'de. Enes'in makinesinde EditMode 353/353, `SunucuIleOyunTesti` yerel sunucuyla 2/2 (sunucu değişikliklerinden sonra da).
- Sunucu S5'te bitti: sohbet + DM, Temiz filtresi, grup paketi. Tek kaynak `docs/Protokol.md`. İstemci tarafının ayrıntıları `tasks/g2-s5-sohbet.md`'de.
- `Metinler.csv`'ye bir yer tutucu satır eklendi (`hata.Uygunsuz`). Metni istediğin gibi değiştir.

## 1. S5'i kapat

`tasks/g2-s5-sohbet.md`'nin 1, 2 ve 5. maddeleri:
- Sohbeti bağla.
- Temiz mod uyarılarını göster (`UYGUNSUZ`, `ATILDIN` ve `atildi`'nin `neden`'i).
- Grup paketini yükle (`"@bizim"`).

PC için ek olarak:
- **Ekran düzeni (Ana Plan 6.2):** sohbet PC'de sağda dursun, ortada oyun sütunu, solda oyuncular. Ayrı ekran yok.
- **Klavye:** `Enter` gönderir (sohbet ve soru kutusu). Göndermeden sonra odak kutuda kalsın.

## 2. PC düzeltmeleri (Enes'in bulguları)

### 2.1 Aynı bilgisayarda iki pencere birbirinin kimliğini alıyor (önemli)

**Belirti:** aynı bilgisayarda iki pencere aynı odaya girince iki pencere birbirini atıp duruyor.

**Neden:** `OnlineIstemci.Katil`, kayıtlı anahtarı oda kodu aynı diye kullanıyor, isme bakmıyor:
```csharp
string eskiAnahtar = kalici != null && kalici.Oku(KayitOda) == oda ? kalici.Oku(KayitAnahtar) : null;
```
`PlayerPrefs` bilgisayardaki bütün pencerelerde ortak. Akış:
1. 1. pencere odayı kurar, anahtarını kaydeder.
2. 2. pencere aynı koda katılınca o anahtarı yollar.
3. Sunucu bunu "aynı kişi döndü" sayar ve 1. pencereyi düşürür.
4. 1. pencere aynı anahtarla geri döner, bu kez 2. pencereyi düşürür.

Bu, PC'de test yapmanın önünü kesiyor.

**Öneri:**
- Kayıtlı anahtarı sadece kayıtlı isim, girilen isimle aynı kişiyse kullan (`Oyuncu.IsimAnahtari`).
- Testler için istersen komut satırından bir profil: `TheLighter.exe -profil 2` → `PlayerPrefs` anahtarlarının başına `p2.` eklenir. Kararı senin.

**Test:** iki farklı isimle aynı odaya katılan istemci, diğerinin kayıtlı anahtarını yollamıyor. Aynı isimle dönen yine anahtarla dönüyor.

### 2.2 Pencere arka plana geçince oyun duruyor

`ProjectSettings` içinde `runInBackground: 0`. PC'de oyuncu Discord'a geçince oyun donar: geri sayım durur, mesajlar işlenmez. Aynı bilgisayarda açılan diğer pencereler de sadece öne gelince çalışır.

**Yapılacak:** Player Settings → Resolution and Presentation → **Run In Background** açık. Elden elede saat zaten senin kuralınla duruyor (perde, menü), buna bağlı değil.

### 2.3 Pencere modu

Şu an tam ekran pencere (`fullscreenMode: 1`) ve boyut değiştirilemiyor (`resizableWindow: 0`).
- Pencerede oynanabilsin, boyutu değişebilsin. Ya ayardan ya da oyun içinde "tam ekran" düğmesiyle; kararı senin.
- Testte aynı bilgisayarda birden çok pencere için şu komut satırı çalışır: `TheLighter.exe -screen-fullscreen 0 -screen-width 900 -screen-height 1000`.
- 1366×768, 1920×1080, 2560×1440 ve dar bir pencerede taşma ya da kesilme olmasın.

### 2.4 Düşük öncelik

`tasks/g2-s5-sohbet.md`, 3. madde: `PaketListesiGuncelleyici` paket listesinin üzerine yazmasın.

## 3. PC test planı

Hepsi **Windows build'iyle**, editörde değil (`Assets/Settings/Build Profiles/Windows.asset`). Bulduğun hataları bir listeye yaz; sunucu tarafındakileri Enes'e ilet (hangi adım, ne bekledin, ne oldu).

### 3.1 Elden ele (Playtest 2 hazırlığı)

Tek bilgisayarda 4–6 kişi, "Hızlı başla", ceza, görev ve kaos açık. **Aynı grupla arka arkaya 2 oyun.** Bakılacaklar:
- Grup hafızası tanıyor mu?
- "Bizim paket" ikinci oyunda çıkıyor mu?
- Unvanlar ve paylaşım kartı dosyası doğru mu?
- Bir de Temiz modda bir oyun.

### 3.2 Online, tek bilgisayar

- Sunucuyu başlat: `dotnet run --project Sunucu/Cakmak.Sunucu`.
- 4 pencere aç (2.1 düzeldikten sonra), her biri farklı isimle. Oyunu sonuna kadar oyna.
- Ya da editördeki deneme botları (TheLighter → Online deneme botları) + 1 build penceresi.

### 3.3 Online, yerel ağda 2+ bilgisayar

- Sunucuyu ağa aç: `dotnet run --project Sunucu/Cakmak.Sunucu -- --urls http://0.0.0.0:5000`. Windows güvenlik duvarı ilk açılışta izin ister: "Özel ağ"a izin ver.
- Sunucu bilgisayarının IP'sini `ipconfig` ile bul (IPv4). Diğerleri "Sunucu" alanına `ws://<o IP>:5000/ws` yazar.
- Senaryolar:
  1. Ceza, görev ve kaos açık tam oyun. Sonunda özet ve tekrar oyna.
  2. Oyun ortasında bir bilgisayarın ağını kes, 20–30 sn sonra aç: "Bağlanıyor…", sonra kaldığı yerden devam.
  3. Pencereyi kapat, oyunu yeniden aç: "Kaldığın odaya dön".
  4. 60 sn'den uzun kopukluk: oyundan çıkar, dönünce izler.
  5. AFK: iki turda hiçbir şey yapma. "Buradayım" düğmesi çıkmalı.
  6. Kurucu birini atar. Kurucunun ağı kesilince kuruculuk geçer.
  7. Oyun sürerken katılan bekleme odasına girer, tekrar oyna'da oyuncu olur.
  8. Sohbet: genel mesaj ve DM. DM üçüncü kişiye düşmüyor.
  9. Temiz modda 3 küfürlü mesaj: uyarılar (1/3, 2/3), sonra atılma. Diğerleri "atıldı (Temiz)" görüyor.
  10. Grup paketi: kurucu "Bizim paket"i açıyor, oyunda "Öneri çek" ondan da geliyor.
  11. Oyun sırasında Discord'a geç, geri gel (2.2): oyun donmamalı.

### 3.4 VPS hazır olunca

Enes adresi verecek. Aynı senaryolar farklı ağlardan (ev, telefon paylaşımı), `wss://` ile. "4 cihaz farklı ağlardan bir oyun bitiriyor" ölçütü bu. Sonra Playtest 3: ses Discord'dan.

## 4. Şimdilik yapma

- **Android:** PC bitince ayrı görev gelecek. Var olan mobil kodu (dikey düzen, safe area, klavye kayması, arka plandan dönünce bağlanma) bozma, ama Android build'i ve Android'e özel iş şimdi yok.
- **S6:** Vivox ses, Steamworks, mağaza görselleri, rapor/engelleme ekranı.
- Skinler, animasyon, ses efektleri (S7).

## 5. Sözleşme oturumu gündemi (seninle ilgili)

Hepsi `tasks/g1-todo.md`'de:
- Kararlar 20–23 (PR #7 + NativeShare) ve 25–30.
- Sözleşme önerisi 14.
- `docs/Guvenlik.md` ve `.gitignore` onayı.
- Sunucu adresinin build'e konması (karar 29).

Android paket adı Android'e geçerken konuşulacak.

## 6. Bitti ölçütü

- EditMode'un hepsi yeşil. `SunucuIleOyunTesti` yerel sunucuyla geçiyor.
- Windows build'iyle 3.1, 3.2 ve 3.3 geçti. Bulgu listesi PR açıklamasında ya da Enes'e mesajla.
- **PR'lar küçük:** (1) S5 kapanışı, (2) PC düzeltmeleri (2.1–2.3). Sadece senin klasörlerin ve `ProjectSettings/` (2.2, 2.3), `.meta`'lar tam.
