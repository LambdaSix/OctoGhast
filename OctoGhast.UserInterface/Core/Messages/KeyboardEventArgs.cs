using System;
using libtcod;

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

    public class KeyboardData
    {
        public char Character { get; private set; }
        public TCODKeyCode KeyCode { get; private set; }
        public bool IsKeyDown { get; private set; }
        public ControlKeys ControlKeys { get; private set; }

        public KeyboardData(char character, TCODKeyCode keyCode, bool isKeyDown, ControlKeys controlKeys) {
            Character = character;
            KeyCode = keyCode;
            IsKeyDown = isKeyDown;
            ControlKeys = controlKeys;
        }

        public KeyboardData(TCODKey tcodKey) {
            Character = tcodKey.Character;
            KeyCode = tcodKey.KeyCode;
            IsKeyDown = tcodKey.Pressed;

            var modifier = ControlKeys.None;

            if (tcodKey.LeftAlt)
                modifier |= ControlKeys.LeftAlt;
            if (tcodKey.RightAlt)
                modifier |= ControlKeys.RightAlt;
            if (tcodKey.LeftControl)
                modifier |= ControlKeys.LeftControl;
            if (tcodKey.RightControl)
                modifier |= ControlKeys.RightControl;
            if (tcodKey.Shift)
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