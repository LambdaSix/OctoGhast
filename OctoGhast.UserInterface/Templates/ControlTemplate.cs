using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OctoGhast.Spatial;
using OctoGhast.UserInterface.Core;

namespace OctoGhast.UserInterface.Templates
{
    /// <summary>
    /// Specifies a cardinal direction (assuming Up is North) for use in the layout helper methods.
    /// </summary>
    public enum LayoutDirection
    {
        North,
        NorthEast,
        East,
        SouthEast,
        South,
        SouthWest,
        West,
        NorthWest
    };

    /// <summary>
    /// This class builds on the Widget Template, and offers some layout helper methods for
    /// positioning controls relative to each other.
    /// </summary>
    public abstract class ControlTemplate : WidgetTemplate
    {
        public BindingTarget Binding { get; set; }

        /// <summary>
        /// The upper left position of this control.  Defaults to the origin (0,0)
        /// </summary>
        public Vec UpperLeftPos { get; set; }

        /// <summary>
        /// If not null (the default), the text that is displayed as a tooltip.
        /// </summary>
        public string Tooltip { get; set; }

        /// <summary>
        /// If true (the default), this control will be Active when created.
        /// </summary>
        public bool IsActiveInitially { get; set; }

        /// <summary>
        /// True if this control should react when the mouse is over it.
        /// </summary>
        public bool MouseOverHighlight { get; set; }

        /// <summary>
        /// True if this control can take keyboard focus
        /// </summary>
        public bool CanHaveKeyboardFocus { get; set; }

        /// <summary>
        /// Use this to manually size the control. If this is empty (default) 
        /// then the control will attempt to autosize
        /// </summary>
        public Size AutoSizeOverride { get; set; }

        /// <summary>
        /// Draw a frame around this control
        /// </summary>
        public bool HasFrameBorder { get; set; }

        /// <summary>
        /// If set and <seealso cref="HasFrameBorder"/> is true, draw into the controls frame title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Default constructor initializes properties to their defaults.
        /// </summary>
        protected ControlTemplate()
        {
            Tooltip = null;
            IsActiveInitially = true;
        }

        /// <summary>
        /// Calculates the Rect (in screen coordinates) of this control.
        /// </summary>
        /// <returns></returns>
        public Rect CalculateRect()
        {
            return new Rect(UpperLeftPos, CalculateSize());
        }

        /// <summary>
        /// Layout helper - positions the control by setting the upper right coordinates.
        /// </summary>
        /// <param name="upperRight"></param>
        public void SetUpperRight(Vec upperRight) {
            UpperLeftPos = upperRight.Offset(1 - CalculateSize().Width, 0);
        }

        /// <summary>
        /// Layout helper - positions the control by setting the lower right coordinates.
        /// </summary>
        /// <param name="lowerRight"></param>
        public void SetLowerRight(Vec lowerRight) {
            UpperLeftPos = lowerRight.Offset(1 - CalculateSize().Width, 1 - CalculateSize().Height);
        }

        /// <summary>
        /// Layout helper - positions the control by setting the lower left coordinates.
        /// </summary>
        /// <param name="lowerLeft"></param>
        public void SetLowerLeft(Vec lowerLeft)
        {
            UpperLeftPos = lowerLeft.Offset(0, 1 - CalculateSize().Height);
        }

        /// <summary>
        /// Layout helper - positions the control by setting the top center coordinates.
        /// </summary>
        /// <param name="topCenter"></param>
        public void SetTopCenter(Vec topCenter)
        {
            Vec ctr = CalculateRect().Center;

            UpperLeftPos = new Vec(topCenter.X - ctr.X, topCenter.Y);
        }

        /// <summary>
        /// Layout helper - positions the control by setting the center right coordinates.
        /// </summary>
        /// <param name="rightCenter"></param>
        public void SetRightCenter(Vec rightCenter)
        {
            Vec ctr = CalculateRect().Center;

            SetUpperRight(new Vec(rightCenter.X, rightCenter.Y - ctr.Y));
        }

