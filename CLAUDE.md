# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Package Overview

`com.calluna.ui` (v1.3.2) — A Unity UPM package providing reactive UI MonoBehaviours built on top of Calluna's Observable/DI systems. Targets Unity 6000.33 LTS.

**Dependencies:**
- `com.calluna.core` — provides `Observable<T>`, `ReadonlyObservable<T>`, `CoroutineHelper`, `ScriptableObjectId`, `ObservableListChangeDetector<T>`
- `com.calluna.di` — provides `Injectable`, `Initializable`, `Cleanable` lifecycle interfaces, `Resolver`, `Binder`, `MonoInstaller`
- TextMeshPro — `TextMeshProUGUI`, `TMP_InputField`, `TMP_Dropdown`
- UnityEngine.UI — `Slider`, `Image`, `Button`, `Toggle`, `Graphic`, `RectTransform`

## Running Tests

Tests live in `Tests~/` (tilde suffix = disabled in UPM by default). Run via Unity Editor: **Window > General > Test Runner**. There are currently no custom test runner scripts.

## Architecture

### Core Lifecycle Pattern

Every UI component follows the Calluna DI lifecycle — all three interfaces are implemented together:

```csharp
public void Inject(Resolver resolver)   // Resolve dependencies from DI container
public void Initialize()                // Subscribe to Observable.OnChanged, apply initial state
public void Clean()                     // Unsubscribe — mirror of Initialize()
```

The DI container calls these in order at scene startup. `Clean()` must exactly undo `Initialize()` to prevent leaks.

### Observable Binding Rules

- **Display components** resolve `ReadonlyObservable<T>` — they only read values.
- **Input components** resolve `Observable<T>` — they read and write (two-way binding).
- The same Observable instance is shared across the scene via DI; components don't own their data.

### Subsystems

**MutableValueDisplay** (`Runtime/MutableValueDisplay/`)  
`ObservableValueTextDisplay<TValue>` → concrete types `FloatTextDisplay`, `IntTextDisplay`, `StringTextDisplay`, `DoubleTextDisplay`, `LongTextDisplay`. All render to `TextMeshProUGUI` using a format string.

**BasicInput** (`Runtime/BasicInput/`)  
`BasicInput<TValue>` is the generic base. Two sub-hierarchies:
- `TextInput<TValue>` → `FloatInput`, `IntInput`, `StringInput` (backed by `TMP_InputField`)
- `SliderInput<TValue>` → `FloatSlider`, `IntSlider` (backed by `UnityEngine.UI.Slider`)
- `ToggleInput` (backed by `Toggle`)
- `BasicDropdown` (backed by `TMP_Dropdown`) — uses `ObservableListChangeDetector<T>` and a dirty flag polled in `Update()` to batch option list rebuilds.

Base classes handle DI wiring and Observable subscription; subclasses implement `AddInputListener()`, `RemoveInputListener()`, and value parsing — Template Method pattern.

**Progress** (`Runtime/Progress/`)  
`ProgressBar` (Slider-based) and `FilledImageProgressDisplay` (Image fill-based). Both subscribe to `ReadonlyObservable<float>` in the 0–1 range.

**RollingNumber** (`Runtime/RollingNumber/`)  
`RollingNumber<T>` → `FloatRollingNumber`, `IntRollingNumber`. Animated number transitions using `CoroutineHelper` (coroutines keyed by ID). Implements all three DI interfaces. If `WithValue()` is called before the container runs `Initialize()`, the component initialises automatically. If fluent setup happens after injection (common pattern), call `Init()` manually after the builder chain:

```csharp
rollingNumber.WithValue(observable)
    .WithEase(EaseFunction)
    .WithDuration(0.5f)
    .WithFormat(v => v.ToString("F2"))
    .Init();
```

**Styles** (`Runtime/Styles/`)  
ScriptableObject-based color theming. `ColorStyleId` (extends `ScriptableObjectId`) is a type-safe asset reference. `ColorStyleSettings` maps IDs to colors. `ApplyColorStyle` subscribes to `Observable<ColorStyleSettings>` and applies colors to `Graphic` components. `SetColorStylesButton` / `ClearColorStylesButton` mutate the observable to switch themes.

**Drag** (`Runtime/Drag/`)  
`DragableUI` implements `IDragHandler`, `IBeginDragHandler`, `IEndDragHandler`. `DragableUIInstaller` (a `MonoInstaller`) configures DI with an `Arguments` struct holding an optional `RectTransform` bounds reference.

### Editor Reset Convention

Components use Unity's `Reset()` callback (called when a component is first added in the Editor) to auto-populate serialized UI references:

```csharp
private void Reset()
{
    _textField = GetComponent<TextMeshProUGUI>();
}
```

### Naming Conventions

- Private fields: `_camelCase` (underscore prefix)
- Serialized fields: `[SerializeField] private Type _name;`
- Generic type parameters: `TValue` for value generics, `T` for simpler ones
- No `Abstract` prefix on abstract classes — the `abstract` keyword is sufficient

## Samples

`Samples~/` contains three importable samples demonstrating usage patterns:
- `BasicUI/` — installers and testers for text displays, inputs, progress, and color styles
- `DragableUI/` — drag example with logging
- `AdvancedUI/` — `RollingNumber` with fluent builder and easing
