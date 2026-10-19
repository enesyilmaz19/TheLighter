# Sunucu kurulumu (VPS)

Hetzner CX23, Ubuntu 24.04 LTS. Hesap ve ödeme Enes'te. Neden böyle: `docs/Guvenlik.md`.

**Denendi (2026-10-13):** 2. adım birebir WSL'deki Ubuntu 24.04'te (systemd ile) uygulandı. Servis kısıtlı kullanıcıyla ve kum havuzuyla açıldı (`systemd-analyze security`: 3.0 OK), paketler yüklendi, 4 kişilik oyun, soru gizliliği ve Temiz filtresi (Linux'ta Unicode) geçti, günlükte uyarı yok.

## 1. Makine

- Sunucuyu açarken **SSH anahtarını** ekle (şifreyle giriş hiç açılmasın).
- Hetzner Cloud Firewall: gelen sadece **22, 80, 443** (TCP). Makinede de aynısı:
  ```bash
  ufw default deny incoming && ufw allow 22/tcp && ufw allow 80/tcp && ufw allow 443/tcp && ufw enable
  ```
- `/etc/ssh/sshd_config`: `PasswordAuthentication no`, `PermitRootLogin prohibit-password`. Sonra `systemctl restart ssh`.
- Güncellemeler ve kaba kuvvet koruması:
  ```bash
  apt install -y unattended-upgrades fail2ban libicu74
  ```
  (Ubuntu'da fail2ban'ın SSH koruması kendiliğinden açık. `libicu74`: .NET'in Unicode kütüphanesi; çoğu imajda zaten var, yoksa sunucu açılmaz.)
- `ufw enable` "SSH bağlantısı kopabilir" diye sorar: 22 zaten izinli, `y` de. Kapatmadan önce **yeni bir terminalden** SSH ile girebildiğini dene.

## 2. Uygulama

Bilgisayarda:

```bash
dotnet publish Sunucu/Cakmak.Sunucu -c Release -r linux-x64 --self-contained -o yayin
```

`yayin/` klasörünü sunucuda `/opt/cakmak`'a kopyala. Sunucuda:

```bash
useradd --system --no-create-home --shell /usr/sbin/nologin cakmak
chown -R root:root /opt/cakmak && chmod -R a+rX,go-w /opt/cakmak
chmod +x /opt/cakmak/Cakmak.Sunucu   # Windows'ta derlenen dosyanın çalıştırma izni gelmez
cp cakmak.service /etc/systemd/system/ && systemctl daemon-reload && systemctl enable --now cakmak
```

Uygulama dosyaları root'un: uygulama kendi dosyalarını değiştiremez. Gizli bilgi gerekirse `/etc/cakmak/ortam` (root:cakmak, 0640), repoya ve Unity build'ine asla girmez.

## 3. Caddy (HTTPS)

[Caddy'nin resmi kurulumu](https://caddyserver.com/docs/install#debian-ubuntu-raspbian), sonra `Caddyfile`'ı `/etc/caddy/Caddyfile`'a koy, alan adını yaz, `systemctl reload caddy`. Alan adının DNS kaydı (A/AAAA) sunucuyu göstermeli.

**Caddy aynı makinede olmalı:** sunucu `X-Forwarded-For`'a sadece localhost'tan güveniyor. Önüne Cloudflare gibi bir şey daha konursa `ForwardedHeadersOptions.KnownNetworks` ayarlanmalı, yoksa herkes aynı IP görünür ve sınırlar herkesi birden keser.

## 4. Kontrol

- `curl https://oyun.ornek.com/` → `TheLighter sunucusu`
- `curl -I https://oyun.ornek.com/` → `Server` başlığı yok
- `journalctl -u cakmak -f` → açılışta paket ve ceza sayıları
- Dışarıdan `http://SUNUCU_IP:5000` **açılmamalı**

## Ayarlar

Sınırlar (`docs/Protokol.md`, "Sınırlar") ortam değişkeniyle değişir, `cakmak.service` içine ya da `/etc/cakmak/ortam`'a: `Sinirlar__IpBasinaBaglanti=50`.

## Raporlar ve IP yasağı

Sunucunun yazabildiği tek yer `/var/lib/cakmak` (`cakmak.service`: `StateDirectory`). Yayın klasörü (`/opt/cakmak`) yeni sürümle değişse de raporlar kalır.

- **Raporlar:** `/var/lib/cakmak/raporlar/yyyy-MM-dd.jsonl`, satır başına bir rapor (`docs/Protokol.md`, "Rapor ve engelleme"). 30 gün sonra sunucu kendisi siler. Okumak: `sudo cat /var/lib/cakmak/raporlar/*.jsonl`.
- **IP yasağı:** rapordaki `ip`'yi `/var/lib/cakmak/yasakli-ip.txt`'ye bir satır olarak ekle (`#` ile başlayan satır not). Sunucuyu yeniden başlatmak gerekmez; o IP'den yeni bağlantı açılmaz. Aynı Wi-Fi'daki herkes aynı IP'dir: yasak bütün evi/kafeyi keser.

```bash
echo "203.0.113.5" | sudo tee -a /var/lib/cakmak/yasakli-ip.txt
```
