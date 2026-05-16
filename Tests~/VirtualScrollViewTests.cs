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
        private TestQuitDetector _quitDetector;

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

            _quitDetector = _root.AddComponent<TestQuitDetector>();
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
            SetField(_scroll, "_quitDetector", _quitDetector);
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

            _quitDetector.SetQuitting();
            _items.Add("d");

            Assert.AreEqual(requestsBefore, _scroll.RequestCount,
                "Adding an item after quit must not request a new cell");
        }

        [Test]
        public void VirtualScrollView_OnQuit_ItemRemovedDoesNotTriggerRefresh()
        {
            _scroll.DoInitialize();
            int requestsBefore = _scroll.RequestCount;

            _quitDetector.SetQuitting();
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

            _items[1] = "B"; // fires OnItemReplaced → ReplaceActiveItem(1)

            Assert.AreEqual(1, _scroll.ReturnCount,
                "must return the replaced item");
            Assert.AreEqual(requestsBefore + 1, _scroll.RequestCount,
                "must request a replacement cell for the replaced index");
        }

        [Test]
        public void VirtualScrollView_OnItemsSwapped_ActiveItems_BothGetReplaced()
        {
            _scroll.DoInitialize();
            _scroll.DoSettle(); // settle deferred activation so items a,b,c are active
            int requestsBefore = _scroll.RequestCount;

            _items.Swap(0, 2); // fires OnItemsSwapped → ReplaceActiveItem(0) + ReplaceActiveItem(2)

            Assert.AreEqual(2, _scroll.ReturnCount,
                "must return both swapped items");
            Assert.AreEqual(requestsBefore + 2, _scroll.RequestCount,
                "must request replacements for both swapped indices");
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

            protected override RectTransform RequestItem(int index)
            {
                RequestCount++;
                var go = new GameObject($"Item{index}", typeof(RectTransform));
                go.transform.SetParent(transform);
                return go.GetComponent<RectTransform>();
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
                // _layout, _items, _quitDetector already injected via reflection in SetUp.
                SetField(this, "_pool", _pool);
                ((Initializable)this).Initialize();
            }

            public void DoLateUpdate() => LateUpdate();

            // Two LateUpdate calls: first clears the defer flag, second activates visible items.
            public void DoSettle() { DoLateUpdate(); DoLateUpdate(); }

            public void DoClean() => ((Cleanable)this).Clean();
        }

        private sealed class FakePool : Pool<RectTransform, string, PrefabInstantiationArguments>
        {
            public RectTransform Request(string arg1, PrefabInstantiationArguments arg2) => null;
            public void Return(RectTransform item) { }
        }

        private sealed class TestQuitDetector : QuitDetector
        {
            public void SetQuitting() => OnApplicationQuitting();
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
