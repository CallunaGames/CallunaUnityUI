using Calluna.DI;

namespace Calluna.UI.Samples.VirtualScrollUI
{
    /// <summary>
    /// Binds the shared data list for the Virtual Scroll sample.
    /// Add this alongside <see cref="VirtualScrollGridInstaller"/> on the same GameObjectContext.
    /// </summary>
    public class SceneInstaller : MonoInstaller
    {
        private readonly ObservableList<ItemData> _items = new();

        public override void InstallBindings(Binder binder)
        {
            binder.Bind<ObservableList<ItemData>>()
                .And<ReadonlyObservableList<ItemData>>()
                .ToInstance(_items);
            binder.BindComponent<CoroutineHelper>()
                .FromNewComponentOnNewGameObject("CoroutineHelper", transform)
                .AsSingle();
        }
    }
}