# Sunucu kurulumu (VPS)

Hetzner CX23, Ubuntu 24.04 LTS. Hesap ve ödeme Enes'te. Neden böyle: `docs/Guvenlik.md`.

## 1. Makine

- Sunucuyu açarken **SSH anahtarını** ekle (şifreyle giriş hiç açılmasın).
- Hetzner Cloud Firewall: gelen sadece **22, 80, 443** (TCP). Makinede de aynısı:
  ```bash
  ufw default deny incoming && ufw allow 22/tcp && ufw allow 80/tcp && ufw allow 443/tcp && ufw enable
  ```
- `/etc/ssh/sshd_config`: `PasswordAuthentication no`, `PermitRootLogin prohibit-password`. Sonra `systemctl restart ssh`.
- Güncellemeler ve kaba kuvvet koruması:
  ```bash
  apt install -y unattended-upgrades fail2ban
  ```
  (Ubuntu'da fail2ban'ın SSH koruması kendiliğinden açık.)

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
