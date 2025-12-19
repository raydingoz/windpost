using UnityEngine;

namespace Windpost.Bootstrap
{
    public sealed class AppBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            var noVr = ModeSelector.IsNoVr();
            var mode = ModeSelector.DetermineMode();
            Debug.Log($"[AppBootstrap] Selected mode: {mode} (args: -vr to enable VR, -novr to force Desktop).");

            if (noVr)
            {
                Debug.Log("[AppBootstrap] -novr present; blocking XR.");
                XRBootstrap.StopXR();
                return;
            }

            if (mode == GameMode.VR)
            {
                var started = XRBootstrap.TryStartXR();
                Debug.Log(started
                    ? "[AppBootstrap] XR start result: success."
                    : "[AppBootstrap] XR start result: failed; continuing in Desktop mode.");

                if (!started)
                {
                    XRBootstrap.StopXR();
                }

                return;
            }

            XRBootstrap.StopXR();
        }
    }
}
