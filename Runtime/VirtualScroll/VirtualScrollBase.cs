using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Calluna.UI
{
    /// <summary>
    /// Non-generic root for all virtualised scroll view types. Provides the
    /// shared DI binding ID used to resolve the scroll <see cref="ValueTweener{TValue}"/>.
    /// </summary>
    public abstract class VirtualScrollBase : MonoBehaviour
    {
        /// <summary>DI ID under which <c>ValueTweener&lt;float&gt;</c> is registered.</summary>
        public const string ScrollTweenerId = "virtualscroll-tweener";
    }

    /// <summary>
    /// Non-generic shared logic for virtualised scroll views.
    /// Handles scroll events, visible-range computation, item placement,
    /// and pool request/return dispatch. Layout-agnostic — works with any
    /// <see cref="IScrollLayout"/> implementation (grid, vertical list, horizontal list, etc.).
    /// </summary>
    public abstract class VirtualScrollBase<TItem> : VirtualScrollBase
        where TItem : Component
    {
        [SerializeField] private ScrollRect _scrollRect;

        protected IScrollLayout _layout;
        protected RectTransform _contentRect;
        protected ValueTweener<float> _scrollTweener;

        private readonly Dictionary<int, TItem> _activeItems = new();
        private readonly List<int> _recycleBuffer   = new();
        private readonly Vector3[] _viewportCorners = new Vector3[4];

        // Cached static delegate — avoids a heap allocation on every insert-shift operation.
        private static readonly Comparison<int> _descendingComparison = (a, b) => b.CompareTo(a);

        protected bool _isDirty;
        private bool _initialized;
        private bool _deferFirstActivation;
        private Vector2 _lastViewportSize;
        private RectTransform _viewport;

        // ── Overridable by concrete variants ────────────────────────────────────

        protected abstract int ItemCount { get; }

        /// <summary>Request a cell for <paramref name="index"/> from the pool.</summary>
        protected abstract TItem RequestItem(int index);

        /// <summary>Return a cell to the pool.</summary>
        protected abstract void ReturnItem(TItem item);

        // ── Shared lifecycle helpers ─────────────────────────────────────────────

        /// <summary>
        /// Call from <c>Initialize()</c> after setting <see cref="_layout"/>.
        /// Configures the content RectTransform and subscribes to scroll events.
        /// Does an initial <see cref="Rebuild"/>.
        /// </summary>
        protected void InitializeBase()
        {
            _contentRect = _scrollRect.content;
            _viewport    = _scrollRect.viewport;

            // Content must use top-left anchor/pivot so anchoredPosition grows downward.
            _contentRect.anchorMin        = new Vector2(0f, 1f);
            _contentRect.anchorMax        = new Vector2(0f, 1f);
            _contentRect.pivot            = new Vector2(0f, 1f);
            _contentRect.anchoredPosition = Vector2.zero;

            // Stop inertia before subscribing so the ScrollRect cannot drive the content
            // away from anchoredPosition zero during the two-frame deferred activation window.
            _scrollRect.StopMovement();

            _initialized          = true;
            _deferFirstActivation = true;
            _scrollRect.onValueChanged.AddListener(OnScrolled);
            ResizeContent();
        }

        /// <summary>Call from <c>Clean()</c>. Unsubscribes from scroll and returns all active items.</summary>
        protected void CleanBase()
        {
            // Guard must be cleared before ReturnAll so LateUpdate cannot re-activate items
            // while the pool is mid-teardown.
            _initialized          = false;
            _deferFirstActivation = false;
            _lastViewportSize     = Vector2.zero;
            _viewport             = null;
            _scrollRect.onValueChanged.RemoveListener(OnScrolled);
            _scrollTweener?.Stop();
            _scrollRect.StopMovement();
            ReturnAll();
        }

        protected void SetDirty() => _isDirty = true;

        // ── Fine-grained update helpers ──────────────────────────────────────────

        /// <summary>Resizes the content rect to match the current item count.</summary>
        protected void ResizeContent()
            => _contentRect.sizeDelta = _layout.ComputeContentSize(ItemCount);

        /// <summary>
        /// Returns the active cell at <paramref name="index"/> to the pool and removes it from
        /// the active set. Does nothing when the index is not active.
        /// </summary>
        protected void ReturnActiveItemAt(int index)
        {
            if (!_activeItems.TryGetValue(index, out TItem item)) return;
            ReturnItem(item);
            _activeItems.Remove(index);
        }

        /// <summary>
        /// If the item at <paramref name="index"/> is currently active, returns it to the pool
        /// and immediately re-requests it so the cell receives fresh data via DI injection.
        /// Does nothing when the index is not visible.
        /// </summary>
        protected void ReplaceActiveItem(int index)
        {
            if (!_activeItems.TryGetValue(index, out TItem item)) return;
            ReturnItem(item);
            _activeItems.Remove(index);
            ActivateItem(index);
        }

        /// <summary>
        /// Re-keys every active item whose index is ≥ <paramref name="fromIndex"/> by
        /// <paramref name="delta"/> (+1 for insert, −1 for remove) and repositions each one.
        /// </summary>
        protected void ShiftActiveItems(int fromIndex, int delta)
        {
            _recycleBuffer.Clear();
            foreach (int key in _activeItems.Keys)
                if (key >= fromIndex) _recycleBuffer.Add(key);

            // Descending order for positive delta (insert) to avoid key collisions.
            if (delta > 0) _recycleBuffer.Sort(_descendingComparison);
            else           _recycleBuffer.Sort();

            foreach (int key in _recycleBuffer)
            {
                TItem item = _activeItems[key];
                _activeItems.Remove(key);
                int newKey = key + delta;
                _activeItems[newKey] = item;
                ((RectTransform)item.transform).anchoredPosition = _layout.ComputeItemPosition(newKey);
            }
        }

        /// <summary>
        /// Scrolls so that the item at <paramref name="index"/> is visible according to
        /// <paramref name="alignment"/>. When <paramref name="duration"/> is greater than zero
        /// the scroll is animated using the supplied <paramref name="tweenType"/>; otherwise it
        /// snaps immediately. Requires <see cref="InitializeBase"/> to have been called first.
        /// If <see cref="CoroutineHelper"/> was not resolved, animation degrades to instant snap.
        /// <para>
        /// Note: if called before the Canvas has laid out (e.g. directly after Initialize),
        /// call <c>Canvas.ForceUpdateCanvases()</c> first to ensure viewport dimensions are correct.
        /// </para>
        /// </summary>
        protected void ScrollToIndex(int index, ScrollAlignment alignment, float duration = 0f,
            TweenType tweenType = TweenType.EaseInOutSine)
        {
            if (!_initialized || index < 0 || index >= ItemCount) return;

            Vector2 itemPos     = _layout.ComputeItemPosition(index);
            Vector2 itemSize    = _layout.ItemSize;
            Vector2 contentSize = _layout.ComputeContentSize(ItemCount);
            Vector2 vpSize      = _viewport.rect.size;

            float maxScrollX = Mathf.Max(0f, contentSize.x - vpSize.x);
            float maxScrollY = Mathf.Max(0f, contentSize.y - vpSize.y);

            // anchoredPosition: x positive-right, y negative-down → negate y for pixel offset from top
            float itemLeft = itemPos.x;
            float itemTop  = -itemPos.y;

            (float rawX, float rawY) = alignment switch
            {
                ScrollAlignment.Center => (itemLeft + itemSize.x * 0.5f - vpSize.x * 0.5f,
                                           itemTop  + itemSize.y * 0.5f - vpSize.y * 0.5f),
                ScrollAlignment.End    => (itemLeft + itemSize.x - vpSize.x,
                                           itemTop  + itemSize.y - vpSize.y),
                _                      => (itemLeft, itemTop),
            };

            float normX = maxScrollX > 0f ? Mathf.Clamp01(rawX / maxScrollX)        : 0f;
            float normY = maxScrollY > 0f ? 1f - Mathf.Clamp01(rawY / maxScrollY) : 0f;

            if (duration <= 0f || _scrollTweener == null)
            {
                if (maxScrollX > 0f) _scrollRect.horizontalNormalizedPosition = normX;
                if (maxScrollY > 0f) _scrollRect.verticalNormalizedPosition   = normY;
                return;
            }

            float startX = _scrollRect.horizontalNormalizedPosition;
            float startY = _scrollRect.verticalNormalizedPosition;
            _scrollTweener.Perform(0f, 1f, duration, tweenType, t =>
            {
                if (maxScrollX > 0f)
                    _scrollRect.horizontalNormalizedPosition = Mathf.LerpUnclamped(startX, normX, t);
                if (maxScrollY > 0f)
                    _scrollRect.verticalNormalizedPosition   = Mathf.LerpUnclamped(startY, normY, t);
            });
        }

        // ── Unity messages ───────────────────────────────────────────────────────

        protected virtual void Reset()
        {
            _scrollRect = GetComponent<ScrollRect>();
        }

        protected virtual void OnApplicationQuit() => CleanBase();

        protected virtual void LateUpdate()
        {
            if (!_initialized) return;

            if (_deferFirstActivation)
            {
                _deferFirstActivation = false;
                return;
            }

            Vector2 viewportSize = _viewport.rect.size;
            if (viewportSize != _lastViewportSize)
            {
                _lastViewportSize = viewportSize;
                RefreshVisibleItems();
            }

            if (!_isDirty) return;
            _isDirty = false;
            Rebuild();
        }

        // ── Core logic ───────────────────────────────────────────────────────────

        private void OnScrolled(Vector2 _) => RefreshVisibleItems();

        protected void Rebuild()
        {
            _contentRect.sizeDelta = _layout.ComputeContentSize(ItemCount);
            ReturnAll();
            RefreshVisibleItems();
        }

        /// <summary>
        /// Reconciles the active cell set with the current visible index range:
        /// returns cells that scrolled out of view and activates cells that scrolled in.
        /// </summary>
        protected void RefreshVisibleItems()
        {
            (int first, int last) = _layout.GetVisibleIndexRange(ItemCount, GetViewportRect());

            _recycleBuffer.Clear();
            foreach (int index in _activeItems.Keys)
            {
                if (index < first || index > last)
                    _recycleBuffer.Add(index);
            }
            foreach (int index in _recycleBuffer)
            {
                ReturnItem(_activeItems[index]);
                _activeItems.Remove(index);
            }

            for (int i = first; i <= last; i++)
            {
                if (_activeItems.ContainsKey(i)) continue;
                ActivateItem(i);
            }
        }

        private void ActivateItem(int index)
        {
            TItem item = RequestItem(index);
            var rt = (RectTransform)item.transform;
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(0f, 1f);
            rt.pivot            = new Vector2(0f, 1f);
            rt.anchoredPosition = _layout.ComputeItemPosition(index);
            rt.sizeDelta        = _layout.ItemSize;
            _activeItems[index] = item;
        }

        private void ReturnAll()
        {
            _recycleBuffer.Clear();
            _recycleBuffer.AddRange(_activeItems.Keys);
            foreach (int key in _recycleBuffer)
            {
                ReturnItem(_activeItems[key]);
                _activeItems.Remove(key);
            }
        }

        /// <summary>
        /// Viewport rect in content-local space (Unity y-up).
        /// yMax = top of viewport (less negative), yMin = bottom (more negative).
        /// Override in tests to inject a fixed rect without needing a live ScrollRect.
        /// </summary>
        protected virtual Rect GetViewportRect()
        {
            _viewport.GetWorldCorners(_viewportCorners);
            // corners: [0]=BL  [1]=TL  [2]=TR  [3]=BR
            Vector2 topLeft     = _contentRect.InverseTransformPoint(_viewportCorners[1]);
            Vector2 bottomRight = _contentRect.InverseTransformPoint(_viewportCorners[3]);
            return new Rect(topLeft.x, bottomRight.y,
                            bottomRight.x - topLeft.x,
                            topLeft.y     - bottomRight.y);
        }
    }
}
