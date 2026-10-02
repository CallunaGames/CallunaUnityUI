## [1.11.0] - 2026-10-02

Requires `com.calluna.core` 1.7.0 and `com.calluna.di` 1.6.0.

### Changed
- `VirtualScrollView` stops following its list in `QuitHandler.HandleQuit()` instead of on `QuitDetector.OnQuit` (obsolete since DI 1.6.0). It no longer needs a `QuitDetector`; on quit it's called while all contexts are still intact, before any of them is torn down.

---

## [1.10.1] - 2026-09-30

### Added
- `VirtualScrollViewConsistencyTests` - makes sure the cells always match the data list: every list change (add, insert, remove, replace, swap, clear, `OverrideWith`, `OverrideWithEvents`) at positions before, inside and behind the viewport, several changes per frame and random sequences with scrolling. After every change each active cell must show the item at its index at that index's position, without leaked or double-returned cells; after the layout update exactly the visible indices must have a cell. No runtime changes.

---

## [1.10.0] - 2026-09-29

Requires `com.calluna.core` 1.7.0. Release together with `com.calluna.inventory` 1.2.0, whose `Container.ActiveSlots` now reports individual changes instead of a reset.

### Added
- `VirtualScrollBase.SetLayoutDirty()` / `ApplyLayoutChanges()` — batched layout update. `SetLayoutDirty()` marks content size and visible cells as outdated; `ApplyLayoutChanges()` resizes the content and reconciles the visible cells once. It runs automatically on `Canvas.preWillRenderCanvases` - after all `LateUpdate` calls, independent of script execution order - and can be called to apply changes earlier.
- `VirtualScrollBase.SwapActiveItems(int, int)` — moves two active cells to each other's position without returning or requesting cells.
- `VirtualScrollBase.ReturnAllActiveItems()` — returns all active cells to the pool.

### Changed
- `VirtualScrollView` subscribes via `ReadonlyObservableList.Subscribe(added:, removed:, replaced:, swapped:, reset:)` instead of the obsolete list events, and updates content size and visible cells once per frame instead of once per change. A burst of changes (e.g. `OverrideWithEvents` after a sort or filter change) no longer requests cells for intermediate states.
  - Swap: the two cells trade positions; previously both were returned and requested again.
  - Replace: the cell is returned right away, the new one is requested with the layout update.
  - Reset (`Clear`, `OverrideWith`): all cells are returned right away, the visible ones are requested with the layout update.
- `VirtualScrollBase.ScrollToIndex` and scroll events apply pending layout changes first, so they always work with the current content size.
- `VirtualScrollBase.LateUpdate` handles viewport-size changes and `SetDirty()` through the batched layout update.
- `BasicDropdown` uses `SubscribeAny` instead of the obsolete `ObservableListChangeDetector`. The subscription is created in `Initialize` (previously the detector was created in `Inject`).
- `RollingNumber<T>` runs its roll in a `CoroutineSlot` instead of `CoroutineHelper.ReplaceWithID`. The slot is recreated on every `Inject`, so a replaced `CoroutineHelper` (e.g. after a scene reload) is never used.
- `RollingNumber<T>.WithValue()` subscribes only once the rolling number is applied; before that, `Apply()` subscribes.
- `package.json`: `unity` / `unityRelease` fixed to `6000.0` / `33f1`.

### Fixed
- `RollingNumber<T>` stopped updating after `Clean()` followed by `Initialize()` without a new `WithValue()` call, because `Apply()` didn't subscribe again.
- `RollingNumber<T>` kept rolling after `Clean()`. The roll is now stopped.
- `RollingNumber<T>` threw a `NullReferenceException` if the value changed between `WithValue()` and the first `Apply()`.

---

## [1.9.0] - 2026-06-28

### Added
- `TextInput<TValue>._cropWhitespace` serialized field — when `true`, leading and trailing whitespace is stripped from the raw input string before parsing and before the empty-prohibited guard. The display field updates to the trimmed value automatically via the observable feedback loop. Works in both `OnValueChanged` and `OnSubmit` update modes.

---

## [1.8.5] - 2026-05-14

