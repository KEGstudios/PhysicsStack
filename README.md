# PhysicsStack — Prototip 1

3D kule yığma prototipi. Ekranın üstünden gelen gri kutuları parmakla sürükleyip
bırakıyorsun; fizik gerisini hallediyor. Kule hedef yüksekliği geçip **oturursa**
kazanıyorsun, bir parça zeminin altına düşerse kaybediyorsun.

Bu bir oyun değil, bir **his denemesi**: amaç dokunmatik girdiyi fizik dünyasına
doğru bağlamak. Sanat, ses, menü yok — hepsi gri kutu.

- Unity 6 (6000.5.10f1) · URP · hedef platform Android
- Süre: 5 gün, sabit. 5. günün sonunda repo kapanır.

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
devam mı ediyor, bitti mi. `LevelRules` hedefe ve isteğe bağlı kutu sınırına
bakıyor, `EndlessRules` sadece bir şey düştü mü diye bakıyor.

Kurallara `StackTracker`'ı doğrudan vermedim, `StackSnapshot` diye bir struct
veriyorum: yükseklik, yerleşmiş kutu sayısı, bir şey düştü mü, yığın oturdu mu.
İki kazancı var. Kural sınıfı sahnedeki hiçbir bileşene bağlı değil — MonoBehaviour
bile değiller, düz C# sınıfı. Ve aynı kararın içinde ölçümü iki kez okuyup iki
farklı cevap alma ihtimali yok; ölçüm karede bir kez donduruluyor.

Skoru da kural hesaplıyor, çünkü "skor" iki modda aynı şeyi anlatmıyor: seviyede
harcadığın kutu sayısı (az olsun), sonsuz modda ulaştığın kule boyu (çok olsun).
Ters yönler; controller'ın bunu bilmesi için bir sebep yok.

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
    Data/            -> ScriptableObject varlıkları (DragSettings.asset)
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
Unity.exe -batchmode -quit -projectPath . -buildTarget Android           -executeMethod PhysicsStack.EditorTools.AndroidBuild.BuildApk
```

Çıktı: `Build/Android/PhysicsStack.apk` (Build klasörü commit'lenmiyor).
Editörden almak istersen aynı iş `PhysicsStack > Android APK Al` menüsünde.

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
- [ ] Gün 8 — Seviye verisi ve zorluk eğrisi
- [ ] Gün 9 — Mod seçimi, tur sonu, ilerleme kaydı
- [ ] Gün 10 — Telefon testi, his ayarı, kapanış

## Kapsam dışı

Ses, sanat, parçacık efekti, karakter animasyonu, reklam/IAP, bulut kayıt, çoklu
dil. Bilinçli olarak yok — Faz 2 oyunu **oynanabilir** yapıyor, **yayınlanabilir**
değil.

Seviye sistemi ve skor kaydı Faz 1'de kapsam dışıydı, Faz 2'de kapsama girdi.
