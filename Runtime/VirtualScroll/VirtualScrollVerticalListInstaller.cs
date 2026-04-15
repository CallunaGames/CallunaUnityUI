using Calluna.DI;
using UnityEngine;

namespace Calluna.UI
{
    /// <summary>
    /// Installer that binds <see cref="IScrollLayout"/> with a <see cref="VerticalListScrollLayout"/>
    /// configured from the serialized <see cref="VerticalListScrollLayout.Settings"/> in the Inspector.
    ///
    /// Create a one-line concrete subclass to make the component attachable in the Unity Editor:
    /// <code>
    /// public class MyListInstaller : VirtualScrollVerticalListInstaller { }
    /// </code>
    /// Pair with a <c>MonoPoolInstaller</c> for the cell type and bind your data list separately.
    /// </summary>
    public abstract class VirtualScrollVerticalListInstaller : MonoInstaller
    {
        [SerializeField] private VerticalListScrollLayout.Settings _layoutSettings;

        public override void InstallBindings(Binder binder)
        {
            binder.Bind<IScrollLayout>()
                  .ToInstance(new VerticalListScrollLayout(_layoutSettings));
        }
    }
}
