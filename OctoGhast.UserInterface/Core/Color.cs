using System;
using System.Globalization;
using System.Text;
using libtcod;
using OctoGhast.DataStructures;

namespace OctoGhast.UserInterface.Core
{
    public static class ColorExtensions
    {
        public static TCODColor ToTcodColor(this IColor color)
        {
            return new TCODColor(color.Red, color.Green, color.Blue);
        }
    }

    public class Color : IColor
    {
        public byte Red { get; private set; }

        public byte Green { get; private set; }

        public byte Blue { get; private set; }

        public float Hue { get; private set; }

        public float Saturation { get; private set; }

        public float Value { get; private set; }

        public Color(TCODColor color)
        {
            if (color == null)
                throw new ArgumentNullException("color");

            Red = color.Red;
            Green = color.Green;
            Blue = color.Blue;
        }

        public Color(byte red, byte green, byte blue)
        {
            Red = red;
            Green = green;
            Blue = blue;
        }

        public Color(long packedColor)
        {
            long r, g, b;

            r = packedColor & 0xFF0000;
            g = packedColor & 0x00FF00;
            b = packedColor & 0x0000FF;

            r = r >> 16;
            g = g >> 8;

            Red = (byte)r;
            Green = (byte)g;
            Blue = (byte)b;
        }

        public IColor ScaleSaturation(float scalar)
        {
            float h, s, v;

            var color = this.ToTcodColor();
            color.getHSV(out h, out s, out v);
            color.setHSV(h, s * scalar, v);

            return new Color(color);
        }

        public IColor ScaleValue(float scalar)
        {
            float h, s, v;

            var color = this.ToTcodColor();
            color.getHSV(out h, out s, out v);
            color.setHSV(h, s, v * scalar);

            return new Color(color);
        }

        public IColor ChangeHue(float hue)
        {
            float h, s, v;

            var color = this.ToTcodColor();
            color.getHSV(out h, out s, out v);
            color.setHSV(hue, s, v);

            return new Color(color);
        }

        public IColor ChangeSaturation(float saturation)
        {
            float h, s, v;

            var color = this.ToTcodColor();
            color.getHSV(out h, out s, out v);
            color.setHSV(h, saturation, v);

            return new Color(color);
        }

        public IColor ChangeValue(float value)
        {
            float h, s, v;

            var color = this.ToTcodColor();
            color.getHSV(out h, out s, out v);
            color.setHSV(h, s, value);

            return new Color(color);
        }

        internal const char CodeForeground = '\x06';
        internal const char CodeBackground = '\x07';
        internal const char CodeStop = '\x08';

        private string ColorEncode()
        {
            var r = Math.Max(Red, (byte)1);
            var g = Math.Max(Green, (byte)1);
            var b = Math.Max(Blue, (byte)1);

            char[] c = Encoding.ASCII.GetChars(new[] { r, g, b });

            var str = String.Format("{0}{1}{2}", c[0], c[1], c[2]);
            return str;
        }

        public string ForegroundCode()
        {
            return CodeForeground + ColorEncode();
        }

        public string BackgroundCode()
        {
            return CodeBackground + ColorEncode();
        }

        public string DefaultColorCode()
        {
            return CodeStop.ToString(CultureInfo.InvariantCulture);
        }

        public static implicit operator TCODColor(Color color) {
            return color.ToTcodColor();
        }
    }
}