using System;
using OctoGhast.Spatial;
using OctoGhast.UserInterface.Core;
using OctoGhast.UserInterface.Core.Messages;
using OctoGhast.UserInterface.Templates;
using OctoGhast.UserInterface.Theme;
using MouseButton = OctoGhast.UserInterface.Core.Messages.MouseButton;

namespace OctoGhast.UserInterface.Controls
{
    /// <summary>
    ///     Controls are added to a window, through which they receive action and system
    ///     messages.
    /// </summary>
    public abstract class ControlBase : Widget
    {
        /// <summary>
        ///     Construct a ControlBase instance from the given template.
        /// </summary>
        /// <param name="template"></param>
        protected ControlBase(ControlTemplate template) : base(template) {
            Position = template.UpperLeftPos;

            HasKeyboardFocus = false;
            CanHaveKeyboardFocus = true;
            IsActive = true;
            HasFrame = true;
            TooltipText = template.Tooltip;
            MouseOverHighlight = false;
            IsActive = template.IsActiveInitially;
        }

        /// <summary>
        ///     True if currently has keyboard focus.  This is set automatically by
        ///     the framework in response to user input, or by calling Window.TakeKeyboard.
        /// </summary>
        public bool HasKeyboardFocus { get; private set; }

        /// <summary>
        ///     True tells parent window that this control is able to
        ///     capture keyboard focus.
        /// </summary>
        public bool CanHaveKeyboardFocus { get; set; }

        /// <summary>
        ///     If false, notifies framework that it does not want to receive user input messages.  This
        ///     control will stil receive system messages.  Input messages will propagate under
        ///     inactive controls - this allows inactive controls to be placed over other controls
        ///     without blocking messages.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        ///     True if this control will draw itself hilighted when the mouse is over it.
        /// </summary>
        public bool MouseOverHighlight { get; set; }

        /// <summary>
        ///     True if the mouse pointer is currently over this control, and the control
        ///     is topmost at that position.
        /// </summary>
        public bool IsMouseOver { get; private set; }

        /// <summary>
        ///     True if this control is currently being pushed (left mouse button down while over)
        /// </summary>
        public bool IsBeingPushed { get; private set; }

        /// <summary>
        ///     Set to true if a frame should be drawn around the boder.
        /// </summary>
        public bool HasFrame { get; protected set; }

        /// <summary>
        ///     Set to a non-empty string to display a tooltip over this control on a hover action.
        /// </summary>
        public string TooltipText { get; set; }

        /// <summary>
        ///     Get the current parent window of control
        /// </summary>
        protected internal Window ParentWindow { get; internal set; }

        /// <summary>
        ///     Raised when control has taken the keyboard focus.  This typically happens after
        ///     the control recieves a left mouse button down message.
        /// </summary>
        public event EventHandler TakeKeyboardFocus;

        /// <summary>
        ///     Raised when the control has released the keyboard focus.  This typically
        ///     happens when a left mouse button down action happens away from this control.
        /// </summary>
        public event EventHandler ReleaseKeyboardFocus;

        /// <summary>
        ///     Raised when the mouse cursor has entered the control region and the control
        ///     is topmost at that position.
        /// </summary>
        public event EventHandler MouseEnter;

        /// <summary>
        ///     Raised when the mouse cursor has left the control region and the control
        ///     is topmost at that position.
        /// </summary>
        public event EventHandler MouseLeave;

        /// <summary>
        ///     Translates the given screen space position to local space.  This is often necessary
        ///     when handling mouse messages, since the position contained in MouseData is in screen
        ///     space.
        /// </summary>
        /// <param name="screenPos"></param>
        /// <returns></returns>
        public Vec ScreenToLocal(Vec screenPos) {
            return new Vec(screenPos.X - ScreenRectangle.TopLeft.X, screenPos.Y - ScreenRectangle.TopLeft.Y);
        }

        /// <summary>
        ///     Translates the given local space position to screen space position.
        /// </summary>
        /// <param name="localPos"></param>
        /// <returns></returns>
        public Vec LocalToScreen(Vec localPos) {
            return new Vec(localPos.X + ScreenRectangle.TopLeft.X, localPos.Y + ScreenRectangle.TopLeft.Y);
        }

        /// <summary>
        ///     Draw a frame around the control border.  If the <paramref name="pigment" /> is null,
        ///     the frame will drawn with the Canvas' current default pigment.
        /// </summary>
        protected void DrawFrame(Pigment pigment = null) {
            if (Size.Width > 2 && Size.Height > 2) {
                Canvas.PrintFrame(null, pigment);
            }
        }

