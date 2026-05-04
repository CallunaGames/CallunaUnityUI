using Calluna.DI;
using UnityEngine;

namespace Calluna.UI
{
    /// <summary>
    /// Installer that binds <see cref="IScrollLayout"/> with a <see cref="GridScrollLayout"/>
    /// configured from the serialized <see cref="GridScrollLayout.Settings"/> in the Inspector.
    ///
    /// Add directly to a GameObjectContext alongside a <c>MonoPoolInstaller</c> for the cell
    /// type and a separate <c>MonoInstaller</c> that binds your data list.
    /// </summary>
    public class VirtualScrollGridInstaller : VirtualScrollViewInstallerBase
    {
        [SerializeField] private GridScrollLayout.Settings _layoutSettings;

        protected override void InstallLayout(Binder binder)
        {
            binder.Bind<IScrollLayout>()
                  .ToInstance(new GridScrollLayout(_layoutSettings));
        }
    }
}
