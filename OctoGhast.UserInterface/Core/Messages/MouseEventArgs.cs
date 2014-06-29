using System;
using libtcod;
using OctoGhast.Spatial;
using OctoGhast.UserInterface.Core.Messages.Interface;

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

        public MouseData(TCODMouseData tcodMouseData) {
            Position = new Vec(tcodMouseData.CellX, tcodMouseData.CellY);
            ScreenPosition = new Vec(tcodMouseData.PixelX, tcodMouseData.PixelY);
            MouseButton = MouseButton.None;

            if (tcodMouseData.LeftButton)
                MouseButton = MouseButton.Left;
            if (tcodMouseData.MiddleButton)
                MouseButton = MouseButton.Middle;
            if (tcodMouseData.RightButton)
                MouseButton = MouseButton.Right;
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