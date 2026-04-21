using System;
using NUnit.Framework;
using UnityEngine;

namespace Calluna.UI.Tests
{
    public class HorizontalListScrollLayoutTests
    {
        private static HorizontalListScrollLayout Make(float itemW, float itemH,
            float spacing = 0,
            float padTop = 0, float padBottom = 0, float padLeft = 0, float padRight = 0)
        {
            return new HorizontalListScrollLayout(new HorizontalListScrollLayout.Settings
            {
                ItemSize = new Vector2(itemW, itemH),
                Spacing  = spacing,
                Padding  = new Padding { Top = padTop, Bottom = padBottom, Left = padLeft, Right = padRight }
            });
        }

        // ── ComputeContentSize ───────────────────────────────────────────────────

        [Test]
        public void HorizontalListScrollLayout_ComputeContentSize_ZeroItems_ReturnsZero()
        {
            var layout = Make(100, 200);
            Assert.AreEqual(Vector2.zero, layout.ComputeContentSize(0));
        }

        [Test]
        public void HorizontalListScrollLayout_ComputeContentSize_SingleItem_ReturnsItemSizePlusPadding()
        {
            var layout = Make(100, 200, padTop: 5, padBottom: 5, padLeft: 10, padRight: 10);
            Vector2 size = layout.ComputeContentSize(1);
            Assert.AreEqual(120f, size.x, 0.001f); // 10+100+10
            Assert.AreEqual(210f, size.y, 0.001f); // 5+200+5
        }

        [Test]
        public void HorizontalListScrollLayout_ComputeContentSize_MultipleItems_WidthIncludesSpacing()
        {
            var layout = Make(100, 200, spacing: 10);
            Vector2 size = layout.ComputeContentSize(3);
            // width = 3*100 + 2*10 = 320
            Assert.AreEqual(320f, size.x, 0.001f);
        }

        [Test]
        public void HorizontalListScrollLayout_ComputeContentSize_HeightIsConstant()
        {
            var layout = Make(100, 200);
            Assert.AreEqual(layout.ComputeContentSize(1).y, layout.ComputeContentSize(10).y, 0.001f);
        }

        // ── ComputeItemPosition ──────────────────────────────────────────────────

        [Test]
        public void HorizontalListScrollLayout_ComputeItemPosition_FirstItem_ReturnsOriginPlusPadding()
        {
            var layout = Make(100, 200, padTop: 5, padLeft: 10);
            Vector2 pos = layout.ComputeItemPosition(0);
            Assert.AreEqual(10f, pos.x, 0.001f);
            Assert.AreEqual(-5f, pos.y, 0.001f);
        }

        [Test]
        public void HorizontalListScrollLayout_ComputeItemPosition_YIsConstantAcrossItems()
        {
            var layout = Make(100, 200, padTop: 8);
            Assert.AreEqual(layout.ComputeItemPosition(0).y, layout.ComputeItemPosition(5).y, 0.001f);
        }

        [Test]
        public void HorizontalListScrollLayout_ComputeItemPosition_XIncreasesByStepEachItem()
        {
            var layout = Make(100, 200, spacing: 10);
            float step = 110f; // itemW + spacing
            Assert.AreEqual(layout.ComputeItemPosition(0).x + step, layout.ComputeItemPosition(1).x, 0.001f);
            Assert.AreEqual(layout.ComputeItemPosition(1).x + step, layout.ComputeItemPosition(2).x, 0.001f);
        }

        // ── GetVisibleIndexRange ─────────────────────────────────────────────────

        [Test]
        public void HorizontalListScrollLayout_GetVisibleIndexRange_ZeroItems_ReturnsEmpty()
        {
            var layout = Make(100, 200);
            (int first, int last) = layout.GetVisibleIndexRange(0, new Rect(0, -200, 300, 200));
            Assert.Greater(first, last);
        }

        [Test]
        public void HorizontalListScrollLayout_GetVisibleIndexRange_FullyVisible_ReturnsAll()
        {
            var layout = Make(100, 200);
            // 3 items, content width = 300. Viewport covers full content.
            (int first, int last) = layout.GetVisibleIndexRange(3, new Rect(0, -200, 300, 200));
            Assert.AreEqual(0, first);
            Assert.AreEqual(2, last);
        }

        [Test]
        public void HorizontalListScrollLayout_GetVisibleIndexRange_ScrolledPastFirstItem_ExcludesIt()
        {
            var layout = Make(100, 200, spacing: 0);
            // Item 0 right edge is at x=100. Shifting the viewport 1px past that (xMin=101)
            // ensures item 0 is fully out of view.
            (int first, int last) = layout.GetVisibleIndexRange(3, new Rect(101, -200, 200, 200));
            Assert.AreEqual(1, first);
            Assert.AreEqual(2, last);
        }

