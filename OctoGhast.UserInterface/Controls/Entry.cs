using System;
using System.Reflection.Emit;
using libtcod;
using OctoGhast.Spatial;
using OctoGhast.UserInterface.Core;
using OctoGhast.UserInterface.Core.Interface;
using OctoGhast.UserInterface.Core.Messages;
using OctoGhast.UserInterface.Templates;
using OctoGhast.UserInterface.Theme;

namespace OctoGhast.UserInterface.Controls
{
    public abstract class EntryTemplate : ControlTemplate
    {
        public HAlign LabelAlign { get; set; }
        public VAlign VerticalAlign { get; set; }
        public bool ReplaceOnFirstKey { get; set; }
        public bool CommitOnLostFocus { get; set; }
        public string Label { get; set; }
        public uint BlinkDelay { get; set; }

        public EntryTemplate() {
            Label = "";
            CanHaveKeyboardFocus = true;
            MouseOverHighlight = false;
            CommitOnLostFocus = false;
            ReplaceOnFirstKey = false;
            HasFrameBorder = true;
            VerticalAlign = VAlign.Center;
            LabelAlign = HAlign.Left;
            BlinkDelay = 500;
        }

        public abstract int CalculateMaxCharacters();
    }

    public abstract class Entry : ControlBase
    {
        private bool waitingToCommitText { get; set; }
        private bool waitingToOverwrite { get; set; }
        private bool _cursorOn = true;
        private Rect _labelRect;
        private Rect _fieldRect;
        private int _cursorY;
        private readonly uint _blinkDelay;

        protected abstract string DefaultField { get; }

        public event EventHandler EntryChanged;

        public string Label { get; set; }
        public int MaximumCharacters { get; set; }

        public bool CommitOnLostFocus { get; set; }
        public bool ReplaceOnFirstKey { get; set; }

        public VAlign VerticalAlign { get; set; }
        public HAlign LabelAlign { get; set; }

        public string CurrentText { get; set; }
        public string TextInput { get; set; }
        public int CursorPos { get; set; }

        public Entry(EntryTemplate template) : base(template) {
            Label = template.Label;

            if (Size.Width < 3 || Size.Height < 3)
                template.HasFrameBorder = false;

            HasFrame = template.HasFrameBorder;
            MaximumCharacters = template.CalculateMaxCharacters();

            CommitOnLostFocus = template.CommitOnLostFocus;
            ReplaceOnFirstKey = template.ReplaceOnFirstKey;
            CanHaveKeyboardFocus = template.CanHaveKeyboardFocus;
            MouseOverHighlight = template.MouseOverHighlight;

            VerticalAlign = template.VerticalAlign;
            LabelAlign = template.LabelAlign;

            CurrentText = "";
            waitingToCommitText = false;
            TextInput = CurrentText;
            _blinkDelay = template.BlinkDelay;

            CalcMetrics(template);
        }

        protected abstract bool ValidateCharacter(char character);

        protected abstract bool ValidateField(string entry);

        public bool TrySetField(string changeTo) {
            TextInput = changeTo;
            return TryCommit();
        }

        public bool TryCommit() {
            if (CurrentText == TextInput)
                return false;
            if (ValidateField(TextInput) && TextInput.Length <= MaximumCharacters) {
                CurrentText = TextInput;

                OnFieldChanged();
                return true;
            }
            TextInput = CurrentText;
            return false;
        }

        public override void OnSettingUp()
        {
            base.OnSettingUp();

            // Add a schedule to blink the cursor (turn it on if off, and Vice versa)
            AddSchedule(new Schedule(() => _cursorOn = !_cursorOn, _blinkDelay));
        }

        protected override void Redraw() {
            base.Redraw();

            if (!String.IsNullOrEmpty(Label))
                Canvas.PrintStringAligned(_labelRect.TopLeft, Label, LabelAlign, VerticalAlign, Size);

            if (waitingToOverwrite) {
                Canvas.PrintStringAligned(_fieldRect.TopLeft, TextInput, HAlign.Left, VerticalAlign, Size,
                    Pigments[PigmentType.ViewSelected]);
            }
            else {
                Canvas.PrintStringAligned(_fieldRect.TopLeft, TextInput, HAlign.Left, VerticalAlign, Size);
            }

            if (_cursorOn && HasKeyboardFocus) {
                int cursorX = _fieldRect.Left + CursorPos;
                if (cursorX <= LocalRectangle.Right) {
                    Canvas.PrintChar(cursorX, _cursorY, (char) TCODSpecialCharacter.Block1,
                        Pigments[PigmentType.ViewSelected]);
                }
            }
        }

        protected virtual void OnFieldChanged() {
            if (EntryChanged != null)
                EntryChanged(this, EventArgs.Empty);
        }

        public override void OnKeyPressed(KeyboardData keyData) {
            base.OnKeyPressed(keyData);

            if (keyData.Character != 0 && ValidateCharacter(keyData.Character)) {
                if (waitingToOverwrite) {
                    TextInput = keyData.Character.ToString();
                    CursorPos = 1;
                    waitingToOverwrite = false;
                }
                else if (TextInput.Length < MaximumCharacters) {
                    TextInput += keyData.Character;
                    CursorPos++;
                }
            }
            else if (keyData.KeyCode == TCODKeyCode.Backspace && TextInput.Length > 0) {
                TextInput = TextInput.Substring(0, TextInput.Length - 1);
                CursorPos--;
            }
            else if (keyData.KeyCode == TCODKeyCode.Enter) {
                waitingToCommitText = true;
                ParentWindow.ReleaseKeyboard(this);
            }
            else if (keyData.KeyCode == TCODKeyCode.Escape) {
                TextInput = CurrentText;
                waitingToCommitText = true;
                ParentWindow.ReleaseKeyboard(this);
            }
        }

        protected internal override void OnTakeKeyboardFocus() {
            base.OnTakeKeyboardFocus();

            waitingToCommitText = false;
            TextInput = CurrentText;

            if (ReplaceOnFirstKey)
                waitingToOverwrite = true;

            CursorPos = CurrentText.Length;
        }

        protected internal override void OnReleaseKeyboardFocus() {
            base.OnReleaseKeyboardFocus();

            if (waitingToCommitText || CommitOnLostFocus)
                TryCommit();
            else
                TextInput = CurrentText;

            waitingToOverwrite = false;
        }

        private void CalcMetrics(EntryTemplate template) {
            var viewRect = LocalRectangle;

            if (template.HasFrameBorder)
                viewRect = viewRect.Inflate(-1, -1);

            int remaining = viewRect.Size.Y;
            int labelLength = template.Label.Length;
            int fieldLength = template.CalculateMaxCharacters();

            remaining -= fieldLength;

            if (remaining < 0) {
                fieldLength += remaining;
                labelLength = 0;
            }
            else {
                remaining -= labelLength;
                labelLength += remaining - 1;
            }

            if (labelLength < 1) {
                Label = "";
                labelLength = 0;
            }

            _labelRect = new Rect(viewRect.TopLeft, new Size(labelLength, viewRect.Size.X));
            _fieldRect = new Rect(_labelRect.TopRight.OffsetX(1), new Size(fieldLength, viewRect.Size.X));

            switch (VerticalAlign)
            {
                case VAlign.Top:
                    _cursorY = _fieldRect.Top;
                    break;

                case VAlign.Center:
                    _cursorY = _fieldRect.Center.Y;
                    break;

                case VAlign.Bottom:
                    _cursorY = _fieldRect.Bottom;
                    break;
            }
        }
    }
}