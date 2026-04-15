using System;
using UnityEngine;

namespace Calluna.UI
{
    /// <summary>
    /// <see cref="IScrollLayout"/> implementation for a single-column vertical list.
    /// Items are stacked top-to-bottom with a uniform spacing between them.
    /// All methods are pure arithmetic — no Unity object access, fully unit-testable.
    /// </summary>
    public class VerticalListScrollLayout : IScrollLayout
    {
        private readonly Settings _settings;
        private readonly float _itemStep;

        public VerticalListScrollLayout(Settings settings)
        {
            if (settings.ItemSize.y <= 0)
                throw new ArgumentException("ItemSize.y must be greater than 0.", nameof(settings));

            _settings = settings;
            _itemStep  = settings.ItemSize.y + settings.Spacing;
        }

        public Vector2 ItemSize => _settings.ItemSize;

        public Vector2 ComputeContentSize(int itemCount)
        {
            if (itemCount <= 0)
                return Vector2.zero;

            float width  = _settings.Padding.Left + _settings.ItemSize.x + _settings.Padding.Right;
            float height = _settings.Padding.Top
                         + itemCount * _settings.ItemSize.y
                         + (itemCount - 1) * _settings.Spacing
                         + _settings.Padding.Bottom;
            return new Vector2(width, height);
        }

        public Vector2 ComputeItemPosition(int index)
        {
            // anchoredPosition with top-left anchor/pivot: x grows right, y grows downward (negative).
            float x =  _settings.Padding.Left;
            float y = -(_settings.Padding.Top + index * _itemStep);
            return new Vector2(x, y);
        }

        public (int first, int last) GetVisibleIndexRange(int itemCount, Rect viewportLocalRect)
        {
            if (itemCount <= 0)
                return (0, -1);

            // Items are positioned with negative y (top-left anchor/pivot, y-up):
            //   itemTop_i    = -(padding.top + i * itemStep)
            //   itemBottom_i = -(padding.top + i * itemStep + itemSize.y)
            //
            // viewportLocalRect uses Unity y-up: yMax is the top of the viewport (less negative),
            // yMin is the bottom (more negative).
            float viewTop    = viewportLocalRect.yMax;
            float viewBottom = viewportLocalRect.yMin;

            // First visible: smallest i where itemBottom_i <= viewTop
            //   i >= (-viewTop - padding.top - itemSize.y) / itemStep
            int first = Mathf.Max(0, Mathf.CeilToInt(
                (-viewTop - _settings.Padding.Top - _settings.ItemSize.y) / _itemStep));

            // Last visible: largest i where itemTop_i >= viewBottom
            //   i <= (-viewBottom - padding.top) / itemStep
            int last = Mathf.FloorToInt(
                (-viewBottom - _settings.Padding.Top) / _itemStep);

            first = Mathf.Min(first, itemCount - 1);
            last  = Mathf.Clamp(last, 0, itemCount - 1);
            return (first, last);
        }

        [Serializable]
        public struct Settings
        {
            public Vector2 ItemSize;
            public float   Spacing;
            public Padding Padding;
        }
    }
}
