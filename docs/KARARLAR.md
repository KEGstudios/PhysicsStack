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

## Gün 8 — Seviye verisi, ve oyunun asıl eksiği

**Gün yanlış plandan başladı.** Elimde 12 seviyelik bir tablo vardı ve tek
büyüyen sayı hedef yükseklikti: 2.5'ten 8'e. Tabloyu yazıp bakınca ilk
seviyelerin absürt kolay göründüğünü fark ettim, ama asıl sorun oydu sanmıştım.
Değildi.

Asıl sorun şuydu: **oyunda hiç risk yok.** Kutuyu kulenin üstüne milimetrik
yerine getirip sıfır hızla bırakabiliyordum. Beceri testi değil sabır testiydi.
Böyle bir oyunda zorluk ancak kutu sayısıyla artabilir — yani 5. seviyede 5
kutu, 100. seviyede 100 kutu. Yükseklik bir *miktar*; seviye ise bir *soru*
olmalı, ve 2.5 birim kolay bir soru değil, soru değil.

**Yeni mekanik: bırakma mesafesi.** Kutu, kule tepesinin belirli bir mesafe
üstünden bırakılmak zorunda; parmakla o çizginin altına indirilemiyor.
Yerleştirme bir *koyma* olmaktan çıkıp bir *atış* oluyor: x'i ve zamanlamayı
oyuncu, gerisini fizik belirliyor.

Üç şeyi birden çözdü:

- Zorluk artık tek bir mesafe. Aynı beş kutuyla çok daha zor bir soru sorabiliyorum.
- Gün 4'te ayarladığım his değerleri (takip gücü, ivme sınırı, bırakma hız
  kelepçesi) nihayet oyunu etkiliyor. Risk yokken hiçbiri bir şey ifade etmiyordu.
- Rüzgâr ve engel gibi sonraki mekaniklerin dayanacağı zemin bu: ikisi de ancak
  kutu havada yol kat ediyorsa anlamlı.

**Çizgi kutunun altını gösteriyor, merkezini değil.** İlk yazışta kısıtı
rigidbody merkezine koymuştum. Boyut oynayan seviyelerde bu, aynı sayının farklı
kutular için farklı düşme mesafesi anlamına geliyordu — zorluk sessizce
rastgeleleşirdi. Yarım boy üretim anında bir kez ölçülüyor; kutu döndükçe
yeniden ölçseydim kısıt oyuncunun elinde değişirdi.

**Yatay rastgelelik kaldırıldı.** Kutu artık her seferinde aynı noktada beliriyor.
Rastgelelik zorluğun kaynağı olmamalı: varken aynı seviyeyi iki kez oynamak iki
farklı problem çözmek demekti, dolayısıyla seviye tasarlamanın da bir anlamı
kalmıyordu.

**Seviyeler koddaki bir tablodan üretiliyor.** Sekiz seviyeyi tek bir tabloda yan
yana görmek, sekiz ayrı Inspector penceresinde görmekten iyi — eğri bir bütün.
Üretim mevcut varlığın üstüne yazmıyor; oynayarak ayarladığım sayıları sahneyi
tazelemek çöpe atmamalı. Eğriyi baştan kurmak için ayrı bir menü var.

**Sadece genişlik oynuyor, boy sabit 1 birim.** Önce ikisini birden oynatmıştım;
daha çeşitli görünüyordu ama seviyenin sorusunu bozuyordu. Kutu boyu değişince
aynı hedef yüksekliğe bazen beş, bazen altı kutuyla çıkılıyor — yani "bu hedefe
çıkabilir miyim" sorusunun cevabını kısmen zar veriyor. Genişlik tam tersi: kaç
kutu gerektiğini değiştirmiyor, sadece üst üste koymayı zorlaştırıyor. Zorluk
orada olmalı, sayımda değil.

Kütle genişlikle birlikte ölçekleniyor. Sabit kütle bırakılsaydı dar kutu taş gibi
ağır, geniş kutu köpük gibi hafif olurdu. Sürükleme hissi etkilenmiyor çünkü hızı
doğrudan atıyoruz; kütle yalnızca çarpışmalarda konuşuyor.

**Eğri.** Hedef yükseklik 3'ten 6'ya çıkıp orada duruyor; artan şey bırakma
mesafesi (1.0 → 3.5). Önce sadece mesafe (1-3), sonra kutu sınırı (4), sonra
genişlik oynaması (5). Her kısıt tek başına bir seviyede tanıtılıyor, sonrakiler
birleştiriyor. Sonsuz modda aynı eğri turun içinde, ilk 15 kutuda tepeye çıkıp
sabitleniyor — tavanı olmayan zorluk, oyuncunun becerisinin değil eğrinin
kazandığı bir yer yaratır.

**Reddedilen fikir: oyunu birleştirme (merge) oyununa çevirmek.** İyi bir loop
ama PhysicsStack'in devamı değil: sürükleme yok, kamera takibi yok, kule ölçümü
yok. Kendi beş gününü hak ediyor, [FIKIRLER.md](FIKIRLER.md)'ye yazıldı.

**İlk iki seviyeyi oynayınca iki şey çıktı.**

**Mesafeler çok kısaymış.** 1.0 birim, bir kutu boyu kadar düşüş demek — kutu daha
hızlanmadan yerine oturuyor. Eğri 2.0-4.0'a yükseltildi. Bunun bir yan maliyeti
var: kutu kendi bırakma çizgisinin üstünde belirmek zorunda olduğu için daha
yüksekten başlıyor, dolayısıyla kameranın tepe boşluğu 6'dan 7'ye çıktı. Kule
kadrajda biraz daha aşağıda duruyor artık; mekaniğin bedeli bu.

**Hedefi geçmek kazanmaya yetiyormuş — asıl hata bu.** İkinci seviyede hedefi
geçtim, kule hafifçe kayıyordu, on saniye sonra devrildi. Ama kazanmıştım.

Sebebi yerleşme eşiğiydi: 0.1 rad/s'in altındaki dönüş "durdu" sayılıyor. Saniyede
5.7 derece, yani on saniyede 57 derece. Duran bir kule değil o, yavaş devrilen bir
kule.

Eşiği sıkılaştırıp tek eşikle yürüyebilirdim ama ikisi farklı iş yapıyor: biri
"sıradaki kutu gelebilir" diyor (yanılırsa oyuncu bir saniye erken kutu alır),
diğeri "bu tur kazanıldı" diyor (yanılırsa oyun yalan söyler). İkinci soru daha
sıkı bir eşiği hak ediyor — 0.02 rad/s, saniyede bir derece.

Sadece eşik de yetmiyordu. **Geçmek bir an, tutunmak bir süre:** kule hedefin
üstünde 1.5 saniye kıpırdamadan durmak zorunda. Bunun için kurala üçüncü bir cevap
eklendi — `Pending`: "karar askıda, ama sıradaki kutuyu da verme". Oyuncunun elinde
kutu varken kaybetmesi, seyrederken kaybetmesinden farklı bir şey olurdu.

Ekranda hedef çizgisi o sırada sarıya dönüyor: "geçtin ama henüz kazanmadın"ı menü
kurmadan söylemenin yolu. Yeşil ancak tutunduktan sonra geliyor.

**Oynayınca iki şey daha çıktı.**

**Bırakılan kutu geri alınabiliyormuş.** Yerleştirdiğin kutuyu tekrar tutup
yeniden bırakabiliyordun. Bu, oyunun bütün zorluğunu siliyor: beğenmediğin her
atışı düzeltebiliyorsan bırakma mesafesinin de kule dengesinin de tutunma şartının
da bir anlamı kalmıyor, herkes mükemmel kuleyi kuruyor — sadece daha uzun sürede.
Bir atış bir karardır; geri alınabilen karar karar değildir. Kutu bırakıldığı anda
dokunulmaz oluyor artık.

**Kamera her atışta hoplyordu.** Sebebi şuydu: kutu bırakıldığı anda yığının
parçası sayılıyordu, ama o an kutu havada ve bırakma mesafesi kadar yukarıda.
Kamera "kule iki birim uzadı" deyip yukarı çıkıyor, kutu iniyor, kamera geri
iniyor. Mesafeleri büyütünce zıplama da büyüdü — yani hatayı ben görünür yaptım,
kendisi baştan vardı.

Kule yüksekliği artık yalnızca **bir kez oturmuş** kutuları sayıyor. Havadaki kutu
kulenin parçası değil; oturunca oluyor. Bu "bir kez oturmuş olmak" kalıcı bir
etiket: sonradan sallanan kutu listeden düşmüyor, çünkü düşseydi kule sallandığında
yükseklik anlık azalır ve kamera bu sefer aşağı hoplardı.

**Yarım kalan:** Rüzgâr ve hareketli engel Gün 9'a kaldı; 6-8. seviyeler şimdilik
sadece mesafe ve sınırla zorlaşıyor. Mod ve seviye seçimi hâlâ Inspector'da.
Tutunma süresi bütün seviyelerde 1.5 sn — bilerek: bu bir zorluk kolu değil,
oyunun kuralı, seviyeye göre değişseydi her seviyede yeniden öğrenilmesi
gerekirdi.

## Gün 9 — Tehditler: rüzgâr ve top atıcı

**Ortak kural: tehdit yalnızca havadaki kutuya dokunur.** Duran kuleyi bozan bir
tehlike, oyuncu hiçbir hata yapmadan kaybettirir — bu ceza değil haksızlıktır.
Rüzgâr sadece bırakılmış ve henüz oturmamış kutuya kuvvet uyguluyor; top atıcının
namlusu da kulenin tepesinin altına hiç inmiyor.

