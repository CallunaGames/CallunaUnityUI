using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Calluna.UI.Tests
{
    /// <summary>
    /// Tests for VirtualScrollBase lifecycle behaviour.
    ///
    /// FakeScrollBase overrides GetViewportRect to return a fixed rect, bypassing
    /// GetWorldCorners (which requires a canvas stack). ViewportRectCallCount is used
    /// as the observable signal for whether RefreshVisibleItems fired.
    ///
    /// Tests that need real active items use ItemCount > 0 with
    /// new GameObject("…", typeof(RectTransform)) for pool items, which avoids the
    /// Transform/RectTransform incompatibility. Items are parented to the scroll root
    /// so TearDown cleans them up automatically.
    ///
    /// LateUpdate reads _scrollRect.viewport.rect.size directly. A point-anchored
    /// RectTransform (anchorMin == anchorMax) returns sizeDelta as rect.size regardless
    /// of parent, so no Canvas is needed.
    /// </summary>
    public class VirtualScrollBaseTests
    {
        private GameObject _root;
        private FakeScrollBase _scroll;
        private ScrollRect _scrollRect;

        [SetUp]
        public void SetUp()
        {
            // ScrollRect requires a RectTransform on its own GameObject.
            _root = new GameObject("TestScroll", typeof(RectTransform));

            var viewportGO = new GameObject("Viewport", typeof(RectTransform));
            viewportGO.transform.SetParent(_root.transform);
            var viewportRT = viewportGO.GetComponent<RectTransform>();
            // Point anchors (default 0.5, 0.5): rect.size == sizeDelta regardless of parent.
            viewportRT.sizeDelta = new Vector2(300f, 200f);

            var contentGO = new GameObject("Content", typeof(RectTransform));
            contentGO.transform.SetParent(_root.transform);

            _scrollRect = _root.AddComponent<ScrollRect>();
            _scrollRect.viewport = viewportRT;
            _scrollRect.content  = contentGO.GetComponent<RectTransform>();

            _scroll = _root.AddComponent<FakeScrollBase>();
            SetField(_scroll, "_scrollRect", _scrollRect);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        // ── Lifecycle guard tests (bugs fixed) ───────────────────────────────────

        [Test]
        public void VirtualScrollBase_LateUpdateAfterClean_DoesNotCallRefreshVisibleItems()
        {
            _scroll.DoInitialize();
            _scroll.DoLateUpdate();  // consume initial size-cache miss; VRC advances
            _scroll.DoClean();
            int countBefore = _scroll.ViewportRectCallCount;

            _scroll.DoLateUpdate(); // _initialized == false → must return immediately

            Assert.AreEqual(countBefore, _scroll.ViewportRectCallCount,
                "LateUpdate after Clean must not call RefreshVisibleItems");
        }

        [Test]
        public void VirtualScrollBase_CleanResetsViewportSizeCache_ReinitializeTriggersSizeChangeRefresh()
        {
            _scroll.DoInitialize();
            _scroll.DoLateUpdate();  // viewport size (300,200) is now in _lastViewportSize
            _scroll.DoClean();       // fix: resets _lastViewportSize to zero
            _scroll.DoInitialize();  // Rebuild runs; _lastViewportSize is still zero
            int countBefore = _scroll.ViewportRectCallCount;

            _scroll.DoLateUpdate(); // (300,200) != (0,0) → size change → RefreshVisibleItems

            Assert.Greater(_scroll.ViewportRectCallCount, countBefore,
                "LateUpdate after re-initialize must call RefreshVisibleItems because Clean() reset the viewport size cache");
        }

        // ── InitializeBase ───────────────────────────────────────────────────────

        [Test]
        public void VirtualScrollBase_InitializeBase_SetsContentRectToTopLeftAnchorAndZeroPosition()
        {
            _scroll.DoInitialize();

            RectTransform content = _scrollRect.content;
            Assert.AreEqual(new Vector2(0f, 1f), content.anchorMin,       "anchorMin");
            Assert.AreEqual(new Vector2(0f, 1f), content.anchorMax,       "anchorMax");
            Assert.AreEqual(new Vector2(0f, 1f), content.pivot,           "pivot");
            Assert.AreEqual(Vector2.zero,         content.anchoredPosition, "anchoredPosition");
        }

        [Test]
        public void VirtualScrollBase_InitializeBase_SubscribesScrollEvent_ScrollTriggersRefresh()
        {
            _scroll.DoInitialize();
            int countBefore = _scroll.ViewportRectCallCount;

            _scrollRect.onValueChanged.Invoke(Vector2.zero);

            Assert.Greater(_scroll.ViewportRectCallCount, countBefore,
                "Scroll event must trigger RefreshVisibleItems after Initialize");
        }

        // ── CleanBase ────────────────────────────────────────────────────────────

        [Test]
        public void VirtualScrollBase_CleanBase_RemovesScrollListener_SubsequentScrollDoesNotRefresh()
        {
            _scroll.DoInitialize();
            _scroll.DoClean();
            int countBefore = _scroll.ViewportRectCallCount;

            _scrollRect.onValueChanged.Invoke(Vector2.zero);

            Assert.AreEqual(countBefore, _scroll.ViewportRectCallCount,
                "Scroll event must not trigger RefreshVisibleItems after Clean");
        }

        [Test]
        public void VirtualScrollBase_CleanBase_ReturnsAllActiveItems()
        {
            _scroll.SetItemCount(3);
            _scroll.DoInitialize(); // activates items 0, 1, 2

            _scroll.DoClean();

            Assert.AreEqual(3, _scroll.ReturnCount);
        }

        // ── SetDirty ─────────────────────────────────────────────────────────────

        [Test]
        public void VirtualScrollBase_SetDirty_TriggersRebuildOnNextLateUpdate()
        {
            _scroll.DoInitialize();
            _scroll.DoLateUpdate(); // consume initial size-cache miss
            int countBefore = _scroll.ViewportRectCallCount;

            _scroll.DoSetDirty();
            _scroll.DoLateUpdate();

            Assert.Greater(_scroll.ViewportRectCallCount, countBefore,
                "LateUpdate after SetDirty must call Rebuild → RefreshVisibleItems");
        }

        [Test]
        public void VirtualScrollBase_SetDirty_FlagClearedAfterRebuild_SecondLateUpdateDoesNotRebuild()
        {
            _scroll.DoInitialize();
            _scroll.DoLateUpdate();
            _scroll.DoSetDirty();
            _scroll.DoLateUpdate(); // rebuild; dirty flag cleared
            int countAfterRebuild = _scroll.ViewportRectCallCount;

            _scroll.DoLateUpdate(); // no dirty, no size change → no action

            Assert.AreEqual(countAfterRebuild, _scroll.ViewportRectCallCount,
                "LateUpdate without SetDirty must not trigger another Rebuild");
        }

        // ── RefreshVisibleItems ──────────────────────────────────────────────────

        [Test]
        public void VirtualScrollBase_RefreshVisibleItems_ItemsOutsideNewViewport_AreReturned()
        {
            _scroll.SetItemCount(3);
            _scroll.DoInitialize(); // items 0, 1, 2 all visible in the default (300,200) viewport

            // Shrink to only show item 0 (yMin=-39 gives last=floor(39/40)=0)
            _scroll.SetViewport(new Rect(0, -39, 300, 39));
            _scrollRect.onValueChanged.Invoke(Vector2.zero);

            Assert.AreEqual(2, _scroll.ReturnCount,
                "Items 1 and 2 must be returned when they scroll out of the visible range");
        }

        [Test]
        public void VirtualScrollBase_RefreshVisibleItems_AlreadyActiveItems_NotRerequested()
        {
            _scroll.SetItemCount(3);
            _scroll.DoInitialize();
            int requestsAfterInit = _scroll.RequestCount;

            // Same viewport — all items still visible; nothing should be re-requested.
            _scrollRect.onValueChanged.Invoke(Vector2.zero);

            Assert.AreEqual(requestsAfterInit, _scroll.RequestCount,
                "RequestItem must not be called for indices already in the active set");
        }

        // ── ReturnActiveItemAt ───────────────────────────────────────────────────

        [Test]
        public void VirtualScrollBase_ReturnActiveItemAt_ActiveIndex_CallsReturnItem()
        {
            _scroll.SetItemCount(3);
            _scroll.DoInitialize();

            _scroll.DoReturnActiveItemAt(1);

            Assert.AreEqual(1, _scroll.ReturnCount);
        }

        [Test]
        public void VirtualScrollBase_ReturnActiveItemAt_ActiveIndex_RemovedFromActiveSet()
        {
            _scroll.SetItemCount(3);
            _scroll.DoInitialize();
            _scroll.DoReturnActiveItemAt(1);

            // Second call on the same index must be a no-op — item is already gone.
            _scroll.DoReturnActiveItemAt(1);

            Assert.AreEqual(1, _scroll.ReturnCount,
                "Second ReturnActiveItemAt on the same index must not call ReturnItem again");
        }

        [Test]
        public void VirtualScrollBase_ReturnActiveItemAt_InactiveIndex_DoesNothing()
        {
            _scroll.DoInitialize(); // ItemCount=0, no active items

            Assert.DoesNotThrow(() => _scroll.DoReturnActiveItemAt(5));
            Assert.AreEqual(0, _scroll.ReturnCount);
        }

        // ── ReplaceActiveItem ────────────────────────────────────────────────────

        [Test]
        public void VirtualScrollBase_ReplaceActiveItem_ActiveIndex_ReturnsThenRequestsForSameIndex()
        {
            _scroll.SetItemCount(3);
            _scroll.DoInitialize(); // RequestCount=3, ReturnCount=0

            _scroll.DoReplaceActiveItem(1);

            Assert.AreEqual(1, _scroll.ReturnCount,  "must return the old item");
            Assert.AreEqual(4, _scroll.RequestCount, "must request a fresh replacement");
        }

        [Test]
        public void VirtualScrollBase_ReplaceActiveItem_InactiveIndex_DoesNothing()
        {
            _scroll.DoInitialize(); // ItemCount=0

            Assert.DoesNotThrow(() => _scroll.DoReplaceActiveItem(5));
            Assert.AreEqual(0, _scroll.ReturnCount);
            Assert.AreEqual(0, _scroll.RequestCount);
        }

        // ── ShiftActiveItems ─────────────────────────────────────────────────────

        [Test]
        public void VirtualScrollBase_ShiftActiveItems_PositiveDelta_UpdatesPositionsOfShiftedItems()
        {
            _scroll.SetItemCount(3);
            _scroll.DoInitialize(); // items 0,1,2 at positions (0,0),(0,-40),(0,-80)

            _scroll.DoShiftActiveItems(1, +1); // keys 1 and 2 → re-keyed to 2 and 3

            Assert.AreEqual(new Vector2(0f,  -80f), _scroll.RequestedItems[1].anchoredPosition,
                "item originally at index 1 must be repositioned to index 2");
            Assert.AreEqual(new Vector2(0f, -120f), _scroll.RequestedItems[2].anchoredPosition,
                "item originally at index 2 must be repositioned to index 3");
        }

        [Test]
        public void VirtualScrollBase_ShiftActiveItems_PositiveDelta_LeavesItemsBelowFromIndexUnchanged()
        {
            _scroll.SetItemCount(3);
            _scroll.DoInitialize();

            _scroll.DoShiftActiveItems(1, +1);

            Assert.AreEqual(new Vector2(0f, 0f), _scroll.RequestedItems[0].anchoredPosition,
                "item below fromIndex must not be repositioned");
        }

        [Test]
        public void VirtualScrollBase_ShiftActiveItems_NegativeDelta_UpdatesPositionsOfShiftedItems()
        {
            _scroll.SetItemCount(3);
            _scroll.DoInitialize();
            // Mirror the real usage: the removed item is returned before the shift.
            _scroll.DoReturnActiveItemAt(1);

            _scroll.DoShiftActiveItems(2, -1); // key 2 → re-keyed to 1

            Assert.AreEqual(new Vector2(0f, -40f), _scroll.RequestedItems[2].anchoredPosition,
                "item originally at index 2 must be repositioned to index 1");
        }

        // ── ScrollToIndex ────────────────────────────────────────────────────────
        //
        // Geometry: VerticalListScrollLayout, itemHeight=40, spacing=0, no padding.
        // Viewport: 300×200 (from SetUp).
        //   contentHeight(n) = n × 40
        //   maxScrollY(n)    = n × 40 − 200   (positive when n > 5)
        //   itemTopY(i)      = i × 40
        //   normY            = 1 − clampedOffsetY / maxScrollY

        [Test]
        public void VirtualScrollBase_ScrollToIndex_BeforeInitialize_IsNoOp()
        {
            // _initialized == false — must return without touching normalizedPosition.
            Vector2 before = _scrollRect.normalizedPosition;

            _scroll.SetItemCount(20);
            _scroll.DoScrollToIndex(0, ScrollAlignment.Start);

            Assert.AreEqual(before, _scrollRect.normalizedPosition);
        }

        [Test]
        public void VirtualScrollBase_ScrollToIndex_NegativeIndex_IsNoOp()
        {
            _scroll.SetItemCount(20);
            _scroll.DoInitialize();
            Vector2 before = _scrollRect.normalizedPosition;

            _scroll.DoScrollToIndex(-1, ScrollAlignment.Start);

            Assert.AreEqual(before, _scrollRect.normalizedPosition);
        }

        [Test]
        public void VirtualScrollBase_ScrollToIndex_IndexEqualToItemCount_IsNoOp()
        {
            _scroll.SetItemCount(20);
            _scroll.DoInitialize();
            Vector2 before = _scrollRect.normalizedPosition;

            _scroll.DoScrollToIndex(20, ScrollAlignment.Start);

            Assert.AreEqual(before, _scrollRect.normalizedPosition);
        }

        [Test]
        public void VirtualScrollBase_ScrollToIndex_ContentFitsInViewport_NormYUnchanged()
        {
            // 3 items × 40 px = 120 px < 200 px viewport → maxScrollY = 0 → no movement.
            _scroll.SetItemCount(3);
            _scroll.DoInitialize();
            float before = _scrollRect.normalizedPosition.y;

            _scroll.DoScrollToIndex(0, ScrollAlignment.Start);

            Assert.AreEqual(before, _scrollRect.normalizedPosition.y, 0.0001f);
        }

        [Test]
        public void VirtualScrollBase_ScrollToIndex_Start_FirstItem_NormYIsOne()
        {
            // 20 items, scroll to top: offset = 0 → normY = 1.
            _scroll.SetItemCount(20);
            _scroll.DoInitialize();

            _scroll.DoScrollToIndex(0, ScrollAlignment.Start);

            Assert.AreEqual(1f, _scrollRect.normalizedPosition.y, 0.0001f);
        }

        [Test]
        public void VirtualScrollBase_ScrollToIndex_End_LastItem_NormYIsZero()
        {
            // 20 items: itemTopY(19)=760, offset = 760+40-200 = 600 = maxScrollY → normY = 0.
            _scroll.SetItemCount(20);
            _scroll.DoInitialize();

            _scroll.DoScrollToIndex(19, ScrollAlignment.End);

            Assert.AreEqual(0f, _scrollRect.normalizedPosition.y, 0.0001f);
        }

        [Test]
        public void VirtualScrollBase_ScrollToIndex_Center_MiddleItem_NormYBetweenZeroAndOne()
        {
            _scroll.SetItemCount(20);
            _scroll.DoInitialize();

            _scroll.DoScrollToIndex(9, ScrollAlignment.Center);

            float normY = _scrollRect.normalizedPosition.y;
            Assert.Greater(normY, 0f, "normY must be above 0 for a middle item centred");
            Assert.Less(normY,    1f, "normY must be below 1 for a middle item centred");
        }

        [Test]
        public void VirtualScrollBase_ScrollToIndex_Start_MiddleItem_NormYMatchesExpected()
        {
            // 20 items: itemTopY(10) = 400, maxScrollY = 600 → normY = 1 − 400/600 ≈ 0.3333
            _scroll.SetItemCount(20);
            _scroll.DoInitialize();

            _scroll.DoScrollToIndex(10, ScrollAlignment.Start);

            float expected = 1f - 400f / 600f;
            Assert.AreEqual(expected, _scrollRect.normalizedPosition.y, 0.0001f);
        }

        [Test]
        [Description("ScrollToIndex End, middle item => verticalNormalizedPosition matches expected?")]
        public void VirtualScrollBase_ScrollToIndex_End_MiddleItem_NormYMatchesExpected()
        {
            // 20 items, itemHeight=40, viewport=200.
            // itemTop(5) = 5*40 = 200. rawY = 200 + 40 - 200 = 40. maxScrollY = 20*40 - 200 = 600.
            // normY = 1 - 40/600
            _scroll.SetItemCount(20);
            _scroll.DoInitialize();

            _scroll.DoScrollToIndex(5, ScrollAlignment.End);

            float expected = 1f - 40f / 600f;
            Assert.AreEqual(expected, _scrollRect.normalizedPosition.y, 0.0001f);
        }

        // ── ScrollToIndex — horizontal axis ─────────────────────────────────────

        [Test]
        [Description("ScrollToIndex Start on horizontal layout => horizontalNormalizedPosition matches expected?")]
        public void VirtualScrollBase_ScrollToIndex_HorizontalContent_NormXMatchesExpected()
        {
            // HorizontalListScrollLayout: ItemSize=(100,200), Spacing=0.
            // 10 items → contentWidth = 1000. Viewport = 300×200 (SetUp). maxScrollX = 700.
            // index=5: itemLeft = 5*100 = 500. rawX = 500. normX = 500/700.
            // maxScrollY = 200 - 200 = 0 → verticalNormalizedPosition must not be set.
            _scroll.SetItemCount(10);
            _scroll.DoInitializeWithLayout(new HorizontalListScrollLayout(
                new HorizontalListScrollLayout.Settings
                {
                    ItemSize = new Vector2(100f, 200f),
                    Spacing  = 0f,
                    Padding  = default
                }));

            _scroll.DoScrollToIndex(5, ScrollAlignment.Start);

            float expectedNormX = 500f / 700f;
            Assert.AreEqual(expectedNormX, _scrollRect.horizontalNormalizedPosition, 0.0001f,
                "horizontalNormalizedPosition must match expected for index 5");
        }

        // ── VirtualScrollBase constant ───────────────────────────────────────────

        [Test]
        [Description("ScrollTweenerId constant => has expected string value?")]
        public void VirtualScrollBase_ScrollTweenerId_HasExpectedValue()
        {
            Assert.AreEqual("virtualscroll-tweener", VirtualScrollBase.ScrollTweenerId);
        }

        // ── ResizeContent / Rebuild content size ─────────────────────────────────

        [Test]
        [Description("ResizeContent => content sizeDelta matches layout ComputeContentSize?")]
        public void VirtualScrollBase_ResizeContent_SetsContentSizeDeltaToLayoutComputedSize()
        {
            // VerticalListScrollLayout: ItemSize=(300,40), Spacing=0.
            // ComputeContentSize(5) = (300, 200).
            _scroll.SetItemCount(5);
            _scroll.DoInitialize();

            _scroll.DoResizeContent();

            Assert.AreEqual(new Vector2(300f, 200f), _scrollRect.content.sizeDelta);
        }

        [Test]
        [Description("Rebuild after SetItemCount change => content sizeDelta updated from layout?")]
        public void VirtualScrollBase_Rebuild_SetsContentSizeDeltaFromLayout()
        {
            // After DoInitialize with 3 items: contentHeight = 120.
            // After SetItemCount(7) + DoSetDirty + DoLateUpdate: contentHeight = 280.
            _scroll.SetItemCount(3);
            _scroll.DoInitialize();
            _scroll.DoLateUpdate(); // consume initial size-cache miss

            _scroll.SetItemCount(7);
            _scroll.DoSetDirty();
            _scroll.DoLateUpdate(); // _isDirty == true → Rebuild fires

            Assert.AreEqual(new Vector2(300f, 280f), _scrollRect.content.sizeDelta);
        }

        [Test]
        [Description("ScrollToIndex positive duration with null scrollTweener => falls back to instant snap?")]
        public void VirtualScrollBase_ScrollToIndex_PositiveDuration_NullTweener_SnapsInstantly()
        {
            // _scrollTweener is null in FakeScrollBase (never set).
            // 20 items, itemHeight=40, viewport=200. maxScrollY=600.
            // index=10, Start alignment: rawY=400, normY = 1 − 400/600.
            _scroll.SetItemCount(20);
            _scroll.DoInitialize();

            _scroll.DoScrollToIndex(10, ScrollAlignment.Start, 1f);

            float expected = 1f - 400f / 600f;
            Assert.AreEqual(expected, _scrollRect.normalizedPosition.y, 0.0001f,
                "null scrollTweener with positive duration must fall back to instant snap");
        }

        [Test]
        [Description("ShiftActiveItems with a gap in the active set => skips missing indices and repositions the rest?")]
        public void VirtualScrollBase_ShiftActiveItems_GapInActiveSet_SkipsMissingIndices()
        {
            _scroll.SetItemCount(3);
            _scroll.DoInitialize(); // items 0, 1, 2 active
            _scroll.DoReturnActiveItemAt(1); // gap at index 1; active set is {0, 2}

            // Shift fromIndex=0, delta=+1: keys 0→1 and 2→3.
            _scroll.DoShiftActiveItems(0, +1);

            // RequestedItems[0] was originally at index 0, now repositioned to index 1 (y=-40).
            Assert.AreEqual(new Vector2(0f, -40f), _scroll.RequestedItems[0].anchoredPosition,
                "item originally at index 0 must be repositioned to index 1");
            // RequestedItems[2] was originally at index 2, now repositioned to index 3 (y=-120).
            Assert.AreEqual(new Vector2(0f, -120f), _scroll.RequestedItems[2].anchoredPosition,
                "item originally at index 2 must be repositioned to index 3");
        }

        // ── Test double ──────────────────────────────────────────────────────────

        private sealed class FakeScrollBase : VirtualScrollBase<RectTransform>
        {
            public int ViewportRectCallCount;
            public int RequestCount;
            public int ReturnCount;
            public readonly List<RectTransform> RequestedItems = new List<RectTransform>();

            private Rect _viewport  = new Rect(0, -200, 300, 200);
            private int  _itemCount;

            public void SetItemCount(int count) => _itemCount = count;
            public void SetViewport(Rect rect)  => _viewport  = rect;

            protected override int ItemCount => _itemCount;

            protected override RectTransform RequestItem(int index)
            {
                RequestCount++;
                var go = new GameObject($"Item{index}", typeof(RectTransform));
                go.transform.SetParent(transform); // parented to root → destroyed by TearDown
                var rt = go.GetComponent<RectTransform>();
                RequestedItems.Add(rt);
                return rt;
            }

            protected override void ReturnItem(RectTransform item)
            {
                ReturnCount++;
                if (item != null)
                    Object.DestroyImmediate(item.gameObject);
            }

            // Counts each invocation so tests can detect RefreshVisibleItems calls
            // without needing world-space coordinates or a Canvas stack.
            protected override Rect GetViewportRect()
            {
                ViewportRectCallCount++;
                return _viewport;
            }

            public void DoInitialize()
            {
                _layout = new VerticalListScrollLayout(new VerticalListScrollLayout.Settings
                {
                    ItemSize = new Vector2(300f, 40f),
                    Spacing  = 0f,
                    Padding  = default
                });
                InitializeBase();
            }

            public void DoInitializeWithLayout(IScrollLayout layout)
            {
                _layout = layout;
                InitializeBase();
            }

            public void DoResizeContent() => ResizeContent();

            public void DoClean()      => CleanBase();
            public void DoLateUpdate() => LateUpdate();
            public void DoSetDirty()   => SetDirty();

            public void DoReturnActiveItemAt(int index) => ReturnActiveItemAt(index);
            public void DoReplaceActiveItem(int index)  => ReplaceActiveItem(index);
            public void DoShiftActiveItems(int fromIndex, int delta) => ShiftActiveItems(fromIndex, delta);
            public void DoScrollToIndex(int index, ScrollAlignment alignment) => ScrollToIndex(index, alignment);
            public void DoScrollToIndex(int index, ScrollAlignment alignment, float duration) => ScrollToIndex(index, alignment, duration);
        }

        // ── Reflection helper ────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            Type type = target.GetType();
            FieldInfo field = null;
            while (field == null && type != null)
            {
                field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
                type  = type.BaseType;
            }
            field.SetValue(target, value);
        }
    }
}
