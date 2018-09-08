﻿using Capsicum.Interfaces;

namespace OctoGhast.Activities {
    public class AttributesComponent : IComponent {
        public int Strength { get; set; }
        public int Intelligence { get; set; }
        public int Dexterity { get; set; }
        public int Constitution { get; set; }
        public int Wisdom { get; set; }
        public int Charisma { get; set; }
    }
}