Bunu çarpışma katmanıyla değil **geometriyle** sağladım: namlu fiziksel olarak
kuleye ateş edemeyecek bir koridorda geziniyor. Filtre unutulur, geometri
unutulmaz.

**Rüzgâr kütleden bağımsız.** Fiziksel olarak rüzgâr yüzey alanıyla iter, yani
geniş kutu daha çok sürüklenmeli. Ama bizim geniş kutumuz aynı zamanda ağır ve iki
etki birbirini götürünce ortaya "bazı kutular neden daha çok savruluyor" diye
açıklanamayan bir düzensizlik çıkıyordu. Sabit ivme öngörülebilir; öngörülebilir
olan öğrenilebilir. 6. seviyede sabit yön (bir kez öğrenip telafi ediyorsun),
7'de salınan yön (telafi zamanlamaya bağlanıyor).

**Engel çubuğu yerine top atıcı.** Plandaki "hareketli engel çizgisi" fikrinden
vazgeçtim. Çubuk tek seferlik bir nişan alma problemi: bir kez çözülür, her
seferinde aynı şekilde çözülür. Kenarda gezinen ve aralıklarla ateş eden bir namlu
ise ritim problemi — aynı seviyeyi ikinci kez oynadığında da beklemek zorundasın.

Mermi yerçekimsiz ve düz gidiyor. Parabol çizen bir mermi, oyuncudan tehdidi
okumak için ayrı bir sezgi isterdi; tehdidin adil olması görülebilir olmasından
geçiyor. İlk çarpışmada yok oluyor, yoksa sahnede biriken mermiler kuleye yeni bir
zemin olurdu.

**Namlu her kutuda ışınlanıyordu.** İlk hâlinde bant "kule tepesi ile kutunun
beliriş yüksekliği arası" idi ve gezinme `PingPong(gidilen yol, bant yüksekliği)`
ile hesaplanıyordu. İkisi de her kutuda değişiyor: kutu oturunca taban, yeni kutu
belirince tavan zıplıyor. `PingPong`'un çıktısı aralık değişince sıçrıyor,
dolayısıyla namlu da sıçrıyordu.

Bant artık sabit yükseklikte ve kule tepesinin hemen üstünden başlıyor; tabanı da
kameranınkiyle aynı ilaçla, `SmoothDamp` ile yumuşatılıyor. Ölçüm ani değişiyorsa
gösterim ona yumuşayarak gitmeli.

Sabit bant aynı zamanda daha iyi bir tasarım: namlu ekranın altına da üstüne de
inmiyor, oyuncu nereye kadar çıkacağını biliyor. Öngörülemeyen tehdit zorlaştırmaz,
sadece sinirlendirir.

**Koridor uzunluğu düşme mesafesinden ayrıldı — asıl teknik karar bu.** Topun
gezineceği koridoru uzatmanın doğal yolu bırakma mesafesini büyütmekti. Olmuyor:
serbest düşüşte hız yükseklikle karekök olarak artıyor, 4 birimden düşen kutu yere
~9 m/s ile çarpıyor, 6 birimden ~11 m/s. O hızda kutu yerleşmiyor, kuleyi
süpürüyor. Yani düşme mesafesi oynanabilirlik tavanına dayanmış durumda.

Oysa koridorun uzun olması gereken kısmı düşüş değil, oyuncunun kutuyu aşağı
indirdiği kısım. Ayrı bir sayı yaptım (`spawnLift`): kutu bırakma çizgisinin epey
üstünde beliriyor, oyuncu onu topun arasından indiriyor, ama bıraktıktan sonraki
düşüş güvenli mesafede kalıyor.

**Kameranın tepe boşluğu artık sabit değil.** Uzun koridorlu seviyede kutu çok
yukarıda beliriyor; kamera onu göremezse spawn kırpılıyor ve kural sessizce
gevşiyor. Ama aynı boşluğu bütün seviyelere vermek kuleyi her seviyede lüzumsuz
yere kadrajın dibine iterdi. Gereken boşluğu, sayıyı zaten hesaplayan kuyruk
söylüyor; kamera tabanla istenenin büyüğünü kullanıyor. Dün 7'ye çıkardığım taban
6'ya geri döndü.

**Oynayınca üç şey daha çıktı.**

**Rüzgârın görünen hiçbir işareti yokmuş.** Kutu savruluyordu ama ortada rüzgâr
olduğunu söyleyen bir şey yoktu. Görünmeyen kuvvet zorluk değil kafa karışıklığı
üretiyor: oyuncu kendi hatasını arıyor. Kadrajın üstünde, merkezden esme yönüne
doğru uzayan bir çubuk koydum — uzunluğu şiddet, yönü yön. Dünya nesnesi, arayüz
değil: sahnedeki diğer iki gösterge de (hedef ve bırakma çizgisi) böyle çalışıyor
ve aynı dili konuşan üç gösterge, yarısı Canvas'ta duran bir arayüzden okunur.

Göstergenin ilk hâli işe yaramadı ve sebebi görselde değil mantıktaydı: rüzgâr
yalnızca kuvvet uygulanırken hesaplanıyordu, yani gösterge tam da bakılması gereken
anda — kutu bırakılmadan önce — sıfır gösteriyordu. Rüzgârı atıştan sonra görmenin
hiçbir değeri yok; atışı ona göre ayarlamak için önce görmek gerekiyor. Rüzgâr artık
bir ortam değeri: kutu havada olmasa da esiyor, sadece dokunacak bir şey bulamıyor.
Çubuğun ucuna da 45° döndürülmüş bir küp koydum, eşkenar dörtgen olarak okunuyor
ve yönü tek bakışta veriyor.

**Rüzgâr da oynanmaz derecede sertmiş.** 3.0 ve 4.0 yazmıştım; sayı ivme olduğu
için yerçekimiyle (9.81) aynı birimde, yani 3.0 demek "yerçekiminin %30'u kadar
yana it" demek. Hesap acımasız: 3.5 birimlik düşüş ~0.85 sn sürüyor, yatay sapma
½·a·t² = 1.07 birim. Kutu 1 birim geniş — rüzgâr kutuyu kendi genişliğinden fazla
kaydırıyordu. Bu telafi edilebilir bir bozulma değil, atışı baştan nişanlamak.

1.0 ve 1.4'e indirdim; sapma düzeldi ama bu sefer başka bir şey çıktı: nişan
tutuyordu, kutu iniş anındaki yatay hızıyla deviriliyordu. İki ayrı sebebi vardı.

**Rüzgâr kutu yere değdikten sonra da esiyordu.** Uygulama koşulu "oturmuş mu" idi
ve oturmuş sayılmak için 0.2 sn kesintisiz durmak gerekiyor — yani kutu kuleye
indikten sonra hâlâ yanlamasına itiliyordu. Rüzgârın işi kutu havadayken biter;
yere değdikten sonrası artık fizik. Kutu ilk temasını `OnCollisionEnter` ile
işaretliyor ve rüzgâr orada kesiliyor.

**Rüzgâr sınırsız ivme olarak modellenmişti.** Sabit ivme, yatay hızı düşüş boyunca
büyütmeye devam ediyor ve iniş hızının bir tavanı olmuyor. Gerçek rüzgâr ise
hareket eden hava: cismi kendi hızına doğru iter, geçirmez. Kuvvet artık bağıl hıza
orantılı (`(rüzgâr hızı - kutu hızı) × sertlik`). Hem fiziksel olarak doğrusu hem de
iniş hızına doğal bir tavan koyuyor.

Sayı artık ivme değil **hız**: 0.7 m/s rüzgâr, ~0.38 birim sapma ve inişte 0.65 m/s
yatay hız demek. Göstergenin ölçeği de büyütüldü, yoksa küçülen sayılarla çubuk
okunmaz olurdu.

**Namlu kuleye fazla yakındı.** Bandın alt kenarı kule tepesinin 0.9 birim
üstündeydi; tehdit, kutuyu indirirken değil neredeyse yerleşirken devreye
giriyordu — yani oyuncunun düzeltme şansı olmayan bir anda. 2.0'a çıktı: bant
artık kule ile kutunun bırakıldığı yerin arasında. Sayı da seviye verisine taşındı,
sahne bileşeninde kalmasının bir sebebi yoktu.

**Kamera ve namlu arada bir fırlıyordu — ve bu gerçek bir hataydı.** Sebep şu:
yukarı savrulan bir kutu tepe noktasında neredeyse sıfır hıza iniyor, çünkü
yükseliş bitip düşüş başlarken hız işaret değiştiriyor. O tek karede kutu "oturdu"
sayılınca kule birden havadaki kutu kadar uzuyor, kamera ve namlu yukarı fırlıyor,
kutu düşünce geri iniyorlardı.

İlginç tarafı: bu hata top atıcıdan önce de vardı, sadece kutuyu yukarı savuracak
bir şey olmadığı için neredeyse hiç tetiklenmiyordu. Top onu görünür yaptı.

Çözüm, kazanma kontrolündekiyle aynı fikir: tek kare "durdu" görmek yetmiyor.
Kutunun oturmuş sayılması için 0.2 saniye kesintisiz durması gerekiyor artık.
Tepe noktası bir kare sürüyor, gerçekten oturmuş kutu ise durmaya devam ediyor.

**Yarım kalan:** Mod ve seviye seçimi, tur sonu ekranı, `PlayerPrefs` ilerlemesi
Gün 10'a kaldı. Sonsuz modda tehdit yok — bilinçli: oradaki eğri bırakma mesafesi
üzerinden yürüyor ve tek şeyin sürekli artması, üst üste binen üç şeyden daha
okunur bir tırmanış veriyor.

## Gün 10 — Akış: menü, tur sonu, ilerleme

