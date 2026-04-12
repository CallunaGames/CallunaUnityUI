using Calluna.DI;
using UnityEngine;

namespace Calluna.UI
{
    /// <summary>
    /// Virtualised grid that displays items without per-item data arguments.
    /// Items are requested from a <see cref="Pool{TItem,PrefabInstantiationArguments}"/> and placed
    /// by the injected <see cref="IScrollLayout"/>. Item count is driven by a
    /// <see cref="ReadonlyObservable{Int32}"/>.
    ///
    /// DI bindings required:
    /// <list type="bullet">
    ///   <item><see cref="IScrollLayout"/> — e.g. <c>GridScrollLayout</c></item>
    ///   <item><see cref="Pool{TItem,PrefabInstantiationArguments}"/> — via <c>MonoPoolInstaller&lt;TItem&gt;</c></item>
    ///   <item><see cref="ReadonlyObservable{Int32}"/> — item count</item>
    /// </list>
    /// </summary>
    public abstract class VirtualScrollGrid<TItem> : VirtualScrollGridBase<TItem>, Injectable, Initializable, Cleanable
        where TItem : Component
    {
        private Pool<TItem, PrefabInstantiationArguments> _pool;
        private ReadonlyObservable<int> _count;

        protected override int ItemCount => _count.Value;

        void Injectable.Inject(Resolver resolver)
        {
            _layout = resolver.Resolve<IScrollLayout>();
            _pool   = resolver.Resolve<Pool<TItem, PrefabInstantiationArguments>>();
            _count  = resolver.Resolve<ReadonlyObservable<int>>();
        }

        void Initializable.Initialize()
        {
            _count.OnChanged += SetDirty;
            InitializeBase();
        }

        void Cleanable.Clean()
        {
            _count.OnChanged -= SetDirty;
            CleanBase();
        }

        protected override TItem RequestItem(int index)
            => _pool.Request(PrefabInstantiationArguments.CreateUIArgs(_contentRect));

        protected override void ReturnItem(TItem item)
            => _pool.Return(item);
    }

    /// <summary>
    /// Virtualised grid that passes a <typeparamref name="TData"/> entry as a DI argument to each
    /// cell when it is (re-)activated. The pool injects and initialises the cell so it can read
    /// its data via <c>resolver.Resolve&lt;TData&gt;()</c>.
    ///
    /// DI bindings required:
    /// <list type="bullet">
    ///   <item><see cref="IScrollLayout"/> — e.g. <c>GridScrollLayout</c></item>
    ///   <item><see cref="Pool{TItem,TData,PrefabInstantiationArguments}"/> — via <c>MonoPoolInstaller&lt;TItem,TData&gt;</c></item>
    ///   <item><see cref="ReadonlyObservableList{TData}"/> — data source</item>
    /// </list>
    /// </summary>
    public abstract class VirtualScrollGrid<TItem, TData> : VirtualScrollGridBase<TItem>, Injectable, Initializable, Cleanable
        where TItem : Component
    {
        private Pool<TItem, TData, PrefabInstantiationArguments> _pool;
        private ReadonlyObservableList<TData> _items;
        private ObservableListChangeDetector<TData> _changeDetector;

        protected override int ItemCount => _items.Count;

        void Injectable.Inject(Resolver resolver)
        {
            _layout          = resolver.Resolve<IScrollLayout>();
            _pool            = resolver.Resolve<Pool<TItem, TData, PrefabInstantiationArguments>>();
            _items           = resolver.Resolve<ReadonlyObservableList<TData>>();
            _changeDetector  = new ObservableListChangeDetector<TData>(_items);
        }

        void Initializable.Initialize()
        {
            _changeDetector.OnChanged += SetDirty;
            InitializeBase();
        }

        void Cleanable.Clean()
        {
            _changeDetector.OnChanged -= SetDirty;
            _changeDetector.Dispose();
            CleanBase();
        }

        protected override TItem RequestItem(int index)
            => _pool.Request(_items[index], PrefabInstantiationArguments.CreateUIArgs(_contentRect));

        protected override void ReturnItem(TItem item)
            => _pool.Return(item);
    }
}
