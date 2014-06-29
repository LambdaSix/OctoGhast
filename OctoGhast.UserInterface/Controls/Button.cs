﻿using System;
using OctoGhast.Spatial;
using OctoGhast.UserInterface.Core;
using OctoGhast.UserInterface.Core.Interface;
using OctoGhast.UserInterface.Core.Messages;
using OctoGhast.UserInterface.Templates;
using OctoGhast.UserInterface.Theme;

namespace OctoGhast.UserInterface.Controls
{
    public class ButtonTemplate : ControlTemplate
    {
        public string Label { get; set; }
        public int MinimumWidth { get; set; }
        public HAlign LabelAlignment { get; set; } 
        public bool MouseOverHighlight { get; set; }
        public bool CanHaveKeyboardFocus { get; set; }
        public bool HasFrameBorder { get; set; }
        public Size AutoSizeOverride { get; set; }
        public VAlign VAlignment { get; set; }
        
        public ButtonTemplate() {
            LabelAlignment = HAlign.Left;
            Label = "";
            MinimumWidth = 0;
            MouseOverHighlight = true;
            CanHaveKeyboardFocus = false;
            HasFrameBorder = true;
            VAlignment = VAlign.Center;
        }

        public override Size CalculateSize() {
            if (AutoSizeOverride.IsEmpty) {
                int len = CanvasUtil.MeasureStr(Label);
                int width = len;
                int height = 2;

                if (HasFrameBorder) {
                    width += 2;
                    height += 2;
                }

                return new Size(Math.Max(width, MinimumWidth), height);
            }

            return AutoSizeOverride;
        }
    }

    public class Button : Control
    {
        public event EventHandler ButtonClick;

        public string Label { get; private set; }
        public HAlign LabelAlignment { get; set; }
        public VAlign VAlignment { get; set; }

        private Rect LabelRect { get; set; }

        public Button(ButtonTemplate template) : base(template) {
            Label = template.Label;
            LabelAlignment = template.LabelAlignment;
            HilightWhenMouseOver = template.MouseOverHighlight;
            CanHaveKeyboardFocus = template.CanHaveKeyboardFocus;

            LabelRect = new Rect(Vec.Zero, Size);
            VAlignment = template.VAlignment;

            if (template.HasFrameBorder && Size.Width > 2 && Size.Height > 2)
                LabelRect = LabelRect.Inflate(-1, -1);
        }

        protected override void Redraw() {
            base.Redraw();
            if (!OwnerDraw) {
                Canvas.PrintStringAligned(LabelRect.TopLeft, Label, LabelAlignment, VAlignment, Size);
            }
        }

        protected override Pigment DetermineMainPigment() {
            if (IsActive && IsBeingPushed)
                return Pigments[PigmentType.ViewDepressed];

            return base.DetermineMainPigment();
        }

        protected override Pigment DetermineFramePigment() {
            if (IsActive && IsBeingPushed)
                return Pigments[PigmentType.FrameDepressed];

            return base.DetermineFramePigment();
        }

        public override void OnMouseButtonUp(MouseData mouseData) {
            var wasBeingPushed = IsBeingPushed;

            base.OnMouseButtonUp(mouseData);

            if (mouseData.MouseButton == MouseButton.Left && wasBeingPushed)
                OnButtonPushed();
        }

        protected virtual void OnButtonPushed() {
            if (ButtonClick != null)
                ButtonClick(this, EventArgs.Empty);
        }
    }
}