**Arayüz sahnede değil, kodda kuruluyor.** Arayüzde elle ayarlanacak hiçbir şey
yok — ne sanat, ne düzen, ne yazı tipi. Sahneye kurulunca ortaya diff'i okunmayan
onlarca RectTransform'luk bir YAML yığını çıkıyor ve her küçük değişiklik için
Editor açmak gerekiyor. Kodda duran arayüz okunuyor, gözden geçirilebiliyor ve
sahne kurulumunu şişirmiyor. Sanat girseydi bu karar tersine dönerdi.

Konumlar normalize koordinatlarla (0-1) veriliyor, piksel değil: seviye ızgarası
seviye sayısına göre kendini hesaplıyor, yani dokuzuncu seviyeyi eklemek düzeni
baştan kurmak anlamına gelmiyor.

**EventSystem yok.** uGUI'nin `Button` bileşeni bir EventSystem, bir input modülü
ve `GraphicRaycaster` istiyor; proje yalnızca yeni Input System kullandığı için
modülün doğru kurulması ayrı bir bakım borcu. Buradaki düğmeler dikdörtgen ve bir
dokunuşun içeride olup olmadığı tek satır:
`RectTransformUtility.RectangleContainsScreenPoint`. Sürükleme, odak ya da klavye
gezinme gerekseydi bu yanlış karar olurdu.

**Yazı tipi motorun içindeki eski çalışma zamanı fontu.** TextMeshPro daha iyi
görünürdü ama projeye ayrıca "TMP Essentials" içe aktarmayı gerektiriyor — sahneyi
tek komutla kurabilme kuralını bozan elle bir adım. Gri kutu prototipinde yazının
güzel olması gerekmiyor, okunması yetiyor.

**Menü ile tur arasında sahne yeniden yükleniyor.** İkisini aynı sahnede yan yana
çalıştırıp turu "temizlemek" mümkündü ama tur bittiğinde ortada onlarca rigidbody,
bir kule ve yarım kalmış fizik durumu oluyor. Onları tek tek temizleyen kod, sahne
yeniden yüklemekten hem uzun hem de her yeni nesne eklendiğinde güncellenmesi
gereken bir borç. Seçim `RunRequest` adlı statik bir sınıfta taşınıyor — iki sayı
için bundan fazlası gerekmiyor.

**İlerlemeyi controller yazıyor, arayüz değil.** Kaydın arayüze bağlı olması,
arayüzü değiştirdiğimde kaydı da bozma riski demek. `PlayerPrefs` kullanmamın
sebebi saklanacak şeyin üç sayı olması: kendi dosya formatımı tasarlamak, JSON
yazmak ya da kayıt sürümlemesi düşünmek burada israf olurdu.

**Geliştirici kilidi kayda yazılmıyor**, oyun her açıldığında kapalı başlıyor.
Yanlışlıkla açık kalan bir hile bayrağı, test ettiğim şeyin gerçek oyun olmadığı
anlamına gelir.

Gün 5'in "ekrana dokun, sahne yeniden yüklensin" çözümü kalktı. Tek seviyeli bir
prototip için doğruydu; seviyeler ve iki mod gelince oyuncunun bitişte verebileceği
karar birden fazla oldu.

## Kapanış — Faz 2 bitti

**Çıktı:** WebGL, 12.5 MB, 5.4 dk. `gh-pages` dalından GitHub Pages ile yayında.
iPhone'da Safari'de açılıyor, dokunmatik girdiyi alıyor, menü portrede okunuyor ve
oynanabiliyor.

Build script'in raporladığı boyut da bu turda düzeltildi. "23.6 MB" yazıyordu;
ölçüme hem Burst'ün `_DoNotShip` klasörü hem de WebGL çıktısının içindeki `.git`
(yayın o klasörden push edildiği için orada duruyor, eski build'lerin nesneleriyle
birlikte ~12 MB) giriyordu. Gün 5'teki "667 MB APK" ile aynı aile hata: ölçtüğüm
sayının "kullanıcının indireceği şey" olmadığı bir ölçüm, ölçüm değil.

**Faz 2 ne getirdi:** Kamera kuleyi takip ediyor ve kadraj cihazdan bağımsız.
Kural katmanı arayüzün arkasında, iki mod var. Sekiz seviye veri olarak duruyor.
Kutu artık zorunlu bir mesafeden bırakılıyor — oyunun risk taşıyan tek mekaniği bu
ve Faz 1'de ayarladığım his değerlerini geriye dönük anlamlı kılan da o. Rüzgâr ve
top atıcı, ikisi de yalnızca havadaki kutuya dokunacak şekilde. Menü, tur sonu
ekranı ve `PlayerPrefs` ilerlemesi.

**Bitmeyen ne kaldı — dürüst liste:**

- **Android APK bu fazda hiç alınmadı**, dolayısıyla gerçek Android cihazda hâlâ
  denenmedi. Faz 1'de alınan APK de denenmemişti. "Telefonda çalışıyor" derken
  kastettiğim şey iPhone'da tarayıcıda çalışıyor olması.
- **Performans ölçülmedi.** Telefonda akıcı hissettirdi ama elimde tek bir kare
  süresi ölçümü yok; "düzgün çalışıyor" bir gözlem, ölçüm değil.
- **Zorluk eğrisi yalnızca kabaca ayarlandı.** 7. seviye belirgin şekilde zor.
  Sayıların hepsi veri olduğu için ayarlamak kolay ama oturmuş bir eğri değil.
- **Seviye başına en iyi skor tutulmuyor**; kayıtta sadece açılan seviye ve sonsuz
  modun rekoru var.
- **Sonsuz modda tehdit yok.** Bilinçli bir tercih ama sonsuz modu uzun vadede
  ilginç kılacak şeyin bu olmadığını da biliyorum.
- Ses, sanat, partikül, çoklu dil, IAP: kapsam dışıydı, öyle kaldı.

Faz 2 oyunu **oynanabilir** yaptı, **yayınlanabilir** değil. Baştaki söz buydu.

---

# Faz 3

## Görünüş — palet, ışık, post-process

**Gri kutu kuralı neden kalktı.** O kural Faz 1 için konmuştu ve orada doğruydu:
soru "dokunmatik girdiyi fiziğe nasıl bağlarım"dı, sanat o sorunun cevabını
gizlerdi. Şimdiki bağlam farklı — bu repo bir portföy parçası olarak açılacak ve
açan kişilerin çoğu koda bakmadan önce ekran görüntüsüne bakıyor. Gri küpler,
kodun kalitesinden bağımsız olarak "yarım kalmış teknik demo" diye okunuyor.

**Yön: pastel ve düz renk.** Üç sebeple. Kutuların birbirinden ve zeminden
ayrılması gerekiyor, pastel palette her kutu farklı ton alabiliyor. Sanatçı
gerektirmiyor — düz renk, ışık ve post-process; doku yok. Ve hâlâ mobil tarayıcıda
çalışıyoruz. Yan faydası: aynı sade formlar artık kaza değil kasıtlı görünüyor.

**Bütün renkler tek varlıkta.** Dağıtılmış renkler hızlı ilerletiyor ama bütünü
görmeyi imkânsızlaştırıyor: kutunun rengi prefab'da, zemin bootstrap'ta, çizgiler
kendi bileşenlerinde duruyordu. Renk denemesi artık sahneyi yeniden kurmayı
gerektirmiyor. Kutulara renk **sırayla** dağıtılıyor, rastgele değil — rastgelede
yan yana aynı renk gelebiliyor ve palet bozuk değil, kod bozukmuş gibi görünüyor.

**Zeminin arka planla karışması bir renk sorunu değil aydınlatma sorunuymuş.**
Zemin hem yönlü ışığı hem de gökyüzü ortam ışığını alıp beyaza yaklaşıyordu.
Albedo belirgin şekilde koyulaştırıldı ve ortam ışığının gökyüzü katkısı kısıldı.

**Gradyan gökyüzü elle yazılmış bir shader.** İhtiyaç duyulan tek şey iki renk
arasında dikey geçiş; doku kullanmak hem birkaç megabaytlık varlık hem de palet
değişince yeniden üretilmesi gereken bir şey olurdu.

**Tonemapping Neutral, ACES değil.** ACES koyu ve doygun görüntüde iyi ama pastel
tonları eziyor, açık renkleri birbirine yaklaştırıyor.

**Göstergeler ışıksız malzemede.** Çizgiler ve rüzgâr oku dünyanın nesneleri
değil, oyuncuya bilgi veren işaretler. Işık alan bir gösterge sahnenin
aydınlatmasına göre renk değiştiriyor ve "şu an sarı mı yeşil mi" sorusunu
belirsizleştiriyor. Gölge de düşürmüyorlar: havada duran ince bir çizginin kulenin
üstüne gölge düşürmesi bilgi değil gürültü.

**Yazı TextMeshPro'ya geçti.** Eski `Text` bileşeni yazıyı piksel haritası olarak
rasterliyor; telefonla masaüstü arasında üç kat ölçek farkı olan bir oyunda bu
doğrudan görünüyordu. TMP mesafe alanı kullanıyor, her boyutta keskin.

Kaynak içe aktarma da koddan yapılıyor — normalde menüden elle yapılan bir adım.
Sebebi projenin kurallarından biri: sahne ve varlıklar tek komutla kurulabilmeli.
Burada bir tuzak vardı: `AssetDatabase.ImportPackage` asenkron çalışıyor ve
Unity `-quit` ile import bitmeden çıkıyor — hata da vermiyor.

**Oyun içi yazı ile geliştirici paneli ayrıldı.** Oyuncunun gördüğü üç sayı
`OnGUI` ile çiziliyordu: bir ölçü aleti, arayüz değil, ve düşük kalitesi bundandı.
Artık ayrı bir HUD var; debug paneli duruyor ama varsayılan olarak kapalı.

