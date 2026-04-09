namespace Calluna.UI
{
    public class StringInput : TextInput<string>
    {
        protected override bool TryParseInput(string input, out string result)
        {
            result = input;
            return true;
        }
    }
}
