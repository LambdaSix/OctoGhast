using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using OctoGhast.Spatial;
using OctoGhast.UserInterface.Core;
using OctoGhast.UserInterface.Core.Messages;

namespace OctoGhast.Framework
{
    public class InputManager
    {
        public Component OwningWindow { get; private set; }

        private Vec lastMousePosition;
        private Vec lastMousePixelPosition;
        private MouseButton lastMouseButton;
        private float lastMouseMoveTime;

        private bool isHovering;
        private Vec StartLeftButtonDown;
        private bool isDragging;

        private const int DragPixel = 24;
        private const float HoverMs = 600f;
        
        private KeyboardState _previousKeyState { get; set; }
        private KeyboardState _currentKeyState { get; set; }

        /// <summary>
        /// Create an InputManager instance bound to a Window instance.
        /// </summary>
        /// <param name="IComponent"></param>
        public InputManager(Component IComponent) {
            if (IComponent == null)
                throw new ArgumentNullException("IComponent");

            OwningWindow = IComponent;
        }

        public void Update(GameTime time) {
            PollMouse(time.ElapsedGameTime.Milliseconds);
            PollKeyboard();
        }

        private void PollKeyboard() {
            _previousKeyState = _currentKeyState;
            _currentKeyState = Keyboard.GetState();

            foreach (var key in _currentKeyState.GetPressedKeys()) {
                if (_currentKeyState[key] == KeyState.Down)
                    OwningWindow.OnKeyPressed(new KeyboardData(key, _currentKeyState));
                else if (_previousKeyState[key] == KeyState.Down && _currentKeyState.IsKeyUp(key))
                    OwningWindow.OnKeyReleased(new KeyboardData(key, _currentKeyState));
            }
        }

        private void CheckMouseButtons(MouseData mouse) {
            if (mouse.MouseButton != lastMouseButton) {
                if (lastMouseButton == MouseButton.None)
                    DoMouseButtonDown(mouse);
                else
                    DoMouseButtonUp(new MouseData(lastMouseButton, mouse.Position, mouse.ScreenPosition));

                lastMouseButton = mouse.MouseButton;
            }
        }

        private void StartHover(MouseData mouse) {
            OwningWindow.OnMouseHoverBegin(mouse);
            isHovering = true;
        }

        private void StopHover(MouseData mouse) {
            OwningWindow.OnMouseHoverEnd(mouse);
            isHovering = false;
        }

        private void StartDrag(MouseData mouse) {
            isDragging = true;
            OwningWindow.OnMouseDragBegin(mouse.Position);
        }

        private void StopDrag(MouseData mouse) {
            isDragging = false;
            OwningWindow.OnMouseDragEnd(mouse.Position);
        }

        private void DoMouseMove(MouseData mouse) {
            StopHover(mouse);
            OwningWindow.OnMouseMoved(mouse);

            if (mouse.MouseButton == MouseButton.Left) {
                var delta = Math.Abs(mouse.ScreenPosition.X - StartLeftButtonDown.X)
                            + Math.Abs(mouse.ScreenPosition.Y - StartLeftButtonDown.Y);

                if (delta > DragPixel && isDragging == false) {
                    StartDrag(mouse);
                }
            }
        }

        private void DoMouseButtonDown(MouseData mouse) {
            if (isDragging)
                StopDrag(mouse);

            if (mouse.MouseButton == MouseButton.Left)
                StartLeftButtonDown = mouse.ScreenPosition;

            OwningWindow.OnMouseButtonDown(mouse);
        }

        private void DoMouseButtonUp(MouseData mouse) {
            if (isDragging)
                StopDrag(mouse);

            OwningWindow.OnMouseButtonUp(mouse);
        }

        private void PollMouse(int totalElapsed) {
            var mouse = new MouseData(Mouse.GetState(), Game.CurrentFont);
            CheckMouseButtons(mouse);

            if (mouse.ScreenPosition != lastMousePixelPosition) {
                DoMouseMove(mouse);

                lastMousePosition = mouse.Position;
                lastMousePixelPosition = mouse.ScreenPosition;
                lastMouseMoveTime = totalElapsed;
            }

            if ((totalElapsed - lastMouseMoveTime) > HoverMs && isHovering == false)
                StartHover(mouse);
        }
    }
}