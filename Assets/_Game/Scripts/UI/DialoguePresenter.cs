using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Windpost.UI
{
    public sealed class DialoguePresenter : MonoBehaviour
    {
        [Header("Setup (optional; can auto-build)")]
        [SerializeField] private bool autoBuildIfMissing = true;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text subtitleText;

        [Header("Presentation")]
        [SerializeField] private bool worldSpace = false;
        [SerializeField] private Vector2 canvasSize = new Vector2(1200f, 220f);
        [SerializeField] private float canvasWorldScale = 0.0015f;
        [SerializeField] private Transform followTarget;
        [SerializeField] private Vector3 localOffset = new Vector3(0f, -0.2f, 1.2f);
        [SerializeField] private bool faceFollowTarget = true;

        public bool IsVisible => canvasGroup != null && canvasGroup.alpha > 0.01f;

        private void Awake()
        {
            if (autoBuildIfMissing)
            {
                TryAutoBuild();
            }

            Hide();
        }

        private void LateUpdate()
        {
            if (!worldSpace || canvasGroup == null)
            {
                return;
            }

            if (followTarget == null)
            {
                return;
            }

            var t = canvasGroup.transform;
            t.position = followTarget.TransformPoint(localOffset);

            if (faceFollowTarget)
            {
                var direction = t.position - followTarget.position;
                if (direction.sqrMagnitude > 0.0001f)
                {
                    t.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                }
            }
        }

        public void ShowLine(string text)
        {
            if (subtitleText == null)
            {
                Debug.LogWarning("[DialoguePresenter] subtitleText is not assigned.");
                return;
            }

            subtitleText.text = text ?? string.Empty;
            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        private void TryAutoBuild()
        {
            if (canvasGroup != null && subtitleText != null)
            {
                return;
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponentInChildren<CanvasGroup>(includeInactive: true);
            }

            if (subtitleText == null)
            {
                subtitleText = GetComponentInChildren<TMP_Text>(includeInactive: true);
            }

            if (canvasGroup != null && subtitleText != null)
            {
                return;
            }

            var canvasObject = new GameObject(
                worldSpace ? "DialogueCanvas_WorldSpace" : "DialogueCanvas_Overlay",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(CanvasGroup)
            );
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            if (worldSpace)
            {
                canvas.renderMode = RenderMode.WorldSpace;
            }
            else
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 1800;
            }

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGroup = canvasObject.GetComponent<CanvasGroup>();

            var canvasRect = (RectTransform)canvasObject.transform;
            canvasRect.sizeDelta = canvasSize;
            if (worldSpace)
            {
                canvasObject.transform.localScale = Vector3.one * canvasWorldScale;
            }

            var panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(canvasObject.transform, false);

            var panelRect = (RectTransform)panelObject.transform;
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var panelImage = panelObject.GetComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.65f);

            var textObject = new GameObject("Subtitle (TMP)", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(panelObject.transform, false);

            var textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(32f, 24f);
            textRect.offsetMax = new Vector2(-32f, -24f);

            subtitleText = textObject.GetComponent<TextMeshProUGUI>();
            subtitleText.text = string.Empty;
            subtitleText.fontSize = worldSpace ? 56f : 42f;
            subtitleText.alignment = TextAlignmentOptions.Center;
            subtitleText.enableWordWrapping = true;

            EnsureEventSystemForOverlayIfNeeded(canvas.renderMode);
        }

        private static void EnsureEventSystemForOverlayIfNeeded(RenderMode renderMode)
        {
            if (renderMode != RenderMode.ScreenSpaceOverlay)
            {
                return;
            }

            if (EventSystem.current != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystemObject.hideFlags = HideFlags.DontSave;
        }
    }
}