        /// <summary>
        /// Layout helper - positions the control by setting the bottom center coordinates.
        /// </summary>
        /// <param name="bottomCenter"></param>
        public void SetBottomCenter(Vec bottomCenter)
        {
            Vec ctr = CalculateRect().Center;

            SetLowerLeft(new Vec(bottomCenter.X - ctr.X, bottomCenter.Y));
        }

        /// <summary>
        /// Layout helper - positions the control by setting the center left coordinates.
        /// </summary>
        /// <param name="leftCenter"></param>
        public void SetLeftCenter(Vec leftCenter)
        {
            Vec ctr = CalculateRect().Center;

            UpperLeftPos = new Vec(leftCenter.X, leftCenter.Y - ctr.Y);
        }

        /// <summary>
        /// Layout helper - Aligns this control to the specified direction of the spcecified
        /// control template.  This provides a means to specify control positions relative to
        /// previously created templates.
        /// </summary>
        /// <param name="toDirection"></param>
        /// <param name="ofControl"></param>
        /// <param name="padding"></param>
        public void AlignTo(LayoutDirection toDirection, ControlTemplate ofControl, int padding = 0)
        {

            switch (toDirection)
            {
                case LayoutDirection.North:
                    AlignNorth(ofControl.CalculateRect(), padding);
                    break;

                case LayoutDirection.NorthEast:
                    AlignNorthEast(ofControl.CalculateRect(), padding);
                    break;

                case LayoutDirection.East:
                    AlignEast(ofControl.CalculateRect(), padding);
                    break;

                case LayoutDirection.SouthEast:
                    AlignSouthEast(ofControl.CalculateRect(), padding);
                    break;

                case LayoutDirection.South:
                    AlignSouth(ofControl.CalculateRect(), padding);
                    break;

                case LayoutDirection.SouthWest:
                    AlignSouthWest(ofControl.CalculateRect(), padding);
                    break;

                case LayoutDirection.West:
                    AlignWest(ofControl.CalculateRect(), padding);
                    break;

                case LayoutDirection.NorthWest:
                    AlignNorthWest(ofControl.CalculateRect(), padding);
                    break;
            }
        }
        
        private void AlignSouth(Rect ofRect, int padding)
        {
            Vec ourCtr = CalculateRect().Center;
            Vec ofCtr = ofRect.Center;

            UpperLeftPos = new Vec(ofCtr.X - ourCtr.X, ofRect.Bottom + 1 + padding);
        }
        
        private void AlignEast(Rect ofRect, int padding)
        {
            Vec ourCtr = CalculateRect().Center;
            Vec ofCtr = ofRect.Center;

            UpperLeftPos = new Vec(ofRect.Right + 1 + padding, ofCtr.Y - ourCtr.Y);
        }
        
        private void AlignNorth(Rect ofRect, int padding)
        {
            Vec ourCtr = CalculateRect().Center;
            Vec ofCtr = ofRect.Center;

            SetLowerLeft(new Vec(ofCtr.X - ourCtr.X, ofRect.Top - (1 + padding)));
        }
        
        private void AlignWest(Rect ofRect, int padding)
        {
            Vec ourCtr = CalculateRect().Center;
            Vec ofCtr = ofRect.Center;

            SetUpperRight(new Vec(ofRect.Left - (1 + padding), ofCtr.Y - ourCtr.Y));
        }
        
        private void AlignNorthEast(Rect ofRect, int padding)
        {
            SetLowerLeft(ofRect.TopRight.Offset(1 + padding, -(1 + padding)));
        }
        
        private void AlignSouthEast(Rect ofRect, int padding)
        {
            UpperLeftPos = ofRect.BottomLeft.Offset(1 + padding, 1 + padding);
        }
        
        private void AlignSouthWest(Rect ofRect, int padding)
        {
            SetUpperRight(ofRect.BottomLeft.Offset(-(1 + padding), 1 + padding));
        }
        
        private void AlignNorthWest(Rect ofRect, int padding)
        {
            SetLowerRight(ofRect.BottomLeft.Offset(-(1 + padding), -(1 + padding)));
        }
    }
}
