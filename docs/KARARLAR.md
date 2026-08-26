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

**Denenecek:** üç yaklaşım (doğrudan position set / MovePosition / hız tabanlı takip).

**Yapılan:** _(doldurulacak)_

**Karar ve gerekçe:** _(doldurulacak)_

**His notu:** _(sürüklerken ne yanlış hissettirdi, hangi değeri çevirince düzeldi)_

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
