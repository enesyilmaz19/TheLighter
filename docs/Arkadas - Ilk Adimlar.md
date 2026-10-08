# Arkadaş (G2) — İlk Adımlar

Hoş geldin. Senin alanın oyunun **Unity tarafı:** proje, ekranlar, mini oyunların görüntüsü, ağ bağlantısı, ses, build.
Kurallar, sunucu ve içerik Enes'te (G1). İkiniz `docs/Ana Plan.md` 5. bölümdeki sözleşmeyle bağlısınız.

**Bugünün hedefi:** Boş ama doğru ayarlanmış Unity projesi repoda, `main`'de.

---

## 0. Önce oku (15 dk)

- [ ] `AGENTS.md`: repo kuralları. **Yapay zekâna da ilk iş bunu okut.** Claude Code `CLAUDE.md` üzerinden kendisi okur. Cursor, Codex ve Copilot `AGENTS.md`'yi okur. Başka bir araç kullanıyorsan dosyayı sohbete yapıştır.
- [ ] `docs/Brief.md`: oyun ne.
- [ ] `docs/Ana Plan.md`: en azından 1. bölüm (kararlar), 4. bölüm (iş bölümü), 5. bölüm (sözleşme), 6.2 (ekran) ve 8. bölüm (tekrar oynatma).

## 1. Kurulum

- [ ] GitHub'dan gelen repo davetini kabul et.
- [ ] Unity Hub → Installs → **6000.6.4f1** kur. Modüller:
  - **Android Build Support** (içindeki OpenJDK ve Android SDK & NDK dahil)
  - Windows Build Support zaten editörle geliyor
- [ ] Başka bir Unity sürümüyle **açma.** Sürüm farkı projeyi bozar.

## 2. Repoyu al

```bash
git clone https://github.com/enesyilmaz19/TheLighter.git
```

Klasörde şimdilik sadece `.gitignore`, `AGENTS.md`, `CLAUDE.md`, `README.md` ve `docs/` var.

## 3. Unity projesini kur

Unity Hub boş olmayan bir klasöre proje açmaz. Bu yüzden projeyi başka yerde kurup içine taşıyacaksın:

- [ ] Unity Hub → New project → **6000.6.4f1** → şablon: **Universal 2D** (URP). Konum: geçici bir klasör, örneğin masaüstü.
- [ ] Unity'yi kapat.
- [ ] Geçici projeden şu **üç klasörü** `TheLighter/` içine taşı: `Assets/` · `Packages/` · `ProjectSettings/`. Gerisini (`Library/`, `Logs/`, `UserSettings/`...) taşıma, geçici klasörü sil.
- [ ] Unity Hub → Add → `TheLighter` klasörünü seç ve aç.

## 4. Ayarlar

- [ ] **Edit → Project Settings → Editor:**
  - Asset Serialization → **Force Text**
  - Version Control → **Visible Meta Files**
  - *(Unity 6'da ikisi de varsayılan olarak böyle, bir kere bak yeter.)*
- [ ] **Player:** Company Name ve Product Name → `TheLighter`
- [ ] **Window → Package Manager → + → Add package by name:**
  - `com.unity.nuget.newtonsoft-json`: ağ mesajları (JSON) için
  - `com.unity.localization`: oyuncuya görünen metinler ilk günden tabloda olacak
- [ ] Platformu Android'e **şimdi geçirme.** Windows'ta kalsın. Android build'ini birleştirmeden sonra deneriz.

## 5. Klasörler

`Assets/` altında aç:

```
Assets/
  Scenes/
  Scripts/Ekran/
  Prefabs/
  UI/
  Art/
  Audio/
```

`Kurallar/`, `Kurallar.Tests/` ve `Icerik/` klasörlerini **açma.** Onları Enes açacak.

## 6. Gönder

```bash
git checkout -b g2/proje-kurulumu
git add -A
git status
```

- [ ] `git status` çıktısına bak. **`Library/`, `Temp/` ya da `Logs/` görünmemeli.** Görünüyorsa durdur ve Enes'e yaz.
- [ ] `.meta` dosyaları **görünmeli.** Onlar commit'lenir.

```bash
git commit -m "Unity 6000.6.4f1 boş proje, URP, klasörler"
git push -u origin g2/proje-kurulumu
```

- [ ] GitHub'da `main`'e **Pull Request** aç. Açıklamaya şunu yaz:
  - Arayüz için **uGUI** mi **UI Toolkit** mi kullanacaksın? Hangisini iyi biliyorsan o.
- [ ] Enes PR'ı birleştirsin.

## 7. Enes'le sözleşme oturumu (1 saat)

PR birleşince ikiniz oturun. `docs/Ana Plan.md` 5. bölümün üstünden geçin:
- Metot isimleri, `Gorunum`'da hangi alanlar var, aşama isimleri.
- Senin ekranların için eksik bir bilgi var mı? Örneğin "bu ekranı çizmek için X'i bilmem lazım".
- Değişen her şey önce `docs/Ana Plan.md`'ye yazılır.

Oturumdan sonra Enes `Assets/Kurallar/` içine sözleşmenin **boş iskeletini** koyacak. Tipler gerçek olacak ama içleri boş.

## 8. Sonra: S1

İskelet gelince **sahte veriyle elden ele ekranlarına** başlarsın:
- İsim girişi → soru sorma → perde ("Telefonu Ayşe'ye ver") → cevap seçme → bölünmüş ekran mini oyun → ifşa / güme → oyun sonu.
- Veri sahte: elle doldurulmuş bir `Gorunum`. Ekranlar gerçek tiplere bağlı olduğu için Enes'in kodu gelince sadece kaynağı değiştirirsin.

**Bitti ölçütü (S1):** Ekranlar baştan sona sahte veriyle geçiyor.

---

**Takılırsan:** `AGENTS.md`'deki sahiplik kuralı geçerli. Enes'in klasöründe bir sorun görürsen düzeltme, ona yaz.