**MSAA, yumuşak gölge ve gölge mesafesi sahne kurulumuna girdi.** Bunlar
Inspector'dan tıklanabilir şeyler ama proje iki makine arasında taşınıyor ve
tıklamaların bir kısmı taşınmıyor. Mobil profilinde yumuşak gölge kapalıydı, yani
"PC'de düzgün, telefonda tırtıklı" gibi bulması zor bir fark oluşuyordu.

## Kadraj ve oyun alanı

**Geniş ekranda oyunun çoğu görünmüyordu.** Gün 6'da koyduğum kural "görünür
genişliği 5 birimde sabitle"ydi ve portrede doğruydu. Geniş ekranda aynı kural
tersine dönüyor: 16:10'da görünür yükseklik 3.1 birime düşüyor ve hedef yüksekliği
4.0 olan oyunda kule hedefe varmadan kadrajın dışına çıkıyor. Kadrajı artık iki
kısıt belirliyor — en az 5 birim genişlik **ve** en az 11 birim yükseklik.

**Oyun alanı yatayda sınırlandı.** Bu top atıcıyla ortaya çıktı: sürükleme
sınırsız olduğu için oyuncu kutuyu namlunun dışına götürüp tehdidi tamamen
atlatabiliyordu. Daha derin sorun şuydu: oyun alanı ekran genişliğine göre
büyüyordu, yani **geniş ekranda oyun kendiliğinden kolaylaşıyordu**. Artık kutu
sabit bir bandın dışına çıkamıyor ve namlu o bandın hemen dışında duruyor.

## His — ezilme, toz, sarsıntı

**Kutunun görseli fizik gövdesinden ayrıldı.** Ölçeği rigidbody'de oynatmak
collider'ı da oynatır, yani görsel bir süsleme fiziği değiştirir ve kule kendi
kendine sallanmaya başlar. Mesh ayrı bir çocuk nesnede duruyor artık.

**Ezilme sönümlü sinüs.** Tek yönlü bir ezilip açılma "lastik" gibi duruyor;
genliği azalan salınım, sert cismin çarpma anındaki titremesini taklit ediyor.
Salınım sayısı önce 1.6 idi ve oynayınca "ezilme değil jiggle" geri bildirimi
geldi — 1.0'a indi. Hacim korunuyor: y'de ezilirken x ve z büyüyor, yoksa kutu bir
an küçülüyor ve çarpma değil uzaklaşma gibi okunuyor.

**Düşüşte uzama sonradan eklendi.** Ezilme tek başına çarpmayı anlatıyor ama
öncesindeki düşüş "hiçbir şey olmuyor" gibi duruyordu. Çarpma anında uzama
sıfırlanıyor: geçiş ne kadar keskin olursa çarpma o kadar sert okunuyor. Sürükleme
sırasında uygulanmıyor — orada hız parmağın hızı ve kutunun elde incelmesi
kontrolü bulanıklaştırıyor.

**Toz tek bir parçacık sisteminden çıkıyor**, her çarpmada oraya taşınıp
patlatılıyor. Çarpma başına ayrı sistem üretmek bir turda onlarca
`Instantiate`/`Destroy` demekti. Doku yok: parçacıklar küçük kareler ve bu, oyunun
geometrik diline zaten uyuyor.

**Zaman yavaşlaması `OnDestroy`'da geri alınıyor.** Sahne yeniden yüklenirken
coroutine nesneyle birlikte ölüyor ama `Time.timeScale` global: geri yazılmazsa
oyun kalıcı olarak ağır çekimde açılır.

## Günün asıl dersi: üç sessiz hata

Bugün üç hata çıktı ve üçünde de derleme temizdi, sahne kuruluyordu, log
sessizdi. Yalnızca oyun yanlış çalışıyordu.

**1. `Start` sırası.** Rüzgâr göstergesi `Start`'ında `wind.Active`'e bakıp
rüzgârsız seviyede kendini kapatıyordu. Unity aynı nesnedeki bileşenlerin `Start`
sırasını garanti etmiyor; gösterge, rüzgâr daha ayarlarını okumadan çalışıp
"rüzgâr yok" sonucuna varıyordu. Çözüm sıralamayı zorlamak değil — o, projeye
görünmez bir ayar borcu bırakır. Kararı tek seferlik olmaktan çıkardım: gösterge
rüzgâr esmeye başladığı ilk karede kendini kuruyor.

**2. `CopySerialized` varlığın adını da kopyalıyor.** Paleti koddaki
varsayılanlara döndürürken kaynak olarak taze bir örnek verdim; onun adı boştu ve
palet varlığının adı silindi. Adsız kalan bir ana varlığı `AssetDatabase`
bulamıyor, dolayısıyla sahnedeki bütün palet referansları boşa düştü ve oyun
varsayılan renklerle çalıştı.

**3. Varlık üretmek eldeki referansı geçersizleştiriyor.** Toz malzemesi yeni bir
varlık olarak üretilince `AssetDatabase` tazeleniyor ve o an tutulan palet nesnesi
geçersizleşiyor; atandığında Unity hata vermeden alanı boş bırakıyor. Palet artık
bağlanmadan hemen önce yeniden okunuyor.

Üçünün ortak dersi: **"0 hata" bir doğrulama değil.** Artık `SetReference`
yazdığını okuyup doğruluyor ve yazılamazsa bağırıyor, ve sahne kuran her komuttan
sonra çıktıyı gözle kontrol ediyorum.

## Ses — dosyadan değil, koddan

Karar dosya indirmek ile sentezlemek arasındaydı. İndirilmiş CC0 ses paketi daha
iyi ses verirdi; sentezlemeyi seçmemin sebebi projede başka hiçbir hazır varlık
olmaması ve sesin, hazır varlıkların sızdığı en kolay yer olması. Oyunun ihtiyacı
olan seslerin tamamının darbe sesi olması da kararı kolaylaştırdı — vuruş, tık ve
uğultu, sentezlemesi en kolay ses ailesi. Melodik bir oyunda tersini seçerdim.

Riski bilerek aldım ve sınırladım: `ProceduralAudio` üretimi yapıyor, `SfxPlayer`
çalıyor ve klibin nereden geldiğini bilmiyor. Sesler ucuz gelirse tek yapılacak
şey üretim sınıfını dosya yüklemesiyle değiştirmek olur.

Sentezde iki şey öğrendim. Vuruş sesindeki perde düşüşü doğrusal olduğunda ses
siren gibi duyuluyor; karekökle düşürünce vuruş gibi duyuluyor. Ve ham gürültü
ile "tık" arasındaki tek fark bir filtre: alçak frekansları çıkarınca "şşş" sesi
tıka dönüşüyor, alçak geçirip bırakınca tok bir çarpma oluyor.

Tek klip hıza göre farklı ağırlıkta cisim gibi duyulabiliyor — sert çarpmada ses
seviyesi yukarı, perde aşağı. Üstüne küçük bir perde rastgeleliği koydum, yoksa
kule çökerken arka arkaya gelen onlarca çarpma makineli tüfek gibi duyuluyor.

`SfxPlayer` sahneler arası yaşıyor. İki sebep: menüden seviyeye geçiş sahneyi
yeniden yüklemek demek ve düğme sesi tam o anda çalıyor — sahneyle birlikte
silinseydi hiç duyulmazdı. İkincisi klipler açılışta üretiliyor ve her sahne
yüklemesinde yeniden üretmek gereksiz bir iş. Kendi nesnesinde duruyor, "Systems"
üzerinde değil: orada olsaydı bütün oyun sistemleri de kalıcı hale gelir ve sahne
yeniden yüklendiğinde iki kontrolcü, iki kuyruk olurdu. Sesin kalıcı olabilen tek
bileşen olması tesadüf değil — tur durumu tutmayan tek sistem o.

Sesi kulakla doğrulayamadığım için sayıyla doğruladım: `AudioProbe` klipleri süre,
tepe genlik, RMS ve bozuk örnek sayısıyla ölçüp `.wav` olarak dışa aktarıyor.
Gerekçesi Faz 3'ün "0 hata bir doğrulama değil" dersinin aynısı: sentez kodu
hatasız derlenip sıfır dolu bir tampon üretebilir ve konsolda tek satır çıkmaz,
oyun sadece sessiz çalışır. Denetleyici klip tablosunu oyunla paylaşıyor; kendi
kopyasını tutsaydı bir sesi değiştirdiğimde eskisini ölçmeye devam ederdi.

## Görünmeyen hız çizgileri — asıl sebep benim düzeltmemdi

Hız çizgileri bir kez çalıştı. Geri bildirim "sanki bana geliyormuş gibi aşağı
doğru gitmesindense" idi: görünüyorlardı, yönleri yanlıştı. Parçacık sistemi
şeklin +Z yönüne fırlatıyor ve dönmemiş hâlde o yön kameraya bakıyordu.

Yönü düzeltmek için başlangıç hızını sıfırlayıp hareketi `velocityOverLifetime`
modülüne verdim. Çizgiler tam o anda kayboldu. Gerilmiş parçacık kendi hız
vektörü boyunca uzatılarak çiziliyor; hız sıfır olunca uzatılacak yön kalmıyor.
Toz sisteminde bu sorun yok çünkü o gerilmiyor, kameraya dönük düz bir kart.
Doğru düzeltme hızı öldürmek değil, yayılım şeklini X'te 90° döndürüp +Z'yi
dünya -Y'ye çevirmekti.

