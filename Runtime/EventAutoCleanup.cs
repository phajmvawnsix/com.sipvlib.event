using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SiPVLib.Event
{
    public sealed class EventAutoCleanup : MonoBehaviour
    {
        private class StringListenerInfo
        {
            public readonly string key;
            public readonly string targetId;
            public readonly Action listener;

            public StringListenerInfo(string key, Action action, string targetId = null)
            {
                this.key = key;
                listener = action;
                this.targetId = targetId;
            }
        }

        private class ParamsListenerInfo
        {
            public readonly string eventKey;
            public readonly string targetId;
            public readonly Action<object[]> listener;

            public ParamsListenerInfo(string key, Action<object[]> action, string targetId = null)
            {
                eventKey = key;
                listener = action;
                this.targetId = targetId;
            }
        }

        private class GenericListenerInfo
        {
            public readonly string eventKey;
            public readonly string targetId;
            public readonly object listener;
            public readonly Type listenerType;

            public GenericListenerInfo(string key, object action, Type type, string targetId = null)
            {
                eventKey = key;
                listener = action;
                listenerType = type;
                this.targetId = targetId;
            }
        }

        private class TypeListenerInfo
        {
            public readonly string targetId;
            public readonly object listener;
            public readonly Type listenerType;

            public TypeListenerInfo(object action, Type type, string targetId = null)
            {
                listener = action;
                listenerType = type;
                this.targetId = targetId;
            }
        }

        private readonly List<StringListenerInfo> _stringListeners = new();
        private readonly List<ParamsListenerInfo> _paramsListeners = new();
        private readonly List<GenericListenerInfo> _genericListeners = new();
        private readonly List<TypeListenerInfo> _typeListeners = new();

        // ── Reflection Cache (Private Static) ────────────────────────────

        private static readonly MethodInfo RemoveGenericMethod;
        private static readonly MethodInfo RemoveGenericTargetedMethod;
        private static readonly MethodInfo RemoveTypeMethod;
        private static readonly MethodInfo RemoveTypeTargetedMethod;

        private static readonly Dictionary<Type, MethodInfo> RemoveGenericCache = new();
        private static readonly Dictionary<Type, MethodInfo> RemoveGenericTargetedCache = new();
        private static readonly Dictionary<Type, MethodInfo> RemoveTypeCache = new();
        private static readonly Dictionary<Type, MethodInfo> RemoveTypeTargetedCache = new();

        static EventAutoCleanup()
        {
            foreach (var method in typeof(EventManager).GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (method.Name != "Remove" || !method.IsGenericMethodDefinition) continue;

                var parameters = method.GetParameters();
                switch (parameters.Length)
                {
                    case 1:
                        RemoveTypeMethod = method;
                        break;
                    case 2 when parameters[0].ParameterType == typeof(string):
                        RemoveGenericMethod = method;
                        break;
                    case 2 when parameters[1].ParameterType == typeof(string):
                        RemoveTypeTargetedMethod = method;
                        break;
                    case 3 when parameters[0].ParameterType == typeof(string) && parameters[1].ParameterType == typeof(string):
                        RemoveGenericTargetedMethod = method;
                        break;
                }
            }
        }

        private static MethodInfo GetCachedGenericMethod(MethodInfo definition, Dictionary<Type, MethodInfo> cache, Type listenerType)
        {
            if (!cache.TryGetValue(listenerType, out var closed))
            {
                closed = definition.MakeGenericMethod(listenerType);
                cache[listenerType] = closed;
            }
            return closed;
        }

        // ── Unity Lifecycle ───────────────────────────────────────────────

        private void OnDestroy()
        {
            RemoveAllListeners();
        }

        // ── Public API ────────────────────────────────────────────────────

        public MonoBehaviour Owner { get; private set; }

        public void Initialize(MonoBehaviour owner)
        {
            Owner = owner;
        }

        public bool HasStringListener(string eventKey, Action listener, string targetId = null)
        {
            return _stringListeners.Exists(info => info.key == eventKey && info.listener == listener && info.targetId == targetId);
        }

        public bool HasParamsListener(string eventKey, Action<object[]> listener, string targetId = null)
        {
            return _paramsListeners.Exists(info => info.eventKey == eventKey && info.listener == listener && info.targetId == targetId);
        }

        public bool HasGenericListener<T>(string eventKey, Action<T> listener, string targetId = null)
        {
            return _genericListeners.Exists(info => info.eventKey == eventKey && info.listener.Equals(listener) && info.targetId == targetId);
        }

        public bool HasTypeListener<T>(Action<T> listener, string targetId = null)
        {
            return _typeListeners.Exists(info => info.listener.Equals(listener) && info.targetId == targetId);
        }

        public void AddStringListener(string eventKey, Action listener, string targetId = null)
        {
            _stringListeners.Add(new StringListenerInfo(eventKey, listener, targetId));
        }

        public void AddParamsListener(string eventKey, Action<object[]> listener, string targetId = null)
        {
            _paramsListeners.Add(new ParamsListenerInfo(eventKey, listener, targetId));
        }

        public void AddGenericListener<T>(string eventKey, Action<T> listener, string targetId = null)
        {
            _genericListeners.Add(new GenericListenerInfo(eventKey, listener, typeof(T), targetId));
        }

        public void AddTypeListener<T>(Action<T> listener, string targetId = null)
        {
            _typeListeners.Add(new TypeListenerInfo(listener, typeof(T), targetId));
        }

        public void RemoveStringListener(string eventKey, Action listener, string targetId = null)
        {
            _stringListeners.RemoveAll(info => info.key == eventKey && info.listener == listener && info.targetId == targetId);
        }

        public void RemoveParamsListener(string eventKey, Action<object[]> listener, string targetId = null)
        {
            _paramsListeners.RemoveAll(info => info.eventKey == eventKey && info.listener == listener && info.targetId == targetId);
        }

        public void RemoveGenericListener<T>(string eventKey, Action<T> listener, string targetId = null)
        {
            _genericListeners.RemoveAll(info => info.eventKey == eventKey && info.listener.Equals(listener) && info.targetId == targetId);
        }

        public void RemoveTypeListener<T>(Action<T> listener, string targetId = null)
        {
            _typeListeners.RemoveAll(info => info.listener.Equals(listener) && info.targetId == targetId);
        }

        public void RemoveAllListeners()
        {
            foreach (var info in _stringListeners)
            {
                if (string.IsNullOrEmpty(info.targetId))
                    EventManager.Remove(info.key, info.listener);
                else
                    EventManager.Remove(info.key, info.targetId, info.listener);
            }

            foreach (var info in _paramsListeners)
            {
                if (string.IsNullOrEmpty(info.targetId))
                    EventManager.Remove(info.eventKey, info.listener);
                else
                    EventManager.Remove(info.eventKey, info.targetId, info.listener);
            }

            foreach (var info in _genericListeners)
            {
                if (string.IsNullOrEmpty(info.targetId))
                {
                    var method = GetCachedGenericMethod(RemoveGenericMethod, RemoveGenericCache, info.listenerType);
                    method.Invoke(null, new[] { info.eventKey, info.listener });
                }
                else
                {
                    var method = GetCachedGenericMethod(RemoveGenericTargetedMethod, RemoveGenericTargetedCache, info.listenerType);
                    method.Invoke(null, new[] { info.eventKey, info.targetId, info.listener });
                }
            }

            foreach (var info in _typeListeners)
            {
                if (string.IsNullOrEmpty(info.targetId))
                {
                    var method = GetCachedGenericMethod(RemoveTypeMethod, RemoveTypeCache, info.listenerType);
                    method.Invoke(null, new[] { info.listener });
                }
                else
                {
                    var method = GetCachedGenericMethod(RemoveTypeTargetedMethod, RemoveTypeTargetedCache, info.listenerType);
                    method.Invoke(null, new[] { info.listener, info.targetId });
                }
            }

            _stringListeners.Clear();
            _paramsListeners.Clear();
            _genericListeners.Clear();
            _typeListeners.Clear();
        }
    }
}
