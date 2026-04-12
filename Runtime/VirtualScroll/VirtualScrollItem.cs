using Calluna.DI;

namespace Calluna.UI
{
    /// <summary>
    /// Base class for items managed by a <see cref="VirtualScrollGrid{TItem,TData}"/>.
    /// The pool delivers <typeparamref name="TData"/> via argument injection each time the
    /// item is (re-)activated; subclasses implement <see cref="OnInitialize"/> to apply it.
    /// </summary>
    public abstract class VirtualScrollItem<TData> : UnityEngine.MonoBehaviour, Injectable, Initializable, Cleanable
    {
        protected TData Data { get; private set; }

        void Injectable.Inject(Resolver resolver) => OnInject(resolver);
        void Initializable.Initialize()           => OnInitialize();
        void Cleanable.Clean()                    => OnClean();

        protected virtual void OnInject(Resolver resolver)
        {
            Data = resolver.Resolve<TData>();
        }

        /// <summary>Called after injection; apply <see cref="Data"/> to the cell's visuals.</summary>
        protected abstract void OnInitialize();

        protected virtual void OnClean() { }
    }
}
