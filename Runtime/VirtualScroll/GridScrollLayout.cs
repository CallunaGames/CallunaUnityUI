using System;
using UnityEngine;

namespace Calluna.UI
{
    /// <summary>
    /// IScrollLayout implementation for a top-to-bottom grid with a fixed column count.
    /// All methods are pure arithmetic — no Unity object access, fully unit-testable.
    /// </summary>
    public class GridScrollLayout : IScrollLayout
    {
        private readonly Settings _settings;

        // Pre-computed per construction to avoid repeated arithmetic in hot paths.
        private readonly float _cellStepX;
        private readonly float _cellStepY;

        public GridScrollLayout(Settings settings)
        {
            if (settings.Columns < 1)
                throw new ArgumentException("Columns must be at least 1.", nameof(settings));

            _settings = settings;
            _cellStepX = settings.CellSize.x + settings.Spacing.x;
            _cellStepY = settings.CellSize.y + settings.Spacing.y;
        }

        public Vector2 ItemSize => _settings.CellSize;

        public Vector2 ComputeContentSize(int itemCount)
        {
            if (itemCount <= 0)
                return Vector2.zero;

            int rows = Mathf.CeilToInt((float)itemCount / _settings.Columns);
            float width  = _settings.Padding.Left + _settings.Columns * _settings.CellSize.x
                         + (_settings.Columns - 1) * _settings.Spacing.x + _settings.Padding.Right;
            float height = _settings.Padding.Top  + rows * _settings.CellSize.y
                         + (rows - 1) * _settings.Spacing.y + _settings.Padding.Bottom;
            return new Vector2(width, height);
        }

        public Vector2 ComputeItemPosition(int index)
        {
            int col = index % _settings.Columns;
            int row = index / _settings.Columns;
            // anchoredPosition with top-left anchor/pivot: x grows right, y grows downward (negative).
            float x =  _settings.Padding.Left + col * _cellStepX;
            float y = -(_settings.Padding.Top  + row * _cellStepY);
            return new Vector2(x, y);
        }

        public (int first, int last) GetVisibleIndexRange(int itemCount, Rect viewportLocalRect)
        {
            if (itemCount <= 0)
                return (0, -1);

            // Items are positioned with negative y (anchoredPosition, top-left pivot, y-up):
            //   rowTop_i    = -(padding.top + i * cellStepY)
            //   rowBottom_i = -(padding.top + i * cellStepY + cellSize.y)
            //
            // viewportLocalRect uses Unity y-up convention: yMax is the top of the viewport
            // (less negative), yMin is the bottom (more negative).
            float viewTop    = viewportLocalRect.yMax;
            float viewBottom = viewportLocalRect.yMin;

            // First visible row: smallest i where rowBottom_i <= viewTop
            //   i >= (-viewTop - padding.top - cellSize.y) / cellStepY
            int firstRow = Mathf.Max(0, Mathf.CeilToInt(
                (-viewTop - _settings.Padding.Top - _settings.CellSize.y) / _cellStepY));

            // Last visible row: largest i where rowTop_i >= viewBottom
            //   i <= (-viewBottom - padding.top) / cellStepY
            int lastRow = Mathf.Max(0, Mathf.FloorToInt(
                (-viewBottom - _settings.Padding.Top) / _cellStepY));

            int first = firstRow * _settings.Columns;
            int last  = Mathf.Min(lastRow * _settings.Columns + (_settings.Columns - 1), itemCount - 1);
            first     = Mathf.Min(first, itemCount - 1);

            return (first, last);
        }

        [Serializable]
        public struct Settings
        {
            public int     Columns;
            public Vector2 CellSize;
            public Vector2 Spacing;
            public Padding Padding;
        }
    }

}
