using Calluna.DI;

namespace Calluna.UI.Samples.VirtualScrollUI
{
    /// <summary>
    /// Concrete installer for the Virtual Scroll sample.
    /// Inheriting <see cref="VirtualScrollGridInstaller"/> handles the <see cref="IScrollLayout"/> binding.
    /// This subclass adds the data list so the grid has a source to react to.
    /// </summary>
    public class SceneInstaller : VirtualScrollGridInstaller
    {
        private readonly ObservableList<ItemData> _items = new();

        public override void InstallBindings(Binder binder)
        {
            base.InstallBindings(binder);

            binder.Bind<ObservableList<ItemData>>()
                  .And<ReadonlyObservableList<ItemData>>()
                  .ToInstance(_items);
        }
    }
}
