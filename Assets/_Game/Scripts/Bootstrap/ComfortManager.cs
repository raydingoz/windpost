using System;
using UnityEngine;
using Windpost.Settings;

namespace Windpost.Bootstrap
{
    public sealed class ComfortManager : MonoBehaviour
    {
        [Header("Defaults")]
        [SerializeField] private ComfortConfig config;
        [SerializeField] private ComfortPreset defaultPreset = ComfortPreset.Comfort;
        [SerializeField] private bool applyDefaultPresetOnAwake = true;

        public static ComfortManager Instance { get; private set; }

        public event Action<ComfortSettings> SettingsChanged;

        public ComfortSettings Current { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[ComfortManager] Duplicate instance found; destroying this one.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (applyDefaultPresetOnAwake)
            {
                ApplyPreset(defaultPreset);
            }
        }

        public void ApplyPreset(ComfortPreset preset)
        {
            var presetSettings = GetPresetSettings(preset);
            SetSettings(presetSettings);
        }

        public void SetSnapTurnEnabled(bool enabled)
        {
            var settings = Current;
            settings.SnapTurnEnabled = enabled;
            SetSettings(settings);
        }

        public void SetSnapTurnDegrees(float degrees)
        {
            var settings = Current;
            settings.SnapTurnDegrees = degrees;
            SetSettings(settings);
        }

        public void SetVignette(float amount01)
        {
            var settings = Current;
            settings.Vignette = amount01;
            SetSettings(settings);
        }

        public void SetSpeedLimit(float speedLimit)
        {
            var settings = Current;
            settings.SpeedLimit = speedLimit;
            SetSettings(settings);
        }

        public void SetHorizonAssist(float amount01)
        {
            var settings = Current;
            settings.HorizonAssist = amount01;
            SetSettings(settings);
        }

        public void SetSettings(ComfortSettings settings)
        {
            var sanitized = ComfortSettings.Sanitize(settings);

            if (AreApproximatelyEqual(Current, sanitized))
            {
                Current = sanitized;
                return;
            }

            Current = sanitized;
            SettingsChanged?.Invoke(Current);
        }

        private ComfortSettings GetPresetSettings(ComfortPreset preset)
        {
            if (config != null)
            {
                return config.GetPreset(preset);
            }

            return preset == ComfortPreset.Performance
                ? ComfortSettings.CreatePerformancePreset()
                : ComfortSettings.CreateComfortPreset();
        }

        private static bool AreApproximatelyEqual(ComfortSettings a, ComfortSettings b)
        {
            if (a.SnapTurnEnabled != b.SnapTurnEnabled)
            {
                return false;
            }

            return Mathf.Abs(a.SnapTurnDegrees - b.SnapTurnDegrees) < 0.0001f &&
                   Mathf.Abs(a.Vignette - b.Vignette) < 0.0001f &&
                   Mathf.Abs(a.SpeedLimit - b.SpeedLimit) < 0.0001f &&
                   Mathf.Abs(a.HorizonAssist - b.HorizonAssist) < 0.0001f;
        }
    }
}

