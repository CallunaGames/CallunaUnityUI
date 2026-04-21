using UnityEngine;

namespace Calluna.UI
{
    public interface IUIBoundsConstrainer
    {
        Rect? GetBoundsRect();
        void Clamp(RectTransform target);
    }
}
