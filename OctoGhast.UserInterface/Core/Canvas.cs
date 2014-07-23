using System;
using System.Text;
using System.Xml.Schema;
using libtcod;
using OctoGhast.Spatial;
using OctoGhast.UserInterface.Core.Interface;
using OctoGhast.UserInterface.Theme;

namespace OctoGhast.UserInterface.Core
{
    public class Canvas : ICanvas
    {
        public Pigment DefaultPigment { get; set; }
        private Config _config;

        public TCODConsole Buffer { get; private set; }
        public Size Size { get; private set; }

        public Canvas(Size size) {
            _config = new Config();
            DefaultPigment = new Pigment(0xFFFFFF, 0x000000);
            Buffer = new TCODConsole(size.Width, size.Height);
            Size = size;
        }

        public void SetDefaultPigment(Pigment pigment) {
            if (pigment == null)
                throw new ArgumentNullException("pigment");
            DefaultPigment = pigment;
            setPigment(pigment);
        }

        public void SetPigmentAt(int x, int y, Pigment pigment) {
            if (pigment == null)
                throw new ArgumentNullException("pigment");

            Buffer.setCharBackground(x, y, pigment.Background.ToTcodColor());
            Buffer.setCharForeground(x, y, pigment.Foreground.ToTcodColor());
        }

        public void SetPigmentAt(Vec position, Pigment pigment) {
            SetPigmentAt(position.X, position.Y, pigment);
        }

        public void Blit(TCODConsole console, int x, int y) {
            int maxWidth = _config.Width - x;
            int maxHeight = _config.Height - y;

            if (maxWidth < 1 || maxHeight < 1)
                return;

            int finalWidth = Math.Min(Size.Width, maxWidth);
            int finalHeight = Math.Min(Size.Height, maxHeight);

            var finalSize = new Size(finalWidth, finalHeight);
            TCODConsole.blit(Buffer,
                0, 0,
                finalSize.Width, finalSize.Height,
                console, x, y);
        }

        public void Blit(TCODConsole console, int x, int y, float fgAlpha, float bgAlpha) {
            int maxWidth = _config.Width - x;
            int maxHeight = _config.Height - y;

            if (maxWidth < 1 || maxHeight < 1)
                return;

            int finalWidth = Math.Min(Size.Width, maxWidth);
            int finalHeight = Math.Min(Size.Height, maxHeight);

            var finalSize = new Size(finalWidth, finalHeight);
            TCODConsole.blit(Buffer,
                0, 0,
                finalSize.Width, finalSize.Height,
                TCODConsole.root, x, y, fgAlpha, bgAlpha);
        }

        public void Blit(TCODConsole console, Vec position) {
            Blit(console, position.X, position.Y);
        }

        public void Blit(TCODConsole console, Vec position, float fgAlpha, float bgAlpha) {
            Blit(console, position.X, position.Y, fgAlpha, bgAlpha);
        }

        public void Blit(int x, int y) {
            Blit(TCODConsole.root, x, y);
        }

        public void Blit(int x, int y, float fgAlpha, float bgAlpha) {
            Blit(TCODConsole.root, x, y, fgAlpha, bgAlpha);
        }

        public void Blit(Vec position) {
            Blit(TCODConsole.root, position.X, position.Y);
        }

        public void Blit(Vec position, float fgAlpha, float bgAlpha) {
            Blit(TCODConsole.root, position.X, position.Y, fgAlpha, bgAlpha);
        }

        public void Blit(ICanvas dest, int x, int y) {
            Blit(dest.Buffer, x, y);
        }

        public void Blit(ICanvas dest, Vec destVec) {
            Blit(dest.Buffer, destVec.X, destVec.Y);
        }

        public void PrintFrame(string title, Pigment pigment = null) {
            if (pigment != null)
                setPigment(pigment);

            if (string.IsNullOrWhiteSpace(title)) {
                Buffer.printFrame(0, 0, Size.Width, Size.Height, false);
            }
            else {
                Buffer.printFrame(0, 0, Size.Width, Size.Height, false,
                    TCODBackgroundFlag.Set, title);
            }

            if (pigment != null)
                setPigment(DefaultPigment);
        }

        public void Clear() {
            Buffer.clear();
        }

        public void Scroll(int deltaX, int deltaY) {
            var srcSize = new Size(width: Size.Width - Math.Abs(deltaX),
                height: Size.Height - Math.Abs(deltaY));

            using (ICanvas canvas = new Canvas(srcSize)) {
                int srcY, destX, destY;
                int srcX = srcY = destX = destY = 0;

                if (deltaX < 0)
                    srcX = -deltaX;

                if (deltaX > 0)
                    destX = deltaX;

                if (deltaY < 0)
                    srcY = -deltaY;

                if (deltaY > 0)
                    destY = deltaY;

                TCODConsole.blit(Buffer,
                    srcX, srcY, srcSize.Width, srcSize.Height,
                    canvas.Buffer,
                    destX, destY);
                Clear();
                Blit(canvas, 0, 0);
            }
        }

