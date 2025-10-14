namespace Calluna.UI
{
    public class StringInput : TextInput<string>
    {
        protected override string ParseInput(string input)
        {
            return input;
        }
    }
}
