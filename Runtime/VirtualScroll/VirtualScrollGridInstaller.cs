using Calluna.DI;
using UnityEngine;

namespace Calluna.UI
{
    /// <summary>
    /// Installer that binds <see cref="IScrollLayout"/> with a <see cref="GridScrollLayout"/>
    /// configured from the serialized <see cref="GridScrollLayout.Settings"/> in the Inspector.
    ///
    /// Create a one-line concrete subclass to make the component attachable in the Unity Editor:
    /// <code>
    /// public class MyGridInstaller : VirtualScrollGridInstaller { }
    /// </code>
    /// Pair with a <c>MonoPoolInstaller</c> for the cell type and bind your data list separately.
    /// </summary>
    public abstract class VirtualScrollGridInstaller : MonoInstaller
    {
        [SerializeField] private GridScrollLayout.Settings _layoutSettings;

        public override void InstallBindings(Binder binder)
        {
            binder.Bind<IScrollLayout>()
                  .ToInstance(new GridScrollLayout(_layoutSettings));
        }
    }
}
