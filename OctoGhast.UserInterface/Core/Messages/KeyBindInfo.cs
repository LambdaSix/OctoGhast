using System.Data;
using Microsoft.Xna.Framework.Input;

namespace OctoGhast.UserInterface.Core.Messages
{
    /// <summary>
    /// Provide a wrapper around a key binding
    /// </summary>
    public class KeyBindInfo
    {
        public Keys KeyCode { get; private set; }

        public KeyBindInfo(Keys keyCode) {
            KeyCode = keyCode;
        }

        public override bool Equals(object obj) {
            if (!(obj is KeyBindInfo)) return false;

            return ((KeyBindInfo) obj).KeyCode == KeyCode;
        }

        public override int GetHashCode() {
            return KeyCode.GetHashCode();
        }

        // Aids for implicit conversions.
        // Into the danger zoooone
        public static implicit operator KeyBindInfo(Keys code) {
            return new KeyBindInfo(code);
        }

        public static implicit operator KeyBindInfo(KeyboardData data) {
            return new KeyBindInfo(data.KeyCode)
            {
                KeyCode = data.KeyCode
            };
        }
    }
}