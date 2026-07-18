using System;
using SiPVLib.Debugging;
using UnityEngine;

namespace SiPVLib.Event
{
    public static class EventExtensions
    {
        private static EventAutoCleanup GetCleanupFor(MonoBehaviour behaviour, bool createIfMissing = true)
        {
            var cleanups = behaviour.gameObject.GetComponents<EventAutoCleanup>();
            foreach (var cleanup in cleanups)
            {
                if (cleanup.Owner == behaviour)
                {
                    return cleanup;
                }
            }

            if (!createIfMissing)
            {
                return null;
            }

            var newCleanup = behaviour.gameObject.AddComponent<EventAutoCleanup>();
            newCleanup.hideFlags = HideFlags.HideAndDontSave;
            newCleanup.Initialize(behaviour);
            return newCleanup;
        }

        public static void ListenEvent(this MonoBehaviour behaviour, string eventKey, Action listener,
            bool autoCleanup = true)
        {
            if (!Application.isPlaying)
            {
                CustomLog.LogWarning("AddWithAutoCleanup should only be called during play mode");
                EventManager.Add(eventKey, listener);
                return;
            }

            var cleanup = GetCleanupFor(behaviour, autoCleanup);
            if (cleanup != null && cleanup.HasStringListener(eventKey, listener))
            {
                CustomLog.LogWarning($"Duplicate listener registration for event '{eventKey}'. Skipping.");
                return;
            }

            EventManager.Add(eventKey, listener);

            if (!autoCleanup) return;

            cleanup.AddStringListener(eventKey, listener);
        }

        public static void ListenEvent<T>(this MonoBehaviour behaviour, string eventKey, Action<T> listener,
            bool autoCleanup = true)
        {
            if (!Application.isPlaying)
            {
                CustomLog.LogWarning("AddWithAutoCleanup should only be called during play mode");
                EventManager.Add(eventKey, listener);
                return;
            }

            var cleanup = GetCleanupFor(behaviour, autoCleanup);
            if (cleanup != null && cleanup.HasGenericListener(eventKey, listener))
            {
                CustomLog.LogWarning($"Duplicate listener registration for event '{eventKey}'. Skipping.");
                return;
            }

            EventManager.Add(eventKey, listener);

            if (!autoCleanup) return;

            cleanup.AddGenericListener(eventKey, listener);
        }

        public static void ListenEvent<T>(this MonoBehaviour behaviour, Action<T> listener, bool autoCleanup = true)
        {
            if (!Application.isPlaying)
            {
                CustomLog.LogWarning("AddWithAutoCleanup should only be called during play mode");
                EventManager.Add(listener);
                return;
            }

            var cleanup = GetCleanupFor(behaviour, autoCleanup);
            if (cleanup != null && cleanup.HasTypeListener(listener))
            {
                CustomLog.LogWarning($"Duplicate listener registration for type '{typeof(T)}'. Skipping.");
                return;
            }

            EventManager.Add(listener);

            if (!autoCleanup) return;

            cleanup.AddTypeListener(listener);
        }

        public static void ListenEvent(this MonoBehaviour behaviour, string eventKey, Action<object[]> listener,
            bool autoCleanup = true)
        {
            if (!Application.isPlaying)
            {
                CustomLog.LogWarning("AddWithAutoCleanup should only be called during play mode");
                EventManager.Add(eventKey, listener);
                return;
            }

            var cleanup = GetCleanupFor(behaviour, autoCleanup);
            if (cleanup != null && cleanup.HasParamsListener(eventKey, listener))
            {
                CustomLog.LogWarning($"Duplicate listener registration for event '{eventKey}'. Skipping.");
                return;
            }

            EventManager.Add(eventKey, listener);

            if (!autoCleanup) return;

            cleanup.AddParamsListener(eventKey, listener);
        }

        public static void ListenEvent(this MonoBehaviour behaviour, string eventKey, string targetId, Action listener,
            bool autoCleanup = true)
        {
            if (!Application.isPlaying)
            {
                CustomLog.LogWarning("AddWithAutoCleanup should only be called during play mode");
                EventManager.Add(eventKey, targetId, listener);
                return;
            }

            var cleanup = GetCleanupFor(behaviour, autoCleanup);
            if (cleanup != null && cleanup.HasStringListener(eventKey, listener, targetId))
            {
                CustomLog.LogWarning($"Duplicate listener registration for event '{eventKey}' with target '{targetId}'. Skipping.");
                return;
            }

            EventManager.Add(eventKey, targetId, listener);

            if (!autoCleanup) return;

            cleanup.AddStringListener(eventKey, listener, targetId);
        }

        public static void ListenEvent<T>(this MonoBehaviour behaviour, string eventKey, string targetId, Action<T> listener,
            bool autoCleanup = true)
        {
            if (!Application.isPlaying)
            {
                CustomLog.LogWarning("AddWithAutoCleanup should only be called during play mode");
                EventManager.Add(eventKey, targetId, listener);
                return;
            }

            var cleanup = GetCleanupFor(behaviour, autoCleanup);
            if (cleanup != null && cleanup.HasGenericListener(eventKey, listener, targetId))
            {
                CustomLog.LogWarning($"Duplicate listener registration for event '{eventKey}' with target '{targetId}'. Skipping.");
                return;
            }

            EventManager.Add(eventKey, targetId, listener);

            if (!autoCleanup) return;

            cleanup.AddGenericListener(eventKey, listener, targetId);
        }