        [Test]
        public void HorizontalListScrollLayout_GetVisibleIndexRange_ViewportShowsPartialItem_IncludesIt()
        {
            var layout = Make(100, 200, spacing: 0);
            // Viewport shows x: 50 to 150 — partially overlaps item 0 and item 1.
            (int first, int last) = layout.GetVisibleIndexRange(3, new Rect(50, -200, 100, 200));
            Assert.AreEqual(0, first);
            Assert.AreEqual(1, last);
        }

        // ── Constructor validation ───────────────────────────────────────────────

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(-100f)]
        [Description("Constructor with ItemSize.x <= 0 => throws ArgumentException?")]
        public void HorizontalListScrollLayout_Constructor_ItemSizeXZeroOrNegative_ThrowsArgumentException(float itemW)
        {
            Assert.Throws<ArgumentException>(() =>
                new HorizontalListScrollLayout(new HorizontalListScrollLayout.Settings
                {
                    ItemSize = new Vector2(itemW, 200f),
                    Spacing  = 0f,
                    Padding  = new Padding()
                }));
        }

        // ── ItemSize ─────────────────────────────────────────────────────────────

        [TestCase(100f, 200f)]
        [TestCase(50f,  80f)]
        [TestCase(300f, 150f)]
        [Description("ItemSize property => returns the ItemSize passed in Settings?")]
        public void HorizontalListScrollLayout_ItemSize_ReturnsSettingsItemSize(float itemW, float itemH)
        {
            var layout = Make(itemW, itemH);
            Assert.AreEqual(new Vector2(itemW, itemH), layout.ItemSize);
        }

        // ── GetVisibleIndexRange: padding offsets visibility ─────────────────────

        [Test]
        [Description("GetVisibleIndexRange with non-zero left padding => first/last indices account for padding?")]
        public void HorizontalListScrollLayout_GetVisibleIndexRange_WithPadding_AdjustsFirstAndLastCorrectly()
        {
            // 3 items, itemW=100, padLeft=40, no spacing.
            // Item 0 occupies x:  40 to 140.
            // Item 1 occupies x: 140 to 240.
            // Item 2 occupies x: 240 to 340.
            // Using xMin=141 and xMax=239 keeps the viewport 1px inside item 1 on both sides,
            // so the "touching = visible" boundary condition cannot pull items 0 or 2 into range.
            var layout = Make(100, 200, padLeft: 40);
            (int first, int last) = layout.GetVisibleIndexRange(3, new Rect(141, -200, 98, 200));
            Assert.AreEqual(1, first);
            Assert.AreEqual(1, last);
        }

        // ── ComputeContentSize: negative itemCount ───────────────────────────────

        [TestCase(-1)]
        [TestCase(-5)]
        [TestCase(-100)]
        [Description("ComputeContentSize with negative itemCount => returns Vector2.zero (same as zero)?")]
        public void HorizontalListScrollLayout_ComputeContentSize_NegativeItemCount_ReturnsZero(int itemCount)
        {
            var layout = Make(100, 200);
            Assert.AreEqual(Vector2.zero, layout.ComputeContentSize(itemCount));
        }

        // ── GetVisibleIndexRange: middle window of a long list ───────────────────

        [Test]
        [Description("GetVisibleIndexRange with viewport over only a middle window of a long list => first and last indices are both bounded away from the extremes?")]
        public void HorizontalListScrollLayout_GetVisibleIndexRange_ViewportShowsMiddleWindow_ExcludesFirstAndLastItems()
        {
            // 6 items, itemW=50, no spacing/padding.
            // Item 0: x [0, 50],    Item 1: x [50, 100],   Item 2: x [100, 150],
            // Item 3: x [150, 200], Item 4: x [200, 250],  Item 5: x [250, 300].
            // Viewport: xMin=51, xMax=249 (width=198) — 1px past item 0's right, 1px before item 5's left.
            // first = max(0, ceil((51-0-50)/50)) = max(0, ceil(0.02)) = 1
            // last  = floor((249-0)/50)           = floor(4.98)       = 4
            var layout = Make(50, 200);
            (int first, int last) = layout.GetVisibleIndexRange(6, new Rect(51, -200, 198, 200));
            Assert.AreEqual(1, first);
            Assert.AreEqual(4, last);
        }
    }
}
