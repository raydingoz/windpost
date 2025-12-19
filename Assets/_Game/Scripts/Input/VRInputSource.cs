using UnityEngine;
using UnityEngine.InputSystem;

namespace Windpost.Input
{
    public sealed class VRInputSource : IInputSource
    {
        private readonly InputAction _rudder;
        private readonly InputAction _sail;
        private readonly InputAction _interact;
        private readonly InputAction _menu;
        private readonly InputAction _look;
        private readonly InputAction _turn;

        public VRInputSource(InputActionAsset inputActions)
        {
            if (inputActions == null)
            {
                Debug.LogError("[VRInputSource] InputActionAsset is null; all inputs will read default values.");
                return;
            }

            var gameplay = inputActions.FindActionMap("Gameplay", throwIfNotFound: false);
            if (gameplay == null)
            {
                Debug.LogError("[VRInputSource] Action map 'Gameplay' not found; all inputs will read default values.");
                return;
            }

            _rudder = gameplay.FindAction("Rudder", throwIfNotFound: false);
            _sail = gameplay.FindAction("Sail", throwIfNotFound: false);
            _interact = gameplay.FindAction("Interact", throwIfNotFound: false);
            _menu = gameplay.FindAction("Menu", throwIfNotFound: false);
            _look = gameplay.FindAction("Look", throwIfNotFound: false);
            _turn = gameplay.FindAction("Turn", throwIfNotFound: false);
        }

        public float Rudder => ReadAxis(_rudder);
        public float Sail => ReadAxis(_sail);
        public bool InteractPressed => WasPressedThisFrame(_interact);
        public bool MenuPressed => WasPressedThisFrame(_menu);
        public Vector2 Look => ReadVector2(_look);
        public Vector2 Turn => ReadVector2(_turn);

        private static float ReadAxis(InputAction action)
        {
            if (action == null)
            {
                return 0f;
            }

            if (action.expectedControlType == "Axis" || action.expectedControlType == "Button" || string.IsNullOrEmpty(action.expectedControlType))
            {
                return action.ReadValue<float>();
            }

            var value = action.ReadValue<Vector2>();
            return value.x;
        }

        private static Vector2 ReadVector2(InputAction action)
        {
            if (action == null)
            {
                return Vector2.zero;
            }

            if (action.expectedControlType == "Vector2" || string.IsNullOrEmpty(action.expectedControlType))
            {
                return action.ReadValue<Vector2>();
            }

            return new Vector2(action.ReadValue<float>(), 0f);
        }

        private static bool WasPressedThisFrame(InputAction action)
        {
            if (action == null)
            {
                return false;
            }

            return action.WasPressedThisFrame();
        }
    }
}

