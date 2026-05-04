using Calluna.DI;
using UnityEngine;

namespace Calluna.UI
{
    /// <summary>
    /// Installer that binds <see cref="IScrollLayout"/> with a <see cref="HorizontalListScrollLayout"/>
    /// configured from the serialized <see cref="HorizontalListScrollLayout.Settings"/> in the Inspector.
    ///
    /// Add directly to a GameObjectContext alongside a <c>MonoPoolInstaller</c> for the cell
    /// type and a separate <c>MonoInstaller</c> that binds your data list.
    /// </summary>
    public class VirtualScrollHorizontalListInstaller : VirtualScrollViewInstallerBase
    {
        [SerializeField] private HorizontalListScrollLayout.Settings _layoutSettings;

        protected override void InstallLayout(Binder binder)
        {
            binder.Bind<IScrollLayout>()
                  .ToInstance(new HorizontalListScrollLayout(_layoutSettings));
        }
    }
}
