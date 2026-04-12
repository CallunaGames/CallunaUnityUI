using UnityEngine;

namespace Calluna.UI
{
    /// <summary>
    /// Defines the geometry contract for a virtual scroll layout.
    /// Implementations are stateless pure-math classes — no Unity object references.
    /// </summary>
    public interface IScrollLayout
    {
        /// <summary>The size of a single item cell in pixels.</summary>
        Vector2 ItemSize { get; }

        /// <summary>Returns the total size the content RectTransform should be set to.</summary>
        Vector2 ComputeContentSize(int itemCount);

        /// <summary>
        /// Returns the anchoredPosition for the item at the given index.
        /// Assumes content anchor and pivot are at top-left (0,1).
        /// </summary>
        Vector2 ComputeItemPosition(int index);

        /// <summary>
        /// Returns the first and last item indices (inclusive) that overlap the visible viewport.
        /// viewportLocalRect is the viewport rect expressed in content local space.
        /// This computation is O(1) — no iteration over all items.
        /// </summary>
        (int first, int last) GetVisibleIndexRange(int itemCount, Rect viewportLocalRect);
    }
}
