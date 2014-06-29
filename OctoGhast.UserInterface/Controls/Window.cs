using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using OctoGhast.Spatial;
using OctoGhast.UserInterface.Core;
using OctoGhast.UserInterface.Core.Messages;
using OctoGhast.UserInterface.Templates;
using OctoGhast.UserInterface.Theme;

namespace OctoGhast.UserInterface.Controls
{
    /// <summary>
    /// When subclassing a type of Window, consider
    /// also subclassing WindowTemplate to provide an interface for the client to specify
    /// options.
    /// </summary>
    public class WindowTemplate : WidgetTemplate
    {
        private readonly Size _size ;

        /// <summary>
        /// Default constructor initializes properties to their defaults.
        /// </summary>
        public WindowTemplate(Size size)
        {
            HasFrame = false;

            TooltipFGAlpha = 1.0f;
            TooltipBGAlpha = 0.6f;
            _size = size;
        }

        /// <summary>
        /// True if a frame is drawn around the window initially.
        /// </summary>
        public bool HasFrame { get; set; }

        /// <summary>
        /// The foreground alpha for any tooltips shown on this window.  Default to 1.0.
        /// </summary>
        public float TooltipFGAlpha { get; set; }

        /// <summary>
        /// The background alpha for any tooltips shown on this window.  Defaults to 0.6.
        /// </summary>
        public float TooltipBGAlpha { get; set; }

        /// <summary>
        /// Returns the screen size.
        /// </summary>
        /// <returns></returns>
        public override Size CalculateSize() {
            return _size;
        }
    }

    /// <summary>
    /// Windows act as a drawing region and container of controls.
    /// A window is the same size as the screen buffer and an application has
    /// </summary>
    public class Window : Widget
    {
        public IList<Control> Controls { get; set; }
        private ICollection<Control> ControlsPending { get; set; }
        private ICollection<Control> ControlsRemoving { get; set; } 
        
        public IList<Manager> Managers { get; set; }
        private ICollection<Manager> ManagersPending { get; set; } 
        private ICollection<Manager> ManagersRemoving { get; set; }

        public Control CurrentKeyboardFocus { get; private set; }
        public Control CurrentUnderMouse { get; private set; }
        public Control LastLeftButtonDown { get; private set; }
        public Control CurrrentDragging { get; private set; }

        internal Tooltip CurrentTooltip { get; set; }
        internal Control CurrentDragging { get; set; }

        // TODO: Replace with the application framework reference
        private Size WindowSize { get; set; }

        public IApplication ParentApplication { get; set; }

        public float TooltipBGAlpha { get; set; }
        public float TooltipFGAlpha { get; set; }

        private bool HasFrame { get; set; }

        protected IReadOnlyCollection<Control> ControlList
        {
            get { return new ReadOnlyCollection<Control>(Controls); }
        }

        public Window(WindowTemplate template) : base(template) {
            WindowSize = template.CalculateSize();

            Controls = new List<Control>();
            ControlsPending = new List<Control>();
            ControlsRemoving = new List<Control>();
            Managers = new List<Manager>();
            ManagersPending = new List<Manager>();
            ManagersRemoving = new List<Manager>();
        }

        public void AddManager(Manager manager) {
            if (Managers.Contains(manager) || ManagersPending.Contains(manager))
                throw new ArgumentException("Manager instances should be unique", "manager");

            ManagersPending.Add(manager);
            manager.ParentWindow = this;

            if (!manager.Initialized)
                manager.OnSettingUp();
        }

        public void AddManagers(IEnumerable<Manager> managers) {
            foreach (var manager in managers) {
                AddManager(manager);
            }
        }

        public void RemoveManager(Manager manager) {
            if (manager == null)
                throw new ArgumentNullException("manager");

            if (Managers.Contains(manager))
                ManagersRemoving.Add(manager);

            if (ManagersPending.Contains(manager))
                ManagersPending.Remove(manager);
        }

        public bool ContainsControl(Control control) {
            return Controls.Contains(control);
        }

        public bool AddControl(Control control) {
            if (ContainsControl(control) || ControlsPending.Contains(control))
                throw new ArgumentException("CurrentWindow already contains an instance of this control");

            ControlsPending.Add(control);
            bool atRequestedPosition = CheckAddedControlPosition(control);

            if (!atRequestedPosition && ScreenRectangle.Contains(control.ScreenRectangle))
                throw new ArgumentException("The specified control is too large to fit on the screen");

            CheckAddedControlMessages(control);

            control.ParentWindow = this;
            control.Pigments = new PigmentMapping(Pigments, control.PigmentOverrides);

            if (!control.Initialized) {
                control.OnSettingUp();
            }

            return atRequestedPosition;
        }

