namespace Calluna.UI
{
    public class FloatSlider : SliderInput<float>
    {
        protected override float ToSliderValue(float value) => value;

        protected override float FromSliderValue(float sliderValue) => sliderValue;
    }
}
