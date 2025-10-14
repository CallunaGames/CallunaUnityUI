using System;

namespace Calluna.UI
{
    public class IntInput : TextInput<int>
    {
        protected override int ParseInput(string input)
        {
            if (!int.TryParse(input, out int result))
            {
                throw new ArgumentException();
            }

            return result;
        }
    }
}