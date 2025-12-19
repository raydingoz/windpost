using System;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Windpost.VR
{
    public sealed class VRRigInstaller : MonoBehaviour
    {
        [Header("XR Ray Interactors (optional)")]
        [SerializeField] private Component leftRayInteractor;
        [SerializeField] private Component rightRayInteractor;

        [Header("UI")]
        [SerializeField] private bool ensureEventSystem = true;

        private void Awake()
        {
            Install();
        }

        public void Install()
        {
            if (ensureEventSystem)
            {
                EnsureEventSystem();
            }

            TryEnableUiInteraction(leftRayInteractor);
            TryEnableUiInteraction(rightRayInteractor);
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
            var xrUiInputModuleType = FindType(
                "UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule, Unity.XR.Interaction.Toolkit",
                "UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule, UnityEngine.XR.Interaction.Toolkit"
            );
            if (xrUiInputModuleType != null)
            {
                return xrUiInputModuleType;
            }

#if ENABLE_INPUT_SYSTEM
            return typeof(InputSystemUIInputModule);
#else
            return typeof(StandaloneInputModule);
#endif
        }

        private static void TryEnableUiInteraction(Component rayInteractor)
        {
            if (rayInteractor == null)
            {
                return;
            }

            var type = rayInteractor.GetType();

            var property = type.GetProperty("enableUIInteraction");
            if (property != null && property.PropertyType == typeof(bool) && property.CanWrite)
            {
                property.SetValue(rayInteractor, true);
                return;
            }

            var field = type.GetField("enableUIInteraction");
            if (field != null && field.FieldType == typeof(bool))
            {
                field.SetValue(rayInteractor, true);
            }
        }

        private static Type FindType(params string[] assemblyQualifiedTypeNames)
        {
            if (assemblyQualifiedTypeNames == null)
            {
                return null;
            }

            for (var i = 0; i < assemblyQualifiedTypeNames.Length; i++)
            {
                var name = assemblyQualifiedTypeNames[i];
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var type = Type.GetType(name, throwOnError: false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }
    }
}

