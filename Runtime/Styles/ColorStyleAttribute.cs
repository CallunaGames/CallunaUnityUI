using System;

namespace Calluna.UI
{
    public class ColorStyleAttribute : Attribute
    {
        public bool IsExample { get; private set; }

        public ColorStyleAttribute()
        {
            IsExample = false;
        }

        public ColorStyleAttribute(bool isExample)
        {
            IsExample = isExample;
        }
    }
}