        public void AddControls(IEnumerable<Control> controls) {
            foreach (var control in controls) {
                AddControl(control);
            }
        }

        public void RemoveControl(Control control) {
            if (control == null)
                throw new ArgumentNullException("control");

            if (ContainsControl(control))
                ControlsRemoving.Add(control);

            if (ControlsPending.Contains(control))
                ControlsPending.Remove(control);
        }

        public void MoveToTop(Control control) {
            if (control == null)
                throw new ArgumentNullException("control");

            // Danger will robinson, this relies on List<T> preserving ordering.

            if (ContainsControl(control)) {
                Controls.Remove(control);
                Controls.Add(control);
            }
        }

        public void MoveToBottom(Control control) {
            if (control == null)
                throw new ArgumentNullException("control");

            if (ContainsControl(control)) {
                Controls.Remove(control);
                Controls.Insert(0, control);
            }
        }

        public void ReleaseKeyboard(Control control) {
            if (control == null)
                throw new ArgumentNullException("control");

            if (control == CurrentKeyboardFocus) {
                control.OnReleaseKeyboardFocus();
                CurrentKeyboardFocus = null;
            }
        }

        public void TakeKeyboard(Control control) {
            if (control == null)
                throw new ArgumentNullException("control");

            if (control != CurrentKeyboardFocus) {
                control.OnTakeKeyboardFocus();
                if (CurrentKeyboardFocus != null) 
                    CurrentKeyboardFocus.OnReleaseKeyboardFocus();

                CurrentKeyboardFocus = control;
            }
        }

        protected Control GetTopControlAt(Vec screenPos) {
            return Controls.Where(control => control.IsActive)
                .FirstOrDefault(control => control.ScreenRectangle.Contains(screenPos));
        }

        protected internal void ShowTooltip(string text, Vec screenPos) {
            if (!String.IsNullOrWhiteSpace(text))
                CurrentTooltip = new Tooltip(text, screenPos, this);
        }

        protected override void Redraw()
        {
            base.Redraw();

            if (HasFrame) {
                Canvas.PrintFrame("", DetermineFramePigment());
            }
        }

        protected override Pigment DetermineMainPigment() {
            return Pigments[PigmentType.Window];
        }

        protected override Pigment DetermineFramePigment() {
            return Pigments[PigmentType.FrameNormal];
        }

        public override void OnDraw() {
            base.OnDraw();

            foreach (var control in Controls) {
                control.OnDraw();
            }

            if (CurrentTooltip != null)
                CurrentTooltip.DrawToScreen();
        }

        public override void OnTick() {
            base.OnTick();

            UpdateManagers();
            UpdateControls();

            Notify(Managers, _ => _.OnTick());
            Notify(Controls, _ => _.OnTick());
        }

        public override void OnQuitting() {
            base.OnQuitting();

            Notify(Managers, _ => _.OnQuitting());
            Notify(Controls, _ => _.OnQuitting());
        }

        public override void OnKeyPressed(KeyboardData keyData) {
            base.OnKeyPressed(keyData);

            Notify(Managers, _ => _.OnKeyPressed(keyData));
            Notify(Controls, _ => _.OnKeyPressed(keyData));
        }

        public override void OnKeyReleased(KeyboardData keyData) {
            base.OnKeyReleased(keyData);

            Notify(Managers, _ => _.OnKeyReleased(keyData));
            Notify(Controls, _ => _.OnKeyReleased(keyData));
        }

        public override void OnMouseButtonDown(MouseData mouseData) {
            base.OnMouseButtonDown(mouseData);

            Notify(Managers, _ => _.OnMouseButtonDown(mouseData));

            if (CurrentUnderMouse != null && CurrentUnderMouse.IsActive) {
                CurrentUnderMouse.OnMouseButtonDown(mouseData);
                LastLeftButtonDown = CurrentUnderMouse;
            }

            if (CurrentKeyboardFocus != null && CurrentKeyboardFocus != CurrentUnderMouse) {
                CurrentKeyboardFocus.OnReleaseKeyboardFocus();
                CurrentKeyboardFocus = null;
            }

            if (CanAssignFocus(CurrentUnderMouse, mouseData)) {
                CurrentKeyboardFocus = CurrentUnderMouse;
                CurrentKeyboardFocus.OnTakeKeyboardFocus();
            }
        }

        public override void OnMouseButtonUp(MouseData mouseData) {
            base.OnMouseButtonUp(mouseData);

            Notify(Managers, _ => _.OnMouseButtonUp(mouseData));

            if (CurrentUnderMouse != null && CurrentUnderMouse.IsActive) {
                CurrentUnderMouse.OnMouseButtonUp(mouseData);
            }

            LastLeftButtonDown = null;
        }

