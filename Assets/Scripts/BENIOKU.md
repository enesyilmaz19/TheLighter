# İstemci (G2): kod haritası

Elden ele modu, Enes'in kural motoruyla (`Assets/Kurallar/`, `Cakmak.Kurallar.Oyun`) uçtan uca oynanır. Sahneye bir şey koymak gerekmez: Play'e basınca `Uygulama` kendini kurar.

| Klasör | Ne var |
|---|---|
| `Akis/` | Saf C# (`noEngineReferences`, `Kurallar`'a bağlı). `EldenEleAkisi`: telefon kimin elinde, perde, hangi ekran kimin görünümüyle çizilir. Kural hesaplamaz. Perdede ve menüde `Ilerle` çağrılmaz. |
| `Akis/Metin/` | CSV metin tablosu okuyucu, Türkçe ekler (Ayşe'ye, Mehmet'te). |
| `Akis.Tests/` | EditMode testleri (Window → General → Test Runner): perde, saat, gizlilik, 60 rastgele oyun. |
| `Cekirdek/` | Açılış, panel ve ölçek, safe area, metin yükleme, soru paketi listesi, arayüz yardımcıları. |
| `Editor/` | Sadece editörde çalışır: soru paketi listesini günceller. |
| `Ekran/` | Ana menü, kurulum, oyun ekranı ve parçaları (perde, soru sorma, cevap seçme, mini oyunlar, reaksiyon, sonuç, oyun sonu). |

- Arayüz: UI Toolkit, koddan kuruluyor. Görünüş `Assets/UI/Resources/TheLighter/Stil.uss`.
- Metinler: `Assets/UI/Resources/TheLighter/Metinler.csv` (Localization CSV biçimi, `Key,Turkish(tr)`). Koda metin yazılmaz.
- Soru paketleri ("Öneri çek"): Enes'in `Assets/Icerik/*.txt` dosyaları. `Editor/PaketListesiGuncelleyici` bunları editörde kendiliğinden `Assets/UI/Resources/TheLighter/PaketListesi.asset` listesine yazar (paket eklenince, silinince, editör açılınca). Oyun bu listeden okur, paketler build'e de girer. Elle: menü → TheLighter → Soru paketlerini güncelle.

## Henüz yok

Curcuna ve Temiz mod ekranları, online, ses, sohbet, S3 tekrar oynatma ekranları, animasyon ve ses efektleri.
