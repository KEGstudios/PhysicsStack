# PhysicsStack — Prototip 1

3D kule yığma prototipi. Kutuyu parmakla sürüklüyorsun ama **kulenin belirli bir
mesafe üstünden bırakmak zorundasın** — yerleştirme bir koyma değil, bir atış.
Kule hedefi geçip orada **tutunursa** kazanıyorsun.

İki mod var: sekiz seviyelik seri (her seviyenin kendi sorusu var — bırakma
mesafesi, yıldız için kutu bütçesi, rüzgâr, top atıcı) ve seriyi bitirince açılan
sonsuz mod. Sonsuzda zorluk seviyeler arasında değil turun **içinde** artıyor:
tehditler sırayla devreye giriyor, belirli kutu sayılarında da kulenin altı
donarak yeni bir zemin oluyor. Sanatçısı olmayan bir projede görsel ve ses
tamamen koddan geliyor: pastel palet, elle yazılmış gökyüzü shader'ı ve dosyadan
değil sentezden çıkan ses efektleri.

- Unity 6 (6000.5.10f1) · URP · Android + WebGL
- **Faz 1 (5 gün):** dokunmatik girdiyi fiziğe bağlamak. `v1-prototype` olarak donduruldu.
- **Faz 2 (5 gün):** onu oynanabilir bir oyuna çevirmek. `v2-playable`.
- **Faz 3:** bitmiş gibi görünmesini ve hissettirmesini sağlamak.

Faz 1'in sonunda elimde bir teknik gösterim vardı: beş kutu koyuyordun, bitiyordu.
Çekirdeğin üstüne gerçek bir oyun döngüsü koymanın maliyeti, sıfırdan yeni bir
prototip başlatmaktan düşüktü — [neden devam ettiğim](docs/FAZ2.md).

---

## Neden böyle yaptım

Bu bölüm "ne yaptım" listesi değil. Her başlıkta **alternatif neydi, neden elendi**
onu yazıyorum; çalışan kodun kendisi zaten `Assets/_Project/Scripts/` altında.

### Sürükleme: hız tabanlı takip

Denenen üç yaklaşım:

| # | Yaklaşım | Sonuç |
|---|----------|-------|
| 1 | `rb.position`'ı doğrudan set etmek | **Elendi.** Nesne kareler arasında ışınlanıyor, çarpışma çözücüsü araya giremiyor; içinden geçiyor. Bırakma anında hız sıfır olduğu için fırlatma hissi de yok. |
| 2 | `MovePosition` ile kinematik takip | **Elendi.** Çarpışmalara saygılı ama kinematik cisim itilemez: parmakla sürüklenen kutu yığındaki diğer kutuları ezip geçiyor, kendisi hiç zorlanmıyor. His sahte. |
| 3 | **Hız tabanlı takip** | **Seçilen.** Nesne rigidbody kalıyor; her `FixedUpdate`'te hedefe doğru bir hız atanıyor. |

Seçilenin çekirdeği:

```csharp
Vector3 delta = hedefNokta - rb.position;
rb.linearVelocity = Vector3.ClampMagnitude(delta / Time.fixedDeltaTime * takipGucu, maxHiz);
```

Üç şeyi bedavaya veriyor:

- **Gerçek çarpışma** — nesne dinamik kaldığı için yığındaki kutulara gerçekten çarpar,
  onları devirir, kendisi de yavaşlar.
- **Ağırlık hissi** — `maxHiz` sınırı yüzünden parmağı hızlı çekince nesne geride kalır.
  Bu gecikme "ağır cisim" olarak okunuyor; ayrıca ayarlanabilir bir his kolu.
- **Fırlatma** — bırakma anında rigidbody'nin üstünde zaten doğru hız var. Ayrıca
  bir "fırlat" kodu yazmaya gerek kalmıyor.

**Sonradan öğrenilen:** `takipGucu` ile `maxHiz` bağımsız iki his kolu değil.
Hız `delta * 50 * takipGucu` olduğu için, gerçek bir sürüklemede delta zaten büyük
ve sonuç neredeyse hep `maxHiz` sınırına takılıyor — yani hissedilen gecikmenin
tamamını `maxHiz` belirliyor, `takipGucu` ancak `maxHiz` yüksekken devreye giriyor.
Bunu değerleri çevirip hiçbir fark hissetmeyince fark ettim.

### Hedef nokta: kameraya bakan bir düzlem üzerinde raycast

Parmağın ekran koordinatını dünya koordinatına çevirmek gerekiyor.
`ScreenToWorldPoint` + sabit uzaklık **kullanılmadı**: kamera açısı ya da FOV
değiştiği anda sürükleme düzlemi kayıyor ve ayarlanan his bozuluyor.
Onun yerine kameraya bakan sabit bir `Plane` tanımlanıp `Plane.Raycast` ile
kesişim alınıyor — kamera nereye giderse gitsin sürükleme aynı düzlemde kalıyor.

### Görüntü 3D, simülasyon 2D

Kutuların rigidbody'sinde z ekseni konumu ve x/y ekseni dönüşü kilitli.

İlk halinde fizik üç eksende de serbestti ve kule oyuncunun göremediği derinlik
ekseninde devriliyordu. Oyuncu sürüklemeyi yalnızca XY düzleminde yapabildiği için
buna müdahale etmesi imkânsızdı — kaybedebileceği ama kazanamayacağı bir eksen.

Kilit sonrası kutular hâlâ devriliyor, yuvarlanıyor, birbirini itiyor; sadece
görünen düzlemde. Kural olarak: **kontrol 2D ise simülasyon da 2D olmalı.**

### Girdi `Update`'te, fizik `FixedUpdate`'te

Girdi kare hızında gelir, fizik sabit adımda çalışır. İkisi karışırsa aynı parmak
hareketi bazı karelerde iki kez, bazılarında hiç işlenmez — his kare hızına bağlı
hale gelir. Bu yüzden `Update` yalnızca **parmağın son pozisyonunu bir alana yazar**;
hızı `FixedUpdate` hesaplar.

### Yerleşme tespiti: uyku hâli / hız eşiği

Kazanma kontrolünü her karede yapmak yanlış sonuç verir — kule daha sallanırken
tepe noktası hedefi bir kare için geçebilir. Bu yüzden önce **yerleşme** aranıyor:
tüm rigidbody'ler `IsSleeping()` ya da hızları eşiğin altında. Kule oturduktan
sonra yükseklik ölçülüyor. "Geçti mi" değil, "oturduktan sonra geçmiş mi" sorusu.

### Kule neden dört kutuda duruyordu

Gün 3'ün sonunda dörtten fazlasını üst üste koyamıyordum. İlk refleksim hedefi
düşürmekti; onun yerine sebebini aradım. Tek sebep yokmuş, üç ayrı yerde
birikiyormuş:

