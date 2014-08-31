using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RenderLike
{
    public enum FontLayout
    {
        InRow,
        InColumn,
        TCOD
    }

    public enum FontType
    {
        /// <summary>
        /// Key Colour determined by the space character, all pixels with that colour are
        /// transparent, all other pixel colours opaque.
        /// </summary>
        NoAA,

        /// <summary>
        /// Transparency determined by the alpha channel of the image
        /// </summary>
        AlphaAA,

        /// <summary>
        /// Transparency determined by the brightness of pixels, black fully opaque, white opaque.
        /// </summary>
        GreyscaleAA
    }

    public enum SpecialChar : short
    {
        HorizontalLine = 196,
        VerticalLine = 179,
        NorthEastLine = 191,
        NorthWestLine = 218,
        SouthEastLine = 217,
        SouthWestLine = 192,

        DoubleHorzLine = 205,
        DoubleVertLine = 186,
        DoubleNorthEast = 187,
        DoubleNorthWest = 201,
        DoubleSouthEast = 188,
        DoubleSouthWest = 200,

        TeeWest = 180,
        TeeEast = 195,
        TeeNorth = 193,
        TeeSouth = 194,

        DoubleTeeWest = 185,
        DoubleTeeEast = 204,
        DoubleTeeNorth = 202,
        DoubleTeeSouth = 203,

        CrossLines = 197,
        DoubleCrossLines = 206,

        Block1 = 176,
        Block2 = 177,
        Block3 = 178,

        ArrowNorth = 24,
        ArrowSouth = 25,
        ArrowEast = 26,
        ArrowWest = 27,

        ArrowNorthNoTail = 30,
        ArrowSouthNoTail = 31,
        ArrowEastNoTail = 16,
        ArrowWestNoTail = 17,

        DoubleArrowHorz = 29,
        DoubleArrowVert = 18,

        CheckBoxUnset = 224,
        CheckBoxSet = 225,
        RadioUnset = 9,
        RadioSet = 10,

        SubpixelNorthWest = 226,
        SubpixelNorthEast = 227,
        SubpixelNorth = 228,
        SubpixelSouthEast = 229,
        SubpixelDiagonal = 230,
        SubpixelEast = 231,
        SubpixelSouthWest = 232,

        Smilie = 1,
        SmilieInverse = 2,
        Heart = 3,
        Diamond = 4,
        Club = 5,
        Spade = 6,
        Bullet = 7,
        BulletInverse = 8,
        Male = 11,
        Female = 12,
        Note = 13,
        NoteDouble = 14,
        Light = 15,
        ExclamationDouble = 19,
        Pilcrow = 20,
        Section = 21,
        Pound = 156,
        Multiplication = 158,
        Function = 159,
        Reserved = 169,
        Half = 171,
        OneQuarter = 172,
        Copyright = 184,
        Cent = 189,
        Yen = 190,
        Currency = 207,
        ThreeQuarters = 243,
        Division = 246,
        Umlaut = 249,
        Power1 = 251,
        Power3 = 252,
        Power2 = 253,
        BulletSquare = 254
    }

    public class Font
    {
        public int CharacterWidth { get; private set; }
        public int CharacterHeight { get; private set; }

        public int RowCount { get; private set; }
        public int ColumnCount { get; private set; }

        internal Texture2D Texture { get; set; }

        private Point[] charMap;
        private const byte SolidChar = 0xFF;

        public Font() {
            charMap = new Point[256];
        }

        public void MapAsciiCode(char asciiCode, int fontCharX, int fontCharY) {
            if (fontCharX < 0 || fontCharX >= ColumnCount)
                throw new ArgumentOutOfRangeException("fontCharX");
            if (fontCharY < 0 || fontCharY >= RowCount)
                throw new ArgumentOutOfRangeException("fontCharY");

            charMap[asciiCode].X = fontCharX;
            charMap[asciiCode].Y = fontCharY;
        }

        public void MapConsecutiveAsciiCodes(char firstAsciiCode, int number, int startFontCharX, int startFontCharY, bool inColumn) {
            if (startFontCharX < 0 || startFontCharX >= ColumnCount)
                throw new ArgumentOutOfRangeException("startFontCharX");
            if (startFontCharY < 0 || startFontCharY >= RowCount)
                throw new ArgumentOutOfRangeException("startFontCharY");

            int x = startFontCharX;
            int y = startFontCharY;

            for (int i = 0; i < number; i++) {
                MapAsciiCode((char) (firstAsciiCode + i), x, y);

                if (inColumn) {
                    y++;
                    if (y >= RowCount) {
                        y = 0;
                        x++;
                        if (x >= ColumnCount)
                            return;
                    }
                }
                else {
                    x++;
                    if (x >= ColumnCount) {
                        x = 0;
                        y++;
                        if (y >= RowCount)
                            return;
                    }
                }
            }
        }

        public void MapString(string str, int startFontCharX, int startFontCharY, bool inColumn) {
            if (startFontCharX < 0 || startFontCharX >= ColumnCount)
                throw new ArgumentOutOfRangeException("startFontCharX");
            if (startFontCharY < 0 || startFontCharY >= RowCount)
                throw new ArgumentOutOfRangeException("startFontCharY");

            int x = startFontCharX;
            int y = startFontCharY;

            for (int i = 0; i < str.Length; i++) {
                MapAsciiCode(str[i], x, y);

                if (inColumn) {
                    y++;
                    if (y >= RowCount) {
                        y = 0;
                        x++;
                        if (x >= ColumnCount)
                            return;
                    }
                }
                else {
                    x++;
                    if (x >= ColumnCount) {
                        x = 0;
                        y++;
                        if (y >= RowCount)
                            return;
                    }
                }
            }
        }

        internal void SetFontSourceRect(ref Rectangle src, char c) {
            src.Width = CharacterWidth;
            src.Height = CharacterHeight;
            src.X = CharacterWidth*charMap[c].X;
            src.Y = CharacterHeight*charMap[c].Y;
        }

        private static void ProcessFont(Font font, FontLayout layout, FontType type) {
            SetMapping(font, layout);
        }

        private static void SetMapping(Font font, FontLayout layout) {
            switch (layout) {
                case FontLayout.InColumn:
                    font.MapConsecutiveAsciiCodes((Char) 0, 256, 0, 0, true);
                    break;
                case FontLayout.InRow:
                    font.MapConsecutiveAsciiCodes((char) 0, 256, 0, 0, false);
                    break;
                case FontLayout.TCOD:
                    MapTCOD(font);
                    break;
            }
        }

        private static void MapTCOD(Font font) {
            font.MapAsciiCode(' ', 0, 0);
            font.MapString("!\"#$%&'()*+,-./0123456789:;<=>?@[\\]^_`{|}~", 1, 0, false);
            font.MapAsciiCode((char)SpecialChar.Block1, 11, 1);
            font.MapAsciiCode((char)SpecialChar.Block2, 12, 1);
            font.MapAsciiCode((char)SpecialChar.Block3, 13, 1);
            font.MapAsciiCode((char)SpecialChar.VerticalLine, 14, 1);
            font.MapAsciiCode((char)SpecialChar.HorizontalLine, 15, 1);
            font.MapAsciiCode((char)SpecialChar.CrossLines, 16, 1);
            font.MapAsciiCode((char)SpecialChar.TeeWest, 17, 1);
            font.MapAsciiCode((char)SpecialChar.TeeNorth, 18, 1);
            font.MapAsciiCode((char)SpecialChar.TeeEast, 19, 1);
            font.MapAsciiCode((char)SpecialChar.TeeSouth, 20, 1);
            font.MapAsciiCode((char)SpecialChar.SouthWestLine, 21, 1);
            font.MapAsciiCode((char)SpecialChar.NorthWestLine, 22, 1);
            font.MapAsciiCode((char)SpecialChar.NorthEastLine, 23, 1);
            font.MapAsciiCode((char)SpecialChar.SouthEastLine, 24, 1);
            font.MapAsciiCode((char)SpecialChar.SubpixelNorthWest, 25, 1);
            font.MapAsciiCode((char)SpecialChar.SubpixelNorthEast, 26, 1);
            font.MapAsciiCode((char)SpecialChar.SubpixelNorth, 27, 1);
            font.MapAsciiCode((char)SpecialChar.SubpixelSouthEast, 28, 1);
            font.MapAsciiCode((char)SpecialChar.SubpixelDiagonal, 29, 1);
            font.MapAsciiCode((char)SpecialChar.SubpixelEast, 30, 1);
            font.MapAsciiCode((char)SpecialChar.SubpixelSouthWest, 31, 1);
            font.MapAsciiCode((char)SpecialChar.ArrowNorth, 0, 2);
            font.MapAsciiCode((char)SpecialChar.ArrowSouth, 1, 2);
            font.MapAsciiCode((char)SpecialChar.ArrowWest, 2, 2);
            font.MapAsciiCode((char)SpecialChar.ArrowEast, 3, 2);
            font.MapAsciiCode((char)SpecialChar.ArrowNorthNoTail, 4, 2);
            font.MapAsciiCode((char)SpecialChar.ArrowSouthNoTail, 5, 2);
            font.MapAsciiCode((char)SpecialChar.ArrowWestNoTail, 6, 2);
            font.MapAsciiCode((char)SpecialChar.ArrowEastNoTail, 7, 2);
            font.MapAsciiCode((char)SpecialChar.DoubleArrowVert, 8, 2);
            font.MapAsciiCode((char)SpecialChar.DoubleArrowHorz, 9, 2);
            font.MapAsciiCode((char)SpecialChar.CheckBoxUnset, 10, 2);
            font.MapAsciiCode((char)SpecialChar.CheckBoxSet, 11, 2);
            font.MapAsciiCode((char)SpecialChar.RadioUnset, 12, 2);
            font.MapAsciiCode((char)SpecialChar.RadioSet, 13, 2);
            font.MapAsciiCode((char)SpecialChar.DoubleVertLine, 14, 2);
            font.MapAsciiCode((char)SpecialChar.DoubleHorzLine, 15, 2);
            font.MapAsciiCode((char)SpecialChar.DoubleCrossLines, 16, 2);
            font.MapAsciiCode((char)SpecialChar.DoubleTeeWest, 17, 2);
            font.MapAsciiCode((char)SpecialChar.DoubleTeeNorth, 18, 2);
            font.MapAsciiCode((char)SpecialChar.DoubleTeeEast, 19, 2);
            font.MapAsciiCode((char)SpecialChar.DoubleTeeSouth, 20, 2);
            font.MapAsciiCode((char)SpecialChar.DoubleSouthWest, 21, 2);
            font.MapAsciiCode((char)SpecialChar.DoubleNorthWest, 22, 2);
            font.MapAsciiCode((char)SpecialChar.DoubleNorthEast, 23, 2);
            font.MapAsciiCode((char)SpecialChar.DoubleSouthEast, 24, 2);
            font.MapString("ABCDEFGHIJKLMNOPQRSTUVWXYZ", 0, 3, false);
            font.MapString("abcdefghijklmnopqrstuvwxyz", 0, 4, false);
            font.MapConsecutiveAsciiCodes((char)128, 32, 0, 5, false);
            font.MapAsciiCode((char) 255, font.ColumnCount - 1, font.RowCount - 1);
        }

        private static void ProcessStandard(Font font, FontLayout layout) {
            var fontData = new Color[font.Texture.Width*font.Texture.Height];
            font.Texture.GetData(fontData);

            // Use the space character to determine the transparency key mask.
            var c = font.charMap[' '];
            var key = fontData[c.X*font.CharacterWidth + (c.Y*font.CharacterHeight)*font.Texture.Width];

            // Set all pixels matching the keycolor to 0 alpha.
            for (int i = 0; i < fontData.Length; i++) {
                if (fontData[i].R == key.R && fontData[i].G == key.G && fontData[i].B == key.B) {
                    fontData[i].A = fontData[i].R = fontData[i].G = fontData[i].B = 0;
                }
                else {
                    fontData[i].A = fontData[i].R = fontData[i].G = fontData[i].B = 255;
                }
            }

            font.Texture.SetData(fontData);
        }

        private static void ProcessAlphaAA(Font font, FontLayout layout) {
            var fontData = new Color[font.Texture.Width*font.Texture.Height];

            font.Texture.GetData(fontData);

            // Set color according to alpha.
            for (int i = 0; i < fontData.Length; i++) {
                fontData[i].R = fontData[i].G = fontData[i].B = 255;
            }

            font.Texture.SetData(fontData);
        }

        private static void ProcessGreyscaleAA(Font font, FontLayout layout) {
            var fontData = new Color[font.Texture.Width*font.Texture.Height];
            font.Texture.GetData(fontData);

            // Set alpha according to greyscale value
            for (int i = 0; i < fontData.Length; i++) {
                int gs = (fontData[i].R + fontData[i].G + fontData[i].B)/3;
                fontData[i].A = (byte) gs;
            }

            font.Texture.SetData(fontData);
        }

        private static void MakeSolidBlock(Font font) {
            var fontData = new Color[font.Texture.Width*font.Texture.Height];
            font.Texture.GetData(fontData);

            // Create solid block at 0xFF
            int sx = font.Texture.Width - font.CharacterWidth;
            int sy = font.Texture.Height - font.CharacterHeight;

            for (int y = 0; y < font.CharacterHeight; y++) {
                for (int x = 0; x < font.CharacterWidth; x++) {
                    fontData[x + sx + (y + sy)*font.Texture.Width] = new Color(255, 255, 255, 255);
                }
            }

            font.Texture.SetData(fontData);
        }

        #region Static Create* Methods

        public static Font CreateDefaultFont(GraphicsDevice device) {
            if (device == null)
                throw new ArgumentNullException("device");

            return CreateFromFile(device, "terminal.png", FontLayout.InColumn, FontType.NoAA);
        }

        public static Font CreateFromTexture(Texture2D texture, FontLayout layout, FontType type) {
            int w, h;
            switch (layout) {
                case FontLayout.InColumn:
                case FontLayout.InRow:
                    w = h = 16;
                    break;
                default:
                case FontLayout.TCOD:
                    w = 32;
                    h = 8;
                    break;
            }

            return CreateFromTexture(texture, layout, type, w, h);
        }

        public static Font CreateFromTexture(Texture2D texture, FontLayout layout, FontType type, int cols, int rows) {
            if (texture == null)
                throw new ArgumentNullException("texture");
            if (cols < 1)
                throw new ArgumentOutOfRangeException("cols");
            if (rows < 1)
                throw new ArgumentOutOfRangeException("rows");

            var newFont = new Font()
            {
                ColumnCount = cols,
                RowCount = rows,
                CharacterWidth = texture.Width/cols,
                CharacterHeight = texture.Height/rows,
                Texture = texture
            };

            ProcessFont(newFont, layout, type);
            return newFont;
        }

        public static Font CreateFromFile(GraphicsDevice device, string filename, FontLayout layout, FontType type) {
            int w, h;
            switch (layout) {
                case FontLayout.InColumn:
                case FontLayout.InRow:
                    w = h = 16;
                    break;
                default:
                    w = 32;
                    h = 32;
                    break;
            }
            return CreateFromFile(device, filename, layout, type, w, h);
        }

        public static Font CreateFromFile(GraphicsDevice device, string filename, FontLayout layout, FontType type,
            int cols, int rows) {
            if (device == null)
                throw new ArgumentNullException("device");
            if (String.IsNullOrEmpty(filename))
                throw new ArgumentNullException("filename");
            if (cols < 1)
                throw new ArgumentOutOfRangeException("cols");
            if (rows < 1)
                throw new ArgumentOutOfRangeException("rows");
            if (!File.Exists(filename))
                throw new ArgumentException("Font file not found", "filename");

            Texture2D texture;
            using (var file = new FileStream(filename, FileMode.Open, FileAccess.Read)) {
                texture = Texture2D.FromStream(device, file);
            }

            return CreateFromTexture(texture, layout, type, cols, rows);
        }

        #endregion
    }
}