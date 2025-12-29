using Calluna.DI;
using UnityEngine;

namespace Calluna.UI
{
    public class DragableUIInstaller : MonoInstaller
    {
        [SerializeField] private RectTransform _dragableTransform;
        [SerializeField, Header("Optional")] private RectTransform _bounds;

        public override void InstallBindings(Binder binder)
        {
            binder.BindInstance(
                new DragableUI.Arguments()
                {
                    TransformToDrag = _dragableTransform,
                    Bounds = _bounds
                });
        }
    }
}