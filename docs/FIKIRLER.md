# Sıradaki prototip fikirleri

PhysicsStack üzerinde çalışırken çıkan ama bu prototipe ait olmayan fikirler.
Buraya yazılmalarının sebebi: iyi fikirler, ama **bu oyunun** fikirleri değiller.

## Birleştirme oyunu (prototip adayı)

Bir kabın içine yukarıdan şekil bırakıyorsun. Aynı boydaki iki şekil değince
birleşip bir üst boya dönüşüyor. Kap taşınca tur bitiyor, skor birleşmelerden
geliyor.

**Neden PhysicsStack'e eklemedim:** bu oyunda sürükleme yok — sadece X seçip
bırakıyorsun. Kamera takibi, kule ölçümü, çöküş tespiti, kural katmanı, yani
PhysicsStack'in tamamı bu oyunda işsiz kalıyor. Faz 1'in cevapladığı soru
("dokunmatik girdiyi fiziğe doğru bağlamak") burada hiç sorulmuyor bile.

Aynı gövdeye zorla oturtmak yerine kendi beş gününü hak ediyor. Ortak
kullanılabilecek şeyler: fizik ayarları, build hattı, gri kutu yaklaşımı,
`PointerDragInput`'un girdi/fizik ayrımı.

**Dikkat:** Suika (karpuz oyunu) ailesi çok kalabalık. Portföye girecekse
üstüne özgün bir şey koymak gerekir — kabın eğilmesi, birleşmenin fiziksel bir
patlama üretmesi, ya da birleştirme yerine ayırma.

## Hareketli engel (PhysicsStack, Gün 9)

Engel çizgileri sabit durursa tek seferlik bir nişan alma problemi oluyor:
çözünce her seferinde aynı şekilde çözülüyor. Yatay hareket eden ve geçişi
belirli anlarda **tamamen kapatan** bir engel, problemi zamanlamaya çeviriyor —
kutuyu nereye bırakacağın kadar ne zaman bırakacağın da önemli hale geliyor.

Bırakma mesafesi zorunluluğuyla birlikte iyi çalışması gerekiyor: kutu zaten
havada yol kat ediyor, engel o yolu kesiyor.
