using System;
using System.Reflection;
using UnityEngine;

namespace Windpost.Bootstrap
{
    public static class XRBootstrap
    {
        private static bool _started;

        public static bool TryStartXR()
        {
            if (_started)
            {
                Debug.Log("[XRBootstrap] XR already started.");
                return true;
            }

            try
            {
                if (!TryGetXrManager(out var manager, out var error))
                {
                    Debug.LogWarning($"[XRBootstrap] XR unavailable: {error}");
                    _started = false;
                    return false;
                }

                Invoke(manager, "InitializeLoaderSync");

                var activeLoader = GetPropertyValue(manager, "activeLoader");
                if (activeLoader == null)
                {
                    Debug.LogWarning("[XRBootstrap] XR loader failed to initialize (activeLoader is null).");
                    _started = false;
                    return false;
                }

                Invoke(manager, "StartSubsystems");
                _started = true;
                Debug.Log("[XRBootstrap] XR started.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[XRBootstrap] XR start failed: {ex}");
                _started = false;
                return false;
            }
        }

        public static void StopXR()
        {
            if (!_started)
            {
                TryStopIfAutoStarted();
                return;
            }

            try
            {
                if (!TryGetXrManager(out var manager, out _))
                {
                    _started = false;
                    return;
                }

                Invoke(manager, "StopSubsystems");
                Invoke(manager, "DeinitializeLoader");
                _started = false;
                Debug.Log("[XRBootstrap] XR stopped.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[XRBootstrap] XR stop encountered an error: {ex}");
                _started = false;
            }
        }

        private static void TryStopIfAutoStarted()
        {
            try
            {
                if (!TryGetXrManager(out var manager, out _))
                {
                    return;
                }

                var activeLoader = GetPropertyValue(manager, "activeLoader");
                if (activeLoader == null)
                {
                    return;
                }

                Invoke(manager, "StopSubsystems");
                Invoke(manager, "DeinitializeLoader");
                Debug.Log("[XRBootstrap] XR was auto-started; stopped by runtime guard.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[XRBootstrap] XR auto-start guard failed to stop XR: {ex}");
            }
        }

        private static bool TryGetXrManager(out object manager, out string error)
        {
            manager = null;
            error = string.Empty;

            var generalSettingsType =
                Type.GetType("UnityEngine.XR.Management.XRGeneralSettings, Unity.XR.Management") ??
                Type.GetType("UnityEngine.XR.Management.XRGeneralSettings");

            if (generalSettingsType == null)
            {
                error = "XRGeneralSettings type not found (XR Management package missing?).";
                return false;
            }

            var instanceProperty = generalSettingsType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            var generalSettingsInstance = instanceProperty?.GetValue(null);
            if (generalSettingsInstance == null)
            {
                error = "XRGeneralSettings.Instance is null.";
                return false;
            }

            var managerProperty = generalSettingsType.GetProperty("Manager", BindingFlags.Public | BindingFlags.Instance);
            manager = managerProperty?.GetValue(generalSettingsInstance);
            if (manager == null)
            {
                error = "XRGeneralSettings.Manager is null.";
                return false;
            }

            return true;
        }

        private static void Invoke(object target, string methodName)
        {
            if (target == null || string.IsNullOrWhiteSpace(methodName))
            {
                throw new ArgumentException("Invalid reflection invoke arguments.");
            }

            var method = target.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            if (method == null)
            {
                throw new MissingMethodException(target.GetType().FullName, methodName);
            }

            method.Invoke(target, null);
        }

        private static object GetPropertyValue(object target, string propertyName)
        {
            if (target == null || string.IsNullOrWhiteSpace(propertyName))
            {
                return null;
            }

            var property = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            return property?.GetValue(target);
        }
    }
}

