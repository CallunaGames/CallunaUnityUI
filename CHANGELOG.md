## [1.4.0] - 2026-04-11

### Breaking Changes
- `VirtualScrollItem<TData>` has been removed. Cell MonoBehaviours should implement `Injectable`, `Initializable`, and `Cleanable` directly and resolve their data via `resolver.Resolve<TData>()` in `Inject`.
- `VirtualScrollGrid<TItem>` (count-only variant) has been removed. Use `VirtualScrollGrid<TItem, TData>` with a `ReadonlyObservableList<TData>` instead — this allows the grid to respond correctly to insertions, removals, and replacements, not just count changes. For uniform cells with no meaningful per-item data, use a lightweight model type (e.g. an empty struct) as `TData`.
- `Inject`, `Initialize`, and `Clean` are now explicit interface implementations on `BasicInput`, `BasicDropdown`, all display components (`ObservableValueTextDisplay<TValue>` and subclasses), and all style components (`ApplyColorStyle`, `SetColorStylesButton`, `ClearColorStylesButton`). Code that called these methods directly on a concrete reference (e.g. `myFloatInput.Initialize()`) must cast to the appropriate interface first, or call through the DI container. Subclasses of `BasicInput` can still override the lifecycle via the protected virtual bridge methods.
- `SliderInput<TValue>` abstract methods `ParseValue` and `ParseInput` have been renamed to `ToSliderValue` and `FromSliderValue`. Any custom `SliderInput` subclass must rename its overrides accordingly.
- `TextInput<TValue>` abstract method `ParseInput(string)` has been replaced by `TryParseInput(string, out TValue)`. Subclasses must adopt the `out` parameter signature and return a `bool` indicating success; invalid input is now silently ignored with a `Debug.LogWarning` rather than throwing. The overridable `OnParseFailure(string)` hook is available for custom failure handling.
- `RollingNumber<T>` now implements `Initializable` and is initialised automatically when the DI container calls `Initialize()`. If you previously called `Init()` manually after a fluent builder chain without using DI, you must ensure the component is injected before building; if DI is not used, calling `Init()` explicitly after the builder chain remains supported and is still required in that case.

### Added
- New `VirtualScroll` subsystem: `VirtualScrollGrid<TItem>` and `VirtualScrollGrid<TItem, TData>` provide a virtualised, pooled grid scroll view that recycles off-screen cells on scroll. Accompanying types include `IScrollLayout`, `GridScrollLayout`, `Padding`, and `VirtualScrollGridInstaller`.
- `ColorStyleSettings.TryGetColorOf(ColorStyleId, out Color)` — a non-throwing alternative to `GetColorOf` for callers that need to handle missing style entries without catching exceptions.
- New `VirtualScrollUI` importable sample demonstrating the virtualised grid with live data updates.

### Fixed
- `TextInput<TValue>` subclasses no longer throw an unhandled exception when the user types a value that cannot be parsed; the input is silently discarded and a `Debug.LogWarning` is emitted. Override `OnParseFailure(string)` to supply custom error handling.

### Changed
- `VirtualScrollGrid<TItem, TData>` now subscribes directly to `ReadonlyObservableList<TData>` events (`OnItemAdded`, `OnItemRemoved`, `OnItemReplaced`, `OnItemsSwapped`) instead of using `ObservableListChangeDetector`. Replacements and swaps refresh only the affected visible cells with no rebuild; insertions and removals shift and reposition active cells, then reconcile the visible range.
- `BasicDropdown` no longer allocates a new `List` on every option list rebuild; a reusable `_optionsBuffer` is used instead, reducing per-frame GC pressure during dropdown population.
- `RollingNumber<T>` animation logic has been extracted into an internal `RollingNumberAnimator<T>` to make the pure animation behaviour independently testable.
- `RollingNumber<T>` default format delegate no longer boxes value types.
- `DragableUI.ClampPositionToBounds` is now an `internal static` method, making it testable without a live `RectTransform`.
