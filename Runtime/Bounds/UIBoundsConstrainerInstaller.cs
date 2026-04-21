using Calluna.DI;
using UnityEngine;

namespace Calluna.UI
{
    public class UIBoundsConstrainerInstaller : MonoInstaller
    {
        [SerializeField, Header("Optional")] private RectTransform _bounds;

        public override void InstallBindings(Binder binder)
        {
            binder.BindInstance(new UIBoundsConstrainer.Arguments(_bounds));
            binder.BindComponent<UIBoundsConstrainer>()
                .FromNewComponentOnNewGameObject("UIBoundsConstrainer", transform)
                .AsSingle();
        }
    }
}