Sonraki dört turda sebebi başka yerlerde aradım. Bulduklarım gerçek hatalardı,
düzeltilmeleri iyi oldu, ama hiçbiri bu hatanın sebebi değildi: kontrast yokluğu
(saydam beyaz çizgi, neredeyse beyaz gökyüzü), saydamlığın iki kez uygulanması
(URP parçacık shader'ı malzeme rengini parçacık rengiyle çarpıyor, 0.43 × 0.43 =
0.19), parçacıkların yarısının opak kutunun içinde doğması, ve malzeme için elle
kurulup hiç doğrulanmamış beş ayarlık bir saydamlık yapılandırması. Dördü de
düzeldi, ekranda hâlâ hiçbir şey yoktu.

Asıl ders: **bir şey önce çalışıp sonra bıraktıysa sebep benim değiştirdiğim
şeydedir.** Bu bilgi ilk günden elimdeydi. "Görüyordum ama yönü yanlıştı"
cümlesini bir yön raporu olarak okudum; aynı zamanda bir "çalışıyor" raporuydu.
Dört turu hiç kırılmamış katmanları inceleyerek geçirdim.

İkinci ders ölçüyle ilgili. Üç tur tahmin ettikten sonra panele düşüş hızı,
üretilen ve canlı parçacık sayısı ile "çiziliyor mu" bayrağını ekledim. Sayılar
sebebi söylemedi ama aramayı daralttı: parçacıkların üretildiği ve yaşadığı
görülünce renk ve eşikle ilgili bütün hipotezler elendi. Paneli F1'e bağlamamın
sebebi de bu — yalnızca Inspector'dan açılan bir ölçü aleti, ölçmek istediğim
anda elimde olmuyor ve tahmin etmeye başlıyorum.

## Rüzgâr göstergesine etiket

Gösterge bir şeyi doğru anlatıyordu ama neyi anlattığını söylemiyordu; ilk kez
oynayan biri hareket eden çubuğun ne olduğunu anlamıyordu. Simge yerine yazı
koydum, çünkü simge de öğrenilmesi gereken bir şey — tek kelime değil. Yazı küçük
ve solgun: bilgi çubukta, bu sadece çubuğun adı.

## Seviye başına derece: en az kutu

Sonsuz modda skor kulenin yüksekliği, ama seviyelerde hedef sabit bir yükseklik —
"daha yükseğe çık" diye bir yarış yok. Geriye kalan tek anlamlı ölçü hedefe kaç
kutuyla ulaşıldığı; bu da doğrudan yerleştirme isabetini ölçüyor.

Süre tutmayı düşündüm ve eledim: bu oyunda acele etmek her zaman kötü oynamak
demek, dolayısıyla süreyi ödüllendirmek mekaniğin tersine çalışırdı.

İki modun ölçüsünün ters yönde çalışması (sonsuzda yüksek iyi, seviyede düşük
iyi) kafa karıştırmıyor, çünkü ikisi hiçbir ekranda yan yana gelmiyor ve her biri
kendi biriminde yazılıyor.

Derece menüde seviye adının altında, küçük ve solgun duruyor. Aynı satıra aynı
puntoda koyduğumda sekiz düğmenin sekizi de iki bilgi birden bağırıyor ve ızgara
okunmaz oluyordu. Rengi paletten türetiliyor: zengin metin etiketine elle hex
yazsaydım palet değiştiğinde bu satır sessizce eski renkte kalırdı.

Tur sonu ekranında rekor kırıldığında "5 kutu · en iyi 5 kutu" gibi kendini
tekrar eden bir satır çıkıyordu; onun yerine rekor ayrıca duyuruluyor.

## Yıldız sistemi ve kutu ekonomisi

Derece "en az kutu" olarak başlamıştı; yıldıza çevirmek onu bir istatistikten
hedefe dönüştürdü. Üç yıldız hedef yüksekliğin bir fazlası kadar kutu, her fazla
kutu bir yıldız götürüyor, üçüncü fazlada tur kayıp.

Üç yıldız eşiğinin **yükseklik + 1** olmasının sebebi fiziksel: kutular sabit 1
birim yüksekliğinde, yani teorik alt sınır hedef yüksekliği kadar kutu. Ama
kutular otururken aralarında küçük bir temas payı kalıyor ve tam sınırdaki bir
kule hedefi ıskalayabiliyor. Bir kutuluk pay, üç yıldızı "kusursuz istifle"
olmaktan çıkarıp "bir kutuluk israfla" yapıyor.

Elle verilen `boxLimit` alanını kaldırdım. Yıldız sistemi gelince iki alan aynı
şeyi ölçmeye başlamıştı ve çelişebilirlerdi: sınır üç yıldız sayısının altında
kalsaydı üç yıldızın hiç alınamadığı bir seviye ortaya çıkardı ve bunu ancak
oynayan fark ederdi. Sınır artık türetiliyor. Bunun bedeli, kutu sınırının
seviye başına ayarlanabilir bir zorluk kolu olmaktan çıkması; zorluk artık
bırakma mesafesi ve tehditlerden geliyor.

Zorluk göstergesi de seviye verisinden türetiliyor, elle yazılmıyor. Ağırlıklar
elle seçildi ama en azından verinin kendisini okuyorlar: elle yazılmış bir zorluk
sayısı, seviye ayarını her değiştirdiğimde güncellenmesi gereken ve
güncellenmediğinde kimsenin fark etmediği ikinci bir yer olurdu.

Yıldız simgesi koddan çiziliyor: on köşeli çokgen, piksel başına 3x3 örnekle
kenar yumuşatma. `★` karakterini kullanmadım çünkü TMP yalnızca font atlasındaki
karakterleri çizebiliyor — fontta yoksa ekranda boş kare çıkar ve bu ancak build
alınınca görülür. Hazır sprite ise "hiçbir hazır varlık yok" kuralını bozardı.

Seviyeye dokunmak artık turu doğrudan başlatmıyor, bir kart açıyor. Yıldız
sistemi ancak eşiği önceden bilinirse hedef olur; tur sonunda öğrenilen bir eşik
sürprizdir. Bu bilgiyi ızgaradaki düğmeye sığdırmayı denedim, sekiz düğmenin
sekizi de dört satır bilgi bağırınca ızgara okunmaz oluyordu.

Kart açıkken arkadaki düğmeler görsel olarak değil, mantıksal olarak da devre
dışı: yalnızca üstünü örtseydim kartın yanındaki boşluğa dokunmak arkadaki
seviyeyi başlatırdı.

## Yıldız arayüzünün iki yazı düzeltmesi

Oynanmamış seviyenin kartında "henüz geçmedin" yazıyordu; kaldırdım. Bilgi
taşımıyordu — yıldızlar zaten boş ve oyuncu bunu görüyor. Boş olduğunu ayrıca
yazmak ekranı bilgiyle değil metinle doldurmak.

"En iyin: 4 kutu" yerine "en düşük: 4 kutu". Seviyede iyi olan az kutu
kullanmak, ve bir sayının hangi yönde iyi olduğunu etiketin kendisi söylemeli.
Sonsuz modda tersi geçerli olduğu için orada "en iyi" doğru kalıyor — aynı
kelimeyi her yerde kullanmak tutarlılık değil, iki farklı şeye aynı adı vermek
olurdu.

## Ekrandan bağımsız düzen, sabit punto ile olmuyor

Seviye kartı dikey telefonda sığıyordu, yatay ekranda taşıyordu. Sebep kanvasın
hem genişliğe hem yüksekliğe eşlenmesi: aynı kart yatayda basık kalıyor ve dört
satır yazı aynı puntoda artık sığmıyor.

Kartı büyütmek tek başına yeterli olmazdı, sadece sınırı öteler. Yazılara punto
aralığı verdim (sarma açık, otomatik küçülme). Bunu her etikete koymadım çünkü
otomatik boyutlandırma her karede ölçüm yapıyor; yalnızca içeriği değişken olan
yerlerde var.

## Faz 3 · 2. günün sonu

Bugün biten işler: kodla sentezlenen ses efektleri ve ses açma/kapama, rüzgâr
göstergesine etiket, hız çizgilerinin dört tur süren hata avı, yıldız sistemi ve
seviye kartı, kutu ekonomisinin tek kaynağa indirilmesi, seviye başına derece.

WebGL build'i alındı: yayına giden yük 13 MB (Burst hata ayıklama verisi ve git
klasörü hariç). Bir önceki sürüm ~12 MB'tı; artış TextMeshPro ve yeni arayüz
kodundan. Ses build'i büyütmedi, çünkü klipler dosya değil kod.

Kalan: sonsuz moda tehdit, 30 saniyelik kayıt, README v3 ve `v3` etiketi.

## Faz 3 · 3. gün

### Aynı taşma, ikinci ekran

Tur sonu ekranında da yazılar sığmıyordu. Sebep seviye kartındakiyle aynı: panel
ekran yüksekliğinin %40'ı kadar ve kanvas yatay ekranda 1080 birime eşleniyor,
yani başlık bandı 110 piksele kadar iniyor — 84 puntoluk "KAZANDIN" oraya
sığmıyor.

Punto aralığı yardımcısı `MenuUI`'nin içinde özel bir metottu; `UIKit`'e taşıdım.
İki ekranın da aynı kanvas ölçeğini kullandığı düşünülürse sorun ekranların
değil, bir kutuya sabit punto yazmanın sorunuymuş. Aynı hatayı iki yerde
düzeltmek, üçüncü ekranda yeniden yapacağım anlamına gelirdi.

### Hız çizgisi: geometriden değil dokudan

Çizgiler görünüyordu ama çizgi gibi durmuyordu — dikdörtgen prizma gibi. Sebep
basit: dokusuz bir parçacık düz bir dikdörtgen olarak çiziliyor, gerilince de
kenarları keskin, iki ucu düz kesik bir çubuk oluyor.

