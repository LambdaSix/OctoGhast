namespace OctoGhast.Activities {
    public enum BoneSize : int {
        /// <summary>
        /// Very small bones, used for decoration or small traps
        /// </summary>
        Tiny,

        /// <summary>
        /// Bones usable for shaping into needles or other small tools
        /// </summary>
        Small,

        /// <summary>
        /// Larger bones, tibia/fibia, scapulas, etc.
        /// Bones that could be knapped into very primitive tools (axes, knives)
        /// </summary>
        Large,

        /// <summary>
        /// Very large bones from mutated creatures. Probably leg or arm bones
        /// Bones that are > 1kg or so
        /// </summary>
        Huge
    }
}