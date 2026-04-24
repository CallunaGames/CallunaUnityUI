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
    /// </list>
    /// </summary>
    public abstract class VirtualScrollView<TItem, TData> : VirtualScrollBase<TItem>, Injectable, Initializable, Cleanable
        where TItem : Component
    {
        private Pool<TItem, TData, PrefabInstantiationArguments> _pool;
        private ReadonlyObservableList<TData> _items;

        protected override int ItemCount => _items.Count;

        void Injectable.Inject(Resolver resolver)
        {
            _layout = resolver.Resolve<IScrollLayout>();
            _pool   = resolver.Resolve<Pool<TItem, TData, PrefabInstantiationArguments>>();
            _items  = resolver.Resolve<ReadonlyObservableList<TData>>();
        }

        void Initializable.Initialize()
        {
            _items.OnItemAdded    += OnItemAdded;
            _items.OnItemRemoved  += OnItemRemoved;
            _items.OnItemReplaced += OnItemReplaced;
            _items.OnItemsSwapped += OnItemsSwapped;
            InitializeBase();
        }

        void Cleanable.Clean()
        {
            UnsubscribeItems();
            CleanBase();
        }

        protected override void OnApplicationQuit()
        {
            UnsubscribeItems();
            base.OnApplicationQuit();
        }

        private void UnsubscribeItems()
        {
            _items.OnItemAdded    -= OnItemAdded;
            _items.OnItemRemoved  -= OnItemRemoved;
            _items.OnItemReplaced -= OnItemReplaced;
            _items.OnItemsSwapped -= OnItemsSwapped;
        }

        protected override TItem RequestItem(int index)
            => _pool.Request(_items[index], PrefabInstantiationArguments.CreateUIArgs(_contentRect));

        protected override void ReturnItem(TItem item)
            => _pool.Return(item);

        // ── List event handlers ──────────────────────────────────────────────────

        private void OnItemAdded(TData item, int index)
        {
            ShiftActiveItems(index, +1);
            ResizeContent();
            RefreshVisibleItems();
        }

        private void OnItemRemoved(TData item, int index)
        {
            ReturnActiveItemAt(index);
            ShiftActiveItems(index + 1, -1);
            ResizeContent();
            RefreshVisibleItems();
        }

        private void OnItemReplaced(TData newItem, TData formerItem, int index)
            => ReplaceActiveItem(index);

        private void OnItemsSwapped(TData item1, int index1, TData item2, int index2)
        {
            ReplaceActiveItem(index1);
            ReplaceActiveItem(index2);
        }
    }
}
