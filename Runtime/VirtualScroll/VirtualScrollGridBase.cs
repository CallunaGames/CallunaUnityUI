using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Calluna.UI
{
    /// <summary>
    /// Non-generic shared logic for both virtualised grid variants.
    /// Handles scroll events, visible-range computation, item placement,
    /// and pool request/return dispatch.
    /// </summary>
    public abstract class VirtualScrollGridBase<TItem> : MonoBehaviour
        where TItem : Component
    {
        [SerializeField] private ScrollRect _scrollRect;

        protected IScrollLayout _layout;
        protected RectTransform _contentRect;

        private readonly Dictionary<int, TItem> _activeItems = new();
        private readonly List<int> _recycleBuffer   = new();
        private readonly Vector3[] _viewportCorners = new Vector3[4];

        protected bool _isDirty;
        private Vector2 _lastViewportSize;

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

            // Content must use top-left anchor/pivot so anchoredPosition grows downward.
            _contentRect.anchorMin        = new Vector2(0f, 1f);
            _contentRect.anchorMax        = new Vector2(0f, 1f);
            _contentRect.pivot            = new Vector2(0f, 1f);
            _contentRect.anchoredPosition = Vector2.zero;

            _scrollRect.onValueChanged.AddListener(OnScrolled);
            Rebuild();
        }

        /// <summary>Call from <c>Clean()</c>. Unsubscribes from scroll and returns all active items.</summary>
        protected void CleanBase()
        {
            _scrollRect.onValueChanged.RemoveListener(OnScrolled);
            ReturnAll();
        }

        protected void SetDirty() => _isDirty = true;

        // ── Unity messages ───────────────────────────────────────────────────────

        protected virtual void Reset()
        {
            _scrollRect = GetComponent<ScrollRect>();
        }

        private void LateUpdate()
        {
            Vector2 viewportSize = _scrollRect.viewport.rect.size;
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

        private void RefreshVisibleItems()
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

                TItem item = RequestItem(i);
                var rt = (RectTransform)item.transform;
                rt.anchorMin        = new Vector2(0f, 1f);
                rt.anchorMax        = new Vector2(0f, 1f);
                rt.pivot            = new Vector2(0f, 1f);
                rt.anchoredPosition = _layout.ComputeItemPosition(i);
                rt.sizeDelta        = _layout.ItemSize;
                _activeItems[i]     = item;
            }
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
        /// </summary>
        private Rect GetViewportRect()
        {
            _scrollRect.viewport.GetWorldCorners(_viewportCorners);
            // corners: [0]=BL  [1]=TL  [2]=TR  [3]=BR
            Vector2 topLeft     = _contentRect.InverseTransformPoint(_viewportCorners[1]);
            Vector2 bottomRight = _contentRect.InverseTransformPoint(_viewportCorners[3]);
            return new Rect(topLeft.x, bottomRight.y,
                            bottomRight.x - topLeft.x,
                            topLeft.y     - bottomRight.y);
        }
    }
}
