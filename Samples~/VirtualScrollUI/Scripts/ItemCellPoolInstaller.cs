using Calluna.DI;

namespace Calluna.UI.Samples.VirtualScrollUI
{
    /// <summary>
    /// Installs the <see cref="ItemCell"/> pool.
    /// Attach to the same Context as <see cref="SceneInstaller"/> and assign the ItemCell prefab.
    /// </summary>
    public class ItemCellPoolInstaller : MonoPoolInstaller<ItemCell, ItemData>
    {
    }
}
