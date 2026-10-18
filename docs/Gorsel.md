# Görsel ve animasyon kararları (Enes + Arda)

Taslak: Claude, 2026-10-18. Enes ve Arda birlikte doldurur; her satırın "Karar" sütunu boş. Kesinleşenler Ana Plan'a işlenir.

## Fikir (Enes)

Bütün oyunlar bir **masada** oynanır. Oyuncular masanın çevresinde **3D karakterler**. Çakmak verilirken **havada dönerek** gider; ifşa, güme, ceza, ölüm masada görünür. Arda'nın ekranları (soru yazma, oy, ayarlar) masanın üstünde katman olarak kalır.

Kural motoru her olayı zaten yayınlıyor (soru soruldu, düello, ifşa, güme, elendi, gece başladı...). Masa bu olayları dinleyip oynar: kurallara ve sunucuya dokunmaz, ekranlarla aynı durumu okur.

## Bugünkü durum

- Proje **2D URP şablonuyla** açılmış (`Assets/Settings/Renderer2D.asset`, 2D paketleri). 3D masa için URP'nin 3D renderer'ı (Universal Renderer) eklenmeli.
- Görsel dosya yok. Ekranlar UI Toolkit (`Assets/UI/Stil.uss`, `Tema.tss`), sadece renk ve yazı.
- Projede Timeline var. Tween kütüphanesi, Cinemachine ve Git LFS yok.
- Ana Plan'da "His" (çakmak, devretme, ifşa / güme animasyonu, ses) Arda'nın satırında ve S7'de. Bu fikirle iş büyüyor: kimin yapacağı aşağıda.

## Kararlar

