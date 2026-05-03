using Calluna.DI;
using UnityEngine;
using UnityEngine.UI;

namespace Calluna.UI
{
    public class TextInputVisualArgsInstaller : MonoInstaller
    {
        [SerializeField] private Graphic _target;
        [SerializeField, Header("Optional")] private Color _invalidColor = Color.red;

        private void Reset()
        {
            _target = GetComponentInChildren<Graphic>();
        }

        public override void InstallBindings(Binder binder)
        {
            binder.BindInstance(new TextInputVisualArgs(_target, _invalidColor));
        }
    }
}
