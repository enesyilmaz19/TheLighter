# Çakmak — Brief (tek kaynak)

Kaydedildi: **2026-10-02** · Enes'in yazdığı metin, değiştirilmedi. Kararlar → [[Ana Plan]]

---

# Proje: Çok Oyunculu Sosyal Dedikodu / Tahmin Oyunu (Cross-Platform)

## 1. Proje Özeti ve Temel Mekanik
Gerçek hayatta "çakmak oyunu" olarak bilinen sosyal blöf ve tahmin mekaniğini hem masaüstü (PC/Web) hem mobil platformlara taşıyoruz. 
- **Oynanış Döngüsü:**
  1. Sıradaki oyuncu (A), gruptan gizlice birini (B) seçer ve sadece B'nin göreceği/duyacağı bir hedefleme sorusu iletir ("Grupta en güvenilmez bulduğun kişi kim?", "Kiminle tatile gitmek istemezsin?" vb.).
  2. B, sorunun cevabı olarak düşündüğü üçüncü kişiye (C) oyun içi simgesel nesneyi devreder.
  3. B ile C arasında hızlı bir mini oyun oynanır (Varsayılan: Taş-Kağıt-Makas; ek mini oyunlar entegre edilebilir).
  4. Mini oyunu C kazanırsa; soru ifşa olur ve tüm lobi soruyu görür/duyar. B kazanırsa; soru gizli kalır ("güme gider").
  5. Mini oyunun sonucundan bağımsız olarak yeni turda soru sorma hakkı hedefteki oyuncuya (C) geçer.
- **Oyun Tipleri:**
  - **Online Çok Oyunculu:** Özel lobi kodlu (Private Lobby), sesli iletişim (Voice Chat) ve yazılı chat (genel ve kişiye özel DM).
  - **Pass-and-Play (Offline Tek Cihaz):** Telefonun elden ele gezdirildiği, ekran gizleme adımları içeren yerel parti modu.

---

## 2. Planlanan Oyun Modları
- **Normal:** Standart serbest yazım/sesli soru döngüsü.
- **Aile Dostu / Sansürlü (Clean):** Belirlenen küfür ve cinsellik kara listesindeki girdileri engelleyen; ihlal durumunda oyuncuyu lobiden atan mod.
- **Curcuna (Question Pool):** Oyun başında her oyuncudan sisteme 2'şer soru girmesi istenir; turlarda oyuncular soru yazmaz, havuzdan rastgele soru atanır.
- *(İlerisi için planlanan: Çoklu dil desteği - TR, EN, DE; modifiye edilebilir nesne skinleri; gelişmiş report sistemi).*

---

## 3. İstenen çıktılar

- **A.** 2 kişilik görev dağılımı (Frontend/Client ↔ Backend/Networking) ve ortak entegrasyon noktaları (API kontratları, event veri yapıları)
- **B.** Kritik noktalar: desync ve kopma yönetimi · mobil/PC ekran uyumu · sesli sohbet (Agora, LiveKit, WebRTC maliyet/performans) · küfür filtresinin sınırları ve lobiden atmanın kötüye kullanımı · gecikmeden etkilenmeyen mini oyunlar
- **C.** 2–3 yeni özgün mod konsepti
- **D.** MVP'den yayına aşama aşama yol haritası, sprint mantığında
- **Ek:** Bu tarz bir oyun için hangi platform, artıları ve eksileri