### Fixed
- `VirtualScrollBase.ScrollToIndex` now works correctly when called immediately after `Initialize()`, before the data source has produced its first item set. Previously, `ItemCount = 0` caused the guard to return silently and the call was lost. The request is now stored and applied automatically on the first `LateUpdate` after items are available. If the index is no longer valid at that point (item filtered out or list shrank) the request is silently dropped. `CleanBase()` discards any stored request so stale indices cannot fire on re-open.

---

## [1.8.4] - 2026-05-14

### Fixed
- `VirtualScrollBase.InitializeBase()` now resets `ScrollRect.normalizedPosition` to `(0, 1)` before subscribing to `onValueChanged`. This clears the ScrollRect's internal scroll-position tracking so its `LateUpdate` does not restore a previous session's horizontal offset via elastic or clamp correction during the two-frame deferred activation window. `(0, 1)` maps to left edge, top — matching `anchoredPosition = Vector2.zero` for a top-left-anchored content rect. The call is a no-op before canvas layout (bounds are zero-sized), and correctly resets on subsequent opens.

---

## [1.8.3] - 2026-05-14

### Fixed
- `VirtualScrollBase.InitializeBase()` now calls `ScrollRect.StopMovement()` before subscribing to `onValueChanged` to prevent inertia-driven content drift during the two-frame deferred activation window. `CleanBase()` also calls `StopMovement()` as a symmetrical guard on teardown.

---

## [1.8.2] - 2026-05-13

### Performance
- `VirtualScrollBase.InitializeBase()` no longer calls `Rebuild()` synchronously on the first frame. A deferred activation flag causes the first `LateUpdate` after initialization to skip item activation; the second `LateUpdate` detects the viewport-size change and activates visible items normally. This spreads the pool-take and DI-init cost across frames rather than concentrating it in the initialization frame. `CleanBase()` resets the flag so re-initialization after cleaning behaves identically.

---

## [1.8.1] - 2026-05-13

### Performance
- `VirtualScrollView` now subscribes to `OnContentsReplaced` on its data list and calls `Rebuild` once instead of triggering a full refresh cycle per element when the list is updated via `OverrideWith`. This eliminates excessive `RefreshVisibleItems`, `ReturnActiveItemAt`, `ShiftActiveItems`, and `ResizeContent` calls during bulk data updates such as inventory container refreshes.

---

## [1.8.0] - 2026-05-03

### Added
- `TextInputUpdateMode` enum (`OnValueChanged` / `OnSubmit`) — controls when `TextInput<TValue>` writes the parsed value back to the `Observable`. Use `OnSubmit` to defer writes until the user confirms input.
- `TextInputVisualArgs` class — DI-injectable configuration for `TextInput<TValue>` visual feedback, specifying the target `Graphic` and the color to apply when input is invalid.
- `TextInputVisualArgsInstaller` — concrete `MonoInstaller` that binds `TextInputVisualArgs` into the DI context.
- `TextInput<TValue>._isEmptyProhibited` serialized field — when `true`, blank or whitespace input is silently ignored in `OnValueChanged` mode, or the display is reverted to the last valid value in `OnSubmit` mode.
- `TextInput<TValue>.ParsingFailed` event — fires when `TryParseInput` returns `false`, supplying the raw input string to subscribers.
- `ScrollAlignment` enum (`Start`, `Center`, `End`) — specifies where a target item should be positioned within the viewport during a scroll-to operation.
- `VirtualScrollView<TItem, TData>.ScrollToIndex(int, ScrollAlignment, float, TweenType)` — public method to scroll to a specific list item, with optional animated transition.
- `VirtualScrollBase.ScrollTweenerId` constant (`"virtualscroll-tweener"`) — DI ID used to resolve the `ValueTweener<float>` that drives animated scroll transitions.

### Fixed
- `VirtualScrollBase.ShiftActiveItems` with a negative delta no longer throws `ArgumentNullException`. The `null` comparison delegate was previously passed to `List<T>.Sort(Comparison<T>)`; the method now uses the pre-existing static readonly comparator.
- `VirtualScrollBase.ScrollToIndex` no longer displaces content along a clamped axis. `horizontalNormalizedPosition` and `verticalNormalizedPosition` are now only written when the corresponding axis has scrollable content (`maxScroll > 0`).

