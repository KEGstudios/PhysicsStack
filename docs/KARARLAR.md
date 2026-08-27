# Karar günlüğü

Her gün: ne denendi, ne seçildi, **neden**. Elenen yaklaşımlar da burada kalır —
elenmiş bir denemenin kaydı, hiç denenmemiş olmasından değerlidir.

---

## Gün 1 — İskelet

**Yapılan:** Unity 6 (6000.5.10f1) URP projesi, `_Project` klasör yapısı,
.gitignore / .gitattributes, assembly definition'lar, ilk commit.

**Kararlar:**

- *Assembly definition baştan mı, sonra mı?* Baştan. Sonradan eklemek, o ana kadar
  yazılmış her scriptin referanslarını tek tek düzeltmek demek. Şimdi dosya sayısı
  sıfırken bedeli de sıfır.
- *Git LFS baştan kuruldu.* Prototipte sanat yok ama repo iki makine arasında
  gidip geliyor. İlk binary dosya geçmişe girdikten sonra LFS'e almak, geçmişi
  yeniden yazmak demek.
- *Klasörler `.gitkeep` ile boş halde commit'lendi.* Yapıyı ilk günden sabitlemek,
  "acaba bunu nereye koysam" sorusunu ileride hiç sordurmuyor.

- *Sahne kodla kuruldu (`SceneBootstrap.cs`).* Zemin/kamera/ışık/kutu prefab'ını elle
  tıklamak yerine bir Editor menü komutu üretiyor. Sebep: his ayarlarken sahneyi
  bozmak kaçınılmaz; tek tuşla temiz başlangıca dönebilmek, "acaba neyi kaydırdım"
  diye aramaktan ucuz. Araç Editor assembly'sinde, build'e girmiyor.
