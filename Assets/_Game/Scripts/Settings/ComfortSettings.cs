using System;
using UnityEngine;

namespace Windpost.Settings
{
    [Serializable]
    public struct ComfortSettings
    {
        public bool SnapTurnEnabled;
        public float SnapTurnDegrees;
        [Range(0f, 1f)] public float Vignette;
        public float SpeedLimit;
        [Range(0f, 1f)] public float HorizonAssist;

        public ComfortSettings(bool snapTurnEnabled, float snapTurnDegrees, float vignette, float speedLimit, float horizonAssist)
        {
            SnapTurnEnabled = snapTurnEnabled;
            SnapTurnDegrees = snapTurnDegrees;
            Vignette = vignette;
            SpeedLimit = speedLimit;
            HorizonAssist = horizonAssist;
        }

        public static ComfortSettings CreateComfortPreset()
        {
            return new ComfortSettings(
                snapTurnEnabled: true,
                snapTurnDegrees: 45f,
                vignette: 0.6f,
                speedLimit: 1f,
                horizonAssist: 0.7f
            );
        }

        public static ComfortSettings CreatePerformancePreset()
        {
            return new ComfortSettings(
                snapTurnEnabled: false,
                snapTurnDegrees: 0f,
                vignette: 0f,
                speedLimit: 2f,
                horizonAssist: 0f
            );
        }

        public static ComfortSettings Sanitize(ComfortSettings settings)
        {
            settings.SnapTurnDegrees = Mathf.Clamp(settings.SnapTurnDegrees, 0f, 90f);
            settings.Vignette = Mathf.Clamp01(settings.Vignette);
            settings.SpeedLimit = Mathf.Max(0f, settings.SpeedLimit);
            settings.HorizonAssist = Mathf.Clamp01(settings.HorizonAssist);
            return settings;
        }
    }
}

