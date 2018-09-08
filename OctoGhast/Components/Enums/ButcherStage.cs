using System;
using System.ComponentModel;

namespace OctoGhast.Activities {
    [Flags]
    public enum ButcherStage {
        None,

        [Description("Field Dress")]
        FieldDress,

        [Description("Skin Pelt")]
        SkinPelt,

        [Description("Harvest Ingredients")]
        HarvestIngredients,

        [Description("Butcher Meat")]
        ButcherMeat,

        [Description("Finish")]
        Finished
    }
}