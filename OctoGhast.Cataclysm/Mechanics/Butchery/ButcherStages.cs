namespace OctoGhast.Cataclysm.Mechanics.Butchery {
    public enum ButcherStages {
        /// <summary>
        /// Skin the carcass, retrieving any hide or furs.
        /// </summary>
        Skinning,

        /// <summary>
        /// Field dress the carcass by removing organs, extends the lifetime of the carcass.
        /// </summary>
        FieldDressing,

        /// <summary>
        /// Cut the carcass into quarters for easier transport, will ruin furs/hides if performed prior to skinning.
        /// </summary>
        Quarter,
        
        /// <summary>
        /// Carefully butcher the carcass, retrieving as much as meat and by-products as possible.
        /// Requires a full butchery workshop.
        /// </summary>
        FullButchery,

        /// <summary>
        /// Quickly field-butcher the carcass, mostly only retrieving the meat.
        /// Only requires basic tools.
        /// </summary>
        QuickButchery
    }
}