using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Calluna.DI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace Calluna.UI.Tests
{
    /// <summary>
    /// Makes sure the cells of a <see cref="VirtualScrollView{TItem,TData}"/> always match its data
    /// list, whatever the list does: after any sequence of changes, every active cell shows the item
    /// at its index and sits at that index's position, and after the layout update exactly the
    /// visible indices have a cell. Cells are never leaked or returned twice.
    /// <para>
    /// Every item value is unique and a cell is named after the item it was requested for, so a
    /// cell showing a wrong or stale item is detected.
    /// </para>
    /// </summary>
    public class VirtualScrollViewConsistencyTests
    {
        private const float ItemHeight = 40f;
        private const float ViewportHeight = 200f;

        private GameObject _root;
        private ScrollRect _scrollRect;
        private TrackingScrollView _scroll;
        private ObservableList<string> _items;
        private VerticalListScrollLayout _layout;
        private int _nextValue;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("TestScroll", typeof(RectTransform));

            var viewportGO = new GameObject("Viewport", typeof(RectTransform));
            viewportGO.transform.SetParent(_root.transform);
            var viewportRT = viewportGO.GetComponent<RectTransform>();
            viewportRT.sizeDelta = new Vector2(300f, ViewportHeight);

            var contentGO = new GameObject("Content", typeof(RectTransform));
            contentGO.transform.SetParent(_root.transform);

            _scrollRect = _root.AddComponent<ScrollRect>();
            _scrollRect.viewport = viewportRT;
            _scrollRect.content = contentGO.GetComponent<RectTransform>();

            _nextValue = 0;
            _items = new ObservableList<string>();
            _layout = new VerticalListScrollLayout(new VerticalListScrollLayout.Settings
            {
                ItemSize = new Vector2(300f, ItemHeight),
                Spacing = 0f,
                Padding = default
            });

            _scroll = _root.AddComponent<TrackingScrollView>();
            _scroll.Source = _items;
            SetField(_scroll, "_scrollRect", _scrollRect);
            SetField(_scroll, "_layout", _layout);
            SetField(_scroll, "_items", _items);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        // ── Single changes at every position relative to the viewport ────────────

        public enum Change { Add, Insert, RemoveAt, Replace, SwapWithinView, SwapIntoView, SwapOutOfView, Clear, OverrideWith, OverrideWithEvents }

        // The viewport shows indices 10-15 of 30 items, so each change happens before, inside and
        // behind the visible range.
        [Test]
        public void Change_AtEachPosition_CellsMatchItems(
            [Values] Change change,
            [Values(0, 9, 10, 12, 15, 16, 29)] int index)
        {
            Fill(30);
            Initialize();
            ScrollTo(10 * ItemHeight);
            AssertConsistent("before the change");

            Apply(change, index);
            AssertCellsShowTheirItems($"right after {change} at {index}");

            _scroll.Apply();
            AssertConsistent($"after {change} at {index}");
        }

        [Test]
        public void SeveralChangesInOneFrame_CellsMatchItemsAfterLayoutUpdate()
        {
            Fill(30);
            Initialize();
            ScrollTo(10 * ItemHeight);

            _items.Insert(11, NewValue());
            _items.RemoveAt(13);
            _items.Swap(10, 20);
            _items[12] = NewValue();
            _items.Insert(0, NewValue());
            _items.RemoveAt(29);
            AssertCellsShowTheirItems("before the layout update");

            _scroll.Apply();
            AssertConsistent("after the layout update");
        }

        // ── Random sequences ─────────────────────────────────────────────────────

        // Random changes - several per frame, with scrolling in between - checked after every change
        // and after every layout update. A failure names the seed and step to reproduce it.
        [Test]
        public void RandomChanges_CellsAlwaysMatchItems([Values(1, 2, 3, 4, 5, 6, 7, 8)] int seed)
        {
            var random = new Random(seed);
            Fill(random.Next(0, 40));
            Initialize();

            for (int step = 0; step < 300; step++)
            {
                int changesThisFrame = random.Next(1, 7);
                for (int i = 0; i < changesThisFrame; i++)
                {
                    string description = ApplyRandomChange(random);
                    AssertCellsShowTheirItems($"seed {seed}, step {step}: right after {description}");
                }

                _scroll.Apply();
                AssertConsistent($"seed {seed}, step {step}: after the layout update");

                if (random.Next(4) == 0)
                {
                    float maxOffset = Mathf.Max(0f, _items.Count * ItemHeight - ViewportHeight);
                    ScrollTo((float)random.NextDouble() * maxOffset);
                    AssertConsistent($"seed {seed}, step {step}: after scrolling");
                }
            }
        }

        // ── Changes ──────────────────────────────────────────────────────────────

        private void Apply(Change change, int index)
        {
            switch (change)
            {
                case Change.Add: _items.Add(NewValue()); break;
                case Change.Insert: _items.Insert(index, NewValue()); break;
                case Change.RemoveAt: _items.RemoveAt(index); break;
                case Change.Replace: _items[index] = NewValue(); break;
                case Change.SwapWithinView: _items.Swap(index, 12); break;
                case Change.SwapIntoView: _items.Swap(index, 25); break;
                case Change.SwapOutOfView: _items.Swap(index, 2); break;
                case Change.Clear: _items.Clear(); break;
                case Change.OverrideWith: _items.OverrideWith(Reordered(new Random(index))); break;
                case Change.OverrideWithEvents: _items.OverrideWithEvents(Reordered(new Random(index))); break;
            }
        }

        private string ApplyRandomChange(Random random)
        {
            int count = _items.Count;
            int kind = count == 0 ? 0 : random.Next(100);
            if (kind < 20)
            {
                _items.Add(NewValue());
                return "Add";
            }
            if (kind < 40)
            {
                int index = random.Next(count + 1);
                _items.Insert(index, NewValue());
                return $"Insert({index})";
            }
            if (kind < 58)
            {
                int index = random.Next(count);
                _items.RemoveAt(index);
                return $"RemoveAt({index})";
            }
            if (kind < 70)
            {
                int index = random.Next(count);
                _items[index] = NewValue();
                return $"Replace({index})";
            }
            if (kind < 85)
            {
                int index1 = random.Next(count);
                int index2 = random.Next(count);
                _items.Swap(index1, index2);
                return $"Swap({index1}, {index2})";
            }
            if (kind < 94)
            {
                _items.OverrideWithEvents(Reordered(random));
                return "OverrideWithEvents";
            }
            if (kind < 98)
            {
                _items.OverrideWith(Reordered(random));
                return "OverrideWith";
            }
            _items.Clear();
            return "Clear";
        }

        // The current items shuffled, some dropped and some new ones added - like a filter or sort change.
        private List<string> Reordered(Random random)
        {
            List<string> result = _items.Where(_ => random.Next(4) != 0).OrderBy(_ => random.Next()).ToList();
            int added = random.Next(0, 6);
            for (int i = 0; i < added; i++)
                result.Insert(random.Next(result.Count + 1), NewValue());
            return result;
        }

        // ── Checks ───────────────────────────────────────────────────────────────

        // Holds at any time, also between a change and the layout update.
        private void AssertCellsShowTheirItems(string when)
        {
            Assert.AreEqual(0, _scroll.DoubleReturns, $"A cell was returned twice ({when}).");
            Assert.AreEqual(_scroll.LiveCells.Count, _scroll.ActiveCells.Count,
                $"Requested cells that aren't returned must all be active - otherwise cells leak ({when}).");
            foreach (KeyValuePair<int, RectTransform> cell in _scroll.ActiveCells)
            {
                Assert.Less(cell.Key, _items.Count, $"Cell for index {cell.Key} beyond the {_items.Count} items ({when}).");
                Assert.AreEqual(_items[cell.Key], cell.Value.name, $"Cell at index {cell.Key} shows the wrong item ({when}).");
                Assert.AreEqual(_layout.ComputeItemPosition(cell.Key), cell.Value.anchoredPosition,
                    $"Cell at index {cell.Key} ('{cell.Value.name}') is at the wrong position ({when}).");
            }
        }

        // Holds after the layout update: additionally, exactly the visible indices have a cell.
        private void AssertConsistent(string when)
        {
            AssertCellsShowTheirItems(when);
            Assert.AreEqual(_layout.ComputeContentSize(_items.Count), _scroll.ContentSize, $"Content size ({when}).");

            (int first, int last) = _layout.GetVisibleIndexRange(_items.Count, _scroll.Viewport);
            List<int> expected = Enumerable.Range(first, Math.Max(0, last - first + 1)).ToList();
            List<int> actual = _scroll.ActiveCells.Keys.OrderBy(i => i).ToList();
            CollectionAssert.AreEqual(expected, actual,
                $"Cells must cover exactly the visible indices {first}-{last} of {_items.Count} ({when}).");
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private void Fill(int count)
        {
            for (int i = 0; i < count; i++)
                _items.Add(NewValue());
        }

        private string NewValue() => $"v{_nextValue++}";

        private void Initialize()
        {
            _scroll.DoInitialize();
            _scroll.DoLateUpdate(); // consumes the deferred first activation
            _scroll.DoLateUpdate(); // activates the visible cells
            AssertConsistent("after initialization");
        }

        // Moves the viewport down by offset pixels - GetViewportRect is content-local and y-up.
        private void ScrollTo(float offset)
        {
            _scroll.Viewport = new Rect(0f, -offset - ViewportHeight, 300f, ViewportHeight);
            _scrollRect.onValueChanged.Invoke(Vector2.zero);
        }

        private static void SetField(object target, string name, object value)
        {
            Type type = target.GetType();
            FieldInfo field = null;
            while (field == null && type != null)
            {
                field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
                type = type.BaseType;
            }
            field.SetValue(target, value);
        }

        // ── Test doubles ─────────────────────────────────────────────────────────

        private sealed class TrackingScrollView : VirtualScrollView<RectTransform, string>
        {
            public ObservableList<string> Source;
            public Rect Viewport = new Rect(0f, -ViewportHeight, 300f, ViewportHeight);
            public readonly HashSet<RectTransform> LiveCells = new HashSet<RectTransform>();
            public int DoubleReturns;

            private static readonly FieldInfo ActiveItemsField =
                typeof(VirtualScrollBase<RectTransform>).GetField("_activeItems", BindingFlags.NonPublic | BindingFlags.Instance);

            public IReadOnlyDictionary<int, RectTransform> ActiveCells =>
                (Dictionary<int, RectTransform>)ActiveItemsField.GetValue(this);

            public Vector2 ContentSize => _contentRect.sizeDelta;

            // Named after the item it shows, so a check can tell which item a cell was requested for.
            protected override RectTransform RequestItem(int index)
            {
                var go = new GameObject(Source[index], typeof(RectTransform));
                go.transform.SetParent(transform);
                var cell = go.GetComponent<RectTransform>();
                LiveCells.Add(cell);
                return cell;
            }

            protected override void ReturnItem(RectTransform item)
            {
                if (!LiveCells.Remove(item))
                    DoubleReturns++;
                if (item != null)
                    Object.DestroyImmediate(item.gameObject);
            }

            protected override Rect GetViewportRect() => Viewport;

            public void DoInitialize()
            {
                SetField(this, "_pool", new NullPool());
                ((Initializable)this).Initialize();
            }

            public void DoLateUpdate() => LateUpdate();
            public void Apply() => ApplyLayoutChanges();
        }

        private sealed class NullPool : Pool<RectTransform, string, PrefabInstantiationArguments>
        {
            public RectTransform Request(string arg1, PrefabInstantiationArguments arg2) => null;
            public void Return(RectTransform item) { }
        }
    }
}
