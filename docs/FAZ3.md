# Faz 3 — Görünüş ve his

Faz 2 oyunu oynanabilir yaptı. Bu faz **bitmiş gibi görünmesini** sağlıyor.

**Neden kural değişti.** Gri kutu kuralı Faz 1 için konmuştu ve orada doğruydu:
soru "dokunmatik girdiyi fiziğe nasıl bağlarım"dı, sanat o sorunun cevabını
gizlerdi. O soru cevaplandı. Şimdiki bağlam farklı — bu repo bir portföy parçası
olarak açılacak ve açan kişilerin çoğu koda bakmadan önce ekran görüntüsüne
bakıyor. Gri küpler, kodun kalitesinden bağımsız olarak "yarım kalmış teknik
demo" diye okunuyor.

Yani `CLAUDE.md`'deki "sanat, ses, partikül kapsam dışı" kuralı ve Faz 2'nin
"yayınlanabilir değil" sınırı bu faz için geçersiz.

**Sert kural aynı:** takvimde 5 gün. Bu fazın sonunda repo kapanır.

## Görsel yön: minimal, pastel

Düz pastel renkler, yumuşak gölgeler, hafif bloom ve vignette, gradyan arka plan.
Üç sebeple bu yön seçildi:

- **Fizik okunabilirliği.** Kutuların birbirinden ve zeminden ayrılması gerekiyor.
  Pastel palette her kutu farklı ton alabiliyor; koyu/neon yönde kenarlar kayboluyor.
- **Sanatçı gerektirmiyor.** Düz renk + ışık + post-process; doku ve modelleme yok.
  Sıcak/gerçekçi yön en riskli olanı: malzeme ayarı kötüyse gri kutudan da kötü görünür.
- **Mobil tarayıcıda çalışıyoruz.** Ucuz kadraj hâlâ gereklilik.

Yan faydası: aynı sade formlar artık kaza değil **kasıtlı** görünüyor.

## Gün planı

### Yarım gün — Görsel temel
Palet, malzemeler, ışık ve gölge, gradyan gökyüzü, URP post-process yığını
(bloom, vignette, renk ayarı, tonemapping). Ekran görüntüsünü en çok değiştiren
gün bu ve en ucuzu.

### Gün 3 — His (juice)
İniş anında ezilme-uzama, çarpma tozu (partikül), kamera sarsıntısı, çöküşte
zaman yavaşlaması, çizgi ve rüzgâr göstergesinin yeniden tasarımı. Görsel
"güzel"i "canlı"ya çeviren gün.

### Gün 4 — Ses ve arayüz
Ses efektleri, düzgün bir yazı tipi, menü ve tur sonu ekranının yeniden
tasarımı, geçişler.

### Gün 5 — İçerik ve kapanış
Zorluk eğrisi, sonsuz moda tehdit, seviye başına en iyi skor, son build,
30 saniyelik kayıt, README v3.

## Kapsam dışı — bu fazda da

Karakter, animasyon sistemi, reklam/IAP, bulut kayıt, çoklu dil, çoklu oyuncu.
