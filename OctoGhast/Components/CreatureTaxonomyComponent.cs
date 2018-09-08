using Capsicum.Interfaces;

namespace OctoGhast.Activities {
    public class CreatureTaxonomyComponent : IComponent {

        /// <summary>
        /// How big the creature is.
        /// </summary>
        public ActorSize Size { get; set; }

        /// <summary>
        /// Is this creature the result of a mutation?
        /// </summary>
        public bool IsMutated { get; set; }

        /// <summary>
        /// Is this creature poisonous? (Consuming it's flesh will confer poison)
        /// </summary>
        public bool IsPoisonous { get; set; }

        /// <summary>
        /// Touching this creature may confer poison.
        /// </summary>
        public bool IsTouchPoisonous { get; set; }

        /// <summary>
        /// Is this creature Venomous (Bites to skin will confer toxins)
        /// </summary>
        public bool IsVenomous { get; set; }

        /// <summary>
        /// This creature is furred in some fashion.
        /// </summary>
        public bool HasFurredPelt { get; set; }

        /// <summary>
        /// This creature has usable skin
        /// </summary>
        public bool HasPelt { get; set; }

        /// <summary>
        /// This creature is large 
        /// </summary>
        public bool HasUsableBones { get; set; }
    }
}