| # | Konu | Seçenekler | Öneri | Karar |
|---|---|---|---|---|
| 1 | Görünüm | (a) 3D masa + üstte ekranlar · (b) 2D masa (üstten çizim) · (c) sadece ekranlar | (a), Enes'in fikri | |
| 2 | Render | URP Universal Renderer, mobil ayarlar · Built-in | URP: zaten kurulu, mobilde iyi | |
| 3 | Sanat tarzı | Low-poly düz renk · toon (çizgi film gölgesi) · gerçekçi | Low-poly + hafif toon: hazır paketler bu tarzda, telefonda ucuz, parti oyununa uyuyor | |
| 4 | Karakter modeli | Quaternius (ücretsiz, CC0, animasyonlu) · Kenney (ücretsiz, CC0, daha basit) · Synty POLYGON (ücretli, lisansı satın almadan okuyun) · kendi modelimiz (Blender) | Quaternius ile prototip; beğenilmezse Synty. Tek gövde, her oyuncu kendi renginde (sistemde her oyuncunun rengi zaten var) | |
| 5 | Karakter animasyonu | Paketin kendi klipleri · Mixamo (ücretsiz, Adobe hesabı; oyunda kullanmak serbest, ham dosyayı dağıtmak değil) · kendimiz | Paket + Mixamo. Unity'nin Humanoid sistemi hepsini aynı karaktere uydurur. Gereken: oturma, bekleme, el uzatma, sevinme, üzülme, şaşırma, düşme | |
| 6 | Masa, sandalye, oda | Kenney Furniture Kit (CC0) · Quaternius · Synty | Karakterle aynı paket ailesinden, tarz tutsun | |
| 7 | Çakmak modeli | Kendimiz (Blender) · hazır model | Kendimiz: oyunun kahramanı ve skinler (Ana Plan 8.7: Zippo...) bunun üstüne | |
| 8 | Karakterde kişiselleştirme | Sadece renk · renk + aksesuar (şapka, gözlük) | Önce sadece renk. Aksesuar skin gibi sonra | |
| 9 | Kodla animasyon (tween) | PrimeTween (ücretsiz, çöp üretmez, mobilde hafif) · DOTween (ücretsiz, en yaygın) · Unity Animation | PrimeTween. Çakmağın kavisli uçuşu, kamera, ekran geçişleri bununla. Uzun sahneler (gece geçişi) Timeline'la | |
| 10 | Kamera | Cinemachine (Unity'nin, ücretsiz) · elle | Cinemachine: düelloya yakınlaşma, gece kamerası | |
| 11 | Efektler (alev, kıvılcım, duman) | Particle System · VFX Graph | Particle System: VFX Graph telefonlarda zayıf | |
| 12 | Işık, gece-gündüz | Tek gerçek zamanlı ışık + renk geçişi · baked ışık | Tek ışık + Volume ile gece rengi. Çakmak alevi küçük bir nokta ışık | |
| 13 | Arayüz ikonları | Emoji · Kenney UI (CC0) · Google Material Symbols (Apache 2.0) | Bugünkü emoji kalsın; ikon gereken yerde Material Symbols | |
| 14 | Font | Google Fonts | Türkçe harfleri (ğ ş ı İ ç ö ü) olan biri: Nunito, Baloo 2, Rubik adayları, ekranda deneyin | |
| 15 | Ses efektleri | Kenney Audio (CC0) · freesound (her dosyanın lisansı ayrı, CC-BY atıf ister) · jsfxr ile üretmek | Kenney + jsfxr; freesound'dan alınana atıf listesi | |
| 16 | Müzik | Yok · lisanssız müzik (Pixabay vb.) · bestelenmiş | Lobide ve gecede hafif bir döngü; lisansını dosya dosya yazın | |
| 17 | Mini oyunlar | Ekranda (bugünkü) · masada (zar masaya atılır, TKM elle) | İlk sürümde ekranda; Zar masada olursa en çok o göze batar, sonra | |
| 18 | Elden elede masa | Masa her an görünür · perde ekranlarında gizlenir | Perdelerde gizli: telefonu tutan başkasının gizli bilgisini masadan görmesin | |
| 19 | Performans bütçesi | Hedef cihaz, kare hızı, karakter başına üçgen | En zayıf telefonumuzda 18 karakterle 30 fps üstü. Karakter başına ~3 bin üçgen, tek malzeme | |
| 20 | Dosyalar ve git | Git LFS (fbx, png, wav) · LFS'siz | LFS: binary dosyalar repoyu şişirmesin. GitHub'ın ücretsiz LFS kotası küçük, açmadan bakın. `.blend` gibi ham dosyalar repoya girmez, sadece Unity'ye giren | |
| 21 | Sahne çakışması | Herkes aynı sahnede · masa ayrı prefab/sahne | Masa ayrı prefab, ekranlar ayrı: Unity sahneleri git'te birleşmez, bir dosyayı aynı anda tek kişi değiştirir | |
| 22 | Mağaza görselleri | Sanatçı (Fiverr, çevre) · oyun içi ekran görüntüsü · yapay zekâ | Kapsül görseli için sanatçı; ekran görüntüleri oyundan. Yapay zekâ kullanılırsa Steam formunda beyan edilir | |
| 23 | Kim ne yapar | Aşağıda | | |

## 23. İş bölümü (öneri)

- **Masa (Enes):** sahne, karakterlerin yerleşmesi (6–18 kişi, yuvarlak masa büyür), olayları dinleyip animasyon oynatan katman, çakmak ve efektleri.
- **Ekranlar (Arda):** bugünkü gibi; masanın üstünde katman. Kamera ve masa olaylarına dokunmaz, sadece hangi ekranın açık olduğunu söyler (perdede masa gizlensin diye).
- **Birlikte:** 1–8 ve 14–16 (tarz, paketler, font, ses). Yeni bir arayüz yok: ikisi de aynı oyun durumunu ve olayları okuyor.

## Sıra

1. Bu tabloyu birlikte doldurun.
2. Yer tutucularla prototip (kapsül karakterler, kutu masa): masa olayları doğru oynuyor mu, his tutuyor mu.
3. Paketleri indirin, tarzı prototipte deneyin.
4. Masa ekranlarla birleşir.
