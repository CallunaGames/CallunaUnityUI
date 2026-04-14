using Calluna.DI;
using UnityEngine;

namespace Calluna.UI
{
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
