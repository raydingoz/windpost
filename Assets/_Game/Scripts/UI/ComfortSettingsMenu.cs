using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using Windpost.Bootstrap;
using Windpost.Input;
using Windpost.Settings;

namespace Windpost.UI
{
    public sealed class ComfortSettingsMenu : MonoBehaviour
    {
        [Header("Wiring (optional)")]
        [SerializeField] private ComfortManager comfortManager;
        [SerializeField] private InputRouter inputRouter;

        [Header("Visibility")]
        [SerializeField] private bool toggleWithMenuButton = true;
        [SerializeField] private bool startVisible;

        [Header("Auto-build UI (optional)")]
        [SerializeField] private bool autoBuildIfMissing = true;
        [SerializeField] private bool buildWorldSpaceCanvas;
        [SerializeField] private Vector2 canvasSize = new Vector2(900f, 520f);
        [SerializeField] private float worldSpaceScale = 0.0015f;
        [SerializeField] private Vector3 worldSpacePosition = new Vector3(0f, 1.4f, 1.2f);
        [SerializeField] private Vector3 worldSpaceEulerAngles = new Vector3(0f, 180f, 0f);

        [Header("UI References (optional; auto-build assigns)")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Toggle snapTurnToggle;
        [SerializeField] private Slider snapTurnDegreesSlider;
        [SerializeField] private TMP_Text snapTurnDegreesValueText;
        [SerializeField] private Slider vignetteSlider;
        [SerializeField] private TMP_Text vignetteValueText;
        [SerializeField] private Slider speedLimitSlider;
        [SerializeField] private TMP_Text speedLimitValueText;
        [SerializeField] private Slider horizonAssistSlider;
        [SerializeField] private TMP_Text horizonAssistValueText;

        private bool _suppressUiEvents;

        private void Awake()
        {
            if (autoBuildIfMissing)
            {
                TryAutoBuild();
            }

            if (comfortManager == null)
            {
                comfortManager = FindObjectOfType<ComfortManager>();
            }

            if (inputRouter == null)
            {
                inputRouter = FindObjectOfType<InputRouter>();
            }

            WireUiEvents();

            if (startVisible)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }

        private void OnEnable()
        {
            if (comfortManager != null)
            {
                comfortManager.SettingsChanged += OnSettingsChanged;
            }
        }

        private void OnDisable()
        {
            if (comfortManager != null)
            {
                comfortManager.SettingsChanged -= OnSettingsChanged;
            }
        }

        private void Update()
        {
            if (!toggleWithMenuButton)
            {
                return;
            }

            if (inputRouter == null)
            {
                return;
            }

            var inputSource = inputRouter.CurrentInputSource;
            if (inputSource == null)
            {
                return;
            }

            if (inputSource.MenuPressed)
            {
                Toggle();
            }
        }

        public void Toggle()
        {
            if (IsVisible)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        public void Show()
        {
            EnsureEventSystem();
            SetVisible(true);
            RefreshFromManager();
        }

        public void Hide()
        {
            SetVisible(false);
        }

        public bool IsVisible => canvasGroup != null && canvasGroup.alpha > 0.01f;

        private void OnSettingsChanged(ComfortSettings settings)
        {
            if (!IsVisible)
            {
                return;
            }

            RefreshFromManager();
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        private void RefreshFromManager()
        {
            if (comfortManager == null)
            {
                return;
            }

            var settings = comfortManager.Current;

            _suppressUiEvents = true;

            if (snapTurnToggle != null)
            {
                snapTurnToggle.isOn = settings.SnapTurnEnabled;
            }

            if (snapTurnDegreesSlider != null)
            {
                snapTurnDegreesSlider.value = settings.SnapTurnDegrees;
            }

            if (vignetteSlider != null)
            {
                vignetteSlider.value = settings.Vignette;
            }

            if (speedLimitSlider != null)
            {
                speedLimitSlider.value = settings.SpeedLimit;
            }

            if (horizonAssistSlider != null)
            {
                horizonAssistSlider.value = settings.HorizonAssist;
            }

            UpdateValueTexts(settings);
            _suppressUiEvents = false;
        }

        private void UpdateValueTexts(ComfortSettings settings)
        {
            if (snapTurnDegreesValueText != null)
            {
                snapTurnDegreesValueText.text = $"{settings.SnapTurnDegrees:0}°";
            }

            if (vignetteValueText != null)
            {
                vignetteValueText.text = $"{settings.Vignette:0.00}";
            }

            if (speedLimitValueText != null)
            {
                speedLimitValueText.text = $"{settings.SpeedLimit:0.00}";
            }

            if (horizonAssistValueText != null)
            {
                horizonAssistValueText.text = $"{settings.HorizonAssist:0.00}";
            }
        }

        private void WireUiEvents()
        {
            if (comfortManager == null)
            {
                return;
            }

            if (snapTurnToggle != null)
            {
                snapTurnToggle.onValueChanged.RemoveAllListeners();
                snapTurnToggle.onValueChanged.AddListener(isOn =>
                {
                    if (_suppressUiEvents)
                    {
                        return;
                    }

                    comfortManager.SetSnapTurnEnabled(isOn);
                });
            }

            if (snapTurnDegreesSlider != null)
            {
                snapTurnDegreesSlider.onValueChanged.RemoveAllListeners();
                snapTurnDegreesSlider.onValueChanged.AddListener(value =>
                {
                    if (_suppressUiEvents)
                    {
                        return;
                    }

                    comfortManager.SetSnapTurnDegrees(value);
                    UpdateValueTexts(comfortManager.Current);
                });
            }

            if (vignetteSlider != null)
            {
                vignetteSlider.onValueChanged.RemoveAllListeners();
                vignetteSlider.onValueChanged.AddListener(value =>
                {
                    if (_suppressUiEvents)
                    {
                        return;
                    }

                    comfortManager.SetVignette(value);
                    UpdateValueTexts(comfortManager.Current);
                });
            }

            if (speedLimitSlider != null)
            {
                speedLimitSlider.onValueChanged.RemoveAllListeners();
                speedLimitSlider.onValueChanged.AddListener(value =>
                {
                    if (_suppressUiEvents)
                    {
                        return;
                    }

                    comfortManager.SetSpeedLimit(value);
                    UpdateValueTexts(comfortManager.Current);
                });
            }

            if (horizonAssistSlider != null)
            {
                horizonAssistSlider.onValueChanged.RemoveAllListeners();
                horizonAssistSlider.onValueChanged.AddListener(value =>
                {
                    if (_suppressUiEvents)
                    {
                        return;
                    }

                    comfortManager.SetHorizonAssist(value);
                    UpdateValueTexts(comfortManager.Current);
                });
            }
        }

        private void TryAutoBuild()
        {
            if (canvasGroup != null)
            {
                return;
            }

            var canvasObject = new GameObject("ComfortSettingsMenu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, worldPositionStays: false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.pixelPerfect = false;

            var rect = (RectTransform)canvasObject.transform;

            if (buildWorldSpaceCanvas)
            {
                canvas.renderMode = RenderMode.WorldSpace;
                rect.sizeDelta = canvasSize;
                rect.localScale = Vector3.one * worldSpaceScale;
                rect.position = worldSpacePosition;
                rect.rotation = Quaternion.Euler(worldSpaceEulerAngles);
                TryUpgradeToTrackedDeviceRaycaster(canvasObject);
            }
            else
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGroup = canvasObject.AddComponent<CanvasGroup>();

            var panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(canvasObject.transform, false);

            var panelRect = (RectTransform)panelObject.transform;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(canvasSize.x, canvasSize.y);

            var panelImage = panelObject.GetComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.72f);

            var layoutObject = new GameObject("Layout", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            layoutObject.transform.SetParent(panelObject.transform, false);

            var layoutRect = (RectTransform)layoutObject.transform;
            layoutRect.anchorMin = Vector2.zero;
            layoutRect.anchorMax = Vector2.one;
            layoutRect.offsetMin = new Vector2(40f, 40f);
            layoutRect.offsetMax = new Vector2(-40f, -40f);

            var vLayout = layoutObject.GetComponent<VerticalLayoutGroup>();
            vLayout.spacing = 18f;
            vLayout.childAlignment = TextAnchor.UpperLeft;
            vLayout.childControlHeight = true;
            vLayout.childControlWidth = true;
            vLayout.childForceExpandHeight = false;
            vLayout.childForceExpandWidth = true;

            var fitter = layoutObject.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            AddHeader(layoutObject.transform, "Comfort Settings");
            snapTurnToggle = AddToggle(layoutObject.transform, "Snap Turn", out _);
            snapTurnDegreesSlider = AddSlider(layoutObject.transform, "Snap Degrees", 0f, 90f, out snapTurnDegreesValueText);
            vignetteSlider = AddSlider(layoutObject.transform, "Vignette", 0f, 1f, out vignetteValueText);
            speedLimitSlider = AddSlider(layoutObject.transform, "Speed Limit", 0f, 5f, out speedLimitValueText);
            horizonAssistSlider = AddSlider(layoutObject.transform, "Horizon Assist", 0f, 1f, out horizonAssistValueText);
        }

        private static void AddHeader(Transform parent, string title)
        {
            var headerObject = new GameObject("Header", typeof(RectTransform), typeof(TextMeshProUGUI));
            headerObject.transform.SetParent(parent, false);

            var text = headerObject.GetComponent<TextMeshProUGUI>();
            text.text = title;
            text.fontSize = 54f;
            text.margin = new Vector4(0f, 0f, 0f, 12f);
            text.alignment = TextAlignmentOptions.Left;
        }

        private static Toggle AddToggle(Transform parent, string label, out TMP_Text valueText)
        {
            var row = CreateRow(parent, label, out valueText);

            var toggleObject = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle), typeof(Image));
            toggleObject.transform.SetParent(row, false);

            var background = toggleObject.GetComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0.12f);

            var checkmarkObject = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkmarkObject.transform.SetParent(toggleObject.transform, false);

            var checkmarkRect = (RectTransform)checkmarkObject.transform;
            checkmarkRect.anchorMin = new Vector2(0.5f, 0.5f);
            checkmarkRect.anchorMax = new Vector2(0.5f, 0.5f);
            checkmarkRect.pivot = new Vector2(0.5f, 0.5f);
            checkmarkRect.sizeDelta = new Vector2(18f, 18f);

            var checkmark = checkmarkObject.GetComponent<Image>();
            checkmark.color = new Color(0.25f, 0.8f, 1f, 0.95f);

            var toggle = toggleObject.GetComponent<Toggle>();
            toggle.targetGraphic = background;
            toggle.graphic = checkmark;

            var rect = (RectTransform)toggleObject.transform;
            rect.sizeDelta = new Vector2(42f, 42f);

            if (valueText != null)
            {
                valueText.transform.SetAsLastSibling();
            }

            return toggle;
        }

        private static Slider AddSlider(Transform parent, string label, float min, float max, out TMP_Text valueText)
        {
            var row = CreateRow(parent, label, out valueText);

            var sliderObject = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            sliderObject.transform.SetParent(row, false);

            var sliderRect = (RectTransform)sliderObject.transform;
            sliderRect.sizeDelta = new Vector2(420f, 42f);

            var backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
            backgroundObject.transform.SetParent(sliderObject.transform, false);

            var backgroundRect = (RectTransform)backgroundObject.transform;
            backgroundRect.anchorMin = new Vector2(0f, 0.5f);
            backgroundRect.anchorMax = new Vector2(1f, 0.5f);
            backgroundRect.sizeDelta = new Vector2(0f, 10f);
            backgroundRect.anchoredPosition = Vector2.zero;

            var backgroundImage = backgroundObject.GetComponent<Image>();
            backgroundImage.color = new Color(1f, 1f, 1f, 0.12f);

            var fillAreaObject = new GameObject("Fill Area", typeof(RectTransform));
            fillAreaObject.transform.SetParent(sliderObject.transform, false);

            var fillAreaRect = (RectTransform)fillAreaObject.transform;
            fillAreaRect.anchorMin = new Vector2(0f, 0.5f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.5f);
            fillAreaRect.sizeDelta = new Vector2(-20f, 10f);
            fillAreaRect.anchoredPosition = Vector2.zero;

            var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(fillAreaObject.transform, false);

            var fillRect = (RectTransform)fillObject.transform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            var fillImage = fillObject.GetComponent<Image>();
            fillImage.color = new Color(0.25f, 0.8f, 1f, 0.75f);

            var handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleObject.transform.SetParent(sliderObject.transform, false);

            var handleRect = (RectTransform)handleObject.transform;
            handleRect.sizeDelta = new Vector2(24f, 24f);

            var handleImage = handleObject.GetComponent<Image>();
            handleImage.color = new Color(1f, 1f, 1f, 0.75f);

            var slider = sliderObject.GetComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.targetGraphic = handleImage;
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;

            slider.direction = Slider.Direction.LeftToRight;

            if (valueText != null)
            {
                valueText.transform.SetAsLastSibling();
            }

            return slider;
        }

        private static Transform CreateRow(Transform parent, string label, out TMP_Text valueText)
        {
            var rowObject = new GameObject(label, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            rowObject.transform.SetParent(parent, false);

            var layout = rowObject.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 16f;
            layout.childControlHeight = true;
            layout.childControlWidth = false;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(rowObject.transform, false);

            var labelText = labelObject.GetComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 36f;
            labelText.alignment = TextAlignmentOptions.Left;

            var labelLayout = labelObject.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = 320f;

            var valueObject = new GameObject("Value", typeof(RectTransform), typeof(TextMeshProUGUI));
            valueObject.transform.SetParent(rowObject.transform, false);

            valueText = valueObject.GetComponent<TextMeshProUGUI>();
            valueText.text = string.Empty;
            valueText.fontSize = 32f;
            valueText.alignment = TextAlignmentOptions.Right;

            var valueLayout = valueObject.AddComponent<LayoutElement>();
            valueLayout.preferredWidth = 120f;

            return rowObject.transform;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            if (Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem") != null)
            {
                eventSystemObject.AddComponent<InputSystemUIInputModule>();
                return;
            }
#endif
            var legacyModuleType = Type.GetType("UnityEngine.EventSystems.StandaloneInputModule, UnityEngine.UI");
            if (legacyModuleType != null)
            {
                eventSystemObject.AddComponent(legacyModuleType);
            }
        }

        private static void TryUpgradeToTrackedDeviceRaycaster(GameObject canvasObject)
        {
            if (canvasObject == null)
            {
                return;
            }

            var trackedRaycasterType =
                Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit") ??
                Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, UnityEngine.XR.Interaction.Toolkit");

            if (trackedRaycasterType == null)
            {
                return;
            }

            if (canvasObject.GetComponent(trackedRaycasterType) != null)
            {
                return;
            }

            canvasObject.AddComponent(trackedRaycasterType);

            var classicRaycaster = canvasObject.GetComponent<GraphicRaycaster>();
            if (classicRaycaster != null)
            {
                Destroy(classicRaycaster);
            }
        }
    }
}
