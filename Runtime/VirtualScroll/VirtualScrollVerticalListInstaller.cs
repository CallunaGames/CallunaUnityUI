using Calluna.DI;
using UnityEngine;

namespace Calluna.UI
{
    /// <summary>
    /// Installer that binds <see cref="IScrollLayout"/> with a <see cref="VerticalListScrollLayout"/>
    /// configured from the serialized <see cref="VerticalListScrollLayout.Settings"/> in the Inspector.
    ///
    /// Add directly to a GameObjectContext alongside a <c>MonoPoolInstaller</c> for the cell
    /// type and a separate <c>MonoInstaller</c> that binds your data list.
    /// </summary>
    public class VirtualScrollVerticalListInstaller : VirtualScrollViewInstallerBase
    {
        [SerializeField] private VerticalListScrollLayout.Settings _layoutSettings;

        protected override void InstallLayout(Binder binder)
        {
            binder.Bind<IScrollLayout>()
                  .ToInstance(new VerticalListScrollLayout(_layoutSettings));
        }
    }
}
