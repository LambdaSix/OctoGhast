using System;
using OctoGhast.Spatial;
using OctoGhast.UserInterface.Core;
using OctoGhast.UserInterface.Templates;
using OctoGhast.UserInterface.Theme;

namespace OctoGhast.UserInterface.Controls
{
    public class PanelTemplate : ControlTemplate
    {
        /// <summary>
        /// The size of the Panel, defaults to 1x1
        /// </summary>
        public Size Size { get; set; }

        public PanelTemplate() {
            HasFrameBorder = true;
            CanHaveKeyboardFocus = false;
            MouseOverHighlight = false;
            Size = new Size(1, 1);
        }

        public override Size CalculateSize() {
            return Size;
        }
    }

    public class Panel : ControlBase
    {
        public Panel(PanelTemplate template) : base(template) {
            HasFrame = template.HasFrameBorder;
            CanHaveKeyboardFocus = template.CanHaveKeyboardFocus;
            MouseOverHighlight = template.MouseOverHighlight;
        }

        protected override Pigment DetermineMainPigment() {
            return Pigments[PigmentType.Window];
        }
    }
}