        /// <summary>
        ///     Base class clears the Canvas and draws the frame if HasFrame is true.  If OwnerDraw
        ///     is true, this method does nothing. Override to add custom drawing
        ///     code after calling base class.
        /// </summary>
        protected override void Redraw() {
            base.Redraw();

            if (HasFrame && OwnerDraw == false) {
                DrawFrame(DetermineFramePigment());
            }
        }

        /// <summary>
        ///     Returns the pigment for the control according to its state.
        ///     Override to return a custom color for the main drawing area of the control, or to add
        ///     additional colors for the control based on custom states.
        /// </summary>
        protected override Pigment DetermineMainPigment() {
            if (HasKeyboardFocus) {
                return Pigments[PigmentType.ViewFocus];
            }

            if (IsActive) {
                if (IsMouseOver && MouseOverHighlight) {
                    return Pigments[PigmentType.ViewHighlight];
                }
                return Pigments[PigmentType.ViewNormal];
            }
            return Pigments[PigmentType.ViewInactive];
        }


        /// <summary>
        ///     Returns the pigment for the frame according to its state.
        ///     Override to return a custom color for the frame area of the control, or to add
        ///     additional colors for the control based on custom states.
        /// </summary>
        protected override Pigment DetermineFramePigment() {
            if (HasKeyboardFocus) {
                return Pigments[PigmentType.FrameFocus];
            }

            if (IsActive) {
                if (IsMouseOver && MouseOverHighlight) {
                    return Pigments[PigmentType.FrameHighlight];
                }
                return Pigments[PigmentType.FrameNormal];
            }
            return Pigments[PigmentType.FrameInactive];
        }

        /// <summary>
        ///     Returns a string representing the displayed tooltip, or null if none.  Base method
        ///     simply returns the TooltipText property.  Override to add custom tooltip code, e.g.
        ///     when the tooltip depends on where the mouse is positioned.
        /// </summary>
        /// <returns></returns>
        protected virtual string DetermineTooltipText() {
            return TooltipText;
        }

        /// <summary>
        ///     This method sets HasKeyboardFocus to true, and raises the TakeKBFocus event.  Override
        ///     to add custom handling code after calling this base method.
        /// </summary>
        protected internal virtual void OnTakeKeyboardFocus() {
            HasKeyboardFocus = true;

            if (TakeKeyboardFocus != null) {
                TakeKeyboardFocus(this, EventArgs.Empty);
            }
        }


        /// <summary>
        ///     This method sets HasKBFocus to false, and raises the ReleaseKeyboardFocus event.
        ///     Override to add custom handling code after calling this base method.
        /// </summary>
        protected internal virtual void OnReleaseKeyboardFocus() {
            HasKeyboardFocus = false;

            if (ReleaseKeyboardFocus != null) {
                ReleaseKeyboardFocus(this, EventArgs.Empty);
            }
        }

        /// <summary>
        ///     This raises the Enter event and sets IsMouseOver to true.  Override to add
        ///     custom handling code after calling this base method.
        /// </summary>
        protected internal virtual void OnMouseEnter() {
            if (MouseEnter != null) {
                MouseEnter(this, EventArgs.Empty);
            }

            IsMouseOver = true;
        }


        /// <summary>
        ///     This method raises the Leave event and sets IsMouseOver to false.  Override to add
        ///     custom handling code after calling this base method.
        /// </summary>
        protected internal virtual void OnMouseLeave() {
            if (MouseLeave != null) {
                MouseLeave(this, EventArgs.Empty);
            }

            IsMouseOver = false;
            IsBeingPushed = false;
        }


        /// <summary>
        ///     Base method sets the IsBeingPushed state if applicable.  Override to add
        ///     custom handling code after calling this base method.
        /// </summary>
        /// <param name="mouseData"></param>
        public override void OnMouseButtonDown(MouseData mouseData) {
            base.OnMouseButtonDown(mouseData);

            if (mouseData.MouseButton == MouseButton.Left) {
                IsBeingPushed = true;
            }
        }


        /// <summary>
        ///     Base method sets the IsBeingPushed state if applicable.  Override to add
        ///     custom handling code after calling this base method.
        /// </summary>
        /// <param name="mouseData"></param>
        public override void OnMouseButtonUp(MouseData mouseData) {
            base.OnMouseButtonUp(mouseData);

            if (mouseData.MouseButton == MouseButton.Left) {
                IsBeingPushed = false;
            }
        }


        /// <summary>
        ///     Base method requests that a tooltip be displayed, calling this.DetermineTooltipText()
        ///     to get the displayed text.  Override to add custom handling code after calling
        ///     this base method.
        /// </summary>
        /// <param name="mouseData"></param>
        public override void OnMouseHoverBegin(MouseData mouseData) {
            base.OnMouseHoverBegin(mouseData);
            ParentWindow.ShowTooltip(DetermineTooltipText(), mouseData.Position);
        }
    }
}