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

### Hedef nokta: kameraya bakan bir düzlem üzerinde raycast

Parmağın ekran koordinatını dünya koordinatına çevirmek gerekiyor.
`ScreenToWorldPoint` + sabit uzaklık **kullanılmadı**: kamera açısı ya da FOV
değiştiği anda sürükleme düzlemi kayıyor ve ayarlanan his bozuluyor.
Onun yerine kameraya bakan sabit bir `Plane` tanımlanıp `Plane.Raycast` ile
kesişim alınıyor — kamera nereye giderse gitsin sürükleme aynı düzlemde kalıyor.

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

### Ayar değerleri koda gömülmüyor

Takip gücü, maksimum hız, yerleşme eşiği, hedef yükseklik — hepsi `DragSettings`
ScriptableObject'inde. Sebep: his ayarı **oynaya oynaya** bulunuyor. Değer koda
gömülüyse her deneme derleme bekliyor; SO'da ise Play Mode'da canlı çevirebiliyorum.
Ayrıca "ağır kutu / hafif kutu" gibi varyantlar ileride ayrı asset olarak açılabilir.

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

## Durum

5 günlük planın günlük kararları ve notları: [docs/KARARLAR.md](docs/KARARLAR.md)

- [x] Gün 1 — Proje şablonu, .gitignore, klasör yapısı, ilk commit
- [ ] Gün 2 — Sürükleme çekirdeği
- [ ] Gün 3 — Kazanma/kaybetme, yerleşme tespiti, kutu kuyruğu
- [ ] Gün 4 — His ayarı, değerlerin SO'ya taşınması, debug overlay
- [ ] Gün 5 — Telefonda build, 30 sn kayıt, README kapanışı

## Kapsam dışı

Menü, ses, skor kaydı, seviye sistemi, parçacık efekti, birden fazla kutu tipi,
karakter animasyonu. Bilinçli olarak yok.