Çözüm için üç yol vardı. Parçacığı daha da inceltmek: oranı düzeltir ama keskin
kenar ve düz uç kalır, çubuk sadece incelir. Trail modülü: gerçek bir iz çizer,
ama her parçacık için ayrı bir mesh üretiyor ve burada gereken tek şey görüntü.
Doku: alfası ortada parlak, iki uca ve iki kenara doğru sönen 128×16'lık bir
görüntü — çizgi hissi buradan geliyor.

Dokuyu kodla üretip PNG olarak yazıyorum. Sanatçısı olmayan bir projede elle
çizilmiş 8 KB'lık bir dosyanın depoda ne işi olduğunu birkaç ay sonra kimse
hatırlamaz; formül kodda durunca "çizgi neden böyle görünüyor" sorusunun cevabı
da kodda oluyor. Yine de dosyaya yazılıyor, çünkü malzeme bir varlık ve
varlıklar birbirine GUID ile bağlanıyor: çalışma zamanında üretilen bir doku
sahne kaydedilince boşa düşer.

Doku gelince kalınlık gerçekten görünür oldu ve eski değerler çizgiyi değil
şeridi çizdiriyordu; kalınlık 0.08–0.14'ten 0.05–0.09'a indi, uzunluk çarpanı
4.5'ten 7'ye çıktı — hem oranı korumak hem de dokunun sönen uçlarını telafi
etmek için.

### Sonsuz moda tehdit: yığmak değil, sıraya dizmek

Sonsuz modda uzun süre hiç tehdit yoktu ve gerekçesi şuydu: tek bir şeyin
(bırakma mesafesinin) sürekli artması, üst üste binen üç şeyden daha okunur bir
tırmanış verir. Gerekçe hâlâ doğru ama eksikmiş — mesafe 15 kutuda tavana
vuruyor ve ondan sonrası sabit zorlukta bir tur oluyor. "Tırmanış okunur olsun"
derken tırmanışın kendisini 15 kutuyla sınırlamışım.

Düzeltme tehditleri üst üste yığmak değil, sıraya dizmek. Her biri bir öncekinin
doyduğu yerde giriyor ve tek başına öğreniliyor:

| Kutu | Giren şey |
|---|---|
| 0–15 | bırakma mesafesi 2.5 → 3.6, genişlik oynaması 0 → 0.25 |
| 6–14 | rüzgâr 0 → 1.0 m/s, sabit yönlü |
| 14+ | rüzgâr yön değiştirmeye başlıyor (3.2 sn periyot) |
| 18+ | namlu devreye giriyor |

Sıra rastgele değil: sabit yönlü rüzgâr bir kez öğrenilip telafi edilen bir şey,
salınım telafiyi zamanlamaya bağlıyor, namlu ise ritim problemi. Üçü aynı anda
gelseydi 18. kutuda oyuncu neyi yanlış yaptığını göremezdi. 18'den sonra hiçbir
şey artmıyor: sonsuza kadar tırmanan bir eğri, oyuncunun becerisinin değil
eğrinin kazandığı bir yer yaratır.

### Tehdit artık tur başına değil, kutu başına

Bunun için kuralın arayüzü değişti: `Hazards` özelliği `HazardsFor(snapshot)`
metodu oldu. Eski hâlin gerekçesi belgede yazılıydı — "rüzgâr ve top sahnede
duran şeyler, her kutuda yeniden pazarlık edilmiyorlar" — ve sonsuz modda
tehdit tur içinde büyümeye başlayınca o cümle doğru olmaktan çıktı. Seviye
modunda hiçbir şey değişmiyor: orada cevap anlık görüntüden bağımsız.

Değer kutu başına bir kez hesaplanıyor, kare başına değil. Böylece zorluk
basamak basamak artıyor ve oyuncu bir kutuyu bir rüzgârla indirip aynı kutu
inerken başka bir rüzgâr bulmuyor.

Sahnedeki `Wind` ve `Cannon` ayarı `Start`'ta bir kez okuyordu. Rüzgâr artık her
karede soruyor (birkaç float kopyalamak, "değişti mi" diye haber mekanizması
kurmaktan ucuz). Namlu ise değişimi yakalayıp yeniden kuruluyor, çünkü turun
ortasında beliriyor: bandı kulenin o anki tepesinden başlatmak gerekiyor, yoksa
yumuşatma sıfırdan tırmanırken namlu kulenin içinden geçiyor; ateş sayacı da
sıfırlanıyor, çünkü belirdiği anda ateş eden bir tehdit öğrenilecek bir ritim
değil kaza olurdu.

Debug paneline sonsuz moda özel bir satır ekledim: kutu sayısı, rüzgâr hızı,
salınım periyodu, namlunun durumu. Eğriyi ekranda gösteren başka bir şey yok —
oyuncu için doğrusu bu, ama ayarı gözle yapmak imkânsız hâle geliyordu.

### 8 kutuluk tavan bir zorluk değil, tasarım sınırıydı

Sonsuz modu ilk denediğimde turlar 8 kutu civarında bitiyordu ve tehdit
merdiveni 8/16/22'ye kurulmuştu — yani merdivenin tamamı hiç görülmeyen bir
yerdeydi. İlk tepkim eşikleri aşağı çekmek oldu. Proje sahibinin sorusu doğru
olanı gösterdi: *"diyelim ileride bu oyun çıkacak, sadece 8 blok üst üste
koyunca ileri seviyeleri tasarlamak zor olur."* Merdiveni indirmek semptomu
saklar; asıl mesele kulenin ne kadar yükselebildiği ve bu, üstüne içerik
tasarlanabilecek alanın kendisi.

Tavanı belirleyen şey ölçülebilirdi ve gerekçesi zaten `BoxDifficulty`'de
yazılıydı: kutu kule tepesinden 3 birim yukarıdan bırakılıyor, serbest düşüşle
~7.7 m/s ile çarpıyor. Her yeni kutu kuleye bir çekiç darbesi; kuleyi deviren
şey tek bir kötü atış değil, her inişte biraz büyüyen sallanma.

Üç kolu birden çevirmek yerine üçünü de aynı şeye — çarpma enerjisine —
yönlendirdim:

- **Bırakılan kutuya hava sürtünmesi** (`DragSettings.fallDrag = 1.2`). Düşüş
  hızına tavan koyuyor: 3 birimden düşen kutu ~7.7 yerine ~5.5 m/s ile çarpıyor,
  yani enerji neredeyse yarıya iniyor. Alternatifleri eledim: bırakma mesafesini
  kısaltmak oyunun tek risk kolunu söker, yerçekimini azaltmak ise sürüklemeyi
  ve çöküşü de ağır çekime alır. Sürtünme yalnızca serbest düşüşe dokunuyor.
- **Açısal sürtünme 0.35 → 0.6.** Sallanma dönme demek; frenlenecek yer burası.
- **Çözücü tekrarları 12 → 20, hız tekrarları 2 → 4.** Bu bir kolaylaştırma
  değil, simülasyon kalitesi: temas noktaları daha az kayıyor.

Sabit adımı (0.02) bilerek değiştirmedim. Sürükleme çekirdeği hızı
`delta / Time.fixedDeltaTime` ile hesaplıyor, yani adımı kısaltmak takibi
sertleştirir ve his ayarını bozar — hiçbiri "kule daha yüksek olsun" isteğiyle
ilgili olmayan bir yan etki.

Merdiven de yukarı geri alındı (rüzgâr 6→14, salınım 14, namlu 18). Bırakma
mesafesinin tavanı şimdilik 3.6'da bırakıldı; aynı turda hem çarpmayı yumuşatıp
hem mesafeyi büyütseydim, çıkan tur uzunluğunun hangisinden geldiğini
ölçemezdim.

Bunun bir bedeli var ve peşinen kabul edildi: 8 seviyenin zorluğu eski fiziğe
göre ayarlanmıştı, hepsi bir miktar kolaylaştı. Zaten istenen de buydu — tavan
yükselmeden üstüne seviye tasarlanamıyor.

### Kontrol noktası: yükseklik sınırını kaldıran şey

Fizik sağlamlaştıktan sonra sonsuz modda tur 8 kutudan 13'e çıktı. Proje
sahibinin sorusu bir sonraki sınırı gösterdi: *"ileride seviye 100'e gelirsek
hâlâ '15 tane stackle' demeyelim, onu da checkpointli yapalım."*

Fizik ayarı tavanı yükseltiyor ama kaldırmıyor: sallanma en alttan başlıyor ve
her kutu onu biraz büyütüyor, yani belli bir yükseklikten sonra kule oyuncunun
becerisiyle değil birikmiş salınımla devriliyor. Kontrol noktası tam bu zinciri
kesiyor — altı sabitlenirse yüksekliğin üst sınırı kalmıyor.

**Ne yapıyor:** belirli kutu sayılarında, o ana kadar oturmuş bütün kutular
kinematik hâle geliyor. İtilemiyorlar ama hâlâ çarpışıyorlar; kule için yeni bir
zemin oluyorlar. Yan kazanç: donan cisimler çözücünün dışına çıkıyor, yani 20
tekrarlı çözücüde 30 kutunun 20'si hesaptan düşüyor.

**Ne zaman:** kutu bırakıldığında değil, kule tam durduğunda. Sallanan bir kuleyi
dondurmak eğikliği kalıcı hâle getirir ve oyuncunun düzeltme şansı olmadan
verilmiş bir cezaya dönüşür. Eğikliği düzeltmiyorum da: o eğiklik oyuncunun
bırakma biçiminden geldi, düzeltmek kuleyi oyuncunun yaptığı şey olmaktan
çıkarır.