### Changed
- `ObservableValueTextDisplay<TValue>.GetText()` has been renamed to `FormatDisplayText()` (protected virtual). Subclasses that override this method must rename their override accordingly.
- `TextInput<TValue>` now caches a `Func<TValue, string>` formatter during `OnInitialize()` to avoid boxing value types on every input event.

---

## [1.7.3] - 2026-04-25

### Fixed
- `VirtualScrollView` no longer crashes on application quit when a filter is active. Previously, filter cleanup could modify the observable model list while `VirtualScrollView` was still subscribed, causing it to request new pool items after DI bindings had been torn down. `VirtualScrollView` now resolves `QuitDetector` from DI and unsubscribes all list listeners via `QuitDetector.OnQuit`, which fires before `AppContext` begins teardown.

---

## [1.7.2] - 2026-04-24

### Fixed
- `VirtualScrollBase` and `VirtualScrollView` now subscribe to `OnApplicationQuit` and tear down all subscriptions immediately when the application exits, before DI teardown begins. This prevents callbacks from firing against partially-destroyed DI contexts during quit.

---

## [1.7.1] - 2026-04-24

### Fixed
- `VirtualScrollBase`: `LateUpdate()` could fire after `Clean()` and call `ActivateItem()` against a mid-teardown pool, causing DI injection exceptions. An `_initialized` guard is now set to `false` at the start of `CleanBase()` to block any further `LateUpdate` work.
- `VirtualScrollBase`: Reopening a scroll view popup a second time showed only the first row of items despite the content size being calculated correctly. `CleanBase()` now resets `_lastViewportSize` to `Vector2.zero` so the first `LateUpdate` after re-initialization detects a size change and triggers `RefreshVisibleItems()`.

---

## [1.7.0] - 2026-04-21

### Added
- `UIBoundsConstraintApplier` — `MonoBehaviour` and `Injectable` that resolves `UIBoundsConstrainer` from DI and calls `Clamp(_target)` each `LateUpdate`. The target `RectTransform` is configured via a `[SerializeField]` Inspector field; no installer is required.
- `BoundsConstraintApplier` sample — demonstrates `UIBoundsConstraintApplier` driving a `RectTransform` along a Lissajous path that exceeds the configured bounds, showing the clamping in action.

### Changed
- `DragableUI` now resolves `IUIBoundsConstrainer` as an optional dependency. Scenes that have no `UIBoundsConstrainerInstaller` in the DI context no longer require a bounds binding to be present; dragging proceeds unclamped when none is registered.

---

## [1.6.0] - 2026-04-21

### Breaking Changes
- `DragableUI.Arguments` no longer has a `Bounds` field. Configure bounds clamping via `UIBoundsConstrainerInstaller` in the same DI context instead; omit it entirely for unclamped dragging.

### Added
- `UIBoundsConstrainer` — a `MonoBehaviour` that clamps any `RectTransform` inside a configurable bounds region. Usable standalone (e.g. to keep tooltips on screen) or alongside `DragableUI` for bounded dragging.
- `IUIBoundsConstrainer` — interface exposing `Clamp(RectTransform)` and `GetBoundsRect()`; `DragableUI` now depends on this interface, enabling test doubles without a live scene.
- `UIBoundsConstrainerInstaller` — `MonoInstaller` that binds `UIBoundsConstrainer.Arguments` and creates the `UIBoundsConstrainer` component on a named child `GameObject`.

### Internal
- `DragableUI` resolves `IUIBoundsConstrainer` via DI instead of owning the bounds geometry directly; `_draggedRect` renamed to `_constrainedRect` for clarity.

---

## [1.5.0] - 2026-04-14

