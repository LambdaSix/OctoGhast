using System;
using System.Linq;
using OctoGhast.Spatial;
using OctoGhast.UserInterface.Core;
using OctoGhast.UserInterface.Core.Interface;
using OctoGhast.UserInterface.Theme;

namespace OctoGhast.UserInterface.Controls
{
    public class TooltipEventArgs : EventArgs
    {
        public string Text { get; set; }
        public Vec Position { get; private set; }

        public TooltipEventArgs(string text, Vec position) {
            Text = text;
            Position = position;
        }
    }

    public class Tooltip : IDisposable
    {
        private ICanvas Canvas { get; set; }
        private Size Size { get; set; }
        private Vec Position { get; set; }
        private Window ParentWindow { get; set; }

        public Tooltip(string text, Vec screenPosition, Window parentWindow) {
            Size = new Size(CanvasUtil.MeasureLongestLine(text) + 2, 3 + text.Count(s => s == '\n'));
            ParentWindow = parentWindow;

            Position = AutoPosition(screenPosition);
            Canvas = new Canvas(Config.RootConsoleFunc(), Size);
            Canvas.SetDefaultPigment(parentWindow.Pigments[PigmentType.Tooltip]);
            Canvas.PrintFrame("");

            foreach (var line in text.Split('\r', '\n').Where(s => s != "").Select((s, i) => new {Line = s, i})) {
                var printLine = line.Line.PadRight(line.Line.Length + (Size.Width - line.Line.Length) - 2);
                Canvas.PrintString(1, 1 + line.i, printLine);
            }
        }

        public void DrawToScreen() {
            Canvas.BlitToConsole(Position, ParentWindow.TooltipFGAlpha, ParentWindow.TooltipBGAlpha);
        }

        private Vec AutoPosition(Vec nearPos) {
            return ParentWindow.AutoPosition(nearPos.Offset(2, 2), Size);
        }

        private bool _alreadyDisposed;

        ~Tooltip() {
            Dispose(false);
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual void Dispose(bool isDisposing) {
            if (_alreadyDisposed)
                return;

            if (isDisposing) {
                if (Canvas != null) {
                    Canvas.Dispose();
                }
            }
            _alreadyDisposed = true;
        }
    }
}