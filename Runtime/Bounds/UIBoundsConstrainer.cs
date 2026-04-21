using Calluna.DI;
using UnityEngine;

namespace Calluna.UI
{
    public class UIBoundsConstrainer : MonoBehaviour, Injectable, IUIBoundsConstrainer
    {
        private RectTransform _boundsRegion;

        void Injectable.Inject(Resolver resolver)
        {
            Arguments args = resolver.Resolve<Arguments>();
            _boundsRegion = args.Bounds;
        }

        public virtual void Clamp(RectTransform target)
        {
            Rect? boundsNullable = GetBoundsRect();
            if (!boundsNullable.HasValue)
                return;

            Vector2 size = target.rect.size * (Vector2)target.lossyScale;
            Vector2 clamped = ClampPositionToBounds((Vector2)target.position, size, target.pivot, boundsNullable.Value);
            target.position = new Vector3(clamped.x, clamped.y, target.position.z);
        }

        public virtual Rect? GetBoundsRect()
        {
            if (_boundsRegion == null)
                return null;
            Rect bounds = _boundsRegion.rect;
            bounds.size = Vector2.Scale(bounds.size, _boundsRegion.lossyScale);
            bounds.position = (Vector2)_boundsRegion.position - (bounds.size * _boundsRegion.pivot);
            return bounds;
        }

        public static Vector2 ClampPositionToBounds(Vector2 position, Vector2 size, Vector2 pivot, Rect bounds)
        {
            // Expand the pivot-anchored position into world-space edges of the rect.
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

        public readonly struct Arguments
        {
            public readonly RectTransform Bounds;

            public Arguments(RectTransform bounds)
            {
                Bounds = bounds;
            }
        }
    }
}
