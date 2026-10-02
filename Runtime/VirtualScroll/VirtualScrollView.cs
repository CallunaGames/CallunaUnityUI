using System;
using Calluna;
using Calluna.DI;
using UnityEngine;

namespace Calluna.UI
{
    /// <summary>
    /// Layout-agnostic virtualised scroll view. Passes a <typeparamref name="TData"/> entry as a
    /// DI argument to each cell when it is (re-)activated. The pool injects and initialises the
    /// cell so it can read its data via <c>resolver.Resolve&lt;TData&gt;()</c>.
    ///
    /// Reacts to the individual changes of its <see cref="ReadonlyObservableList{TData}"/>:
    /// <list type="bullet">
    ///   <item>Swap — the two active cells trade positions; no cell is returned or requested.</item>
    ///   <item>Replace — the active cell is returned; the new one is requested with the next layout update.</item>
    ///   <item>Insert/remove — active cells behind the mutation point are shifted and repositioned.</item>
    ///   <item>Reset (<c>Clear</c>, <c>OverrideWith</c>) — all active cells are returned.</item>
    /// </list>
    /// Content size and visible cells are then updated once per frame, right before the canvases
    /// render (see <see cref="VirtualScrollBase{TItem}.SetLayoutDirty"/>) - so a burst of changes,
    /// e.g. from <c>OverrideWithEvents</c>, doesn't request cells for intermediate states.
    ///
    /// DI bindings required:
    /// <list type="bullet">
    ///   <item><see cref="IScrollLayout"/> — any layout implementation</item>
    ///   <item><see cref="Pool{TItem,TData,PrefabInstantiationArguments}"/> — via <c>MonoPoolInstaller&lt;TItem,TData&gt;</c></item>
    ///   <item><see cref="ReadonlyObservableList{TData}"/> — data source</item>
    ///   <item><c>ValueTweener&lt;float&gt;</c> id <see cref="VirtualScrollBase.ScrollTweenerId"/> —
    ///         provided by the layout installer; requires <see cref="CoroutineHelper"/></item>
    /// </list>
    /// </summary>
    public abstract class VirtualScrollView<TItem, TData> : VirtualScrollBase<TItem>, Injectable, Initializable, Cleanable,
        QuitHandler where TItem : Component
    {
        private Pool<TItem, TData, PrefabInstantiationArguments> _pool;
        private ReadonlyObservableList<TData> _items;
        private IDisposable _itemsSubscription;

        protected override int ItemCount => _items.Count;

        void Injectable.Inject(Resolver resolver)
        {
            _layout        = resolver.Resolve<IScrollLayout>();
            _pool          = resolver.Resolve<Pool<TItem, TData, PrefabInstantiationArguments>>();
            _items         = resolver.Resolve<ReadonlyObservableList<TData>>();
            _scrollTweener = resolver.Resolve<ValueTweener<float>>(ScrollTweenerId);
        }

        void Initializable.Initialize()
        {
            _itemsSubscription = _items.Subscribe(
                added: OnItemAdded,
                removed: OnItemRemoved,
                replaced: OnItemReplaced,
                swapped: OnItemsSwapped,
                reset: OnItemsReset);
            InitializeBase();
        }

        void Cleanable.Clean()
        {
            UnsubscribeItems();
            CleanBase();
        }

        // On quit the contexts are torn down - stop following the list before its owner clears it,
        // so the view doesn't request cells from a pool that is being torn down.
        void QuitHandler.HandleQuit() => UnsubscribeItems();

        private void UnsubscribeItems()
        {
            _itemsSubscription?.Dispose();
            _itemsSubscription = null;
        }

        /// <summary>
        /// Scrolls so that the item at <paramref name="index"/> is visible.
        /// Pass a positive <paramref name="duration"/> for an animated scroll; omit or pass 0 for
        /// an instant snap. Animated scroll is driven by the <c>ValueTweener&lt;float&gt;</c> and
        /// <see cref="CoroutineHelper"/> provided by the layout installer.
        /// </summary>
        public void ScrollToIndex(int index, ScrollAlignment alignment = ScrollAlignment.Start,
            float duration = 0f, TweenType tweenType = TweenType.EaseInOutSine)
            => base.ScrollToIndex(index, alignment, duration, tweenType);

        protected override TItem RequestItem(int index)
            => _pool.Request(_items[index], PrefabInstantiationArguments.CreateUIArgs(_contentRect));

        protected override void ReturnItem(TItem item)
            => _pool.Return(item);

        // ── List change handlers ─────────────────────────────────────────────────

        private void OnItemAdded(TData _, int index)
        {
            ShiftActiveItems(index, +1);
            SetLayoutDirty();
        }

        private void OnItemRemoved(TData _, int index)
        {
            ReturnActiveItemAt(index);
            ShiftActiveItems(index + 1, -1);
            SetLayoutDirty();
        }

        // The cell got its data when it was requested, so a new item needs a new cell.
        private void OnItemReplaced(TData _, TData _2, int index)
        {
            ReturnActiveItemAt(index);
            SetLayoutDirty();
        }

        // The cells keep their data - they just move along with it.
        private void OnItemsSwapped(TData _, int index1, TData _2, int index2)
        {
            SwapActiveItems(index1, index2);
            SetLayoutDirty();
        }

        private void OnItemsReset()
        {
            ReturnAllActiveItems();
            SetLayoutDirty();
        }
    }
}