**1. Çözücü iterasyonu.** Unity'nin varsayılanı `Default Solver Iterations = 6`,
`Velocity Iterations = 1`. Üst üste duran cisimlerde çözücü her adımda küçük bir
hata bırakıyor; iterasyon azken bu hata birikiyor ve kule kimse dokunmadan
sallanmaya başlıyor. 12 / 2 yaptım. Maliyeti var ama bu sahnede on kadar
rigidbody dönüyor, ölçülecek bir fark değil.

**2. Sürtünme hiç ayarlanmamıştı.** Collider'da fizik malzemesi yoktu, yani
PhysX'in varsayılanı (0.6 / 0.6) çalışıyordu ve kutular en ufak temasta yatay
kayıyordu. `PM_Box` ile statik sürtünmeyi 0.85, dinamiği 0.6 yaptım — *duran
kutuyu kaydırmak, kayan kutuyu durdurmaktan zor olsun* diye. `Friction Combine`
değerini `Maximum` seçtim ki kutu–zemin temasında zeminin düşük değeri kazanmasın.
Yanına açısal sönümü 0.05'ten 0.35'e çektim; varsayılan neredeyse sıfır ve bir kez
dönmeye başlayan kutu hiç durmuyordu.

> Bu iki sayı Faz 3'te bir kez daha yükseldi (iterasyon 20/4, açısal sönüm 0.6).
> Sebebi bu bölümdekiyle aynı sorunun daha yüksek kulelerde geri gelmesiydi:
> [Kulenin tavanını yükseltmek](#kulenin-tavanını-yükseltmek).

**3. Asıl sebep: sürüklediğim kutu kuleye her fizik adımında yeniden vuruyordu.**
Bunu ancak ilk ikisini düzeltip hâlâ devrilince gördüm. Hızı doğrudan
`rb.linearVelocity`'ye yazdığım için, kutu kulenin üstündeki kutuya değdiği anda
çözücü hızı sıfırlıyor, ben bir sonraki `FixedUpdate`'te aynı hızı geri yazıyorum.
Yani temas boyunca saniyede elli kez kuleye taze bir darbe gidiyor. Kutuyu
nazikçe yaklaştırmak imkânsızdı, çünkü "nazik" diye bir seçenek bırakmamıştım.

### Hız atamak yerine hıza doğru yürümek

Üçüncü maddenin çözümü tek satır:

```csharp
Vector3 hedefHiz = Vector3.ClampMagnitude(delta / Time.fixedDeltaTime * takipGucu, maxHiz);
rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity, hedefHiz, maxIvme * Time.fixedDeltaTime);
```

Hızı atamak yerine ona doğru adım adım gidiyorum. Kutu bir engele dayandığında hız
birikemiyor, çünkü adım başına değişim `maxIvme` ile sınırlı: itişin sertliğinin
artık bir üst sınırı var. Boşlukta hiçbir şey değişmiyor — kutu birkaç adımda zaten
hedef hıza ulaşıyor, ben aradaki farkı hissetmiyorum.

**Peki neden `AddForce` değil?** Aynı hesabın hazır hâli gibi duruyor
(`ForceMode.Acceleration` birebir bu). Kullanmadım, çünkü kuvvet uygulayıp sonucu
fiziğe bırakınca kutunun ne kadar hızlanacağını kütle, sürtünme ve temas belirliyor
— yani "parmağa ne kadar yetişecek" sorusunun cevabı elimden çıkıyor ve his ayarı
dolaylı bir kola dönüşüyor. Burada **hedef hız benim, ona ulaşma sertliği fiziğin**.
İkisinin arasındaki sınırın nerede olduğunu bilmek istiyordum.

Bir de bırakma anına sınır koydum: `releaseSpeedClamp`. Kuleye yaklaşırken parmağı
hızlı oynatıp bırakınca kutu maksimum hızla kulenin içine giriyor ve altındaki her
şeyi süpürüyordu. Kırpma fırlatmayı öldürmüyor, sadece üst sınırını kuleyi
yıkmayacak seviyeye çekiyor.

### Ayar değerleri koda gömülmüyor

Takip gücü, maksimum hız, maksimum ivme ve bırakma hızı sınırı `DragSettings`
ScriptableObject'inde (`Assets/_Project/Data/`). Sebep şu: Play Mode'da yapılan
değişiklikler oyunu durdurunca kayboluyor — **ama ScriptableObject varlığına
yapılanlar kalıyor.** His ayarı ancak oynarken yapılabilen bir şey; her denemede
oyunu durdurup değeri yeniden girmek ayarın kendisini engelliyordu.

Yanında iki faydası daha var: değerler tek yerde durduğu için "hangi kutuda hangi
değer kalmış" sorusu ortadan kalkıyor, ve ileride "ağır kutu" gibi bir varyant
gerekirse ikinci bir varlık oluşturup prefab'a vermek yetiyor, kod değişmiyor.

Her ayarı oraya taşımadım. Yerleşme eşiği `StackTracker`'da, hedef yükseklik ve
oturma süresi `StackGameController`'da kaldı — onlar hisle değil kuralla ilgili ve
kural sınıfının kendi Inspector'ında durmaları daha okunur. Aynı varlığa doldurmak
"ayarlar" diye her şeyi toplayan bir çöp kutusu üretirdi.

### Debug paneli: neden Canvas değil OnGUI

Ekranın köşesinde durum, kule yüksekliği, kutu sayısı ve oturma sayacı yazıyor.
Canvas kurmak sahneye dört beş nesne daha ekler ve prototipi "menüsü olan oyun"a
benzetmeye başlar; oysa bu bir arayüz değil, **ölçü aleti** — işi build alındıktan
sonra bitiyor. `OnGUI` tek dosyada duruyor ve component'i kapatınca hiç iz
bırakmıyor. Oyunun kendi arayüzü olsaydı OnGUI yanlış seçim olurdu: her karede çöp
üretir ve dokunmatik ölçeği yoktur (punto ekran yüksekliğine oranlandı, telefonda
okunsun diye).

Asıl kazancı telefonda: konsol logunu göremediğim yerde "kutu neden yerleşmiş
sayılmıyor" sorusunu ancak oturma sayacını gözümle görerek cevaplayabiliyorum.

### Bitişi ekranda göstermek — ama menü kurmadan

Beş kutuyu üst üste koyup kazandığımda oyunun bittiğini anlamadım; yeni kutu
gelmeyince "takıldı mı" diye düşündüm. Kural doğru çalışıyordu, sadece kimseye
söylemiyordu. Debug panelinde tek kelime olarak yazıyordu ama oynarken oraya
bakılmıyor — bitişin **oyuncunun zaten baktığı yerde**, kulenin tepesinde olması
gerekiyordu.

Menü kapsam dışı, o yüzden sahnede zaten duran bir nesneyi kullandım: hedef
çizgisi kazanınca yeşile, kaybedince kırmızıya dönüyor. Rengi
`MaterialPropertyBlock` ile veriyorum, `renderer.material` ile değil — ikincisi
materyalin çalışma zamanı kopyasını çıkarır, hem paylaşılan varlığa dokunmuş
oluruz hem de kopya ayrı bir draw call'a düşer.

Yanına dokununca yeniden başlatma koydum. Bu da menü değil: telefonda kazandıktan
sonra yapılabilecek tek şey uygulamayı kapatmaktı ve bu prototipin çıktısı 30
saniyelik bir kayıt. "Durumu sıfırla" fonksiyonu yazmak yerine sahneyi yeniden
yüklüyorum — unutulan bir alan, silinmeyen bir olay aboneliği ya da sahnede kalan
bir kutu ihtimali kalmıyor. Tek sahnelik bir prototipte yükleme maliyeti yok
denecek kadar az; büyük bir projede bu tercih tersine dönerdi.

### Kamera: kadrajı FOV değil görünür genişlik belirliyor

Oyunu portreye çevirdiğimde kutu ekranda görünmez oldu. Sebep hata değil geometri:
sabit bir dikey FOV ile 9:19.5 bir ekranda görünen dünya genişliği, 16:9'dakinin
yarısından az oluyor. Kadrajı dikey açıya bırakırsan oyunun ne kadar geniş
göründüğü cihaza göre değişir.

Tersine çevirdim. Sabitlediğim şey **dünya biriminden görünür genişlik**; dikey FOV
her en boy oranı için `tan(hFov/2) = oran · tan(vFov/2)` bağıntısından hesaplanıyor.
Kule her cihazda aynı genişlikte görünüyor, değişen tek şey yukarıda kalan boşluk —
portrede zaten istediğim şey o.

Takip iki farklı hızda: yükselirken 0.35 sn, alçalırken 1.1 sn yumuşatma. Tek süre
kullanınca kule devrildiğinde kamera aşağı fırlayıp yıkılışı kaçırıyordu.

Kamera "kulenin tepesi"ni sorarken elde tutulan kutuyu saydırmıyor. Yeni kutu
kulenin üstünde belirdiği için oyuncu ona dokunduğu anda ölçüm tavana fırlıyor ve
kamera boşuna zıplıyordu. Kule yüksekliği, elindeki kutu değil yerleştirdiklerin.

### İki mod, tek controller

Faz 2'de iki mod var: hedef yüksekliği olan seviye modu ve düşene kadar yığdığın
sonsuz mod. İkisini tek sınıfa `if (sonsuz)` ile sığdırmak bugün çalışırdı; ayırma
kararını üçüncü modu hayal ettiğim için değil, şunu fark ettiğim için verdim:
controller'ın yaptığı işlerin hiçbiri moda göre değişmiyor. Girdiyi dinlemek,
sıradaki kutuyu istemek, yığının oturmasını beklemek, durumu yayınlamak — hepsi
aynı. Değişen tek şey "bu anlık görüntü ne anlama geliyor" sorusunun cevabı.

Sınırı oraya çektim. `IStackRules` tek bir soru soruyor: bu anlık görüntüde tur
devam mı ediyor, bitti mi. `LevelRules` hedefe ve kutu sınırına bakıyor (sınır
elle verilmiyor, yıldız bütçesinden türüyor), `EndlessRules` sadece bir şey
düştü mü diye bakıyor.

Arayüz zamanla iki soru daha kazandı ve ikisi de aynı sebeple: cevabı moda göre
değişen ama controller'ı ilgilendirmeyen şeyler. `HazardsFor` — tehditler şu an
ne olmalı; `IsCheckpoint` — kulenin altı bu kutu sayısında dondurulsun mu.

Kurallara `StackTracker`'ı doğrudan vermedim, `StackSnapshot` diye bir struct
veriyorum: yükseklik, yerleşmiş kutu sayısı, bir şey düştü mü, yığın oturdu mu.
İki kazancı var. Kural sınıfı sahnedeki hiçbir bileşene bağlı değil — MonoBehaviour
bile değiller, düz C# sınıfı. Ve aynı kararın içinde ölçümü iki kez okuyup iki
farklı cevap alma ihtimali yok; ölçüm karede bir kez donduruluyor.

Skoru da kural hesaplıyor, çünkü "skor" iki modda aynı şeyi anlatmıyor: seviyede
harcadığın kutu sayısı (az olsun), sonsuz modda ulaştığın kule boyu (çok olsun).
Ters yönler; controller'ın bunu bilmesi için bir sebep yok.

### Oyunun asıl eksiği: risk yokmuş

Seviye tablosunu yazarken fark ettim: kutuyu kulenin üstüne milimetrik yerine
getirip sıfır hızla bırakabiliyordum. Beceri testi değil sabır testiydi. Böyle bir
oyunda zorluk ancak kutu sayısıyla artabilir — 5. seviyede 5 kutu, 100. seviyede
100 kutu. Yükseklik bir *miktar*, seviye ise bir *soru* olmalı.

Çözüm: **kutu, kule tepesinin belirli bir mesafe üstünden bırakılmak zorunda.**
Parmakla o çizginin altına indirilemiyor. Yerleştirme bir koymadan bir atışa
dönüşüyor; x'i ve zamanlamayı oyuncu, gerisini fizik belirliyor.

Kısıt hedefe uygulanıyor, cismin kendisine değil: kutu fizikle aşağı itilebilir,
sadece oyuncu tarafından indirilemez. Çizgi de kutunun **altının** inebileceği yeri
gösteriyor, merkezinin değil — merkeze göre tanımlasaydım boyut oynayan
seviyelerde aynı sayı farklı kutular için farklı düşme mesafesi anlamına gelirdi.

Yan etkisi hoşuma gitti: Gün 4'te ayarladığım his değerleri (takip gücü, ivme
sınırı, bırakma hız kelepçesi) nihayet oyunu etkiliyor. Risk yokken hiçbiri bir
şey ifade etmiyordu.

