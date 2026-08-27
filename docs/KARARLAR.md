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
