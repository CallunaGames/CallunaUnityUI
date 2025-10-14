namespace Calluna.UI
{
    public class FloatSlider : SliderInput<float>
    {
        protected override float ParseValue(float value)
        {
            return value;
        }

        protected override float ParseInput(float input)
        {
            return input;
        }
    }
}
