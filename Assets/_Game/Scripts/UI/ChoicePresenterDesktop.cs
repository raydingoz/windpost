using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Windpost.UI
{
    public sealed class ChoicePresenterDesktop : MonoBehaviour, IChoicePresenter
    {
        [Header("Setup (optional; can auto-build)")]
        [SerializeField] private bool autoBuildIfMissing = true;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform listRoot;
        [SerializeField] private Button choiceButtonPrefab;

        [Header("Overlay layout")]
        [SerializeField] private Vector2 canvasSize = new Vector2(900f, 520f);
        [SerializeField] private float panelPadding = 40f;
        [SerializeField] private float buttonHeight = 100f;
        [SerializeField] private float buttonFontSize = 44f;

        private readonly List<Button> _buttons = new List<Button>(4);
        private ChoiceData[] _currentChoices;
        private Action<ChoiceData> _onChoiceSelected;

        public bool IsVisible => canvasGroup != null && canvasGroup.alpha > 0.01f;

        private void Awake()
        {
            if (autoBuildIfMissing)
            {
                TryAutoBuild();
            }

            Hide();
        }

        public void Show(IReadOnlyList<ChoiceData> choices, Action<ChoiceData> onChoiceSelected)
        {
            if (choices == null)
            {
                Debug.LogError("[ChoicePresenterDesktop] choices is null.");
                return;
            }

            if (onChoiceSelected == null)
            {
                Debug.LogError("[ChoicePresenterDesktop] onChoiceSelected is null.");
                return;
            }

            if (choices.Count < 1 || choices.Count > 4)
            {
                Debug.LogError($"[ChoicePresenterDesktop] choices count must be 1-4, got {choices.Count}.");
            }

            EnsureEventSystem();
            EnsureButtonPool(Mathf.Clamp(choices.Count, 1, 4));

            _currentChoices = new ChoiceData[Mathf.Clamp(choices.Count, 1, 4)];
            for (var i = 0; i < _currentChoices.Length; i++)
            {
                _currentChoices[i] = choices[i];
            }

            _onChoiceSelected = onChoiceSelected;

            for (var i = 0; i < _buttons.Count; i++)
            {
                var button = _buttons[i];
                var active = i < _currentChoices.Length;

                button.gameObject.SetActive(active);
                button.onClick.RemoveAllListeners();

                if (!active)
                {
                    continue;
                }

                var choice = _currentChoices[i];
                SetButtonLabel(button, choice.Text);

                var capturedIndex = i;
                button.onClick.AddListener(() => SelectByIndex(capturedIndex));
            }

            SetVisible(true);
            SelectFirstButton();
        }

        public void Hide()
        {
            _currentChoices = null;
            _onChoiceSelected = null;
            SetVisible(false);
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

        private void SelectByIndex(int index)
        {
            if (_currentChoices == null || index < 0 || index >= _currentChoices.Length)
            {
                return;
            }

            var choice = _currentChoices[index];
            var callback = _onChoiceSelected;
            Hide();
            callback?.Invoke(choice);
        }

        private void EnsureButtonPool(int targetCount)
        {
            if (listRoot == null)
            {
                Debug.LogError("[ChoicePresenterDesktop] listRoot is not assigned (and auto-build is disabled or failed).");
                return;
            }

            if (choiceButtonPrefab == null)
            {
                Debug.LogError("[ChoicePresenterDesktop] choiceButtonPrefab is not assigned (and auto-build is disabled or failed).");
                return;
            }

            while (_buttons.Count < targetCount)
            {
                var button = Instantiate(choiceButtonPrefab, listRoot);
                _buttons.Add(button);
            }
        }

        private static void SetButtonLabel(Button button, string text)
        {
            if (button == null)
            {
                return;
            }

            var label = button.GetComponentInChildren<TMP_Text>(includeInactive: true);
            if (label == null)
            {
                return;
            }

            label.text = text ?? string.Empty;
        }

        private void SelectFirstButton()
        {
            if (_buttons.Count == 0)
            {
                return;
            }

            var first = _buttons[0];
            if (first == null || !first.gameObject.activeInHierarchy)
            {
                return;
            }

            var eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return;
            }

            eventSystem.SetSelectedGameObject(first.gameObject);
        }

        private void TryAutoBuild()
        {
            if (canvasGroup != null && listRoot != null && choiceButtonPrefab != null)
            {
                return;
            }

            var root = gameObject;

            if (canvasGroup == null)
            {
                canvasGroup = root.GetComponentInChildren<CanvasGroup>(includeInactive: true);
            }

            if (canvasGroup == null)
            {
                var canvasObject = new GameObject("ChoiceCanvas_Desktop", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
                canvasObject.transform.SetParent(root.transform, false);

                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 2000;

                var scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;

                canvasGroup = canvasObject.GetComponent<CanvasGroup>();
            }

            var canvasTransform = canvasGroup.transform as RectTransform;
            if (canvasTransform == null)
            {
                return;
            }

            if (listRoot == null)
            {
                var panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image));
                panelObject.transform.SetParent(canvasTransform, false);

                var panelRect = (RectTransform)panelObject.transform;
                panelRect.anchorMin = new Vector2(0.5f, 0.18f);
                panelRect.anchorMax = new Vector2(0.5f, 0.18f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.sizeDelta = canvasSize;

                var panelImage = panelObject.GetComponent<Image>();
                panelImage.color = new Color(0f, 0f, 0f, 0.72f);

                var listObject = new GameObject("ChoiceList", typeof(RectTransform), typeof(VerticalLayoutGroup));
                listObject.transform.SetParent(panelObject.transform, false);

                var listRect = (RectTransform)listObject.transform;
                listRect.anchorMin = Vector2.zero;
                listRect.anchorMax = Vector2.one;
                listRect.offsetMin = new Vector2(panelPadding, panelPadding);
                listRect.offsetMax = new Vector2(-panelPadding, -panelPadding);

                var layout = listObject.GetComponent<VerticalLayoutGroup>();
                layout.spacing = 18f;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlHeight = true;
                layout.childControlWidth = true;
                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = true;

                listRoot = listRect;
            }

            if (choiceButtonPrefab == null && listRoot != null)
            {
                choiceButtonPrefab = BuildDefaultButtonPrefab();
            }
        }

        private Button BuildDefaultButtonPrefab()
        {
            var buttonObject = new GameObject("ChoiceButtonPrefab", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.hideFlags = HideFlags.HideAndDontSave;

            var rect = (RectTransform)buttonObject.transform;
            rect.sizeDelta = new Vector2(0f, buttonHeight);

            var layout = buttonObject.AddComponent<LayoutElement>();
            layout.minHeight = buttonHeight;

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.12f);

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0.12f);
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.25f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.35f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            var textObject = new GameObject("Text (TMP)", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(buttonObject.transform, false);

            var textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(22f, 14f);
            textRect.offsetMax = new Vector2(-22f, -14f);

            var tmpText = textObject.GetComponent<TextMeshProUGUI>();
            tmpText.text = "Choice";
            tmpText.fontSize = buttonFontSize;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.textWrappingMode = TextWrappingModes.Normal;
            tmpText.enableWordWrapping = true;

            return button;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), GetDefaultInputModuleType());
            eventSystemObject.hideFlags = HideFlags.DontSave;
        }

        private static Type GetDefaultInputModuleType()
        {
#if ENABLE_INPUT_SYSTEM
            return typeof(InputSystemUIInputModule);
#else
            return typeof(StandaloneInputModule);
#endif
        }
    }
}

