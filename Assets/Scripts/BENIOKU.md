# İstemci (G2): kod haritası

Elden ele modu, Enes'in kural motoruyla (`Assets/Kurallar/`, `Cakmak.Kurallar.Oyun`) uçtan uca oynanır. Sahneye bir şey koymak gerekmez: Play'e basınca `Uygulama` kendini kurar.

| Klasör | Ne var |
|---|---|
| `Akis/` | Saf C# (`noEngineReferences`, `Kurallar`'a bağlı). `EldenEleAkisi`: telefon kimin elinde, perde, hangi ekran kimin görünümüyle çizilir. Kural hesaplamaz. Perdede ve menüde `Ilerle` çağrılmaz. |
| `Akis/Metin/` | CSV metin tablosu okuyucu, Türkçe ekler (Ayşe'ye, Mehmet'te). |
| `Akis.Tests/` | EditMode testleri (Window → General → Test Runner): perde, saat, gizlilik, 60 rastgele oyun; S3 akışları (görev turu, Curcuna, Grup Kararı, Bedel, ceza onayı) ve metin tablosunun enum anahtarları. |
| `Cekirdek/` | Açılış, panel ve ölçek, safe area, metin yükleme, soru paketleri ve sesli cezalar, grup kaydı (cihazda JSON), paylaşım kartı görüntüsü, arayüz yardımcıları. |
| `Editor/` | Sadece editörde çalışır: soru paketi listesini günceller. |
| `Ekran/` | Ana menü, lobi (mod, paketler, ceza, görev/kaos, Hızlı başla, grup hafızası), oyun ekranı ve parçaları (perde, kaos duyurusu, Curcuna soru yazma, gizli görev, soru sorma, cevap seçme + ısı, mini oyunlar, reaksiyon, Grup Kararı oyu, İkiye Katla / Bedel, ceza çarkı ve kartları, sonuç, oyun sonu: unvanlar, görevler, grup kaydı, paylaşım kartı). |

- Arayüz: UI Toolkit, koddan kuruluyor. Görünüş `Assets/UI/Resources/TheLighter/Stil.uss`.
- Metinler: `Assets/UI/Resources/TheLighter/Metinler.csv` (Localization CSV biçimi, `Key,Turkish(tr)`). Koda metin yazılmaz.
- Soru paketleri ("Öneri çek"): Enes'in `Assets/Icerik/*.txt` dosyaları. `Editor/PaketListesiGuncelleyici` bunları editörde kendiliğinden `Assets/UI/Resources/TheLighter/PaketListesi.asset` listesine yazar (paket eklenince, silinince, editör açılınca). Oyun bu listeden okur, paketler build'e de girer. Elle: menü → TheLighter → Soru paketlerini güncelle.

## Elden ele saat kuralı

Perde açıkken, menü açıkken, görevlere bakılırken, Curcuna'da soru yazılırken, Grup Kararı oyu dolaşırken ve ceza kartları okunurken `Ilerle` çağrılmaz (`EkranDurumu.SaatDurur`). Telefon sırayla herkese gidiyor, herkes masada; motorun süreleri tek telefonda yetmez.

## Henüz yok

Online, ses, sohbet, Android'de paylaşma menüsü (kart şimdilik dosyaya kaydediliyor), çakmak skinleri, animasyon ve ses efektleri.