**Hangi sayılarda:** sonsuz modda 10, 25, 45, 70, 100... — aralık her seferinde
5 kutu büyüyor. Sabit aralık (her 10 kutuda bir) daha basit olurdu ama yanlış
şeyi ölçer: ilk 10 kutu ile 90'dan 100'e giden 10 kutu aynı iş değil, çünkü
ikincisinde zorluk çoktan tavana vurmuş. Aralığın büyümesi kontrol noktasının
ödül olarak değerini koruyor. Dizi yerine döngüyle hesaplanıyor: sonsuz modun
sonu yok ve tabloyu bir yere kadar yazmak, o yerden sonrasını sessizce farklı
davranan bir oyun demek.

Seviye modunda `LevelDefinition.checkpointEvery` alanı var, varsayılanı 0 yani
kapalı. Mevcut sekiz seviyenin hiçbiri buna ihtiyaç duymuyor (kuleleri 3-6
birim); alanı yine de şimdi ekledim, çünkü asıl sebebi ileride yüksek kule
isteyen bir seviye tasarlayabilmek.

**Geri bildirim üç katmanlı ve biri kalıcı.** Kısa bir ezilme ve yeni bir ses
"az önce bir şey oldu" diyor; rengin bir basamak koyulaşması ise "bu kutular
artık sabit" demeye devam ediyor. Yalnızca anlık geri bildirim verseydim oyuncu
birkaç kutu sonra hangi kısmın donmuş olduğunu bilemezdi. Renk ayrı bir gri
"donmuş" tonu değil, paletteki rengin 0.82 ile çarpımı: tek gri renk kuleyi
renklerinden eder ve donmuş kısım oyunun dışından gelmiş gibi dururdu. Ses de
kazanma sesinden bilerek farklı — iki nota, kısa. Aynı sesi kısarak kullanmak
kulağa tur bitmiş gibi gelmesini engellemiyordu.

**Kaybetme kuralı bilerek değişmedi.** Kontrol noktasının üstündeki kısım
çökerse tur yine bitiyor. "Çöktükten sonra kontrol noktasından devam" ayrı bir
karar: sonsuz modu affedici bir moda çevirir ve düşen parçaları temizlemeyi de
gerektirir.

## Faz 3 · 3. günün ikinci yarısı

### Menü ikiye ayrıldı

Tek ekranda oyunun adı, sekiz seviyelik ızgara, sonsuz mod ve ses düğmesi
vardı. Sekiz seviye ekrana ancak sığıyordu; dokuzuncu sığmayacaktı. Izgarayı
küçültmek çözüm değildi — düğmeler zaten yıldız sırası taşıyor ve küçülünce
yıldızlar okunmaz oluyor.

Ayrıca ilk açılışta oyuncuya sorulan soru "hangi seviye" değil "hangi mod".
Izgarayı ilk ekrana koymak o soruyu sekiz seçenekle birlikte soruyordu. Artık
ana ekranda iki düğme var (Seviyeler, Sonsuz Mod), seviye listesi ayrı bir
ekranda ve orada sonsuz mod yok: aynı seçeneğin iki yerde durması, listenin
sonuna inen oyuncu için "bunun burada ne işi var" sorusu doğururdu.

Oyunun adı bir kez ortada duruyor, sonra yukarı kayıp başlık oluyor. Aynı yazı
hem tanıtım hem başlık işini görüyor; ayrı bir açılış ekranı kurmak bir saniye
sonra çöpe atılacak ikinci bir kanvas demekti. Animasyon oturumda bir kez
oynuyor ve ekrana dokununca atlanıyor — her seviye dönüşünde tekrar izlemek,
üçüncü seferde bekleme süresine dönüşür.

Punto da animasyonun parçası: ad ortadayken kutusu büyük, yukarıdayken küçük ve
otomatik boyutlandırma puntoyu kutuya göre kendi buluyor. Yani animasyon tek bir
sayıyla (0 → 1) sürüyor, iki ayrı şey (konum ve punto) senkronda tutulmuyor.

### Kaydırmalı liste, EventSystem olmadan

uGUI'nin `ScrollRect`'i EventSystem, input modülü ve `GraphicRaycaster` istiyor.
Bu projede o altyapı hiç kurulu değil — dokunuş okuması baştan beri
`RectangleContainsScreenPoint` ile yapılıyor. Yalnızca bu liste için EventSystem
kurmak, bütün dokunuş okumasını ikinci bir sisteme taşımak olurdu.

Kaydırmayı elle yazdım: sürükleme, sönen atalet, sınırlarda durma, fare
tekerleği. Kırpma için `RectMask2D` kullanılıyor — o bir çizim özelliği,
EventSystem istemiyor.

İki ayrıntı oynanışta belirleyici:

- **Dokunuş bırakışta okunuyor, basışta değil.** Kaydırılabilen bir listede
  basış anında karar vermek, listeyi kaydırmak isteyen her parmağın altındaki
  seviyeyi açardı. Oyunun geri kalanında karar basışta veriliyor ve orada
  doğrusu o: sabit bir düğmenin altında kaydırma diye bir ihtimal yok.
- **Dokunuş mu kaydırma mı, toplam yol üzerinden.** Baştan sona mesafeye
  bakılsaydı parmağını aşağı indirip geri getiren biri "hiç kaymamış" sayılırdı.

Satır yüksekliği görünen satır sayısına bağlı (3.4), seviye sayısına değil:
dokuzuncu seviye eklenince düğmeler küçülmüyor, liste uzuyor. Buçuklu sayı da
bilinçli — dördüncü satırın bir kısmı görünüyor ve listenin devam ettiğini
söyleyen şey bu. Kaydırma çubuğu koymak yerine içeriğin kendisini kırptım;
çubuk, dokunmatik bir ekranda kimsenin tutmadığı bir şey.

### Üç yeni seviye: yeni kol yükseklik

Seviye 9-11 eklendi. Bunlar eğriyi mesafeyle değil **yükseklikle** sürdürüyor:
hedef 6'dan 7 ve 8'e çıkıyor, bırakma mesafesi 3.5-4.0'da kalıyor. Sebebi kendi
notum — mesafe 8. seviyede zaten oynanabilirlik tavanına dayanmıştı; 4 birimden
düşen kutu ~9 m/s ile çarpıyor ve kuleyi süpürüyor. İkinci kolu açan şey düşüş
sürtünmesi ve fizik ayarlarıydı.

9. seviyede tehdit yok. Sekizinci seviyeden sonra bu bir geri adım gibi
görünebilir ama orada sorulan soru "yüksek kule kurabiliyor musun" ve yanına
ikinci bir soru koymak cevabı bulanıklaştırırdı — sonsuz moddaki merdivenle aynı
mantık.

Kontrol noktası bu üç seviyeye konmadı. Sistem duruyor ve `checkpointEvery`
alanı kapalı; yüksek kule isteyen bir seviye ileride tasarlanırsa hazır.

Zorluk göstergesinin yükseklik aralığı da 3-6'dan 3-8'e çıktı. Aksi hâlde 6'nın
üstündeki her hedef aynı görünüyordu, yani gösterge tam da yeni açılan kolu
ölçemiyordu. Yeni dağılım: 1, 2, 2, 2, 3, 3, 4, 4, 3, 4, 5.

Sonsuz modun kilidi 8. seviyede kaldı, sona taşınmadı: sonsuz mod bir bitirme
ödülü değil, ikinci bir oynama biçimi. On birinci seviyeye bağlasaydım
oyuncuların çoğu onu hiç görmezdi.

### Görünmeyen animasyon: hata çalışmamak değil, çizilmeden bitmekti

