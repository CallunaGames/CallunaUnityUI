# Unity UI package
The Calluna Games package for Unity UI

## Description
This Unity package implements a solution for the following common UI problems:
- Changing colors of Unity UI elements using a style
- Displaying values via text, progress bars
- Inputing values via input fields, sliders, dropdowns

## Planned Features
- Popup system
- Toast messages

## Dependencies
The package is dependant on the following packages. Please make sure to import them via the package manager.
- [Calluna Core v1.0.3](https://github.com/CallunaGames/CallunaUnityCore)
- [Calluna DI v.1.0.2](https://github.com/CallunaGames/CallunaUnityDI)

## Features
### Value display
The value displays make use of the Calluna Core `Observerable` class or more precisely the `ReadonlyObservable` class. 
If its value changes the display also changes. The observable value is injected into the display class using the Calluna DI. 
Following value types are supported:
- float
- int
- string 

Following visual representation is implemented:
- TextMeshProUGUI Texts (`FloatTextDisplay`, `IntTextDisplay`, `StringTextDisplay`)
- Slider Progress Bar (`ProgressBar`)
- Fillable Images (`FilledImageProgressDisplay`)

#### Example 1: Show float value as progress bar
1. Bind the float value using a MonoInstaller: 
```
public class MyInstaller : MonoInstaller
{
    public override void InstallBindings(Binder binder)
    {
        binder.Bind<ReadonlyObservable<float>>().And<Observable<float>>().ToNew<Observable<float>>().AsSingle();
    }
}
```
2. Add the Installer to a Context like a `SceneContext` or `ObjectContext`
3. Setup a Progress Bar by using an Unity UI slider. Add the `ProgressBar` script to the Slider object. Setup the serialized fields.

![Progress Bar Objects](Documentation/ProgressBarObjects.png)
4. Optional: Change the float value during runtime to see the progress bar in action
5. Press play

![Progress Bar Play](Documentation/ProgressBarPlay.png)

### Value Input

### Color Styles

### Virtual Scroll Grid

A virtualised scrollable grid that only instantiates cells for the items currently visible in the viewport. Use it when a `ScrollRect` must display a large or unbounded list of items and you cannot afford to instantiate one GameObject per entry.

#### Class hierarchy

```
IScrollLayout
    GridScrollLayout(Settings)          — top-to-bottom fixed-column grid

VirtualScrollGridBase<TItem>            — MonoBehaviour, shared scroll/pool/layout logic
    VirtualScrollGrid<TItem>            — no per-item data; item count from ReadonlyObservable<int>
    VirtualScrollGrid<TItem, TData>     — data variant; data list from ReadonlyObservableList<TData>

VirtualScrollItem<TData>                — optional MonoBehaviour base for cells in the data variant
```

`GridScrollLayout` is constructed with a serializable `Settings` struct:

| Field | Type | Description |
|---|---|---|
| `Columns` | `int` | Number of columns |
| `CellSize` | `Vector2` | Width and height of each cell in pixels |
| `Spacing` | `Vector2` | Gap between cells (horizontal, vertical) |
| `Padding` | `Padding` | Outer padding (`Top`, `Bottom`, `Left`, `Right`) |

#### Usage (data variant)

**Step 1 — Define the data struct**

```csharp
public struct ItemData
{
    public int    Index;
    public string Label;
}
```

**Step 2 — Create the cell MonoBehaviour**

Extend `VirtualScrollItem<TData>` for the simplest setup. The base class resolves `TData` automatically on each activation; override `OnInitialize()` to apply it to visuals.

```csharp
public class ItemCell : VirtualScrollItem<ItemData>
{
    [SerializeField] private TextMeshProUGUI _label;

    protected override void OnInitialize()
    {
        _label.text = $"{Data.Index}: {Data.Label}";
    }
}
```

**Step 3 — Create the grid MonoBehaviour**

Subclass `VirtualScrollGrid<TItem, TData>` with a single line. The base class handles all DI wiring and scroll logic.

```csharp
public class ItemGrid : VirtualScrollGrid<ItemCell, ItemData> { }
```

**Step 4 — Write the installer**

```csharp
public class ItemGridInstaller : MonoInstaller
{
    [SerializeField] private GridScrollLayout.Settings _layoutSettings;

    public override void InstallBindings(Binder binder)
    {
        // Layout strategy
        binder.Bind<IScrollLayout>()
              .ToInstance(new GridScrollLayout(_layoutSettings));

        // Data list — bind as both mutable and readonly so other systems can write to it
        var items = new ObservableList<ItemData>();
        binder.Bind<ObservableList<ItemData>>()
              .And<ReadonlyObservableList<ItemData>>()
              .ToInstance(items);
    }
}
```

Add a `MonoPoolInstaller<ItemCell, ItemData>` component to the same context to register the pool. Assign the cell prefab in its Inspector field.

**Step 5 — Scene hierarchy**

```
SceneContext
└── ItemGridInstaller          (MonoInstaller)
└── MonoPoolInstaller<ItemCell, ItemData>
ScrollRect
└── Viewport
    └── Content
        └── ItemGrid           (ItemGrid component + ScrollRect reference serialized in)
```

Assign the `ScrollRect` reference on the `ItemGrid` component in the Inspector. The `ScrollRect`'s **Content** and **Viewport** fields must be wired as usual for Unity UI.

#### Exposing the readonly interface

Any system that only reads the list should resolve `ReadonlyObservableList<ItemData>`. A system that populates the list resolves `ObservableList<ItemData>` and calls `.Add()` / `.Remove()` — the grid reacts automatically.