        public void Scroll(Vec delta) {
            Scroll(delta.X, delta.Y);
        }

        public Size MeasureChar() {
            int w, h;
            TCODSystem.getCharSize(out w, out h);

            return new Size(w, h);
        }

        public int MeasureString(string str) {
            int length = str.Length;

            foreach (var c in str) {
                switch (c) {
                    case Color.CodeForeground:
                    case Color.CodeBackground:
                        length = length - 4;
                        break;
                    case Color.CodeStop:
                        length = length - 1;
                        break;
                }
            }

            return length;
        }

        public string TrimText(string text, int length) {
            var sb = new StringBuilder();

            int i, w;
            i = w = 0;

            while (w < length) {
                char c = text[i];

                switch (c) {
                    case Color.CodeForeground:
                    case Color.CodeBackground:
                        sb.Append(c);
                        sb.AppendFormat("{0}{1}{2}", text[i + 1], text[i + 2], text[i + 3]);
                        i += 4;
                        break;
                    case Color.CodeStop:
                        sb.Append(c);
                        i++;
                        break;
                    default:
                        sb.Append(c);
                        i++;
                        w++;
                        break;
                }
            }

            return sb.ToString();
        }

        private int HOffset(string str, HAlign alignment, int fieldLength) {
            int startX = 0;
            switch (alignment) {
                case HAlign.Center:
                    startX = (fieldLength - MeasureString(str))/2;
                    break;
                case HAlign.Right:
                    startX = (fieldLength - MeasureString(str));
                    break;
            }
            return startX;
        }

        private int VOffset(string str, VAlign alignment, int fieldHeight) {
            int startY = 0;
            switch (alignment) {
                case VAlign.Center:
                    startY = (fieldHeight - 1)/2;
                    break;
                case VAlign.Bottom:
                    startY = (fieldHeight - 1);
                    break;
            }

            return startY;
        }


        public Vec MeasureAlignOffset(Vec pos, string str, HAlign alignment, int fieldLength) {
            if (String.IsNullOrWhiteSpace(str))
                return Vec.Zero;

            int xOffset = HOffset(str, alignment, fieldLength);

            return pos.OffsetX(xOffset);
        }

        public Vec MeasureAlignOffset(Vec pos, string str, HAlign hAlign, VAlign vAlign, Size fieldSize) {
            if (String.IsNullOrWhiteSpace(str))
                return Vec.Zero;

            int xOffset = HOffset(str, hAlign, fieldSize.Width);
            int yOffset = VOffset(str, vAlign, fieldSize.Height);

            return pos.Offset(xOffset, yOffset);
        }

        public void PrintChar(int x, int y, char character, Pigment pigment = null) {
            checkInBounds(x, y);

            var color = pigment ?? DefaultPigment ?? new Pigment(0xFFFFFF, 0x000000);

            Buffer.putCharEx(x, y, character, color.Foreground.ToTcodColor(), color.Background.ToTcodColor());
        }

        public void PrintChar(Vec pos, char character, Pigment pigment = null) {
            PrintChar(pos.X, pos.Y, character, pigment);
        }

        private void setPigment(Pigment pigment) {
            Buffer.setBackgroundColor(pigment.Background.ToTcodColor());
            Buffer.setBackgroundFlag(pigment.BackgroundFlag);
            Buffer.setForegroundColor(pigment.Foreground.ToTcodColor());
        }

        private void print(int x, int y, string str) {
            int cX = x;
            var bg = Buffer.getBackgroundColor();
            var fg = Buffer.getForegroundColor();
            int i = 0;

            while (i < str.Length) {
                char c = str[i];

                if (c == Color.CodeForeground) {
                    int r = str[i + 1];
                    int g = str[i + 2];
                    int b = str[i + 3];

                    Buffer.setForegroundColor(new TCODColor(r, g, b));
                    i += 4; // Skip over the codes
                }
                else if (c == Color.CodeBackground) {
                    int r = str[i + 1];
                    int g = str[i + 2];
                    int b = str[i + 3];
                    Buffer.setBackgroundColor(new TCODColor(r, g, b));
                    i += 4;
                }
                else if (c == Color.CodeStop) {
                    Buffer.setForegroundColor(fg);
                    Buffer.setBackgroundColor(bg);
                    i++;
                }
                else {
                    Buffer.putChar(cX, y, c);
                    i++;
                    cX++;

                    if (cX >= Size.Width)
                        return;
                }
            }
        }

        private bool tryCheckInBounds(int x, int y) {
            try {
                checkInBounds(x, y);
            }
            catch (ArgumentOutOfRangeException) {
                return false;
            }
            return true;
        }

        private void checkInBounds(int x, int y) {
            if (x < 0 || x > Size.Width)
                throw new ArgumentOutOfRangeException("x", "The specified X co-ordinte is out of range");

            if (y < 0 || y > Size.Height)
                throw new ArgumentOutOfRangeException("y", "The specified Y co-ordinate is out of range");
        }