Tanıtım Unity'de hiç oynamıyordu. İlk şüphem statik alanın Play oturumları
arasında yaşamasıydı (Editor'de "Enter Play Mode Options" açıkken olabiliyor);
onu `RuntimeInitializeOnLoadMethod` ile sıfırladım ama asıl sebep başkaydı.

Animasyonu duvar saatiyle sürüyordum. Sahne açılırken ilk kareler çok uzun
sürüyor — shader derlemesi, TMP atlasının üretilmesi, ses kliplerinin
sentezlenmesi hep orada. `Time.unscaledDeltaTime` o karede saniyeler
gösterebiliyor, yani 1.15 saniyelik animasyon **tek bir karede** başlayıp
bitiyordu. Ekranda hiçbir şey görünmemesinin sebebi animasyonun çalışmaması
değil, çizilmeden bitmesiydi.

Çözüm, tanıtım saatinin bir karede ilerleyebileceği adımı sınırlamak (0.05 sn).
Süre artık kareye bağlı: en kötü ihtimalle tanıtım biraz uzun sürer ama mutlaka
çizilir. Açılış animasyonu için doğru takas bu — oynanışta tersi doğru olurdu,
orada fizik duvar saatini takip etmeli.

### Oynanıştan gelen üç düzeltme

**1. Kutu panelin arkasında beliriyordu.** Beliriş noktası kadrajın üstünden
sabit 1 birim aşağıda tutuluyordu. Kule yükseldikçe kamera geri çekiliyor,
kadraj büyüyor ve o bir birim, panelin kapladığı yerin çok altında kalıyor —
yani kutu yazının arkasında beliriyordu. Pay artık sabit değil, kadrajın oranı
(`HudUI.TopBandFraction`). Sayı panelin kendi dosyasında duruyor: kamerada ayrı
bir kopya olsaydı paneli aşağı kaydırdığım gün ikisi sessizce ayrışırdı.

Kameranın ayırdığı tepe boşluğu da bu payı içeriyor. İçermeseydi kadraj kutunun
tam üstünde biter, kırpma kuralı da kutuyu panelin altına indirmek için düşüş
mesafesini sessizce kısaltırdı — oyunun tek risk kolu, kule yükseldikçe
kendiliğinden gevşerdi.

**2. Sabit beliriş noktası oyunu çözüyordu.** Yatay rastgelelik Faz 2'de
bilerek kaldırılmıştı: *"zorluk atışın kendisinden gelmeli, kutunun nereye
düştüğünü şansın belirlemesinden değil."* Gerekçe doğru ama açığı varmış —
kutu her seferinde aynı yerde belirince parmağı hiç kıpırdatmadan aynı noktaya
arka arkaya dokunmak kusursuz bir kule veriyor. Oyun oynanmadan çözülüyor.

Geri gelen şey eskisi değil: rastgele olan tek şey kutunun **belirdiği** yer.
Nereye ineceğine hâlâ tamamen oyuncu karar veriyor, çünkü kutu zaten
sürüklenerek indiriliyor. Yani şans sonucu değil yalnızca başlangıç noktasını
belirliyor; aynı seviye iki kez oynandığında problem aynı, tek fark her kutu
için gerçekten bir hamle yapmak zorunda olmak.

Arka arkaya iki kutu arasında en az bir mesafe şartı var. Olmasaydı rastgelelik
işini yapmazdı: iki kutu tesadüfen aynı yere düştüğünde sabit noktanın açığı o
iki kutu boyunca geri gelirdi.

**3. Namlunun bandı artık oyunun kendi işaretlerinden geliyor.** Bant sabit
yükseklikteydi ve kule tepesinin belli bir pay üstünden başlıyordu.
Öngörülebilirdi ama koridorla ilgisi yoktu: bırakma mesafesi büyük seviyelerde
namlu bırakma çizgisinin epey üstüne çıkıyor, küçüklerinde kuleye fazla
yaklaşıyordu.

Artık tabanı kulenin tepesi, tavanı bırakma çizgisi. Namlu tam da kutuyu
indirdiğin koridorda geziniyor; çizginin üstü güvenli alan, çünkü orası
oyuncunun kutuyu tutup nişan aldığı yer ve orada vurulmak öğrenilebilir bir
tehdit değil, kaza.

Bunun bir bedeli vardı: bant artık her kutuda değişiyor ve `PingPong` ham
mesafe üzerinden çalışırken aralık değişince çıktısını sıçratıyor — bandın sabit
tutulmasının asıl sebebi de buydu. Çözüm bandı sabitlemek değil, evreyi
normalize etmek: gezinme 0-1 arası bir sayı üzerinden yürüyor ve bant büyüyüp
küçülürken namlu kendi yolunun aynı noktasında kalıyor. Hız da banda bölünüyor,
yani dar koridorda daha sık gidip geliyor ama gezinme hızı her yerde aynı
hissediliyor.

`cannonPatrolSpan` alanı tamamen kalktı: bandın yüksekliği artık veri değil,
türetilen bir şey. Veride kalsaydı iki ayrı yerden gelen iki tavan olurdu.

### Sayaç yanlış şeyi sayıyordu

Sonsuz modda oynarken çıktı ve ikisi de aynı hatanın iki yüzüymüş: hem tehdit
merdiveni hem kontrol noktası **atılan kutu sayısına** bakıyordu, kulenin boyuna
değil. Kutuları kulenin yanına atınca sayaç ilerliyor, rüzgâr geliyor, top
çıkıyor — ama kule 7 birimde duruyor. Yani hem zorluk hem ödül, ikisini de hak
eden şeyden bağımsız veriliyordu.

Bütün eşikler kule boyuna çevrildi: bırakma mesafesi eğrisi, rüzgârın başladığı
ve dolduğu nokta, salınım, namlu ve kontrol noktaları. Sayılar aynı kaldı çünkü
kutu boyu 1 birim — "15 kutu" ile "15 birim" düzgün yığılmış bir kulede zaten
aynı şey. Değişen tek şey artık yalnızca kuleye konan kutunun sayılması.

Kontrol noktasının sorusu da değişmek zorunda kaldı. Kutu sayısı ayrık, yani
"bu sayı bir kontrol noktası mı" diye sorulabiliyordu; yükseklik sürekli ve kule
9.98'den 10.03'e geçiyor. Arayüz artık "verilen yükseklikten sonraki ilk kontrol
noktası nerede" diye soruyor, controller da o eşiği geçince donduruyor.

### Iskalanan kutu artık kaybettiriyor

Aynı turda ortaya çıkan ikinci sorun: kutuyu kulenin yanına atmak hiçbir şey
yapmıyordu. Tur devam ediyor, kule duruyor, ceza yok. Yani en güvenli oynayış
"kutuyu kenara at" oluyordu — bir oyunun en güvenli hamlesi hiçbir şey
yapmamaksa orada oyun yoktur.

Ölçü zemine oturmuş kutu sayısı: biri kulenin temeli, fazlası ıska. Bunu
çarpışmayla ölçmek ("kutu kuleye değdi mi") yanlış olurdu, çünkü kuleden sekip
yere düşen kutu da değmiş sayılırdı; bakılması gereken şey temas değil, kutunun
nerede durduğu.

Kural iki modda da geçerli. Seviyede ıskanın cezası eskiden "bir kutu daha az"
idi, yani sınırı geniş seviyelerde cezasızlıktı. Aynı kuralın her yerde geçerli
olması ayrıca önemli: oyuncu ıskanın ne demek olduğunu bir kez öğreniyor.

Bunun bilerek kabul edilen bir yan etkisi var: geniş taban kurmak — kutuları yan
yana dizip üstüne yığmak — artık mümkün değil. Kuleyi sağlamlaştıran bir
stratejiydi ama oyun "yığ" diyor, "diz" demiyor.

### Seviye 12-13: birleşimler ve ikinci namlu

Buraya kadar her seviye tek bir soru soruyordu. Son iki seviye soruların
birlikte sorulabildiği yer, ve sıra yine bilinçli: önce **iki farklı tehdit**
(rüzgâr + namlu), sonra **aynı tehdidin iki katı** (çift namlu). İkisini de aynı
seviyeye koysaydım kaybın sebebi okunmazdı.

`HazardSettings.cannon` alanı `bool`'dan `cannonCount`'a çevrildi. İki namlu
sahnede baştan duruyor; tehdidin istediği kadarı açılıyor, kapalı olan gövdesini
gizleyip `Update`'in ilk satırında dönüyor. Alternatifi namluyu çalışma zamanında
üretmekti — prefab, referans bağlama ve "üretilen nesne sahneye ait değil"
sorunları için, kazancı kapalıyken hiçbir şey yapmayan bir nesne olan bir şey.

İkinci namlu **yarım tur kaymış** başlıyor: hem gezinmesi hem atışı. Aynı fazda
başlasalardı iki namlu tek bir tehdit gibi davranırdı — aynı anda, aynı
yükseklikten iki mermi, oyuncu için tek bir mermiyle aynı problem. Kaydırınca
iki namlu koridoru bölüşüyor ve ortaya bir ritim çıkıyor.

Çift namluda atış aralığı da uzadı (2.2 → 3.0). Oyuncuya gelen mermi sıklığı
zaten iki katı; aralık aynı kalsaydı koridor sürekli dolu olur ve ortaya
beceriyle değil şansla geçilen bir seviye çıkardı.

Rüzgâr ve namluyu birleştirmek için ayrı bir "hem rüzgâr hem namlu" üreticisi
yazmadım; var olan ayara rüzgâr ekleyen bir sarmalayıcı var. Ayrı üretici
yazmak, üçüncü tehditte üç üreticiye çıkardı.

### Ölçülmemiş performans ve doğrulanmamış platform

İki açığı da yazıya geçirdim.

**APK hiçbir zaman telefonda çalışmadı** — elimde Android cihaz yok. Build hattı
çalışıyor ve APK derleniyor, ama cihazda çalıştığı doğrulanmadı. Dokunmatik
girdi ve performans telefonun tarayıcısındaki WebGL sürümüyle test edildi; girdi
yolu ikisinde de aynı (`Pointer` katmanı) ama IL2CPP/ARM64 çıktısının cihazdaki
davranışı bilinmiyor. README'de bunu yazmadan "Android hedefli" demek,
doğrulanmamış bir şeyi doğrulanmış göstermek olurdu.

**Performans hiç ölçülmemişti.** Debug paneline kare hızı, son bir saniyenin en
kötü karesi ve uyanık cisim sayısı eklendi.

Üçünün de ayrı bir sebebi var. Ortalama fps tek başına yalan söylüyor: saniyede
bir kez 120 ms süren bir kare ortalamayı 58'in altına indirmiyor ama oyuncunun
hissettiği tek şey o. Uyanık cisim sayısı da yanında, çünkü bu oyunda kare
süresini belirleyen şey çizim değil fizik — ve dondurulmuş kutular çözücünün
dışında olduğu için toplam kutu sayısı yanlış ölçü.

Ölçüm panel kapalıyken de işliyor: açıldığı anda sıfırdan başlasaydı, bir
takılmayı fark edip paneli açtığımda takılma çoktan geçmiş olurdu. Ölçek dışı
zaman kullanılıyor, çünkü çöküşteki zaman yavaşlatması devredeyken
`Time.deltaTime` gerçek kare süresini değil oyunun yavaşlatılmış süresini verir
— yani tam da en çok kare düşen anda ölçüm yalan söylerdi.

Paneli telefonda açmanın yolu da eklendi: sol üst köşeye çift dokunuş. Klavye
yok, Inspector yok, yani panel telefonda pratikte hiç açılamıyordu — tam da en
çok ölçüm gereken yerde. Çift dokunuş şart, çünkü tek dokunuş oyunun içinde bir
hamle.
