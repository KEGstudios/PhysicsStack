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

### Gün 4 — Ses ve arayüz  *(bitti)*
Ses efektleri, düzgün bir yazı tipi, menü ve tur sonu ekranının yeniden
tasarımı, geçişler.

### Gün 5 — İçerik ve kapanış
Zorluk eğrisi *(bitti)*, seviye başına en iyi skor *(bitti)*, yıldız sistemi
*(bitti)*, sonsuz moda tehdit *(bitti)*, seviye 9-13 *(bitti)*, menü ve ayarlar
*(bitti)*, duraklatma *(bitti)*, performans ölçümü *(bitti)*, seviye
hedefinin kutu sayısına dönmesi *(bitti)*.

Gün 5'in listesindeki işler gün 3'ün sonunda bitti; performans ölçümü de beş
cihazda tamamlandı ve README'nin tutarlılık okuması yapıldı. Kalan iki iş
bilerek en sona bırakıldı: **30 saniyelik kayıt** ve **`v3` etiketi**. İkisi de
"o anki hâli" dondurma işi — kayıt bugün alınsaydı iki gün sonraki oyunu değil,
bugünkü oyunu gösterirdi ve portföyde duracak tek görüntü o.

Yani gün 4 ve 5 boş değil, **serbest**: planın kapattığı işler bitti, kalan süre
oynayarak çıkan işlere ayrıldı.

### Gün 4 — Testler ve bir ölçüm hatası

Serbest günün ilk işi kural katmanına **29 EditMode testi** yazmak oldu. Gerekçe
Faz 3'ün kendi tecrübesiydi: çıkan üç hatanın üçünde de derleme temizdi, sahne
kuruluyordu, log sessizdi. "0 hata" bir doğrulama değil.

Testler ucuza geldi çünkü altyapı zaten vardı — kural sınıfları MonoBehaviour
değil, karar girdisi salt-okunur bir struct. İkisi de test için alınmış kararlar
değildi; test edilebilirlik onların yan ürünü.

Aynı gün README baştan sona okundu. Dosya hep sona ekleyerek büyümüştü ve
okuyucuya yol gösteren hiçbir şeyi yoktu: en üste oyna linki, üç teknik başlık ve
bir okuma kılavuzu girdi. İki gerçek çelişki de çıktı — README kendi içinde bir
yerde "sonsuz modda tek kutu kaybettiriyor" derken altı satır aşağıda kuralın
birleştiğini anlatıyordu.

Günün asıl işi ise planlanmamıştı. Proje sahibi oynarken bir sömürü buldu: zemine
üç kutu atıp üstlerine yığınca, hedefi altı olan seviye en yüksek sütun dört
kutuyken geçiliyordu. Sebep gün 3'te girmiş bir formüldü — "kulede kaç kutu var"
sorusu çıkarmayla cevaplanıyordu ve formülün yazılı olmayan bir varsayımı vardı.
Düzeltmesi ölçüyü tek kaynağa indirmek oldu: kutu sayısı artık ölçülen boydan
türetiliyor. Gerekçesi ve çıkan iki ders [KARARLAR.md](KARARLAR.md)'de.

Hatanın yeri dikkat çekici: testler yazılırken "burası test edilmiyor" diye
ilan edilen sınırın tam öbür tarafındaydı. Kuralın matematiği doğruydu, girdi
yanlıştı.

### Plana sonradan giren işler

Faz 3'ün planında olmayan ama oynanışta ortaya çıkan işler: yığma tavanını
yükselten fizik ayarları, kontrol noktası sistemi, kaydırmalı seviye listesi,
ayarlar ekranı ve oyun içi duraklatma. Hiçbiri "eklesek güzel olur" diye
gelmedi; her biri oynarken çıkan somut bir sorunun cevabı ve gerekçeleri
[KARARLAR.md](KARARLAR.md)'de.

En geç gelen de en büyüğü oldu: seviyenin hedefi yükseklikten kutu sayısına
döndü, yıldız da harcanan kutu yerine düşürülen kutuyu ölçüyor. Fazın son
gününde kural değiştirmek risk ama değişen şey oynanış değil, oynanışın
ölçüsüydü: aynı kuleyi aynı şekilde yapıyorsun, ekrandaki sayı artık ne
yaptığını doğru anlatıyor.

### Gün 5 — Kapanış

APK bir Oppo Reno 2Z'ye kurulup çalıştırıldı; Faz 3'ün en uzun ömürlü açık
maddesi böylece kapandı. Düşürme sayacında bilinen bir artık kaldı ve bilerek
düzeltilmedi — gerekçesi [KARARLAR.md](KARARLAR.md)'de, özeti şu: hata oyuncunun
lehine çalışıyor ve fazın son gününde cihazda doğrulanmış bir build'i geçersiz
kılmak, kazandığından çok riski olan bir iş.

Faz 3 burada kapanıyor. Kalan iki iş "o anki hâli dondurma" işi: **30 saniyelik
kayıt** ve **`v3` etiketi**.

## Kapsam dışı — bu fazda da

Karakter, animasyon sistemi, reklam/IAP, bulut kayıt, çoklu dil, çoklu oyuncu.
