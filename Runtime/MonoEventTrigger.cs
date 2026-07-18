using UnityEngine;

namespace SiPVLib.Event
{
    internal sealed class MonoEventTrigger : MonoBehaviour
    {
        internal event System.Action OnUpdateEvent;
        internal event System.Action OnFixedUpdateEvent;
        internal event System.Action OnLateUpdateEvent;

        private void Update()       => OnUpdateEvent?.Invoke();
        private void FixedUpdate()  => OnFixedUpdateEvent?.Invoke();
        private void LateUpdate()   => OnLateUpdateEvent?.Invoke();
    }
}