# İstemci (G2): kod haritası

Elden ele modu, Enes'in kural motoruyla (`Assets/Kurallar/`, `Cakmak.Kurallar.Oyun`) uçtan uca oynanır. Online modda kural motoru sunucuda (`Sunucu/`), istemci sadece gelen `durum`'u çizer. Sahneye bir şey koymak gerekmez: Play'e basınca `Uygulama` kendini kurar.

| Klasör | Ne var |
|---|---|
| `Akis/` | Saf C# (`noEngineReferences`, `Kurallar`'a bağlı). `IOyunAkisi`: oyun ekranının konuştuğu arayüz (iki mod da). `EldenEleAkisi`: telefon kimin elinde, perde, hangi ekran kimin görünümüyle çizilir. Kural hesaplamaz. Perdede ve menüde `Ilerle` çağrılmaz. |
| `Ag/` | Saf C#, online (S4/S5). `Json` (bağımlılıksız), `Cevirici` (`docs/Protokol.md` ↔ Kurallar tipleri), `Tasima` (`ClientWebSocket`), `OnlineIstemci` (oda kur/katıl, yeniden bağlanma, sürüm filtresi, mesaj sınırı; anahtar günlüğe girmez), `OnlineAkis` (durum'dan bu cihazın ekranı, komutlar). |
| `Ag.Tests/` | EditMode: Protokol.md örnekleri, bozuk mesajlar, giden mesaj biçimi, oturum (sahte bağlantıyla), online akışın ekran seçimi, sunucu hata kodlarının metinleri. |
| `Akis/Metin/` | CSV metin tablosu okuyucu, Türkçe ekler (Ayşe'ye, Mehmet'te). |
| `Akis.Tests/` | EditMode testleri (Window → General → Test Runner): perde, saat, gizlilik, 60 rastgele oyun; S3 akışları (görev turu, Curcuna, Grup Kararı, Bedel, ceza onayı) ve metin tablosunun enum anahtarları. |
| `Cekirdek/` | Açılış, panel ve ölçek, safe area, metin yükleme, soru paketleri ve sesli cezalar, grup kaydı (cihazda JSON), paylaşım kartı görüntüsü, arayüz yardımcıları. `OnlineYonetici`: oturumu her karede ilerletir, odanın yerine göre ekranı seçer, "Bağlanıyor…" katmanı. |
| `Editor/` | Sadece editörde çalışır: soru paketi listesini günceller. |
| `Ekran/` | Online menüsü (oda kur, koda katıl, sunucu adresi), online lobi (kod, oyuncular, kurucu ayarları, atma), bekleme odası. Ana menü, elden ele lobisi (mod, paketler, ceza, görev/kaos, Hızlı başla, grup hafızası), oyun ekranı ve parçaları (perde, kaos duyurusu, Curcuna soru yazma, gizli görev, soru sorma, cevap seçme + ısı, mini oyunlar, reaksiyon, Grup Kararı oyu, İkiye Katla / Bedel, ceza çarkı ve kartları, sonuç, oyun sonu: unvanlar, görevler, grup kaydı, paylaşım kartı). |

- Arayüz: UI Toolkit, koddan kuruluyor. Görünüş `Assets/UI/Resources/TheLighter/Stil.uss`.
- Metinler: `Assets/UI/Resources/TheLighter/Metinler.csv` (Localization CSV biçimi, `Key,Turkish(tr)`). Koda metin yazılmaz.
- Soru paketleri ("Öneri çek"): Enes'in `Assets/Icerik/*.txt` dosyaları. `Editor/PaketListesiGuncelleyici` bunları editörde kendiliğinden `Assets/UI/Resources/TheLighter/PaketListesi.asset` listesine yazar (paket eklenince, silinince, editör açılınca). Oyun bu listeden okur, paketler build'e de girer. Elle: menü → TheLighter → Soru paketlerini güncelle.

## Elden ele saat kuralı

Perde açıkken, menü açıkken, görevlere bakılırken, Curcuna'da soru yazılırken, Grup Kararı oyu dolaşırken ve ceza kartları okunurken `Ilerle` çağrılmaz (`EkranDurumu.SaatDurur`). Telefon sırayla herkese gidiyor, herkes masada; motorun süreleri tek telefonda yetmez.

## Online

- Sunucu adresi koda yazılmaz: online menüsündeki "Sunucu" alanından girilir, cihazda saklanır. Varsayılan yerel deneme adresi `ws://localhost:5000/ws` (`dotnet run --project Sunucu/Cakmak.Sunucu`).
- Saat sunucuda: menü açıkken de süre akar, perde yok, ceza kartı onayı yok. Geri sayımı istemci yürütür, yeni `durum`'da sıfırlar.
- Komutun sonucu sonradan gelir: düğmeler basınca kapanır, ekran yeni durumla değişir. Hatalar kısa bildirim olarak görünür.
- Oyuncunun yazdığı her şey etikete düşebildiği için `Arayuz.Yazi` ve `Arayuz.Buton` zengin metni kapatır (`enableRichText = false`).

## Henüz yok

Sohbet ve DM (protokolü bekleniyor), ses, Android'de paylaşma menüsü (kart şimdilik dosyaya kaydediliyor), çakmak skinleri, animasyon ve ses efektleri.
