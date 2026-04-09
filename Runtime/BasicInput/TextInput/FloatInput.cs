namespace Calluna.UI
{
    public class FloatInput : TextInput<float>
    {
        protected override bool TryParseInput(string input, out float result)
            => float.TryParse(input, out result);
    }
}