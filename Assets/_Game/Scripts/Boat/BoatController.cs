using UnityEngine;
using UnityEngine.InputSystem;
using Windpost.Bootstrap;
using Windpost.Input;

namespace Windpost.Boat
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class BoatController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Rigidbody body;
        [SerializeField] private InputRouter inputRouter;
        [SerializeField] private ComfortManager comfortManager;

        [Header("Input (fallback, single-scene tests)")]
        [SerializeField] private InputActionAsset inputActions;

        [Header("Comfort")]
        [SerializeField] private bool readSpeedLimitFromComfortManager = true;
        [SerializeField] private float fallbackSpeedLimit = 2f;

        [Header("Physics (sim-cade)")]
        [SerializeField] private float accelClamp = 1.5f;
        [SerializeField] private float turnClamp = 25f;
        [SerializeField] private float lateralDamping = 1.2f;

        [Header("Debug")]
        [SerializeField] private bool drawDebug = true;

        private IInputSource _fallbackInputSource;

        private void Awake()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            if (inputRouter == null)
            {
                inputRouter = FindObjectOfType<InputRouter>();
            }

            if (comfortManager == null)
            {
                comfortManager = ComfortManager.Instance != null ? ComfortManager.Instance : FindObjectOfType<ComfortManager>();
            }

            if (inputRouter == null && inputActions != null)
            {
                _fallbackInputSource = new DesktopInputSource(inputActions);
            }
        }

        private void OnEnable()
        {
            if (inputRouter == null && inputActions != null)
            {
                inputActions.Enable();
            }
        }

        private void OnDisable()
        {
            if (inputRouter == null && inputActions != null)
            {
                inputActions.Disable();
            }
        }

        private void FixedUpdate()
        {
            if (body == null)
            {
                return;
            }

            var inputSource = GetInputSource();
            if (inputSource == null)
            {
                return;
            }

            var rudderInput = Mathf.Clamp(inputSource.Rudder, -1f, 1f);
            var sailInput = Mathf.Clamp(inputSource.Sail, -1f, 1f);

            ApplyTurn(rudderInput);
            ApplyThrust(sailInput);
            ApplyLateralDamping();
        }

        private IInputSource GetInputSource()
        {
            var routed = inputRouter != null ? inputRouter.CurrentInputSource : null;
            if (routed != null)
            {
                return routed;
            }

            if (_fallbackInputSource != null)
            {
                return _fallbackInputSource;
            }

            return null;
        }

        private void ApplyTurn(float rudderInput)
        {
            if (Mathf.Abs(rudderInput) < 0.001f)
            {
                return;
            }

            var yawAccel = Mathf.Clamp(rudderInput * turnClamp, -turnClamp, turnClamp) * Mathf.Deg2Rad;
            body.AddTorque(Vector3.up * yawAccel, ForceMode.Acceleration);
        }

        private void ApplyThrust(float sailInput)
        {
            if (Mathf.Abs(sailInput) < 0.001f)
            {
                return;
            }

            var speedLimit = Mathf.Max(0f, GetSpeedLimit());
            var planarVelocity = Vector3.ProjectOnPlane(body.velocity, Vector3.up);
            var planarSpeed = planarVelocity.magnitude;

            var forwardInput = sailInput;
            if (speedLimit > 0f && planarSpeed >= speedLimit && forwardInput > 0f)
            {
                forwardInput = 0f;
            }

            var forwardAccel = Mathf.Clamp(forwardInput * accelClamp, -accelClamp, accelClamp);
            body.AddForce(transform.forward * forwardAccel, ForceMode.Acceleration);

            if (speedLimit > 0f && planarSpeed > speedLimit)
            {
                var overspeed = planarSpeed - speedLimit;
                var brakeAccel = Mathf.Min(overspeed / Time.fixedDeltaTime, accelClamp);
                if (planarVelocity.sqrMagnitude > 0.0001f)
                {
                    body.AddForce(-planarVelocity.normalized * brakeAccel, ForceMode.Acceleration);
                }
            }
        }

        private void ApplyLateralDamping()
        {
            if (lateralDamping <= 0f)
            {
                return;
            }

            var localVelocity = transform.InverseTransformDirection(body.velocity);
            var lateralSpeed = localVelocity.x;
            if (Mathf.Abs(lateralSpeed) < 0.001f)
            {
                return;
            }

            var lateralAccel = -lateralSpeed * lateralDamping;
            body.AddForce(transform.right * lateralAccel, ForceMode.Acceleration);
        }

        private float GetSpeedLimit()
        {
            if (readSpeedLimitFromComfortManager && comfortManager != null)
            {
                return comfortManager.Current.SpeedLimit;
            }

            return fallbackSpeedLimit;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebug)
            {
                return;
            }

            var t = transform;
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
            Gizmos.DrawLine(t.position, t.position + t.forward * 2f);

            if (body != null)
            {
                Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.9f);
                var planarVelocity = Vector3.ProjectOnPlane(body.velocity, Vector3.up);
                Gizmos.DrawLine(t.position, t.position + planarVelocity);
            }

#if UNITY_EDITOR
            if (body != null)
            {
                var planarSpeed = Vector3.ProjectOnPlane(body.velocity, Vector3.up).magnitude;
                var speedLimit = GetSpeedLimit();
                UnityEditor.Handles.Label(
                    t.position + Vector3.up * 1.2f,
                    $"Boat\nSpeed: {planarSpeed:0.00} m/s\nLimit: {speedLimit:0.00}\nAccelClamp: {accelClamp:0.00}\nTurnClamp: {turnClamp:0.0}"
                );
            }
#endif
        }
    }
}
