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
        private RectTransform _boundTransform;
        private RectTransform.Axis? _moveAxis;
        private Rect? _bounds;

        void Injectable.Inject(Resolver resolver)
        {
            Arguments args = resolver.Resolve<Arguments>();
            _transform = args.TransformToDrag;
            _boundsTransform = args.Bounds;
            _boundTransform = args.BoundTransform == null ? args.TransformToDrag : args.BoundTransform;
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
            _transform.position += (Vector3)LimitToBounds(new Vector2(delta.x, delta.y));
        }

        private Vector2 LimitToBounds(Vector2 delta)
        {
            _bounds = GetBoundsRect();

            if (!_bounds.HasValue)
                return delta;
            
            Vector2 position = (Vector2)_boundTransform.position + delta;
            Rect rect = _boundTransform.rect;
            Vector2 pivot = _boundTransform.pivot;
            Vector2 scale = _boundTransform.lossyScale;
            Vector2 size = rect.size * scale;
            float minX = position.x - size.x * pivot.x;
            float maxX = position.x + size.x * (1 - pivot.x);
            float minY = position.y - size.y * pivot.y;
            float maxY = position.y + size.y * (1 - pivot.y);
            Vector2 min = new Vector2(minX, minY);
            Vector2 max = new Vector2(maxX, maxY);

            Rect bounds = _bounds.Value;
            float deltaMinX = min.x < bounds.min.x ? min.x - bounds.min.x : 0;
            float deltaMinY = min.y < bounds.min.y ? min.y - bounds.min.y : 0;
            float deltaMaxX = max.x > bounds.max.x ? max.x - bounds.max.x : 0;
            float deltaMaxY = max.y > bounds.max.y ? max.y - bounds.max.y : 0;

            Vector2 deltaMin = new Vector2(deltaMinX, deltaMinY);
            Vector2 deltaMax = new Vector2(deltaMaxX, deltaMaxY);

            return delta - deltaMin - deltaMax;
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
            public RectTransform BoundTransform;
            public RectTransform.Axis? MoveAxis;
        }
    }
}
