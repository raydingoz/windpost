using System;
using UnityEngine;

namespace Windpost.Episode
{
    [RequireComponent(typeof(Collider))]
    public sealed class SleepPoint : MonoBehaviour
    {
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private bool triggerOnce = true;

        private bool _triggered;

        public event Action SleepTriggered;

        private void Reset()
        {
            var collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_triggered && triggerOnce)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(playerTag) && !other.CompareTag(playerTag))
            {
                return;
            }

            _triggered = true;
            SleepTriggered?.Invoke();
        }
    }
}

