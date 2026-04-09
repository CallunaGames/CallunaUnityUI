namespace Calluna.UI
{
    public class IntInput : TextInput<int>
    {
        protected override bool TryParseInput(string input, out int result)
            => int.TryParse(input, out result);
    }
}