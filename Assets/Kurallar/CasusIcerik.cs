using System.Collections.Generic;

namespace Cakmak.Kurallar
{
    /// <summary>Bir mekân ve rolleri, ya da bir kategori ve kelimeleri.</summary>
    public sealed class CasusGrubu
    {
        public readonly string Ad;
        public readonly IReadOnlyList<string> Ogeler;

        public CasusGrubu(string ad, params string[] ogeler)
        {
            Ad = ad;
            Ogeler = ogeler;
        }
    }

    /// <summary>
    /// Casus'un içeriği (docs/Oyunlar.md, 4). Mekânda herkese ayrı rol düşsün diye her mekânda en az 9 rol
    /// (10 kişide casus dışında 9 kişi). Temiz modda da oynanır: hepsi Temiz filtresinden geçer (IcerikTestleri).
    /// ponytail: içerik kodda, köstebek görevleri gibi; çeviri ya da sunucudan güncelleme gerekirse dosyaya taşınır.
    /// </summary>
    public static class CasusIcerik
    {
        public static readonly CasusGrubu[] Mekanlar =
        {
            new CasusGrubu("Uçak", "Pilot", "Yardımcı pilot", "Hostes", "Kabin amiri", "Business yolcusu", "Ekonomi yolcusu", "Bebekli anne", "Uçmaktan korkan yolcu", "Hava polisi"),
            new CasusGrubu("Hastane", "Doktor", "Hemşire", "Cerrah", "Hasta", "Hasta yakını", "Stajyer doktor", "Temizlik görevlisi", "Ambulans şoförü", "Danışma görevlisi"),
            new CasusGrubu("Düğün", "Gelin", "Damat", "Nikâh memuru", "Sağdıç", "Davetli teyze", "Fotoğrafçı", "Orkestracı", "Garson", "Takı takan amca"),
            new CasusGrubu("Okul", "Öğretmen", "Müdür", "Öğrenci", "Hademe", "Kantinci", "Veli", "Rehber öğretmen", "Nöbetçi öğrenci", "Beden eğitimi öğretmeni"),
            new CasusGrubu("Stadyum", "Futbolcu", "Hakem", "Teknik direktör", "Taraftar", "Seyyar satıcı", "Spiker", "Kaleci", "Güvenlik görevlisi", "Yedek oyuncu"),
            new CasusGrubu("Plaj", "Cankurtaran", "Turist", "Dondurmacı", "Şezlong görevlisi", "Sörfçü", "Kumdan kale yapan çocuk", "Güneşlenen teyze", "Mısırcı", "Voleybolcu"),
            new CasusGrubu("Restoran", "Şef", "Garson", "Bulaşıkçı", "Müşteri", "Restoran sahibi", "Yemek eleştirmeni", "Komi", "Kasiyer", "Doğum günü kutlayan"),
            new CasusGrubu("Polis karakolu", "Komiser", "Polis memuru", "Şüpheli", "Avukat", "Şikâyetçi", "Nöbetçi polis", "Dedektif", "Gazeteci", "Tercüman"),
            new CasusGrubu("Banka", "Şube müdürü", "Gişe memuru", "Güvenlik görevlisi", "Müşteri", "Kredi danışmanı", "Soyguncu", "Temizlik görevlisi", "Emekli", "Stajyer"),
            new CasusGrubu("Sinema", "Gişe görevlisi", "Mısırcı", "Seyirci", "Makinist", "Yer gösterici", "Sevgili çift", "Film eleştirmeni", "Uyuyakalan seyirci", "Temizlik görevlisi"),
            new CasusGrubu("Berber", "Berber", "Çırak", "Müşteri", "Sırada bekleyen", "Çaycı", "Dükkân sahibi", "Damat adayı", "Dedikoducu müşteri", "Gazete okuyan amca"),
            new CasusGrubu("Uzay istasyonu", "Komutan", "Astronot", "Mühendis", "Bilim insanı", "Doktor", "Uzay turisti", "Pilot", "Bakım robotu", "Kaçak yolcu"),
            new CasusGrubu("Korsan gemisi", "Kaptan", "Tayfa", "Aşçı", "Gözcü", "Esir", "Topçu", "Dümenci", "Papağan bakıcısı", "Haritacı"),
            new CasusGrubu("Tren", "Makinist", "Kondüktör", "Yolcu", "Vagon restoran garsonu", "Bilet kontrolörü", "Kaçak yolcu", "Sırt çantalı gezgin", "Uyuyan yolcu", "Seyyar satıcı"),
            new CasusGrubu("Kamp alanı", "İzci lideri", "İzci", "Kampçı", "Orman bekçisi", "Gitar çalan", "Ateşi yakan", "Aşçı", "Kaybolan çocuk", "Doğa fotoğrafçısı"),
            new CasusGrubu("Süpermarket", "Kasiyer", "Reyon görevlisi", "Müşteri", "Güvenlik görevlisi", "Mağaza müdürü", "Kasap", "Fırıncı", "Tadım standı görevlisi", "İndirim avcısı teyze"),
            new CasusGrubu("Otel", "Resepsiyonist", "Kat görevlisi", "Bavul taşıyıcı", "Turist", "Otel müdürü", "Balayı çifti", "Animatör", "Havuz görevlisi", "Şef"),
            new CasusGrubu("Kışla", "Komutan", "Er", "Çavuş", "Aşçı", "Nöbetçi", "Revir doktoru", "Acemi er", "Tankçı", "Telsizci"),
            new CasusGrubu("Sirk", "Palyaço", "Akrobat", "Hokkabaz", "Sihirbaz", "Hayvan terbiyecisi", "Sunucu", "İp cambazı", "Seyirci", "Bilet satıcısı"),
            new CasusGrubu("Müze", "Rehber", "Güvenlik görevlisi", "Turist", "Müze müdürü", "Restoratör", "Okul gezisindeki öğrenci", "Sanat hırsızı", "Ressam", "Hediyelik eşya satıcısı"),
            new CasusGrubu("Spor salonu", "Antrenör", "Vücut geliştirici", "Yeni üye", "Resepsiyonist", "Yoga hocası", "Protein satıcısı", "Aynada poz veren", "Temizlik görevlisi", "Diyetisyen"),
            new CasusGrubu("Kütüphane", "Kütüphaneci", "Öğrenci", "Araştırmacı", "Yazar", "Fısıldaşan çift", "Sınava çalışan öğrenci", "Kitap kurdu emekli", "Stajyer", "Fotokopici"),
            new CasusGrubu("Hamam", "Tellak", "Natır", "Müşteri", "Turist", "Hamam sahibi", "Kese yapan", "Masör", "Çaycı", "Gelin hamamına gelen teyze"),
            new CasusGrubu("Semt pazarı", "Manav", "Balıkçı", "Müşteri", "Zabıta", "Hamal", "Simitçi", "Pazarlıkçı teyze", "Tezgâh sahibi", "Annesine yardım eden çocuk"),
            new CasusGrubu("İtfaiye", "İtfaiyeci", "Ekip şefi", "Şoför", "Telsizci", "Acemi itfaiyeci", "Kedisi ağaçta kalan", "Aşçı", "Ziyarete gelen öğrenci", "Sağlıkçı"),
            new CasusGrubu("Konser", "Solist", "Davulcu", "Gitarist", "Hayran", "Güvenlik görevlisi", "Ses teknisyeni", "Menajer", "Karaborsacı", "Sahne arkası görevlisi"),
            new CasusGrubu("Hapishane", "Gardiyan", "Mahkûm", "Cezaevi müdürü", "Avukat", "Ziyaretçi", "Aşçı", "Kaçış planlayan mahkûm", "Revir doktoru", "Yeni gelen mahkûm"),
            new CasusGrubu("Çiftlik", "Çiftçi", "Çoban", "Veteriner", "Traktör sürücüsü", "Süt sağan", "Arıcı", "Şehirden gelen torun", "Mevsimlik işçi", "Komşu çiftçi"),
            new CasusGrubu("Lunapark", "Bilet satıcısı", "Dönme dolap görevlisi", "Pamuk şekerci", "Çocuk", "Ebeveyn", "Hız treninden korkan", "Atış poligonu görevlisi", "Maskot", "Güvenlik görevlisi"),
            new CasusGrubu("Kahvehane", "Ocakçı", "Okey oynayan emekli", "Tavla şampiyonu", "Çaycı", "Kahvehane sahibi", "Maç izleyen", "Gazete okuyan", "Yeni gelen genç", "Oyunu seyredip akıl veren"),
        };