        public static void ListenEvent<T>(this MonoBehaviour behaviour, Action<T> listener, string targetId, bool autoCleanup = true)
        {
            if (!Application.isPlaying)
            {
                CustomLog.LogWarning("AddWithAutoCleanup should only be called during play mode");
                EventManager.Add(listener, targetId);
                return;
            }

            var cleanup = GetCleanupFor(behaviour, autoCleanup);
            if (cleanup != null && cleanup.HasTypeListener(listener, targetId))
            {
                CustomLog.LogWarning($"Duplicate listener registration for type '{typeof(T)}' with target '{targetId}'. Skipping.");
                return;
            }

            EventManager.Add(listener, targetId);

            if (!autoCleanup) return;

            cleanup.AddTypeListener(listener, targetId);
        }

        public static void ListenEvent(this MonoBehaviour behaviour, string eventKey, string targetId, Action<object[]> listener,
            bool autoCleanup = true)
        {
            if (!Application.isPlaying)
            {
                CustomLog.LogWarning("AddWithAutoCleanup should only be called during play mode");
                EventManager.Add(eventKey, targetId, listener);
                return;
            }

            var cleanup = GetCleanupFor(behaviour, autoCleanup);
            if (cleanup != null && cleanup.HasParamsListener(eventKey, listener, targetId))
            {
                CustomLog.LogWarning($"Duplicate listener registration for event '{eventKey}' with target '{targetId}'. Skipping.");
                return;
            }

            EventManager.Add(eventKey, targetId, listener);

            if (!autoCleanup) return;

            cleanup.AddParamsListener(eventKey, listener, targetId);
        }


        public static void InvokeEventDelayed(this MonoBehaviour behaviour, string eventKey, float delay)
        {
            behaviour.StartCoroutine(InvokeDelayedCoroutine(eventKey, delay));
        }

        public static void InvokeEventDelayed<T>(this MonoBehaviour behaviour, string eventKey, T parameter,
            float delay)
        {
            behaviour.StartCoroutine(InvokeDelayedCoroutine(eventKey, parameter, delay));
        }

        private static System.Collections.IEnumerator InvokeDelayedCoroutine(string eventKey, float delay)
        {
            yield return new WaitForSeconds(delay);
            EventManager.Invoke(eventKey);
        }

        private static System.Collections.IEnumerator InvokeDelayedCoroutine<T>(string eventKey, T parameter,
            float delay)
        {
            yield return new WaitForSeconds(delay);
            EventManager.Invoke(eventKey, parameter);
        }

        public static void RemoveAllEventListeners(this MonoBehaviour behaviour)
        {
            var cleanup = GetCleanupFor(behaviour, false);
            if (cleanup != null)
            {
                cleanup.RemoveAllListeners();
            }
        }

        public static void RemoveEventListener(this MonoBehaviour behaviour, string eventKey, Action listener)
        {
            EventManager.Remove(eventKey, listener);
            var cleanup = GetCleanupFor(behaviour, false);
            if (cleanup != null)
            {
                cleanup.RemoveStringListener(eventKey, listener);
            }
        }

        public static void RemoveEventListener<T>(this MonoBehaviour behaviour, string eventKey, Action<T> listener)
        {
            EventManager.Remove(eventKey, listener);

            var cleanup = GetCleanupFor(behaviour, false);
            if (cleanup != null)
            {
                cleanup.RemoveGenericListener(eventKey, listener);
            }
        }

        public static void RemoveEventListener<T>(this MonoBehaviour behaviour, Action<T> listener)
        {
            EventManager.Remove(listener);

            var cleanup = GetCleanupFor(behaviour, false);
            if (cleanup != null)
            {
                cleanup.RemoveTypeListener(listener);
            }
        }

        public static void RemoveEventListener(this MonoBehaviour behaviour, string eventKey, Action<object[]> listener)
        {
            EventManager.Remove(eventKey, listener);

            var cleanup = GetCleanupFor(behaviour, false);
            if (cleanup != null)
            {
                cleanup.RemoveParamsListener(eventKey, listener);
            }
        }

        public static void RemoveEventListener(this MonoBehaviour behaviour, string eventKey, string targetId, Action listener)
        {
            EventManager.Remove(eventKey, targetId, listener);
            var cleanup = GetCleanupFor(behaviour, false);
            if (cleanup != null)
            {
                cleanup.RemoveStringListener(eventKey, listener, targetId);
            }
        }

        public static void RemoveEventListener<T>(this MonoBehaviour behaviour, string eventKey, string targetId, Action<T> listener)
        {
            EventManager.Remove(eventKey, targetId, listener);

            var cleanup = GetCleanupFor(behaviour, false);
            if (cleanup != null)
            {
                cleanup.RemoveGenericListener(eventKey, listener, targetId);
            }
        }

        public static void RemoveEventListener<T>(this MonoBehaviour behaviour, Action<T> listener, string targetId)
        {
            EventManager.Remove(listener, targetId);

            var cleanup = GetCleanupFor(behaviour, false);
            if (cleanup != null)
            {
                cleanup.RemoveTypeListener(listener, targetId);
            }
        }

        public static void RemoveEventListener(this MonoBehaviour behaviour, string eventKey, string targetId, Action<object[]> listener)
        {
            EventManager.Remove(eventKey, targetId, listener);

            var cleanup = GetCleanupFor(behaviour, false);
            if (cleanup != null)
            {
                cleanup.RemoveParamsListener(eventKey, listener, targetId);
            }
        }
    }
}
