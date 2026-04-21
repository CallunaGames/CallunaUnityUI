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

        private IUIBoundsConstrainer _boundsConstrainer;
        private RectTransform _transform;
        private RectTransform _constrainedRect;
        private RectTransform.Axis? _moveAxis;

        void Injectable.Inject(Resolver resolver)
        {
            Arguments args = resolver.Resolve<Arguments>();
            _boundsConstrainer = resolver.ResolveOptional<UIBoundsConstrainer>();
            _transform = args.TransformToDrag;
            _constrainedRect = args.BoundTransform == null ? args.TransformToDrag : args.BoundTransform;
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
            if (_boundsConstrainer == null)
                return delta;

            Rect? boundsNullable = _boundsConstrainer.GetBoundsRect();
            if (!boundsNullable.HasValue)
                return delta;

            Vector2 size = _constrainedRect.rect.size * (Vector2)_constrainedRect.lossyScale;
            Vector2 prospectivePosition = (Vector2)_constrainedRect.position + delta;
            Vector2 clampedPosition = UIBoundsConstrainer.ClampPositionToBounds(prospectivePosition, size, _constrainedRect.pivot, boundsNullable.Value);
            return clampedPosition - (Vector2)_constrainedRect.position;
        }

        public readonly struct Arguments
        {
            public readonly RectTransform TransformToDrag;
            public readonly RectTransform BoundTransform;
            public readonly RectTransform.Axis? MoveAxis;

            public Arguments(
                RectTransform transformToDrag,
                RectTransform boundTransform  = null,
                RectTransform.Axis? moveAxis  = null)
            {
                TransformToDrag = transformToDrag;
                BoundTransform  = boundTransform;
                MoveAxis        = moveAxis;
            }
        }
    }
}
