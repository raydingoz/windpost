# Sail – Interaction Pack (Desktop + VR)

Bu paket; aynı WorldScene içinde Desktop (PC) ve VR etkileşimlerini birlikte yürütmek için temel interaksiyon, mekanizma kontrolleri, outline highlight ve ayarlar menüsü scriptlerini içerir.

## İçerik (Klasörler)

- `Assets/Sail/Scripts/Interaction/`
  - `DesktopGrabInteractor.cs` : Desktop raycast + LMB tut/taşı, RMB döndür, wheel mesafe (smooth)
  - `IDesktopGrabControl.cs` : Mekanizma kontrol interface’i
  - `PromptUI.cs` : TMP / legacy UI text sürücü
  - `ReticleFeedbackUI.cs` : Focus/Grab durumuna göre reticle görsel feedback
- `Assets/Sail/Scripts/Visual/`
  - `OutlineHighlightFeedback.cs` : URP outline highlight (Desktop + VR)
- `Assets/Sail/Shaders/`
  - `Sail_URP_Outline.shader` : Outline shader (URP)
- `Assets/Sail/Scripts/Mechanisms/`
  - `DesktopDetentLinearControl.cs` : Gaz kolu (Idle/Half/Full) detent + snap/hysteresis
  - `DesktopRotaryControlPro.cs` : Dümen/valf/knob (smooth rotary)
  - `DesktopToggleSwitchControl.cs` : 2 konum switch
  - `DesktopPushButtonControl.cs` : Momentary button
  - `DesktopCrankControl.cs` : Crank/winch
  - `MechanismAudioFeedback.cs` : Opsiyonel hareket loop + detent click
  - `CargoItem.cs` : Opsiyonel kargo mass helper
- `Assets/Sail/Scripts/VR/`
  - `XRInteractableHighlighter.cs` : XRI hover/select -> outline tetikler
  - `XRGrabMassScaler.cs` : Rigidbody.mass’e göre throw scale azaltır (reflection)
- `Assets/Sail/Scripts/Settings/`
  - `SettingsManager.cs` : PlayerPrefs (FOV / mouse sens / invertY)
  - `SettingsMenuUI.cs` : UI slider/toggle bağları
  - `SettingsApplierDesktop.cs` : Desktop kamera + rig controller’a ayarları uygular  
    Not: `GameMode` bağımlılığı yok; `camera.stereoEnabled == false` iken FOV uygular.

## Sahne Kurulumu (Önerilen)

### WorldScene (özet)
- `WorldCanvas`
  - `Reticle` (Image) + `ReticleFeedbackUI`
  - `PromptText` (TMP_Text) + `PromptUI`
  - `SettingsPanel` (kapalı) + `SettingsMenuUI`
- `PlayerSpawnPoint` (Transform)
- (Varsa) `PlayerRigSpawner` (senin mevcut spawner’ın)
  - `desktopRigPrefab` -> `DesktopRig.prefab`
  - `vrRigPrefab` -> `XROriginRig.prefab`
  - `spawnOverride` -> `PlayerSpawnPoint`

## DesktopRig.prefab Bağlantıları

- Root:
  - Senin controller scriptin (ör. `DesktopRigControllerPro`)
  - `SettingsApplierDesktop`
    - `desktopCamera` = Main Camera
    - `desktopRigController` = Desktop rig controller component’i
- Child (öneri): `DesktopInteractor`
  - `DesktopGrabInteractor`
    - `cam` = Main Camera
    - `promptUI` = WorldCanvas/PromptText üzerindeki `PromptUI`
    - `reticleUI` = WorldCanvas/Reticle üzerindeki `ReticleFeedbackUI`
    - `rigInputSource` = (opsiyonel) rig controller (InputEnabled property’si varsa)

## Interactable Obje Kurulumu

Her etkileşilebilir objenin ROOT’una:
- `OutlineHighlightFeedback`

Mekanizma pivot’una (hareket eden parçaya) ihtiyaca göre:
- Gaz kolu: `DesktopDetentLinearControl`
- Dümen/valf: `DesktopRotaryControlPro`
- Switch: `DesktopToggleSwitchControl`
- Button: `DesktopPushButtonControl`
- Crank: `DesktopCrankControl`

Event’leri (`UnityEvent`) kendi oyun mantığına bağla:
- `onValue01Changed(float)` -> throttle/rudder vb.
- `onDetentNameChanged(string)` -> opsiyonel UI/ses/telemetry

## VR Highlight (XRI)

Objede `XRBaseInteractable` (örn. `XRGrabInteractable`) varsa aynı objeye:
- `XRInteractableHighlighter`

Bu script hover/select durumunda `OutlineHighlightFeedback`’i tetikler.

## Kargo / Kütle Hissi

- Desktop: `DesktopGrabInteractor` Rigidbody.mass’e göre joint drive ölçekler (ağır kargo daha “mushy/ağır” hisseder).
- VR: Kargo objesine ekle:
  - `XRGrabInteractable`
  - `XRGrabMassScaler`

## Notlar / Sorun Giderme

- Outline görünmüyorsa:
  - URP aktif olmalı
  - Shader: `Sail/URP/Outline` bulunuyor olmalı (`Sail_URP_Outline.shader` import edilmiş olmalı)
- Prompt/Reticle çalışmıyorsa:
  - `DesktopGrabInteractor` içindeki `promptUI` ve `reticleUI` referanslarını kontrol et.
- Raycast kaçırıyorsa:
  - `DesktopGrabInteractor.layers` mask’ine interactable layer’ını ekle.
  - Collider’ların doğru olduğundan emin ol.
