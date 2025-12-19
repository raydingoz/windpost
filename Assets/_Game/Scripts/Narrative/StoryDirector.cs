using System;
using System.Collections.Generic;
using Ink.Runtime;
using UnityEngine;
using Windpost.Save;
using Windpost.UI;

namespace Windpost.Narrative
{
    public sealed class StoryDirector : MonoBehaviour
    {
        [Header("Ink")]
        [SerializeField] private TextAsset storyJsonAsset;
        [SerializeField] private string startingKnot = "main";

        [Header("State")]
        [SerializeField] private FlagStore flagStore;
        [SerializeField] private string tone = "neutral";
        [SerializeField] private string route = "default";

        [Header("Save")]
        [SerializeField] private bool tryLoadOnStart = true;
        [SerializeField] private SaveService saveService;

        [Header("UI (optional)")]
        [SerializeField] private DialoguePresenter dialoguePresenter;
        [SerializeField] private ChoicePresenterVR choicePresenterVr;
        [SerializeField] private ChoicePresenterDesktop choicePresenterDesktop;

        private Story _story;
        private IReadOnlyList<ChoiceData> _lastChoices;
        private string _lastPathString;

        public event Action<string> LineEmitted;
        public event Action<IReadOnlyList<ChoiceData>> ChoicesPresented;
        public event Action StoryEnded;

        public bool HasActiveStory => _story != null;

        private void Start()
        {
            if (tryLoadOnStart)
            {
                if (TryLoadFromSave())
                {
                    return;
                }
            }

            StartNewStory();
        }

        public void StartNewStory()
        {
            if (!TryCreateStory(out _story))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(startingKnot))
            {
                TryChoosePath(startingKnot);
            }

            Continue();
        }

        public void Continue()
        {
            if (_story == null)
            {
                Debug.LogWarning("[StoryDirector] Continue called with no active story.");
                return;
            }

            HideChoices();

            while (_story.canContinue)
            {
                var line = _story.Continue();
                _lastPathString = _story.state?.currentPathString ?? _lastPathString;
                EmitLine(line);
            }

            if (_story.currentChoices != null && _story.currentChoices.Count > 0)
            {
                PresentChoices(_story.currentChoices);
                return;
            }

            if (!_story.canContinue)
            {
                StoryEnded?.Invoke();
            }
        }

        public void ChooseChoice(ChoiceData choice)
        {
            if (_story == null)
            {
                return;
            }

            if (!int.TryParse(choice.Id, out var index))
            {
                Debug.LogWarning($"[StoryDirector] Choice id is not a valid index: '{choice.Id}'.");
                return;
            }

            if (_story.currentChoices == null || index < 0 || index >= _story.currentChoices.Count)
            {
                Debug.LogWarning($"[StoryDirector] Choice index out of range: {index}.");
                return;
            }

            _story.ChooseChoiceIndex(index);
            _lastPathString = _story.state?.currentPathString ?? _lastPathString;
            Continue();
        }

        public void SaveNow()
        {
            if (saveService == null)
            {
                Debug.LogWarning("[StoryDirector] SaveNow called but saveService is not assigned.");
                return;
            }

            var saveData = BuildSaveData();
            saveService.Save(saveData);
        }

        public bool TryLoadFromSave()
        {
            if (saveService == null)
            {
                return false;
            }

            if (!saveService.TryLoad(out var saveData))
            {
                return false;
            }

            if (!TryCreateStory(out _story))
            {
                return false;
            }

            tone = saveData.Tone ?? tone;
            route = saveData.Route ?? route;

            if (flagStore != null)
            {
                flagStore.ReplaceAll(saveData.GetFlags());
            }

            if (!string.IsNullOrWhiteSpace(saveData.CurrentKnotOrPath))
            {
                TryChoosePath(saveData.CurrentKnotOrPath);
            }
            else if (!string.IsNullOrWhiteSpace(startingKnot))
            {
                TryChoosePath(startingKnot);
            }

            Continue();
            return true;
        }

        private SaveData BuildSaveData()
        {
            var saveData = new SaveData
            {
                Tone = tone,
                Route = route,
                CurrentKnotOrPath = _lastPathString,
            };

            if (flagStore != null)
            {
                saveData.SetFlags(flagStore.Flags);
            }

            return saveData;
        }

        private bool TryCreateStory(out Story story)
        {
            story = null;

            if (storyJsonAsset == null || string.IsNullOrWhiteSpace(storyJsonAsset.text))
            {
                Debug.LogError("[StoryDirector] storyJsonAsset is not assigned (or empty). Assign the compiled Ink JSON TextAsset.");
                return false;
            }

            try
            {
                story = new Story(storyJsonAsset.text);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StoryDirector] Failed to create Ink story (is this compiled JSON?): {ex}");
                return false;
            }

            if (flagStore != null)
            {
                flagStore.BindToStory(story);
            }

            return true;
        }

        private void TryChoosePath(string knotOrPath)
        {
            if (_story == null || string.IsNullOrWhiteSpace(knotOrPath))
            {
                return;
            }

            try
            {
                _story.ChoosePathString(knotOrPath);
                _lastPathString = _story.state?.currentPathString ?? _lastPathString;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[StoryDirector] ChoosePathString failed for '{knotOrPath}': {ex.Message}");
            }
        }

        private void EmitLine(string line)
        {
            var text = line?.Trim();
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            LineEmitted?.Invoke(text);

            if (dialoguePresenter != null)
            {
                dialoguePresenter.ShowLine(text);
            }
        }

        private void PresentChoices(IReadOnlyList<Choice> choices)
        {
            var mapped = new List<ChoiceData>(choices?.Count ?? 0);

            if (choices != null)
            {
                for (var i = 0; i < choices.Count; i++)
                {
                    var choice = choices[i];
                    mapped.Add(new ChoiceData(choice.index.ToString(), choice.text));
                }
            }

            _lastChoices = mapped;

            ChoicesPresented?.Invoke(mapped);

            var presenter = SelectChoicePresenter();
            if (presenter != null)
            {
                presenter.Show(mapped, ChooseChoice);
            }
        }

        private IChoicePresenter SelectChoicePresenter()
        {
            if (choicePresenterVr != null && choicePresenterVr.gameObject.activeInHierarchy)
            {
                return choicePresenterVr;
            }

            if (choicePresenterDesktop != null && choicePresenterDesktop.gameObject.activeInHierarchy)
            {
                return choicePresenterDesktop;
            }

            return null;
        }

        private void HideChoices()
        {
            if (choicePresenterVr != null)
            {
                choicePresenterVr.Hide();
            }

            if (choicePresenterDesktop != null)
            {
                choicePresenterDesktop.Hide();
            }

            _lastChoices = null;
        }
    }
}