        public override void OnMouseMoved(MouseData mouseData) {
            base.OnMouseMoved(mouseData);

            Notify(Managers, _ => _.OnMouseMoved(mouseData));

            var checkUnderMouse = GetTopControlAt(mouseData.Position);
            if (checkUnderMouse != CurrentUnderMouse) {
                if (CurrentUnderMouse != null && CurrentUnderMouse.IsActive)
                    CurrentUnderMouse.OnMouseLeave();

                if (checkUnderMouse != null && checkUnderMouse.IsActive)
                    checkUnderMouse.OnMouseEnter();

                CurrentUnderMouse = checkUnderMouse;
            }

            if (CurrentUnderMouse != null && CurrentUnderMouse.IsActive)
                CurrentUnderMouse.OnMouseMoved(mouseData);
        }

        public override void OnMouseHoverBegin(MouseData mouseData) {
            base.OnMouseHoverBegin(mouseData);

            Notify(Managers, _ => _.OnMouseHoverBegin(mouseData));

            if (CurrentUnderMouse != null && CurrentUnderMouse.IsActive)
                CurrentUnderMouse.OnMouseHoverBegin(mouseData);
        }

        public override void OnMouseHoverEnd(MouseData mouseData) {
            if (CurrentTooltip != null) {
                CurrentTooltip.Dispose();
                CurrentTooltip = null;
            }

            base.OnMouseHoverEnd(mouseData);

            Notify(Managers, _ => _.OnMouseHoverEnd(mouseData));

            if (CurrentUnderMouse != null && CurrentUnderMouse.IsActive)
                CurrentUnderMouse.OnMouseHoverEnd(mouseData);
        }

        public override void OnMouseDragBegin(Vec startPosition) {
            base.OnMouseDragBegin(startPosition);

            Notify(Managers, _ => _.OnMouseDragBegin(startPosition));

            if (LastLeftButtonDown != null && LastLeftButtonDown.IsActive) {
                CurrentDragging = LastLeftButtonDown;
                LastLeftButtonDown.OnMouseDragBegin(startPosition);
            }
        }

        public override void OnMouseDragEnd(Vec endPosition) {
            base.OnMouseDragEnd(endPosition);

            Notify(Managers, _ => _.OnMouseDragEnd(endPosition));

            if (CurrentUnderMouse != null && CurrentUnderMouse.IsActive) {
                CurrentDragging = null;
                CurrentUnderMouse.OnMouseDragEnd(endPosition);
            }
        }

        private bool CanAssignFocus(Control underMouse, MouseData mouseData) {
            return underMouse != null
                   && underMouse.CanHaveKeyboardFocus
                   && !underMouse.HasKeyboardFocus
                   && mouseData.MouseButton == MouseButton.Left
                   && underMouse.IsActive;
        }

        private void Notify<T>(IEnumerable<T> items, Action<T> action) {
            foreach (var item in items) {
                action(item);
            }
        }

        public Vec AutoPosition(Vec nearPos, Size controlSize) {
            var container = new Rect(nearPos, controlSize);
            int dx = 0;
            int dy = 0;

            int screenRight = WindowSize.Width - 1;
            int screenBottom = WindowSize.Height - 1;

            if (container.Left < 0)
                dx = -container.Left;
            else if (container.Right > screenRight)
                dx = screenRight - container.Right;

            if (container.Top < 0)
                dy = -container.Top;
            else if (container.Bottom > screenBottom)
                dy = screenBottom - container.Bottom;

            int finalX = nearPos.X + dx;
            int finalY = nearPos.Y + dy;
            return new Vec(finalX, finalY);
        }

        private void UpdateManagers() {
            foreach (var manager in ManagersPending) {
                Managers.Add(manager);
            }

            ManagersPending.Clear();

            foreach (var manager in ManagersRemoving) {
                Managers.Remove(manager);
            }

            ManagersRemoving.Clear();
        }

        private void UpdateControls() {
            foreach (var control in ControlsPending) {
                Controls.Add(control);
            }

            ControlsPending.Clear();

            foreach (var control in ControlsRemoving) {
                Controls.Remove(control);
            }

            ControlsRemoving.Clear();
        }

        private bool CheckAddedControlPosition(Control control) {
            Vec newVec = AutoPosition(control.Position, control.Size);

            if (newVec == control.Position)
                return true;

            control.Position = newVec;
            return false;
        }

        private void CheckAddedControlMessages(Control control) {
            if (control.ScreenRectangle.Contains(CurrentMousePosition)) {
                control.OnMouseEnter();
                CurrentUnderMouse = control;
            }
        }

        protected override void Dispose(bool isDisposing) {
            base.Dispose(isDisposing);

            if (isDisposing) {
                if (CurrentTooltip != null)
                    CurrentTooltip.Dispose();
            }
        }
    }
}