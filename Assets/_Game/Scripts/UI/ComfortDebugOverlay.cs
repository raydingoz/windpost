using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Windpost.Bootstrap;
using Windpost.Settings;

namespace Windpost.UI
{
    public sealed class ComfortDebugOverlay : MonoBehaviour
    {
        [Header("Wiring (optional)")]
        [SerializeField] private ComfortManager comfortManager;

        [Header("Auto-build UI (optional)")]
        [SerializeField] private bool autoBuildIfMissing = true;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text text;

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
        }

        private void OnEnable()
        {
            if (comfortManager != null)
            {
                comfortManager.SettingsChanged += OnSettingsChanged;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (comfortManager != null)
            {
                comfortManager.SettingsChanged -= OnSettingsChanged;
            }
        }

        public void SetVisible(bool visible)
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        private void OnSettingsChanged(ComfortSettings settings)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (text == null || comfortManager == null)
            {
                return;
            }

            var s = comfortManager.Current;
            text.text =
                "Comfort (runtime)\n" +
                $"Snap Turn: {(s.SnapTurnEnabled ? "On" : "Off")} ({s.SnapTurnDegrees:0}°)\n" +
                $"Vignette: {s.Vignette:0.00}\n" +
                $"Speed Limit: {s.SpeedLimit:0.00}\n" +
                $"Horizon Assist: {s.HorizonAssist:0.00}";
        }

        private void TryAutoBuild()
        {
            if (text != null)
            {
                return;
            }

            var canvasObject = new GameObject("ComfortDebugOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, worldPositionStays: false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            canvasGroup = canvasObject.AddComponent<CanvasGroup>();
            SetVisible(true);

            var textObject = new GameObject("Text (TMP)", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(canvasObject.transform, false);

            var rect = (RectTransform)textObject.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(16f, -16f);
            rect.sizeDelta = new Vector2(520f, 220f);

            text = textObject.GetComponent<TextMeshProUGUI>();
            text.fontSize = 28f;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.raycastTarget = false;

            var backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
            backgroundObject.transform.SetParent(textObject.transform, false);
            backgroundObject.transform.SetAsFirstSibling();

            var bgRect = (RectTransform)backgroundObject.transform;
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = new Vector2(-12f, -12f);
            bgRect.offsetMax = new Vector2(12f, 12f);

            var bgImage = backgroundObject.GetComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.55f);
            bgImage.raycastTarget = false;
        }
    }
}

