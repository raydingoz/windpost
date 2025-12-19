using UnityEngine;
using Windpost.Bootstrap;

namespace Windpost.Input
{
    public sealed class InputDebugLogger : MonoBehaviour
    {
        [SerializeField] private InputRouter inputRouter;
        [SerializeField] private bool logEveryFrame;
        [SerializeField] private float axisLogThreshold = 0.05f;

        private Vector2 _lastLook;
        private Vector2 _lastTurn;
        private float _lastRudder;
        private float _lastSail;

        private void Awake()
        {
            if (inputRouter == null)
            {
                inputRouter = GetComponent<InputRouter>();
            }
        }

        private void Update()
        {
            if (inputRouter == null)
            {
                return;
            }

            var input = inputRouter.CurrentInputSource;
            if (input == null)
            {
                return;
            }

            if (inputRouter.CurrentMode == GameMode.Desktop)
            {
                LogAxisIfChanged("Rudder", ref _lastRudder, input.Rudder);
                LogAxisIfChanged("Sail", ref _lastSail, input.Sail);
                LogVector2IfChanged("Look", ref _lastLook, input.Look);
                LogVector2IfChanged("Turn", ref _lastTurn, input.Turn);
            }

            if (input.InteractPressed)
            {
                Debug.Log("[InputDebug] InteractPressed");
            }

            if (input.MenuPressed)
            {
                Debug.Log("[InputDebug] MenuPressed");
            }
        }

        private void LogAxisIfChanged(string name, ref float lastValue, float newValue)
        {
            if (!logEveryFrame && Mathf.Abs(newValue - lastValue) < axisLogThreshold)
            {
                return;
            }

            lastValue = newValue;
            Debug.Log($"[InputDebug] {name}={newValue:0.000}");
        }

        private void LogVector2IfChanged(string name, ref Vector2 lastValue, Vector2 newValue)
        {
            if (!logEveryFrame && Vector2.Distance(newValue, lastValue) < axisLogThreshold)
            {
                return;
            }

            lastValue = newValue;
            Debug.Log($"[InputDebug] {name}={newValue}");
        }
    }
}