### Breaking Changes
- `RollingNumber<T>.Init()` has been renamed to `Apply()`. Replace all calls to `.Init()` with `.Apply()` at the end of the fluent builder chain.
- `VirtualScrollGrid<TItem, TData>` has been renamed to `VirtualScrollView<TItem, TData>`. Update all type references, installer subclasses, and serialized MonoBehaviour components accordingly.
- `VirtualScrollGridBase<TItem>` has been renamed to `VirtualScrollBase<TItem>`. Any custom base-class references or subclasses must be updated to use the new name.
- `DragableUI.Arguments` is now a `readonly struct` with a constructor instead of a mutable struct with object-initializer fields. Replace object-initializer syntax (`new DragableUI.Arguments { BoundsRegion = rect }`) with the constructor form (`new DragableUI.Arguments(rect)`).
- `TextInput<TValue>.OnParsingFailure` event has been renamed to `ParsingFailed`. Update all subscriptions and unsubscriptions to use the new name.

### Added
- `VerticalListScrollLayout` — single-column top-to-bottom virtualised list layout with configurable item size, spacing, and padding.
- `HorizontalListScrollLayout` — single-row left-to-right virtualised list layout with configurable item size, spacing, and padding.
- `VirtualScrollVerticalListInstaller` and `VirtualScrollHorizontalListInstaller` — abstract installer base classes for list scroll views, following the same pattern as `VirtualScrollGridInstaller`.
- `TextInput<TValue>.ParsingFailed` event — subscribe to receive the raw string whenever the user's input cannot be parsed, without needing to subclass.

### Fixed
- `RollingNumber<T>.Apply()` no longer starts a second animation coroutine when the DI container calls `Initialize()` after the caller has already called `Apply()` manually. The first `Apply()` call marks the component as applied; the container's automatic path becomes a no-op. Calling `Apply()` explicitly again still re-applies and restarts the animation as expected.
- `ApplyColorStyle` no longer throws when a `ColorStyleId` is not present in the active `ColorStyleSettings`; it falls back to the component's initial color instead.

### Performance
- `ColorStyleSettings.TryGetColorOf` no longer allocates a closure and delegate on every style-change event; the internal lookup now uses a plain loop.
- `VirtualScrollBase` no longer allocates a sort delegate on every list-insert operation; the descending-index `Comparison<int>` is now a static readonly field.

## [1.4.0] - 2026-04-11

### Breaking Changes
- `VirtualScrollItem<TData>` has been removed. Cell MonoBehaviours should implement `Injectable`, `Initializable`, and `Cleanable` directly and resolve their data via `resolver.Resolve<TData>()` in `Inject`.
- `VirtualScrollGrid<TItem>` (count-only variant) has been removed. Use `VirtualScrollGrid<TItem, TData>` with a `ReadonlyObservableList<TData>` instead — this allows the grid to respond correctly to insertions, removals, and replacements, not just count changes. For uniform cells with no meaningful per-item data, use a lightweight model type (e.g. an empty struct) as `TData`.
- `Inject`, `Initialize`, and `Clean` are now explicit interface implementations on `BasicInput`, `BasicDropdown`, all display components (`ObservableValueTextDisplay<TValue>` and subclasses), and all style components (`ApplyColorStyle`, `SetColorStylesButton`, `ClearColorStylesButton`). Code that called these methods directly on a concrete reference (e.g. `myFloatInput.Initialize()`) must cast to the appropriate interface first, or call through the DI container. Subclasses of `BasicInput` can still override the lifecycle via the protected virtual bridge methods.
- `SliderInput<TValue>` abstract methods `ParseValue` and `ParseInput` have been renamed to `ToSliderValue` and `FromSliderValue`. Any custom `SliderInput` subclass must rename its overrides accordingly.
- `TextInput<TValue>` abstract method `ParseInput(string)` has been replaced by `TryParseInput(string, out TValue)`. Subclasses must adopt the `out` parameter signature and return a `bool` indicating success; invalid input is now silently ignored with a `Debug.LogWarning` rather than throwing. The overridable `OnParseFailure(string)` hook is available for custom failure handling.
- `RollingNumber<T>.Init()` has been renamed to `Apply()`. The method now also guards against double-application: if `Apply()` has already been called (e.g. by another component's `Initialize()`), the DI container's automatic call via `Initializable.Initialize()` is a no-op. Calling `Apply()` again explicitly at runtime (e.g. after `WithDuration()`) re-applies the new configuration and restarts the animation as before.

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
