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

**Yapılan:** _(doldurulacak)_

**Karar ve gerekçe:** _(doldurulacak)_

---

## Gün 4 — His ayarı, SO'ya taşıma, debug overlay

**Yapılan:** _(doldurulacak)_

**Bulunan değerler:** _(takip gücü / maks hız / yerleşme eşiği — ve neden bu değerler)_

---

## Gün 5 — Build ve kapanış

**Yapılan:** _(doldurulacak)_

**Bitmeyen ne kaldı:** _(dürüst liste; "bitti" demek yerine ne eksik kaldıysa yaz)_