- *Template artıkları silindi:* `Assets/Scenes/SampleScene`, `TutorialInfo/`,
  `Readme.asset`. `Assets/Settings/` (URP asset'leri) duruyor — GraphicsSettings
  oradan referans veriyor, taşımak render pipeline'ı kırar.

**Notlar:**

- Editor ayarları (Force Text = `m_SerializationMode: 2`, Visible Meta Files)
  URP template'inde zaten doğru geliyor; yine de ProjectSettings içinden doğrulandı.
- Android bundle kimliği hâlâ template'ten kalma
  (`com.UnityTechnologies.com.unity.template.urpblank`). Gün 5'teki telefon
  build'inden önce düzeltilecek.
- Runtime assembly'si şu an boş; Unity bunun için uyarı basıyor. Gün 2'de ilk
  script girince susacak.

---

## Gün 2 — Sürükleme çekirdeği

**Yapılan:** İki script. `PointerDragInput` parmağı/fareyi okuyup hedef noktayı
besliyor, `DraggableBody` fiziği uyguluyor.

**Kararlar:**

- *Girdi ve fizik ayrı sınıflarda.* Girdi platforma bağlı ve kare hızında, takip
  mantığı platformdan bağımsız ve sabit adımda çalışıyor. Aynı sınıfa koysaydım
  telefonda bozulan şeyin hangisi olduğunu ayırt edemezdim.
- *Üç yaklaşım da kodda, `FollowMode` enum'ıyla seçilebilir.* Elenenleri silmedim:
  hangisinin neden elendiği okuyarak değil, sırayla denenerek anlaşılıyor.
- *Sürükleme düzlemi orijinden geçen sabit bir düzlem*, kutunun yakalandığı
  noktadan geçen bir düzlem değil. Tek yığın var; kutu başına ayrı düzlem olsaydı
  yığın derinlemesine dağılır, kule kameradan "duruyor" görünüp aslında kutular
  birbirine değmezdi.
- *Yakalanan kutunun merkezi parmağa gidiyor*, yakalama offset'i korunmuyor
  (CLAUDE.md'deki formülün doğrudan karşılığı). Alternatif: kutunun neresinden
  tuttuysan orası parmakta kalsın — daha "gerçek" ama kule dizerken merkeze
  hizalamak zorlaşıyor. Gün 4'te his ayarında tekrar bakılacak.
- *`Pointer.current` kullanıldı* (proje Input System New'e ayarlı). Fareyi ve
  dokunmatiği aynı kod yolundan geçiriyor: editörde test ettiğim şey telefonda
  çalışan şeyle aynı.

**Elenenler ve neden (üçü de sahnede denendi):**

En net ayrım **bırakma anında** ortaya çıkıyor. Kutuyu hızlıca yana sürükleyip
hareket hâlindeyken bırakınca:

| Mod | Bırakınca | Çarpışma |
|-----|-----------|----------|
| DirectPosition | Olduğu yerde taş gibi düşüyor | Kutu diğerinin içinden geçip öbür tarafta beliriyor, sonra ikisi birbirini itip fırlıyor |
| KinematicMovePosition | Yine taş gibi düşüyor | Diğerlerini savuruyor ama kendisi hiç zorlanmıyor — sürüklenen şeyin ağırlığı yok |
| **VelocityFollow** | **Sürüklendiği yöne savruluyor** | Çarptığı şeye gerçekten çarpıyor, kendisi de yavaşlıyor |

Fırlatma için tek satır kod yazmadım; bırakma anında rigidbody'nin üstünde zaten
doğru hız olduğu için bedavaya geldi. Elenen iki yaklaşımda bu hız hiç oluşmuyor,
fırlatmayı ayrıca yazmak gerekirdi — ve o yazılan şey fizik değil taklit olurdu.

**Tünelleme notu:** DirectPosition'ın zeminden geçmesini bekliyordum, geçmedi.
Sebep sahneye özel: zemin 1 birim kalınlığında, kutu da 1 birim. Kutu zemine
ışınlanıyor, çözücü bir sonraki adımda dışarı itiyor — tam geçiş yerine gömülüp
zıplama oluyor. Tünelleme ince collider'larda görünür hale geliyor. Kusur duruyor,
sadece bu sahnede kendini başka türlü gösteriyor.

**His notu — iki kolun birbirine bağlı olduğunu geç fark ettim:**

`hız = (delta / fixedDeltaTime) * followStrength`, yani `delta * 50 * strength`.
`followStrength = 0.35` iken delta 0.8 birimi geçtiği anda sonuç `maxSpeed = 14`
sınırına takılıyor; strength 1'de bu eşik 0.28 birime iniyor. Gerçek sürüklemede
kutu zaten parmağın gerisinde kaldığı için delta neredeyse hep bu eşiklerin
üstünde — sonuç: kutu sürekli clamp'lenmiş halde gidiyor ve `followStrength`'i
çevirmek hiçbir şeyi değiştirmiyor.

Yani **şu an hissedilen gecikmenin tamamı `maxSpeed`'ten geliyor.**
`followStrength` ancak `maxSpeed` yüksekken (ör. 40) bir işe yarıyor.
İki değeri birbirinden bağımsız iki his kolu sanmak yanlıştı; `maxSpeed` baskın olan.

**Gün 4'e devreden:** Hız atamak yerine kuvvet uygulamak (`AddForce`) denenecek.
Şu anki halde her fizik adımında hızın üzerine yazdığımız için kutu çarpmaya
takılıp kalamıyor; direnç hissi bu yüzden zayıf. Kuvvet gerçek direnç verir ama
takip hassasiyetini düşürür, kule dizmek zorlaşır. Ölçülecek.

---

## Gün 3 — Kazanma / kaybetme

**Yapılan:** Üç sınıf — `BoxQueue` (üretim), `StackTracker` (ölçüm),
`StackGameController` (kural). `DraggableBody`'ye `Grabbed`/`Released` olayları
ve `HoldInPlace()` bekleme durumu eklendi.

**Kararlar:**

- *Ölçüm ile kural ayrı sınıflarda.* `StackTracker` sadece ölçüyor: her şey durdu
  mu, tepe nerede, bir şey düştü mü. Kazanma kararını controller veriyor. Gün 4'te
  eşikleri kurcalayacağım; ölçüm ile kural iç içe olsaydı her his denemesi kuralı
  da riske atardı.
- *Sıradaki kutu kinematik ve yerçekimsiz bekliyor*, ilk dokunuşta kendini serbest
  bırakıyor. Alternatif ayrı bir "bekleyen kutu" nesnesi tutmaktı — iki farklı kutu
  tipi, iki farklı kod yolu demekti. Tek tip kutu, iki durum daha ucuz.
- *Yerleşme tespitinde önce `IsSleeping()`, sonra kendi hız eşiğimiz.* Fizik motoru
  bir cismi uykuya aldıysa "bu artık hareket etmiyor" demiş oluyor; bu, elle
  seçilmiş bir eşikten güvenilir. Eşik yalnızca uykuya hiç girmeyen ama pratikte
  duran cisimler için yedekte.
- *Tek kare "duruyor" görmek yetmiyor.* Sallanan kule hız sıfırdan geçiyor ve o
  karede yanlışlıkla "oturdu" diyorduk. `settleGraceTime` (0.3 sn) kesintisiz
  durma şartı bu yanlış pozitifi eledi.
- *Kazanma sorusu "tepe hedefi geçti mi" değil, "oturduktan sonra hedefin üstünde mi".*
  Sallanan kule bir kare için hedefi geçip sonra devrilebiliyor; o kazanma değil.
- *Yükseklik collider sınırlarından ölçülüyor*, transform pozisyonundan değil.
  Kutu yan yattığında merkezi alçalır ama üst kenarı yükselir; kule yüksekliği
  dediğimiz şey ikincisi.
- *Kutu kaydı bırakıldığında değil yakalandığında yapılıyor.* Oyuncu kutuyu havada
  tutup zeminin altına sürüklerse bu da bir kayıp; yığının parçası sayılmalı.
- *Havuz (pooling) yok.* Bir turda üretilen kutu iki haneli sayıda. Erken
  optimizasyon burada okunabilirlikten çalardı; gerekirse yeri belli.
- *`TargetLine`:* y = 4'te ince, collider'sız gri çizgi. Hedef görünmeden oynamak,
  kaç kutu kaldığını sayarak oynamak olurdu.

**Oyun testinden çıkan iki düzeltme:**

İlk oynanışta kutular üst üste binince arkaya devriliyordu ve oyuncunun buna
müdahale şansı yoktu. İki ayrı kusur üst üste binmiş:

1. *Sürükleme düzlemi eğikti.* Kamera 8° aşağı baktığı için `-camera.forward`
   normaliyle kurulan düzlem de 8° yatıktı: parmağı yukarı sürüklemek kutuyu
   yukarı **ve arkaya** taşıyordu. Kutular farklı yüksekliklerde bırakıldığı için
   her biri farklı derinlikte kalıyor, kule kameradan düzgün görünürken aslında
   derinlemesine kayıyordu. Düzlem dünyanın XY düzlemine (z = 0) sabitlendi.
   "Kameraya bakan sabit düzlem" kararı yanlış değildi; kamera eğik olduğu anda
   o düzlemin yığın düzlemi olmaktan çıktığını hesaba katmamıştım.

2. *Kontrol 2D, simülasyon 3D idi.* Oyuncunun hiç erişemediği bir eksende fizik
   serbest çalışıyordu. Kule derinlemesine devrildiğinde yapılabilecek hiçbir şey
   yok — bu zorluk değil, adaletsizlik. Rigidbody kısıtı eklendi: z'de konum,
   x/y'de dönüş kilitli. Görüntü 3D kalıyor, simülasyon 2D oluyor; kutular hâlâ
   devriliyor, yuvarlanıyor, birbirini itiyor, ama yalnızca görünen düzlemde.

Karar şu cümlede toplanıyor: **kontrol 2D ise simülasyon da 2D olmalı.**
Serbest bıraktığın her eksen, oyuncunun kaybedebileceği ama kazanamayacağı bir eksen.

**Açık kalan — oynandı, cevap geldi:** Hedef yükseklik 4 birim (≈4 kutu) tahminî
bir başlangıçtı. İlk oturumda **4'ten fazlasını üst üste koymak çok zor** çıktı.

Gün 4'te önce sebebi aranacak, hedef küçültülerek kaçılmayacak. İlk şüpheli fizik
malzemesi: kutuların statik/dinamik sürtünmesi ayarlanmadı, şu an varsayılan
değerlerle kaygan duruyorlar. İkinci şüpheli sürükleme hassasiyeti — `maxSpeed`
baskın olduğu için kutuyu tam istediğim noktaya bırakmak zor olabilir.

**Yol boyunca öğrenilen:** Editör açıkken `-batchmode` aynı projeyi açamıyor;
süreç hiçbir şey yapmadan `return code 1` ile çıkıyor. Sahneyi kod üretiyorsa
editörün kapalı olması gerekiyor — ya da komutu editörün kendi menüsünden
çalıştırmak.

---

## Gün 4 — His ayarı, SO'ya taşıma, debug overlay

**Yapılan:** Gün 3'ün açık sorusuyla başladım: dörtten fazla kutu neden
konulamıyor. Hedefi düşürmek yerine sebebini aradım, üç ayrı yerde birikiyormuş.

1. **Çözücü iterasyonu az.** `Default Solver Iterations` 6 → 12,
   `Velocity Iterations` 1 → 2. Üst üste duran cisimlerde çözücünün her adımda
   bıraktığı hata birikip kuleyi kendiliğinden sallıyordu.
2. **Sürtünme hiç ayarlanmamış.** Collider'da fizik malzemesi yoktu; PhysX
   varsayılanı (0.6 / 0.6) ile kutular temasta yatay kayıyordu. `PM_Box`:
   statik 0.85, dinamik 0.6, zıplama 0, `Friction Combine = Maximum`.
   Yanına açısal sönüm 0.05 → 0.35.
3. **Asıl sebep — sürüklenen kutu kuleye her fizik adımında yeniden vuruyordu.**
   Hızı doğrudan atadığım için, temas anında çözücü hızı sıfırlıyor, ben bir
   sonraki adımda aynı hızı geri yazıyordum: saniyede elli darbe. Çözüm hızı
   atamak yerine `MoveTowards` ile hedefe doğru yürümek — adım başına değişimi
   `maxAcceleration` sınırlıyor, yani itişin sertliğinin üst sınırı var.
   Bırakma anına da `releaseSpeedClamp` koydum; hızlı bırakış kuleyi süpürüyordu.

Ayrıca his değerleri `DragSettings` ScriptableObject'ine taşındı ve ekranın
köşesine OnGUI ile durum paneli eklendi (durum / kule / kutu / oturma sayacı).

**Neden AddForce değil:** `ForceMode.Acceleration` birebir aynı hesap. Kuvvet
uygulayıp sonucu fiziğe bırakınca hızlanmayı kütle, sürtünme ve temas belirliyor;
"parmağa ne kadar yetişecek" sorusunun cevabı elimden çıkıyor. Bu haliyle hedef
hız benim, ona ulaşma sertliği fiziğin.

**Neden her ayar SO'ya girmedi:** Yerleşme eşiği `StackTracker`'da, hedef yükseklik
ve oturma süresi `StackGameController`'da kaldı. Onlar hisle değil kuralla ilgili;
hepsini tek varlığa doldurmak "ayarlar" adında bir çöp kutusu üretirdi.

**Bulunan değerler:** takip gücü 0.35 · maks hız 14 → **10** · maks ivme **90** ·
bırakma kırpması **5**. Maks hızı düşürdüm çünkü hissedilen gecikmenin tamamını o
belirliyor (Gün 2 notu) ve 14'te kutuyu istediğim noktaya bırakmak zordu.
İvme sınırı 90: 10 birim/sn hıza yaklaşık 0.11 saniyede çıkıyor — boşlukta fark
edilmiyor, temasta darbeyi kesiyor.

**Oynandı, cevap geldi:** Beş kutu üst üste kondu — Gün 3'te dört sınırdı, üç
düzeltme birlikte işe yaradı. Değerleri elle kurcalamama gerek kalmadı.

**Bu turda çıkan yeni gözlem:** Beşinciden sonra kutu gelmeyince "oyun takıldı mı"
diye düşündüm; halbuki kazanmıştım. Kural doğru çalışıyor (tepe hedefi geçti, yığın
oturdu, kuyruk durdu) ama **kazanma ekranda duyulmuyor** — köşedeki panelde tek
kelime olarak yazıyor, oynarken oraya bakmıyorsun. Prototipin menüsü olmayacak,
ama Gün 5'te telefonda kayıt alırken bitişin görülmesi lazım: hedef çizgisinin
rengini değiştirmek gibi kod tarafında ucuz bir işaret yeter.

---

## Gün 5 — Build ve kapanış

**Yapılan:** Önce bitişin görünürlüğü: hedef çizgisi kazanınca yeşile, kaybedince
kırmızıya dönüyor (`MaterialPropertyBlock` ile — `renderer.material` materyalin
kopyasını çıkarıp ayrı bir draw call açardı), ve bittikten sonra ekrana dokununca
sahne yeniden yükleniyor. İkincisi menü değil: telefonda kazandıktan sonra
yapılabilecek tek şey uygulamayı kapatmaktı ve günün çıktısı 30 saniyelik kayıt.

Build ayarları `PlayerBuilds.cs`'e yazıldı. Sebep: proje iki makine arasında Git ile
taşınıyor ve Inspector'daki tıklamaların bir kısmı taşınmıyor. Batchmode'da build
başarısız olursa `Exit(1)` veriyor — sessizce geçen bir hata, yeşil görünen kırık
bir build demek.

- **Android APK:** 32.5 MB, 8.7 dk (IL2CPP + ARM64). Mono ARM64 desteklemiyor,
  modern telefonların bir kısmı 32-bit çalıştırmıyor.
- **WebGL:** 12 MB, 9.1 dk (Brotli + `decompressionFallback`; GitHub Pages
  sıkıştırılmış dosyayı doğru başlıkla sunmadığı için yükleyicinin kendisi çözüyor).

**Plandan sapma — ve sebebi:** Plan "telefonda build" diyordu, elimdeki telefon
iPhone çıktı. iOS build'i Xcode ve macOS istiyor; Windows'ta mümkün değil. Android
APK yine de alındı (pipeline'ın kurulduğunun kanıtı) ama **gerçek cihazda
denenmedi**. Onun yerine WebGL build'i GitHub Pages'e konuldu: iPhone'da Safari'de
açılıyor, dokunmatik girdiyi gerçekten alıyor, yani "parmakla nasıl hissettiriyor"
sorusu gerçek cihazda cevaplanabiliyor. Yan faydası portföy için indirilen bir
dosya yerine tıklanır bir link.

**Bitmeyen ne kaldı — dürüst liste:**

- Android APK gerçek cihazda çalıştırılmadı. "Telefonda çalışıyor" demiyorum,
  çünkü denemedim.
- WebGL'in iPhone Safari'deki performansı ölçülmedi. Unity, WebGL'i mobil
  tarayıcıda resmî olarak desteklenen saymıyor; gerekirse `devicePixelRatio`
  düşürmek için özel bir WebGL şablonu gerekecek.
- Kamera sabit: kule yükseldikçe kadrajdan çıkıyor. Beş kutuda sorun olmuyor ama
  oynanabilirliğin önündeki en büyük engel bu.
- Ekran yönü yatay kilitli. Bu oyunun doğal yönü portre; kamera yeniden
  kurulmadan portreye geçilemezdi.
- Ses, menü, skor kaydı yok — bunlar bilinçli olarak kapsam dışıydı.

Son üçü Faz 2'nin ilk günü. Bkz. [FAZ2.md](FAZ2.md).

---

# Faz 2

## Gün 6 — Kamera, yön ve kadraj

**Yapılan:** Kamera kulenin tepesini takip ediyor, ekran yönü portreye döndü,
sıradaki kutu artık sabit bir yükseklikte değil kulenin biraz üstünde beliriyor.

**Kadrajı FOV değil genişlik belirliyor.** Portreye geçtiğimde kutu ekranda
görünmez oldu. Sebep bir hata değil, geometriydi: sabit bir dikey FOV ile 9:19.5
bir ekranda görünen dünya genişliği, 16:9'dakinin yarısından az oluyor. Bunu
tersine çevirdim — sabitlediğim şey artık **dünya biriminden görünür genişlik**,
dikey FOV her orana göre `tan(hFov/2) = en boy oranı · tan(vFov/2)` bağıntısından
hesaplanıyor. Böylece "kule ne kadar geniş görünüyor" cihazdan bağımsız hâle
geldi; değişen tek şey yukarıda ne kadar boşluk kaldığı, ki portrede zaten
istediğim şey o.

**Yükselirken hızlı, alçalırken yavaş.** Takip için tek bir yumuşatma süresi
kullanınca kule devrildiğinde kamera aşağı fırlıyor ve olan biteni kaçırıyorsun.
İki ayrı süre var: yükselirken 0.35 sn, alçalırken 1.1 sn. Kamera yeni kutuya
hemen yetişiyor ama yıkılışı seyrediyor.

**"Kulenin tepesi" eldeki kutuyu saymıyor.** Yeni kutu kulenin üstünde belirdiği
için, oyuncu ona dokunduğu anda ölçüm birden tavana fırlıyor ve kamera boşuna
zıplıyordu. `StackTracker` iki ayrı cevap veriyor artık: `HighestPointY` her şeyi
sayıyor, `HighestRestingPointY` elde tutulanı saymıyor. Kamera ikincisini
kullanıyor — kule yüksekliği, oyuncunun elindeki kutu değil yerleştirdiği kutular.

**Spawn kadraja değil kuleye bağlı.** Önce kutuyu kadrajın üst kenarına göre
konumlandırmıştım; kule yükseldikçe düşme mesafesi kısalıyor, başta ise kutu dokuz
birim yukarıdan düşüyordu. Şimdi kule tepesinin sabit 4 birim üstünde beliriyor,
kadraj sınırı yalnızca üst sınır olarak devrede. Düşme mesafesi turun her anında
aynı, yani "kutu nereye düşecek" tahmini öğrenilebilir bir şey oldu.

**Yarım kalan:** İlk iki kutuda spawn noktası hedef çizgisinin altında kalıyor.
`minSpawnHeight` ile kozmetik olarak kapattım ama genel bir çözüm değil — kule
büyüdükçe çizgiyi geçmek zaten doğal ve bilgi verici.

## Gün 7 — Kural katmanının ikiye ayrılması

**Yapılan:** `StackGameController` artık kimin kazandığını bilmiyor. Karar bir
arayüzün arkasına çıktı: `IStackRules`, iki uygulaması `LevelRules` ve
`EndlessRules`.

**Neden arayüz, neden şimdi.** İki modu tek sınıfa `if (sonsuz)` ile sığdırmak
bugün çalışırdı. Ayırmamın sebebi üçüncü modu düşünmek değil, şunu fark etmek:
controller'ın yaptığı işlerin — girdiyi dinlemek, sıradaki kutuyu istemek, yığının
oturmasını beklemek, durumu yayınlamak — hiçbiri modlara göre değişmiyor. Değişen
tek şey "bu anlık görüntü ne anlama geliyor" sorusunun cevabı. Sınırı oraya
çektim; başka bir yere çekseydim iki taraf da yarım olurdu.

**Kural sınıfları MonoBehaviour değil.** Düz C# sınıfı oldular: sahneye bağlı
değiller, mod değiştirmek bir nesne değiştirmek kadar ucuz ve kuralı sahne açmadan
test edebiliyorum. ScriptableObject da yapabilirdim ama o zaman her kural bir
varlık dosyası isterdi; Gün 8'de varlık olacak şey kuralın kendisi değil,
seviyenin **verisi**.

**`StackSnapshot` — ölçümü tek seferde dondurmak.** Kural sınıflarına tracker'ı
doğrudan verebilirdim, daha kısa olurdu. İki şey kaybederdim: kural sahnedeki bir
bileşene bağlanırdı ve aynı kararın içinde ölçümü iki kez okuyup iki farklı cevap
alma ihtimali doğardı. Karede bir kez okunuyor, struct olarak geçiliyor, çöp
üretmiyor.

**Kutu sınırı kaybettirmek için değil.** `LevelRules` isteğe bağlı bir kutu sınırı
alıyor. Amacı zorluk eklemek değil kimlik vermek: "beş kutuyla şu yüksekliğe çık"
ile "istediğin kadar kutuyla çık" iki farklı problem, ve seviye modunu birbirinin
kopyası on iki turdan kurtaracak şey bu ayrım.

**Hedef çizgisi artık veriye bakıyor.** Çizgi sahnede sabit bir yükseklikte
duruyordu; hedef kural setinden gelmeye başlayınca bu yalan söylemek demekti.
Şimdi kendini hedefe göre yerleştiriyor, hedefi olmayan modda tamamen kapanıyor.
Sonsuz modda tur bitince kulenin ulaştığı yüksekliğe taşınıp kırmızıya dönüyor:
aynı çizgi "geçmen gereken yer"den "geldiğin yer"e dönüşüyor. Menü kurmadan
sonucu göstermenin hâlâ en ucuz yolu bu.

**Günün asıl bulgusu: kaybetmek mümkün değilmiş.** Sonsuz modu yazarken bitiş
koşulunu kontrol ettim — `killHeight` -1, yani "bir parça zeminin altına düşerse
kaybettin". Ama zemin 14 birim geniş. Kuleden devrilen kutu zemine oturuyor,
ölüm yüksekliğinin altına hiç inmiyor. Yani sonsuz mod **hiç bitmezdi**, seviye
modunda da enkazın üstüne yığmaya devam edilebiliyordu; beş kutuda hedefe
ulaşıldığı için bunu beş gün boyunca fark etmemişim.

Ölçülmesi gereken şey kutunun nereye gittiği değil kulenin kısalması. Tur boyunca
ulaşılan en yüksek oturmuş boyu (`PeakHeight`) tutuyorum; oturmuş boy zirvenin
0.6 birim altına düşerse tepeden en az bir kutu gitmiş demektir, tur biter. Zirve
yalnızca oturmuş ölçümle güncelleniyor — sallanan kule bir kare için olduğundan
yüksek okunuyor ve o sahte zirve yazılsaydı sonraki her ölçüm "çökmüş" görünürdü.
Zemini daraltmak da bir çözümdü ama o, kuralı sahne geometrisine yazmak olurdu.

**Skor buraya kaydı.** Gün 6'nın planında skor ölçümü vardı, oraya koymadım:
"skor" kelimesi iki modda aynı şeyi anlatmıyor. Seviyede harcadığın kutu sayısı (az olsun),
sonsuz modda ulaştığın kule boyu (çok olsun). Ters yönler.

Sonsuz modda skoru kutu sayısı yapmayı düşünmüştüm, vazgeçtim: kutu sayısı kuleyi
hiç yükseltmeyen kutularla da artıyor, yani yere yan yana kutu dizerek skor
toplanabilirdi. Boy ancak gerçekten yükseldiğinde artıyor. Kuralı yazmak yerine
skoru doğru şeyi ölçecek biçimde seçmek, aynı sömürüyü ek bir kontrol olmadan
kapatıyor.

**Yarım kalan:** Mod seçimi hâlâ Inspector'daki bir enum; gerçek seçim Gün 9'da.
Sonsuz modun zorluk eğrisi yok, şu an sonsuza kadar aynı zorlukta — onu ilginç
kılacak şey Gün 8'de geliyor.
