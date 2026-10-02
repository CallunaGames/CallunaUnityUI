using System.Collections.Generic;
using System.Reflection;
using System;
using Calluna.DI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Calluna.UI.Tests
{
    public class VirtualScrollViewTests
    {
        private GameObject _root;
        private FakeScrollView _scroll;
        private ObservableList<string> _items;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("TestScroll", typeof(RectTransform));

            var viewportGO = new GameObject("Viewport", typeof(RectTransform));
            viewportGO.transform.SetParent(_root.transform);
            var viewportRT = viewportGO.GetComponent<RectTransform>();
            viewportRT.sizeDelta = new Vector2(300f, 200f);

            var contentGO = new GameObject("Content", typeof(RectTransform));
            contentGO.transform.SetParent(_root.transform);

            var scrollRect = _root.AddComponent<ScrollRect>();
            scrollRect.viewport = viewportRT;
            scrollRect.content  = contentGO.GetComponent<RectTransform>();

            _items        = new ObservableList<string> { "a", "b", "c" };
            _scroll       = _root.AddComponent<FakeScrollView>();

            SetField(_scroll, "_scrollRect", scrollRect);
            SetField(_scroll, "_layout", new VerticalListScrollLayout(new VerticalListScrollLayout.Settings
            {
                ItemSize = new Vector2(300f, 40f),
                Spacing  = 0f,
                Padding  = default
            }));
            SetField(_scroll, "_items",        _items);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void VirtualScrollView_OnQuit_ItemAddedDoesNotRequestNewCell()
        {
            _scroll.DoInitialize();
            int requestsBefore = _scroll.RequestCount;

            ((QuitHandler)_scroll).HandleQuit();
            _items.Add("d");

            Assert.AreEqual(requestsBefore, _scroll.RequestCount,
                "Adding an item after quit must not request a new cell");
        }

        [Test]
        public void VirtualScrollView_OnQuit_ItemRemovedDoesNotTriggerRefresh()
        {
            _scroll.DoInitialize();
            int requestsBefore = _scroll.RequestCount;

            ((QuitHandler)_scroll).HandleQuit();
            _items.RemoveAt(0);

            Assert.AreEqual(requestsBefore, _scroll.RequestCount,
                "Removing an item after quit must not request replacement cells");
        }

        [Test]
        public void VirtualScrollView_Clean_ItemAddedDoesNotRequestNewCell()
        {
            _scroll.DoInitialize();
            _scroll.DoClean();
            int requestsBefore = _scroll.RequestCount;

            _items.Add("d");

            Assert.AreEqual(requestsBefore, _scroll.RequestCount,
                "Adding an item after Clean must not request a new cell");
        }

        [Test]
        public void VirtualScrollView_OnItemReplaced_ActiveItem_RequestsReplacement()
        {
            _scroll.DoInitialize();
            _scroll.DoSettle(); // settle deferred activation so items a,b,c are active
            int requestsBefore = _scroll.RequestCount;

            _items[1] = "B"; // returns the cell; the new one is requested with the layout update

            Assert.AreEqual(1, _scroll.ReturnCount,
                "must return the replaced item");
            Assert.AreEqual(requestsBefore, _scroll.RequestCount,
                "must not request before the layout update");

            _scroll.DoApplyLayoutChanges();

            Assert.AreEqual(requestsBefore + 1, _scroll.RequestCount,
                "must request a replacement cell for the replaced index");
        }

        [Test]
        public void VirtualScrollView_OnItemReplacedTwiceInAFrame_RequestsOnce()
        {
            _scroll.DoInitialize();
            _scroll.DoSettle();
            int requestsBefore = _scroll.RequestCount;

            _items[1] = "B";
            _items[1] = "C";
            _scroll.DoApplyLayoutChanges();

            Assert.AreEqual(requestsBefore + 1, _scroll.RequestCount,
                "the intermediate item must never get a cell");
        }

        [Test]
        public void VirtualScrollView_OnItemsSwapped_ActiveItems_TradePositionsWithoutPoolTraffic()
        {
            _scroll.DoInitialize();
            _scroll.DoSettle(); // settle deferred activation so items a,b,c are active
            int requestsBefore = _scroll.RequestCount;
            RectTransform cellA = _scroll.Requested[0];
            RectTransform cellC = _scroll.Requested[2];

            _items.Swap(0, 2);
            _scroll.DoApplyLayoutChanges();

            Assert.AreEqual(0, _scroll.ReturnCount, "swapped cells must not be returned");
            Assert.AreEqual(requestsBefore, _scroll.RequestCount, "swapped cells must not be requested");
            Assert.AreEqual(new Vector2(0f, -80f), cellA.anchoredPosition, "cell of 'a' must move to index 2");
            Assert.AreEqual(new Vector2(0f, 0f), cellC.anchoredPosition, "cell of 'c' must move to index 0");
        }

        [Test]
        public void VirtualScrollView_SeveralAddsInAFrame_RequestOnlyOnLayoutUpdate()
        {
            _scroll.DoInitialize();
            _scroll.DoSettle();
            int requestsBefore = _scroll.RequestCount;

            _items.Add("d");
            _items.Add("e");
            _items.Add("f");
            _items.Insert(0, "x"); // the viewport shows indices 0-5: 'f' ends up out of view

            Assert.AreEqual(requestsBefore, _scroll.RequestCount, "must not request before the layout update");

            _scroll.DoApplyLayoutChanges();

            Assert.AreEqual(requestsBefore + 3, _scroll.RequestCount,
                "only the new visible items 'x', 'd' and 'e' must be requested");
            Assert.AreEqual(0, _scroll.ReturnCount);
            Assert.AreEqual(new Vector2(300f, 280f), _scroll.ContentSize);
        }

        [Test]
        public void VirtualScrollView_Removed_ReturnsCellAndShiftsTheRest()
        {
            _scroll.DoInitialize();
            _scroll.DoSettle();
            RectTransform cellC = _scroll.Requested[2];

            _items.RemoveAt(0);
            _scroll.DoApplyLayoutChanges();

            Assert.AreEqual(1, _scroll.ReturnCount);
            Assert.AreEqual(new Vector2(0f, -40f), cellC.anchoredPosition, "cell of 'c' must move to index 1");
            Assert.AreEqual(new Vector2(300f, 80f), _scroll.ContentSize);
        }

        [Test]
        public void VirtualScrollView_Reset_ReturnsAllCellsAndRebuildsOnLayoutUpdate()
        {
            _scroll.DoInitialize();
            _scroll.DoSettle();
            int requestsBefore = _scroll.RequestCount;

            _items.OverrideWith(new[] { "x", "y" });

            Assert.AreEqual(3, _scroll.ReturnCount, "a reset must return all cells right away");

            _scroll.DoApplyLayoutChanges();

            Assert.AreEqual(requestsBefore + 2, _scroll.RequestCount);
            Assert.AreEqual(new Vector2(300f, 80f), _scroll.ContentSize);
        }

        [Test]
        public void VirtualScrollView_ScrollToIndex_BeforeInitialize_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _scroll.ScrollToIndex(0, ScrollAlignment.Start),
                "ScrollToIndex before Initialize must be a silent no-op");
        }

        // ── Test doubles ─────────────────────────────────────────────────────────

        private sealed class FakeScrollView : VirtualScrollView<RectTransform, string>
        {
            public int RequestCount;
            public int ReturnCount;

            private readonly FakePool _pool = new FakePool();

            public readonly List<RectTransform> Requested = new List<RectTransform>();

            public Vector2 ContentSize => _contentRect.sizeDelta;

            protected override RectTransform RequestItem(int index)
            {
                RequestCount++;
                var go = new GameObject($"Item{index}", typeof(RectTransform));
                go.transform.SetParent(transform);
                var rt = go.GetComponent<RectTransform>();
                Requested.Add(rt);
                return rt;
            }

            protected override void ReturnItem(RectTransform item)
            {
                ReturnCount++;
                if (item != null)
                    Object.DestroyImmediate(item.gameObject);
            }

            protected override Rect GetViewportRect() => new Rect(0, -200, 300, 200);

            public void DoInitialize()
            {
                // _layout, _items already injected via reflection in SetUp.
                SetField(this, "_pool", _pool);
                ((Initializable)this).Initialize();
            }

            public void DoLateUpdate() => LateUpdate();

            public void DoApplyLayoutChanges() => ApplyLayoutChanges();

            // Two LateUpdate calls: first clears the defer flag, second activates visible items.
            public void DoSettle() { DoLateUpdate(); DoLateUpdate(); }

            public void DoClean() => ((Cleanable)this).Clean();
        }

        private sealed class FakePool : Pool<RectTransform, string, PrefabInstantiationArguments>
        {
            public RectTransform Request(string arg1, PrefabInstantiationArguments arg2) => null;
            public void Return(RectTransform item) { }
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
