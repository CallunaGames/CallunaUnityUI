using System;

namespace Calluna.UI
{
    public class FloatInput : TextInput<float>
    {
        protected override float ParseInput(string input)
        {
            if (!float.TryParse(input, out float result))
            {
                throw new ArgumentException();
            }

            return result;
        }
    }
}