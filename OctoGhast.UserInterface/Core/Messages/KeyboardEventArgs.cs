using System;
using Microsoft.Xna.Framework.Input;
using OctoGhast.UserInterface.Core.Messages.Interface;

namespace OctoGhast.UserInterface.Core.Messages
{
    [Flags]
    public enum ControlKeys
    {
        None = 0,
        LeftAlt = 1,
        RightAlt = 2,
        LeftControl = 4,
        RightControl = 8,
        Shift = 16,
    }

    public class KeyboardData : IKeyboardData
    {
        public Keys KeyCode { get; private set; }
        public bool IsKeyDown { get; private set; }
        public ControlKeys ControlKeys { get; private set; }

        public KeyboardData(Keys keyCode, bool isKeyDown, ControlKeys controlKeys) {
            KeyCode = keyCode;
            IsKeyDown = isKeyDown;
            ControlKeys = controlKeys;
        }

        public KeyboardData(Keys key, KeyboardState keyboardState) {
            KeyCode = key;
            IsKeyDown = keyboardState.IsKeyDown(key);

            var modifier = ControlKeys.None;

            if (keyboardState.IsKeyDown(Keys.LeftAlt))
                modifier |= ControlKeys.LeftAlt;

            if (keyboardState.IsKeyDown(Keys.RightAlt))
                modifier |= ControlKeys.RightAlt;

            if (keyboardState.IsKeyDown(Keys.LeftControl))
                modifier |= ControlKeys.LeftControl;

            if (keyboardState.IsKeyDown(Keys.RightControl))
                modifier |= ControlKeys.RightControl;

            if (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
                modifier |= ControlKeys.Shift;

            ControlKeys = modifier;
        }
    }

    public class KeyboardEventArgs : EventArgs
    {
        public KeyboardData KeyboardData { get; private set; }

        public KeyboardEventArgs(KeyboardData keyboardData) {
            KeyboardData = keyboardData;
        }
    }
}