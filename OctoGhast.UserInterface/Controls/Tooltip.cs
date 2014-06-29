using System;
using OctoGhast.Configuration;
using OctoGhast.Spatial;
using OctoGhast.UserInterface.Core;
using OctoGhast.UserInterface.Core.Theme;

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
            Size = new Size(Canvas.MeasureString(text) + 2, 3);
            this.ParentWindow = parentWindow;

            Position = AutoPosition(screenPosition);
            Canvas = new Canvas(new Config(), Size);
            Canvas.SetDefaultPigment(parentWindow.Pigments[PigmentType.Tooltip]);
            Canvas.PrintFrame("");
            Canvas.PrintString(1, 1, text);
        }

        public void DrawToScreen() {
            Canvas.Blit(Position, ParentWindow.TooltipFGAlpha, ParentWindow.TooltipBGAlpha);
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