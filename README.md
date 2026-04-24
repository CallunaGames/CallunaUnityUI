# Unity UI package
The Calluna Games package for Unity UI

## Description
This Unity package implements reactive UI MonoBehaviours built on Calluna's Observable and DI systems. It covers the following common UI problems:
- Displaying values via text labels and progress bars
- Inputting values via input fields, sliders, dropdowns, and toggles
- Animated number transitions (rolling numbers)
- Draggable UI panels with optional bounds clamping
- Keeping UI elements (tooltips, panels) within a bounds region
- ScriptableObject-based color theming
- Virtualised scrollable views (grid, vertical list, horizontal list) for large or unbounded lists

## Planned Features
- Popup system
- Toast messages

## Dependencies
The package is dependent on the following packages. Please make sure to import them via the package manager.
- [Calluna Core v1.2.0](https://github.com/CallunaGames/CallunaUnityCore)
- [Calluna DI v1.3.1](https://github.com/CallunaGames/CallunaUnityDI)

## Features

---

### Value Display

Value displays subscribe to a `ReadonlyObservable<T>` injected via DI and update automatically when the value changes.

Supported value types: `float`, `int`, `string`, `double`, `long`

**Text displays** (`FloatTextDisplay`, `IntTextDisplay`, `StringTextDisplay`, `DoubleTextDisplay`, `LongTextDisplay`)  
Render the value into a `TextMeshProUGUI` component using a configurable format string. The format string is applied via `IFormattable.ToString(format, culture)`. A `CultureInfo` may be optionally bound in the DI context; when absent, `CultureInfo.InvariantCulture` is used.

**Progress displays**
- `ProgressBar` — drives a `UnityEngine.UI.Slider` from a `ReadonlyObservable<float>` in the 0–1 range.
- `FilledImageProgressDisplay` — drives an `Image` fill amount from a `ReadonlyObservable<float>` in the 0–1 range.

#### Example: Show a float value as a progress bar

1. Bind the observable in a `MonoInstaller`:

```csharp
public class MyInstaller : MonoInstaller
{
    public override void InstallBindings(Binder binder)
    {
        binder.Bind<ReadonlyObservable<float>>()
              .And<Observable<float>>()
              .ToNew<Observable<float>>()
              .AsSingle();
    }
}
```

2. Add the installer to a `SceneContext` or `GameObjectContext`.
3. Add a Unity UI `Slider` to the scene and attach the `ProgressBar` script to it.
4. Press play. The slider tracks the observable value automatically.

---

### Value Input

Input components resolve `Observable<TValue>` from DI and provide two-way binding: the component displays the current value and writes back to the observable when the user interacts with it.

| Component | Backed by | Value type |
|---|---|---|
| `FloatInput` | `TMP_InputField` | `float` |
| `IntInput` | `TMP_InputField` | `int` |
| `StringInput` | `TMP_InputField` | `string` |
| `FloatSlider` | `UnityEngine.UI.Slider` | `float` |
| `IntSlider` | `UnityEngine.UI.Slider` | `int` |
| `ToggleInput` | `UnityEngine.UI.Toggle` | `bool` |
| `BasicDropdown` | `TMP_Dropdown` | `int` (selected index) |

`BasicDropdown` additionally resolves a `ReadonlyObservableList<TMP_Dropdown.OptionData>` for the option list, and reacts live to list changes using `ObservableListChangeDetector<T>`.

`TextInput` subclasses emit a `Debug.LogWarning` and fire the `ParsingFailed` event (`Action<string>`) when input cannot be parsed. Override `OnParseFailure(string)` for custom error handling beyond the event. Text inputs also optionally resolve a `CultureInfo` from DI for number formatting; when absent, `CultureInfo.InvariantCulture` is used.

#### Example: Int input field

```csharp
public class MyInstaller : MonoInstaller
{
    public override void InstallBindings(Binder binder)
    {
        binder.Bind<Observable<int>>()
              .And<ReadonlyObservable<int>>()
              .ToNew<Observable<int>>()
              .AsSingle();
    }
}
```

Add an `IntInput` component to a `TMP_InputField` GameObject in the same context. The field reads and writes the shared `Observable<int>` automatically.

---

### Color Styles

ScriptableObject-based color theming. At runtime, a single `Observable<ColorStyleSettings>` drives all styled UI elements; switching themes is a one-line observable write.

**Key types:**

| Type | Description |
|---|---|
| `ColorStyleId` | Type-safe `ScriptableObject` asset reference identifying a named color slot |
| `ColorStyleSettings` | `ScriptableObject` that maps `ColorStyleId` → `Color`; call `TryGetColorOf(id, out color)` to look up a color (returns `false` when the id is absent), or `GetColorOf(id)` to look up and throw when absent |
| `ApplyColorStyle` | `MonoBehaviour` — resolves `ReadonlyObservable<ColorStyleSettings>` and sets a `Graphic` component's color whenever the active settings change |
| `SetColorStylesButton` | Mutates the `Observable<ColorStyleSettings>` to a configured preset on click |
| `ClearColorStylesButton` | Clears the `Observable<ColorStyleSettings>` (reverts graphics to their initial colors) |

#### Example: Apply a theme color to an Image

1. Create a `ColorStyleId` asset (right-click in Project > Create > Calluna > Color Style Id).
2. Create a `ColorStyleSettings` asset and assign the ID → color mapping in the Inspector.
3. In an installer, bind the settings:

```csharp
public class ThemeInstaller : MonoInstaller
{
    [SerializeField] private ColorStyleSettings _theme;

    public override void InstallBindings(Binder binder)
    {
        var observable = new Observable<ColorStyleSettings>(_theme);
        binder.Bind<Observable<ColorStyleSettings>>()
              .And<ReadonlyObservable<ColorStyleSettings>>()
              .ToInstance(observable);
    }
}
```

4. Add `ApplyColorStyle` to any `Image`, `Text`, or other `Graphic` component. Assign the `ColorStyleId` and `Graphic` reference in the Inspector.
5. Use `SetColorStylesButton` / `ClearColorStylesButton` to switch or clear the active theme at runtime.

---

### Rolling Number

`FloatRollingNumber` and `IntRollingNumber` animate a `TextMeshProUGUI` label smoothly from its current value to a new one whenever the bound observable changes.

Configured via a fluent builder API. Always call `Apply()` at the end of the builder chain to start (or restart) the display. When `WithValue()` is called before the DI container runs `Initialize()`, the `Initialize()` callback will call `Apply()` automatically — but the common pattern is to configure the component inside another component's `Initialize()` and then call `Apply()` explicitly.

```csharp
rollingNumber
    .WithValue(myObservable)
    .WithDuration(0.5f)
    .WithEase(Tween.EaseOutCubic)
    .WithFormat(v => v.ToString("F2"))
    .WithRollOnInit()
    .Apply();
```

| Builder method | Description |
|---|---|
| `WithValue(ReadonlyObservable<T>)` | Observable to track |
| `WithDuration(float)` | Animation length in seconds (default: 0.33) |
| `WithEase(Func<float,float>)` | Easing function applied to the interpolation parameter |
| `WithFormat(Func<T,string>)` | Custom display formatter |
| `WithRollOnInit(bool)` | When `true`, the number animates in from zero on first display |

**DI requirements:** `CoroutineHelper` (provided by Calluna Core) must be bound in the same context.

---

### Bounds Constrainer

`UIBoundsConstrainer` clamps any `RectTransform` so it stays inside a configured bounds region. It is used by `DragableUI` for drag clamping, and can be used standalone to keep UI elements (such as tooltips or floating panels) within a screen or panel region.

**Class hierarchy:**

```
IUIBoundsConstrainer
    UIBoundsConstrainer : MonoBehaviour, Injectable, IUIBoundsConstrainer
```

**`IUIBoundsConstrainer`** — readonly interface for bounds clamping:
- `Rect? GetBoundsRect()` — returns the current bounds as a world-space `Rect`, or `null` when no bounds region is set.
- `void Clamp(RectTransform target)` — repositions `target` in world space so it fits within the bounds region.

**`UIBoundsConstrainer`** additionally exposes:
- `static Vector2 ClampPositionToBounds(Vector2 position, Vector2 size, Vector2 pivot, Rect bounds)` — pure-geometry helper; usable without a MonoBehaviour instance.

Configure via `UIBoundsConstrainerInstaller` (a `MonoInstaller`):

| Field | Type | Description |
|---|---|---|
| `Bounds` | `RectTransform` (optional) | Region to clamp inside; no clamping applied when `null` |

```
GameObjectContext
└── UIBoundsConstrainerInstaller    (assign Bounds RectTransform in Inspector)
```

#### Usage: standalone bounds clamping with UIBoundsConstraintApplier

`UIBoundsConstraintApplier` is a lightweight `MonoBehaviour` that resolves a `UIBoundsConstrainer` from DI and calls `Clamp(_target)` every `LateUpdate`. Use it to keep any UI element — a tooltip, floating label, or context menu — inside the bounds region without writing a custom component.

```csharp
// No installer needed for UIBoundsConstraintApplier itself.
// Add it to the target GameObject in the same DI context as UIBoundsConstrainerInstaller.
// The [SerializeField] _target field auto-populates via Reset() to the component's own RectTransform,
// or assign a different RectTransform in the Inspector.
```

**Scene hierarchy:**

```
GameObjectContext
├── UIBoundsConstrainerInstaller    (assign Bounds RectTransform)
└── Panel
    └── UIBoundsConstraintApplier   (assign Target RectTransform; defaults to own RectTransform)
```

#### Usage: static geometry helper

```csharp
Vector2 clamped = UIBoundsConstrainer.ClampPositionToBounds(
    position:  (Vector2)myRect.position,
    size:      myRect.rect.size * (Vector2)myRect.lossyScale,
    pivot:     myRect.pivot,
    bounds:    boundsRect);
myRect.position = new Vector3(clamped.x, clamped.y, myRect.position.z);
```

---

### Drag

`DragableUI` makes any `RectTransform` draggable, with optional bounds clamping via `UIBoundsConstrainer` and optional axis locking.

Configure via `DragableUIInstaller` (a `MonoInstaller`). Its serialized fields map directly to the `DragableUI.Arguments` struct:

| Inspector field | Type | Description |
|---|---|---|
| `Dragable Transform` | `RectTransform` | The transform that moves on drag |
| `Bound Transform` | `RectTransform` (optional) | Override which rect is measured against bounds (defaults to `Dragable Transform`) |
| `Limit Axis` | `bool` | Enable axis locking |
| `Move Axis` | `RectTransform.Axis` | Active when `Limit Axis` is ticked — locks movement to Horizontal or Vertical |

Bounds clamping requires a `UIBoundsConstrainerInstaller` in the same DI context — `DragableUI` resolves `UIBoundsConstrainer` from DI via `ResolveOptional`. Omit `UIBoundsConstrainerInstaller` for unclamped dragging.

`DragableUI` exposes `OnDragStart` and `OnDragEnd` events (both `Action<Vector2>`). `LimitToBounds(Vector2 delta)` is `protected virtual` — subclass `DragableUI` to customise clamping behaviour.

```
GameObjectContext
├── UIBoundsConstrainerInstaller    (optional — assign Bounds to enable clamping)
└── DragableUIInstaller             (set BoundTransform / MoveAxis if needed)
Panel
└── DragableUI                      (drag target)
```

---

### Virtual Scroll View

A virtualised scrollable view that only instantiates cells for items currently visible in the viewport. Use it when a `ScrollRect` must display a large or unbounded list of items and you cannot afford to instantiate one `GameObject` per entry.

#### Class hierarchy

```
IScrollLayout
    GridScrollLayout(Settings)              — top-to-bottom fixed-column grid
    VerticalListScrollLayout(Settings)      — single-column top-to-bottom list
    HorizontalListScrollLayout(Settings)    — single-row left-to-right list

VirtualScrollBase<TItem>                   — MonoBehaviour, shared scroll/pool/layout logic
    VirtualScrollView<TItem, TData>        — data list from ReadonlyObservableList<TData>
```

#### Layout types

**`GridScrollLayout`** — top-to-bottom grid with a fixed column count.

| Field | Type | Description |
|---|---|---|
| `Columns` | `int` | Number of columns |
| `CellSize` | `Vector2` | Width and height of each cell in pixels |
| `Spacing` | `Vector2` | Gap between cells (horizontal, vertical) |
| `Padding` | `Padding` | Outer padding (`Top`, `Bottom`, `Left`, `Right`) |

Installer base class: `VirtualScrollGridInstaller`

**`VerticalListScrollLayout`** — single-column list scrolling vertically.

| Field | Type | Description |
|---|---|---|
| `ItemSize` | `Vector2` | Width and height of each item in pixels |
| `Spacing` | `float` | Vertical gap between items |
| `Padding` | `Padding` | Outer padding (`Top`, `Bottom`, `Left`, `Right`) |

Installer base class: `VirtualScrollVerticalListInstaller`

**`HorizontalListScrollLayout`** — single-row list scrolling horizontally.

| Field | Type | Description |
|---|---|---|
| `ItemSize` | `Vector2` | Width and height of each item in pixels |
| `Spacing` | `float` | Horizontal gap between items |
| `Padding` | `Padding` | Outer padding (`Top`, `Bottom`, `Left`, `Right`) |

Installer base class: `VirtualScrollHorizontalListInstaller`

Each installer base class is abstract — create a one-line concrete subclass to make it attachable in the Unity Editor:

```csharp
public class MyGridInstaller : VirtualScrollGridInstaller { }
public class MyVerticalListInstaller : VirtualScrollVerticalListInstaller { }
public class MyHorizontalListInstaller : VirtualScrollHorizontalListInstaller { }
```

#### Usage

The view takes a `ReadonlyObservableList<TData>` as its data source, so it reacts correctly to insertions, removals, replacements, and swaps — not just count changes. If cells are uniform and carry no meaningful data, use a lightweight model (e.g. an empty struct or an `int` index) as `TData`.

The pool injects the `TData` entry into each cell as a DI argument on every activation.

**Step 1 — Define the data struct**

```csharp
public struct ItemData
{
    public int    Index;
    public string Label;
}
```

**Step 2 — Create the cell MonoBehaviour**

Implement `Injectable`, `Initializable`, and `Cleanable`. The pool passes the `TData` entry as a DI argument on every activation, so `resolver.Resolve<TData>()` returns the correct entry for that cell.

```csharp
public class ItemCell : MonoBehaviour, Injectable, Initializable, Cleanable
{
    [SerializeField] private TextMeshProUGUI _label;

    private ItemData _data;

    void Injectable.Inject(Resolver resolver) => _data = resolver.Resolve<ItemData>();

    void Initializable.Initialize() => _label.text = $"{_data.Index}: {_data.Label}";

    void Cleanable.Clean() => _label.text = string.Empty;
}
```

**Step 3 — Create the scroll view MonoBehaviour**

```csharp
public class ItemGrid : VirtualScrollView<ItemCell, ItemData> { }
```

**Step 4 — Write the installer**

Subclass the appropriate layout installer to get the `IScrollLayout` binding for free. Its serialized `_layoutSettings` field appears in the Inspector automatically. Call `base.InstallBindings(binder)` first, then add the data list binding:

```csharp
public class ItemGridInstaller : VirtualScrollGridInstaller
{
    private readonly ObservableList<ItemData> _items = new();

    public override void InstallBindings(Binder binder)
    {
        base.InstallBindings(binder);

        binder.Bind<ObservableList<ItemData>>()
              .And<ReadonlyObservableList<ItemData>>()
              .ToInstance(_items);
    }
}
```

Add a `MonoPoolInstaller<ItemCell, ItemData>` component to the same context and assign the cell prefab in its Inspector field.

**Step 5 — Scene hierarchy**

```
SceneContext
└── ItemGridInstaller                     (MonoInstaller)
└── MonoPoolInstaller<ItemCell, ItemData> (assign cell prefab)
ScrollRect
└── Viewport
    └── Content
        └── ItemGrid    (ItemGrid component; assign ScrollRect reference in Inspector)
```

The `ScrollRect`'s **Content** and **Viewport** fields must be wired as usual for Unity UI.

#### Exposing the readonly interface

Any system that only reads the list should resolve `ReadonlyObservableList<ItemData>`. A system that populates the list resolves `ObservableList<ItemData>` and calls `.Add()` / `.Remove()` — the view reacts automatically.

---

## Samples

The following importable samples are available via the Unity Package Manager.

| Sample | Description |
|---|---|
| **Basic UI** | Installers and testers for text displays, inputs, progress bars, and color styles |
| **Dragable UI** | Drag example with event logging |
| **Advanced UI** | `RollingNumber` with fluent builder configuration and easing |
| **Virtual Scroll UI** | Virtualised grid scroll view with live data insertion and removal |
| **Bounds Constraint Applier** | Demonstrates `UIBoundsConstraintApplier` keeping a moving panel inside a bounds region using a `BoundsConstraintMoverTester` script |