Aynı sebeple kutunun beliriş noktasındaki yatay rastgeleliği kaldırdım.
Rastgelelik zorluğun kaynağı olmamalı: varken aynı seviyeyi iki kez oynamak iki
farklı problem çözmek demekti.

### Gri kutudan çıkış

Gri kutu kuralı Faz 1 için konmuştu ve orada doğruydu: soru "dokunmatik girdiyi
fiziğe nasıl bağlarım"dı, sanat o sorunun cevabını gizlerdi. Faz 3'te bağlam
değişti — bu repo bir portföy parçası olarak açılıyor ve açanların çoğu koda
bakmadan önce ekran görüntüsüne bakıyor.

Yön pastel ve düz renk. Estetik tercih kadar teknik bir seçim: kutuların
birbirinden ve zeminden ayrılması gerekiyor ve pastel palette her kutu farklı ton
alabiliyor. Sanatçı da gerektirmiyor — düz renk, ışık ve post-process; doku ve
model yok. Yan faydası, aynı sade formların artık kaza değil kasıtlı görünmesi.

Bütün renkler tek bir varlıkta. Dağıtılmış renkler hızlı ilerletiyor ama bütünü
görmeyi imkânsızlaştırıyor. Kutulara renk sırayla dağıtılıyor, rastgele değil:
rastgelede yan yana aynı renk gelebiliyor ve o an palet bozuk değil, kod bozukmuş
gibi görünüyor.

