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
    /// Reacts to individual <see cref="ReadonlyObservableList{TData}"/> events:
    /// <list type="bullet">
    ///   <item>Replace/swap — only the affected visible cells are refreshed; no rebuild.</item>
    ///   <item>Insert/remove — active cells above the mutation point are shifted and repositioned;
    ///         <see cref="VirtualScrollBase{TItem}.RefreshVisibleItems"/> reconciles the visible range.</item>
    /// </list>
    ///
    /// DI bindings required:
    /// <list type="bullet">
    ///   <item><see cref="IScrollLayout"/> — any layout implementation</item>
    ///   <item><see cref="Pool{TItem,TData,PrefabInstantiationArguments}"/> — via <c>MonoPoolInstaller&lt;TItem,TData&gt;</c></item>
    ///   <item><see cref="ReadonlyObservableList{TData}"/> — data source</item>
    ///   <item><see cref="QuitDetector"/> — provided by the DI framework's AppContext</item>
    ///   <item><c>ValueTweener&lt;float&gt;</c> id <see cref="VirtualScrollBase.ScrollTweenerId"/> —
    ///         provided by the layout installer; requires <see cref="CoroutineHelper"/></item>
    /// </list>
    /// </summary>
    public abstract class VirtualScrollView<TItem, TData> : VirtualScrollBase<TItem>, Injectable, Initializable, Cleanable
        where TItem : Component
    {
        private Pool<TItem, TData, PrefabInstantiationArguments> _pool;
        private ReadonlyObservableList<TData> _items;
        private QuitDetector _quitDetector;

        protected override int ItemCount => _items.Count;

        void Injectable.Inject(Resolver resolver)
        {
            _layout        = resolver.Resolve<IScrollLayout>();
            _pool          = resolver.Resolve<Pool<TItem, TData, PrefabInstantiationArguments>>();
            _items         = resolver.Resolve<ReadonlyObservableList<TData>>();
            _quitDetector  = resolver.Resolve<QuitDetector>();
            _scrollTweener = resolver.Resolve<ValueTweener<float>>(ScrollTweenerId);
        }

        void Initializable.Initialize()
        {
            _quitDetector.OnQuit  += UnsubscribeItems;
            _items.OnItemAdded    += OnItemAdded;
            _items.OnItemRemoved  += OnItemRemoved;
            _items.OnItemReplaced += OnItemReplaced;
            _items.OnItemsSwapped += OnItemsSwapped;
            InitializeBase();
        }

        void Cleanable.Clean()
        {
            _quitDetector.OnQuit  -= UnsubscribeItems;
            UnsubscribeItems();
            CleanBase();
        }

        private void UnsubscribeItems()
        {
            _items.OnItemAdded    -= OnItemAdded;
            _items.OnItemRemoved  -= OnItemRemoved;
            _items.OnItemReplaced -= OnItemReplaced;
            _items.OnItemsSwapped -= OnItemsSwapped;
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

        // ── List event handlers ──────────────────────────────────────────────────

        private void OnItemAdded(TData _, int index)
        {
            ShiftActiveItems(index, +1);
            ResizeContent();
            RefreshVisibleItems();
        }

        private void OnItemRemoved(TData _, int index)
        {
            ReturnActiveItemAt(index);
            ShiftActiveItems(index + 1, -1);
            ResizeContent();
            RefreshVisibleItems();
        }

        private void OnItemReplaced(TData _, TData _2, int index)
            => ReplaceActiveItem(index);

        private void OnItemsSwapped(TData _, int index1, TData _2, int index2)
        {
            ReplaceActiveItem(index1);
            ReplaceActiveItem(index2);
        }
    }
}
