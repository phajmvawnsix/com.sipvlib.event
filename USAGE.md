# Event Module

Static, string/type-keyed event bus for decoupled pub/sub messaging between systems that shouldn't reference each other directly.

## Structure

- **`EventManager`** — static event bus. Holds all listener registries and dispatches invocations. Thread-safe (`lock`), pools internal `List<>` instances to cut GC allocs. Auto-resets on each play session start (survives disabled domain reload) and drives `mono.update` / `mono.fixed_update` / `mono.late_update` global ticks via an internal `DontDestroyOnLoad` `MonoEventTrigger`.
- **`MonoEventTrigger`** — internal `MonoBehaviour` singleton that forwards Unity's `Update`/`FixedUpdate`/`LateUpdate` into `EventManager` events. Not used directly.
- **`EventExtensions`** — `MonoBehaviour` extension methods (`ListenEvent`, `RemoveEventListener`, `InvokeEventDelayed`, ...) that wrap `EventManager` and add optional auto-cleanup.
- **`EventAutoCleanup`** — hidden component (`HideAndDontSave`) auto-attached to a `GameObject` when a `MonoBehaviour` registers a listener with `autoCleanup: true`. Tracks that behaviour's registrations and removes them all in `OnDestroy`, preventing dangling listeners on destroyed objects.

## Listener kinds

Four independent registries, each usable with or without a `targetId` filter (targeted events only fire listeners registered for that specific `targetId`):

| Kind | Key | Signature | Use for |
|---|---|---|---|
| String | `string eventKey` | `Action` | Simple signals, no payload |
| String + params | `string eventKey` | `Action<object[]>` | Signals with loosely-typed payload |
| String + generic | `string eventKey` | `Action<T>` | Signals with one strongly-typed payload |
| Type | `typeof(T)` | `Action<T>` | "Broadcast this struct/class to whoever cares about it", no string key needed |

## Usage

### Direct via `EventManager` (no auto-cleanup, manual lifecycle)

```csharp
EventManager.Add("player.died", OnPlayerDied);
EventManager.Invoke("player.died");
EventManager.Remove("player.died", OnPlayerDied);

EventManager.Add<int>("score.changed", OnScoreChanged);
EventManager.Invoke("score.changed", 100);

EventManager.Add<DamageEvent>(OnDamage); // type-keyed, no string
EventManager.Invoke(new DamageEvent { Amount = 10 });
```

### Via `MonoBehaviour` extensions (recommended — auto-cleanup on destroy)

```csharp
public class Player : MonoBehaviour
{
    private void OnEnable()
    {
        this.ListenEvent("player.died", OnPlayerDied);          // auto-removed in OnDestroy
        this.ListenEvent<int>("score.changed", OnScoreChanged);
        this.ListenEvent<DamageEvent>(OnDamage);

        this.ListenEvent("quest.completed", questId, OnQuestDone); // targeted
    }

    private void OnPlayerDied() { }
    private void OnScoreChanged(int score) { }
    private void OnDamage(DamageEvent evt) { }
    private void OnQuestDone() { }
}
```

Pass `autoCleanup: false` to skip the auto-cleanup component and manage removal manually:

```csharp
this.ListenEvent("player.died", OnPlayerDied, autoCleanup: false);
...
this.RemoveEventListener("player.died", OnPlayerDied);
```

Manual removal helpers mirror every `ListenEvent` overload: `RemoveEventListener(...)`, or `RemoveAllEventListeners()` to strip everything a behaviour registered.

### Delayed invocation

```csharp
this.InvokeEventDelayed("player.died", delay: 2f);
this.InvokeEventDelayed("score.changed", 100, delay: 1f);
```

### Built-in mono tick events

```csharp
EventManager.Add(EventManager.EventMonoUpdate, OnEveryFrame);
```

## Notes

- Listeners are invoked from a snapshot copy of the list, so adding/removing listeners from inside a callback is safe.
- Exceptions inside a listener are caught and logged (`CustomLog.LogException`) — one bad listener never breaks dispatch for the rest.
- Duplicate `ListenEvent` registration (same key + listener + targetId) logs a warning and is skipped when using an `EventAutoCleanup`-tracked behaviour.
- Not for use outside `MonoBehaviour` lifecycle-bound code without care — `EventManager` calls have no owner tracking; only the extension methods provide auto-cleanup.