Zeminin arka planla karışması bir renk sorunu değil **aydınlatma** sorunu çıktı:
zemin hem yönlü ışığı hem gökyüzü ortam ışığını alıp beyaza yaklaşıyordu.

### Ezilme-uzama fiziğe dokunmuyor

Kutunun görsel gövdesi ayrı bir çocuk nesnede. Ölçeği rigidbody'de oynatmak
collider'ı da oynatır — görsel bir süsleme fiziği değiştirir ve kule kendi kendine
sallanmaya başlar.

Ezilme sönümlü bir sinüs: tek yönlü ezilip açılma "lastik" gibi duruyor, genliği
azalan salınım sert cismin çarpma anındaki titremesini taklit ediyor. Hacim
korunuyor, yoksa kutu bir an küçülüyor ve çarpma değil uzaklaşma gibi okunuyor.

Düşüşte uzama sonradan eklendi: ezilme tek başına çarpmayı anlatıyor ama
öncesindeki düşüş "hiçbir şey olmuyor" gibi duruyordu. Çarpma anında uzama
sıfırlanıyor — geçiş ne kadar keskin olursa çarpma o kadar sert okunuyor.

### "0 hata" bir doğrulama değil

Faz 3'te üç hata çıktı ve üçünde de derleme temizdi, sahne kuruluyordu, log
sessizdi; yalnızca oyun yanlış çalışıyordu.

Bileşenlerin `Start` sırası garanti değil — rüzgâr göstergesi, rüzgâr daha
ayarlarını okumadan "rüzgâr yok" deyip kendini kapatıyordu. `CopySerialized`
varlığın adını da kopyalıyor; adsız kalan bir varlığı `AssetDatabase` bulamıyor ve
sahnedeki bütün palet referansları sessizce boşa düşüyor. Ve yeni bir varlık
üretmek elde tutulan varlık referansını geçersizleştiriyor; atandığında Unity hata
vermeden alanı boş bırakıyor.

Üçünün ortak sonucu aynı: sahne kurulum aracı artık yazdığı her referansı geri
okuyup doğruluyor ve yazamadığında bağırıyor.

### Ses dosyadan gelmiyor, koddan üretiliyor

Oyundaki on sesin hiçbiri bir dosya değil. Hepsi açılışta örnek örnek
hesaplanıyor: zarf, filtrelenmiş gürültü ve birkaç sinüs.

Alternatif indirilmiş CC0 ses paketiydi ve dürüst olmak gerekirse **daha iyi ses
verirdi.** Sentezlemeyi seçmemin sebebi şu: bu projede başka hiçbir hazır varlık
yok ve ses, hazır varlıkların sızdığı en kolay yer. Üstelik oyunun ihtiyacı olan
seslerin tamamı darbe sesi — tok bir vuruş, bir tık, bir uğultu — ve bunlar
sentezlemesi en kolay ses ailesi. Melodik bir oyun yapsaydım bu karar yanlış
olurdu.

Sentezin kendisinde öğrendiğim iki şey:

**Perde düşüşü doğrusal olmamalı.** Vuruş sesi alçalan bir sinüsten geliyor; o
alçalma doğrusal olduğunda ses siren gibi duyuluyor. Karekökle düşürünce —
başta hızlı, sonra yavaş — vuruş gibi duyuluyor.

**Ham gürültü ile tık arasındaki tek fark bir filtre.** Aynı gürültüden alçak
frekansları çıkarınca "şşş" sesi "tık" oluyor. Alçak geçirip bırakınca ise tok
bir çarpma. Üç ayrı ses değil, aynı kaynağın üç filtresi.

Tek klip, hıza göre farklı ağırlıklarda cisim gibi duyulabiliyor: çarpma sertse
ses seviyesi yukarı, perde aşağı. İki ayrı klip üretmeye gerek kalmıyor.

Ses üretimini çalma katmanından ayırdım — `SfxPlayer` klibin nereden geldiğini
bilmiyor. Sentetik sesler kulağa ucuz gelirse tek yapılacak şey üretim sınıfını
dosya yüklemesiyle değiştirmek; tetikleme noktalarının hiçbiri değişmiyor.

Sesi de gözle doğrulayamadığım için bir denetim aracı yazdım: klipler süre, tepe
genlik, RMS ve bozuk örnek sayısıyla ölçülüyor ve `.wav` olarak dışa
aktarılıyor. Sebebi doğrudan yukarıdaki "0 hata bir doğrulama değil" dersi —
sentez kodu hatasız derlenip sıfır dolu bir tampon üretebilir ve hiçbir yerde
hata görünmez, oyun sadece sessiz çalışır.

### Dört tur yanlış yerde aramak

Bu, projede en çok vakit kaybettiğim hata ve kaybın sebebi teknik değildi.

Hız çizgileri bir kez **çalıştı**. Geri bildirim şuydu: *"sanki bana geliyormuş
gibi aşağı doğru gitmesindense."* Yani görünüyorlardı, sadece yönleri yanlıştı —
parçacık sistemi her zaman şeklin +Z yönüne fırlatıyor ve dönmemiş hâlde o yön
kameraya bakıyordu.

Yönü düzeltmek için başlangıç hızını sıfırladım ve hareketi `velocityOverLifetime`
modülüne devrettim. **Çizgiler tam o anda kayboldu.**

Sebep: gerilmiş parçacık (Stretched Billboard) kendi **hız vektörü boyunca**
uzatılarak çiziliyor. Hız sıfır olunca uzatılacak bir yön kalmıyor. Aynı sahnedeki
toz sisteminin bu sorunu yok, çünkü o gerilmiyor — kameraya dönük düz bir kart ve
hızı umursamıyor. Doğru düzeltme hızı öldürmek değil, yönünü çevirmekti: yayılım
şeklini X ekseninde 90° döndürmek, böylece +Z dünya -Y'ye bakıyor.

Ondan sonraki dört tur boyunca sebebi başka yerlerde aradım. Bulduklarım gerçek
hatalardı ve düzeltilmeleri iyi oldu, ama hiçbiri **bu** hatanın sebebi değildi:

1. **Kontrast yoktu** — çizgiler saydam beyazdı, gökyüzü de neredeyse beyaz.
2. **Saydamlık iki kez uygulanıyordu** — URP'nin parçacık shader'ı malzeme
   rengini parçacık rengiyle çarpıyor; ikisine de saydam renk verince
   0.43 × 0.43 = 0.19 opaklık çıkıyordu.
3. **Parçacıkların yarısı kutunun içinde doğuyordu** — yayılım derinliği ±0.125,
   kutu 1 birim derin; o aralıktakiler opak kutunun içinde kalıyordu.
4. **Doğrulanmamış bir saydamlık yapılandırması** — malzeme için elle beş ayar
   kurmuştum (yüzey tipi, harmanlama, derinlik yazımı, render sırası, keyword) ve
   hiçbirinin çalıştığını görerek doğrulamamıştım. Toz aynı shader'ı varsayılan
   opak hâliyle kullanıyor ve çalıştığı kesin, o yüzden bir tur için kanıtlanmışa
   döndüm. (Sebep bu da değilmiş. Efekt çalıştıktan sonra saydamlık geri geldi ve
   ilk kez gerçekten doğrulanabildi — opak çizgiler hava değil çubuk gibi
   duruyordu.)

