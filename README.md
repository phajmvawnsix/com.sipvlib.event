# com.sipvlib.event

Part of [SiPVLib](https://github.com/phajmvawnsix/SiPVLib). A static, string/type-keyed pub/sub event bus for decoupled messaging between systems, with `MonoBehaviour` extension methods that provide automatic listener cleanup on destroy.

## Install

Add to your project's `Packages/manifest.json`:

```json
"com.sipvlib.event": "https://github.com/phajmvawnsix/com.sipvlib.event.git",
"com.sipvlib.debugging": "https://github.com/phajmvawnsix/com.sipvlib.debugging.git"
```

UPM does not automatically resolve nested git dependencies — you must add `com.sipvlib.debugging` yourself alongside this package, as shown above.

## Documentation
- [Usage guide](USAGE.md) — original module documentation carried over from the SiPVLib monolith
