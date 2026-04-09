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
        private RectTransform _boundsTransform;

        void Injectable.Inject(Resolver resolver)
        {
            Arguments args = resolver.Resolve<Arguments>();
            _transform = args.TransformToDrag;
            _boundsTransform = args.Bounds;
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
            Vector2 delta = eventData.delta;
            _transform.position = LimitToBounds(_transform.position + new Vector3(delta.x, delta.y));
        }

        private Vector2 LimitToBounds(Vector2 position)
        {
            Rect? boundsNullable = GetBoundsRect();
            if (!boundsNullable.HasValue)
                return position;

            Vector2 size = _transform.rect.size * (Vector2)_transform.lossyScale;
            return ClampPositionToBounds(position, size, _transform.pivot, boundsNullable.Value);
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

        private Rect? GetBoundsRect()
        {
            if (_boundsTransform == null)
                return null;
            Rect bounds = _boundsTransform.rect;
            bounds.size = Vector2.Scale(bounds.size, _boundsTransform.lossyScale);
            bounds.position = (Vector2)_boundsTransform.position - (bounds.size * _boundsTransform.pivot);
            return bounds;
        }

        public struct Arguments
        {
            public RectTransform TransformToDrag;
            public RectTransform Bounds;
        }
    }
}
