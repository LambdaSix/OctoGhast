using System;
using System.Diagnostics;
using System.Reflection.Emit;
using OctoGhast.Spatial;
using OctoGhast.UserInterface.Core;
using OctoGhast.UserInterface.Core.Interface;
using OctoGhast.UserInterface.Core.Messages;
using OctoGhast.UserInterface.Templates;
using OctoGhast.UserInterface.Theme;

namespace OctoGhast.UserInterface.Controls
{
    public class CheckBoxTemplate : ControlTemplate
    {
        public Size AutoSizeOverride { get; set; }
        public string Label { get; set; }
        public int MinimumWidth { get; set; }
        public HAlign LabelAlignment { get; set; }
        public VAlign VerticalAlignment { get; set; }
        public bool CheckOnLeft { get; set; }
        public bool MouseOverHighlight { get; set; }
        public bool CanHaveKeyboardFocus { get; set; }
        public bool HasFrameBorder { get; set; }

        public CheckBoxTemplate() {
            Label = String.Empty;
            MinimumWidth = 0;
            LabelAlignment = HAlign.Left;
            CheckOnLeft = true;
            MouseOverHighlight = false;
            CanHaveKeyboardFocus = false;
            HasFrameBorder = true;
            VerticalAlignment = VAlign.Center;
        }

        public override Size CalculateSize() {
            if (!AutoSizeOverride.IsEmpty)
                return AutoSizeOverride;

            int width = CanvasUtil.MeasureStr(Label) + 1;
            int height = 1;

            if (HasFrameBorder) {
                width += 2;
                height += 2;
            }

            width = Math.Max(width, MinimumWidth);

            return new Size(width, height);
        }
    }

    public class CheckBox : ControlBase
    {
        public event EventHandler CheckBoxToggled;

        public string Label { get; set; }
        public bool CheckOnLeft { get; set; }
        public HAlign LabelAlignment { get; set; }
        public VAlign VerticalAlignment { get; set; }
        public bool IsChecked { get; set; }

        private Rect _labelRect;
        private Vec _checkPosition;

        public CheckBox(CheckBoxTemplate template) : base(template) {
            HasFrame = template.HasFrameBorder;

            if (Size.Height < 3 || Size.Width < 3)
                HasFrame = false;

            HilightWhenMouseOver = template.MouseOverHighlight;
            CanHaveKeyboardFocus = template.CanHaveKeyboardFocus;

            Label = template.Label ?? "";

            CheckOnLeft = template.CheckOnLeft;
            LabelAlignment = template.LabelAlignment;
            VerticalAlignment = template.VerticalAlignment;

            CalculateMetrics(template);
        }

        public override void OnMouseButtonDown(MouseData mouseData) {
            base.OnMouseButtonDown(mouseData);

            if (mouseData.MouseButton == MouseButton.Left) {
                if (IsChecked)
                    IsChecked = false;
                else
                    IsChecked = true;

                if (CheckBoxToggled != null)
                    CheckBoxToggled(this, EventArgs.Empty);
            }
        }

        protected override void Redraw() {
            base.Redraw();

            if (!String.IsNullOrWhiteSpace(Label))
                Canvas.PrintStringAligned(_labelRect.TopLeft, Label, LabelAlignment, VerticalAlignment, Size);

            if (IsActive) {
                if (IsChecked)
                    Canvas.PrintChar(_checkPosition, (char) 225, Pigments[PigmentType.ViewNormal]);
                else
                    Canvas.PrintChar(_checkPosition, (char) 224, Pigments[PigmentType.ViewNormal]);
            }
        }

        private void CalculateMetrics(CheckBoxTemplate template) {
            var inner = LocalRectangle;

            if (template.HasFrameBorder && template.CalculateSize().Height >= 3)
                inner = inner.Inflate(-1, -1);

            int checkX;

            if (CheckOnLeft) {
                checkX = inner.Left;
                _labelRect = new Rect(inner.TopLeft.OffsetX(1), inner.BottomRight);
            }
            else {
                checkX = inner.Right;
                _labelRect = new Rect(inner.TopLeft, inner.BottomRight.OffsetX(-1));
            }
            switch (VerticalAlignment) {
                case VAlign.Bottom:
                    _checkPosition = new Vec(checkX, _labelRect.Bottom);
                    break;

                case VAlign.Center:
                    _checkPosition = new Vec(checkX, _labelRect.Center.Y);
                    break;

                case VAlign.Top:
                    _checkPosition = new Vec(checkX, _labelRect.Top);
                    break;
            }
        }
    }
}