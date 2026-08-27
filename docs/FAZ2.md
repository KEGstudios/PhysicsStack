# Faz 2 — Oynanabilir sürüm

5 günlük prototip planlandığı gibi bitti ve `v1-prototype` olarak donduruldu.
Bu belge ondan sonrasını tarif ediyor.

**Neden devam ediyorum:** İlk beş günün hedefi "dokunmatik girdiyi fiziğe doğru
bağlamak"tı ve o soru cevaplandı. Ortaya çıkan şey ise oynanabilir bir oyundan
çok bir teknik gösterim: beş kutu koyuyorsun, bitiyor. Elimdeki çekirdeğin
üstüne gerçek bir oyun döngüsü koymanın maliyeti, sıfırdan yeni bir prototip
başlatmaktan düşük.

**Sert kural yine geçerli:** Faz 2 beş gün. Sonunda ne durumda olursa olsun
kapanıyor. Zaman yetmezse **seviye sayısı kısılır, sistem kısılmaz** — yarım
kalmış bir sistem, az sayıda seviyeden daha pahalıya patlar.

---

## Ne değişiyor

Prototipte oyun "hedef yüksekliği geç, bitir"di. Faz 2'de iki mod var:

| Mod | Kural | Bitiş |
|-----|-------|-------|
| **Seviye** | Her seviyenin kendi sorusu var: bırakma mesafesi, kutu sınırı, engel | Hedefe ulaşınca sonraki seviye |
| **Sonsuz** | Hedef yok, düşene kadar yığıyorsun | Bir parça düşünce skor yazılır |

Sonsuz mod baştan açık değil: seviye modunda 8. seviye bitince açılıyor. Sebebi oyuncuyu oyalamak değil — sonsuz mod, oyuncunun kutuyu
yerleştirmeyi öğrenmiş olmasını varsayıyor. Seviyeler o öğrenmenin kendisi.

## Gün planı

### Gün 6 — Kamera, yön ve ölçüm
Oynanabilirliğin önündeki en büyük engel kameranın sabit olması: kule kadrajdan
çıkıyor. Kamera kulenin tepesini takip edecek (yükselirken yumuşak, alçalırken
gecikmeli). Ekran yönü portreye dönüyor — kule yukarı büyüyor, oyun tek elle
oynanmalı. Skor ölçümü (ulaşılan yükseklik + kutu sayısı) ortak zemin olarak
buraya giriyor; iki mod da onun üstüne oturacak.

### Gün 7 — Kural katmanının ikiye ayrılması
`StackGameController` şu an tek bir kuralı biliyor. İki mod tek sınıfa `if`'le
sığdırılırsa üçüncü bir şey eklendiğinde okunmaz hale gelir. Kural bir arayüzün
arkasına çıkıyor (`IStackRules`), iki uygulaması oluyor: `LevelRules`,
`EndlessRules`. Controller "kim kazandı"yı bilmiyor, sadece soruyor.

### Gün 8 — Seviye verisi ve zorluk eğrisi
Seviyeler ScriptableObject: hedef yükseklik, kutu sınırı, genişlik oynaması ve
**bırakma mesafesi**. Kod değişmeden yeni seviye eklenebilmeli.
Başlangıç: **8 seviye, sonsuz mod 8'i bitirince açılıyor.**

**Plandan sapma — ve sebebi.** Bu gün başlarken eğri "hedef yükseklik artıyor"
üzerine kuruluydu: 12 seviye, 2.5'ten 8'e. Tabloyu görünce yanlış eksende
olduğu ortaya çıktı — yükseklik bir *miktar*, seviye ise bir *soru* olmalı.
Miktarı artırarak seviye üretmek, 5. seviyede 5 kutu / 100. seviyede 100 kutu
demek; aynı soruyu daha uzun süre sordurmaktan başka bir şey değil. İlk
seviyelerin "absürt kolay" görünmesi de bunun belirtisiydi: 2.5 birim kolay bir
soru değil, **soru değil**.

Asıl sorun daha derinde: oyunda hiç risk yoktu. Kutuyu milimetrik yerine
getirip sıfır hızla bırakabiliyordun, yani beceri değil sabır testiydi ve
zorluk ancak kutu sayısıyla artabilirdi.

Yeni mekanik: **kutu, kule tepesinden belirli bir mesafenin üstünden bırakılmak
zorunda.** Yerleştirme bir koymadan bir atışa dönüşüyor. Zorluk kolu artık tek
bir mesafe, ve sonraki mekaniklerin hepsi üstüne oturuyor — rüzgâr havadaki
kutuyu itiyor, engel onu saptırıyor; ikisi de kutu havada yol kat ediyorsa
anlamlı.

### Gün 9 — Rüzgâr, engel, akış ve kalıcılık
Rüzgâr (sadece havadaki kutuyu etkiliyor, aşağıdaki kuleyi değil — yoksa
oyuncunun hatası olmadan kule yıkılır) ve hareketli engel: geçişi belirli
anlarda tamamen kapatarak nişan alma problemini zamanlama problemine çeviriyor.
Sonra mod seçimi, seviye listesi, tur sonu ekranı. İlk kez gerçek bir arayüz giriyor —
gri kutu prensibi burada da geçerli, süs yok. İlerleme ve en iyi skor
`PlayerPrefs`'te: kaydedilecek şey üç sayı, dosya formatı tasarlamanın anlamı yok.
Test edebilmek için kilitleri açan bir geliştirici bayrağı da buraya giriyor.

### Gün 10 — Telefon, his, kapanış
Gerçek cihazda zorluk eğrisi ayarı, performans ölçümü, README v2 ve kayıt.

## Kapsam dışı — burada da tartışmaya kapalı

Ses, sanat, partikül, karakter, reklam/IAP, bulut kayıt, çoklu dil,
sosyal özellikler. Faz 2 oyunu **oynanabilir** yapıyor, **yayınlanabilir** değil.
