using Calluna.DI;
using UnityEngine;

namespace Calluna.UI.Samples
{
    public class ColorStyleInstaller : MonoInstaller
    {
        [SerializeField] private ColorStyleSettings _colorStyleSettings;
        
        public override void InstallBindings(Binder binder)
        {
            binder.BindInstance(_colorStyleSettings);
        }
    }
}
