using System;
using OctoGhast.Spatial;
using OctoGhast.UserInterface.Core;
using OctoGhast.UserInterface.Core.Interface;
using OctoGhast.UserInterface.Templates;
using OctoGhast.UserInterface.Theme;

namespace OctoGhast.UserInterface.Controls
{
    public class LabelTemplate : ControlTemplate
    {
        public string Label { get; set; }
        public int MinimumWidth { get; set; }
        public HAlign LabelAlignment { get; set; }
        public VAlign VAlignment { get; set; }

        public LabelTemplate()
        {
            HasFrameBorder = false;
            CanHaveKeyboardFocus = false;
            MouseOverHighlight = false;
            LabelAlignment = HAlign.Left;
            VAlignment = VAlign.Center;
        }

        public override Size CalculateSize()
        {
            if (AutoSizeOverride.IsEmpty)
            {
                int len = CanvasUtil.MeasureStr(Label);
                int width = len;
                int height = 2;

                if (HasFrameBorder)
                {
                    width += 2;
                    height += 2;
                }

                return new Size(Math.Max(width, MinimumWidth), height);
            }

            return AutoSizeOverride;
        }
    }

    public class Label : ControlBase
    {
        public string LabelText { get; set; }
        public HAlign LabelAlignment { get; set; }
        public VAlign VAlignment { get; set; }
        private Rect LabelRect { get; set; }

        public Label(LabelTemplate template) : base(template) {
            LabelText = template.Label;
            LabelAlignment = template.LabelAlignment;
            VAlignment = template.VAlignment;
            
            LabelRect = new Rect(Vec.Zero, Size);

            if (template.HasFrameBorder && Size.Width > 2 && Size.Height > 2)
                LabelRect = LabelRect.Inflate(-1, -1);
        }

        protected override void Redraw() {
            base.Redraw();

            if (!OwnerDraw)
                Canvas.PrintStringAligned(CalcTopLeft(), LabelText, LabelAlignment, VAlignment, Size);
        }

        protected override Pigment DetermineMainPigment()
        {
            return Pigments[PigmentType.Window];
        }

        private Vec CalcTopLeft() {
            var vec = LabelRect.TopLeft;
            return vec.OffsetX((HasFrame) ? 2 : 0);
        }
    }
}