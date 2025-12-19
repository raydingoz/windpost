using UnityEngine;

namespace Windpost.Settings
{
    [CreateAssetMenu(menuName = "Windpost/Settings/Comfort Config", fileName = "ComfortConfig")]
    public sealed class ComfortConfig : ScriptableObject
    {
        [Header("Presets")]
        [SerializeField] private ComfortSettings comfortPreset = default;
        [SerializeField] private ComfortSettings performancePreset = default;

        public ComfortSettings ComfortPreset => comfortPreset;
        public ComfortSettings PerformancePreset => performancePreset;

        public ComfortSettings GetPreset(ComfortPreset preset)
        {
            return preset == ComfortPreset.Performance ? performancePreset : comfortPreset;
        }

        private void Reset()
        {
            comfortPreset = ComfortSettings.CreateComfortPreset();
            performancePreset = ComfortSettings.CreatePerformancePreset();
        }

        private void OnValidate()
        {
            comfortPreset = ComfortSettings.Sanitize(comfortPreset);
            performancePreset = ComfortSettings.Sanitize(performancePreset);
        }
    }
}