Dördü de düzeldi ve ekranda hâlâ hiçbir şey yoktu.

**Asıl ders:** bir şey önce çalışıp sonra çalışmayı bıraktıysa, sebep benim
değiştirdiğim şeydedir. Bu bilgi ilk günden elimdeydi — "görüyordum, yönü
yanlıştı" cümlesi tam olarak bunu söylüyordu. Ben onu bir *yön* raporu olarak
okudum, oysa aynı zamanda bir *çalışıyor* raporuydu. Sonraki dört turu, hiç
kırılmamış olan renk ve saydamlık katmanlarını inceleyerek geçirdim.

İkinci ders ölçüyle ilgili. Üç tur tahmin ettikten sonra debug paneline düşüş
hızı, üretilen ve canlı parçacık sayısı ile "çiziliyor mu" bayrağını ekledim
(F1 ile açılıyor). Bu sayılar sebebi tek başına söylemedi ama aramayı daralttı:
parçacıkların üretildiğini ve yaşadığını gösterince renk ve eşikle ilgili bütün
hipotezler bir anda elendi. Paneli F1'e bağlamamın sebebi de bu turda anlaşıldı —
yalnızca Inspector'dan açılan bir ölçü aleti, ölçmek istediğim anda elimde
olmuyor ve ben tahmin etmeye başlıyorum.

### Çizgiyi çizgi yapan şey doku, geometri değil

Efekt görünür olduktan sonra ortaya ikinci bir sorun çıktı: çizgiler çizgi gibi
değil, dikdörtgen prizma gibi duruyordu. Sebep basit — dokusuz bir parçacık düz
bir dikdörtgen olarak çiziliyor, gerilince de kenarları keskin, iki ucu düz
kesik bir çubuk oluyor.

Parçacığı daha da inceltmek oranı düzeltirdi ama keskin kenar ve düz uç yerinde
kalırdı; çubuk sadece incelirdi. Trail modülü gerçek bir iz çizer ama her
parçacık için ayrı mesh üretiyor ve burada gereken tek şey görüntü. Doku ile
çözdüm: alfası ortada parlak, iki uca ve iki kenara doğru sönen 128×16'lık bir
görüntü. Çizgi hissi tamamen buradan geliyor.

Dokuyu kodla üretip PNG olarak yazıyorum. Sanatçısı olmayan bir projede elle
çizilmiş 8 KB'lık bir dosyanın depoda ne işi olduğunu birkaç ay sonra kimse
hatırlamaz; formül kodda durunca "çizgi neden böyle görünüyor" sorusunun cevabı
da kodda oluyor. Yine de diske yazılıyor, çünkü malzeme bir varlık ve varlıklar
birbirine GUID ile bağlanıyor — çalışma zamanında üretilen bir doku sahne
kaydedilince boşa düşer.

### Arayüz sahnede değil, kodda

Menü ve tur sonu ekranı kanvaslarını çalışma zamanında kendileri kuruyor. Arayüzde
elle ayarlanacak hiçbir şey yok — ne sanat, ne düzen, ne yazı tipi — ve sahneye
kurulunca ortaya diff'i okunmayan onlarca RectTransform'luk YAML çıkıyor. Sanat
girseydi bu karar tersine dönerdi.

EventSystem de kullanmadım. uGUI'nin `Button` bileşeni bir EventSystem, bir input
modülü ve `GraphicRaycaster` istiyor; proje yalnızca yeni Input System kullandığı
için bunu doğru kurmak ayrı bir bakım borcu. Düğmelerim dikdörtgen ve bir dokunuşun
içeride olup olmadığı tek satır. Sürükleme ya da klavye gezinme gerekseydi bu
yanlış karar olurdu.

Menü ile tur arasında sahneyi yeniden yüklüyorum. Aynı sahnede turu "temizlemek"
mümkündü ama tur bittiğinde ortada onlarca rigidbody ve yarım kalmış fizik durumu
oluyor; onları tek tek temizleyen kod, her yeni nesne eklendiğinde güncellenmesi
gereken bir borç. Seçim statik bir sınıfta taşınıyor.

### Tehditler yalnızca havadaki kutuya dokunuyor

Rüzgâr ve kenarda gezinen top atıcı, ikisi de aynı kurala uyuyor: duran kuleye
dokunamıyorlar. Sebebi basit — oyuncu hiçbir hata yapmadan kaybediyorsa bu ceza
değil haksızlıktır.

Rüzgâr sadece bırakılmış ve henüz oturmamış kutuya kuvvet uyguluyor. Top atıcının
namlusu ise kulenin tepesinin altına hiç inmiyor: bunu çarpışma katmanıyla değil
geometriyle sağladım, çünkü filtre unutulur, geometri unutulmaz.

Namlu sabit yükseklikte bir bantta geziniyor, ekranın altına da üstüne de inmiyor.
İlk hâlinde bant kule tepesi ile kutunun beliriş yüksekliği arasındaydı; ikisi de
her kutuda değiştiği için `PingPong`'un aralığı sürekli değişiyor ve namlu
ışınlanıyordu. Sabit bant hem o hatayı kökünden kesiyor hem de daha iyi tasarım:
öngörülemeyen tehdit zorlaştırmaz, sinirlendirir.

Top atıcıyı hareketli bir engel çubuğuna tercih ettim. Çubuk tek seferlik bir nişan
alma problemi — bir kez çözersin, her seferinde aynı şekilde çözülür. Aralıklarla
ateş eden bir namlu ise ritim problemi: aynı seviyeyi ikinci kez oynadığında da
beklemek zorundasın.

Rüzgârın ekranda bir göstergesi var: kadrajın üstünde, esme yönüne doğru uzayan bir
çubuk ve ucunda bir eşkenar dörtgen. Uzunluğu şiddeti, yönü yönü veriyor. Buna
ihtiyaç oynarken çıktı — görünmeyen bir kuvvet zorluk değil kafa karışıklığı
üretiyor, oyuncu kendi hatasını arıyor.

Göstergenin ilk hâli işe yaramadı ve sebebi görselde değil mantıktaydı: rüzgârı
yalnızca kuvvet uygularken hesaplıyordum, yani gösterge tam da bakılması gereken
anda — kutu bırakılmadan önce — sıfırdı. Atıştan sonra rüzgârı görmenin bir değeri
yok. Rüzgâr artık bir ortam değeri: kutu havada olmasa da esiyor, sadece dokunacak
bir şey bulamıyor.

### Tepe noktasında "durmuş" görünen kutu

Kamera ve namlu arada bir yukarı fırlayıp geri iniyordu. Sebep: yukarı savrulan bir
kutu tepe noktasında neredeyse sıfır hıza iniyor, çünkü yükseliş bitip düşüş
başlarken hız işaret değiştiriyor. O tek karede kutu "oturdu" sayılınca kule birden
havadaki kutu kadar uzuyor, kamera onu takip ediyor, kutu düşünce geri iniyordu.

