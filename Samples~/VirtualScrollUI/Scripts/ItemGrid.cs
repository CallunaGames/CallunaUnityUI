namespace Calluna.UI.Samples.VirtualScrollUI
{
    /// <summary>
    /// Concrete virtualised scroll view for <see cref="ItemCell"/> / <see cref="ItemData"/>.
    /// All behaviour lives in <see cref="VirtualScrollView{TItem,TData}"/>;
    /// this class only exists to give Unity a concrete type to attach to a GameObject.
    /// </summary>
    public class ItemGrid : VirtualScrollView<ItemCell, ItemData>
    {
    }
}
