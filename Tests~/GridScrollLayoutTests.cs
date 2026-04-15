using NUnit.Framework;
using UnityEngine;

namespace Calluna.UI.Tests
{
    public class GridScrollLayoutTests
    {
        private static GridScrollLayout Make(int columns, float cellW, float cellH,
            float spacingX = 0, float spacingY = 0,
            float padTop = 0, float padBottom = 0, float padLeft = 0, float padRight = 0)
        {
            return new GridScrollLayout(new GridScrollLayout.Settings
            {
                Columns  = columns,
                CellSize = new Vector2(cellW, cellH),
                Spacing  = new Vector2(spacingX, spacingY),
                Padding  = new Padding { Top = padTop, Bottom = padBottom, Left = padLeft, Right = padRight }
            });
        }

        // ── ComputeContentSize ───────────────────────────────────────────────────

        [Test]
        public void GridScrollLayout_ComputeContentSize_ZeroItems_ReturnsZero()
        {
            var layout = Make(3, 100, 50);
            Assert.AreEqual(Vector2.zero, layout.ComputeContentSize(0));
        }

        [Test]
        public void GridScrollLayout_ComputeContentSize_SingleItem_ReturnsOneCellPlusPadding()
        {
            var layout = Make(3, 100, 50, padTop: 10, padBottom: 10, padLeft: 5, padRight: 5);
            Vector2 size = layout.ComputeContentSize(1);
            // width = padLeft + 3*100 + 2*0 + padRight = 5+300+5 = 310
            // height = padTop + 1*50 + 0*0 + padBottom = 10+50+10 = 70
            Assert.AreEqual(310f, size.x, 0.001f);
            Assert.AreEqual(70f,  size.y, 0.001f);
        }

        [Test]
        public void GridScrollLayout_ComputeContentSize_MultipleRows_HeightScalesWithRows()
        {
            var layout = Make(2, 100, 50, spacingY: 10);
            Vector2 size = layout.ComputeContentSize(4); // 2 rows
            // height = 2*50 + 1*10 = 110
            Assert.AreEqual(110f, size.y, 0.001f);
        }

        // ── ComputeItemPosition ──────────────────────────────────────────────────

        [Test]
        public void GridScrollLayout_ComputeItemPosition_FirstItem_ReturnsOriginPlusPadding()
        {
            var layout = Make(3, 100, 50, padTop: 10, padLeft: 5);
            Vector2 pos = layout.ComputeItemPosition(0);
            Assert.AreEqual(5f,   pos.x, 0.001f);
            Assert.AreEqual(-10f, pos.y, 0.001f);
        }

        [Test]
        public void GridScrollLayout_ComputeItemPosition_SecondColumn_OffsetByStepX()
        {
            var layout = Make(3, 100, 50, spacingX: 10);
            Vector2 pos = layout.ComputeItemPosition(1); // col 1, row 0
            Assert.AreEqual(110f, pos.x, 0.001f);
            Assert.AreEqual(0f,   pos.y, 0.001f);
        }

        [Test]
        public void GridScrollLayout_ComputeItemPosition_SecondRow_OffsetByStepY()
        {
            var layout = Make(3, 100, 50, spacingY: 10);
            Vector2 pos = layout.ComputeItemPosition(3); // col 0, row 1
            Assert.AreEqual(0f,   pos.x, 0.001f);
            Assert.AreEqual(-60f, pos.y, 0.001f); // -(50+10)
        }

        // ── GetVisibleIndexRange ─────────────────────────────────────────────────

        [Test]
        public void GridScrollLayout_GetVisibleIndexRange_ZeroItems_ReturnsEmpty()
        {
            var layout = Make(3, 100, 50);
            (int first, int last) = layout.GetVisibleIndexRange(0, new Rect(0, -200, 300, 200));
            Assert.Greater(first, last);
        }

        [Test]
        public void GridScrollLayout_GetVisibleIndexRange_FullyVisible_ReturnsAll()
        {
            var layout = Make(3, 100, 50);
            // 6 items = 2 rows, content height = 100
            // viewport covers full content from y=0 to y=-100
            (int first, int last) = layout.GetVisibleIndexRange(6, new Rect(0, -100, 300, 100));
            Assert.AreEqual(0, first);
            Assert.AreEqual(5, last);
        }

        [Test]
        public void GridScrollLayout_GetVisibleIndexRange_ScrolledPastFirstRow_ExcludesFirstRow()
        {
            var layout = Make(3, 100, 50);
            // Row 0 bottom edge is at y=-50. Shifting the viewport 1px below that (yMax=-51)
            // ensures row 0 is fully out of view.
            (int first, int last) = layout.GetVisibleIndexRange(6, new Rect(0, -101, 300, 50));
            Assert.AreEqual(3, first);
            Assert.AreEqual(5, last);
        }
    }
}