Hata top atıcıdan önce de vardı; kutuyu yukarı savuracak bir şey olmadığı için
neredeyse hiç tetiklenmiyordu. Çözüm kazanma kontrolündekiyle aynı fikir: tek kare
"durdu" görmek yetmez, 0.2 saniye kesintisiz durmak gerekir. Tepe noktası bir kare
sürer.

### Tehdit koridoru, düşme mesafesinden ayrı bir sayı

Topun gezineceği koridoru uzatmanın doğal yolu bırakma mesafesini büyütmekti.
Olmuyor: serbest düşüşte hız yükseklikle karekök olarak artıyor, 4 birimden düşen
kutu yere ~9 m/s ile çarpıyor. O hızda kutu yerleşmiyor, kuleyi süpürüyor — düşme
mesafesi oynanabilirlik tavanına dayanmış durumda.

Oysa koridorun uzun olması gereken kısmı düşüş değil, oyuncunun kutuyu aşağı
indirdiği kısım. İkisini ayırdım: kutu bırakma çizgisinin epey üstünde beliriyor,
oyuncu onu topun arasından indiriyor, bıraktıktan sonraki düşüş güvenli mesafede
kalıyor.

Kameranın tepe boşluğu da bu yüzden sabit değil artık. Gereken boşluğu, sayıyı
zaten hesaplayan kuyruk söylüyor; kamera tabanla istenenin büyüğünü kullanıyor.
Böylece uzun koridorlu seviyede kadraj açılıyor, kısa olanlarda kule kadrajın
dibine itilmiyor.

### Bırakılan kutu geri alınmıyor

Yerleştirdiğin kutuyu tekrar tutup yeniden bırakabiliyordun. Bu oyunun bütün
zorluğunu siliyor: beğenmediğin her atışı düzeltebiliyorsan bırakma mesafesinin de
kule dengesinin de bir anlamı kalmıyor, herkes mükemmel kuleyi kuruyor — sadece
daha uzun sürede. Bir atış bir karardır; geri alınabilen karar karar değildir.

### Kule yüksekliği: havadaki kutu kulenin parçası değil

Kamera her atışta hopluyordu. Kutu bırakıldığı anda yığının parçası sayılıyordu ama
o an havada ve bırakma mesafesi kadar yukarıda: kamera "kule iki birim uzadı" deyip
yukarı çıkıyor, kutu iniyor, kamera geri iniyor.

Kule yüksekliği artık yalnızca **bir kez oturmuş** kutuları sayıyor. Etiket kalıcı:
sonradan sallanan kutu listeden düşmüyor, çünkü düşseydi kule sallandığında
yükseklik anlık azalır ve kamera bu sefer aşağı hoplardı.

### Geçmek bir an, tutunmak bir süre

İkinci seviyeyi oynarken hedefi geçtim, kule hafifçe kayıyordu, on saniye sonra
devrildi. Ama kazanmıştım — çünkü kazanma kontrolü "hedefi geçti ve yığın durdu"
diyordu ve yerleşme eşiği 0.1 rad/s'e izin veriyordu. Saniyede 5.7 derece: duran
bir kule değil, yavaş devrilen bir kule.

İki ayrı eşik kullanıyorum artık, çünkü iki farklı soru soruluyor. "Sıradaki kutu
gelebilir mi" gevşek eşikle cevaplanabilir; yanılırsa oyuncu bir saniye erken kutu
alır. "Bu tur kazanıldı mı" yanılırsa oyun yalan söyler — o soru 0.02 rad/s'lik
sıkı eşiği hak ediyor.

Eşik tek başına da yetmiyor: kule hedefin üstünde **1.5 saniye kıpırdamadan durmak
zorunda**. Bunun için kural katmanına üçüncü bir cevap eklendi — `Pending`: "karar
askıda, ama sıradaki kutuyu da verme". Oyuncunun elinde kutu varken kaybetmesi,
seyrederken kaybetmesinden başka bir şey olurdu.

Hedef çizgisi o sırada sarıya dönüyor, yeşil ancak tutunduktan sonra geliyor.
"Geçtin ama henüz kazanmadın"ı menü kurmadan söylemenin yolu bu.

### Kaybetmek mümkün değilmiş

Sonsuz modun bitiş koşulunu yazarken fark ettim: kaybetme kontrolü "bir parça
zeminin altına düşerse" diyordu ama zemin 14 birim geniş. Kuleden devrilen kutu
zemine oturuyor, ölüm yüksekliğinin altına hiç inmiyor. Sonsuz mod hiç bitmezdi;
seviye modunda da enkazın üstüne yığmaya devam edilebiliyordu. Beş kutuda hedefe
ulaşıldığı için beş gün boyunca fark etmemişim.

Ölçülmesi gereken şey kutunun nereye gittiği değil kulenin kısalması. Tur boyunca
ulaşılan en yüksek **oturmuş** boyu tutuyorum; boy zirvenin 0.6 birim altına
düşerse tepeden kutu gitmiş demektir, tur biter. Zirveyi sadece oturmuş ölçümle
güncellemek şart: sallanan kule bir kare için olduğundan yüksek okunuyor ve o
sahte zirve yazılsa sonraki her ölçüm "çökmüş" görünürdü.

Zemini daraltmak da işi görürdü ama o, kuralı sahne geometrisine yazmak olurdu —
zemin boyutunu değiştiren biri farkında olmadan oyunun zorluğunu değiştirirdi.

### Assembly definition kullanıldı

`_Project` altındaki kod ayrı bir assembly (`PhysicsStack.Runtime`). İki sebep:
tek script değişince tüm proje değil sadece bu assembly derleniyor; ve Editor kodu
runtime koduna referans verebilirken tersi mümkün olmuyor — sınır dille zorlanıyor.

---

## Klasör yapısı

```
Assets/
  _Project/          -> benim yazdığım her şey
    Scenes/          -> Main.unity
    Scripts/
      Runtime/       -> PhysicsStack.Runtime assembly
      Editor/        -> PhysicsStack.Editor assembly
    Prefabs/
    Data/            -> ScriptableObject varlıkları
      Levels/        -> seviye varlıkları (koddaki eğriden üretiliyor)
    Art/
      Materials/
      Models/
    Settings/        -> URP ayarları, Physics Materials
  ThirdParty/        -> dışarıdan gelen her şey
```

Kural: dışarıdan gelen hiçbir şey `_Project` altına girmez. Bir günü kendi kodumla
paket kodunu ayırt etmeye harcamak istemiyorum.

## Repo ve sürüm kontrolü

Proje iki makine arasında Git ile taşınıyor.

- Commit'lenen: `Assets/`, `ProjectSettings/`, `Packages/`
- Commit'lenmeyen: `Library/`, `Temp/`, `Logs/`, `Build/`, IDE dosyaları
- `.meta` dosyaları **daima** commit'lenir — kaybolan GUID, sahnedeki tüm
  referansların kopması demek.
