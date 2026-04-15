using System;
using Calluna.DI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Calluna.UI
{
    public class DragableUI : MonoBehaviour, Injectable, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        public event Action<Vector2> OnDragStart;
        public event Action<Vector2> OnDragEnd;

        private RectTransform _transform;
        private RectTransform _boundsRegion;
        private RectTransform _draggedRect;
        private RectTransform.Axis? _moveAxis;

        void Injectable.Inject(Resolver resolver)
        {
            Arguments args = resolver.Resolve<Arguments>();
            _transform = args.TransformToDrag;
            _boundsRegion = args.Bounds;
            _draggedRect = args.BoundTransform == null ? args.TransformToDrag : args.BoundTransform;
            _moveAxis = args.MoveAxis;
        }

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            OnDragStart?.Invoke(_transform.position);
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            OnDragEnd?.Invoke(_transform.position);
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;
            Vector2 delta = new Vector2(
                _moveAxis is null or RectTransform.Axis.Horizontal ? eventData.delta.x : 0,
                _moveAxis is null or RectTransform.Axis.Vertical ? eventData.delta.y : 0);
            _transform.position += (Vector3)LimitToBounds(delta);
        }

        protected virtual Vector2 LimitToBounds(Vector2 delta)
        {
            Rect? boundsNullable = GetBoundsRect();
            if (!boundsNullable.HasValue)
                return delta;

            Vector2 size = _draggedRect.rect.size * (Vector2)_draggedRect.lossyScale;
            Vector2 prospectivePosition = (Vector2)_draggedRect.position + delta;
            Vector2 clampedPosition = ClampPositionToBounds(prospectivePosition, size, _draggedRect.pivot, boundsNullable.Value);
            return clampedPosition - (Vector2)_draggedRect.position;
        }

        // Pure geometry — no Unity object access. Testable without a RectTransform.
        // Computes how far each edge of the element overshoots the bounds and subtracts
        // those overflows from the position to push it back inside.
        internal static Vector2 ClampPositionToBounds(Vector2 position, Vector2 size, Vector2 pivot, Rect bounds)
        {
            float minX = position.x - size.x * pivot.x;
            float maxX = position.x + size.x * (1 - pivot.x);
            float minY = position.y - size.y * pivot.y;
            float maxY = position.y + size.y * (1 - pivot.y);

            float deltaMinX = minX < bounds.min.x ? minX - bounds.min.x : 0;
            float deltaMinY = minY < bounds.min.y ? minY - bounds.min.y : 0;
            float deltaMaxX = maxX > bounds.max.x ? maxX - bounds.max.x : 0;
            float deltaMaxY = maxY > bounds.max.y ? maxY - bounds.max.y : 0;

            return position - new Vector2(deltaMinX, deltaMinY) - new Vector2(deltaMaxX, deltaMaxY);
        }

        protected virtual Rect? GetBoundsRect()
        {
            if (_boundsRegion == null)
                return null;
            Rect bounds = _boundsRegion.rect;
            bounds.size = Vector2.Scale(bounds.size, _boundsRegion.lossyScale);
            bounds.position = (Vector2)_boundsRegion.position - (bounds.size * _boundsRegion.pivot);
            return bounds;
        }

        public readonly struct Arguments
        {
            public readonly RectTransform TransformToDrag;
            public readonly RectTransform Bounds;
            public readonly RectTransform BoundTransform;
            public readonly RectTransform.Axis? MoveAxis;

            public Arguments(
                RectTransform transformToDrag,
                RectTransform bounds          = null,
                RectTransform boundTransform  = null,
                RectTransform.Axis? moveAxis  = null)
            {
                TransformToDrag = transformToDrag;
                Bounds          = bounds;
                BoundTransform  = boundTransform;
                MoveAxis        = moveAxis;
            }
        }
    }
}
