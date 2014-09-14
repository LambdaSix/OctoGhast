using System;
using System.Globalization;
using System.Text;
using OctoGhast.DataStructures;

using XColor = Microsoft.Xna.Framework.Color;

namespace OctoGhast.UserInterface.Core
{
    public class Color : IColor
    {
        public byte Red { get; private set; }

        public byte Green { get; private set; }

        public byte Blue { get; private set; }

        public float Alpha { get; private set; }

        public float Hue { get; private set; }

        public float Saturation { get; private set; }

        public float Value { get; private set; }

        public Color(XColor color)
        {
            if (color == null)
                throw new ArgumentNullException("color");

            Red = color.R;
            Green = color.G;
            Blue = color.B;
            Alpha = color.A;
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

            GetHsv(this, out h, out s, out v);
            return SetHsv(h, s*scalar, v);
        }

        public IColor ScaleValue(float scalar)
        {
            float h, s, v;

            GetHsv(this, out h, out s, out v);
            return SetHsv(h, s, v*scalar);
        }

        public IColor AdditiveBlend(IColor with) {
            return Add(this, with);
        }

        public IColor SubtractiveBlend(IColor with) {
            return Subtract(this, with);
        }

        public IColor ChangeHue(float hue)
        {
            float h, s, v;

            GetHsv(this, out h, out s, out v);
            return SetHsv(hue, s, v);
        }

        public IColor ChangeSaturation(float saturation)
        {
            float h, s, v;

            GetHsv(this, out h, out s, out v);
            return SetHsv(h, saturation, v);
        }

        public IColor ChangeValue(float value)
        {
            float h, s, v;

            GetHsv(this, out h, out s, out v);
            return SetHsv(h, s, v);
        }

        private static Color Add(IColor colorA, IColor colorB) {
            int r, g, b;

            r = colorA.Red + colorB.Red;
            g = colorA.Green + colorB.Green;
            b = colorA.Blue + colorB.Blue;

            r = Math.Min(255, r);
            g = Math.Min(255, g);
            b = Math.Min(255, b);

            return new Color((byte) r, (byte) g, (byte) b);
        }

        private static Color Subtract(IColor colorA, IColor colorB) {
            int r, g, b;

            r = colorA.Red - colorB.Red;
            g = colorA.Green - colorB.Green;
            b = colorA.Blue - colorB.Blue;

            r = Math.Max(0, r);
            g = Math.Max(0, g);
            b = Math.Max(0, b);

            return new Color((byte) r, (byte) g, (byte) b);
        }

        private static byte ClampValue(float value) {
            return (byte) (value*255.0f + 0.5f);
        }

        private static Color SetHsv(float hue, float saturation, float value) {
            int i;
            float f, p, q, t;
            byte r, g, b;

            if (saturation == 0) {
                // achromatic (grey)
                r = g = b = ClampValue(value);
                return new Color(r, g, b);
            }

            hue /= 60; // Sector 0-5
            i = (int) Math.Floor(hue);
            f = hue - i;
            p = value*(1 - saturation);
            q = value*(1 - saturation*f);
            t = value*(1 - saturation*(1 - f));

            switch (i) {
                case 0: {
                    r = ClampValue(value);
                    g = ClampValue(t);
                    b = ClampValue(p);
                    break;
                }
                case 1: {
                    r = ClampValue(q);
                    g = ClampValue(value);
                    b = ClampValue(p);
                    break;
                }
                case 2: {
                    r = ClampValue(p);
                    g = ClampValue(value);
                    b = ClampValue(t);
                    break;
                }
                case 3: {
                    r = ClampValue(p);
                    g = ClampValue(q);
                    b = ClampValue(value);
                    break;
                }
                case 4: {
                    r = ClampValue(t);
                    g = ClampValue(p);
                    b = ClampValue(value);
                    break;
                }
                default: {
                    r = ClampValue(value);
                    g = ClampValue(p);
                    b = ClampValue(q);
                    break;
                }
            }

            return new Color(r, g, b);
        }

        private static void GetHsv(Color color, out float h, out float s, out float v) {
            float min, max, delta;
            min = Math.Min(color.Red, Math.Min(color.Green, color.Blue));
            max = Math.Max(color.Red, Math.Max(color.Green, color.Blue));

            v = max;

            delta = max - min;

            if (max != 0) {
                s = (delta/max);
            }
            else {
                // r = g = b = 0
                s = 0;
                h = -1;
                return;
            }

            if (color.Red == max) {
                h = (color.Green - color.Blue) / delta;  // Between yellow & magenta
            }
            else if (color.Green == max) {
                h = 2 + (color.Blue - color.Red)/delta;  // Between cyan & yellow
            }
            else {
                h = 4 + (color.Red - color.Green)/delta; // Between magenta and cyan
            }

            h *= 60;                                     // Degrees
            if (h < 0)
                h += 360;
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

        private XColor ToMonogameColor() {
            return new Microsoft.Xna.Framework.Color(Red, Green, Blue);
        }

        public static implicit operator XColor(Color color) {
            return color.ToMonogameColor();
        }

        public static implicit operator Color(XColor color) {
            return new Color(color);
        }

        public static IColor operator +(Color self, Color other) {
            return self.AdditiveBlend(other);
        }

        public static IColor operator -(Color self, Color other) {
            return self.SubtractiveBlend(other);
        }

        #region Pre-defined Colors

        public Color AliceBlue {
            get { return new Color(XColor.AliceBlue); }
        }

        #endregion
    }
}