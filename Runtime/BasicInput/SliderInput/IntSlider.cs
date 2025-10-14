namespace Calluna.UI
{
    public class IntSlider : SliderInput<int>
    {
        protected override float ParseValue(int value)
        {
            return (float)value;
        }

        protected override int ParseInput(float input)
        {
            return (int)input;
        }
    }
}