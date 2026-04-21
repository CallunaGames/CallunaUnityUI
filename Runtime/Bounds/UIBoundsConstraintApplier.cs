using Calluna.DI;
using UnityEngine;

namespace Calluna.UI
{
    public class UIBoundsConstraintApplier : MonoBehaviour, Injectable
    {
        [SerializeField] private RectTransform _target;

        private UIBoundsConstrainer _constrainer;

        void Injectable.Inject(Resolver resolver)
        {
            _constrainer = resolver.Resolve<UIBoundsConstrainer>();
        }

        private void LateUpdate()
        {
            if (_target == null)
                return;
            _constrainer.Clamp(_target);
        }

        private void Reset()
        {
            _target = GetComponent<RectTransform>();
        }
    }
}
