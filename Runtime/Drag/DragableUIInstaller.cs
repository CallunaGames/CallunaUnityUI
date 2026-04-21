using Calluna.DI;
using UnityEngine;

namespace Calluna.UI
{
    public class DragableUIInstaller : MonoInstaller
    {
        [SerializeField] private RectTransform _dragableTransform;
        [SerializeField, Header("Optional")] private RectTransform _boundTransform;
        [SerializeField] private bool _limitAxis;
        [SerializeField] private RectTransform.Axis _moveAxis;

        public override void InstallBindings(Binder binder)
        {
            binder.BindInstance(new DragableUI.Arguments(
                transformToDrag: _dragableTransform,
                boundTransform:  _boundTransform,
                moveAxis:        _limitAxis ? _moveAxis : null));
        }
    }
}