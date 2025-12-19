using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Windpost.Bootstrap;

namespace Windpost.Input
{
    public sealed class InputRouter : MonoBehaviour
    {
        public enum RouterMode
        {
            AutoFromModeSelector = 0,
            Desktop = 1,
            VR = 2,
        }

        [Header("Setup")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private RouterMode routerMode = RouterMode.AutoFromModeSelector;

        public event Action<GameMode> ModeChanged;

        public GameMode CurrentMode { get; private set; } = GameMode.Desktop;
        public IInputSource CurrentInputSource => _currentInputSource;

        private DesktopInputSource _desktopInputSource;
        private VRInputSource _vrInputSource;
        private IInputSource _currentInputSource;

        private void Awake()
        {
            _desktopInputSource = new DesktopInputSource(inputActions);
            _vrInputSource = new VRInputSource(inputActions);
            SetMode(DetermineInitialMode());
        }

        private void OnEnable()
        {
            if (inputActions == null)
            {
                Debug.LogError("[InputRouter] InputActions not assigned.");
                return;
            }

            inputActions.Enable();
        }

        private void OnDisable()
        {
            if (inputActions == null)
            {
                return;
            }

            inputActions.Disable();
        }

        public void SetMode(GameMode mode)
        {
            CurrentMode = mode;
            ApplyBindingMask(mode);
            _currentInputSource = mode == GameMode.VR ? _vrInputSource : _desktopInputSource;
            ModeChanged?.Invoke(mode);
        }

        private void ApplyBindingMask(GameMode mode)
        {
            if (inputActions == null)
            {
                return;
            }

            inputActions.bindingMask = mode == GameMode.VR
                ? InputBinding.MaskByGroup("VR")
                : InputBinding.MaskByGroup("Desktop");
        }

        private GameMode DetermineInitialMode()
        {
            if (routerMode == RouterMode.Desktop)
            {
                return GameMode.Desktop;
            }

            if (routerMode == RouterMode.VR)
            {
                return GameMode.VR;
            }

            return ModeSelector.DetermineMode();
        }
    }
}