        /// <summary>Kelime oyunu (Imposter): kategori herkese açık, kelimeyi casus bilmez.</summary>
        public static readonly CasusGrubu[] Kelimeler =
        {
            new CasusGrubu("Hayvanlar", "Kedi", "Köpek", "Fil", "Zürafa", "Penguen", "Aslan", "Kaplumbağa", "Yunus", "Kartal", "Tavşan",
                "At", "Maymun", "Yılan", "Ayı", "Baykuş", "İnek", "Köpek balığı", "Kanguru", "Arı", "Ahtapot"),
            new CasusGrubu("Yiyecekler", "Pizza", "Lahmacun", "Baklava", "Mantı", "Simit", "Dondurma", "Döner", "Sushi", "Hamburger", "Menemen",
                "Kumpir", "Karpuz", "Çikolata", "Kokoreç", "Pilav", "Mercimek çorbası", "İskender", "Gözleme", "Muz", "Patlamış mısır"),
            new CasusGrubu("Meslekler", "Doktor", "Öğretmen", "Aşçı", "Pilot", "Polis", "İtfaiyeci", "Berber", "Avukat", "Mimar", "Çiftçi",
                "Futbolcu", "Diş hekimi", "Yazılımcı", "Garson", "Taksici", "Fotoğrafçı", "Veteriner", "Astronot", "Postacı", "Sihirbaz"),
            new CasusGrubu("Eşyalar", "Şemsiye", "Telefon", "Ayna", "Saat", "Makas", "Gözlük", "Yastık", "Anahtar", "Mum", "Kumanda",
                "Sırt çantası", "Diş fırçası", "Bardak", "Çakmak", "Kulaklık", "Cüzdan", "Tencere", "Merdiven", "Saç kurutma makinesi", "Pil"),
            new CasusGrubu("Spor ve oyunlar", "Futbol", "Basketbol", "Voleybol", "Tenis", "Yüzme", "Satranç", "Okey", "Tavla", "Boks", "Kayak",
                "Bisiklet", "Golf", "Bowling", "Dart", "Güreş", "Masa tenisi", "Sörf", "Koşu", "Okçuluk", "Saklambaç"),
            new CasusGrubu("Şehirler", "İstanbul", "Ankara", "İzmir", "Antalya", "Bursa", "Trabzon", "Konya", "Gaziantep", "Eskişehir", "Adana",
                "Diyarbakır", "Erzurum", "Rize", "Mardin", "Çanakkale", "Edirne", "Samsun", "Van", "Hatay", "Kayseri"),
            new CasusGrubu("Ülkeler", "Almanya", "Fransa", "İtalya", "İspanya", "Japonya", "Çin", "Brezilya", "Mısır", "Rusya", "Amerika",
                "İngiltere", "Hindistan", "Yunanistan", "Kanada", "Meksika", "Arjantin", "Avustralya", "Hollanda", "Güney Kore", "Azerbaycan"),
            new CasusGrubu("Taşıtlar", "Otobüs", "Tren", "Uçak", "Gemi", "Motosiklet", "Taksi", "Dolmuş", "Metro", "Tramvay", "Vapur",
                "Helikopter", "Kayık", "Traktör", "Ambulans", "Kamyon", "Teleferik", "Denizaltı", "Kaykay", "Fayton", "Roket"),
        };
    }
}
