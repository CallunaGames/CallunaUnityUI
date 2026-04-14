namespace Calluna.UI.Samples.VirtualScrollUI
{
    /// <summary>
    /// Concrete virtualised grid for <see cref="ItemCell"/> / <see cref="ItemData"/>.
    /// All behaviour lives in <see cref="VirtualScrollGrid{TItem,TData}"/>;
    /// this class only exists to give Unity a concrete type to attach to a GameObject.
    /// </summary>
    public class ItemGrid : VirtualScrollGrid<ItemCell, ItemData>
    {
    }
}
