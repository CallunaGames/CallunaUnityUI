using NUnit.Framework;
using UnityEngine;

namespace Calluna.UI.Tests
{
    public class DragableUITests
    {
        // Bounds: rect from (0,0) to (200,100), centred at (100,50).
        private static readonly Rect Bounds = new Rect(0f, 0f, 200f, 100f);

        // Element: 40×20, centre-pivot (0.5, 0.5).
        private static readonly Vector2 Size  = new Vector2(40f, 20f);
        private static readonly Vector2 Pivot = new Vector2(0.5f, 0.5f);

        [Test]
        public void DragableUI_ClampPositionToBounds_InsideBounds_Unchanged()
        {
            Vector2 position = new Vector2(100f, 50f);
            Vector2 result = UIBoundsConstrainer.ClampPositionToBounds(position, Size, Pivot, Bounds);
            Assert.AreEqual(position, result);
        }

        [Test]
        public void DragableUI_ClampPositionToBounds_OverflowsLeft_ClampsToLeftEdge()
        {
            // Element centre at x=5 → left edge at 5 - 20 = -15, outside bounds min x=0.
            Vector2 position = new Vector2(5f, 50f);
            Vector2 result = UIBoundsConstrainer.ClampPositionToBounds(position, Size, Pivot, Bounds);
            Assert.AreEqual(20f, result.x, 0.0001f); // centre pushed right so left edge = 0
            Assert.AreEqual(50f, result.y, 0.0001f);
        }

        [Test]
        public void DragableUI_ClampPositionToBounds_OverflowsRight_ClampsToRightEdge()
        {
            // Element centre at x=195 → right edge at 195 + 20 = 215, outside bounds max x=200.
            Vector2 position = new Vector2(195f, 50f);
            Vector2 result = UIBoundsConstrainer.ClampPositionToBounds(position, Size, Pivot, Bounds);
            Assert.AreEqual(180f, result.x, 0.0001f); // centre pulled left so right edge = 200
            Assert.AreEqual(50f, result.y, 0.0001f);
        }

        [Test]
        public void DragableUI_ClampPositionToBounds_OverflowsTop_ClampsToTopEdge()
        {
            // Element centre at y=95 → top edge at 95 + 10 = 105, outside bounds max y=100.
            Vector2 position = new Vector2(100f, 95f);
            Vector2 result = UIBoundsConstrainer.ClampPositionToBounds(position, Size, Pivot, Bounds);
            Assert.AreEqual(100f, result.x, 0.0001f);
            Assert.AreEqual(90f,  result.y, 0.0001f); // centre pulled down so top edge = 100
        }

        [Test]
        public void DragableUI_ClampPositionToBounds_OverflowsBottom_ClampsToBottomEdge()
        {
            // Element centre at y=5 → bottom edge at 5 - 10 = -5, outside bounds min y=0.
            Vector2 position = new Vector2(100f, 5f);
            Vector2 result = UIBoundsConstrainer.ClampPositionToBounds(position, Size, Pivot, Bounds);
            Assert.AreEqual(100f, result.x, 0.0001f);
            Assert.AreEqual(10f,  result.y, 0.0001f); // centre pushed up so bottom edge = 0
        }

        [Test]
        public void DragableUI_ClampPositionToBounds_OverflowsBothAxes_ClampsOnBothAxes()
        {
            Vector2 position = new Vector2(-50f, -50f);
            Vector2 result = UIBoundsConstrainer.ClampPositionToBounds(position, Size, Pivot, Bounds);
            Assert.AreEqual(20f, result.x, 0.0001f);
            Assert.AreEqual(10f, result.y, 0.0001f);
        }

        [Test]
        public void DragableUI_ClampPositionToBounds_TopLeftPivot_ShiftsBoundsCheckCorrectly()
        {
            // Top-left pivot (0, 1): position IS the top-left corner of the element.
            // Element 40×20 placed at (0,100) — top-left corner exactly on top-left of bounds.
            Vector2 pivot    = new Vector2(0f, 1f);
            Vector2 position = new Vector2(0f, 100f);
            Vector2 result = UIBoundsConstrainer.ClampPositionToBounds(position, Size, pivot, Bounds);
            Assert.AreEqual(position, result);
        }

        [Test]
        public void DragableUI_ClampPositionToBounds_TopLeftPivot_OverflowsLeft_Clamped()
        {
            // Top-left pivot: position is top-left corner. At x=-10, left edge is outside.
            Vector2 pivot    = new Vector2(0f, 1f);
            Vector2 position = new Vector2(-10f, 50f);
            Vector2 result = UIBoundsConstrainer.ClampPositionToBounds(position, Size, pivot, Bounds);
            Assert.AreEqual(0f, result.x, 0.0001f);
        }

        [Test]
        [Description("ClampPositionToBounds with element wider than bounds => result x is finite and deterministic (no NaN)?")]
        public void DragableUI_ClampPositionToBounds_ElementWiderThanBounds_ResultXIsFinite()
        {
            // Element width 300 > bounds width 200, centre-pivot, centred inside bounds.
            // minX = 100 - 150 = -50  → deltaMinX = -50 - 0 = -50
            // maxX = 100 + 150 = 250  → deltaMaxX = 250 - 200 = 50
            // result.x = 100 - (-50) - 50 = 100  (forces cancel, x unchanged)
            Vector2 oversizeW = new Vector2(300f, 20f);
            Vector2 result = UIBoundsConstrainer.ClampPositionToBounds(new Vector2(100f, 50f), oversizeW, Pivot, Bounds);
            Assert.IsFalse(float.IsNaN(result.x), "result.x must not be NaN");
            Assert.IsFalse(float.IsInfinity(result.x), "result.x must not be infinite");
            Assert.AreEqual(100f, result.x, 0.0001f);
        }

        [Test]
        [Description("ClampPositionToBounds with element taller than bounds => result y is finite and deterministic (no NaN)?")]
        public void DragableUI_ClampPositionToBounds_ElementTallerThanBounds_ResultYIsFinite()
        {
            // Element height 300 > bounds height 100, centre-pivot, centred inside bounds.
            // minY = 50 - 150 = -100  → deltaMinY = -100 - 0 = -100
            // maxY = 50 + 150 = 200   → deltaMaxY = 200 - 100 = 100
            // result.y = 50 - (-100) - 100 = 50  (forces cancel, y unchanged)
            Vector2 oversizeH = new Vector2(40f, 300f);
            Vector2 result = UIBoundsConstrainer.ClampPositionToBounds(new Vector2(100f, 50f), oversizeH, Pivot, Bounds);
            Assert.IsFalse(float.IsNaN(result.y), "result.y must not be NaN");
            Assert.IsFalse(float.IsInfinity(result.y), "result.y must not be infinite");
            Assert.AreEqual(50f, result.y, 0.0001f);
        }
    }
}
