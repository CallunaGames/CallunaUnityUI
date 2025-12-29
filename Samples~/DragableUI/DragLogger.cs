using Calluna.DI;
using UnityEngine;

namespace Calluna.UI.DragableSample
{
    public class DragLogger : MonoBehaviour, Initializable, Cleanable
    {
        [SerializeField] private DragableUI _dragableUI;
        
        void Initializable.Initialize()
        {
            _dragableUI.OnDragStart += LogDragStart;
            _dragableUI.OnDragEnd += LogDragEnd;
        }

        void Cleanable.Clean()
        {
            _dragableUI.OnDragStart -= LogDragStart;
            _dragableUI.OnDragEnd -= LogDragEnd;
        }

        private void LogDragStart(Vector2 position)
        {
            Debug.Log($"Started dragging {name} from {position}...");
        }

        private void LogDragEnd(Vector2 position)
        {
            Debug.Log($"...to {position}");
        }
    }
}