        private void setColors(Pigment pigment) {
            Buffer.setBackgroundColor(pigment.Background.ToTcodColor());
            Buffer.setForegroundColor(pigment.Foreground.ToTcodColor());
        }

        public void PrintString(int x, int y, string str, Pigment pigment = null) {
            if (str == null)
                throw new ArgumentNullException("str");

            checkInBounds(x, y);

            using (var session = CreatePigmentSession(pigment, DefaultPigment)) {
                print(x, y, str);
            }
        }

        public void PrintString(Vec pos, string str, Pigment pigment = null) {
            PrintString(pos.X, pos.Y, str, pigment);
        }

        public void PrintStringAligned(int x, int y, string str, HAlign alignment, int fieldLength,
            Pigment pigment = null) {
            if (str == null)
                throw new ArgumentNullException("str");

            if (fieldLength < 1)
                throw new ArgumentOutOfRangeException("fieldLength", "Field length should be at least 1");

            checkInBounds(x, y);

            var position = MeasureAlignOffset(new Vec(x, y), str, alignment, fieldLength);

            using (var session = CreatePigmentSession(pigment, DefaultPigment)) {
                print(position.X, position.Y, str);
            }
        }

        public void PrintStringAligned(int x, int y, string str, HAlign hAlign, VAlign vAlign, Size fieldSize,
            Pigment pigment = null) {
            if (str == null)
                throw new ArgumentNullException("str");

            if (fieldSize.Width < 1)
                throw new ArgumentOutOfRangeException("fieldSize", "The specified width of fieldSize is less than 1");
            if (fieldSize.Height < 1)
                throw new ArgumentOutOfRangeException("fieldSize", "The specified width of fieldSize is less than 1");

            checkInBounds(x, y);

            if (fieldSize.Width < MeasureString(str))
                str = TrimText(str, fieldSize.Width);

            var pos = MeasureAlignOffset(new Vec(x, y), str, hAlign, vAlign, fieldSize);

            using (var session = CreatePigmentSession(pigment, DefaultPigment)) {
                print(pos.X, pos.Y, str);
            }
        }

        public void PrintStringAligned(Vec pos, string str, HAlign alignment, int fieldLength, Pigment pigment = null) {
            PrintStringAligned(pos.X, pos.Y, str, alignment, fieldLength, pigment);
        }

        public void PrintStringAligned(Vec pos, string str, HAlign hAlign, VAlign vAlign, Size fieldSize,
            Pigment pigment = null) {
            PrintStringAligned(pos.X, pos.Y, str, hAlign, vAlign, fieldSize, pigment);
        }

        public void DrawHLine(int startX, int startY, int length, Pigment pigment = null) {
            checkInBounds(startX, startY);

            using (var session = CreatePigmentSession(pigment, DefaultPigment)) {
                Buffer.hline(startX, startY, length);
            }
        }

        public void DrawHLine(Vec start, int length, Pigment pigment = null) {
            DrawHLine(start.X, start.Y, length, pigment);
        }

        public void DrawVLine(int startX, int startY, int length, Pigment pigment = null) {
            checkInBounds(startX, startY);

            using (CreatePigmentSession(pigment, DefaultPigment)) {
                Buffer.vline(startX, startX, length);
            }
        }

        public void DrawVLine(Vec start, int length, Pigment pigment = null) {
            DrawVLine(start.X, start.Y, length, pigment);
        }

        private bool _alreadyDisposed;

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (_alreadyDisposed)
                return;

            if (disposing) {
                Buffer.Dispose();
            }

            _alreadyDisposed = true;
        }

        private PigmentSession CreatePigmentSession(Pigment pigment, Pigment defaultPigment) {
            return new PigmentSession(pigment, defaultPigment, setPigment);
        }

        private class PigmentSession : IDisposable
        {
            public Pigment Pigment { get; private set; }
            public Pigment DefaultPigment { get; private set; }
            public Action<Pigment> PigmentSetter { get; private set; }

            public PigmentSession(Pigment pigment, Pigment defaultPigment, Action<Pigment> pigmentSetter) {
                Pigment = pigment;
                DefaultPigment = defaultPigment;
                PigmentSetter = pigmentSetter;

                if (Pigment != null)
                    PigmentSetter(Pigment);
            }

            public void Dispose() {
                if (Pigment != null)
                    PigmentSetter(DefaultPigment);
            }
        }
    }

    public static class CanvasUtil
    {
        public static int MeasureStr(string text) {
            int length = text.Length;

            foreach (var c in text) {
                switch (c) {
                    case Color.CodeForeground:
                    case Color.CodeBackground:
                        length = length - 4;
                        break;
                    case Color.CodeStop:
                        length = length - 1;
                        break;
                }
            }

            return length;
        }
    }
}