- Editor ayarı: Asset Serialization = **Force Text**, Version Control =
  **Visible Meta Files**. İkisi de sahne/prefab dosyalarını diff'lenebilir tutmak için.
- Binary varlıklar için Git LFS `.gitattributes`'ta baştan kurulu.

## Kurulum

```bash
git clone <repo-url>
```

Unity Hub'dan 6000.5.10f1 ile aç. İlk açılış paketleri çözümlediği için uzun sürer.
Sahne: `Assets/_Project/Scenes/Main.unity`.

## Telefon build'i

```bash
# Android
Unity.exe -batchmode -quit -projectPath . -buildTarget Android           -executeMethod PhysicsStack.EditorTools.PlayerBuilds.BuildAndroid

# WebGL
Unity.exe -batchmode -quit -projectPath . -buildTarget WebGL           -executeMethod PhysicsStack.EditorTools.PlayerBuilds.BuildWebGL
```

Çıktı: `Build/Android/PhysicsStack.apk` ve `Build/WebGL/` (Build klasörü
commit'lenmiyor). Editörden almak istersen aynı işler `PhysicsStack > Build`
menüsünde.

**WebGL nasıl yayına gidiyor:** `Build/WebGL` klasörünün içinde ayrı bir Git
deposu var; `gh-pages` dalına bağlı ve GitHub Pages o dalın kökünden servis
ediyor. Build alındıktan sonra o klasörde `git add -A && git commit && git push`
yeterli. `Build/` ana depoda `.gitignore`'da olduğu için ikisi birbirine karışmıyor.

Üç tuzak var.

**Push'tan hemen sonra siteyi yargılama.** Pages yeni commit'i işlerken bir süre
eski ve yeni dosyaları karışık servis ediyor; Unity çalışma zamanı o aralıkta
uyumsuz parçalarla açılıp "Maximum call stack size exceeded" gibi tamamen alakasız
bir JS hatası veriyor. Bir kez buna kodda hata arayarak vakit harcadım. Hatanın
iki farklı cihazda **aynı anda** çıkması teşhisin kendisiymiş: kod hatası olsaydı
cihazdan bağımsız olarak kalıcı olurdu, yayılma sorunu ise birkaç dakikada
kendiliğinden geçiyor.

**Gömülü depo ana depodan hiçbir şey miras almıyor.** Kullanıcı adı/e-posta ana
depoda local olarak ayarlıysa oradaki commit'ler makine kimliğiyle atılıyor ve
GitHub hesabıyla eşleşmiyor.

**Build boyutu ölçümü.** O `.git` klasörü de sayılıyordu; ölçüm artık onu ve
Burst'ün `_DoNotShip` klasörünü atlıyor.

Sahneyi ve seviyeleri de kod kuruyor: `PhysicsStack > Sahneyi Sifirdan Kur` ve
`PhysicsStack > Seviyeleri Yeniden Kur`. İkincisi mevcut seviye varlıklarının
üstüne yazıyor, yani oynayarak yaptığım ayarları koddaki eğriye döndürüyor.

**Build ayarları neden kodda?** Inspector'dan tıklayarak da yapılabilirdi, ama bu
proje iki makine arasında Git ile taşınıyor ve tıklamaların bir kısmı taşınmıyor:
aktif build target ve çıktı yolu makinede kalıyor. Ayarlar `AndroidBuild.cs`'te
durunca build'in nasıl alındığı repoda yazılı oluyor ve iki makinede aynı çıktıyı
veriyor. Build başarısız olursa script batchmode'da `Exit(1)` veriyor — sessizce
geçen bir hata, yeşil görünen kırık bir build demek olurdu.

**IL2CPP + ARM64** seçildi. Mono ARM64'ü desteklemiyor ve modern telefonların bir
kısmı 32-bit çalıştırmıyor; ilk build uzun sürüyor (C++ runtime da derleniyor) ama
telefonda çalışmayan bir APK'yı hızlı almanın değeri yok.

Ekran yönü portre kilitli. Prototipte yataydı — kamera yatay kadraj için kuruluydu.
Faz 2'de kamera görünür genişliği sabitleyecek şekilde yeniden yazılınca portreye
geçmek mümkün oldu; kule yukarı büyüyen bir oyunun doğal yönü de bu ve tek elle
oynanabiliyor.

## Kulenin tavanını yükseltmek

Sonsuz modu ilk denediğimde turlar 8 kutu civarında bitiyordu. İlk tepkim tehdit
eşiklerini aşağı çekmek oldu — merdiven görülemiyorsa merdiveni indir. Yanlış
tepkiymiş: 8 kutuluk bir tavan sadece sonsuz modu kısaltmıyor, üstüne seviye
tasarlanacak alanı da bitiriyor. Bir oyun 8 bloğun üstüne çıkamıyorsa ileri
seviye diye bir şey tasarlayamazsın.

Tavanı belirleyen şeyi zaten kendi notumda yazmıştım: kutu kule tepesinden 3
birim yukarıdan bırakılıyor ve serbest düşüşle ~7.7 m/s ile çarpıyor. Kuleyi
deviren şey tek bir kötü atış değil, her inişte biraz büyüyen sallanma — yani
sorun nişan değil, çarpma enerjisi.

Üç değişiklik de aynı yere bakıyor:

- **Bırakılan kutuya hava sürtünmesi** (`fallDrag = 1.2`). Düşüş hızı ~7.7'den
  ~5.5 m/s'ye iniyor, enerji neredeyse yarıya. Bırakma mesafesini kısaltmak
  oyunun tek risk kolunu sökerdi; yerçekimini azaltmak sürüklemeyi ve çöküşü de
  ağır çekime alırdı. Sürtünme yalnızca serbest düşüşe dokunuyor: nişan zorluğu
  ve koridorun uzunluğu aynı kalıyor, değişen tek şey çarpmanın sertliği.
- **Açısal sürtünme 0.35 → 0.6.** Sallanma dönmedir, frenlenecek yer orası.
- **Çözücü tekrarları 12 → 20.** Bu bir kolaylaştırma değil, simülasyon
  kalitesi: temas noktaları daha az kayıyor.

Sabit fizik adımına (0.02) dokunmadım, çünkü sürükleme çekirdeği hızı
`delta / Time.fixedDeltaTime` ile hesaplıyor: adımı kısaltmak takibi sertleştirir
ve his ayarını, tavanla hiç ilgisi olmayan bir yerden bozardı.

Bedeli de var ve baştan kabul ettim: 8 seviyenin zorluğu eski fiziğe göre
ayarlanmıştı, hepsi bir miktar kolaylaştı. Zaten amaç buydu.

## Kontrol noktası

Fizik sağlamlaştıktan sonra sonsuz modda tur 8 kutudan 13'e çıktı, ama tavan
kalkmadı — sadece yükseldi. Sallanma en alttan başlıyor ve her kutu onu biraz
büyütüyor; belli bir yükseklikten sonra kule benim becerimle değil birikmiş
salınımla devriliyor. Bir oyun 100. seviyeye gelip hâlâ "15 kutu yığ" diyecekse
zaten orada bir sınır var demektir.

Kontrol noktası bu zinciri kesiyor: belirli kutu sayılarında o ana kadar oturmuş
bütün kutular kinematik oluyor. İtilemiyorlar ama hâlâ çarpışıyorlar — kule için
yeni bir zemin. Yan kazanç, donan cisimlerin çözücünün dışına çıkması: 20
tekrarlı çözücüde 30 kutunun 20'si hesaptan düşüyor.

Dondurma kutu bırakıldığında değil **kule tam durduğunda** yapılıyor. Sallanan
bir kuleyi dondurmak eğikliği kalıcılaştırır ve oyuncunun düzeltme şansı olmadan
verilmiş bir ceza olur. Eğikliği düzeltmiyorum da: o eğiklik oyuncunun bırakma
biçiminden geldi.

Sonsuz modda noktalar 10, 25, 45, 70, 100... — aralık her seferinde 5 kutu
büyüyor. Sabit aralık daha basit olurdu ama yanlış şeyi ölçer: ilk 10 kutu ile
90'dan 100'e giden 10 kutu aynı iş değil. Seviye modunda `checkpointEvery`
alanı var ve varsayılanı kapalı; sekiz seviyenin hiçbirinin ihtiyacı yok, ama
ileride yüksek kule isteyen bir seviye tasarlarken mekaniği baştan yazmak
istemiyorum.

Geri bildirimin üç katmanı var ve biri kalıcı: kısa bir ezilme ve bir ses "az
önce bir şey oldu" diyor, rengin bir basamak koyulaşması "bu kutular artık
sabit" demeye devam ediyor. Ayrı bir gri "donmuş" rengi vermedim — tek gri renk
kuleyi renklerinden eder ve donmuş kısım oyunun dışından gelmiş gibi dururdu.

## Sonsuz modun zorluk eğrisi

Sonsuz modda uzun süre hiç tehdit yoktu ve gerekçesini yazmıştım: tek bir şeyin
— bırakma mesafesinin — sürekli artması, üst üste binen üç şeyden daha okunur
bir tırmanış verir. Gerekçe doğruydu ama eksikti. Mesafe 15 kutuda tavana
vuruyor; ondan sonrası sabit zorlukta bir tur. Yani tırmanışı okunur kılayım
derken tırmanışın kendisini 15 kutuyla sınırlamışım.

Tehditleri üst üste yığmak yerine sıraya dizdim. Her biri bir öncekinin doyduğu
yerde giriyor:

| Kutu | Giren şey |
|---|---|
| 0–15 | bırakma mesafesi 2.5 → 3.6, genişlik oynaması 0 → 0.25 |
| 6–14 | rüzgâr 0 → 1.0 m/s, sabit yönlü |
| 14+ | rüzgâr yön değiştirmeye başlıyor |
| 18+ | namlu devreye giriyor |

Sıra rastgele değil. Sabit yönlü rüzgâr bir kez öğrenilip telafi edilen bir şey;
salınım telafiyi zamanlamaya bağlıyor; namlu ise bir ritim problemi. Üçü aynı
anda gelseydi 18. kutuda oyuncu neyi yanlış yaptığını göremezdi. 18'den sonra
hiçbir şey artmıyor: sonsuza kadar tırmanan bir eğri, oyuncunun becerisinin
değil eğrinin kazandığı bir yer yaratır.

Bunun için kural arayüzü değişti — `Hazards` özelliği `HazardsFor(snapshot)`
metodu oldu. Eski hâlin gerekçesi de belgede yazılıydı: "rüzgâr ve top sahnede
duran şeyler, her kutuda yeniden pazarlık edilmiyorlar." Sonsuz modda tehdit tur
içinde büyümeye başlayınca o cümle doğru olmaktan çıktı. Yazılı bir gerekçenin
asıl faydası burada görünüyor: neyin değiştiğini değil, hangi varsayımın artık
tutmadığını gösteriyor.

Değer kutu başına bir kez hesaplanıyor, kare başına değil — oyuncu bir kutuyu
bir rüzgârla indirip aynı kutu inerken başka bir rüzgâr bulmasın diye.

## Durum

Günlük kararlar ve notlar: [docs/KARARLAR.md](docs/KARARLAR.md)

**Faz 1 — prototip (bitti, `v1-prototype`)**

- [x] Gün 1 — Proje şablonu, .gitignore, klasör yapısı, ilk commit
- [x] Gün 2 — Sürükleme çekirdeği
- [x] Gün 3 — Kazanma/kaybetme, yerleşme tespiti, kutu kuyruğu
- [x] Gün 4 — His ayarı, değerlerin SO'ya taşınması, debug overlay
- [x] Gün 5 — Build hattı (Android + WebGL), bitiş göstergesi, README kapanışı

**Faz 2 — oynanabilir sürüm** ([plan](docs/FAZ2.md))

- [x] Gün 6 — Kuleyi takip eden kamera, orana göre kadraj, portre yön
- [x] Gün 7 — Kural katmanı arayüzün arkasına (`LevelRules` / `EndlessRules`)
- [x] Gün 8 — Bırakma mesafesi mekaniği, 8 seviyelik veri ve zorluk eğrisi
- [x] Gün 9 — Rüzgâr ve top atıcı, tehdit koridoru, uyarlanır kadraj
- [x] Gün 10 — Menü, tur sonu ekranı, ilerleme kaydı
- [x] Kapanış — WebGL yayını, telefon testi, README v2

**Faz 3 — bitmiş gibi görünen sürüm** ([plan](docs/FAZ3.md))

- [x] Görünüş — pastel palet, gökyüzü shader'ı, post-process, TMP arayüz
- [x] His — ezilme-uzama, çarpma tozu, kamera sarsıntısı, hız çizgileri
- [x] Ses — koddan sentezlenen on efekt, ses açma/kapama, rüzgâr etiketi
- [x] İçerik — yıldız sistemi, seviye kartı, türetilen kutu ekonomisi ve zorluk
- [x] Sonsuz mod — tur içinde sıraya dizilen tehditler
- [x] Yükseklik tavanı — düşüş sürtünmesi, fizik ayarları, kontrol noktası
- [ ] Kapanış — 30 saniyelik kayıt, README v3, `v3` etiketi

## Kapsam dışı

Karakter, animasyon sistemi, reklam/IAP, bulut kayıt, çoklu dil, çoklu oyuncu.

Kapsam faz faz genişledi ve her genişleme bilinçliydi: seviye sistemi ve skor
kaydı Faz 1'de kapsam dışıydı, Faz 2'de girdi; sanat, ses ve parçacık efekti Faz
2'de kapsam dışıydı, Faz 3'te girdi. Değişmeyen kural şu: **hiçbiri hazır varlık
olarak gelmedi.** Palet, gökyüzü, parçacıklar, arayüz ve ses — hepsi bu repodaki
kodun çıktısı.
