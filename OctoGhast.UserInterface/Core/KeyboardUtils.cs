using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Xna.Framework.Input;

namespace OctoGhast.UserInterface.Core
{
    internal static class KeyboardUtils
    {
        class CharPair
        {
            public readonly char NormalChar;
            public char? ShiftChar;

            public CharPair(char normalChar, char? shiftChar) {
                NormalChar = normalChar;
                ShiftChar = shiftChar;
            }
        }

        private static readonly Dictionary<Keys, CharPair> keyMap = new Dictionary<Keys, CharPair>();

        public static char? KeyToString(Keys key, bool shifted) {
            char? character = null;
            CharPair pair;

            if ((key >= Keys.A && key <= Keys.Z) || key == Keys.Space) {
                // If A-Z or space
                character = shifted ? (char) key : Char.ToLower((char) key);
            } else if (keyMap.TryGetValue(key, out pair)) {
                // Use the keyMap, Luke.
                if (!shifted) {
                    character = pair.NormalChar;
                } else if (pair.ShiftChar.HasValue) {
                    character = pair.ShiftChar.Value;
                }
            }

            return character;
        }

        static KeyboardUtils() {
            Initialize();
        }

        static void Initialize() {
            // TODO: Load from configuration file.

            AddKeyMap(Keys.OemTilde, "`~");
            AddKeyMap(Keys.D1, "1!");
            AddKeyMap(Keys.D2, "2@");
            AddKeyMap(Keys.D3, "3#");
            AddKeyMap(Keys.D4, "4$");
            AddKeyMap(Keys.D5, "5%");
            AddKeyMap(Keys.D6, "6^");
            AddKeyMap(Keys.D7, "7&");
            AddKeyMap(Keys.D8, "8*");
            AddKeyMap(Keys.D9, "9(");
            AddKeyMap(Keys.D0, "0)");
            AddKeyMap(Keys.OemMinus, "-_");
            AddKeyMap(Keys.OemPlus, "=+");

            // Second row of US keyboard.
            AddKeyMap(Keys.OemOpenBrackets, "[{");
            AddKeyMap(Keys.OemCloseBrackets, "]}");
            AddKeyMap(Keys.OemPipe, "\\|");

            // Third row of US keyboard.
            AddKeyMap(Keys.OemSemicolon, ";:");
            AddKeyMap(Keys.OemQuotes, "'\"");
            AddKeyMap(Keys.OemComma, ",<");
            AddKeyMap(Keys.OemPeriod, ".>");
            AddKeyMap(Keys.OemQuestion, "/?");

            // Keypad keys of US keyboard.
            AddKeyMap(Keys.NumPad1, "1");
            AddKeyMap(Keys.NumPad2, "2");
            AddKeyMap(Keys.NumPad3, "3");
            AddKeyMap(Keys.NumPad4, "4");
            AddKeyMap(Keys.NumPad5, "5");
            AddKeyMap(Keys.NumPad6, "6");
            AddKeyMap(Keys.NumPad7, "7");
            AddKeyMap(Keys.NumPad8, "8");
            AddKeyMap(Keys.NumPad9, "9");
            AddKeyMap(Keys.NumPad0, "0");
            AddKeyMap(Keys.Add, "+");
            AddKeyMap(Keys.Divide, "/");
            AddKeyMap(Keys.Multiply, "*");
            AddKeyMap(Keys.Subtract, "-");
            AddKeyMap(Keys.Decimal, ".");
        }

        static void AddKeyMap(Keys key, string charPair) {
            char first = charPair[0];
            char? second = null;

            if (charPair.Length > 1)
                second = charPair[1];

            keyMap.Add(key, new CharPair(first, second));
        }
    }
}