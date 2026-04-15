using System;
using UnityEngine;

namespace Calluna.UI
{
    /// <summary>
    /// <see cref="IScrollLayout"/> implementation for a single-row horizontal list.
    /// Items are placed left-to-right with a uniform spacing between them.
    /// All methods are pure arithmetic — no Unity object access, fully unit-testable.
    /// </summary>
    public class HorizontalListScrollLayout : IScrollLayout
    {
        private readonly Settings _settings;
        private readonly float _itemStep;

        public HorizontalListScrollLayout(Settings settings)
        {
            if (settings.ItemSize.x <= 0)
                throw new ArgumentException("ItemSize.x must be greater than 0.", nameof(settings));

            _settings = settings;
            _itemStep  = settings.ItemSize.x + settings.Spacing;
        }

        public Vector2 ItemSize => _settings.ItemSize;

        public Vector2 ComputeContentSize(int itemCount)
        {
            if (itemCount <= 0)
                return Vector2.zero;

            float width  = _settings.Padding.Left
                         + itemCount * _settings.ItemSize.x
                         + (itemCount - 1) * _settings.Spacing
                         + _settings.Padding.Right;
            float height = _settings.Padding.Top + _settings.ItemSize.y + _settings.Padding.Bottom;
            return new Vector2(width, height);
        }

        public Vector2 ComputeItemPosition(int index)
        {
            // anchoredPosition with top-left anchor/pivot: x grows right, y grows downward (negative).
            float x =  _settings.Padding.Left + index * _itemStep;
            float y = -_settings.Padding.Top;
            return new Vector2(x, y);
        }

        public (int first, int last) GetVisibleIndexRange(int itemCount, Rect viewportLocalRect)
        {
            if (itemCount <= 0)
                return (0, -1);

            // Items are positioned along the x-axis (top-left anchor/pivot):
            //   itemLeft_i  = padding.left + i * itemStep
            //   itemRight_i = padding.left + i * itemStep + itemSize.x
            //
            // viewportLocalRect uses Unity y-up: xMin is the left of the viewport, xMax is the right.
            float viewLeft  = viewportLocalRect.xMin;
            float viewRight = viewportLocalRect.xMax;

            // First visible: smallest i where itemRight_i >= viewLeft
            //   i >= (viewLeft - padding.left - itemSize.x) / itemStep
            int first = Mathf.Max(0, Mathf.CeilToInt(
                (viewLeft - _settings.Padding.Left - _settings.ItemSize.x) / _itemStep));

            // Last visible: largest i where itemLeft_i <= viewRight
            //   i <= (viewRight - padding.left) / itemStep
            int last = Mathf.FloorToInt(
                (viewRight - _settings.Padding.Left) / _itemStep);

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
