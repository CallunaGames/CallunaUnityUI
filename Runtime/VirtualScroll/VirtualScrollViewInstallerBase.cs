using Calluna;
using Calluna.DI;

namespace Calluna.UI
{
    /// <summary>
    /// Base installer for all VirtualScrollView-family installers.
    /// Binds <see cref="IScrollLayout"/> (via <see cref="InstallLayout"/>) and, under
    /// <see cref="VirtualScrollBase.ScrollTweenerId"/>, a <see cref="FloatValueTweener"/> as
    /// <c>ValueTweener&lt;float&gt;</c> for optional animated scrolling.
    ///
    /// Implements <see cref="Injectable"/> so the <see cref="CoroutineHelper"/> can be resolved
    /// from the parent context and forwarded to the tweener's constructor before bindings are installed.
    ///
    /// Subclasses may override <see cref="InstallLayout"/> to add a layout binding, or
    /// add additional bindings by overriding <c>InstallBindings</c> and calling <c>base</c>.
    /// </summary>
    public class VirtualScrollViewInstallerBase : MonoInstaller, Injectable
    {
        private CoroutineHelper _coroutineHelper;

        void Injectable.Inject(Resolver resolver)
        {
            _coroutineHelper = resolver.Resolve<CoroutineHelper>();
        }

        public override void InstallBindings(Binder binder)
        {
            binder.Bind<ValueTweener<float>>(VirtualScrollBase.ScrollTweenerId)
                  .To<FloatValueTweener>()
                  .FromMethod(() => new FloatValueTweener(_coroutineHelper))
                  .AsSingle();
            InstallLayout(binder);
        }

        protected virtual void InstallLayout(Binder binder) { }
    }
}
