# Unity UI package
The Calluna Games package for Unity UI

## Description
This Unity package implements reactive UI MonoBehaviours built on Calluna's Observable and DI systems. It covers the following common UI problems:
- Displaying values via text labels and progress bars
- Inputting values via input fields, sliders, dropdowns, and toggles
- Animated number transitions (rolling numbers)
- Draggable UI panels
- ScriptableObject-based color theming
- Virtualised scrollable grids for large or unbounded lists

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
Render the value into a `TextMeshProUGUI` component using a configurable format string.

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

`TextInput` subclasses silently discard unparseable input and emit a `Debug.LogWarning`. Override `OnParseFailure(string)` for custom error handling.

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
| `ColorStyleSettings` | `ScriptableObject` that maps `ColorStyleId` → `Color`; call `GetColorOf(id)` or `TryGetColorOf(id, out color)` |
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

Configured via a fluent builder API. When used with DI the component initialises automatically; when used without DI call `Init()` explicitly after the builder chain.

```csharp
rollingNumber
    .WithValue(myObservable)
    .WithDuration(0.5f)
    .WithEase(EasingFunctions.EaseOutQuad)
    .WithFormat(v => v.ToString("F2"))
    .WithRollOnInit()
    .Init(); // only needed when DI is not in use
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

### Drag

`DragableUI` makes any `RectTransform` draggable within its parent, with an optional clamped bounds `RectTransform`.

Configure via `DragableUIInstaller` (a `MonoInstaller`). Its serialized `Arguments` struct exposes an optional `Bounds` reference in the Inspector — leave it unassigned for unconstrained dragging.

```
GameObjectContext
└── DragableUIInstaller    (set Bounds if needed)
Panel
└── DragableUI             (drag target)
```

---

### Virtual Scroll Grid

A virtualised scrollable grid that only instantiates cells for items currently visible in the viewport. Use it when a `ScrollRect` must display a large or unbounded list of items and you cannot afford to instantiate one `GameObject` per entry.

#### Class hierarchy

```
IScrollLayout
    GridScrollLayout(Settings)          — top-to-bottom fixed-column grid

VirtualScrollGridBase<TItem>            — MonoBehaviour, shared scroll/pool/layout logic
    VirtualScrollGrid<TItem>            — count-only variant; item count from ReadonlyObservable<int>
    VirtualScrollGrid<TItem, TData>     — data variant; data list from ReadonlyObservableList<TData>
```

`GridScrollLayout` is constructed from a serializable `Settings` struct:

| Field | Type | Description |
|---|---|---|
| `Columns` | `int` | Number of columns |
| `CellSize` | `Vector2` | Width and height of each cell in pixels |
| `Spacing` | `Vector2` | Gap between cells (horizontal, vertical) |
| `Padding` | `Padding` | Outer padding (`Top`, `Bottom`, `Left`, `Right`) |

#### Usage — count-only variant

Use `VirtualScrollGrid<TItem>` when cells are uniform and need no per-item data. Bind a `ReadonlyObservable<int>` for the item count and a `Pool<TItem, PrefabInstantiationArguments>` via `MonoPoolInstaller<TItem>`.

```csharp
public class MyGrid : VirtualScrollGrid<MyCell> { }
```

#### Usage — data variant

Use `VirtualScrollGrid<TItem, TData>` when each cell must display different data. The pool injects the data entry into the cell each time it is activated.

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

**Step 3 — Create the grid MonoBehaviour**

```csharp
public class ItemGrid : VirtualScrollGrid<ItemCell, ItemData> { }
```

**Step 4 — Write the installer**

Subclass `VirtualScrollGridInstaller` to get the `IScrollLayout` binding for free. Its serialized `_layoutSettings` field appears in the Inspector automatically. Call `base.InstallBindings(binder)` first, then add the data list binding:

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

Any system that only reads the list should resolve `ReadonlyObservableList<ItemData>`. A system that populates the list resolves `ObservableList<ItemData>` and calls `.Add()` / `.Remove()` — the grid reacts automatically.
