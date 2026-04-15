using NUnit.Framework;
using UnityEngine;

namespace Calluna.UI.Tests
{
    public class VerticalListScrollLayoutTests
    {
        private static VerticalListScrollLayout Make(float itemW, float itemH,
            float spacing = 0,
            float padTop = 0, float padBottom = 0, float padLeft = 0, float padRight = 0)
        {
            return new VerticalListScrollLayout(new VerticalListScrollLayout.Settings
            {
                ItemSize = new Vector2(itemW, itemH),
                Spacing  = spacing,
                Padding  = new Padding { Top = padTop, Bottom = padBottom, Left = padLeft, Right = padRight }
            });
        }

        // ── ComputeContentSize ───────────────────────────────────────────────────

        [Test]
        public void VerticalListScrollLayout_ComputeContentSize_ZeroItems_ReturnsZero()
        {
            var layout = Make(200, 50);
            Assert.AreEqual(Vector2.zero, layout.ComputeContentSize(0));
        }

        [Test]
        public void VerticalListScrollLayout_ComputeContentSize_SingleItem_ReturnsItemSizePlusPadding()
        {
            var layout = Make(200, 50, padTop: 10, padBottom: 10, padLeft: 5, padRight: 5);
            Vector2 size = layout.ComputeContentSize(1);
            Assert.AreEqual(210f, size.x, 0.001f); // 5+200+5
            Assert.AreEqual(70f,  size.y, 0.001f); // 10+50+10
        }

        [Test]
        public void VerticalListScrollLayout_ComputeContentSize_MultipleItems_HeightIncludesSpacing()
        {
            var layout = Make(200, 50, spacing: 10);
            Vector2 size = layout.ComputeContentSize(3);
            // height = 3*50 + 2*10 = 170
            Assert.AreEqual(170f, size.y, 0.001f);
        }

        [Test]
        public void VerticalListScrollLayout_ComputeContentSize_WidthIsConstant()
        {
            var layout = Make(200, 50);
            Assert.AreEqual(layout.ComputeContentSize(1).x, layout.ComputeContentSize(10).x, 0.001f);
        }

        // ── ComputeItemPosition ──────────────────────────────────────────────────

        [Test]
        public void VerticalListScrollLayout_ComputeItemPosition_FirstItem_ReturnsOriginPlusPadding()
        {
            var layout = Make(200, 50, padTop: 10, padLeft: 5);
            Vector2 pos = layout.ComputeItemPosition(0);
            Assert.AreEqual(5f,   pos.x, 0.001f);
            Assert.AreEqual(-10f, pos.y, 0.001f);
        }

        [Test]
        public void VerticalListScrollLayout_ComputeItemPosition_XIsConstantAcrossItems()
        {
            var layout = Make(200, 50, padLeft: 8);
            Assert.AreEqual(layout.ComputeItemPosition(0).x, layout.ComputeItemPosition(5).x, 0.001f);
        }

        [Test]
        public void VerticalListScrollLayout_ComputeItemPosition_YDecreasesByStepEachItem()
        {
            var layout = Make(200, 50, spacing: 10);
            float step = 60f; // itemH + spacing
            Assert.AreEqual(layout.ComputeItemPosition(0).y - step, layout.ComputeItemPosition(1).y, 0.001f);
            Assert.AreEqual(layout.ComputeItemPosition(1).y - step, layout.ComputeItemPosition(2).y, 0.001f);
        }

        // ── GetVisibleIndexRange ─────────────────────────────────────────────────

        [Test]
        public void VerticalListScrollLayout_GetVisibleIndexRange_ZeroItems_ReturnsEmpty()
        {
            var layout = Make(200, 50);
            (int first, int last) = layout.GetVisibleIndexRange(0, new Rect(0, -200, 200, 200));
            Assert.Greater(first, last);
        }

        [Test]
        public void VerticalListScrollLayout_GetVisibleIndexRange_FullyVisible_ReturnsAll()
        {
            var layout = Make(200, 50);
            // 3 items, content height = 150. Viewport covers full content.
            (int first, int last) = layout.GetVisibleIndexRange(3, new Rect(0, -150, 200, 150));
            Assert.AreEqual(0, first);
            Assert.AreEqual(2, last);
        }

        [Test]
        public void VerticalListScrollLayout_GetVisibleIndexRange_ScrolledPastFirstItem_ExcludesIt()
        {
            var layout = Make(200, 50, spacing: 0);
            // 3 items, each 50px. Viewport shows items 1 and 2 (y: -50 to -150).
            (int first, int last) = layout.GetVisibleIndexRange(3, new Rect(0, -150, 200, 100));
            Assert.AreEqual(1, first);
            Assert.AreEqual(2, last);
        }

        [Test]
        public void VerticalListScrollLayout_GetVisibleIndexRange_ViewportShowsPartialItem_IncludesIt()
        {
            var layout = Make(200, 50, spacing: 0);
            // Viewport shows y: -25 to -75 — partially overlaps item 0 and item 1.
            (int first, int last) = layout.GetVisibleIndexRange(3, new Rect(0, -75, 200, 50));
            Assert.AreEqual(0, first);
            Assert.AreEqual(1, last);
        }
    }
}
