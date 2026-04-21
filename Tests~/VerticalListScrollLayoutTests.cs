using System;
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
            // Item 0 bottom edge is at y=-50. Shifting the viewport 1px below that (yMax=-51)
            // ensures item 0 is fully out of view.
            (int first, int last) = layout.GetVisibleIndexRange(3, new Rect(0, -151, 200, 100));
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

        // ── Constructor validation ───────────────────────────────────────────────

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(-50f)]
        [Description("Constructor with ItemSize.y <= 0 => throws ArgumentException?")]
        public void VerticalListScrollLayout_Constructor_ItemSizeYZeroOrNegative_ThrowsArgumentException(float itemH)
        {
            Assert.Throws<ArgumentException>(() =>
                new VerticalListScrollLayout(new VerticalListScrollLayout.Settings
                {
                    ItemSize = new Vector2(200f, itemH),
                    Spacing  = 0f,
                    Padding  = new Padding()
                }));
        }

        // ── ItemSize ─────────────────────────────────────────────────────────────

        [TestCase(200f, 50f)]
        [TestCase(100f, 30f)]
        [TestCase(400f, 120f)]
        [Description("ItemSize property => returns the ItemSize passed in Settings?")]
        public void VerticalListScrollLayout_ItemSize_ReturnsSettingsItemSize(float itemW, float itemH)
        {
            var layout = Make(itemW, itemH);
            Assert.AreEqual(new Vector2(itemW, itemH), layout.ItemSize);
        }

        // ── GetVisibleIndexRange: padding offsets visibility ─────────────────────

        [Test]
        [Description("GetVisibleIndexRange with non-zero top padding => first/last indices account for padding?")]
        public void VerticalListScrollLayout_GetVisibleIndexRange_WithPadding_AdjustsFirstAndLastCorrectly()
        {
            // 3 items, itemH=50, padTop=30, no spacing.
            // Item 0 occupies y: -30 to -80.
            // Item 1 occupies y: -80 to -130.
            // Item 2 occupies y: -130 to -180.
            // Using yMax=-81 and yMin=-129 keeps the viewport 1px inside item 1 on both sides,
            // so the "touching = visible" boundary condition cannot pull items 0 or 2 into range.
            var layout = Make(200, 50, padTop: 30);
            (int first, int last) = layout.GetVisibleIndexRange(3, new Rect(0, -129, 200, 48));
            Assert.AreEqual(1, first);
            Assert.AreEqual(1, last);
        }

        // ── ComputeContentSize: negative itemCount ───────────────────────────────

        [TestCase(-1)]
        [TestCase(-5)]
        [TestCase(-100)]
        [Description("ComputeContentSize with negative itemCount => returns Vector2.zero (same as zero)?")]
        public void VerticalListScrollLayout_ComputeContentSize_NegativeItemCount_ReturnsZero(int itemCount)
        {
            var layout = Make(200, 50);
            Assert.AreEqual(Vector2.zero, layout.ComputeContentSize(itemCount));
        }

        // ── GetVisibleIndexRange: middle window of a long list ───────────────────

        [Test]
        [Description("GetVisibleIndexRange with viewport over only a middle window of a long list => first and last indices are both bounded away from the extremes?")]
        public void VerticalListScrollLayout_GetVisibleIndexRange_ViewportShowsMiddleWindow_ExcludesTopAndBottomItems()
        {
            // 6 items, itemH=40, no spacing/padding.
            // Item 0: y [0, -40],   Item 1: y [-40, -80],   Item 2: y [-80, -120],
            // Item 3: y [-120, -160], Item 4: y [-160, -200], Item 5: y [-200, -240].
            // Viewport: yMax=-41, yMin=-199 (height=158) — 1px below item 0's bottom, 1px above item 5's top.
            // first = ceil((41-40)/40) = ceil(0.025) = 1
            // last  = floor(199/40)    = floor(4.975) = 4
            var layout = Make(200, 40);
            (int first, int last) = layout.GetVisibleIndexRange(6, new Rect(0, -199, 200, 158));
            Assert.AreEqual(1, first);
            Assert.AreEqual(4, last);
        }
    }
}
