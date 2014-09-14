namespace OctoGhast.DataStructures
{
    public interface IColor
    {
        byte Red { get; }
        byte Green { get; }
        byte Blue { get; }

        float Hue { get; }
        float Saturation { get; }
        float Value { get; }

        IColor ScaleSaturation(float scalar);
        IColor ScaleValue(float scalar);

        IColor AdditiveBlend(IColor with);
        IColor SubtractiveBlend(IColor with);

        IColor ChangeHue(float hue);
        IColor ChangeSaturation(float saturation);
        IColor ChangeValue(float value);

        string ForegroundCode();
        string BackgroundCode();
        string DefaultColorCode();
    }
}