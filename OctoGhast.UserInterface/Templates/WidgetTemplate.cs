using OctoGhast.Spatial;
using OctoGhast.UserInterface.Core.Theme;

namespace OctoGhast.UserInterface.Templates
{
    public abstract class WidgetTemplate
    {
        public bool OwnerDraw { get; set; }

        public PigmentMapping Pigments { get; set; }

        public abstract Size CalculateSize();

        // TODO: PigmentMapping should be injected in. Revisit when object graph is known.

        public WidgetTemplate() {
            OwnerDraw = false;
            Pigments = new PigmentMapping();
        }
    }
}