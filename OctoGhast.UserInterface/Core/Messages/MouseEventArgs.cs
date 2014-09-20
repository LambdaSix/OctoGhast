using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using OctoGhast.Spatial;
using OctoGhast.UserInterface.Core.Messages.Interface;
using RenderLike;

namespace OctoGhast.UserInterface.Core.Messages
{
    public class MouseData : IMouseData
    {
        public Vec Position { get; private set; }
        public Vec ScreenPosition { get; private set; }
        public MouseButton MouseButton { get; private set; }

        public MouseData(MouseButton button, Vec position, Vec screenPosition) {
            MouseButton = button;
            Position = position;
            ScreenPosition = screenPosition;
        }

        public MouseData(MouseState xnaState, Font cellSize) {
            ScreenPosition = new Vec(xnaState.X, xnaState.Y);
            Position = new Vec(xnaState.X/cellSize.CharacterWidth, xnaState.Y/cellSize.CharacterHeight);
            MouseButton = MouseButton.None;

            if (xnaState.LeftButton == ButtonState.Pressed)
                MouseButton = MouseButton.Left;
            if (xnaState.RightButton == ButtonState.Pressed)
                MouseButton = MouseButton.Right;
            if (xnaState.MiddleButton == ButtonState.Pressed)
                MouseButton = MouseButton.Middle;
        }
    }

    public class MouseEventArgs : EventArgs
    {
        public MouseData MouseData { get; private set; }

        public MouseEventArgs(MouseData mouseData) {
            MouseData = mouseData;
        }
    }

    public class MouseDragEventArgs : EventArgs
    {
        public Vec ScreenPosition { get; private set; }

        public MouseDragEventArgs(Vec screenPosition) {
            ScreenPosition = screenPosition;
        }
    }
}