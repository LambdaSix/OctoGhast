using System.Data;
using libtcod;

namespace OctoGhast.UserInterface.Core.Messages
{
    /// <summary>
    /// Provide a wrapper around a key binding
    /// </summary>
    public class KeyBindInfo
    {
        public char Key { get; private set; }
        public TCODKeyCode KeyCode { get; private set; }

        public KeyBindInfo(char key) {
            Key = key;
        }

        public KeyBindInfo(TCODKeyCode keyCode) {
            KeyCode = keyCode;
        }

        public override bool Equals(object obj) {
            if (!(obj is KeyBindInfo)) return false;

            return ((KeyBindInfo) obj).Key == Key && ((KeyBindInfo) obj).KeyCode == KeyCode;
        }

        public override int GetHashCode() {
            return Key.GetHashCode() ^ KeyCode.GetHashCode();
        }

        // Aids for implicit conversions.
        // Into the danger zoooone
        public static implicit operator KeyBindInfo(TCODKeyCode code) {
            return new KeyBindInfo(code);
        }

        public static implicit operator KeyBindInfo(char code) {
            return new KeyBindInfo(code);
        }

        public static implicit operator KeyBindInfo(KeyboardData data) {
            return new KeyBindInfo(data.KeyCode)
            {
                Key = data.Character
            };
        }
    }
}