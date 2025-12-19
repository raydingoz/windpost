# Codex çalışma standartları (Unity / C#)

Bu repo’da amaç: **küçük, güvenli, geri alınabilir değişiklikler** ile PCVR (OpenXR) + SteamVR dağıtımı ve Desktop/Flat oynanışı aynı içerik akışı üzerinden desteklemek.

## Değişiklik prensipleri

- No big refactors: Büyük yeniden yazımlar yok; ihtiyaç oldukça küçük düzeltmeler.
- Small incremental diffs: İnce dilimler halinde ilerle; her PR/patch tek sorunu çözsün.
- Only touch relevant files: Görevle doğrudan ilgili olmayan dosyalara dokunma.
- Var olan yapı/akış korunur; yeni yapı eklemek mevcutları taşımak anlamına gelmez.

## Unity / C# kod stili

- Okunabilirlik > kısalık: Açık isimlendirme, tek sorumluluklu sınıflar, net akış.
- Null-safety: `null` kaynaklarını erken ele al (guard clauses), gereksiz `!` kullanma.
- Açık isimlendirme: `is/has/can` boolean ön ekleri, `Try...` kalıbı, net enum adları.
- Unity yaşam döngüsü: `Awake/OnEnable/Start/Update` tarafında yan etkileri net ayır.
- Serileştirme: Inspector alanlarını `private` + `[SerializeField]` ile tanımla; public alanları sınırlı tut.

## VR + Desktop gereksinimleri (tek içerik akışı)

- İçerik akışı **TEK**: Story/Narrative, Save, Flags/Progression tek sistem.
- Input **tek action map** mantığı: Aynı “oyun aksiyonları” hem VR hem Desktop tarafından sürülür.
- Rig **iki ayrı**: `VR` ve `Desktop` rigleri (kamera/ikili el vs. mouse/klavye) ayrıdır.
- UI sunumu modlara göre değişebilir; içerik ve state değişmemelidir.
- Comfort önceliği: VR’da hızlı dönüş/teleport vs. gibi kararlar modül bazlı ve kapatılabilir olmalı.

## Her görev sonunda rapor formatı

- Changed files: Değişen dosyaların listesi (eklenenler dahil).
- Manual test: En az 3–5 maddelik manuel test checklist’i.
- Risk/Notes: Varsayımlar, potansiyel riskler, sonraki adımlar (varsa).

