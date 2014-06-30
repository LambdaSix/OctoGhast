using System;
using System.Xml.Schema;
using OctoGhast.Spatial;
using OctoGhast.UserInterface.Core;
using OctoGhast.UserInterface.Templates;

namespace OctoGhast.UserInterface.Controls
{
    [Flags]
    public enum TextEntryValidations
    {
        /// <summary>
        /// Allow all printable characters, excluding control codes.
        /// </summary>
        All = 15,

        /// <summary>
        /// Allow [a-zA-Z\s]
        /// </summary>
        Letters = 1,

        /// <summary>
        /// Allow [0-9]
        /// </summary>
        Numbers = 2,

        /// <summary>
        /// Allow [+-.0-9]
        /// </summary>
        Decimal = 4,

        /// <summary>
        /// Allow all printable symbols that are not numbers or letters.
        /// </summary>
        Symbols = 8,
    }

    public class TextEntryTemplate : EntryTemplate
    {
        public int MaximumCharacters { get; set; }
        public TextEntryValidations Validation { get; set; }
        public string StartingField { get; set; }

        public TextEntryTemplate() {
            MaximumCharacters = 1;
            Validation = TextEntryValidations.All;

            StartingField = "";
        }

        public override Size CalculateSize() {
            if (AutoSizeOverride.Height > 0 && AutoSizeOverride.Width >= MaximumCharacters) {
                return AutoSizeOverride;
            }

            if (Label == null)
                Label = "";

            int len = CanvasUtil.MeasureStr(Label);

            int frameSize = 0;

            if (HasFrameBorder) {
                frameSize = 2;
            }

            return new Size(len + MaximumCharacters + 1 + frameSize, 1 + frameSize);
        }

        public override int CalculateMaxCharacters() {
            return MaximumCharacters;
        }
    }

    public class TextEntry : Entry
    {
        public TextEntryValidations Validation { get; set; }

        public TextEntry(TextEntryTemplate template) : base(template) {
            Validation = template.Validation;
            TrySetField(template.StartingField);
        }

        protected override string DefaultField {
            get { return ""; }
        }

        protected override bool ValidateField(string entry) {
            return true;
        }

        protected override bool ValidateCharacter(char character) {
            var valid = false;

            if (Validation.HasFlag(TextEntryValidations.Numbers) || Validation.HasFlag(TextEntryValidations.Decimal)) {
                if (Char.IsNumber(character)) {
                    valid = true;
                }
            }

            if (Validation.HasFlag(TextEntryValidations.Letters)) {
                if (Char.IsLetter(character) || Char.IsWhiteSpace(character)) {
                    valid = true;
                }
            }

            if (Validation.HasFlag(TextEntryValidations.Decimal)) {
                if (character == '+' || character == '-' || character == '.') {
                    valid = true;
                }
            }

            if (Validation.HasFlag(TextEntryValidations.Symbols)) {
                if (Char.IsSymbol(character)) {
                    valid = true;
                }
            }

            return valid;
        }
    }
}