using System;
using System.Collections.Generic;
using SiPVLib.Debugging;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SiPVLib.Event
{
    public static class EventManager
    {
        public const string EVENT_MONO_UPDATE      = "mono.update";
        public const string EVENT_MONO_FIXED_UPDATE = "mono.fixed_update";
        public const string EVENT_MONO_LATE_UPDATE  = "mono.late_update";

        private static readonly Dictionary<string, List<Action<object[]>>> StringParamsEvents = new();
        private static readonly Dictionary<string, List<Action>>           StringOnlyEvents   = new();
        private static readonly Dictionary<string, List<object>>           StringGenericEvents = new();
        private static readonly Dictionary<Type,   List<object>>           GenericTypeEvents   = new();

        private static readonly Dictionary<string, Dictionary<string, List<Action<object[]>>>> StringParamsTargetedEvents  = new();
        private static readonly Dictionary<string, Dictionary<string, List<Action>>>           StringOnlyTargetedEvents    = new();
        private static readonly Dictionary<string, Dictionary<string, List<object>>>           StringGenericTargetedEvents = new();
        private static readonly Dictionary<Type,   Dictionary<string, List<object>>>           GenericTypeTargetedEvents   = new();

        private static readonly Stack<List<Action<object[]>>> PoolParams  = new();
        private static readonly Stack<List<Action>>           PoolActions = new();
        private static readonly Stack<List<object>>           PoolObjects = new();

        private static readonly object LockObject = new();
        private static MonoEventTrigger _monoEventTrigger;

        // Init Mono Event Trigger
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            // Domain reload can be disabled in Unity (Enter Play Mode Options), so static state
            // must be cleared explicitly on each play session start to avoid stale listeners
            // referencing destroyed objects from the previous session.
            StringParamsEvents.Clear();
            StringOnlyEvents.Clear();
            StringGenericEvents.Clear();
            GenericTypeEvents.Clear();

            StringParamsTargetedEvents.Clear();
            StringOnlyTargetedEvents.Clear();
            StringGenericTargetedEvents.Clear();
            GenericTypeTargetedEvents.Clear();

            PoolParams.Clear();
            PoolActions.Clear();
            PoolObjects.Clear();

            _monoEventTrigger = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitMonoEventTrigger()
        {
            if (_monoEventTrigger != null) return;

            var go = new GameObject("_MonoEventTrigger");
            Object.DontDestroyOnLoad(go);
            _monoEventTrigger = go.AddComponent<MonoEventTrigger>();

            // Register Mono Events
            _monoEventTrigger.OnUpdateEvent      += () => Invoke(EVENT_MONO_UPDATE);
            _monoEventTrigger.OnFixedUpdateEvent += () => Invoke(EVENT_MONO_FIXED_UPDATE);
            _monoEventTrigger.OnLateUpdateEvent  += () => Invoke(EVENT_MONO_LATE_UPDATE);
        }

        private static List<Action<object[]>> GetParamsListFromPool()
            => PoolParams.Count > 0 ? PoolParams.Pop() : new List<Action<object[]>>();

        private static void ReturnParamsListToPool(List<Action<object[]>> list)
        {
            list.Clear();
            PoolParams.Push(list);
        }

        private static List<Action> GetActionListFromPool()
            => PoolActions.Count > 0 ? PoolActions.Pop() : new List<Action>();

        private static void ReturnActionListToPool(List<Action> list)
        {
            list.Clear();
            PoolActions.Push(list);
        }

        private static List<object> GetObjectListFromPool()
            => PoolObjects.Count > 0 ? PoolObjects.Pop() : new List<object>();

        private static void ReturnObjectListToPool(List<object> list)
        {
            list.Clear();
            PoolObjects.Push(list);
        }

        public static void Add(string eventKey, Action<object[]> listener)
        {
            if (string.IsNullOrEmpty(eventKey) || listener == null) return;

            lock (LockObject)
            {
                if (!StringParamsEvents.TryGetValue(eventKey, out var listeners))
                {
                    listeners = GetParamsListFromPool();
                    StringParamsEvents[eventKey] = listeners;
                }
                listeners.Add(listener);
            }
        }

        public static void Remove(string eventKey, Action<object[]> listener)
        {
            if (string.IsNullOrEmpty(eventKey) || listener == null) return;

            lock (LockObject)
            {
                if (StringParamsEvents.TryGetValue(eventKey, out var listeners))
                {
                    listeners.Remove(listener);
                    if (listeners.Count == 0)
                    {
                        StringParamsEvents.Remove(eventKey);
                        ReturnParamsListToPool(listeners);
                    }
                }
            }
        }

        public static void Invoke(string eventKey, params object[] parameters)
        {
            if (string.IsNullOrEmpty(eventKey)) return;

            List<Action<object[]>> listenersCopy;
            lock (LockObject)
            {
                if (!StringParamsEvents.TryGetValue(eventKey, out var listeners))
                    return;
                listenersCopy = new List<Action<object[]>>(listeners);
            }

            foreach (var action in listenersCopy)
            {
                try   { action?.Invoke(parameters); }
                catch (Exception ex) { CustomLog.LogException(ex); }
            }
        }

        public static void Add(string eventKey, string targetId, Action<object[]> listener)
        {
            if (string.IsNullOrEmpty(eventKey) || string.IsNullOrEmpty(targetId) || listener == null) return;

            lock (LockObject)
            {
                if (!StringParamsTargetedEvents.TryGetValue(eventKey, out var targets))
                {
                    targets = new Dictionary<string, List<Action<object[]>>>();
                    StringParamsTargetedEvents[eventKey] = targets;
                }
                if (!targets.TryGetValue(targetId, out var listeners))
                {
                    listeners = GetParamsListFromPool();
                    targets[targetId] = listeners;
                }
                listeners.Add(listener);
            }
        }

        public static void Remove(string eventKey, string targetId, Action<object[]> listener)
        {
            if (string.IsNullOrEmpty(eventKey) || string.IsNullOrEmpty(targetId) || listener == null) return;

            lock (LockObject)
            {
                if (StringParamsTargetedEvents.TryGetValue(eventKey, out var targets) &&
                    targets.TryGetValue(targetId, out var listeners))
                {
                    listeners.Remove(listener);
                    if (listeners.Count == 0)
                    {
                        targets.Remove(targetId);
                        ReturnParamsListToPool(listeners);
                        if (targets.Count == 0)
                            StringParamsTargetedEvents.Remove(eventKey);
                    }
                }
            }
        }

        public static void Invoke(string eventKey, string targetId, params object[] parameters)
        {
            if (string.IsNullOrEmpty(eventKey) || string.IsNullOrEmpty(targetId)) return;

            List<Action<object[]>> listenersCopy;
            lock (LockObject)
            {
                if (!StringParamsTargetedEvents.TryGetValue(eventKey, out var targets) ||
                    !targets.TryGetValue(targetId, out var listeners))
                    return;
                listenersCopy = new List<Action<object[]>>(listeners);
            }

            foreach (var action in listenersCopy)
            {
                try   { action?.Invoke(parameters); }
                catch (Exception ex) { CustomLog.LogException(ex); }
            }
        }

        public static void Add(string eventKey, Action listener)
        {
            if (string.IsNullOrEmpty(eventKey) || listener == null) return;

            lock (LockObject)
            {
                if (!StringOnlyEvents.TryGetValue(eventKey, out var listeners))
                {
                    listeners = GetActionListFromPool();
                    StringOnlyEvents[eventKey] = listeners;
                }
                listeners.Add(listener);
            }
        }

        public static void Remove(string eventKey, Action listener)
        {
            if (string.IsNullOrEmpty(eventKey) || listener == null) return;

            lock (LockObject)
            {
                if (StringOnlyEvents.TryGetValue(eventKey, out var listeners))
                {
                    listeners.Remove(listener);
                    if (listeners.Count == 0)
                    {
                        StringOnlyEvents.Remove(eventKey);
                        ReturnActionListToPool(listeners);
                    }
                }
            }
        }

        public static void Invoke(string eventKey)
        {
            if (string.IsNullOrEmpty(eventKey)) return;

            List<Action> listenersCopy;
            lock (LockObject)
            {
                if (!StringOnlyEvents.TryGetValue(eventKey, out var listeners))
                    return;
                listenersCopy = new List<Action>(listeners);
            }

            foreach (var action in listenersCopy)
            {
                try   { action?.Invoke(); }
                catch (Exception ex) { CustomLog.LogException(ex); }
            }
        }

        public static void Add(string eventKey, string targetId, Action listener)
        {
            if (string.IsNullOrEmpty(eventKey) || string.IsNullOrEmpty(targetId) || listener == null) return;

            lock (LockObject)
            {
                if (!StringOnlyTargetedEvents.TryGetValue(eventKey, out var targets))
                {
                    targets = new Dictionary<string, List<Action>>();
                    StringOnlyTargetedEvents[eventKey] = targets;
                }
                if (!targets.TryGetValue(targetId, out var listeners))
                {
                    listeners = GetActionListFromPool();
                    targets[targetId] = listeners;
                }
                listeners.Add(listener);
            }
        }

        public static void Remove(string eventKey, string targetId, Action listener)
        {
            if (string.IsNullOrEmpty(eventKey) || string.IsNullOrEmpty(targetId) || listener == null) return;

            lock (LockObject)
            {
                if (StringOnlyTargetedEvents.TryGetValue(eventKey, out var targets) &&
                    targets.TryGetValue(targetId, out var listeners))
                {
                    listeners.Remove(listener);
                    if (listeners.Count == 0)
                    {
                        targets.Remove(targetId);
                        ReturnActionListToPool(listeners);
                        if (targets.Count == 0)
                            StringOnlyTargetedEvents.Remove(eventKey);
                    }
                }
            }
        }

        public static void Invoke(string eventKey, string targetId)
        {
            if (string.IsNullOrEmpty(eventKey) || string.IsNullOrEmpty(targetId)) return;

            List<Action> listenersCopy;
            lock (LockObject)
            {
                if (!StringOnlyTargetedEvents.TryGetValue(eventKey, out var targets) ||
                    !targets.TryGetValue(targetId, out var listeners))
                    return;
                listenersCopy = new List<Action>(listeners);
            }

            foreach (var action in listenersCopy)
            {
                try   { action?.Invoke(); }
                catch (Exception ex) { CustomLog.LogException(ex); }
            }
        }

        public static void Add<T>(string eventKey, Action<T> listener)
        {
            if (string.IsNullOrEmpty(eventKey) || listener == null) return;

            lock (LockObject)
            {
                if (!StringGenericEvents.TryGetValue(eventKey, out var listeners))
                {
                    listeners = GetObjectListFromPool();
                    StringGenericEvents[eventKey] = listeners;
                }
                listeners.Add(listener);
            }
        }

        public static void Remove<T>(string eventKey, Action<T> listener)
        {
            if (string.IsNullOrEmpty(eventKey) || listener == null) return;

            lock (LockObject)
            {
                if (StringGenericEvents.TryGetValue(eventKey, out var listeners))
                {
                    listeners.Remove(listener);
                    if (listeners.Count == 0)
                    {
                        StringGenericEvents.Remove(eventKey);
                        ReturnObjectListToPool(listeners);
                    }
                }
            }
        }

        public static void Invoke<T>(string eventKey, T parameter)
        {
            if (string.IsNullOrEmpty(eventKey)) return;

            List<object> listenersCopy;
            lock (LockObject)
            {
                if (!StringGenericEvents.TryGetValue(eventKey, out var listeners))
                    return;
                listenersCopy = new List<object>(listeners);
            }

            for (var i = 0; i < listenersCopy.Count; i++)
            {
                if (listenersCopy[i] is Action<T> typedListener)
                {
                    try   { typedListener(parameter); }
                    catch (Exception ex) { CustomLog.LogException(ex); }
                }
            }
        }

        public static void Add<T>(string eventKey, string targetId, Action<T> listener)
        {
            if (string.IsNullOrEmpty(eventKey) || string.IsNullOrEmpty(targetId) || listener == null) return;

            lock (LockObject)
            {
                if (!StringGenericTargetedEvents.TryGetValue(eventKey, out var targets))
                {
                    targets = new Dictionary<string, List<object>>();
                    StringGenericTargetedEvents[eventKey] = targets;
                }
                if (!targets.TryGetValue(targetId, out var listeners))
                {
                    listeners = GetObjectListFromPool();
                    targets[targetId] = listeners;
                }
                listeners.Add(listener);
            }
        }

        public static void Remove<T>(string eventKey, string targetId, Action<T> listener)
        {
            if (string.IsNullOrEmpty(eventKey) || string.IsNullOrEmpty(targetId) || listener == null) return;

            lock (LockObject)
            {
                if (StringGenericTargetedEvents.TryGetValue(eventKey, out var targets) &&
                    targets.TryGetValue(targetId, out var listeners))
                {
                    listeners.Remove(listener);
                    if (listeners.Count == 0)
                    {
                        targets.Remove(targetId);
                        ReturnObjectListToPool(listeners);
                        if (targets.Count == 0)
                            StringGenericTargetedEvents.Remove(eventKey);
                    }
                }
            }
        }

        public static void Invoke<T>(string eventKey, string targetId, T parameter)
        {
            if (string.IsNullOrEmpty(eventKey) || string.IsNullOrEmpty(targetId)) return;

            List<object> listenersCopy;
            lock (LockObject)
            {
                if (!StringGenericTargetedEvents.TryGetValue(eventKey, out var targets) ||
                    !targets.TryGetValue(targetId, out var listeners))
                    return;
                listenersCopy = new List<object>(listeners);
            }

            for (var i = 0; i < listenersCopy.Count; i++)
            {
                if (listenersCopy[i] is Action<T> typedListener)
                {
                    try   { typedListener(parameter); }
                    catch (Exception ex) { CustomLog.LogException(ex); }
                }
            }
        }

        public static void Add<T>(Action<T> listener)
        {
            if (listener == null) return;

            var type = typeof(T);
            lock (LockObject)
            {
                if (!GenericTypeEvents.TryGetValue(type, out var listeners))
                {
                    listeners = GetObjectListFromPool();
                    GenericTypeEvents[type] = listeners;
                }
                listeners.Add(listener);
            }
        }

        public static void Remove<T>(Action<T> listener)
        {
            if (listener == null) return;

            var type = typeof(T);
            lock (LockObject)
            {
                if (GenericTypeEvents.TryGetValue(type, out var listeners))
                {
                    listeners.Remove(listener);
                    if (listeners.Count == 0)
                    {
                        GenericTypeEvents.Remove(type);
                        ReturnObjectListToPool(listeners);
                    }
                }
            }
        }

        public static void Invoke<T>(T parameter)
        {
            var type = typeof(T);
            List<object> listenersCopy;
            lock (LockObject)
            {
                if (!GenericTypeEvents.TryGetValue(type, out var listeners))
                    return;
                listenersCopy = new List<object>(listeners);
            }

            for (var i = 0; i < listenersCopy.Count; i++)
            {
                if (listenersCopy[i] is Action<T> typedListener)
                {
                    try   { typedListener.Invoke(parameter); }
                    catch (Exception ex) { CustomLog.LogException(ex); }
                }
            }
        }

        public static void Add<T>(Action<T> listener, string targetId)
        {
            if (string.IsNullOrEmpty(targetId) || listener == null) return;

            var type = typeof(T);
            lock (LockObject)
            {
                if (!GenericTypeTargetedEvents.TryGetValue(type, out var targets))
                {
                    targets = new Dictionary<string, List<object>>();
                    GenericTypeTargetedEvents[type] = targets;
                }
                if (!targets.TryGetValue(targetId, out var listeners))
                {
                    listeners = GetObjectListFromPool();
                    targets[targetId] = listeners;
                }
                listeners.Add(listener);
            }
        }

        public static void Remove<T>(Action<T> listener, string targetId)
        {
            if (string.IsNullOrEmpty(targetId) || listener == null) return;

            var type = typeof(T);
            lock (LockObject)
            {
                if (GenericTypeTargetedEvents.TryGetValue(type, out var targets) &&
                    targets.TryGetValue(targetId, out var listeners))
                {
                    listeners.Remove(listener);
                    if (listeners.Count == 0)
                    {
                        targets.Remove(targetId);
                        ReturnObjectListToPool(listeners);
                        if (targets.Count == 0)
                            GenericTypeTargetedEvents.Remove(type);
                    }
                }
            }
        }

        public static void Invoke<T>(T parameter, string targetId)
        {
            if (string.IsNullOrEmpty(targetId)) return;

            var type = typeof(T);
            List<object> listenersCopy;
            lock (LockObject)
            {
                if (!GenericTypeTargetedEvents.TryGetValue(type, out var targets) ||
                    !targets.TryGetValue(targetId, out var listeners))
                    return;
                listenersCopy = new List<object>(listeners);
            }

            for (var i = 0; i < listenersCopy.Count; i++)
            {
                if (listenersCopy[i] is Action<T> typedListener)
                {
                    try   { typedListener.Invoke(parameter); }
                    catch (Exception ex) { CustomLog.LogException(ex); }
                }
            }
        }

        public static bool HasListeners(string eventKey)
        {
            if (string.IsNullOrEmpty(eventKey)) return false;

            lock (LockObject)
            {
                return StringOnlyEvents.ContainsKey(eventKey)
                    || StringParamsEvents.ContainsKey(eventKey)
                    || StringGenericEvents.ContainsKey(eventKey);
            }
        }

    }
}
