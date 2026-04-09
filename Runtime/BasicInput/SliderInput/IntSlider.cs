namespace Calluna.UI
{
    public class IntSlider : SliderInput<int>
    {
        protected override float ToSliderValue(int value) => (float)value;

        protected override int FromSliderValue(float sliderValue) => (int)sliderValue;
    }
}