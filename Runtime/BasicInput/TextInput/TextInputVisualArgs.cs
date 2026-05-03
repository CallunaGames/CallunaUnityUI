using UnityEngine;
using UnityEngine.UI;

namespace Calluna.UI
{
    public class TextInputVisualArgs
    {
        public readonly Graphic Target;
        public readonly Color InvalidColor;

        public TextInputVisualArgs(Graphic target, Color invalidColor)
        {
            Target = target;
            InvalidColor = invalidColor;
        }
    }
}
