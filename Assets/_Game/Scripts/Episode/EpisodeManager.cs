using UnityEngine;
using UnityEngine.SceneManagement;
using Windpost.Narrative;

namespace Windpost.Episode
{
    public sealed class EpisodeManager : MonoBehaviour
    {
        [SerializeField] private SleepPoint[] sleepPoints;
        [SerializeField] private StoryDirector storyDirector;

        [Header("Loop (placeholder)")]
        [SerializeField] private bool reloadActiveSceneOnSleep = true;
        [SerializeField] private string sceneNameOverride;

        private void OnEnable()
        {
            SetSubscriptions(active: true);
        }

        private void OnDisable()
        {
            SetSubscriptions(active: false);
        }

        private void SetSubscriptions(bool active)
        {
            if (sleepPoints == null || sleepPoints.Length == 0)
            {
                return;
            }

            for (var i = 0; i < sleepPoints.Length; i++)
            {
                var point = sleepPoints[i];
                if (point == null)
                {
                    continue;
                }

                if (active)
                {
                    point.SleepTriggered += OnSleepTriggered;
                }
                else
                {
                    point.SleepTriggered -= OnSleepTriggered;
                }
            }
        }

        private void OnSleepTriggered()
        {
            if (storyDirector != null)
            {
                storyDirector.SaveNow();
            }

            if (!reloadActiveSceneOnSleep)
            {
                return;
            }

            var sceneName = string.IsNullOrWhiteSpace(sceneNameOverride)
                ? SceneManager.GetActiveScene().name
                : sceneNameOverride;

            if (!string.IsNullOrWhiteSpace(sceneName))
            {
                SceneManager.LoadScene(sceneName);
            }
        }
    }
}

