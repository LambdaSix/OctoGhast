using System;
using OctoGhast.Spatial;
using OctoGhast.UserInterface.Core.Interface;
using OctoGhast.UserInterface.Templates;
using OctoGhast.UserInterface.Theme;
using RenderLike;

namespace OctoGhast.UserInterface.Core
{
    public abstract class Widget : Component, IDisposable
    {
        public Vec Position { get; internal set; }
        protected ICanvas Canvas { get; set; }

        public Size Size { get; set; }
        public event EventHandler Draw;

        /// <summary>
        /// If true, then base classes do not do any drawing to the <seealso cref="Canvas"/>, including clears
        /// or blits to the screen.
        /// This is present for subclasses wishing to implement specialised drawing code.
        /// </summary>
        public bool OwnerDraw { get; set; }

        /// <summary>
        /// Get the pigment map for this widget.  Alternatives can be set or removed
        /// to change the pigments for this widget and its children during runtime.
        /// </summary>
        public PigmentMapping Pigments { get; set; }

        public PigmentMapping PigmentOverrides { get; set; }

        /// <summary>
        /// Get this Widget's bounding rectangle in screen space co-ordinates.
        /// </summary>
        public Rect ScreenRectangle {
            get { return new Rect(Position, Size); }
        }

        /// <summary>
        /// Get this Widget's bounding rectangle in local space co-ordinates.
        /// The upper-left will always be Vec.Zero (0,0)
        /// </summary>
        public Rect LocalRectangle {
            get { return new Rect(Vec.Zero, Size); }
        }

        public Widget(WidgetTemplate template) {
            Position = Vec.Zero;
            Size = template.CalculateSize();
            Canvas = new Canvas(Config.RootConsoleFunc(), Size);

            OwnerDraw = template.OwnerDraw;
            PigmentOverrides = template.Pigments;
        }

        /// <summary>
        /// Clear the canvas surface for redrawing.
        /// </summary>
        protected virtual void Redraw() {
            if (!OwnerDraw) {
                Canvas.SetDefaultPigment(DetermineMainPigment());
                Canvas.Clear();
            }
        }

        /// <summary>
        /// Calculates the current <seealso cref="Pigment"/> of the main drawing area for this <seealso cref="Widget"/>.
        /// Override to change which pigment is used.
        /// </summary>
        /// <returns></returns>
        protected abstract Pigment DetermineMainPigment();

        /// <summary>
        /// Calculates the current <seealso cref="Pigment"/> of the frame area for this <seealso cref="Widget"/>.
        /// </summary>
        /// <returns></returns>
        protected abstract Pigment DetermineFramePigment();

        
        public virtual void OnDraw() {
            Redraw();

            if (Draw != null)
                Draw(this, EventArgs.Empty);

            if (!OwnerDraw)
                Canvas.Blit(Position);
        }

        private bool _alreadyDisposed;

        ~Widget() {
            Dispose(false);
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing) {
            if (_alreadyDisposed) {
                return;
            }
            if (isDisposing) {
                if (Canvas != null)
                    Canvas.Dispose();
            }
            _alreadyDisposed = true;
        }
    }
}