using System;
using System.Reflection;
using OctoGhast.Configuration;
using OctoGhast.Spatial;
using OctoGhast.UserInterface.Core;
using OctoGhast.UserInterface.Core.Theme;

namespace OctoGhast.UserInterface
{
    public abstract class WidgetTemplate
    {
        public bool OwnerDraw { get; set; }

        public PigmentMapping Pigments { get; set; }

        public abstract Size CalculateSize();

        // TODO: PigmentMapping should be injected in. Revisit when object graph is known.

        public WidgetTemplate() {
            OwnerDraw = false;
            Pigments = new PigmentMapping();
        }
    }

    public abstract class Widget
    {
        protected Vec Position { get; set; } 
        protected ICanvas Canvas { get; set; }

        public Size Size { get; set; }
        public event EventHandler Draw;

        /// <summary>
        /// If true, then base classes do not do any drawing to the <seealso cref="Canvas"/>, including clears
        /// or blits to the screen.
        /// This is present for subclasses wishing to implement specialised drawing code.
        /// </summary>
        public bool OwnerDraw { get; set; }
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
            Canvas = new Canvas(new Config(), Size);

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

        internal protected virtual void OnDraw() {
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